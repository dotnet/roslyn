// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using FormattingOptions = Microsoft.VisualStudio.LanguageServer.Protocol.FormattingOptions;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal class TestFormattingOptionsProvider(FormattingOptions options) : FormattingOptionsProvider
{
    public static readonly TestFormattingOptionsProvider Default = new(
        new FormattingOptions()
        {
            InsertSpaces = true,
            TabSize = 4,
        });

    public override FormattingOptions? GetOptions(Uri uri) => options;
}
