﻿using System;
using System.Text;
using System.Windows.Input;

namespace SPCode.Utils;

public class Hotkey
{
    public Key Key { get; }
    public ModifierKeys Modifiers { get; }

    public Hotkey(Key key, ModifierKeys modifiers)
    {
        Key = key;
        Modifiers = modifiers;
    }

    public Hotkey(string keys)
    {
        foreach (var key in keys.Split('+'))
        {
            if (Enum.TryParse(key, out Key parsedKey))
            {
                Key = parsedKey;
            }
            var newKey = key == "Ctrl" ? "Control" : key;
            if (Enum.TryParse(newKey, out ModifierKeys parsedModifiers))
            {
                Modifiers |= parsedModifiers;
            }
        }
    }

    public override string ToString()
    {
        var str = new StringBuilder();

        if (Modifiers.HasFlag(ModifierKeys.Control))
        {
            str.Append("Ctrl+");
        }

        if (Modifiers.HasFlag(ModifierKeys.Shift))
        {
            str.Append("Shift+");
        }

        if (Modifiers.HasFlag(ModifierKeys.Alt))
        {
            str.Append("Alt+");
        }

        if (Modifiers.HasFlag(ModifierKeys.Windows))
        {
            str.Append("Win+");
        }

        str.Append(Key);

        return str.ToString();
    }
}