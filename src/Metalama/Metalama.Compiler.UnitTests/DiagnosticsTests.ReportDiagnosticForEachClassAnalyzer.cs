// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Compiler.UnitTests;

public partial class DiagnosticsTests
{
    private class ReportDiagnosticForEachClassAnalyzer : DiagnosticAnalyzer
    {
        private readonly StringWriter? _outWriter;

        private readonly DiagnosticDescriptor _diagnostic;


        public ReportDiagnosticForEachClassAnalyzer(string id, DiagnosticSeverity severity,
            StringWriter? outWriter = null)
        {
            _outWriter = outWriter;
            _diagnostic = new DiagnosticDescriptor(id, "", "Found a class '{0}'.", "test", severity, true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(_diagnostic);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
            _outWriter?.WriteLine("Analyzer initialized.");
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            _outWriter?.WriteLine("Analyzing syntax tree.");

            foreach (var c in context.Tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                _outWriter?.WriteLine($"Analyzing '{c.Identifier.Text}'.");
                context.ReportDiagnostic(
                    Microsoft.CodeAnalysis.Diagnostic.Create(_diagnostic, c.Location, c.Identifier.Text));
            }
        }
    }
}
