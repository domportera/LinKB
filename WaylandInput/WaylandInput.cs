using System.Runtime.InteropServices;
using System.Text;
// ReSharper disable InconsistentNaming

namespace WaylandInput;

internal unsafe partial class WaylandInput
{
    public enum KeyEvent {Release = 0, Press = 1, Repeat = 2}
    
    [LibraryImport("libc", SetLastError = true, EntryPoint = "ioctl")]
    private static partial int Ioctl(int fd, int request, nint arg);

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "open")]
    private static partial int Open(string pathname, int flags);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "write")]
    private static partial long Write(int fd, void* buf, nint count);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "close")]
    private static partial int Close(int fd);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "read")]
    private static partial long Read(int fd, void* buf, nint count);

    // uinput and input event constants
    //
    private const int O_WRONLY = 0x0001;
    private const int O_RDONLY = 0x0000; // added for reading real keyboards
    private const int O_RDWR = 0x0002;
    private const int O_NONBLOCK = 0x800;
    private const int EV_KEY = 0x01;
    private const int EV_SYN = 0x00;
    private const int SYN_REPORT = 0;
    private const int UI_SET_EVBIT = 0x40045564;
    private const int UI_SET_KEYBIT = 0x40045565;
    private const int UI_DEV_CREATE = 0x5501;
    private const int UI_DEV_DESTROY = 0x5502;

    // Keycode range for generic keyboard
    private const int KEY_MIN = 1;
    private const int KEY_MAX = 255;

    // Structs for uinput
    [StructLayout(LayoutKind.Explicit, Size = sizeof(long) * 2 + sizeof(ushort) * 2 + sizeof(int))]
    public record struct InputEvent
    {
        [FieldOffset(0)] public Time time;
        [FieldOffset(16)] public ushort type;
        [FieldOffset(18)] public ushort code;
        [FieldOffset(20)] public int value;

        [StructLayout(LayoutKind.Sequential, Size = sizeof(long) * 2, Pack = 8)]
        public record struct Time
        {
            public long Seconds;
            public long Microseconds;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct UserInputDevice
    {
        public fixed byte Name[80];
        public InputId Id;
        public int FfEffectsMax;
        public fixed int AbsMax[64];
        public fixed int AbsMin[64];
        public fixed int AbsFuzz[64];
        public fixed int AbsFlat[64];

        [StructLayout(LayoutKind.Sequential)]
        public struct InputId
        {
            public ushort BusType;
            public ushort Vendor;
            public ushort Product;
            public ushort Version;
        }
    }

    private int _uInputFileDescriptorWrite = -1;
    private readonly List<int> _realKeyboardFileDescriptors = new();

    public void Init()
    {
        CreateVirtualKeyboard();
        Console.WriteLine("Created virtual keyboard");
        EnumerateRealKeyboards();
        Console.WriteLine("Enumerated real keyboards");
        foreach (var kb in _realKeyboardFileDescriptors)
        {
            Console.WriteLine($"Real keyboard fd: {kb}");
        }
    }

    private void CreateVirtualKeyboard()
    {
        const string uinputPath = "/dev/uinput";
        _uInputFileDescriptorWrite = Open(uinputPath, O_WRONLY | O_NONBLOCK);
        if (_uInputFileDescriptorWrite < 0) throw new Exception("Failed to open " + uinputPath);

        if (Ioctl(_uInputFileDescriptorWrite, UI_SET_EVBIT, EV_KEY) < 0)
        {
            throw new Exception($"{nameof(UI_SET_EVBIT)} failed");
        }

        // todo - these codes likely need to match that of LinuxKeyCodes
        for (int code = KEY_MIN; code <= KEY_MAX; code++)
        {
            if (Ioctl(_uInputFileDescriptorWrite, UI_SET_KEYBIT, code) < 0)
            {
                throw new Exception($"{nameof(UI_SET_KEYBIT)} failed for key {code}");
            }
        }

        var userInputDev = new UserInputDevice();
        // set name
        string name = "LinKB Keyboard";
        var bytes = Encoding.ASCII.GetBytes(name);
        for (int i = 0; i < bytes.Length && i < 79; i++)
        {
            userInputDev.Name[i] = bytes[i];
        }
        
        userInputDev.Name[Math.Min(bytes.Length, 79)] = 0;

        userInputDev.Id.BusType = 0x03; // USB
        userInputDev.Id.Vendor = 0x1234;
        userInputDev.Id.Product = 0x5678;
        userInputDev.Id.Version = 1;

        if (Write(_uInputFileDescriptorWrite, &userInputDev, sizeof(UserInputDevice)) < 0)
        {
            throw new Exception($"write {nameof(userInputDev)} failed");
        }
        
        if (Ioctl(_uInputFileDescriptorWrite, UI_DEV_CREATE, 0) < 0) // arg is ignored for UI_DEV_CREATE
        {
            throw new Exception($"{nameof(UI_DEV_CREATE)} failed");
        }
    }
    

    /// <summary>
    /// Inject a key event to the virtual keyboard.
    /// </summary>
    /// <param name="linuxKc">The key to manipulate</param>
    /// <param name="evt">0 = key release, 1 = key press, 2 = key repeat</param>
    public void InjectKeyEvent(LinuxKC linuxKc, KeyEvent evt)
    {
        var ev = new InputEvent
        {
            time = new InputEvent.Time { Seconds = 0, Microseconds = 0 },
            type = EV_KEY,
            code = (ushort)linuxKc,
            value = (int)evt
        };
        
        if (Write(_uInputFileDescriptorWrite, &ev, sizeof(InputEvent)) == -1)
        {
            Console.Error.WriteLine("Failed to write input event 1");
        }

        var syn = new InputEvent
        {
            time = new InputEvent.Time { Seconds = 0, Microseconds = 0 },
            type = EV_SYN,
            code = SYN_REPORT,
            value = 0
        };
        
        if (Write(_uInputFileDescriptorWrite, &syn, sizeof(InputEvent)) == -1)
        {
            Console.Error.WriteLine("Failed to write input event 2");
        }
    }

    private void EnumerateRealKeyboards()
    {
        const string inputDir = "/dev/input";
        foreach (var file in Directory.GetFiles(inputDir, "event*"))
        {
            int fd = Open(file, O_RDONLY | O_NONBLOCK);
            if (fd >= 0)
            {
                _realKeyboardFileDescriptors.Add(fd);
            }
        }
    }

    public void ReadKeyboardEvents(IList<(InputEvent Event, int DeviceId, bool IsVirtual)> events)
    {
        for (var index = 0; index < _realKeyboardFileDescriptors.Count; index++)
        {
            var fd = _realKeyboardFileDescriptors[index];
            ReadKb(events, fd, false);
        }
        
        ReadKb(events, _uInputFileDescriptorWrite, true);
    }

    private static void ReadKb(IList<(InputEvent Event, int DeviceId, bool IsVirtual)> events, int fd, bool isVirtual)
    {
        InputEvent ev = default;
        var readByteCount = Read(fd, &ev, sizeof(InputEvent));
        if (readByteCount == -1)
        {
            return;
        }

        if (readByteCount != sizeof(InputEvent))
        {
            Console.Error.WriteLine("Failed to read input event");
        }
        else if (ev.type == EV_KEY)
        {
            //Console.WriteLine($"Read event: {ev}"); // todo : mouse support!
            events.Add((ev, fd, isVirtual));
        }
    }

    public void Dispose()
    {
        if (_uInputFileDescriptorWrite >= 0)
        {
            Ioctl(_uInputFileDescriptorWrite, UI_DEV_DESTROY, 0); // arg is ignored
            Close(_uInputFileDescriptorWrite);
            _uInputFileDescriptorWrite = -1;
        }

        foreach (var fd in _realKeyboardFileDescriptors)
        {
            Close(fd);
        }

        _realKeyboardFileDescriptors.Clear();
    }
    
}