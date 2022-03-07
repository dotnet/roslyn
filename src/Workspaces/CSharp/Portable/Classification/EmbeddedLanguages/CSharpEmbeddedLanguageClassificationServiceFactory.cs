// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    [ExportLanguageService(typeof(IEmbeddedLanguageClassificationService), LanguageNames.CSharp), Shared]
    internal class CSharpEmbeddedLanguageClassificationService : AbstractEmbeddedLanguageClassificationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEmbeddedLanguageClassificationService(
            [ImportMany] IEnumerable<Lazy<IEmbeddedLanguageClassifier, EmbeddedLanguageMetadata>> classifiers)
            : base(LanguageNames.CSharp, CSharpEmbeddedLanguagesProvider.Info, CSharpSyntaxKinds.Instance, CSharpFallbackEmbeddedLanguageClassifier.Instance, classifiers)
        {
        }
    }
}
