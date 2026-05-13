// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Razor.IntegrationTests.InProcess;

internal static class MarkupTestFile
{
    internal static void GetPosition(string markupCode, out string code, out int caretPosition)
    {
        caretPosition = markupCode.IndexOf("$$");
        code = markupCode.Replace("$$", "");
    }
}
