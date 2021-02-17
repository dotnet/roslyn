// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpDiagnosticAnalyzerApiUsageAnalyzer : DiagnosticAnalyzerApiUsageAnalyzer<TypeSyntax>
    {
        protected override bool IsNamedTypeDeclarationBlock(SyntaxNode syntax)
        {
            return syntax.Kind() switch
            {
                SyntaxKind.ClassDeclaration
                or SyntaxKind.StructDeclaration
                or SyntaxKind.EnumDeclaration
#if CODEANALYSIS_V3_OR_BETTER
                or SyntaxKind.RecordDeclaration:
#endif
                or SyntaxKind.InterfaceDeclaration => true,
                _ => false,
            };
        }
    }
}
