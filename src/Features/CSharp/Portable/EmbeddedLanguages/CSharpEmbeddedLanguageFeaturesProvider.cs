// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages
{
    [ExportLanguageService(typeof(IEmbeddedLanguageFeaturesProvider), LanguageNames.CSharp), Shared]
    internal class CSharpEmbeddedLanguageFeaturesProvider : AbstractEmbeddedLanguageFeaturesProvider
    {
        public static IEmbeddedLanguageFeaturesProvider Instance = new CSharpEmbeddedLanguageFeaturesProvider();

#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public CSharpEmbeddedLanguageFeaturesProvider() : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }

        internal override string EscapeText(string text, SyntaxToken token)
            => EmbeddedLanguageUtilities.EscapeText(text, token);
    }
}
