// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Compiler.UnitTests;

public partial class DiagnosticsTests
{
    private class ReportWarningIfTwoCompilationUnitMembersAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor _badWarning = new("MY001", "Test",
            "More than one member in compilation unit: {0}", "Test", DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor _goodWarning = new("MY002", "Test",
            "Processing {0}", "Test", DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(_badWarning, _goodWarning);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var compilationUnit = (CompilationUnitSyntax)context.Tree.GetRoot();

            // Write a message for each member to make sure that the analyzer runs.
            foreach (var member in compilationUnit.Members)
            {
                context.ReportDiagnostic(
                    Microsoft.CodeAnalysis.Diagnostic.Create(_goodWarning, member.Location, member.ToString()));
            }

            // Write a message if there are two members to make sure we see the source code.
            if (compilationUnit.Members.Count > 1)
            {
                foreach (var member in compilationUnit.Members)
                {
                    context.ReportDiagnostic(
                        Microsoft.CodeAnalysis.Diagnostic.Create(_badWarning, member.Location, member.ToString()));
                }
            }
        }
    }
}
