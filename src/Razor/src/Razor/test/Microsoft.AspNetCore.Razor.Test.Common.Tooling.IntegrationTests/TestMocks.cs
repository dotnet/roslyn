// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Moq;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class TestMocks
{
    public static TextLoader CreateTextLoader(string text)
        => CreateTextLoader(text, VersionStamp.Create());

    public static TextLoader CreateTextLoader(string text, VersionStamp version)
        => CreateTextLoader(SourceText.From(text), version);

    public static TextLoader CreateTextLoader(SourceText text)
        => CreateTextLoader(text, VersionStamp.Create());

    public static TextLoader CreateTextLoader(SourceText text, VersionStamp version)
    {
        var mock = new StrictMock<TextLoader>();

        var textAndVersion = TextAndVersion.Create(text, version);

        mock.Setup(x => x.LoadTextAndVersionAsync(It.IsAny<LoadTextOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textAndVersion);

        return mock.Object;
    }
}
