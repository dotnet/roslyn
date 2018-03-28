' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets.SnippetFunctions
    Friend NotInheritable Class SnippetFunctionClassName
        Inherits AbstractSnippetFunctionClassName

        Public Sub New(snippetExpansionClient As SnippetExpansionClient, textView As ITextView, subjectBuffer As ITextBuffer, fieldName As String)
            MyBase.New(snippetExpansionClient, textView, subjectBuffer, fieldName)
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
