// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Compiler.UnitTests;

partial class DiagnosticsTests
{

    class ConsumeEditorConfigAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "OPTION_NOT_DEFINED";
        private readonly string _optionName;
        static readonly DiagnosticDescriptor _diagnosticDescriptor = new DiagnosticDescriptor(DiagnosticId, "Title", "Format", "Category",  DiagnosticSeverity.Error, true);

        public ConsumeEditorConfigAnalyzer(string optionName )
        {
            _optionName = optionName;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_diagnosticDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction( this.AnalyzeSyntaxTree );
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            if (!context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree)
                    .TryGetValue(this._optionName, out _))
            {
                context.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(_diagnosticDescriptor, null, this._optionName));
            }
        }

    }
    
}
