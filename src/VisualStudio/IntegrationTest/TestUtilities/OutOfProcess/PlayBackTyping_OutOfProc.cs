// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using System;
using System.Text.RegularExpressions;
using System.IO;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the Visual Studio editor by remoting calls into Visual Studio.
    /// </summary>
    public partial class Editor_OutOfProc
    {
        public void PlayBackTyping(string typingInputFilePath)
        {
            var typing = File.ReadAllLines(typingInputFilePath);
            foreach (var input in typing)
            {
                var parts = input.Split('\t');
                var character = parts[0].Length > 1 ? parts[0] : SimulateTyping.GetEscaped(parts[0]);
                var delay = int.Parse(parts[1]);
                Thread.Sleep(delay);
                System.Windows.Forms.SendKeys.SendWait(character);
                //VisualStudioInstance.SendKeys.Send(character);
            }
        }

        private static class SimulateTyping
        {
            // Use fixed seed to generate repeatable sequence
            private static Random rng = new Random(42);

            public enum TypingSpeed
            {
                SuperSlow = 10,
                Slow = 20,
                Half = 50,
                Normal = 100,
                Fast = 200,
                ReallyFast = 500,
                OMGFast = 2500,
            }

            public static void RandomDelay(TypingSpeed speedupFactor)
            {
                if ((int)speedupFactor == 0)
                {
                    speedupFactor = TypingSpeed.Normal;
                }

                // Wait between each character typed
                var delay = 500 + rng.Next(3100) + (200 << rng.Next(8));
                Thread.Sleep(delay / (int)speedupFactor);
            }

            private static bool IsCommand(string line)
            {
                var commands = new[]
                {
                "{ENTER}",
                "{END}", "^{END}", "+{END}", "^+{END}",
                "{HOME}", "^{HOME}", "+{HOME}", "^+{HOME}",
                "{UP}", "{DOWN}",
                "{LEFT}", "^{LEFT}", "+{LEFT}", "^+{LEFT}",
                "{RIGHT}", "^{RIGHT}", "+{RIGHT}", "^+{RIGHT}",
                "{ESC}", "{BS}"
            };

                if (line.StartsWith("{") || line.StartsWith("^{") || line.StartsWith("+{") || line.StartsWith("^+{"))
                {
                    foreach (var command in commands)
                    {
                        if (line.ToUpper().Contains(command))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public static IEnumerable<string> GetCommandsAndCharacters(string s, bool ignoreLeadingWhitespace)
            {
                s = s.Replace(@"\t", "{TAB}");

                foreach (var line in s.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    // Handle SendKeys commands (must appear on own line)
                    if (IsCommand(line))
                    {
                        yield return line;
                        continue;
                    }

                    string escapedLine = GetEscaped(line);
                    if (ignoreLeadingWhitespace)
                    {
                        escapedLine = Regex.Replace(escapedLine, @"^\s*", "");
                    }

                    var i = 0;
                    while (i < escapedLine.Length)
                    {
                        // Return escaped characters as a single "character"
                        var length = 0;
                        if (escapedLine[i] == '{')
                        {
                            length = escapedLine.IndexOf('}', i) - i + 1;

                            // Handle escaped } correctly
                            if (escapedLine[i + 1] == '}')
                            {
                                length++;
                            }

                            var substring = escapedLine.Substring(i, length);
                            yield return substring;
                        }
                        else
                        {
                            length = 1;
                            var character = escapedLine[i].ToString();
                            yield return character;
                        }

                        i += length;
                    }

                    var enter = "{ENTER}";
                    yield return enter;
                }
            }

            public static string GetEscaped(string line)
            {
                return Regex.Replace(line, "(?<specialChar>[" + Regex.Escape("+^%~(){}") + "])", "{${specialChar}}");
            }
        }
    }
}
