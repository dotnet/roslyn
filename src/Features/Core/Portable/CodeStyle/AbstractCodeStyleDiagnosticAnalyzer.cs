// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractCodeStyleDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        protected readonly string Id;
        protected readonly DiagnosticDescriptor UnnecessaryWithSuggestionDescriptor;
        protected readonly DiagnosticDescriptor UnnecessaryWithoutSuggestionDescriptor;

        private readonly LocalizableString _localizableTitle;
        private readonly LocalizableString _localizableMessage;
        private readonly DiagnosticDescriptor _descriptor;

        protected AbstractCodeStyleDiagnosticAnalyzer(string id, LocalizableString title, LocalizableString message)
        {
            Id = id;
            _localizableTitle = title;
            _localizableMessage = message;
            _descriptor = CreateDescriptor(id, DiagnosticSeverity.Hidden);
            UnnecessaryWithSuggestionDescriptor = CreateDescriptor(
                id, DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);
            UnnecessaryWithoutSuggestionDescriptor = CreateDescriptor(id + "WithoutSuggestion",
                DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);
            SupportedDiagnostics = ImmutableArray.Create(
                _descriptor, UnnecessaryWithoutSuggestionDescriptor, UnnecessaryWithSuggestionDescriptor);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        protected DiagnosticDescriptor CreateDescriptor(string id, DiagnosticSeverity severity, params string[] customTags)
            => new DiagnosticDescriptor(
                id,
                _localizableTitle,
                _localizableMessage,
                DiagnosticCategory.Style,
                severity,
                isEnabledByDefault: true,
                customTags: customTags);
    }
}