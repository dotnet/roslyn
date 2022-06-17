// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Options;

internal static class EditorOptionProviders
{
    public static SyntaxFormattingOptions GetSyntaxFormattingOptions(this ITextBuffer textBuffer, IEditorOptionsFactoryService factory, IGlobalOptionService globalOptions, HostLanguageServices languageServices)
    {
        var configOptions = new EditorAnalyzerConfigOptions(factory.GetOptions(textBuffer));
        var fallbackOptions = globalOptions.GetSyntaxFormattingOptions(languageServices);
        return configOptions.GetSyntaxFormattingOptions(fallbackOptions, languageServices);
    }

    public static IndentationOptions GetIndentationOptions(this ITextBuffer textBuffer, IEditorOptionsFactoryService factory, IGlobalOptionService globalOptions, HostLanguageServices languageServices)
    {
        var formattingOptions = textBuffer.GetSyntaxFormattingOptions(factory, globalOptions, languageServices);

        return new IndentationOptions(formattingOptions)
        {
            AutoFormattingOptions = globalOptions.GetAutoFormattingOptions(languageServices.Language),
            IndentStyle = globalOptions.GetOption(IndentationOptionsStorage.SmartIndent, languageServices.Language)
        };
    }
}
