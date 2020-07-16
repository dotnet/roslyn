// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractCodeStyleDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        protected readonly string? DescriptorId;

        protected readonly DiagnosticDescriptor Descriptor;

        /// <summary>
        /// Diagnostic descriptor for code you want to fade out *and* want to have a smart-tag
        /// appear for.  This is the common descriptor for code that is being faded out
        /// </summary>
        protected readonly DiagnosticDescriptor? UnnecessaryWithSuggestionDescriptor;

        /// <summary>
        /// Diagnostic descriptor for code you want to fade out and do *not* want to have a smart-tag
        /// appear for.  This is uncommon but useful in some cases.  For example, if you are fading
        /// out pieces of code before/after another piece of code *on the same line*, then you will
        /// only want one usage of <see cref="UnnecessaryWithSuggestionDescriptor"/> and multiple
        /// usages of <see cref="UnnecessaryWithoutSuggestionDescriptor"/>.
        /// 
        /// That's because if you use <see cref="UnnecessaryWithSuggestionDescriptor"/> for all the
        /// faded out code then that will mean the user will see multiple code actions to fix the
        /// same issue when they bring up the code action on that line.  Using these two descriptors
        /// helps ensure that there will not be useless code-action overload.
        /// </summary>
        protected readonly DiagnosticDescriptor? UnnecessaryWithoutSuggestionDescriptor;

        protected readonly LocalizableString _localizableTitle;
        protected readonly LocalizableString _localizableMessageFormat;

        protected AbstractCodeStyleDiagnosticAnalyzer(
            string descriptorId, LocalizableString title,
            LocalizableString? messageFormat = null,
            bool configurable = true)
        {
            DescriptorId = descriptorId;
            _localizableTitle = title;
            _localizableMessageFormat = messageFormat ?? title;

            Descriptor = CreateDescriptorWithId(DescriptorId, _localizableTitle, _localizableMessageFormat, isConfigurable: configurable);
            UnnecessaryWithSuggestionDescriptor = CreateUnnecessaryDescriptor(DescriptorId, configurable);
            UnnecessaryWithoutSuggestionDescriptor = CreateUnnecessaryDescriptor(descriptorId + "WithoutSuggestion", configurable);

            SupportedDiagnostics = ImmutableArray.Create(
                Descriptor, UnnecessaryWithoutSuggestionDescriptor, UnnecessaryWithSuggestionDescriptor);
        }

        protected AbstractCodeStyleDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> supportedDiagnostics)
        {
            SupportedDiagnostics = supportedDiagnostics;

            Descriptor = SupportedDiagnostics[0];
            _localizableTitle = Descriptor.Title;
            _localizableMessageFormat = Descriptor.MessageFormat;
        }

        protected DiagnosticDescriptor CreateUnnecessaryDescriptor(string descriptorId, bool isConfigurable = true)
            => CreateDescriptorWithId(
                descriptorId, _localizableTitle, _localizableMessageFormat,
                isUnnecessary: true,
                isConfigurable);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        protected static DiagnosticDescriptor CreateDescriptorWithId(
            string id,
            LocalizableString title,
            LocalizableString messageFormat,
            bool isUnnecessary = false,
            bool isConfigurable = true,
            LocalizableString? description = null,
            params string[] customTags)
            => new DiagnosticDescriptor(
                    id, title, messageFormat,
                    DiagnosticCategory.Style,
                    DiagnosticSeverity.Hidden,
                    isEnabledByDefault: true,
                    description: description,
                    customTags: DiagnosticCustomTags.Create(isUnnecessary, isConfigurable, customTags));

        public sealed override void Initialize(AnalysisContext context)
        {
            // Code style analyzers should not run on generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            InitializeWorker(context);
        }

        protected abstract void InitializeWorker(AnalysisContext context);
    }
}
