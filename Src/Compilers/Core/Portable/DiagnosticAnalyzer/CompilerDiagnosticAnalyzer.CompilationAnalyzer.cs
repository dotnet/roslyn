// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class CompilerDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Per-compilation DiagnosticAnalyzer for compiler's syntax/semantic/compilation diagnostics.
        /// </summary>
        private class CompilationAnalyzer
        {
            private readonly Compilation compilation;

            public CompilationAnalyzer(Compilation compilation)
            {
                this.compilation = compilation;
            }

            public void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
            {
                var semanticModel = compilation.GetSemanticModel(context.Tree);
                var diagnostics = semanticModel.GetSyntaxDiagnostics(cancellationToken: context.CancellationToken);
                ReportDiagnostics(diagnostics, context.ReportDiagnostic, IsSourceLocation);
            }

            public void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
            {
                var declDiagnostics = context.SemanticModel.GetDeclarationDiagnostics(cancellationToken: context.CancellationToken);
                ReportDiagnostics(declDiagnostics, context.ReportDiagnostic, IsSourceLocation);

                var bodyDiagnostics = context.SemanticModel.GetMethodBodyDiagnostics(cancellationToken: context.CancellationToken);
                ReportDiagnostics(bodyDiagnostics, context.ReportDiagnostic, IsSourceLocation);
            }

            public static void AnalyzeCompilation(CompilationEndAnalysisContext context)
            {
                var diagnostics = context.Compilation.GetDeclarationDiagnostics(cancellationToken: context.CancellationToken);
                ReportDiagnostics(diagnostics, context.ReportDiagnostic, location => !IsSourceLocation(location));
            }

            private static bool IsSourceLocation(Location location)
            {
                return location != null && location.Kind == LocationKind.SourceFile;
            }

            private static void ReportDiagnostics(ImmutableArray<Diagnostic> diagnostics, Action<Diagnostic> reportDiagnostic, Func<Location, bool> locationFilter)
            {
                foreach(var diagnostic in diagnostics)
                {
                    if (locationFilter(diagnostic.Location) &&
                        diagnostic.Severity != DiagnosticSeverity.Hidden)
                    {
                        reportDiagnostic(diagnostic);
                    }
                }
            }
        }    
    }
}
