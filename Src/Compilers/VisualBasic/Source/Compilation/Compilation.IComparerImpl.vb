Imports Roslyn.Compilers.Common

Namespace Roslyn.Compilers.VisualBasic

    Partial Public Class Compilation

        ''' <summary> This is an implementation of a special symbol comparer, which is supposed to be used  for 
        ''' sorting original definition symbols (explicitly or explicitly declared in source  within the same 
        ''' container) in lexical order of their declarations. It will not work on  anything that uses non-source locations. 
        ''' </summary>        
        Private Class LexicalOrderSymbolComparerImpl
            Implements IComparer(Of Symbol)

            Private ReadOnly compilation As Compilation

            Public Sub New(compilation As Compilation)
                Me.compilation = compilation
            End Sub

            Public Function Compare(x As Symbol, y As Symbol) As Integer Implements IComparer(Of Roslyn.Compilers.VisualBasic.Symbol).Compare
                Dim comparison As Integer

                If x Is y Then
                    Return 0
                End If

                Dim xSortKey = x.GetLexicalSortKey(compilation)
                Dim ySortKey = y.GetLexicalSortKey(compilation)

                If xSortKey.EmbeddedKind <> ySortKey.EmbeddedKind Then
                    ' Embedded sort before non-embedded.
                    Return If(ySortKey.EmbeddedKind > xSortKey.EmbeddedKind, 1, -1)
                End If

                If xSortKey.EmbeddedKind = EmbeddedSymbolKind.None AndAlso xSortKey.Tree IsNot ySortKey.Tree Then
                    If xSortKey.Tree Is Nothing Then
                        Return 1
                    ElseIf ySortKey.Tree Is Nothing Then
                        Return -1
                    End If

                    comparison = compilation.CompareSyntaxTreeOrdering(xSortKey.Tree, ySortKey.Tree)
                    Debug.Assert(comparison <> 0)
                    Return comparison
                End If

                comparison = xSortKey.Location - ySortKey.Location
                If comparison <> 0 Then
                    Return comparison
                End If

                comparison = DirectCast(x, ISymbol).Kind.ToSortOrder() - DirectCast(y, ISymbol).Kind.ToSortOrder()
                If comparison <> 0 Then
                    Return comparison
                End If

                comparison = [String].Compare(x.Name, y.Name)
                Debug.Assert(comparison <> 0)
                Return comparison
            End Function
        End Class
    End Class
End Namespace
