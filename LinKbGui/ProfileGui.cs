using ImGuiNET;
using ImGuiWindows;
using LinKb.Configuration;
using LinKb.Core;

namespace LinKbGui;

internal class ProfileGui
{
    private readonly KeyboardGridConfig _currentConfig;
    private readonly Lock _configLock = new();
    private readonly WindowRunner _windowRunner;

    private Task<string?>? _saveAsTask;

    public ProfileGui(KeyboardGridConfig config, WindowRunner windowRunner)
    {
        _currentConfig = config;
        _windowRunner = windowRunner;
    }

    // todo - menu source generation from yaml
    public void DrawFileMenu()
    {
        if (_saveAsTask is { IsCompleted: true })
        {
            var name = _saveAsTask.Result;
            _saveAsTask = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                Log.Debug("Save As task completed but no name was provided");
            }
            else
            {
                UserInfo.SaveAs(name, _currentConfig);
            }
        }

        if (ImGui.BeginMenu("File"))
        {
            var files = UserInfo.GetConfigFiles();
            if (ImGui.BeginMenu("Load"))
            {
                foreach (var file in files)
                {
                    var isSelected = file.Name == _currentConfig.Name;
                    if (ImGui.MenuItem(file.Name, "", isSelected))
                    {
                        if (!isSelected)
                        {
                            LoadTask(file);
                        }
                    }

                    if (ImGui.BeginPopupContextWindow("File Options"))
                    {
                        if (ImGui.MenuItem("Load"))
                        {
                            LoadTask(file);
                        }

                        if (ImGui.MenuItem("Delete"))
                        {
                            DeleteTask(file);
                        }

                        ImGui.EndPopup();
                    }
                }

                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Save"))
            {
                _ = UserInfo.Save(_currentConfig);
            }

            if (_saveAsTask == null && ImGui.MenuItem("Save As"))
            {
                _saveAsTask = _windowRunner.Show("Save as... ", new TextEntryWindow());
            }

            ImGui.EndMenu();
        }
    }

    private void DeleteTask(UserInfo.ConfigFileInfo file)
    {
    }

    private void LoadTask(UserInfo.ConfigFileInfo file)
    {
        var syncContext = SynchronizationContext.Current;
        Task.Run(async () =>
        {
            var result = await UserInfo.TryLoadConfig(file.FilePath);
            if (!result.Success)
            {
                return;
            }
            
            // todo: join back to calling thread/context
            
            _currentConfig.ReplaceContentsWith(result.Value);

        });
    }

    private class TextEntryWindow : IImguiDrawer<string>
    {
        private bool _complete;
        private string? _result;

        public void Init()
        {
        }

        public void OnRender(string windowName, double deltaSeconds, ImFonts fonts, float dpiScale)
        {
            ImGui.Text("Enter text");
            ImGui.InputText("##text", ref _result, 28);

            if (ImGui.IsKeyDown(ImGuiKey.Escape))
            {
                _result = null;
                _complete = true;
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Enter))
            {
                _complete = true;
            }
        }

        public void OnWindowUpdate(double deltaSeconds, out bool shouldClose)
        {
            shouldClose = _complete;
        }

        public void OnClose()
        {
        }

        public void OnFileDrop(string[] filePaths)
        {
        }

        public void OnWindowFocusChanged(bool changedTo)
        {
        }

        public string? Result => _result;
    }
}