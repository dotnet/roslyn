' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module SymbolDisplayPartExtensions
        <Extension()>
        Public Function MassageErrorTypeNames(p As SymbolDisplayPart, Optional replacement As String = Nothing) As SymbolDisplayPart
            If p.Kind = SymbolDisplayPartKind.ErrorTypeName Then
                Dim text = p.ToString()
                If text = String.Empty Then
                    Return If(replacement = Nothing,
                              New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "Object"),
                              New SymbolDisplayPart(SymbolDisplayPartKind.Text, Nothing, replacement))
                End If

                If SyntaxFacts.GetKeywordKind(text) <> SyntaxKind.None Then
                    Return New SymbolDisplayPart(SymbolDisplayPartKind.ErrorTypeName, Nothing, String.Format("[{0}]", text))
                End If
            End If

            Return p
        End Function
    End Module

End Namespace
