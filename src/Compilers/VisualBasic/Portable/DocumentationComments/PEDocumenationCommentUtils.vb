' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Module PEDocumentationCommentUtils

        Friend Function GetDocumentationComment(
            symbol As Symbol,
            containingPEModule As PEModuleSymbol,
            preferredCulture As CultureInfo,
            cancellationToken As CancellationToken,
            ByRef lazyDocComment As Tuple(Of CultureInfo, String)) As String

            If lazyDocComment Is Nothing Then
                Interlocked.CompareExchange(lazyDocComment,
                    Tuple.Create(
                        preferredCulture,
                        containingPEModule.DocumentationProvider.GetDocumentationForSymbol(
                            symbol.GetDocumentationCommentId(), preferredCulture,
                            cancellationToken)),
                    Nothing)
            End If

            If Object.Equals(lazyDocComment.Item1, preferredCulture) Then
                Return lazyDocComment.Item2
            End If

            Return containingPEModule.DocumentationProvider.GetDocumentationForSymbol(
                symbol.GetDocumentationCommentId(), preferredCulture, cancellationToken)
        End Function

    End Module
End Namespace
