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

        protected readonly LocalizableString _localizableTitle;
        protected readonly LocalizableString _localizableMessageFormat;

        protected AbstractCodeStyleDiagnosticAnalyzer(
            string descriptorId, LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
        {
            DescriptorId = descriptorId;
            _localizableTitle = title;
            _localizableMessageFormat = messageFormat ?? title;

            Descriptor = CreateDescriptorWithId(DescriptorId, _localizableTitle, _localizableMessageFormat, isUnnecessary: isUnnecessary, isConfigurable: configurable);
            SupportedDiagnostics = ImmutableArray.Create(Descriptor);
        }

        protected AbstractCodeStyleDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> supportedDiagnostics)
        {
            SupportedDiagnostics = supportedDiagnostics;

            Descriptor = SupportedDiagnostics[0];
            _localizableTitle = Descriptor.Title;
            _localizableMessageFormat = Descriptor.MessageFormat;
        }

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
