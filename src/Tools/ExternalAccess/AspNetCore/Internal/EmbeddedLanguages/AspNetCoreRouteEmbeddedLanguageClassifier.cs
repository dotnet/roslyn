// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.Internal.EmbeddedLanguages
{
    [ExportEmbeddedLanguageClassifierInternal(
        nameof(AspNetCoreRouteEmbeddedLanguageClassifier), LanguageNames.CSharp, supportsUnannotatedAPIs: false, "Route"), Shared]
    internal class AspNetCoreRouteEmbeddedLanguageClassifier : IEmbeddedLanguageClassifier
    {
        // Following CWTs are used to cache the  providers from projects' references,
        // so we can avoid the slow path unless there's any change to the references.
        private readonly ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, IAspNetCoreRouteEmbeddedLanguageClassifier> _analyzerReferencesToClassifierMap = new();
        private readonly ConditionalWeakTable<AnalyzerReference, IAspNetCoreRouteEmbeddedLanguageClassifier> _analyzerReferenceToClassifierMap = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AspNetCoreRouteEmbeddedLanguageClassifier()
        {
        }

        public void RegisterClassifications(EmbeddedLanguageClassificationContext context)
        {
        }

        private IAspNetCoreRouteEmbeddedLanguageClassifier? GetClassifier(Project? project)
        {
            if (project is null)
                return null;

            if (_analyzerReferencesToClassifierMap.TryGetValue(project.AnalyzerReferences, out var classifier))
                return classifier;

            return GetClassifierSlow(project);

            IAspNetCoreRouteEmbeddedLanguageClassifier? GetClassifierSlow(Project project)
                => _analyzerReferencesToClassifierMap.GetValue(project.AnalyzerReferences, _ => ComputeClassifier(project));

            IAspNetCoreRouteEmbeddedLanguageClassifier? ComputeClassifier(Project project)
            {
                foreach (var reference in project.AnalyzerReferences)
                {
                    var classifier = _analyzerReferenceToClassifierMap.GetValue(reference, r => GetClassifier(r));
                    if (classifier != null)
                        return classifier;
                }

                return null;
            }
        }

        private sealed class ClassifierExtensionProvider
            : AbstractProjectExtensionProvider<IAspNetCoreRouteEmbeddedLanguageClassifier, ExportCompletionProviderAttribute>
        {
            public ClassifierExtensionProvider(AnalyzerReference reference)
                : base(reference)
            {
            }

            protected override bool SupportsLanguage(ExportCompletionProviderAttribute exportAttribute, string language)
            {
                return exportAttribute.Language == null
                    || exportAttribute.Language.Length == 0
                    || exportAttribute.Language.Contains(language);
            }

            protected override bool TryGetExtensionsFromReference(AnalyzerReference reference, out ImmutableArray<CompletionProvider> extensions)
            {
                extensions = default;
                return false;
            }
        }
    }
}
