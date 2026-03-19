' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Class NameSyntaxIterator
        Implements IEnumerable(Of NameSyntax)

        Private ReadOnly _name As NameSyntax

        Public Sub New(name As NameSyntax)
            If name Is Nothing Then
                Throw New ArgumentNullException(NameOf(name))
            End If

            Me._name = name
        End Sub

        Public Function GetEnumerator() As IEnumerator(Of NameSyntax) Implements IEnumerable(Of NameSyntax).GetEnumerator
            Dim nodes = New LinkedList(Of NameSyntax)

            Dim current = _name
            While True
                If TypeOf current Is QualifiedNameSyntax Then
                    Dim qualifiedName = DirectCast(current, QualifiedNameSyntax)
                    nodes.AddFirst(qualifiedName.Right)
                    current = qualifiedName.Left
                Else
                    nodes.AddFirst(current)
                    Exit While
                End If
            End While

            Return nodes.GetEnumerator()
        End Function

        Private Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function
    End Class
End Namespace
