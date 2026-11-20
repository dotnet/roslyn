// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class FileKindsTest
{
    public static TheoryData<string, RazorFileKind> KnownFileKinds => new()
    {
        { "C:/repo/MainLayout.razor", RazorFileKind.Component },
        { "C:/repo/Index.cshtml", RazorFileKind.Legacy },
        { "C:/repo/_Imports.razor", RazorFileKind.ComponentImport },
        { "C:/repo/_imports.razor", RazorFileKind.Component },
        { """git:/c:/repo/MainLayout.razor?{"path":"c:\\repo\\MainLayout.razor","ref":"~"}""", RazorFileKind.Component },
        { "git:/c:/repo/MainLayout.razor?%7B%22ref%22%3A%22~%22%7D", RazorFileKind.Component },
        { """git:/c:/repo/Index.cshtml?{"ref":"~"}""", RazorFileKind.Legacy },
        { "git:/c:/repo/_Imports.razor?%7B%22ref%22%3A%22~%22%7D", RazorFileKind.ComponentImport },
        { "git:/c:/repo/MainLayout.razor#revision", RazorFileKind.Component },
        { "git:/c:/repo/_Imports.razor#revision", RazorFileKind.ComponentImport },
        { """custom:/repo/MainLayout.razor?{"ref":"~"}""", RazorFileKind.Component },
        { """untitled:MainLayout.razor?{"ref":"~"}""", RazorFileKind.Component },
        { """untitled:_Imports.razor?{"ref":"~"}""", RazorFileKind.ComponentImport },
        { "git:/c:/repo/MainLayout%2Erazor?revision=HEAD", RazorFileKind.Component },
    };

    [Theory]
    [MemberData(nameof(KnownFileKinds))]
    public void GetFileKindFromPath_UsesDocumentPath(string filePath, RazorFileKind expected)
    {
        Assert.True(FileKinds.TryGetFileKindFromPath(filePath, out var actual));
        Assert.Equal(expected, actual);
        Assert.Equal(expected, FileKinds.GetFileKindFromPath(filePath));
    }

    [Theory]
    [InlineData("MainLayout.cs")]
    [InlineData("MainLayout.razor?query")]
    [InlineData("Index.cshtml#fragment")]
    [InlineData("C:/repo/MainLayout.razor?query")]
    [InlineData("C:/repo/Index.cshtml#fragment")]
    public void GetFileKindFromPath_UnknownPathUsesLegacy(string filePath)
    {
        Assert.False(FileKinds.TryGetFileKindFromPath(filePath, out _));
        Assert.Equal(RazorFileKind.Legacy, FileKinds.GetFileKindFromPath(filePath));
    }
}
