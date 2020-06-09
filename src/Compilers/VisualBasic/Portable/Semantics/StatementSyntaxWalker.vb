' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' This class walks all the statements in some syntax, in order, except those statements that are contained
    ''' inside expressions (a statement can occur inside an expression if it is inside
    ''' a lambda.)
    ''' 
    ''' This is used when collecting the declarations and declaration spaces of a method body.
    ''' 
    ''' Typically the client overrides this class and overrides various Visit methods, being sure to always
    ''' delegate back to the base.
    ''' </summary>
    Friend Class StatementSyntaxWalker
        Inherits VisualBasicSyntaxVisitor

        Public Overridable Sub VisitList(list As IEnumerable(Of VisualBasicSyntaxNode))
            For Each n In list
                Visit(n)
            Next
        End Sub

        Public Overrides Sub VisitCompilationUnit(node As CompilationUnitSyntax)
            VisitList(node.Options)
            VisitList(node.Imports)
            VisitList(node.Attributes)
            VisitList(node.Members)
        End Sub

        Public Overrides Sub VisitNamespaceBlock(node As NamespaceBlockSyntax)
            Visit(node.NamespaceStatement)
            VisitList(node.Members)
            Visit(node.EndNamespaceStatement)
        End Sub

        Public Overrides Sub VisitModuleBlock(ByVal node As ModuleBlockSyntax)
            Visit(node.BlockStatement)
            VisitList(node.Members)
            Visit(node.EndBlockStatement)
        End Sub

        Public Overrides Sub VisitClassBlock(ByVal node As ClassBlockSyntax)
            Visit(node.BlockStatement)
            VisitList(node.Inherits)
            VisitList(node.Implements)
            VisitList(node.Members)
            Visit(node.EndBlockStatement)
        End Sub

        Public Overrides Sub VisitStructureBlock(ByVal node As StructureBlockSyntax)
            Visit(node.BlockStatement)
            VisitList(node.Inherits)
            VisitList(node.Implements)
            VisitList(node.Members)
            Visit(node.EndBlockStatement)
        End Sub

        Public Overrides Sub VisitInterfaceBlock(ByVal node As InterfaceBlockSyntax)
            Visit(node.BlockStatement)
            VisitList(node.Inherits)
            VisitList(node.Members)
            Visit(node.EndBlockStatement)
        End Sub

        Public Overrides Sub VisitEnumBlock(ByVal node As EnumBlockSyntax)
            Visit(node.EnumStatement)
            VisitList(node.Members)
            Visit(node.EndEnumStatement)
        End Sub

        Public Overrides Sub VisitMethodBlock(ByVal node As MethodBlockSyntax)
            Visit(node.BlockStatement)
            VisitList(node.Statements)
            Visit(node.EndBlockStatement)
        End Sub

        Public Overrides Sub VisitConstructorBlock(node As ConstructorBlockSyntax)
            Visit(node.BlockStatement)
            VisitList(node.Statements)
            Visit(node.EndBlockStatement)
        End Sub

        Public Overrides Sub VisitOperatorBlock(node As OperatorBlockSyntax)
            Visit(node.BlockStatement)
            VisitList(node.Statements)
            Visit(node.EndBlockStatement)
        End Sub

        Public Overrides Sub VisitAccessorBlock(node As AccessorBlockSyntax)
            Visit(node.BlockStatement)
            VisitList(node.Statements)
            Visit(node.EndBlockStatement)
        End Sub

        Public Overrides Sub VisitPropertyBlock(ByVal node As PropertyBlockSyntax)
            Visit(node.PropertyStatement)
            VisitList(node.Accessors)
            Visit(node.EndPropertyStatement)
        End Sub

        Public Overrides Sub VisitEventBlock(ByVal node As EventBlockSyntax)
            Visit(node.EventStatement)
            VisitList(node.Accessors)
            Visit(node.EndEventStatement)
        End Sub

        Public Overrides Sub VisitWhileBlock(ByVal node As WhileBlockSyntax)
            Visit(node.WhileStatement)
            VisitList(node.Statements)
            Visit(node.EndWhileStatement)
        End Sub

        Public Overrides Sub VisitUsingBlock(ByVal node As UsingBlockSyntax)
            Visit(node.UsingStatement)
            VisitList(node.Statements)
            Visit(node.EndUsingStatement)
        End Sub

        Public Overrides Sub VisitSyncLockBlock(ByVal node As SyncLockBlockSyntax)
            Visit(node.SyncLockStatement)
            VisitList(node.Statements)
            Visit(node.EndSyncLockStatement)
        End Sub

        Public Overrides Sub VisitWithBlock(ByVal node As WithBlockSyntax)
            Visit(node.WithStatement)
            VisitList(node.Statements)
            Visit(node.EndWithStatement)
        End Sub

        Public Overrides Sub VisitSingleLineIfStatement(ByVal node As SingleLineIfStatementSyntax)
            VisitList(node.Statements)
            Visit(node.ElseClause)
        End Sub

        Public Overrides Sub VisitSingleLineElseClause(ByVal node As SingleLineElseClauseSyntax)
            VisitList(node.Statements)
        End Sub

        Public Overrides Sub VisitMultiLineIfBlock(ByVal node As MultiLineIfBlockSyntax)
            Visit(node.IfStatement)
            VisitList(node.Statements)
            VisitList(node.ElseIfBlocks)
            Visit(node.ElseBlock)
            Visit(node.EndIfStatement)
        End Sub

        Public Overrides Sub VisitElseIfBlock(ByVal node As ElseIfBlockSyntax)
            Visit(node.ElseIfStatement)
            VisitList(node.Statements)
        End Sub

        Public Overrides Sub VisitElseBlock(ByVal node As ElseBlockSyntax)
            Visit(node.ElseStatement)
            VisitList(node.Statements)
        End Sub

        Public Overrides Sub VisitTryBlock(ByVal node As TryBlockSyntax)
            Visit(node.TryStatement)
            VisitList(node.Statements)
            VisitList(node.CatchBlocks)
            Visit(node.FinallyBlock)
            Visit(node.EndTryStatement)
        End Sub

        Public Overrides Sub VisitCatchBlock(ByVal node As CatchBlockSyntax)
            Visit(node.CatchStatement)
            VisitList(node.Statements)
        End Sub

        Public Overrides Sub VisitFinallyBlock(ByVal node As FinallyBlockSyntax)
            Visit(node.FinallyStatement)
            VisitList(node.Statements)
        End Sub

        Public Overrides Sub VisitSelectBlock(ByVal node As SelectBlockSyntax)
            Visit(node.SelectStatement)
            VisitList(node.CaseBlocks)
            Visit(node.EndSelectStatement)
        End Sub

        Public Overrides Sub VisitCaseBlock(ByVal node As CaseBlockSyntax)
            Visit(node.CaseStatement)
            VisitList(node.Statements)
        End Sub

        Public Overrides Sub VisitDoLoopBlock(ByVal node As DoLoopBlockSyntax)
            Visit(node.DoStatement)
            VisitList(node.Statements)
            Visit(node.LoopStatement)
        End Sub

        Public Overrides Sub VisitForBlock(ByVal node As ForBlockSyntax)
            Visit(node.ForStatement)
            VisitList(node.Statements)
        End Sub

        Public Overrides Sub VisitForEachBlock(ByVal node As ForEachBlockSyntax)
            Visit(node.ForEachStatement)
            VisitList(node.Statements)
        End Sub
    End Class
End Namespace
