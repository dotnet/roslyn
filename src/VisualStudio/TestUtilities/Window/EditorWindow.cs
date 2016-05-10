// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using EnvDTE;
using Roslyn.VisualStudio.Test.Utilities.Interop;
using Roslyn.VisualStudio.Test.Utilities.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>Provides a means of interacting with the active editor window in the Visual Studio host.</summary>
    public class EditorWindow
    {
        private readonly VisualStudioInstance _visualStudioInstance;
        private readonly EditorWindowWrapper _editorWindowWrapper;

        internal EditorWindow(VisualStudioInstance visualStudioInstance)
        {
            _visualStudioInstance = visualStudioInstance;

            var integrationService = _visualStudioInstance.IntegrationService;
            _editorWindowWrapper = integrationService.Execute<EditorWindowWrapper>(typeof(EditorWindowWrapper), nameof(EditorWindowWrapper.Create), (BindingFlags.Public | BindingFlags.Static));
        }

        public string CurrentLineTextBeforeCursor
        {
            get
            {
                // Clear the current selected text, if any, so it is not included in the returned value
                if (!string.IsNullOrEmpty(TextSelection.Text))
                {
                    TextSelection.CharLeft(Extend: false, Count: 1);
                }

                // Select everything from the cursor to the beginning of the line
                TextSelection.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstColumn, Extend: true);

                var currentLineTextBeforeCursor = TextSelection.Text;

                // Restore the cursor to its previous position
                if (currentLineTextBeforeCursor != string.Empty)
                {
                    TextSelection.CharRight(Extend: false, Count: 1);
                }

                return currentLineTextBeforeCursor;
            }
        }

        public string CurrentLineTextAfterCursor
        {
            get
            {
                // Clear the current selected text, if any, so it is not included in the returned value
                if (!string.IsNullOrEmpty(TextSelection.Text))
                {
                    TextSelection.CharRight(Extend: false, Count: 1);
                }

                // Select everything from the cursor to the end of the line
                TextSelection.EndOfLine(Extend: true);

                var currentLineTextAfterCursor = TextSelection.Text;

                // Restore the cursor to its previous position
                if (currentLineTextAfterCursor != string.Empty)
                {
                    TextSelection.CharLeft(Extend: false, Count: 1);
                }

                return currentLineTextAfterCursor;
            }
        }

        public string Text
        {
            get
            {
                return _editorWindowWrapper.Contents;
            }

            set
            {
                _editorWindowWrapper.Contents = value;
            }
        }

        private TextSelection TextSelection => (TextSelection)(_visualStudioInstance.Dte.ActiveDocument.Selection);

        public void Activate() => _visualStudioInstance.Dte.ActiveDocument.Activate();

        public void ClearTextSelection() => TextSelection?.Cancel();

        public void Find(string expectedText)
        {
            Activate();
            ClearTextSelection();

            var dteFind = _visualStudioInstance.Dte.Find;

            dteFind.Action = vsFindAction.vsFindActionFind;
            dteFind.FindWhat = expectedText;
            dteFind.MatchCase = true;
            dteFind.MatchInHiddenText = true;
            dteFind.MatchWholeWord = true;
            dteFind.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxLiteral;
            dteFind.Target = vsFindTarget.vsFindTargetCurrentDocument;

            var findResult = dteFind.Execute();

            if (findResult != vsFindResult.vsFindResultFound)
            {
                throw new Exception($"The specified text was not found. ExpectedText: '{expectedText}'; ActualText: '{Text}'");
            }
        }

        public void PlaceCursor(string marker) => Find(marker);

        public Task TypeTextAsync(string text, int wordsPerMinute = 120)
        {
            Activate();
            return SendKeysAsync(text.Replace("\r\n", "\r").Replace("\n", "\r"), wordsPerMinute);
        }

        private async Task SendKeysAsync(string text, int wordsPerMinute)
        {
            var charactersPerSecond = (wordsPerMinute * 4.5) / 60;
            var delayBetweenCharacters = (int)((1 / charactersPerSecond) * 1000);

            foreach (var character in text)
            {
                var foregroundWindowHandle = IntPtr.Zero;
                var inputBlocked = false;

                try
                {
                    inputBlocked = User32.BlockInput(true);
                    foregroundWindowHandle = User32.GetForegroundWindow();

                    var activeWindowHandle = (IntPtr)(_visualStudioInstance.Dte.ActiveWindow.HWnd);

                    if (activeWindowHandle == IntPtr.Zero)
                    {
                        activeWindowHandle = (IntPtr)(_visualStudioInstance.Dte.MainWindow.HWnd);
                    }

                    IntegrationHelper.SetFocus(activeWindowHandle);

                    var vk = User32.VkKeyScan(character);

                    if (vk == -1)
                    {
                        SendCharacter(character);
                    }
                    else
                    {
                        if ((vk & 0x0100) != 0)  // SHIFT
                        {
                            SendKey(User32.VK_SHIFT);
                        }

                        if ((vk & 0x0200) != 0)  // CTRL
                        {
                            SendKey(User32.VK_CONTROL);
                        }

                        if ((vk & 0x0400) != 0)  // ALT
                        {
                            SendKey(User32.VK_MENU);
                        }

                        SendKey((ushort)(vk & 0xFF));

                        if ((vk & 0x0100) != 0)  // SHIFT
                        {
                            SendKey(User32.VK_SHIFT, User32.KEYEVENTF_KEYUP);
                        }

                        if ((vk & 0x0200) != 0)  // CTRL
                        {
                            SendKey(User32.VK_CONTROL, User32.KEYEVENTF_KEYUP);
                        }

                        if ((vk & 0x0400) != 0)  // ALT
                        {
                            SendKey(User32.VK_MENU, User32.KEYEVENTF_KEYUP);
                        }
                    }
                }
                finally
                {
                    if (foregroundWindowHandle != IntPtr.Zero)
                    {
                        IntegrationHelper.SetFocus(foregroundWindowHandle);
                    }

                    if (inputBlocked)
                    {
                        User32.BlockInput(false);
                    }
                }

                await Task.Delay(delayBetweenCharacters);
            }
        }

        private bool IsExtendedKey(ushort vk) => ((vk >= User32.VK_PRIOR) && (vk <= User32.VK_DOWN)) || (vk == User32.VK_INSERT) || (vk == User32.VK_DELETE);

        private void SendKey(ushort vk)
        {
            SendKey(vk);
            SendKey(vk, User32.KEYEVENTF_KEYUP);
        }

        private void SendKey(ushort vk, uint dwFlags = 0)
        {
            var inputs = new User32.INPUT[] {
                new User32.INPUT() {
                    Type = User32.INPUT_KEYBOARD,
                    ki = new User32.KEYBDINPUT() {
                        wVk = vk,
                        wScan = 0,
                        dwFlags = dwFlags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            if (IsExtendedKey(vk))
            {
                inputs[0].ki.dwFlags |= User32.KEYEVENTF_EXTENDEDKEY;
            }

            User32.SendInput(1, inputs, User32.SizeOf_INPUT);
        }

        private void SendCharacter(char character)
        {
            var inputs = new User32.INPUT[] {
                new User32.INPUT() {
                    Type = User32.INPUT_KEYBOARD,
                    ki = new User32.KEYBDINPUT() {
                        wVk = 0,
                        wScan = character,
                        dwFlags = User32.KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                },
                new User32.INPUT() {
                    Type = User32.INPUT_KEYBOARD,
                    ki = new User32.KEYBDINPUT() {
                        wVk = 0,
                        wScan = character,
                        dwFlags = User32.KEYEVENTF_UNICODE | User32.KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            User32.SendInput(2, inputs, User32.SizeOf_INPUT);
        }
    }
}
