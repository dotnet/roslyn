' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets.SnippetFunctions

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets.SnippetFunctions
    Friend NotInheritable Class SnippetFunctionSimpleTypeName
        Inherits AbstractSnippetFunctionSimpleTypeName

        Public Sub New(snippetExpansionClient As SnippetExpansionClient, subjectBuffer As ITextBuffer, fieldName As String, fullyQualifiedName As String)
            MyBase.New(snippetExpansionClient, subjectBuffer, fieldName, fullyQualifiedName)
        End Sub

        Protected Overrides Function TryGetSimplifiedTypeName(documentWithFullyQualifiedTypeName As Document, updatedTextSpan As TextSpan, cancellationToken As CancellationToken, ByRef simplifiedTypeName As String) As Boolean
            simplifiedTypeName = String.Empty

            Dim typeAnnotation = New SyntaxAnnotation()
            Dim syntaxRoot = documentWithFullyQualifiedTypeName.GetSyntaxRootSynchronously(cancellationToken)
            Dim nodeToReplace = syntaxRoot.DescendantNodes().FirstOrDefault(Function(n) n.Span = updatedTextSpan)

            If nodeToReplace Is Nothing Then
                Return False
            End If

            Dim updatedRoot = syntaxRoot.ReplaceNode(nodeToReplace, nodeToReplace.WithAdditionalAnnotations(typeAnnotation, Simplifier.Annotation))
            Dim documentWithAnnotations = documentWithFullyQualifiedTypeName.WithSyntaxRoot(updatedRoot)

            Dim simplifiedDocument = Simplifier.ReduceAsync(documentWithAnnotations, cancellationToken:=cancellationToken).WaitAndGetResult(cancellationToken)
            simplifiedTypeName = simplifiedDocument.GetSyntaxRootSynchronously(cancellationToken).GetAnnotatedNodesAndTokens(typeAnnotation).Single().ToString()
            Return True
        End Function
    End Class
End Namespace
