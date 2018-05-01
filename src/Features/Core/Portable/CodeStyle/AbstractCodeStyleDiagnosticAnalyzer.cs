// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractCodeStyleDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        protected readonly string DescriptorId;

        /// <summary>
        /// Diagnostic descriptors corresponding to each of the DiagnosticSeverities.
        /// </summary>
        protected readonly DiagnosticDescriptor HiddenDescriptor;
        protected readonly DiagnosticDescriptor InfoDescriptor;
        protected readonly DiagnosticDescriptor WarningDescriptor;
        protected readonly DiagnosticDescriptor ErrorDescriptor;

        /// <summary>
        /// Diagnostic descriptor for code you want to fade out *and* want to have a smart-tag
        /// appear for.  This is the common descriptor for code that is being faded out
        /// </summary>
        protected readonly DiagnosticDescriptor UnnecessaryWithSuggestionDescriptor;

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
        protected readonly DiagnosticDescriptor UnnecessaryWithoutSuggestionDescriptor;

        protected readonly LocalizableString _localizableTitle;
        protected readonly LocalizableString _localizableMessage;

        private readonly bool _configurable;

        protected AbstractCodeStyleDiagnosticAnalyzer(
            string descriptorId, LocalizableString title,
            LocalizableString message = null,
            bool configurable = true)
        {
            DescriptorId = descriptorId;
            _localizableTitle = title;
            _localizableMessage = message ?? title;
            _configurable = configurable;

            HiddenDescriptor = CreateDescriptorWithSeverity(DiagnosticSeverity.Hidden);
            InfoDescriptor = CreateDescriptorWithSeverity(DiagnosticSeverity.Info);
            WarningDescriptor = CreateDescriptorWithSeverity(DiagnosticSeverity.Warning);
            ErrorDescriptor = CreateDescriptorWithSeverity(DiagnosticSeverity.Error);

            UnnecessaryWithSuggestionDescriptor = CreateUnnecessaryDescriptor(DiagnosticSeverity.Hidden);

            UnnecessaryWithoutSuggestionDescriptor = CreateDescriptorWithId(
                descriptorId + "WithoutSuggestion",
                _localizableTitle, _localizableMessage,
                DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);

            SupportedDiagnostics = ImmutableArray.Create(
                HiddenDescriptor, UnnecessaryWithoutSuggestionDescriptor, UnnecessaryWithSuggestionDescriptor);
        }

        protected DiagnosticDescriptor CreateUnnecessaryDescriptor(DiagnosticSeverity severity)
            => CreateUnnecessaryDescriptor(DescriptorId, severity);

        protected DiagnosticDescriptor CreateUnnecessaryDescriptor(string descriptorId, DiagnosticSeverity severity)
            => CreateDescriptorWithId(
                descriptorId, _localizableTitle, _localizableMessage,
                severity, DiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        protected DiagnosticDescriptor GetDescriptorWithSeverity(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Hidden: return HiddenDescriptor;
                case DiagnosticSeverity.Info: return InfoDescriptor;
                case DiagnosticSeverity.Warning: return WarningDescriptor;
                case DiagnosticSeverity.Error: return ErrorDescriptor;
                default: throw new InvalidOperationException();
            }
        }

        protected DiagnosticDescriptor CreateDescriptorWithSeverity(DiagnosticSeverity severity, params string[] customTags)
            => CreateDescriptorWithId(DescriptorId, _localizableTitle, _localizableMessage, severity, customTags);

        protected DiagnosticDescriptor CreateDescriptorWithTitle(LocalizableString title, DiagnosticSeverity severity, params string[] customTags)
            => CreateDescriptorWithId(DescriptorId, title, title, severity, customTags);

        protected DiagnosticDescriptor CreateDescriptorWithId(
            string id, LocalizableString title, LocalizableString message,
            DiagnosticSeverity severity, params string[] customTags)
        {
            if (!_configurable)
            {
                customTags = customTags.Concat(WellKnownDiagnosticTags.NotConfigurable).ToArray();
            }

            return new DiagnosticDescriptor(
                id, title, message,
                DiagnosticCategory.Style,
                severity,
                isEnabledByDefault: true,
                customTags: customTags);
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            // Code style analyzers should not run on generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            InitializeWorker(context);
        }

        protected abstract void InitializeWorker(AnalysisContext context);

        public abstract DiagnosticAnalyzerCategory GetAnalyzerCategory();
        public abstract bool OpenFileOnly(Workspace workspace);
    }
}
