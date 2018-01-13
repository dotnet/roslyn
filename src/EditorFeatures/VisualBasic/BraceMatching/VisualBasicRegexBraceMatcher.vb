' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class VisualBasicRegexBraceMatcher
        Implements IBraceMatcher

        Public Async Function FindBracesAsync(document As Document, position As Integer, Optional cancellationToken As CancellationToken = Nothing) As Task(Of BraceMatchingResult?) Implements IBraceMatcher.FindBracesAsync
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim token = root.FindToken(position)
            If token.Kind() <> SyntaxKind.StringLiteralToken Then
                Return Nothing
            End If

            Return Await CommonRegexBraceMatcher.FindBracesAsync(
                document, token, position, cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
