// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract partial class AbstractBuiltInCodeStyleDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        protected readonly DiagnosticDescriptor Descriptor;

        private AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            string descriptorId,
            EnforceOnBuild enforceOnBuild,
            LocalizableString title,
            LocalizableString? messageFormat,
            bool isUnnecessary,
            bool configurable)
        {
            // 'isUnnecessary' should be true only for sub-types of AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer.
            Debug.Assert(!isUnnecessary || this is AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer);

            Descriptor = CreateDescriptorWithId(descriptorId, enforceOnBuild, title, messageFormat ?? title, isUnnecessary: isUnnecessary, isConfigurable: configurable);
            SupportedDiagnostics = ImmutableArray.Create(Descriptor);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors such that all the descriptors have no unique code style option to configure the descriptors.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> supportedDiagnostics)
        {
            SupportedDiagnostics = supportedDiagnostics;

            Descriptor = SupportedDiagnostics[0];
            Debug.Assert(!supportedDiagnostics.Any(descriptor => descriptor.CustomTags.Any(t => t == WellKnownDiagnosticTags.Unnecessary)) || this is AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer);
        }

        public virtual bool IsHighPriority => false;
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        protected static DiagnosticDescriptor CreateDescriptorWithId(
            string id,
            EnforceOnBuild enforceOnBuild,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool isConfigurable = true,
            LocalizableString? description = null)
#pragma warning disable RS0030 // Do not used banned APIs
            => new(
                    id, title, messageFormat ?? title,
                    DiagnosticCategory.Style,
                    DiagnosticSeverity.Hidden,
                    isEnabledByDefault: true,
                    description: description,
                    helpLinkUri: DiagnosticHelper.GetHelpLinkForDiagnosticId(id),
                    customTags: DiagnosticCustomTags.Create(isUnnecessary, isConfigurable, enforceOnBuild));
#pragma warning restore RS0030 // Do not used banned APIs

        /// <summary>
        /// Flags to configure the analysis of generated code.
        /// By default, code style analyzers should not analyze or report diagnostics on generated code, so the value is false.
        /// </summary>
        protected virtual GeneratedCodeAnalysisFlags GeneratedCodeAnalysisFlags => GeneratedCodeAnalysisFlags.None;

        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags);
            context.EnableConcurrentExecution();

            InitializeWorker(context);
        }

        protected abstract void InitializeWorker(AnalysisContext context);
    }
}
