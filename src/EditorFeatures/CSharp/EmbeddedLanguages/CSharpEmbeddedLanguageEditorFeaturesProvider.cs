// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Editor.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Editor.EmbeddedLanguages
{
    [ExportLanguageService(typeof(IEmbeddedLanguageEditorFeaturesProvider), LanguageNames.CSharp), Shared]
    internal class CSharpEmbeddedLanguageEditorFeaturesProvider : AbstractEmbeddedLanguageEditorFeaturesProvider
    {
        public static IEmbeddedLanguageFeaturesProvider Instance = new CSharpEmbeddedLanguageEditorFeaturesProvider();

        public CSharpEmbeddedLanguageEditorFeaturesProvider() : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }
    }
}
