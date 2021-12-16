' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets.SnippetFunctions
    Friend NotInheritable Class SnippetFunctionClassName
        Inherits AbstractSnippetFunctionClassName

        Public Sub New(snippetExpansionClient As SnippetExpansionClient, subjectBuffer As ITextBuffer, fieldName As String)
            MyBase.New(snippetExpansionClient, subjectBuffer, fieldName)
        End Sub

        Protected Overrides Function GetContainingClassName(document As Document, subjectBufferFieldSpan As SnapshotSpan, cancellationToken As CancellationToken, ByRef value As String, ByRef hasDefaultValue As Integer) As Integer
            Dim syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken)
            Dim typeBlock = syntaxTree.FindTokenOnLeftOfPosition(subjectBufferFieldSpan.Start.Position, cancellationToken).GetAncestor(Of TypeBlockSyntax)

            If typeBlock IsNot Nothing AndAlso
               Not String.IsNullOrWhiteSpace(typeBlock.GetNameToken().ValueText) Then

                value = typeBlock.GetNameToken().ValueText
                hasDefaultValue = 1
            End If

            Return VSConstants.S_OK
        End Function
    End Class
End Namespace
