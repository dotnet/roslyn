' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Friend Partial Class SyntaxList

        Friend Class WithThreeChildren
            Inherits SyntaxList

            Private _child0 As SyntaxNode
            Private _child1 As SyntaxNode
            Private _child2 As SyntaxNode

            Friend Sub New(green As InternalSyntax.SyntaxList, parent As SyntaxNode, position As Integer)
                MyBase.New(green, parent, position)
            End Sub

            Friend Overrides Function GetNodeSlot(index As Integer) As SyntaxNode
                Select Case index
                    Case 0
                        Return GetRedElement(Me._child0, 0)
                    Case 1
                        Return GetRedElementIfNotToken(Me._child1)
                    Case 2
                        Return GetRedElement(Me._child2, 2)
                End Select
                Return Nothing
            End Function

            Friend Overrides Function GetCachedSlot(i As Integer) As SyntaxNode
                Select Case i
                    Case 0
                        Return TryCast(Me._child0, VisualBasicSyntaxNode)
                    Case 1
                        Return TryCast(Me._child1, VisualBasicSyntaxNode)
                    Case 2
                        Return TryCast(Me._child2, VisualBasicSyntaxNode)
                    Case Else
                        Return Nothing
                End Select
            End Function
        End Class
    End Class
End Namespace
