// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractCodeStyleDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        protected readonly string DescriptorId;

        /// <summary>
        /// Diagnostic descriptor that for analysis results that we don't want any treatment for.
        /// </summary>
        protected readonly DiagnosticDescriptor HiddenDescriptor;

        /// <summary>
        /// Diagnostic descriptor for code you want to fade out *and* want to have a smart-tag
        /// appear for.  This is the common descriptor for code that is being faded out
        /// </summary>
        protected readonly DiagnosticDescriptor UnnecessaryWithSuggestionDescriptor;

        /// <summary>
        /// Diagnostic descriptor for code you want to fade out and do *not* want to have a smart-tag
        /// appear for.  This is uncommon but useful in some cases.  For example, if you are fading
        /// out pieces of code before/after another piece of code *on the same line*, then you will
        /// only want one usafe of <see cref="UnnecessaryWithSuggestionDescriptor"/> and multiple
        /// usages of <see cref="UnnecessaryWithoutSuggestionDescriptor"/>.
        /// 
        /// That's because if you use <see cref="UnnecessaryWithSuggestionDescriptor"/> for all the
        /// faded out code then that will mean the user will see multiple code actions to fix the
        /// same issue when they bring up the code action on that line.  Using these two descriptors
        /// helps ensure that there will not be useless code-action overload.
        /// </summary>
        protected readonly DiagnosticDescriptor UnnecessaryWithoutSuggestionDescriptor;

        private readonly LocalizableString _localizableTitle;
        private readonly LocalizableString _localizableMessage;

        protected AbstractCodeStyleDiagnosticAnalyzer(
            string descriptorId, LocalizableString title, LocalizableString message = null)
        {
            DescriptorId = descriptorId;
            _localizableTitle = title;
            _localizableMessage = message ?? title;

            HiddenDescriptor = CreateDescriptorWithSeverity(DiagnosticSeverity.Hidden);

            UnnecessaryWithSuggestionDescriptor = CreateDescriptorWithId(
                descriptorId, _localizableTitle, _localizableMessage, 
                DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);

            UnnecessaryWithoutSuggestionDescriptor = CreateDescriptorWithId(
                descriptorId + "WithoutSuggestion",
                _localizableTitle, _localizableMessage, 
                DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);

            SupportedDiagnostics = ImmutableArray.Create(
                HiddenDescriptor, UnnecessaryWithoutSuggestionDescriptor, UnnecessaryWithSuggestionDescriptor);
        }

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        protected DiagnosticDescriptor CreateDescriptorWithSeverity(DiagnosticSeverity severity, params string[] customTags)
            => CreateDescriptorWithId(DescriptorId, _localizableTitle, _localizableMessage, severity, customTags);

        protected DiagnosticDescriptor CreateDescriptorWithTitle(LocalizableString title, DiagnosticSeverity severity, params string[] customTags)
            => CreateDescriptorWithId(DescriptorId, title, title, severity, customTags);

        private DiagnosticDescriptor CreateDescriptorWithId(string id, LocalizableString title, LocalizableString message, DiagnosticSeverity severity, params string[] customTags)
            => new DiagnosticDescriptor(
                id, title, message,
                DiagnosticCategory.Style,
                severity,
                isEnabledByDefault: true,
                customTags: customTags);

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