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
        private static readonly LanguageInformation s_vbLanguageInformation = new(LanguageNames.VisualBasic, ".vbx");
        private static readonly LanguageInformation s_xamlLanguageInformation = new("XAML", string.Empty);

        private static readonly Dictionary<string, LanguageInformation> s_extensionToLanguageInformation = new()
        {
            { ".cs", s_csharpLanguageInformation },
            { ".csx", s_csharpLanguageInformation },
            { ".vb", s_vbLanguageInformation },
            { ".vbx", s_vbLanguageInformation },
            { ".xaml", s_xamlLanguageInformation },
        };

        public LanguageInformation? GetLanguageInformation(string documentPath, string? languageId)
        {
            if (s_extensionToLanguageInformation.TryGetValue(Path.GetExtension(documentPath), out var languageInformation))
            {
                return languageInformation;
            }

            // It is totally possible to not find language based on the file path (e.g. a newly created file that hasn't been saved to disk).
            // In that case, we use the languageId that the client gave us.
            return languageId switch
            {
                "csharp" => s_csharpLanguageInformation,
                "vb" => s_vbLanguageInformation,
                "xaml" => s_xamlLanguageInformation,
                _ => null,
            };
        }
    }
}
