// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract partial class AbstractBuiltInCodeStyleDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        protected readonly string? DescriptorId;

        protected readonly DiagnosticDescriptor Descriptor;

        protected readonly LocalizableString _localizableTitle;
        protected readonly LocalizableString _localizableMessageFormat;

        private AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            string descriptorId,
            EnforceOnBuild enforceOnBuild,
            LocalizableString title,
            LocalizableString? messageFormat,
            bool isUnnecessary,
            bool configurable)
        {
            DescriptorId = descriptorId;
            _localizableTitle = title;
            _localizableMessageFormat = messageFormat ?? title;

            Descriptor = CreateDescriptorWithId(DescriptorId, enforceOnBuild, _localizableTitle, _localizableMessageFormat, isUnnecessary: isUnnecessary, isConfigurable: configurable);
            SupportedDiagnostics = ImmutableArray.Create(Descriptor);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors such that all the descriptors have no unique code style option to configure the descriptors.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> supportedDiagnostics)
        {
            SupportedDiagnostics = supportedDiagnostics;

            Descriptor = SupportedDiagnostics[0];
            _localizableTitle = Descriptor.Title;
            _localizableMessageFormat = Descriptor.MessageFormat;
        }

        public CodeActionRequestPriority RequestPriority => CodeActionRequestPriority.Normal;
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

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
        /// Flag indicating whether or not analyzer should receive analysis callbacks for generated code.
        /// By default, code style analyzers should not run on generated code, so the value is false.
        /// </summary>
        protected virtual bool ReceiveAnalysisCallbacksForGeneratedCode => false;

        public sealed override void Initialize(AnalysisContext context)
        {
            var flags = ReceiveAnalysisCallbacksForGeneratedCode ? GeneratedCodeAnalysisFlags.Analyze : GeneratedCodeAnalysisFlags.None;
            context.ConfigureGeneratedCodeAnalysis(flags);
            context.EnableConcurrentExecution();

            InitializeWorker(context);
        }

        protected abstract void InitializeWorker(AnalysisContext context);
    }
}
