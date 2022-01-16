// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame.StackFrameSyntaxFactory;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame
{
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;

    public partial class StackFrameParserTests
    {
        private static void Verify(
            string input,
            StackFrameMethodDeclarationNode? methodDeclaration = null,
            bool expectFailure = false,
            StackFrameFileInformationNode? fileInformation = null,
            StackFrameToken? eolTokenOpt = null)
        {
            FuzzyTest(input);

            var tree = StackFrameParser.TryParse(input);
            if (expectFailure)
            {
                Assert.Null(tree);
                return;
            }

            AssertEx.NotNull(tree);
            VerifyCharacterSpans(input, tree);

            if (methodDeclaration is null)
            {
                Assert.Null(tree.Root.MethodDeclaration);
            }
            else
            {
                StackFrameUtils.AssertEqual(methodDeclaration, tree.Root.MethodDeclaration);
            }

            if (fileInformation is null)
            {
                Assert.Null(tree.Root.FileInformationExpression);
            }
            else
            {
                StackFrameUtils.AssertEqual(fileInformation, tree.Root.FileInformationExpression);
            }

            var eolToken = eolTokenOpt.HasValue
                ? eolTokenOpt.Value
                : CreateToken(StackFrameKind.EndOfFrame, "");

            StackFrameUtils.AssertEqual(eolToken, tree.Root.EndOfLineToken);
        }

        /// <summary>
        /// Tests that with a given input, no crashes are found
        /// with multiple substrings of the input
        /// </summary>
        private static void FuzzyTest(string input)
        {
            for (var i = 0; i < input.Length - 1; i++)
            {
                StackFrameParser.TryParse(input[i..]);
                StackFrameParser.TryParse(input[..^i]);

                for (var j = 0; j + i < input.Length; j++)
                {
                    var start = input[..j];
                    var end = input[(j + i)..];
                    StackFrameParser.TryParse(start + end);
                }
            }
        }

        private static void VerifyCharacterSpans(string originalText, StackFrameTree tree)
        {
            AssertEx.EqualOrDiff(originalText, tree.Root.ToFullString());

            // Manually enumerate to verify that it works as expected and the spans align.
            // This should be the same as ToFullString, but this tests that enumeration of the 
            // tokens yields the correct order (which we can't guarantee with ToFullString depending
            // on implementation). 
            var textSeq = VirtualCharSequence.Create(0, originalText);
            var index = 0;
            List<VirtualChar> enumeratedParsedCharacters = new();

            foreach (var charSeq in StackFrameUtils.Enumerate(tree.Root))
            {
                foreach (var ch in charSeq)
                {
                    enumeratedParsedCharacters.Add(ch);

                    if (textSeq[index++] != ch)
                    {
                        Assert.True(false, PrintDifference());
                    }
                }
            }

            // Make sure we enumerated the total input
            Assert.Equal(textSeq.Length, index);

            string PrintDifference()
            {
                var sb = new StringBuilder();

                var start = Math.Max(0, index - 10);
                var end = Math.Min(index, originalText.Length - 1);

                sb.Append("Expected: \t");
                PrintString(originalText, start, end, sb);
                sb.AppendLine();

                sb.Append("Actual: \t");
                var enumeratedString = new string(enumeratedParsedCharacters.Select(ch => (char)ch.Value).ToArray());
                PrintString(enumeratedString, start, end, sb);
                sb.AppendLine();

                return sb.ToString();

                static void PrintString(string s, int start, int end, StringBuilder sb)
                {
                    if (start > 0)
                    {
                        sb.Append("...");
                    }

                    sb.Append(s[start..end]);

                    if (end < s.Length - 1)
                    {
                        sb.Append("...");
                    }
                }
            }
        }
    }
}
