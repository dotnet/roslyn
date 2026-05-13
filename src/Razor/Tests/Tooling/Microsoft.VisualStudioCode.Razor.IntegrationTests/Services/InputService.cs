// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Services for keyboard and mouse input in integration tests.
/// </summary>
public class InputService(IntegrationTestServices testServices) : ServiceBase(testServices)
{
    // Platform-specific primary modifier key (Cmd on macOS, Ctrl elsewhere)
    private static readonly string s_primaryModifier = OperatingSystem.IsMacOS() ? "Meta" : "Control";

    private static string GetKeyString(SpecialKey key) => key switch
    {
        SpecialKey.Enter => "Enter",
        SpecialKey.Escape => "Escape",
        SpecialKey.Backspace => "Backspace",
        SpecialKey.End => OperatingSystem.IsMacOS() ? "Meta+ArrowRight" : "End",
        SpecialKey.Space => "Space",
        SpecialKey.ArrowLeft => "ArrowLeft",
        SpecialKey.ArrowRight => "ArrowRight",
        SpecialKey.F12 => "F12",
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
    };

    /// <summary>
    /// Types text at the current cursor position.
    /// </summary>
    public async Task TypeAsync(string text, int delayMs = 50)
    {
        await TestServices.Playwright.Page.Keyboard.TypeAsync(text, new Microsoft.Playwright.KeyboardTypeOptions { Delay = delayMs });
    }

    /// <summary>
    /// Presses a key or key combination.
    /// </summary>
    public async Task PressAsync(SpecialKey key)
    {
        await TestServices.Playwright.Page.Keyboard.PressAsync(GetKeyString(key));
    }

    /// <summary>
    /// Presses a special key with Shift.
    /// </summary>
    /// <param name="key">The key to press with Shift</param>
    public async Task PressWithShiftAsync(SpecialKey key)
    {
        await TestServices.Playwright.Page.Keyboard.PressAsync($"Shift+{GetKeyString(key)}");
    }

    /// <summary>
    /// Presses a character key with the platform-appropriate primary modifier (Cmd on macOS, Ctrl on Windows/Linux).
    /// </summary>
    /// <param name="key">The character key to press with the modifier (e.g., 's' for save)</param>
    public async Task PressWithPrimaryModifierAsync(char key)
    {
        await TestServices.Playwright.Page.Keyboard.PressAsync($"{s_primaryModifier}+{key}");
    }

    /// <summary>
    /// Presses a special key with the platform-appropriate primary modifier (Cmd on macOS, Ctrl on Windows/Linux).
    /// </summary>
    /// <param name="key">The key to press with the modifier</param>
    public async Task PressWithPrimaryModifierAsync(SpecialKey key)
    {
        await TestServices.Playwright.Page.Keyboard.PressAsync($"{s_primaryModifier}+{GetKeyString(key)}");
    }

    /// <summary>
    /// Presses a character key with Shift and the platform-appropriate primary modifier.
    /// </summary>
    /// <param name="key">The character key to press with Shift+modifier</param>
    public async Task PressWithShiftPrimaryModifierAsync(char key)
    {
        await TestServices.Playwright.Page.Keyboard.PressAsync($"{s_primaryModifier}+Shift+{key}");
    }

    /// <summary>
    /// Presses a character key with Control, regardless of the operating system.
    /// Use this for VS Code shortcuts that use Control even on macOS (e.g., Ctrl+G for "Go to Line").
    /// </summary>
    /// <param name="key">The character key to press with Control</param>
    public async Task PressWithControlAsync(char key)
    {
        await TestServices.Playwright.Page.Keyboard.PressAsync($"Control+{key}");
    }
}
