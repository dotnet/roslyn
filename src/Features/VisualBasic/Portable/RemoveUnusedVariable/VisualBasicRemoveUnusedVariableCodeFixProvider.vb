﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.RemoveUnusedVariable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedVariable
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnusedVariable), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddImport)>
    Friend Class VisualBasicRemoveUnusedVariableCodeFixProvider
        Inherits AbstractRemoveUnusedVariableCodeFixProvider(Of
            LocalDeclarationStatementSyntax, ModifiedIdentifierSyntax, VariableDeclaratorSyntax)

        Private Const BC42024 As String = NameOf(BC42024)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(BC42024)

        Protected Overrides Function IsCatchDeclarationIdentifier(token As SyntaxToken) As Boolean
            ' VB does not support catch declarations without an identifier in them
            Return False
        End Function

        Protected Overrides Function GetNodeToRemoveOrReplace(node As SyntaxNode) As SyntaxNode
            node = node.Parent
            Return If(node.Kind() = SyntaxKind.SimpleAssignmentStatement, node, Nothing)
        End Function

        Protected Overrides Sub RemoveOrReplaceNode(editor As SyntaxEditor, node As SyntaxNode, syntaxFacts As ISyntaxFactsService)
            RemoveNode(editor, node, syntaxFacts)
        End Sub

        Protected Overrides Function GetVariables(localDeclarationStatement As LocalDeclarationStatementSyntax) As SeparatedSyntaxList(Of SyntaxNode)
            Return localDeclarationStatement.Declarators
        End Function
    End Class
End Namespace
