' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class SymbolDisplayVisitor
        ''' <summary>
        ''' Visits a symbol, and specifically handles symbol types that do not support visiting.
        ''' </summary>
        ''' <param name="symbol">The symbol to visit.</param>
        Public Sub VisitSymbol(symbol As ISymbol)
            Dim preprocessingSymbol = TryCast(symbol, IPreprocessingSymbol)
            If preprocessingSymbol IsNot Nothing Then
                Dim part = New SymbolDisplayPart(SymbolDisplayPartKind.PreprocessingName, preprocessingSymbol, preprocessingSymbol.Name)
                builder.Add(part)
                Return
            End If

            symbol.Accept(Me)
        End Sub
    End Class
End Namespace
