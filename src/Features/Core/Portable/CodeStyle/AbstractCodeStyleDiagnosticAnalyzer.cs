// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractCodeStyleDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        protected readonly string _id;

        private readonly LocalizableString _localizableTitle;
        private readonly LocalizableString _localizableMessage;

        private readonly DiagnosticDescriptor _descriptor;
        protected readonly DiagnosticDescriptor _unnecessaryWithSuggestionDescriptor;
        protected readonly DiagnosticDescriptor _unnecessaryWithoutSuggestionDescriptor;

        protected AbstractCodeStyleDiagnosticAnalyzer(string id, LocalizableString title, LocalizableString message)
        {
            _id = id;
            _localizableTitle = title;
            _localizableMessage = message;
            _descriptor = CreateDescriptor(id, DiagnosticSeverity.Hidden);
            _unnecessaryWithSuggestionDescriptor = CreateDescriptor(
                id, DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);
            _unnecessaryWithoutSuggestionDescriptor = CreateDescriptor(id + "WithoutSuggestion",
                DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);
            SupportedDiagnostics = ImmutableArray.Create(
                _descriptor, _unnecessaryWithoutSuggestionDescriptor, _unnecessaryWithSuggestionDescriptor);
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