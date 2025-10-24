using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using ImGuiWindows;
using InputHooks;
using LinKb.Configuration;
using LinKb.Core;

/*using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;*/

namespace LinKbGui;
/*
internal interface IReset
{
    ValueTask On(object? context = null);
    ValueTask Off(object? context = null);
    bool AcceptsContext => false;
    bool IsOn { get; }
}

[AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder))]
[StructLayout(LayoutKind.Auto)]
internal readonly struct ValueTaskReference(ValueTask task, object context)
{

}*/

internal class KeyboardConfigWindow : IImguiDrawer
{
    private readonly MidiKeyboardGrid _keyboardGrid;
    private readonly IEventProvider _hooks;
    private readonly Dictionary<KeyCode, bool> _pressedKeys = [];
    private readonly Lock _keyLock = new();
    
    #if DEBUG
    private readonly Stopwatch _stopwatch = new();
    private long _lastFrameTicks = long.MaxValue;
    #endif

    public KeyboardConfigWindow(MidiKeyboardGrid keyboardGrid, IEventProvider hooks)
    {
        _keyboardGrid = keyboardGrid;
        _hooks = hooks;
        _hooks.InputEventReceived += OnKeyEvent;
    }

    private void OnKeyEvent(KeyboardEventArgs e)
    {
        var keys = e.KeyCodes;
        if (keys.Length == 0)
        {
            Log.Error("Received keyboard event with no keys?");
            return;
        }

        var key = keys[0];
        if (keys.Length > 1)
        {
            Log.Warn($"Received keyboard event that's mapped to multiple keys - only the first will be used: {key}");
        }

        Log.Debug($"Received key event: {key} ({e.IsDown}) ({e.IsSimulated})");

        lock (_keyLock)
        {
            _pressedKeys[key] = e.IsDown;
        }
    }

    public void Init()
    {
    }

    public unsafe void OnRender(string windowName, double deltaSeconds, ImFonts fonts, float dpiScale)
    {
        #if DEBUG
        var ticks = _lastFrameTicks;
        var ms = ticks / (Stopwatch.Frequency / 1000.0);
        _stopwatch.Restart();
        #endif
        
        // render the grid config ui
        var kbWidth = _keyboardGrid.Width;
        var kbHeight = _keyboardGrid.Height;
        var consumedKey = false;

        // render as a table
        ReadOnlySpan3D<KeyCode> loaded = default;
        if (DrawFileMenu(_keyboardGrid.Keymap, out loaded))
        {
            _keyboardGrid.ApplyKeymap(loaded);
        }

        SameLineSeparator();

        if (ImGui.Checkbox("Enable key events", ref _keyboardGrid.EnableKeyEvents))
        {
            // clear depressed keys when toggling
            lock (_keyLock)
            {
                _pressedKeys.Clear();
            }
        }

        var layer = _keyboardGrid.Layer;

        SameLineSeparator();
        DrawModKeys(layer);

        // get pressed keys
        KeyCode? pressedKey;
        KeyCode[] pressedKeys;
        lock (_keyLock)
        {
            pressedKeys = _pressedKeys.Where(kv => kv.Value).Select(kv => kv.Key).ToArray();
            pressedKey = pressedKeys.Length == 1 ? pressedKeys[0] : null;
        }

        SameLineSeparator();
        DrawPressedKeys(pressedKeys);
        
        #if DEBUG
        SameLineSeparator();
        ImGui.Text($"Frame time: {ms}ms");
        #endif

        ImGui.Separator();

        var availableHeight = ImGui.GetContentRegionAvail().Y;
        var perCellHeight = availableHeight / kbHeight - ImGui.GetStyle().CellPadding.Y * 2;

        const ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders |
                                           ImGuiTableFlags.RowBg |
                                           ImGuiTableFlags.Resizable |
                                           ImGuiTableFlags.SizingStretchSame;

        const ImGuiTableColumnFlags columnFlags = ImGuiTableColumnFlags.WidthStretch |
                                                  ImGuiTableColumnFlags.NoResize |
                                                  ImGuiTableColumnFlags.NoReorder |
                                                  ImGuiTableColumnFlags.NoSort |
                                                  ImGuiTableColumnFlags.NoHide;

        const ImGuiTableRowFlags rowFlags = ImGuiTableRowFlags.None;

        if (ImGui.BeginTable("KeyboardConfigTable", kbWidth, tableFlags, new Vector2(0, -1)))
        {
            const string colBaseLabel = "#col";
            Span<char> colLabel = stackalloc char[colBaseLabel.Length + 1];
            colBaseLabel.AsSpan().CopyTo(colLabel);
            for (int col = 0; col < kbWidth; col++)
            {
                colLabel[^1] = *(char*)&col;
                ImGui.TableSetupColumn(colLabel, columnFlags);
            }

            for (int row = kbHeight - 1; row >= 0; row--)
            {
                ImGui.TableNextRow(rowFlags);

                for (int col = 0; col < kbWidth; col++)
                {
                    ImGui.TableSetColumnIndex(col);

                    DrawCell(grid: _keyboardGrid,
                        layer: layer,
                        size: new Vector2(-1, perCellHeight),
                        col: col,
                        row: row,
                        pressedKey: consumedKey
                            ? null
                            : pressedKey,
                        hasConsumed: out var hasConsumed);

                    consumedKey |= hasConsumed;
                }
            }

            ImGui.EndTable();
        }

        // ImGui.PopStyleVar();

        if (consumedKey)
        {
            // if we consumed a key, clear the depressed keys?
            lock (_keyLock)
            {
                _pressedKeys.Clear();
            }
        }

        #if DEBUG
        _lastFrameTicks = _stopwatch.ElapsedTicks;
        #endif
        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SameLineSeparator()
        {
            ImGui.SameLine();
            ImGui.Text("\t|\t");
            ImGui.SameLine();
        }
    }

    private static void DrawPressedKeys(KeyCode[] depressedKeys)
    {
        ImGui.Text("Currently depressed keys:");
        if (depressedKeys.Length > 0)
        {
            foreach (var key in depressedKeys)
            {
                var name = KeyInfo.ToName.GetValueOrDefault(key, "Unknown");
                ImGui.SameLine();
                ImGui.Text(name);
            }
        }
        else
        {
            ImGui.SameLine();
            ImGui.Text("None");
        }
    }

    private void DrawModKeys(Layer layer)
    {
        ImGui.Text("Mod keys: ");
        ImGui.SameLine();
        KeyCheckBox(KeyCode.Mod1, out var mod1Pressed);
        ImGui.SameLine();
        KeyCheckBox(KeyCode.Mod2, out var mod2Pressed);
        ImGui.SameLine();
        KeyCheckBox(KeyCode.Mod3, out var mod3Pressed);

        var mod1 = mod1Pressed ? 1 : 0;
        var mod2 = mod2Pressed ? 1 : 0;
        var mod3 = mod3Pressed ? 1 : 0;
        var simulatedLayer = mod1 | mod2 << 1 | mod3 << 2;
        ImGui.SameLine();
        ImGui.Text($"({layer} - {(Layer)simulatedLayer})");
        if (layer != (Layer)simulatedLayer)
        {
            Log.Warn("(simulated layer does not match current layer)");
        }
    }

    private bool KeyCheckBox(KeyCode key, out bool state)
    {
        var mod1Pressed = _keyboardGrid.IsKeyPressed(key);
        var clicked = ImGui.Checkbox(KeyInfo.ToName[key], ref mod1Pressed);
        state = mod1Pressed;
        return clicked;
    }

    private static bool DrawFileMenu(ReadOnlySpan3D<KeyCode> config, out ReadOnlySpan3D<KeyCode> loadedConfig)
    {
        if (ImGui.Button("Save"))
        {
            _ = LayoutSerializer.Save(UserInfo.DefaultConfigFile, config);
        }

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        if (ImGui.Button("Load"))
        {
            loadedConfig = LayoutSerializer
                .LoadOrCreateKeymap(UserInfo.DefaultConfigFile, config.XLength, config.YLength, config.ZLength).Result;
            return true;
        }

        loadedConfig = default;
        return false;
    }

    private static unsafe void DrawCell(MidiKeyboardGrid grid, Layer layer, Vector2 size, int col, int row,
        KeyCode? pressedKey, out bool hasConsumed)
    {
        hasConsumed = false;
        var currentKey = grid.GetKey(col, row, out var foundLayer, layer);
        var isPadPressed = grid.IsPadPressed(col, row);

        var keyOnCurrentLayer = foundLayer == layer;

        // get color based on key
        var color = grid.GetColorVector(col, row);
        const float brightness = 0.4f;
        color *= brightness;
        color.W = 1;

        var axes = grid.GetAxes(col, row);
        DrawTouchDetails(size, axes, color);

        if (isPadPressed)
        {
            // highlight the cell if the key is currently pressed
            color = new Vector4(0f, 1f, 0f, keyOnCurrentLayer ? 0.5f : 0.2f);
            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
        }
        else if (!keyOnCurrentLayer)
        {
            color = color with { W = color.W * 0.5f };
            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
        }

        // convert col/row to a unique id for the combo box
        // to avoid conflicts with other combo boxes
        // we use stackalloc to avoid heap allocations
        const string baseLabel = "##";
        var baseLabelLength = baseLabel.Length;
        Span<char> comboLabel = stackalloc char[2 + baseLabelLength];

        baseLabel.AsSpan().CopyTo(comboLabel);

        var numericPortion = comboLabel[baseLabelLength..];
        var colId = col + 1;
        var rowId = row + 1;
        numericPortion[0] = *(char*)&colId;
        numericPortion[1] = *(char*)&rowId;

        var shownKeyName = currentKey == KeyCode.Undefined
            ? ""
            : KeyInfo.MultilineNames.GetValueOrDefault(currentKey) ?? "Unknown";

        Span<char> buttonLabel = stackalloc char[shownKeyName.Length + comboLabel.Length];
        shownKeyName.AsSpan().CopyTo(buttonLabel);
        comboLabel.CopyTo(buttonLabel[shownKeyName.Length..]);

        const string contextMenuName = "##contextMenu";
        Span<char> selectionMenuLabel = stackalloc char[contextMenuName.Length + buttonLabel.Length];
        contextMenuName.AsSpan().CopyTo(selectionMenuLabel);
        buttonLabel.CopyTo(selectionMenuLabel[contextMenuName.Length..]);
        const ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonLeft;

        if (ImGui.Button(buttonLabel, size))
        {
            ImGui.OpenPopupOnItemClick(selectionMenuLabel, popupFlags);
        }

        if (ImGui.BeginPopupContextItem(selectionMenuLabel))
        {
            if (pressedKey != null)
            {
                var key = pressedKey.Value == KeyCode.Escape ? KeyCode.Undefined : pressedKey.Value;
                if (grid.TrySetKey(col, row, layer, key, out var reason))
                {
                    hasConsumed = true;
                    ImGui.CloseCurrentPopup();
                }
                else
                {
                    HandleFailedKeyAssignment(key, reason);
                }
            }
            else
            {
                foreach (var (key, name) in KeyInfo.OrderedKeys)
                {
                    if (layer != Layer.Layer1 && KeyExtensions.IsMod(key))
                    {
                        continue;
                    }

                    const ImGuiSelectableFlags selectableFlags = ImGuiSelectableFlags.SpanAllColumns;

                    var isSelected = currentKey == key;
                    if (ImGui.Selectable(name, isSelected, selectableFlags))
                    {
                        if (!grid.TrySetKey(col, row, layer, key, out var reason))
                        {
                            HandleFailedKeyAssignment(key, reason);
                        }
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
            }

            ImGui.EndPopup();
        }

        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
    }

    private static void DrawTouchDetails(in Vector2 size, in Vector3 axes, in Vector4 color)
    {
        if (axes == PadStatusExtensions.DefaultAxisValue)
            return;
        
        var cellPadding = ImGui.GetStyle().CellPadding;
        var margin = ImGui.GetStyle().ItemInnerSpacing;
        var totalCellPadding = cellPadding * 2;
        // draw a point on normalized xy pos inside cell,
        // with z determining the size of the point
        var cursorPos = ImGui.GetCursorPos() + cellPadding with {Y = cellPadding.Y + margin.Y };
        var actualSize = new Vector2(
            x: ImGui.GetContentRegionAvail().X + totalCellPadding.X, 
            y: size.Y + totalCellPadding.Y);
            
        var touchPos = new Vector2(axes.X, axes.Y);
        var sizeIndicatorColor = new Vector4(1, 1, 1, 0.2f);

        // draw pressure indicator
        var halfSize = actualSize * 0.5f;
        var sqRtZ = MathF.Sqrt(axes.Z);
        var outSet = halfSize * sqRtZ - cellPadding;
        var center = cursorPos + halfSize;
        ImGui.GetForegroundDrawList().AddRectFilled(
            p_min: center - outSet,
            p_max: center + outSet,
            col: ImGui.ColorConvertFloat4ToU32(sizeIndicatorColor),
            rounding: 0f
        );
            
        // draw x/y crosshair
        var positionIndicatorOffset = new Vector2(actualSize.X * touchPos.X, actualSize.Y * (1 - touchPos.Y));
        var actualPos = cursorPos + positionIndicatorOffset;
        var crosshairColor = new Vector4(1, 0, 0, color.W);
        
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (axes.X != PadStatusExtensions.DefaultAxisValue.X)
        {
            // vertical line, X value
            ImGui.GetForegroundDrawList().AddLine(
                p1: actualPos with { Y = cursorPos.Y },
                p2: actualPos with { Y = cursorPos.Y + actualSize.Y },
                col: ImGui.ColorConvertFloat4ToU32(crosshairColor),
                thickness: 1);
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (axes.Y != PadStatusExtensions.DefaultAxisValue.Y)
        {
            // horizontal line, Y value
            ImGui.GetForegroundDrawList().AddLine(
                p1: actualPos with { X = cursorPos.X },
                p2: actualPos with { X = cursorPos.X + actualSize.X },
                col: ImGui.ColorConvertFloat4ToU32(crosshairColor),
                thickness: 1);
        }
    }

    private static void HandleFailedKeyAssignment(KeyCode key, string reason) => Log.Error(reason);


    public void OnClose()
    {
        _hooks.InputEventReceived -= OnKeyEvent;
    }

    #region Unimplemented

    public void OnWindowUpdate(double deltaSeconds, out bool shouldClose) => shouldClose = false;

    public void OnFileDrop(string[] filePaths)
    {
    }

    public void OnWindowFocusChanged(bool changedTo)
    {
    }

    #endregion Unimplemented
}