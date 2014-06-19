Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Common.Semantics
Imports Microsoft.CodeAnalysis.Common.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
#If REMOVE Then
    Partial Structure ChildSyntaxList
        Partial Structure Reversed
            Public Structure Enumerator : Implements IEnumerator(Of SyntaxNodeOrToken)
                Private ReadOnly list As ChildSyntaxList
                Private index As Integer

                Friend Sub New(list As ChildSyntaxList)
                    Me.list = list
                    index = list.Count
                End Sub

                Public ReadOnly Property Current As SyntaxNodeOrToken Implements IEnumerator(Of SyntaxNodeOrToken).Current
                    Get
                        Return ChildSyntaxList.ItemInternal(list._node, index)
                    End Get
                End Property

                Private ReadOnly Property Current1 As Object Implements IEnumerator.Current
                    Get
                        Return Current
                    End Get
                End Property

                Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
                    index = index - 1
                    Return index >= 0
                End Function

                Public Sub Reset() Implements IEnumerator.Reset
                    index = list.Count
                End Sub

                Public Sub Dispose() Implements IDisposable.Dispose

                End Sub
            End Structure
        End Structure
    End Structure
#End If
End Namespace

