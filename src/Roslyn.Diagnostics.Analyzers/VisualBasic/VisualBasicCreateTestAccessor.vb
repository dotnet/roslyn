' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Analyzer.Utilities
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicCreateTestAccessor))>
    <[Shared]>
    Public NotInheritable Class VisualBasicCreateTestAccessor
        Inherits AbstractCreateTestAccessor(Of TypeStatementSyntax)

        Public Sub New()
        End Sub

        Private Protected Overrides ReadOnly Property RefactoringHelpers As IRefactoringHelpers
            Get
                Return VisualBasicRefactoringHelpers.Instance
            End Get
        End Property

        Protected Overrides Function GetTypeDeclarationForNode(reportedNode As SyntaxNode) As SyntaxNode
            Return reportedNode.FirstAncestorOrSelf(Of TypeStatementSyntax)()?.Parent
        End Function
    End Class
End Namespace
