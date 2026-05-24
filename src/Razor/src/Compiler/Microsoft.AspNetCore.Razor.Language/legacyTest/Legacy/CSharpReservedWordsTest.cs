// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpReservedWordsTest() : ParserTestBase(layer: TestProject.Layer.Compiler, validateSpanEditHandlers: true, useLegacyTokenizer: true)
{
    [Fact]
    public void ReservedWord()
    {
        ParseDocumentTest("@namespace");
    }

    [Fact]
    public void ReservedWordIsCaseSensitive()
    {
        ParseDocumentTest("@NameSpace");
    }
}
