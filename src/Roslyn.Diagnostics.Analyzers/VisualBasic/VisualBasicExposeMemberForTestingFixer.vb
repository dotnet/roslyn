' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public NotInheritable Class VisualBasicExposeMemberForTestingFixer
        Inherits ExposeMemberForTestingFixer

        Protected Overrides ReadOnly Property HasRefReturns As Boolean

        Protected Overrides Function GetTypeDeclarationForNode(reportedNode As SyntaxNode) As SyntaxNode
            Return reportedNode.FirstAncestorOrSelf(Of TypeStatementSyntax)()?.Parent
        End Function

        Protected Overrides Function GetByRefType(type As SyntaxNode, refKind As RefKind) As SyntaxNode
            Return type
        End Function

        Protected Overrides Function GetByRefExpression(expression As SyntaxNode) As SyntaxNode
            Return expression
        End Function
    End Class
End Namespace
