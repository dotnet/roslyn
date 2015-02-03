' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Roslyn.Diagnostics.Analyzers.VisualBasic
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUnusedDeclarationsAnalyzer
        Inherits UnusedDeclarationsAnalyzer(Of SyntaxKind)

        Protected Overrides ReadOnly Property IdentifierSyntaxKind As SyntaxKind
            Get
                Return SyntaxKind.IdentifierName
            End Get
        End Property

        Protected Overrides ReadOnly Property LocalDeclarationStatementSyntaxKind As SyntaxKind
            Get
                Return SyntaxKind.LocalDeclarationStatement
            End Get
        End Property

        Protected Overrides Iterator Function GetLocalDeclarationNodes(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of SyntaxNode)
            Dim locals = TryCast(node, LocalDeclarationStatementSyntax)
            If locals Is Nothing Then
                Return
            End If

            For Each variable In locals.Declarators
                Yield variable
            Next
        End Function
    End Class
End Namespace