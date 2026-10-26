using System.Globalization;

namespace Overmix.WindowsPlatform;

/// <summary>Windows virtual-key codes for the key names Shortcuts use (see Overmix.Core.Shortcuts.KeyNames).</summary>
internal static class KeyCodes
{
    private static readonly Dictionary<string, uint> Codes = Build();

    public static bool TryGet(string keyName, out uint virtualKey) => Codes.TryGetValue(keyName, out virtualKey);

    private static Dictionary<string, uint> Build()
    {
        var codes = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        for (var letter = 'A'; letter <= 'Z'; letter++) codes[letter.ToString()] = letter;
        for (var digit = 0; digit <= 9; digit++)
        {
            codes[digit.ToString(CultureInfo.InvariantCulture)] = (uint)(0x30 + digit);
            codes[$"NumPad{digit}"] = (uint)(0x60 + digit);
        }

        for (var function = 1; function <= 24; function++) codes[$"F{function}"] = (uint)(0x70 + function - 1);

        codes["Space"] = 0x20;
        codes["Enter"] = 0x0D;
        codes["Tab"] = 0x09;
        codes["Backspace"] = 0x08;
        codes["Insert"] = 0x2D;
        codes["Delete"] = 0x2E;
        codes["Home"] = 0x24;
        codes["End"] = 0x23;
        codes["PageUp"] = 0x21;
        codes["PageDown"] = 0x22;
        codes["Up"] = 0x26;
        codes["Down"] = 0x28;
        codes["Left"] = 0x25;
        codes["Right"] = 0x27;
        codes["Pause"] = 0x13;
        codes["ScrollLock"] = 0x91;
        codes["NumPadAdd"] = 0x6B;
        codes["NumPadSubtract"] = 0x6D;
        codes["NumPadMultiply"] = 0x6A;
        codes["NumPadDivide"] = 0x6F;
        codes["NumPadDecimal"] = 0x6E;
        codes["`"] = 0xC0;
        codes["-"] = 0xBD;
        codes["="] = 0xBB;
        codes["["] = 0xDB;
        codes["]"] = 0xDD;
        codes["\\"] = 0xDC;
        codes[";"] = 0xBA;
        codes["'"] = 0xDE;
        codes[","] = 0xBC;
        codes["."] = 0xBE;
        codes["/"] = 0xBF;
        return codes;
    }
}
