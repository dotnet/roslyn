' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class StringLiteralBraceMatcher
        Implements IBraceMatcher

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Async Function FindBraces(document As Document,
                                   position As Integer,
                                   Optional cancellationToken As CancellationToken = Nothing) As Task(Of BraceMatchingResult?) Implements IBraceMatcher.FindBracesAsync
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim token = root.FindToken(position)

            If position = token.SpanStart OrElse position = token.Span.End - 1 Then
                If token.Kind = SyntaxKind.StringLiteralToken AndAlso Not token.ContainsDiagnostics Then
                    Return New BraceMatchingResult(
                        New TextSpan(token.SpanStart, 1),
                        New TextSpan(token.Span.End - 1, 1))
                End If
            End If

            Return Nothing
        End Function
    End Class
End Namespace
