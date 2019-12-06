// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal static class HostLanguageServicesExtensions
    {
        public static TLanguageService GetOriginalLanguageService<TLanguageService>(this HostLanguageServices languageServices) where TLanguageService : class, ILanguageService
        {
            return languageServices.GetOriginalLanguageServices().GetService<TLanguageService>();
        }

        public static HostLanguageServices GetOriginalLanguageServices(this HostLanguageServices languageServices)
        {
            var language = languageServices.Language;
            var originalLanguage = language;

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
