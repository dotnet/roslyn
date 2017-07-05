// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Roslyn.Test.Utilities
{
    public class VisualStudioSendKeys
    {
        /// <summary>
        /// Send keys synchronously to visual studio.
        /// </summary>
        /// <param name="hwnd">The window handle of vs's main window</param>
        /// <param name="timeout">How long to wait for the keys to be processed by Visual Studio.</param>
        /// <param name="escapedKeys">Only works with Visual Studio. Expects correctly escaped sequences as taken by System.Windows.Forms.SendKeys.</param>
        public static void SendWait(IntPtr hwnd, int timeout, string escapedKeys)
        {
            System.Windows.Forms.SendKeys.SendWait(escapedKeys);
            WaitForSendkeys(timeout, hwnd);
        }

        /// <summary>
        /// Splits a piece of code into escaped blocks suitable for use with SendWait.
        /// </summary>
        /// <param name="code">A piece of code to be broken and escaped.</param>
        /// <returns></returns>
        public static IEnumerable<string> BreakAndEscape(string code)
        {
            const int MaxChars = 500;
            var returnList = new List<string>();
            var lines = code.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            // In the unlikely case where the parsing might reduce the size of the original string (not currently possible, but who knows what the future might bring)
            // Padding the count to ensure that we start by adding a new element to the list if any line exists.
            var count = 2 * MaxChars;

            foreach (var line in lines)
            {
                var newLine = new System.Text.StringBuilder();
                foreach (string charOrEscapeSequence in ParseLineIntoEscapedChars(line))
                {
                    newLine.Append(charOrEscapeSequence);
                }

                if (count + newLine.Length < MaxChars)
                {
                    // We're still part of the current block, so update the current element to include
                    // this line.
                    returnList[returnList.Count - 1] = returnList[returnList.Count - 1] + newLine;
                    count += newLine.Length;
                }
                else
                {
                    // This would put us over the block size, so make this line start a new block.
                    returnList.Add(newLine.ToString());
                    count = newLine.Length;
                }
            }

            // Signature help sometimes comes up at random times and eats the up/down keys, so add
            // an escape in first.
            return returnList.Select(s => s.Replace("{UP}", "{ESC}{UP}").Replace("{DOWN}", "{ESC}{DOWN}")).ToList();
        }

        private const int WM_USER = 0x400;

        // The following constants and functions wait for VS to signal that input processing is complete.
        // See src\env\msenv\core\propbar.cpp FnwpPropBar(): Case WM_TIMER on how this works.
        // These turn __SendKeys into a blocking call.
        private const string INPUT_PROCESSED_EVENT = "VSInputProcessed";
        private const int INPUT_PROCESSED_MSG = WM_USER + 0xC92;

        [DllImport("user32", CharSet = CharSet.Unicode)]
        private static extern int SendNotifyMessageW(IntPtr HWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32")]
        public static extern int SetForegroundWindow(IntPtr hWnd);

        private static WaitHandle s_inputProcessedEvent;

        private static WaitHandle InputProcessedEvent
        {
            get
            {
                return s_inputProcessedEvent ?? (s_inputProcessedEvent = new EventWaitHandle(initialState: false, mode: EventResetMode.AutoReset, name: INPUT_PROCESSED_EVENT));
            }
        }

        public static void WaitForSendkeys(int timeout, IntPtr hwnd)
        {
            // Note: Ensure the event is created before sending the INPUT_PROCESSED_MSG
            WaitHandle waitObject = InputProcessedEvent;

            IntPtr apphandle = hwnd;

            // The lParam indicates how long VS waits some before responding to the message.
            // If zero, the default delay is 500ms, which is quite long for typing scenarios.
            // Usually, we want the response to be as fast as possible so that the test case
            // isn't waiting unnecessarily if VS is ready. We can't use 0, so specify 1 instead.
            // Note that this will probably be rounded up to USER_TIMER_MINIMUM (10ms) by the
            // eventual call to SetTimer inside VS. If that happens to be longer than 'timeout'
            // then we're almost certainly going to fail.
            IntPtr pingDelay = new IntPtr(1);
            int notified = SendNotifyMessageW(apphandle, INPUT_PROCESSED_MSG, IntPtr.Zero, lParam: pingDelay);

            if (notified == 0)
            {
                throw new Exception("Couldn't notify the Editor that we are waiting for input processing message");
            }

            try
            {
                waitObject.WaitOne(timeout);
            }
            catch (TimeoutException)
            {
                throw new Exception("Timed out waiting for notification that SendKeys has been processed by client");
            }
        }

        private static IEnumerable<string> ParseLineIntoEscapedChars(string line)
        {
            line = line.TrimStart(new[] { ' ', '\t' });
            for (var i = 0; i < line.Length; i++)
            {
                // Skip SendKeys commands included in source.
                var count = SkipCommands(line, i);

                for (int j = 0; j < count; j++)
                {
                    yield return string.Empty + line[i++];
                }

                if (i < line.Length)
                {
                    var key = line[i];

                    yield return ParseKey(key);
                }
            }

            yield return "{ENTER}";
        }

        private static int SkipCommands(string line, int start)
        {
            var normalLine = line.ToLowerInvariant();
            var commands = new List<string>();
            commands.Add("^{end}");
            commands.Add("{end}");
            commands.Add("{enter}");
            commands.Add("^{home}");
            commands.Add("{home}");
            commands.Add("{up}");
            commands.Add("{down}");
            commands.Add("{left}");
            commands.Add("{right}");
            commands.Add("{esc}");

            int skipLength = 0;
            bool found = true;
            while (found)
            {
                found = false;

                foreach (var command in commands)
                {
                    if (start + skipLength < normalLine.Length &&
                        start + skipLength + command.Length <= normalLine.Length &&
                        normalLine.Substring(start + skipLength, command.Length).Equals(command))
                    {
                        skipLength += command.Length;
                        found = true;
                    }
                }
            }

            return skipLength;
        }

        private static string ParseKey(char key)
        {
            string outputString = key.ToString();

            // Replace Special Characters that need to be escaped
            outputString = Regex.Replace(outputString, "(?<specialChar>[" + Regex.Escape("+^%~(){}") + "])", "{${specialChar}}");

            // Replace \n with Enter
            outputString = Regex.Replace(outputString, "\n", "{ENTER}");

            // Replace \t with Tab
            outputString = Regex.Replace(outputString, "\t", "{TAB}");

            return outputString;
        }
    }
}
