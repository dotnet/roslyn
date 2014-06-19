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
        Partial Public Structure Reversed : Implements IEnumerable(Of SyntaxNodeOrToken)
            Private ReadOnly list As ChildSyntaxList

            Friend Sub New(list As ChildSyntaxList)
                Me.list = list
            End Sub

            Private Function GetEnumerator1() As IEnumerator(Of SyntaxNodeOrToken) Implements IEnumerable(Of SyntaxNodeOrToken).GetEnumerator
                Return GetEnumerator()
            End Function

            Private Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
                Return GetEnumerator()
            End Function

            Public Function GetEnumerator() As Enumerator
                Return New Enumerator(list)
            End Function
        End Structure
    End Structure
#End If
End Namespace

