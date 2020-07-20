// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CodeQuality
{
    internal abstract class AbstractCodeQualityDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private readonly GeneratedCodeAnalysisFlags _generatedCodeAnalysisFlags;

        protected AbstractCodeQualityDiagnosticAnalyzer(
            ImmutableArray<DiagnosticDescriptor> descriptors,
            GeneratedCodeAnalysisFlags generatedCodeAnalysisFlags)
        {
            SupportedDiagnostics = descriptors;
            _generatedCodeAnalysisFlags = generatedCodeAnalysisFlags;
        }

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(_generatedCodeAnalysisFlags);
            context.EnableConcurrentExecution();

            InitializeWorker(context);
        }

        protected abstract void InitializeWorker(AnalysisContext context);

        public abstract DiagnosticAnalyzerCategory GetAnalyzerCategory();

        public bool OpenFileOnly(OptionSet options)
            => false;

        protected static DiagnosticDescriptor CreateDescriptor(
            string id,
            LocalizableString title,
            LocalizableString messageFormat,
            bool isUnnecessary,
            bool isEnabledByDefault = true,
            bool isConfigurable = true,
            LocalizableString description = null,
            params string[] customTags)
            => new DiagnosticDescriptor(
                    id, title, messageFormat,
                    DiagnosticCategory.CodeQuality,
                    DiagnosticSeverity.Info,
                    isEnabledByDefault,
                    description,
                    customTags: DiagnosticCustomTags.Create(isUnnecessary, isConfigurable, customTags));
    }
}
