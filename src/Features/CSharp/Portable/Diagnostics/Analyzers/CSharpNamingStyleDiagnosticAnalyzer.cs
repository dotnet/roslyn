// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.NamingStyles
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpNamingStyleDiagnosticAnalyzer : NamingStyleDiagnosticAnalyzerBase
    {
        protected sealed override void OnCompilationStartAction(
            CompilationStartAnalysisContext context,
            ConcurrentDictionary<Guid, ConcurrentDictionary<string, string>> idToCachedResult)
        {
            // HACK: RegisterSymbolAction doesn't work with local functions
            context.RegisterSyntaxNodeAction(c => SyntaxNodeAction(c, idToCachedResult), SyntaxKind.LocalFunctionStatement);
        }

        private void SyntaxNodeAction(
            SyntaxNodeAnalysisContext context,
            ConcurrentDictionary<Guid, ConcurrentDictionary<string, string>> idToCachedResult)
        {
            var symbolContext = new SymbolAnalysisContext(
                context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken),
                context.Compilation,
                context.Options,
                context.ReportDiagnostic,
                isSupportedDiagnostic: _ => true,
                context.CancellationToken);

            SymbolAction(symbolContext, idToCachedResult);
        }
    }
}
