// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

public class HtmlFormattingFixture : IDisposable
{
    private readonly HtmlFormattingService _htmlFormattingService = new();

    internal HtmlFormattingService Service => _htmlFormattingService;

    public void Dispose()
    {
        _htmlFormattingService.Dispose();
    }
}
