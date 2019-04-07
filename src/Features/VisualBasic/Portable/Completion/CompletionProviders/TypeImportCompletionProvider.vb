' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class TypeImportCompletionProvider
        Inherits AbstractTypeImportCompletionProvider

        Public Sub New(workspace As Workspace)
            MyBase.New(workspace)
        End Sub

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Protected Overrides Async Function CreateContextAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function GetNamespacesInScope(location As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As HashSet(Of INamespaceSymbol)

            ' TODO: handle command line option -imports
            Dim result = New HashSet(Of INamespaceSymbol)(semanticModel.GetImportNamespacesInScope(location))
            Dim containingNamespaceDeclaration = location.GetAncestorOrThis(Of NamespaceStatementSyntax)()

            Dim containingNamespaceSymbol As INamespaceSymbol
            If containingNamespaceDeclaration Is Nothing Then
                containingNamespaceSymbol = semanticModel.Compilation.RootNamespace()
            Else
                containingNamespaceSymbol = semanticModel.GetDeclaredSymbol(containingNamespaceDeclaration, cancellationToken)
            End If

            If containingNamespaceSymbol IsNot Nothing Then
                While Not containingNamespaceSymbol.IsGlobalNamespace
                    result.Add(containingNamespaceSymbol)
                    containingNamespaceSymbol = containingNamespaceSymbol.ContainingNamespace
                End While
            End If

            Return result
        End Function
    End Class
End Namespace
