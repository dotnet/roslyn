' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend NotInheritable Class SyntaxBindingUtilities
        Public Shared Function BindsToResumableStateMachineState(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.YieldStatement) OrElse node.IsKind(SyntaxKind.AwaitExpression)
        End Function

        Public Shared Function BindsToTryStatement(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.TryBlock) OrElse
                   node.IsKind(SyntaxKind.ForEachBlock) OrElse
                   node.IsKind(SyntaxKind.SyncLockBlock) OrElse
                   node.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso node.Parent.IsKind(SyntaxKind.VariableDeclarator) AndAlso node.Parent.Parent.IsKind(SyntaxKind.UsingStatement) OrElse
                   node.IsKind(SyntaxKind.UsingBlock) AndAlso DirectCast(node, UsingBlockSyntax).UsingStatement.Expression IsNot Nothing
        End Function
    End Class
End Namespace
