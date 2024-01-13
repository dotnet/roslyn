// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    public abstract class StringCopyPasteCommandHandlerKnownSourceTests
        : StringCopyPasteCommandHandlerTests
    {
        protected static void TestCopyPaste(
            string copyFileMarkup, string pasteFileMarkup, string expectedMarkup, string afterUndo, bool mockCopyPasteService = true)
        {
            using var state = StringCopyPasteTestState.CreateTestState(
                copyFileMarkup, pasteFileMarkup, mockCopyPasteService);

            state.TestCopyPaste(expectedMarkup, pasteText: null, pasteTextIsKnown: false, afterUndo);
        }

        protected static void TestPasteKnownSource(string pasteText, string markup, string expectedMarkup, string afterUndo)
        {
            using var state = StringCopyPasteTestState.CreateTestState(copyFileMarkup: null, pasteFileMarkup: markup, mockCopyPasteService: true);

            state.TestCopyPaste(expectedMarkup, pasteText, pasteTextIsKnown: true, afterUndo);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/61316")]
        public void TestLineCopyPaste()
        {
            TestCopyPaste(
                """
                Debug.Assert(adjustment != 0, $"Indentation with[||]{|Copy:|} no adjustment should be represented by {nameof(BaseIndentationData)} directly.");

                """,
                """
                Debug.Assert(adjustment != 0, $"Indentation with[||] no adjustment should be represented by {nameof(BaseIndentationData)} directly.");

                """,
                """
                Debug.Assert(adjustment != 0, $"Indentation with no adjustment should be represented by {nameof(BaseIndentationData)} directly.");
                Debug.Assert(adjustment != 0, $"Indentation with[||] no adjustment should be represented by {nameof(BaseIndentationData)} directly.");
                
                """,
                """
                Debug.Assert(adjustment != 0, $"Indentation with[||] no adjustment should be represented by {nameof(BaseIndentationData)} directly.");

                """,
                mockCopyPasteService: false);
        }
    }
}
