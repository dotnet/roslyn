' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Partial Friend Class ContainingStatementInfo
        Private Class MatchingStatementsVisitor
            Inherits VisualBasicSyntaxVisitor(Of IList(Of StatementSyntax))

            Public Shared ReadOnly Instance As MatchingStatementsVisitor = New MatchingStatementsVisitor()

            Private Sub New()
            End Sub

            Public Overrides Function VisitClassBlock(node As ClassBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitMethodBlock(node As MethodBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitConstructorBlock(node As ConstructorBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitOperatorBlock(node As OperatorBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitAccessorBlock(node As AccessorBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitDoLoopBlock(node As DoLoopBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.DoStatement, node.LoopStatement}
            End Function

            Public Overrides Function VisitEnumBlock(node As EnumBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.EnumStatement, node.EndEnumStatement}
            End Function

            Public Overrides Function VisitForBlock(node As ForBlockSyntax) As IList(Of StatementSyntax)
                ' TODO: evilness around ending multiple statements at once with a single "next"
                Return New StatementSyntax() {node.ForStatement, node.NextStatement}
            End Function

            Public Overrides Function VisitForEachBlock(node As ForEachBlockSyntax) As IList(Of StatementSyntax)
                ' TODO: evilness around ending multiple statements at once with a single "next"
                Return New StatementSyntax() {node.ForEachStatement, node.NextStatement}
            End Function

            Public Overrides Function VisitMultiLineIfBlock(node As MultiLineIfBlockSyntax) As IList(Of StatementSyntax)
                ' TODO: just use Yield Return once we have it in VB
                Dim parts As New List(Of StatementSyntax) From {node.IfStatement, node.EndIfStatement}

                parts.AddRange(node.ElseIfBlocks.Select(Function(elseIfBlock) elseIfBlock.ElseIfStatement))

                If node.ElseBlock IsNot Nothing Then
                    parts.Add(node.ElseBlock.ElseStatement)
                End If

                Return parts
            End Function

            Public Overrides Function VisitInterfaceBlock(node As InterfaceBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitModuleBlock(node As ModuleBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitNamespaceBlock(node As NamespaceBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.NamespaceStatement, node.EndNamespaceStatement}
            End Function

            Public Overrides Function VisitPropertyBlock(node As PropertyBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.PropertyStatement, node.EndPropertyStatement}
            End Function

            Public Overrides Function VisitSelectBlock(node As SelectBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.SelectStatement, node.EndSelectStatement}
            End Function

            Public Overrides Function VisitSyncLockBlock(node As SyncLockBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.SyncLockStatement, node.EndSyncLockStatement}
            End Function

            Public Overrides Function VisitTryBlock(node As TryBlockSyntax) As IList(Of StatementSyntax)
                ' TODO: just use Yield Return once we have it in VB
                Dim parts As New List(Of StatementSyntax) From {node.TryStatement, node.EndTryStatement}

                parts.AddRange(node.CatchBlocks.Select(Function(catchBlock) catchBlock.CatchStatement))

                If node.FinallyBlock IsNot Nothing Then
                    parts.Add(node.FinallyBlock.FinallyStatement)
                End If

                Return parts
            End Function

            Public Overrides Function VisitStructureBlock(node As StructureBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitUsingBlock(node As UsingBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.UsingStatement, node.EndUsingStatement}
            End Function

            Public Overrides Function VisitWithBlock(node As WithBlockSyntax) As IList(Of StatementSyntax)
                Return New StatementSyntax() {node.WithStatement, node.EndWithStatement}
            End Function
        End Class
    End Class
End Namespace
