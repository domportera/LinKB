using System.Runtime.InteropServices;
using System.Text;
using SharpHook.Data;

namespace KeyboardGUI.Wayland;

public unsafe partial class WaylandInput
{
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

    [LibraryImport("libc", SetLastError = true, EntryPoint = "mmap")]
    private static partial void* Mmap(void* addr, nint length, int prot, int flags, int fd, nint offset);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "munmap")]
    private static partial int Munmap(void* addr, nint length);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "fcntl")]
    private static partial int Fcntl(int fd, int cmd, nint arg);

    // uinput and input event constants
    private const int O_WRONLY = 0x0001;
    private const int O_RDONLY = 0x0000; // added for reading real keyboards
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
    public record struct input_event
    {
        [FieldOffset(0)] public timeval time;
        [FieldOffset(16)] public ushort type;
        [FieldOffset(18)] public ushort code;
        [FieldOffset(20)] public int value;
    }

    [StructLayout(LayoutKind.Sequential, Size = sizeof(long) * 2, Pack = 8)]
    public record struct timeval
    {
        public long tv_sec;
        public long tv_usec;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct uinput_user_dev
    {
        public fixed byte name[80];
        public input_id id;
        public int ff_effects_max;
        public fixed int absmax[64];
        public fixed int absmin[64];
        public fixed int absfuzz[64];
        public fixed int absflat[64];
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct input_id
    {
        public ushort bustype;
        public ushort vendor;
        public ushort product;
        public ushort version;
    }

    private int _uinputFileDescriptor = -1;
    private List<int> _realKeyboardFileDescriptors = new();

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
        _uinputFileDescriptor = Open(uinputPath, O_WRONLY | O_NONBLOCK);
        if (_uinputFileDescriptor < 0) throw new Exception("Failed to open " + uinputPath);

        int ret = Ioctl(_uinputFileDescriptor, UI_SET_EVBIT, EV_KEY); // pass by value
        if (ret < 0) throw new Exception($"{nameof(UI_SET_EVBIT)} failed");

        for (int code = KEY_MIN; code <= KEY_MAX; code++)
        {
            ret = Ioctl(_uinputFileDescriptor, UI_SET_KEYBIT, code); // pass by value
            if (ret < 0) throw new Exception($"{nameof(UI_SET_KEYBIT)} failed for key {code}");
        }

        var userInputDev = new uinput_user_dev();
        // set name
        string name = "LinKB Keyboard";
        var bytes = Encoding.ASCII.GetBytes(name);
        for (int i = 0; i < bytes.Length && i < 79; i++)
            userInputDev.name[i] = bytes[i];
        userInputDev.name[Math.Min(bytes.Length, 79)] = 0;

        userInputDev.id.bustype = 0x03; // USB
        userInputDev.id.vendor = 0x1234;
        userInputDev.id.product = 0x5678;
        userInputDev.id.version = 1;

        var writeRet = Write(_uinputFileDescriptor, &userInputDev, (nint)sizeof(uinput_user_dev)); // pass size
        if (writeRet < 0) throw new Exception($"write uinput_user_dev failed");
        ret = Ioctl(_uinputFileDescriptor, UI_DEV_CREATE, 0); // arg is ignored for UI_DEV_CREATE
        if (ret < 0) throw new Exception($"{nameof(UI_DEV_CREATE)} failed");
    }

    /// <summary>
    /// Inject a key event to the virtual keyboard.
    /// </summary>
    /// <param name="keyCode">The key to manipulate</param>
    /// <param name="value">0 = key release, 1 = key press, 2 = key repeat</param>
    public void InjectKeyEvent(KeyCode keyCode, int value)
    {
        input_event ev = new input_event
        {
            time = new timeval { tv_sec = 0, tv_usec = 0 },
            type = EV_KEY,
            code = (ushort)keyCode,
            value = value
        };
        
        var ret = Write(_uinputFileDescriptor, &ev, sizeof(input_event));
        if (ret == -1)
        {
            Console.Error.WriteLine("Failed to write input event 1");
        }

        input_event syn = new input_event
        {
            time = new timeval { tv_sec = 0, tv_usec = 0 },
            type = EV_SYN,
            code = SYN_REPORT,
            value = 0
        };
        ret = Write(_uinputFileDescriptor, &syn, sizeof(input_event));
        if (ret == -1)
        {
            Console.Error.WriteLine("Failed to write input event 2");
        }
    }

    private void EnumerateRealKeyboards()
    {
        const string inputDir = "/dev/input";
        foreach (var file in Directory.GetFiles(inputDir, "event*"))
        {
            int fd = Open(file, O_RDONLY | O_NONBLOCK); // use O_RDONLY
            if (fd >= 0)
            {
                _realKeyboardFileDescriptors.Add(fd);
            }
        }
    }

    public void ReadKeyboardEvents(IList<input_event> events)
    {
        for (var index = 0; index < _realKeyboardFileDescriptors.Count; index++)
        {
            var fd = _realKeyboardFileDescriptors[index];
            input_event ev = default;
            var readByteCount = Read(fd, &ev, sizeof(input_event));
            if (readByteCount == -1)
            {
                continue;
            }

            if (readByteCount != sizeof(input_event))
            {
                Console.Error.WriteLine("Failed to read input event");
            }
            else
            {
                Console.WriteLine($"Read event: {ev}");
                events.Add(ev);
            }
        }
    }

    public void Dispose()
    {
        if (_uinputFileDescriptor >= 0)
        {
            Ioctl(_uinputFileDescriptor, UI_DEV_DESTROY, 0); // arg is ignored
            Close(_uinputFileDescriptor);
            _uinputFileDescriptor = -1;
        }

        foreach (var fd in _realKeyboardFileDescriptors)
        {
            Close(fd);
        }

        _realKeyboardFileDescriptors.Clear();
    }
}