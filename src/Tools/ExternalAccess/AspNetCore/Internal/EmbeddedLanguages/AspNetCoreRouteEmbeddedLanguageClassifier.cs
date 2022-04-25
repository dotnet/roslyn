// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.Internal.EmbeddedLanguages
{
    [ExportEmbeddedLanguageClassifierInternal(
        nameof(AspNetCoreRouteEmbeddedLanguageClassifier), LanguageNames.CSharp, supportsUnannotatedAPIs: false, "Route"), Shared]
    internal class AspNetCoreRouteEmbeddedLanguageClassifier : IEmbeddedLanguageClassifier
    {
        private readonly IAspNetCoreRouteEmbeddedLanguageClassifier? _classifier;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AspNetCoreRouteEmbeddedLanguageClassifier(
            [Import(AllowDefault = true)] IAspNetCoreRouteEmbeddedLanguageClassifier? classifier)
        {
            _classifier = classifier;
        }

        public void RegisterClassifications(EmbeddedLanguageClassificationContext context)
            => _classifier?.RegisterClassifications(new AspNetCoreEmbeddedLanguageClassificationContext(context));
    }
}
