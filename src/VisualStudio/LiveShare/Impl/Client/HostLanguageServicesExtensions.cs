// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal static class HostLanguageServicesExtensions
    {
        public static TLanguageService GetOriginalLanguageService<TLanguageService>(this HostLanguageServices languageServices) where TLanguageService : class, ILanguageService
            => languageServices.GetOriginalLanguageServices().GetService<TLanguageService>();

        public static HostLanguageServices GetOriginalLanguageServices(this HostLanguageServices languageServices)
        {
            var language = languageServices.Language;
            string originalLanguage;

            switch (language)
            {
                case StringConstants.CSharpLspLanguageName:
                    originalLanguage = LanguageNames.CSharp;
                    break;
                case StringConstants.VBLspLanguageName:
                    originalLanguage = LanguageNames.VisualBasic;
                    break;
                default:
                    // Unknown language.
                    return null;
            }

            return languageServices.WorkspaceServices.GetLanguageServices(originalLanguage);
        }
    }
}
