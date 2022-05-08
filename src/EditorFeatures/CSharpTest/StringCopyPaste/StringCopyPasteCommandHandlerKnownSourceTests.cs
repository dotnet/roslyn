// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    public abstract class StringCopyPasteCommandHandlerKnownSourceTests
        : StringCopyPasteCommandHandlerTests
    {
        protected static void TestCopyPaste(string copyFileMarkup, string pasteFileMarkup, string expectedMarkup, string afterUndo)
        {
            using var state = StringCopyPasteTestState.CreateTestState(
                copyFileMarkup, pasteFileMarkup);

            state.TestCopyPaste(expectedMarkup, pasteText: null, afterUndo);
        }
    }
}
