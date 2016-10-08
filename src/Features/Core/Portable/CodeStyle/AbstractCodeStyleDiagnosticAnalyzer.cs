// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractCodeStyleDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private readonly string _id;
        private readonly string _title;
        private readonly DiagnosticDescriptor _descriptor;
        private readonly DiagnosticDescriptor _unnecessaryWithSuggestionDescriptor;
        private readonly DiagnosticDescriptor _unnecessaryWithoutSuggestionDescriptor;

        protected AbstractCodeStyleDiagnosticAnalyzer(string id, string title)
        {
            _id = id;
            _title = title;
            _descriptor = CreateDescriptor(id, DiagnosticSeverity.Hidden);
            _unnecessaryWithSuggestionDescriptor = CreateDescriptor(
                id, DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);
            _unnecessaryWithoutSuggestionDescriptor = CreateDescriptor(id + "WithoutSuggestion",
                DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);
            SupportedDiagnostics = ImmutableArray.Create(
                _descriptor, _unnecessaryWithoutSuggestionDescriptor, _unnecessaryWithSuggestionDescriptor);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        private DiagnosticDescriptor CreateDescriptor(string id, DiagnosticSeverity severity, params string[] customTags)
            => new DiagnosticDescriptor(
                id,
                _title,
                _title,
                DiagnosticCategory.Style,
                severity,
                isEnabledByDefault: true,
                customTags: customTags);
    }
}