// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static readonly LanguageInformation s_xamlLanguageInformation = new("XAML", string.Empty);

        private static readonly Dictionary<string, LanguageInformation> s_extensionToLanguageInformation = new()
        {
            { ".cs", s_csharpLanguageInformation },
            { ".csx", s_csharpLanguageInformation },
            { ".fs", s_fsharpLanguageInformation },
            { ".fsx", s_fsharpLanguageInformation },
            { ".vb", s_vbLanguageInformation },
            { ".vbx", s_vbLanguageInformation },
            { ".xaml", s_xamlLanguageInformation },
        };

        public LanguageInformation? GetLanguageInformation(string documentPath, string? lspLanguageId)
        {
            if (s_extensionToLanguageInformation.TryGetValue(Path.GetExtension(documentPath), out var languageInformation))
            {
                return languageInformation;
            }

            return lspLanguageId switch
            {
                "csharp" => s_csharpLanguageInformation,
                "fsharp" => s_csharpLanguageInformation,
                "vb" => s_vbLanguageInformation,
                "xaml" => s_xamlLanguageInformation,
                _ => null,
            };
        }
    }
}
