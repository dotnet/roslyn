' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.InheritanceMargin
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.InheritanceMarginService

    Friend Class InheritanceMarginService
        Inherits AbstractInheritanceMarginService

        Protected Overrides Function GetMembers(root As SyntaxNode) As ImmutableArray(Of SyntaxNode)
            Dim typeStatementNodes = root.DescendantNodes().OfType(Of TypeBlockSyntax)

            Dim builder As ArrayBuilder(Of SyntaxNode) = Nothing

            Using ArrayBuilder(Of SyntaxNode).GetInstance(builder)
                builder.AddRange(typeStatementNodes)
                For Each node In typeStatementNodes
                    For Each member In node.Members
                        If TypeOf member Is MethodBlockSyntax _
                            OrElse TypeOf member Is PropertyBlockSyntax _
                            OrElse TypeOf member Is EventStatementSyntax Then
                            builder.Add(member)
                        End If
                    Next
                Next
            End Using

            Return builder.ToImmutable()
        End Function
    End Class
End Namespace

