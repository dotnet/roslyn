' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This is an implementation of a special symbol comparer, which is supposed to be used  for 
    ''' sorting original definition symbols (explicitly or explicitly declared in source  within the same 
    ''' container) in lexical order of their declarations. It will not work on  anything that uses non-source locations. 
    ''' </summary>        
    Friend Class LexicalOrderSymbolComparer
        Implements IComparer(Of Symbol)

        Public Shared ReadOnly Instance As New LexicalOrderSymbolComparer()

        Private Sub New()
        End Sub

        Public Function Compare(x As Symbol, y As Symbol) As Integer Implements IComparer(Of Symbol).Compare
            Dim comparison As Integer

            If x Is y Then
                Return 0
            End If

            Dim xSortKey = x.GetLexicalSortKey()
            Dim ySortKey = y.GetLexicalSortKey()

            comparison = LexicalSortKey.Compare(xSortKey, ySortKey)
            If comparison <> 0 Then
                Return comparison
            End If

            comparison = DirectCast(x, ISymbol).Kind.ToSortOrder() - DirectCast(y, ISymbol).Kind.ToSortOrder()
            If comparison <> 0 Then
                Return comparison
            End If

            comparison = IdentifierComparison.Compare(x.Name, y.Name)
            Debug.Assert(comparison <> 0)
            Return comparison
        End Function
    End Class
End Namespace
