// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    internal class StackFrameParserHelpers
    {
        /// <summary>
        /// Makes sure that the string at least somewhat resembles the correct form.
        /// Does not check validity on class or method identifiers
        /// Example line:
        /// at ConsoleApp4.MyClass.ThrowAtOne(p1, p2) 
        ///   |-------------------||--------||-------| 
        ///           Class          Method    Args   
        /// </summary>
        /// <remarks>
        /// See https://docs.microsoft.com/en-us/dotnet/api/system.environment.stacktrace for more information
        /// on expected stacktrace form. At time of writing, this is based on the following "ToString" implementation in the runtime: 
        /// https://github.com/dotnet/runtime/blob/72d643d05ab23888f30a57d447154e36f979f3d1/src/libraries/System.Private.CoreLib/src/System/Diagnostics/StackTrace.cs#L206
        /// </remarks>

        public static bool TryParseMethodSignature(ReadOnlySpan<char> line, out TextSpan classSpan, out TextSpan methodSpan, out TextSpan argsSpan)
        {

            var state = new ParseStateMachine();
            Debug.Assert(state.CurrentParsingSpan == CurrentParsingSpan.Type);

            for (var i = 0; i < line.Length; i++)
            {
                if (state.CurrentParsingSpan == CurrentParsingSpan.Finished)
                {
                    break;
                }

                var c = line[i];
                state.CurrentSpanLength++;

                //
                // Every if statement should be a branch and always end with a "continue" statement.
                // It is cumbersome to read this as a switch statement, especially given that not all branches
                // are just switches on a character. If new arms are added, follow the general rule that
                // all top if statements should not continue execution after they exit.
                //

                //
                // When starting to parse an identifier we want the first character to be valid. Arguments will
                // be an exception here so don't check validity of those characters for the first item
                //
                if (state.CurrentSpanLength == 1 && state.CurrentParsingSpan != CurrentParsingSpan.Arguments)
                {
                    // When starting to parse an identifier we want the first character to be valid
                    if (!UnicodeCharacterUtilities.IsIdentifierStartCharacter(c))
                    {
                        state.Reset();
                        continue;
                    }

                    // If we're starting to parse the type then we want the previous character to either be a space
                    // or this to be the beginning of the string. We don't want to try to have valid identifier starts
                    // as a subword of some text
                    if (i > 0)
                    {
                        var previousChar = line[i - 1];
                        if (previousChar != ' ')
                        {
                            state.Reset();
                            continue;
                        }
                    }

                    continue;
                }

                if (c == ' ')
                {
                    if (!state.AllowSpace)
                    {
                        // We encountered a space in an area we don't expect. Reset the state and start trying to parse
                        // the next block as a method signature
                        state.Reset();
                    }

                    continue;
                }

                if (c == '.')
                {
                    // Dot separators are allowed in the following cases:
                    // 1. We are parsing the fully qualified type portion
                    // 2. We are parsing arguments which could use a dot to fully qualify a type
                    // 3. We are inside of a generic context, which could use dot to fully qualify a type
                    if (state.CurrentParsingSpan == CurrentParsingSpan.Type || state.CurrentParsingSpan == CurrentParsingSpan.Arguments)
                    {
                        // Check that the previous item was a valid identifier character or ] (generic closure)
                        if (i > 0)
                        {
                            var previousChar = line[i - 1];
                            if (UnicodeCharacterUtilities.IsIdentifierPartCharacter(previousChar) || previousChar == ']')
                            {
                                continue;
                            }
                        }

                        // Either there is no previous character, or the previous character does not allow for a '.' 
                        // following it. Reset and continue parsing
                        state.Reset();
                        continue;
                    }

                    continue;
                }

                if (c == '[' || c == '<')
                {
                    state.GenericDepth++;
                    continue;
                }

                if (c == ']' || c == '>')
                {
                    if (state.GenericDepth == 0)
                    {
                        state.Reset();
                    }
                    else
                    {
                        state.GenericDepth--;
                    }

                    continue;
                }

                if (c == '(')
                {
                    if (state.CurrentParsingSpan == CurrentParsingSpan.Type)
                    {
                        state.StartParsingArguments(line);
                    }
                    else
                    {
                        // In cases where we encounter a '(' and already are parsing arguments we want
                        // to stop parsing. This could be problematic in cases where the value of a variable
                        // is provided and is a string, but for now we will just fail parsing that.
                        state.Reset();
                    }

                    continue;
                }

                if (c == ')')
                {
                    // ')' is invalid except for closing the end of the arguments list
                    if (state.CurrentParsingSpan != CurrentParsingSpan.Arguments)
                    {
                        state.Reset();
                        continue;
                    }

                    // Similar to assuming that '(' will always be considered a start of the argument section, we assume
                    // that ')' will end it. There are cases where this is not true, but for now that's not supported.
                    state.StopParsingArguments();
                    continue;
                }

                if (c == ',')
                {
                    // Comma is allowed if we are parsing arguments or are currently going through a generic list. 
                    // As of now, no validation is done that the comma is valid in this location
                    if (state.CurrentParsingSpan != CurrentParsingSpan.Arguments && state.GenericDepth == 0)
                    {
                        state.Reset();
                    }

                    continue;
                }

                // In cases where we have no explicitly handled a character, our last effort is to make sure 
                // we are only accepting valid identifier characters. Every character that needs to be handled 
                // differently should be before this check
                if (!UnicodeCharacterUtilities.IsIdentifierPartCharacter(c))
                {
                    state.Reset();
                    continue;
                }
            }

            classSpan = state.TypeSpan;
            methodSpan = state.MethodSpan;
            argsSpan = state.ArgumentsSpan;

            return state.CurrentParsingSpan == CurrentParsingSpan.Finished &&
                classSpan != default &&
                methodSpan != default &&
                argsSpan != default;
        }

        private struct ParseStateMachine
        {
            public int GenericDepth;
            public bool InsideGeneric => GenericDepth > 0;
            public bool AllowSpace => InsideGeneric || CurrentParsingSpan == CurrentParsingSpan.Arguments;
            public int CurrentSpanStart { get; private set; }
            public int CurrentSpanLength;
            public CurrentParsingSpan CurrentParsingSpan { get; private set; }

            /// <summary>
            /// [|ConsoleApp4.MyClass|].M(string s, int i) 
            /// </summary>
            public TextSpan TypeSpan { get; private set; }

            /// <summary>
            /// ConsoleApp4.MyClass.[|M|](string s, int i) 
            /// </summary>
            public TextSpan MethodSpan { get; private set; }

            /// <summary>
            /// ConsoleApp4.MyClass.M([|string s, int i|]) 
            /// </summary>
            public TextSpan ArgumentsSpan { get; private set; }

            public void Reset()
            {
                SoftReset();
                CurrentParsingSpan = CurrentParsingSpan.Type;
                TypeSpan = default;
                MethodSpan = default;
                ArgumentsSpan = default;
            }

            /// <summary>
            /// Resets data that is common between a <see cref="Reset"/>, <see cref="StopParsingArguments"/>, and <see cref="StartParsingArguments"/>
            /// </summary>
            private void SoftReset()
            {
                CurrentSpanStart = CurrentSpanStart + CurrentSpanLength;
                CurrentSpanLength = 0;
                GenericDepth = 0;
            }

            internal void StopParsingArguments()
            {
                if (CurrentParsingSpan != CurrentParsingSpan.Arguments)
                {
                    throw new InvalidOperationException();
                }

                ArgumentsSpan = new TextSpan(CurrentSpanStart, CurrentSpanLength);
                SoftReset();
                CurrentParsingSpan = CurrentParsingSpan.Finished;
            }

            internal void StartParsingArguments(ReadOnlySpan<char> line)
            {
                if (CurrentParsingSpan != CurrentParsingSpan.Type)
                {
                    throw new InvalidOperationException();
                }

                var typeAndMethodSpan = new TextSpan(CurrentSpanStart, CurrentSpanLength);
                SoftReset();
                CurrentParsingSpan = CurrentParsingSpan.Arguments;

                var dotIndex = -1;

                for (var i = typeAndMethodSpan.End - 1; i >= typeAndMethodSpan.Start; i--)
                {
                    if (line[i] == '.')
                    {
                        dotIndex = i;
                        break;
                    }
                }

                if (dotIndex == -1)
                {
                    throw new InvalidOperationException();
                }

                TypeSpan = new TextSpan(typeAndMethodSpan.Start, (dotIndex - typeAndMethodSpan.Start) + 1);

                var methodStart = dotIndex + 1;
                var methodLength = (typeAndMethodSpan.End - 1) - methodStart;
                MethodSpan = new TextSpan(methodStart, methodLength);
            }
        }

        /// <summary>
        /// Order is important here. This is the order we expect
        /// parts of a method declaration to be parsed.
        /// at ConsoleApp4.MyClass.ThrowAtOne(p1, p2,) 
        ///   |-------------------||--------||-------| 
        ///           Class          Method    Args   
        /// </summary>
        private enum CurrentParsingSpan
        {
            Type = 0,
            Method,
            Arguments,
            Finished
        }
    }
}
