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
        public const char ENTER = '\u000D';
        public const char TAB = '\u0009';
        public const char ESC = '\u001B';
        public const char ESCAPE = '\u001B';
        public const char HOME = '\u0024';
        public const char END = '\u0023';
        public const char LEFT = '\u0025';
        public const char RIGHT = '\u0027';
        public const char UP = '\u0026';
        public const char DOWN = '\u0028';
        public const char PGUP = '\u0021';
        public const char PGDN = '\u0022';
        public const char NUMLOCK = '\u0090';
        public const char SCROLLLOCK = '\u0091';
        public const char PRTSC = '\u002C';
        public const char BREAK = '\u0003';
        public const char BACKSPACE = '\u0008';
        public const char BKSP = '\u0008';
        public const char BS = '\u0008';
        public const char CLEAR = '\u000C';
        public const char CAPSLOCK = '\u0014';
        public const char INSERT = '\u002D';
        public const char DEL = '\u002E';
        public const char DELETE = '\u002E';
        public const char HELP = '\u002F';
        public const char F1 = '\u0070';
        public const char F2 = '\u0071';
        public const char F3 = '\u0072';
        public const char F4 = '\u0073';
        public const char F5 = '\u0074';
        public const char F6 = '\u0075';
        public const char F7 = '\u0076';
        public const char F8 = '\u0077';
        public const char F9 = '\u0078';
        public const char F10 = '\u0079';
        public const char F11 = '\u007A';
        public const char F12 = '\u007B';
        public const char F13 = '\u007C';
        public const char F14 = '\u007D';
        public const char F15 = '\u007E';
        public const char F16 = '\u007F';
        public const char MULTIPLY = '\u006A';
        public const char ADD = '\u006B';
        public const char SUBTRACT = '\u006D';
        public const char DIVIDE = '\u006F';

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

        private TextSelection TextSelection => (TextSelection)(IntegrationHelper.RetryRpcCall(() => _visualStudioInstance.Dte.ActiveDocument.Selection));

        public void Activate() => IntegrationHelper.RetryRpcCall(() => _visualStudioInstance.Dte.ActiveDocument.Activate());

        public void ClearTextSelection() => TextSelection?.Cancel();

        public void Find(string expectedText)
        {
            Activate();
            ClearTextSelection();

            var dteFind = IntegrationHelper.RetryRpcCall(() => _visualStudioInstance.Dte.Find);

            dteFind.Action = vsFindAction.vsFindActionFind;
            dteFind.FindWhat = expectedText;
            dteFind.MatchCase = true;
            dteFind.MatchInHiddenText = true;
            dteFind.MatchWholeWord = true;
            dteFind.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxLiteral;
            dteFind.Target = vsFindTarget.vsFindTargetCurrentDocument;

            var findResult = IntegrationHelper.RetryRpcCall(() => dteFind.Execute());

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
                var foregroundWindow = IntPtr.Zero;
                var inputBlocked = false;

                try
                {
                    inputBlocked = IntegrationHelper.BlockInput();
                    foregroundWindow = IntegrationHelper.GetForegroundWindow();

                    _visualStudioInstance.IntegrationService.Execute(typeof(RemotingHelper), nameof(RemotingHelper.ActivateMainWindow));

                    var vk = User32.VkKeyScan(character);

                    if (vk == -1)
                    {
                        SendCharacter(character);
                    }
                    else
                    {
                        if ((vk & 0x0100) != 0)  // SHIFT
                        {
                            SendKey(User32.VK_SHIFT, User32.KEYEVENTF_NONE);
                        }

                        if ((vk & 0x0200) != 0)  // CTRL
                        {
                            SendKey(User32.VK_CONTROL, User32.KEYEVENTF_NONE);
                        }

                        if ((vk & 0x0400) != 0)  // ALT
                        {
                            SendKey(User32.VK_MENU, User32.KEYEVENTF_NONE);
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
                    if (foregroundWindow != IntPtr.Zero)
                    {
                        IntegrationHelper.SetForegroundWindow(foregroundWindow);
                    }

                    if (inputBlocked)
                    {
                        IntegrationHelper.UnblockInput();
                    }
                }

                await Task.Delay(delayBetweenCharacters);
            }
        }

        private bool IsExtendedKey(ushort vk) => ((vk >= User32.VK_PRIOR) && (vk <= User32.VK_DOWN)) || (vk == User32.VK_INSERT) || (vk == User32.VK_DELETE);

        private void SendKey(ushort vk)
        {
            SendKey(vk, User32.KEYEVENTF_NONE);
            SendKey(vk, User32.KEYEVENTF_KEYUP);
        }

        private void SendKey(ushort vk, uint dwFlags)
        {
            var input = new User32.INPUT[] {
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
                input[0].ki.dwFlags |= User32.KEYEVENTF_EXTENDEDKEY;
            }

            IntegrationHelper.SendInput(input);
        }

        private void SendCharacter(char character)
        {
            var input = new User32.INPUT[] {
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

            IntegrationHelper.SendInput(input);
        }
    }
}
