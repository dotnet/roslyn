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
            // HACK: RegisterSymbolAction doesn't work with locals & local functions
            context.RegisterSyntaxNodeAction(syntaxContext => SyntaxNodeAction(syntaxContext, idToCachedResult),
                SyntaxKind.VariableDeclarator,
                SyntaxKind.ForEachStatement,
                SyntaxKind.SingleVariableDesignation,
                SyntaxKind.LocalFunctionStatement);
        }
    }
}
