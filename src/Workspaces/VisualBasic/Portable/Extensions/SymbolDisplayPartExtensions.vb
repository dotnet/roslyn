' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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
