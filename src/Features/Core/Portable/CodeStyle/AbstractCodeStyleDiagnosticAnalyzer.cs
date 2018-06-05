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

        protected readonly DiagnosticDescriptor Descriptor;

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
        protected readonly LocalizableString _localizableMessageFormat;

        private readonly bool _configurable;

        protected AbstractCodeStyleDiagnosticAnalyzer(
            string descriptorId, LocalizableString title,
            LocalizableString messageFormat = null,
            bool configurable = true)
        {
            DescriptorId = descriptorId;
            _localizableTitle = title;
            _localizableMessageFormat = messageFormat ?? title;
            _configurable = configurable;

            Descriptor = CreateDescriptor();
            UnnecessaryWithSuggestionDescriptor = CreateUnnecessaryDescriptor();
            UnnecessaryWithoutSuggestionDescriptor = CreateUnnecessaryDescriptor(descriptorId + "WithoutSuggestion");

            SupportedDiagnostics = ImmutableArray.Create(
                Descriptor, UnnecessaryWithoutSuggestionDescriptor, UnnecessaryWithSuggestionDescriptor);
        }

        protected DiagnosticDescriptor CreateUnnecessaryDescriptor()
            => CreateUnnecessaryDescriptor(DescriptorId);

        protected DiagnosticDescriptor CreateUnnecessaryDescriptor(string descriptorId)
            => CreateDescriptorWithId(
                descriptorId, _localizableTitle, _localizableMessageFormat,
                DiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        protected DiagnosticDescriptor CreateDescriptor(params string[] customTags)
            => CreateDescriptorWithId(DescriptorId, _localizableTitle, _localizableMessageFormat, customTags);

        protected DiagnosticDescriptor CreateDescriptorWithTitle(LocalizableString title, params string[] customTags)
            => CreateDescriptorWithId(DescriptorId, title, title, customTags);

        protected DiagnosticDescriptor CreateDescriptorWithId(
            string id, LocalizableString title, LocalizableString messageFormat,
            params string[] customTags)
        {
            if (!_configurable)
            {
                customTags = customTags.Concat(WellKnownDiagnosticTags.NotConfigurable).ToArray();
            }

            return new DiagnosticDescriptor(
                id, title, messageFormat,
                DiagnosticCategory.Style,
                DiagnosticSeverity.Hidden,
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
