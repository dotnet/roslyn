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
            switch (syntax.Kind())
            {
                case SyntaxKind.ClassDeclaration:
#if CODEANALYSIS_V3_OR_BETTER
                case SyntaxKind.RecordDeclaration:
#endif
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                    return true;

                default:
                    return false;
            }
        }
    }
}
