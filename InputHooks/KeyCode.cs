namespace InputHooks;

public enum KeyCode : ushort
{
    Undefined = 0,

    //@formatter:off
    // number row
    N0, N1, N2, N3, N4, N5, N6, N7, N8, N9, 
    //@formatter:on
    
    //@formatter:off
    // letters (ordered by QWERTY)
    Q, W, E, R, T,     Y, U, I, O, P,
    A, S, D, F, G,     H, J, K, L, 
    Z, X, C, V, B,     N, M,
    //@formatter:on

    // editing & deletion
    Backspace,
    Insert,
    Delete,

    // Navigation and whitespace
    PageUp,
    PageDown,
    End,
    Home,

    Up,
    Right,
    Down,
    Left,

    Tab,
    Enter,
    Space,
    Escape,

    // numpad
    //@formatter:off
    NumPad0, NumPad1, NumPad2, NumPad3, NumPad4, NumPad5, NumPad6, NumPad7, NumPad8, NumPad9,
    //@formatter:on
    
    NumPadAsterisk,
    NumPadPlus,
    NumPadMinus,
    NumPadPeriod,
    NumPadForwardSlash,
    NumPadEnter,
    NumPadEquals,
    NumPadSeparator,
    NumPadClear,

    // lock keys
    CapsLock,
    NumLock,
    ScrollLock,

    // application
    //@formatter:off
    App1, App2, App3, App4, AppCalculator, AppBrowser, AppMail,
    BrowserBack, BrowserForward, BrowserRefresh, BrowserStop, BrowserSearch, BrowserFavorites, BrowserHome,
    MediaPlay, MediaNext, MediaPrevious, MediaStop, MediaEject, MediaSelect,
    //@formatter:on

    // system
    PrintScreen,
    Pause,
    VolumeDown,
    VolumeUp,
    VolumeMute,
    ContextMenu,
    Help,
    Final,
    Function,
    VendorKey,
    Sleep,
    Power,
    Process,
    Vc102,

    // IME
    AlphaNumeric,
    ImeOn,
    ImeOff,
    ChangeInputSource,
    Accept,
    Cancel,
    ModeChange,

    // language-specific
    Hiragana,
    Katakana,
    KatakanaHiragana,
    Convert,
    NonConvert,
    Kana,
    Kanji,
    Hangul,
    Junja,
    Hanja,
    Yen,
    JpComma,


    // punctuation & symbols
    Minus,
    Equals,
    OpenBracket,
    CloseBracket,
    Semicolon,
    Quote,
    BackQuote,
    Comma,
    Period,
    Slash,
    Backslash,
    Underscore,

    // modifiers
    LeftMeta,
    RightMeta,
    RightAlt,
    RightControl,
    RightShift,
    LeftAlt,
    LeftControl,
    LeftShift,

    // function keys
    F1,
    F2,
    F3,
    F4,
    F5,
    F6,
    F7,
    F8,
    F9,
    F10,
    F11,
    F12,
    F13,
    F14,
    F15,
    F16,
    F17,
    F18,
    F19,
    F20,
    F21,
    F22,
    F23,
    F24,
    
    // mouse buttons
    //@formatter:off
    MouseLeft, MouseRight, MouseMiddle, MouseBack, MouseForward, MouseTask,
    //@formatter:on


    /// <summary>
    /// Modifier key 1 - not to be used outside of this application
    /// This forms the first bit of a layer number
    /// </summary>
    Mod1 = ushort.MaxValue - Layer.Layer2 + 1,

    /// <summary>
    /// Modifier key 2 - not to be used outside of this application
    /// This forms the second bit of a layer number
    /// </summary>
    Mod2 = ushort.MaxValue - Layer.Layer3 + 1,

    /// <summary>
    /// Modifier key 3 - not to be used outside of this application
    /// This forms the third bit of a layer number
    /// </summary>
    Mod3 = ushort.MaxValue - Layer.Layer4 + 1,
}