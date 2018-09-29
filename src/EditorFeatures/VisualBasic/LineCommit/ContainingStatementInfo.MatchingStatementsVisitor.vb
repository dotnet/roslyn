' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Partial Friend Class ContainingStatementInfo
        Private Class MatchingStatementsVisitor
            Inherits VisualBasicSyntaxVisitor(Of IEnumerable(Of StatementSyntax))

            Public Shared ReadOnly Instance As MatchingStatementsVisitor = New MatchingStatementsVisitor()

            Private Sub New()
            End Sub

            Public Overrides Function VisitClassBlock(node As ClassBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitMethodBlock(node As MethodBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitConstructorBlock(node As ConstructorBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitOperatorBlock(node As OperatorBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitAccessorBlock(node As AccessorBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitDoLoopBlock(node As DoLoopBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.DoStatement, node.LoopStatement}
            End Function

            Public Overrides Function VisitEnumBlock(node As EnumBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.EnumStatement, node.EndEnumStatement}
            End Function

            Public Overrides Function VisitForBlock(node As ForBlockSyntax) As IEnumerable(Of StatementSyntax)
                ' TODO: evilness around ending multiple statements at once with a single "next"
                Return {node.ForStatement, node.NextStatement}
            End Function

            Public Overrides Function VisitForEachBlock(node As ForEachBlockSyntax) As IEnumerable(Of StatementSyntax)
                ' TODO: evilness around ending multiple statements at once with a single "next"
                Return {node.ForEachStatement, node.NextStatement}
            End Function

            Public Overrides Iterator Function VisitMultiLineIfBlock(node As MultiLineIfBlockSyntax) As IEnumerable(Of StatementSyntax)
                Yield node.IfStatement
                Yield node.EndIfStatement

                For Each part In node.ElseIfBlocks.Select(Function(elseIfBlock) elseIfBlock.ElseIfStatement)
                    Yield part
                Next

                If node.ElseBlock IsNot Nothing Then
                    Yield node.ElseBlock.ElseStatement
                End If

            End Function

            Public Overrides Function VisitInterfaceBlock(node As InterfaceBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitModuleBlock(node As ModuleBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitNamespaceBlock(node As NamespaceBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.NamespaceStatement, node.EndNamespaceStatement}
            End Function

            Public Overrides Function VisitPropertyBlock(node As PropertyBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.PropertyStatement, node.EndPropertyStatement}
            End Function

            Public Overrides Function VisitSelectBlock(node As SelectBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.SelectStatement, node.EndSelectStatement}
            End Function

            Public Overrides Function VisitSyncLockBlock(node As SyncLockBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.SyncLockStatement, node.EndSyncLockStatement}
            End Function

            Public Overrides Iterator Function VisitTryBlock(node As TryBlockSyntax) As IEnumerable(Of StatementSyntax)
                Yield node.TryStatement
                Yield node.EndTryStatement
                For Each part In node.CatchBlocks.Select(Function(catchBlock) catchBlock.CatchStatement)
                    Yield part
                Next
                If node.FinallyBlock IsNot Nothing Then
                    Yield node.FinallyBlock.FinallyStatement
                End If
            End Function

            Public Overrides Function VisitStructureBlock(node As StructureBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.BlockStatement, node.EndBlockStatement}
            End Function

            Public Overrides Function VisitUsingBlock(node As UsingBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.UsingStatement, node.EndUsingStatement}
            End Function

            Public Overrides Function VisitWithBlock(node As WithBlockSyntax) As IEnumerable(Of StatementSyntax)
                Return {node.WithStatement, node.EndWithStatement}
            End Function
        End Class
    End Class
End Namespace
