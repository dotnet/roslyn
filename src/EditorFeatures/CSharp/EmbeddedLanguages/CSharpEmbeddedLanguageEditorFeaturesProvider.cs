// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Editor.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Editor.EmbeddedLanguages
{
    [ExportLanguageService(typeof(IEmbeddedLanguageEditorFeaturesProvider), LanguageNames.CSharp), Shared]
    internal class CSharpEmbeddedLanguageEditorFeaturesProvider : AbstractEmbeddedLanguageEditorFeaturesProvider
    {
        public static IEmbeddedLanguageFeaturesProvider Instance = new CSharpEmbeddedLanguageEditorFeaturesProvider();

#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public CSharpEmbeddedLanguageEditorFeaturesProvider() : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }

        internal override string EscapeText(string text, SyntaxToken token)
            => EmbeddedLanguageUtilities.EscapeText(text, token);
    }
}
