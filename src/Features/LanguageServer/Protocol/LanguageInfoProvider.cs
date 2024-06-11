// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Features.Workspaces;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class LanguageInfoProvider : ILanguageInfoProvider
    {
        private static readonly LanguageInformation s_csharpLanguageInformation = new(LanguageNames.CSharp, ".csx");
        private static readonly LanguageInformation s_fsharpLanguageInformation = new(LanguageNames.FSharp, ".fsx");
        private static readonly LanguageInformation s_vbLanguageInformation = new(LanguageNames.VisualBasic, ".vbx");
        private static readonly LanguageInformation s_typeScriptLanguageInformation = new LanguageInformation(InternalLanguageNames.TypeScript, string.Empty);
        private static readonly LanguageInformation s_razorLanguageInformation = new("Razor", string.Empty);
        private static readonly LanguageInformation s_xamlLanguageInformation = new("XAML", string.Empty);

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
        };

        public LanguageInformation GetLanguageInformation(string documentPath, string? lspLanguageId)
        {
            var extension = Path.GetExtension(documentPath);
            if (s_extensionToLanguageInformation.TryGetValue(extension, out var languageInformation))
            {
                return languageInformation;
            }

            return lspLanguageId switch
            {
                "csharp" => s_csharpLanguageInformation,
                "fsharp" => s_csharpLanguageInformation,
                "vb" => s_vbLanguageInformation,
                "razor" => s_razorLanguageInformation,
                "xaml" => s_xamlLanguageInformation,
                "typescript" => s_typeScriptLanguageInformation,
                "javascript" => s_typeScriptLanguageInformation,
                _ => throw new InvalidOperationException($"Unsupported extension '{extension}' and LSP language id '{lspLanguageId}'")
            };
        }
    }
}
