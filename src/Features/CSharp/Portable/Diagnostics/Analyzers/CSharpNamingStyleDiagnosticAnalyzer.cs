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
            context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.LocalFunctionStatement);

            void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
            {
                var diagnostic = TryGetDiagnostic(
                    syntaxContext.Compilation,
                    syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node, syntaxContext.CancellationToken),
                    syntaxContext.Options,
                    idToCachedResult,
                    syntaxContext.CancellationToken);

                if (diagnostic != null)
                    syntaxContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
