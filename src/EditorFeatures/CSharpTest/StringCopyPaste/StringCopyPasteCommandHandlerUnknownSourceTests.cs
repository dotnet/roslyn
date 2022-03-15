// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    public abstract class StringCopyPasteCommandHandlerUnknownSourceTests
        : StringCopyPasteCommandHandlerTests
    {
        protected static void TestPasteUnknownSource(string pasteText, string markup, string expectedMarkup, string afterUndo)
        {
            using var state = StringCopyPasteTestState.CreateTestState(markup);

            state.TestCopyPaste(expectedMarkup, pasteText, afterUndo);
        }
    }
}
