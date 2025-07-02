' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicCreateTestAccessor))>
    <[Shared]>
    Public NotInheritable Class VisualBasicExposeMemberForTesting
        Inherits AbstractExposeMemberForTesting(Of TypeStatementSyntax)

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub

        Private Protected Overrides ReadOnly Property RefactoringHelpers As IRefactoringHelpers
            Get
                Return VisualBasicRefactoringHelpers.Instance
            End Get
        End Property

        Protected Overrides ReadOnly Property HasRefReturns As Boolean
            Get
                Return False
            End Get
        End Property

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

