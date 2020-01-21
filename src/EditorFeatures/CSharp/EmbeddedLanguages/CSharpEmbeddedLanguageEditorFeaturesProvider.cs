// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Editor.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Editor.EmbeddedLanguages
{
    [ExportLanguageService(typeof(IEmbeddedLanguagesProvider), LanguageNames.CSharp, ServiceLayer.Editor), Shared]
    internal class CSharpEmbeddedLanguageEditorFeaturesProvider : AbstractEmbeddedLanguageEditorFeaturesProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEmbeddedLanguageEditorFeaturesProvider()
            : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }

        internal override string EscapeText(string text, SyntaxToken token)
            => EmbeddedLanguageUtilities.EscapeText(text, token);
    }
}
