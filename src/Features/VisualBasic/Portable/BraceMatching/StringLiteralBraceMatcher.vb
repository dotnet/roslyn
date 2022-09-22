' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.BraceMatching
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic), [Shared]>
    Friend Class StringLiteralBraceMatcher
        Implements IBraceMatcher

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Async Function FindBraces(document As Document,
                                         position As Integer,
                                         options As BraceMatchingOptions,
                                         cancellationToken As CancellationToken) As Task(Of BraceMatchingResult?) Implements IBraceMatcher.FindBracesAsync
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
