// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LanguageInfoProvider : ILanguageInfoProvider
{
    // Constant so that Razor can use it (exposed via EA) otherwise their endpoints won't get hit
    public const string RazorLanguageName = "Razor";

    private static readonly LanguageInformation s_csharpLanguageInformation = new(LanguageNames.CSharp, ".csx");
    private static readonly LanguageInformation s_fsharpLanguageInformation = new(LanguageNames.FSharp, ".fsx");
    private static readonly LanguageInformation s_vbLanguageInformation = new(LanguageNames.VisualBasic, ".vbx");
    private static readonly LanguageInformation s_typeScriptLanguageInformation = new(InternalLanguageNames.TypeScript, scriptExtension: null);
    private static readonly LanguageInformation s_razorLanguageInformation = new(RazorLanguageName, scriptExtension: null);
    private static readonly LanguageInformation s_xamlLanguageInformation = new("XAML", scriptExtension: null);

    private static readonly Dictionary<string, LanguageInformation> s_extensionToLanguageInformation = new()
    {
        { ".cs", s_csharpLanguageInformation },
        { ".csx", s_csharpLanguageInformation },
        { ".fs", s_fsharpLanguageInformation },
        { ".fsx", s_fsharpLanguageInformation },
        { ".vb", s_vbLanguageInformation },
        { ".vbx", s_vbLanguageInformation },
        { ".cshtml", s_razorLanguageInformation },
        { ".razor", s_razorLanguageInformation },
        { ".xaml", s_xamlLanguageInformation },
        { ".ts", s_typeScriptLanguageInformation },
        { ".d.ts", s_typeScriptLanguageInformation },
        { ".tsx", s_typeScriptLanguageInformation },
        { ".js", s_typeScriptLanguageInformation },
        { ".jsx", s_typeScriptLanguageInformation },
        { ".cjs", s_typeScriptLanguageInformation },
        { ".mjs", s_typeScriptLanguageInformation },
        { ".cts", s_typeScriptLanguageInformation },
        { ".mts", s_typeScriptLanguageInformation },
    };

    public bool TryGetLanguageInformation(DocumentUri requestUri, string? lspLanguageId, [NotNullWhen(true)] out LanguageInformation? languageInformation)
    {
        // First try to get language information from the URI path.
        // We can do this for File uris and absolute uris.  We use local path to get the value without any query parameters.
        if (requestUri.ParsedUri is not null && (requestUri.ParsedUri.IsFile || requestUri.ParsedUri.IsAbsoluteUri))
        {
            var localPath = requestUri.ParsedUri.LocalPath;
            var extension = Path.GetExtension(localPath);
            if (s_extensionToLanguageInformation.TryGetValue(extension, out languageInformation))
            {
                return true;
            }
        }

        // If the URI file path mapping failed, use the languageId from the LSP client (if any).
        languageInformation = lspLanguageId switch
        {
            "csharp" => s_csharpLanguageInformation,
            "fsharp" => s_fsharpLanguageInformation,
            "vb" => s_vbLanguageInformation,
            "razor" => s_razorLanguageInformation,
            "xaml" => s_xamlLanguageInformation,
            "typescript" => s_typeScriptLanguageInformation,
            "javascript" => s_typeScriptLanguageInformation,
            _ => null,
        };

        return languageInformation != null;
    }
}
