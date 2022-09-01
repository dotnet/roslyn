// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    [ExportLanguageServiceFactory(typeof(IEmbeddedLanguageClassificationService), LanguageNames.CSharp), Shared]
    internal class CSharpEmbeddedLanguageClassificationServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEmbeddedLanguageClassificationServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new EmbeddedLanguageClassificationService(languageServices.GetRequiredService<IEmbeddedLanguagesProvider>());
    }
}
