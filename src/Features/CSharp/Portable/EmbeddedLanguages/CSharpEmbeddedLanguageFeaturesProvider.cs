// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages
{
    [ExportLanguageService(typeof(IEmbeddedLanguagesProvider), LanguageNames.CSharp, ServiceLayer.Desktop), Shared]
    internal class CSharpEmbeddedLanguageFeaturesProvider : AbstractEmbeddedLanguageFeaturesProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEmbeddedLanguageFeaturesProvider()
            : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }

        public override string EscapeText(string text, SyntaxToken token)
            => EmbeddedLanguageUtilities.EscapeText(text, token);
    }
}
