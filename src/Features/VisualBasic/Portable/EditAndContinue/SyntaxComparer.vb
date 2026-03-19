' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue

    Friend NotInheritable Class SyntaxComparer
        Inherits AbstractSyntaxComparer

        Friend Shared ReadOnly TopLevel As SyntaxComparer = New SyntaxComparer(Nothing, Nothing, Nothing, Nothing, compareStatementSyntax:=False)

        Friend Shared ReadOnly Statement As SyntaxComparer = New SyntaxComparer(Nothing, Nothing, Nothing, Nothing, compareStatementSyntax:=True)

        Private ReadOnly _matchingLambdas As Boolean

        Public Sub New(oldRoot As SyntaxNode, newRoot As SyntaxNode, oldRootChildren As IEnumerable(Of SyntaxNode), newRootChildren As IEnumerable(Of SyntaxNode), Optional matchingLambdas As Boolean = False, Optional compareStatementSyntax As Boolean = False)
            MyBase.New(oldRoot, newRoot, oldRootChildren, newRootChildren, compareStatementSyntax)

            _matchingLambdas = matchingLambdas
        End Sub

        Protected Overrides Function IsLambdaBodyStatementOrExpression(node As SyntaxNode) As Boolean
            Return LambdaUtilities.IsLambdaBodyStatementOrExpression(node)
        End Function

#Region "Labels"

        ' Assumptions:
        ' - Each listed label corresponds to one or more syntax kinds.
        ' - Nodes with same labels might produce Update edits, nodes with different labels don't. 
        ' - If IsTiedToParent(label) is true for a label then all its possible parent labels must precede the label.
        '   (i.e. both MethodDeclaration and TypeDeclaration must precede TypeParameter label).
        ' - All descendants of a node whose kind is listed here will be ignored regardless of their labels
        Friend Enum Label
            '
            ' Top Syntax
            '
            CompilationUnit
            [Option]                         ' tied to parent
            Import                           ' tied to parent
            Attributes                       ' tied to parent

            NamespaceDeclaration
            TypeDeclaration
            EnumDeclaration
            DelegateDeclaration
            FieldDeclaration                 ' tied to parent
            FieldVariableDeclarator          ' tied to parent

            PInvokeDeclaration               ' tied to parent
            MethodDeclaration                ' tied to parent
            ConstructorDeclaration           ' tied to parent
            OperatorDeclaration              ' tied to parent
            PropertyDeclaration              ' tied to parent
            CustomEventDeclaration           ' tied to parent
            EnumMemberDeclaration            ' tied to parent
            AccessorDeclaration              ' tied to parent

            ' Opening statement of a type, method, operator, constructor, property, and accessor.
            ' We need to represent this node in the graph since attributes, (generic) parameters are its children.
            ' However, we don't need to have a specialized label for each type of declaration statement since 
            ' they are tied to the parent and each parent has a single declaration statement.
            DeclarationStatement             ' tied to parent

            ' Event statement is either a child of a custom event or a stand-alone event field declaration.
            EventStatement                   ' tied to parent

            TypeParameterList                ' tied to parent
            TypeParameter                    ' tied to parent

            FieldOrParameterName             ' tied to grand-grandparent (type or method declaration)

            AttributeList                    ' tied to parent
            Attribute                        ' tied to parent

            '
            ' Statement Syntax
            '
            BodyBlock

            ' We need to represent sub/function/ctor/accessor/operator declaration 
            ' begin And End statements since they may be active statements.
            BodyBegin                       ' tied to parent

            LambdaRoot

            TryBlock
            TryStatement                    ' tied to parent
            CatchBlock                      ' tied to parent 
            CatchStatement                  ' tied to parent
            FinallyBlock                    ' tied to parent 
            FinallyStatement                ' tied to parent
            CatchFilterClause               ' tied to parent 
            EndTryStatement                 ' tied to parent 

            ForBlock
            ForEachBlock
            ForEachStatement                ' tied to parent
            ForStatement                    ' tied to parent
            ForStepClause                   ' tied to parent
            NextStatement                   ' tied to parent

            UsingBlockWithDeclarations
            UsingBlockWithExpression
            UsingStatement                  ' tied to parent
            EndUsingStatement               ' tied to parent

            SyncLockBlock
            SyncLockStatement               ' tied to parent 
            EndSyncLockStatement            ' tied to parent

            WithBlock
            WithStatement                   ' tied to parent 
            EndWithStatement                ' tied to parent

            DoWhileBlock
            DoWhileStatement                ' tied to parent
            EndLoop                         ' tied to parent
            WhileOrUntilClause                ' tied to parent

            IfBlock
            IfStatement                     ' tied to parent 
            ElseIfBlock                     ' tied to parent 
            ElseIfStatement                 ' tied to parent 
            ElseBlock                       ' tied to parent
            ElseStatement                   ' tied to parent
            EndIfStatement                  ' tied to parent

            SelectBlock
            SelectStatement                 ' tied to parent
            CaseBlock                       ' tied to parent
            CaseStatement                   ' tied to parent
            CaseClause                      ' tied to parent
            EndSelectStatement              ' tied to parent

            ReDimStatement
            ReDimClause                     ' tied to parent 

            ' Exit Sub|Function|Operator|Property|For|While|Do|Select|Try
            ExitStatement

            ' Continue While|Do|For
            ContinueStatement

            ' Throw, Throw expr
            ThrowStatement
            ErrorStatement

            ' Return, Return expr
            ReturnStatement

            OnErrorStatement
            ResumeStatement

            ' GoTo, Stop, End
            GoToStatement

            LabelStatement
            EraseStatement
            ExpressionStatement
            AssignmentStatement
            EventHandlerStatement

            YieldStatement

            LocalDeclarationStatement           ' tied to parent 
            LocalVariableDeclarator             ' tied to parent 

            AwaitExpression

            Lambda
            LambdaBodyBegin                     ' tied to parent

            QueryExpression
            AggregateClause                     ' tied to parent 
            JoinClause                          ' tied to parent
            FromClause                          ' tied to parent
            WhereClauseLambda                   ' tied to parent 
            LetClause                           ' tied to parent
            SelectClauseLambda                  ' tied to parent
            PartitionWhileLambda                ' tied to parent
            PartitionClause                     ' tied to parent
            GroupByClause                       ' tied to parent
            OrderByClause                       ' tied to parent

            CollectionRangeVariable             ' tied to parent (FromClause, JoinClause, AggregateClause)
            ExpressionRangeVariable             ' tied to parent (LetClause, SelectClause, GroupByClause keys)
            ExpressionRangeVariableItems        ' tied to parent (GroupByClause items)
            FunctionAggregationLambda           ' tied to parent (JoinClause, GroupByClause, AggregateClause)

            OrderingLambda                      ' tied to parent (OrderByClause)
            JoinConditionLambda                 ' tied to parent (JoinClause)

            ParameterList                       ' tied to parent
            Parameter                           ' tied to parent

            LocalVariableName                   ' tied to parent 

            BodyEnd                             ' tied to parent

            ' helpers
            Count
            Ignored = IgnoredNode
        End Enum

        ''' <summary>
        ''' Return true if it is desirable to report two edits (delete and insert) rather than a move edit
        ''' when the node changes its parent.
        ''' </summary>
        Private Overloads Shared Function TiedToAncestor(label As Label) As Integer
            Select Case label
                ' Top level syntax
                Case Label.Option,
                     Label.Import,
                     Label.Attributes,
                     Label.FieldDeclaration,
                     Label.FieldVariableDeclarator,
                     Label.PInvokeDeclaration,
                     Label.MethodDeclaration,
                     Label.OperatorDeclaration,
                     Label.ConstructorDeclaration,
                     Label.PropertyDeclaration,
                     Label.CustomEventDeclaration,
                     Label.EnumMemberDeclaration,
                     Label.AccessorDeclaration,
                     Label.DeclarationStatement,
                     Label.EventStatement,
                     Label.TypeParameterList,
                     Label.TypeParameter,
                     Label.ParameterList,
                     Label.Parameter,
                     Label.AttributeList,
                     Label.Attribute
                    Return 1

                Case Label.FieldOrParameterName
                    Return 3 ' type or method declaration

                ' Statement syntax
                Case Label.BodyBegin,
                     Label.LambdaBodyBegin,
                     Label.BodyEnd,
                     Label.TryStatement,
                     Label.CatchBlock,
                     Label.CatchStatement,
                     Label.FinallyBlock,
                     Label.FinallyStatement,
                     Label.CatchFilterClause,
                     Label.EndTryStatement,
                     Label.ForEachStatement,
                     Label.ForStatement,
                     Label.ForStepClause,
                     Label.NextStatement,
                     Label.UsingStatement,
                     Label.EndUsingStatement,
                     Label.SyncLockStatement,
                     Label.EndSyncLockStatement,
                     Label.WithStatement,
                     Label.EndWithStatement,
                     Label.DoWhileStatement,
                     Label.WhileOrUntilClause,
                     Label.EndLoop,
                     Label.IfStatement,
                     Label.ElseIfBlock,
                     Label.ElseIfStatement,
                     Label.ElseBlock,
                     Label.ElseStatement,
                     Label.EndIfStatement,
                     Label.SelectStatement,
                     Label.CaseBlock,
                     Label.CaseStatement,
                     Label.CaseClause,
                     Label.EndSelectStatement,
                     Label.ReDimClause,
                     Label.AggregateClause,
                     Label.JoinClause,
                     Label.FromClause,
                     Label.WhereClauseLambda,
                     Label.LetClause,
                     Label.SelectClauseLambda,
                     Label.PartitionWhileLambda,
                     Label.PartitionClause,
                     Label.GroupByClause,
                     Label.OrderByClause,
                     Label.CollectionRangeVariable,
                     Label.ExpressionRangeVariable,
                     Label.ExpressionRangeVariableItems,
                     Label.FunctionAggregationLambda,
                     Label.OrderingLambda,
                     Label.JoinConditionLambda,
                     Label.LocalDeclarationStatement,
                     Label.LocalVariableDeclarator,
                     Label.LocalVariableName
                    Return 1

                Case Else
                    Return 0
            End Select

            Throw New NotImplementedException()
        End Function

        Friend Overrides Function Classify(kind As Integer, node As SyntaxNode, ByRef isLeaf As Boolean) As Integer
            Return Classify(CType(kind, SyntaxKind), node, isLeaf, ignoreVariableDeclarations:=False)
        End Function

        ' internal for testing
        Friend Overloads Function Classify(kind As SyntaxKind, nodeOpt As SyntaxNode, ByRef isLeaf As Boolean, ignoreVariableDeclarations As Boolean) As Label
            If _compareStatementSyntax Then
                Return ClassifyStatementSyntax(kind, nodeOpt, isLeaf)
            End If

            Return ClassifyTopSyntax(kind, nodeOpt, isLeaf, ignoreVariableDeclarations)
        End Function

        Friend Shared Function ClassifyStatementSyntax(kind As SyntaxKind, nodeOpt As SyntaxNode, ByRef isLeaf As Boolean) As Label
            isLeaf = False

            Select Case kind
                Case SyntaxKind.SubBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    Return Label.BodyBlock

                Case SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    isLeaf = True
                    Return Label.BodyBegin

                Case SyntaxKind.SubLambdaHeader,
                     SyntaxKind.FunctionLambdaHeader
                    Return Label.LambdaBodyBegin

                Case SyntaxKind.EndSubStatement,
                     SyntaxKind.EndFunctionStatement,
                     SyntaxKind.EndOperatorStatement,
                     SyntaxKind.EndGetStatement,
                     SyntaxKind.EndSetStatement,
                     SyntaxKind.EndAddHandlerStatement,
                     SyntaxKind.EndRemoveHandlerStatement,
                     SyntaxKind.EndRaiseEventStatement
                    isLeaf = True
                    Return Label.BodyEnd

                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock,
                     SyntaxKind.WhileBlock
                    Return Label.DoWhileBlock

                Case SyntaxKind.SimpleDoStatement, SyntaxKind.DoWhileStatement, SyntaxKind.DoUntilStatement,
                     SyntaxKind.WhileStatement
                    Return Label.DoWhileStatement

                Case SyntaxKind.WhileClause,
                     SyntaxKind.UntilClause
                    Return Label.WhileOrUntilClause

                Case SyntaxKind.SimpleLoopStatement, SyntaxKind.LoopWhileStatement, SyntaxKind.LoopUntilStatement,
                     SyntaxKind.EndWhileStatement
                    Return Label.EndLoop

                Case SyntaxKind.ForBlock
                    Return Label.ForBlock

                Case SyntaxKind.ForEachBlock
                    Return Label.ForEachBlock

                Case SyntaxKind.ForStatement
                    Return Label.ForStatement

                Case SyntaxKind.ForEachStatement
                    Return Label.ForEachStatement

                Case SyntaxKind.ForStepClause
                    Return Label.ForStepClause

                Case SyntaxKind.NextStatement
                    isLeaf = True
                    Return Label.NextStatement

                Case SyntaxKind.UsingBlock
                    ' We need to distinguish using statements with expression or single variable declaration from ones with multiple variable declarations. 
                    ' The former generate a single try-finally block, the latter one for each variable. The finally blocks need to match since they
                    ' affect state machine state matching. For simplicity we do not match single-declaration to expression, we just treat usings
                    ' with declarations entirely separately from usings with expressions.
                    '
                    ' The parent is not available only when comparing nodes for value equality.
                    ' In that case it doesn't matter what label the node has as long as it has some.

                    Return If(TryCast(nodeOpt, UsingBlockSyntax)?.UsingStatement.Variables IsNot Nothing, Label.UsingBlockWithDeclarations, Label.UsingBlockWithExpression)

                Case SyntaxKind.UsingStatement
                    Return Label.UsingStatement

                Case SyntaxKind.EndUsingStatement
                    isLeaf = True
                    Return Label.EndUsingStatement

                Case SyntaxKind.SyncLockBlock
                    Return Label.SyncLockBlock

                Case SyntaxKind.SyncLockStatement
                    Return Label.SyncLockStatement

                Case SyntaxKind.EndSyncLockStatement
                    isLeaf = True
                    Return Label.EndSyncLockStatement

                Case SyntaxKind.WithBlock
                    Return Label.WithBlock

                Case SyntaxKind.WithStatement
                    Return Label.WithStatement

                Case SyntaxKind.EndWithStatement
                    isLeaf = True
                    Return Label.EndWithStatement

                Case SyntaxKind.LocalDeclarationStatement
                    Return Label.LocalDeclarationStatement

                Case SyntaxKind.VariableDeclarator
                    Return Label.LocalVariableDeclarator

                Case SyntaxKind.ModifiedIdentifier
                    Return Label.LocalVariableName

                Case SyntaxKind.MultiLineIfBlock,
                     SyntaxKind.SingleLineIfStatement
                    Return Label.IfBlock

                Case SyntaxKind.MultiLineIfBlock,
                     SyntaxKind.SingleLineIfStatement
                    Return Label.IfBlock

                Case SyntaxKind.IfStatement
                    Return Label.IfStatement

                Case SyntaxKind.ElseIfBlock
                    Return Label.ElseIfBlock

                Case SyntaxKind.ElseIfStatement
                    Return Label.ElseIfStatement

                Case SyntaxKind.ElseBlock,
                     SyntaxKind.SingleLineElseClause
                    Return Label.ElseBlock

                Case SyntaxKind.ElseStatement
                    isLeaf = True
                    Return Label.ElseStatement

                Case SyntaxKind.EndIfStatement
                    isLeaf = True
                    Return Label.EndIfStatement

                Case SyntaxKind.TryBlock
                    Return Label.TryBlock

                Case SyntaxKind.TryBlock
                    Return Label.TryBlock

                Case SyntaxKind.TryStatement
                    Return Label.TryStatement

                Case SyntaxKind.CatchBlock
                    Return Label.CatchBlock

                Case SyntaxKind.CatchStatement
                    Return Label.CatchStatement

                Case SyntaxKind.FinallyBlock
                    Return Label.FinallyBlock

                Case SyntaxKind.FinallyStatement
                    Return Label.FinallyStatement

                Case SyntaxKind.CatchFilterClause
                    Return Label.CatchFilterClause

                Case SyntaxKind.EndTryStatement
                    isLeaf = True
                    Return Label.EndTryStatement

                Case SyntaxKind.ErrorStatement
                    isLeaf = True
                    Return Label.ErrorStatement

                Case SyntaxKind.ThrowStatement
                    Return Label.ThrowStatement

                Case SyntaxKind.OnErrorGoToZeroStatement,
                     SyntaxKind.OnErrorGoToMinusOneStatement,
                     SyntaxKind.OnErrorGoToLabelStatement,
                     SyntaxKind.OnErrorResumeNextStatement
                    Return Label.OnErrorStatement

                Case SyntaxKind.ResumeStatement,
                     SyntaxKind.ResumeLabelStatement,
                     SyntaxKind.ResumeNextStatement
                    Return Label.ResumeStatement

                Case SyntaxKind.SelectBlock
                    Return Label.SelectBlock

                Case SyntaxKind.SelectStatement
                    Return Label.SelectStatement

                Case SyntaxKind.CaseBlock,
                     SyntaxKind.CaseElseBlock
                    Return Label.CaseBlock

                Case SyntaxKind.CaseStatement,
                     SyntaxKind.CaseElseStatement
                    Return Label.CaseStatement

                Case SyntaxKind.ElseCaseClause,
                     SyntaxKind.SimpleCaseClause,
                     SyntaxKind.RangeCaseClause,
                     SyntaxKind.CaseEqualsClause,
                     SyntaxKind.CaseNotEqualsClause,
                     SyntaxKind.CaseLessThanClause,
                     SyntaxKind.CaseLessThanOrEqualClause,
                     SyntaxKind.CaseGreaterThanOrEqualClause,
                     SyntaxKind.CaseGreaterThanClause
                    Return Label.CaseClause

                Case SyntaxKind.EndSelectStatement
                    isLeaf = True
                    Return Label.EndSelectStatement

                Case SyntaxKind.ExitForStatement,
                     SyntaxKind.ExitDoStatement,
                     SyntaxKind.ExitWhileStatement,
                     SyntaxKind.ExitSelectStatement,
                     SyntaxKind.ExitTryStatement,
                     SyntaxKind.ExitSubStatement,
                     SyntaxKind.ExitFunctionStatement,
                     SyntaxKind.ExitOperatorStatement,
                     SyntaxKind.ExitPropertyStatement
                    isLeaf = True
                    Return Label.ExitStatement

                Case SyntaxKind.ContinueWhileStatement,
                     SyntaxKind.ContinueDoStatement,
                     SyntaxKind.ContinueForStatement
                    isLeaf = True
                    Return Label.ContinueStatement

                Case SyntaxKind.ReturnStatement
                    Return Label.ReturnStatement

                Case SyntaxKind.GoToStatement,
                     SyntaxKind.StopStatement,
                     SyntaxKind.EndStatement
                    isLeaf = True
                    Return Label.GoToStatement

                Case SyntaxKind.LabelStatement
                    isLeaf = True
                    Return Label.LabelStatement

                Case SyntaxKind.EraseStatement
                    isLeaf = True
                    Return Label.EraseStatement

                Case SyntaxKind.ExpressionStatement,
                     SyntaxKind.CallStatement
                    Return Label.ExpressionStatement

                Case SyntaxKind.MidAssignmentStatement,
                     SyntaxKind.SimpleAssignmentStatement,
                     SyntaxKind.AddAssignmentStatement,
                     SyntaxKind.SubtractAssignmentStatement,
                     SyntaxKind.MultiplyAssignmentStatement,
                     SyntaxKind.DivideAssignmentStatement,
                     SyntaxKind.IntegerDivideAssignmentStatement,
                     SyntaxKind.ExponentiateAssignmentStatement,
                     SyntaxKind.LeftShiftAssignmentStatement,
                     SyntaxKind.RightShiftAssignmentStatement,
                     SyntaxKind.ConcatenateAssignmentStatement
                    Return Label.AssignmentStatement

                Case SyntaxKind.AddHandlerStatement,
                     SyntaxKind.RemoveHandlerStatement,
                     SyntaxKind.RaiseEventStatement
                    Return Label.EventHandlerStatement

                Case SyntaxKind.ReDimStatement,
                     SyntaxKind.ReDimPreserveStatement
                    Return Label.ReDimStatement

                Case SyntaxKind.RedimClause
                    Return Label.ReDimClause

                Case SyntaxKind.YieldStatement
                    Return Label.YieldStatement

                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return Label.Lambda

                Case SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubLambdaHeader
                    Return Label.LambdaBodyBegin

                Case SyntaxKind.ParameterList
                    Return Label.ParameterList

                Case SyntaxKind.Parameter
                    Return Label.Parameter

                Case SyntaxKind.QueryExpression
                    Return Label.QueryExpression

                Case SyntaxKind.WhereClause
                    Return Label.WhereClauseLambda

                Case SyntaxKind.LetClause
                    Return Label.LetClause

                Case SyntaxKind.SkipClause,
                     SyntaxKind.TakeClause
                    Return Label.PartitionClause

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    Return Label.PartitionWhileLambda

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Return Label.OrderingLambda

                Case SyntaxKind.FunctionAggregation
                    Return Label.FunctionAggregationLambda

                Case SyntaxKind.SelectClause
                    Return Label.SelectClauseLambda

                Case SyntaxKind.GroupByClause
                    Return Label.GroupByClause

                Case SyntaxKind.OrderByClause
                    Return Label.OrderByClause

                Case SyntaxKind.SimpleJoinClause,
                     SyntaxKind.GroupJoinClause
                    Return Label.JoinClause

                Case SyntaxKind.AggregateClause
                    Return Label.AggregateClause

                Case SyntaxKind.FromClause
                    Return Label.FromClause

                Case SyntaxKind.ExpressionRangeVariable
                    ' Select, Let, GroupBy
                    '
                    ' All ERVs need to be included in the map,
                    ' so that we are able to map range variable declarations.
                    ' 
                    ' Since we don't distinguish between ERVs that represent a lambda and non-lambda ERVs,
                    ' we need to be handle cases when one maps to the other (and vice versa).
                    ' This is handled in GetCorresondingLambdaBody.
                    '
                    ' On the other hand we don't want map across lambda body boundaries.
                    ' Hence we use a label for ERVs in Items distinct from one for Keys of a GroupBy clause.
                    '
                    ' Node that the nodeOpt is Nothing only when comparing nodes for value equality.
                    ' In that case it doesn't matter what label the node has as long as it has some.
                    If nodeOpt IsNot Nothing AndAlso
                       nodeOpt.Parent.IsKind(SyntaxKind.GroupByClause) AndAlso
                       nodeOpt.SpanStart < DirectCast(nodeOpt.Parent, GroupByClauseSyntax).ByKeyword.SpanStart Then
                        Return Label.ExpressionRangeVariableItems
                    Else
                        Return Label.ExpressionRangeVariable
                    End If

                Case SyntaxKind.CollectionRangeVariable
                    ' From, Aggregate
                    '
                    ' All CRVs need to be included in the map,
                    ' so that we are able to map range variable declarations.

                    Return Label.CollectionRangeVariable

                Case SyntaxKind.JoinCondition
                    ' TODO:
                    Return Label.JoinConditionLambda

                Case SyntaxKind.AwaitExpression
                    Return Label.AwaitExpression

                Case SyntaxKind.GenericName
                    ' optimization - no need to dig into type instantiations
                    isLeaf = True
                    Return Label.Ignored

                Case Else
                    Return Label.Ignored
            End Select
        End Function

        Private Shared Function ClassifyTopSyntax(kind As SyntaxKind, nodeOpt As SyntaxNode, ByRef isLeaf As Boolean, ignoreVariableDeclarations As Boolean) As Label
            Select Case kind
                Case SyntaxKind.CompilationUnit
                    isLeaf = False
                    Return Label.CompilationUnit

                Case SyntaxKind.OptionStatement
                    isLeaf = True
                    Return Label.Option

                Case SyntaxKind.ImportsStatement
                    isLeaf = True
                    Return Label.Import

                Case SyntaxKind.AttributesStatement
                    isLeaf = False
                    Return Label.Attributes

                Case SyntaxKind.NamespaceBlock
                    isLeaf = False
                    Return Label.NamespaceDeclaration

                Case SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.ModuleBlock
                    isLeaf = False
                    Return Label.TypeDeclaration

                Case SyntaxKind.EnumBlock
                    isLeaf = False
                    Return Label.EnumDeclaration

                Case SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                    isLeaf = False
                    Return Label.DelegateDeclaration

                Case SyntaxKind.FieldDeclaration
                    isLeaf = False
                    Return Label.FieldDeclaration

                Case SyntaxKind.VariableDeclarator
                    isLeaf = ignoreVariableDeclarations
                    Return If(ignoreVariableDeclarations, Label.Ignored, Label.FieldVariableDeclarator)

                Case SyntaxKind.ModifiedIdentifier
                    isLeaf = True
                    Return If(ignoreVariableDeclarations, Label.Ignored, Label.FieldOrParameterName)

                Case SyntaxKind.SubBlock, SyntaxKind.FunctionBlock
                    isLeaf = False
                    Return Label.MethodDeclaration

                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    isLeaf = False
                    Return Label.PInvokeDeclaration

                Case SyntaxKind.ConstructorBlock
                    isLeaf = False
                    Return Label.ConstructorDeclaration

                Case SyntaxKind.OperatorBlock
                    isLeaf = False
                    Return Label.OperatorDeclaration

                Case SyntaxKind.PropertyBlock
                    isLeaf = False
                    Return Label.PropertyDeclaration

                Case SyntaxKind.EventBlock
                    isLeaf = False
                    Return Label.CustomEventDeclaration

                Case SyntaxKind.EnumMemberDeclaration
                    isLeaf = False
                    Return Label.EnumMemberDeclaration

                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    isLeaf = False
                    Return Label.AccessorDeclaration

                Case SyntaxKind.ClassStatement,
                     SyntaxKind.StructureStatement,
                     SyntaxKind.InterfaceStatement,
                     SyntaxKind.ModuleStatement,
                     SyntaxKind.NamespaceStatement,
                     SyntaxKind.EnumStatement,
                     SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.PropertyStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    isLeaf = False
                    Return Label.DeclarationStatement

                Case SyntaxKind.EventStatement
                    isLeaf = False
                    Return Label.EventStatement

                Case SyntaxKind.TypeParameterList
                    isLeaf = False
                    Return Label.TypeParameterList

                Case SyntaxKind.TypeParameter
                    isLeaf = False
                    Return Label.TypeParameter

                Case SyntaxKind.ParameterList
                    isLeaf = False
                    Return Label.ParameterList

                Case SyntaxKind.Parameter
                    isLeaf = False
                    Return Label.Parameter

                Case SyntaxKind.AttributeList
                    If nodeOpt IsNot Nothing AndAlso nodeOpt.IsParentKind(SyntaxKind.AttributesStatement) Then
                        isLeaf = False
                        Return Label.AttributeList
                    End If

                    isLeaf = True
                    Return Label.Ignored

                Case SyntaxKind.Attribute
                    isLeaf = True
                    If nodeOpt IsNot Nothing AndAlso nodeOpt.Parent.IsParentKind(SyntaxKind.AttributesStatement) Then
                        Return Label.Attribute
                    End If

                    Return Label.Ignored

                Case Else
                    isLeaf = True
                    Return Label.Ignored
            End Select
        End Function

        Protected Overrides Function GetLabel(node As SyntaxNode) As Integer
            If _matchingLambdas AndAlso (node Is _newRoot OrElse node Is _oldRoot) Then
                Return Label.LambdaRoot
            End If

            Return GetLabelImpl(node)
        End Function

        Friend Function GetLabelImpl(node As SyntaxNode) As Label
            Dim isLeaf As Boolean
            Return Classify(node.Kind, node, isLeaf, ignoreVariableDeclarations:=False)
        End Function

        '' internal for testing
        Friend Overloads Function HasLabel(kind As SyntaxKind, ignoreVariableDeclarations As Boolean) As Boolean
            Dim isLeaf As Boolean
            Return Classify(kind, Nothing, isLeaf, ignoreVariableDeclarations) <> Label.Ignored
        End Function

        Protected Overrides ReadOnly Property LabelCount As Integer
            Get
                Return Label.Count
            End Get
        End Property

        Protected Overrides Function TiedToAncestor(label As Integer) As Integer
            Return TiedToAncestor(CType(label, Label))
        End Function

#End Region

#Region "Comparisons"

        Public Overrides Function ValuesEqual(left As SyntaxNode, right As SyntaxNode) As Boolean
            Dim ignoreChildFunction As Func(Of SyntaxKind, Boolean)

            Select Case left.Kind()
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.PropertyBlock,
                     SyntaxKind.EventBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    ' When comparing a block containing method body statements we need to not ignore 
                    ' VariableDeclaration, ModifiedIdentifier, and AsClause children.
                    ' But when comparing field definitions we should ignore VariableDeclaration children.
                    ignoreChildFunction = Function(childKind) HasLabel(childKind, ignoreVariableDeclarations:=True)

                Case SyntaxKind.AttributesStatement
                    ' Normally attributes and attribute lists are ignored, but for attribute statements
                    ' we need to include them, so just assume they're labelled
                    ignoreChildFunction = Function(childKind) True

                Case Else
                    If HasChildren(left) Then
                        ignoreChildFunction = Function(childKind) HasLabel(childKind, ignoreVariableDeclarations:=False)
                    Else
                        ignoreChildFunction = Nothing
                    End If
            End Select

            Return SyntaxFactory.AreEquivalent(left, right, ignoreChildFunction)
        End Function

        Protected Overrides Function TryComputeWeightedDistance(leftNode As SyntaxNode, rightNode As SyntaxNode, ByRef distance As Double) As Boolean
            Select Case leftNode.Kind
                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock,
                     SyntaxKind.WhileBlock

                    Dim getParts = Function(node As SyntaxNode)
                                       Select Case node.Kind
                                           Case SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock
                                               Dim block = DirectCast(node, DoLoopBlockSyntax)
                                               Return New With {.Begin = CType(block.LoopStatement, StatementSyntax), block.Statements}

                                           Case SyntaxKind.WhileBlock
                                               Dim block = DirectCast(node, WhileBlockSyntax)
                                               Return New With {.Begin = CType(block.WhileStatement, StatementSyntax), block.Statements}

                                           Case Else
                                               Dim block = DirectCast(node, DoLoopBlockSyntax)
                                               Return New With {.Begin = CType(block.DoStatement, StatementSyntax), block.Statements}
                                       End Select
                                   End Function

                    Dim leftParts = getParts(leftNode)
                    Dim rightParts = getParts(rightNode)

                    distance = ComputeWeightedDistance(leftParts.Begin, leftParts.Statements, rightParts.Begin, rightParts.Statements)
                    Return True

                Case SyntaxKind.ForBlock
                    Dim leftFor = DirectCast(leftNode, ForOrForEachBlockSyntax)
                    Dim rightFor = DirectCast(rightNode, ForOrForEachBlockSyntax)
                    Dim leftStatement = DirectCast(leftFor.ForOrForEachStatement, ForStatementSyntax)
                    Dim rightStatement = DirectCast(rightFor.ForOrForEachStatement, ForStatementSyntax)

                    distance = ComputeWeightedDistance(leftStatement.ControlVariable,
                                                       leftFor.ForOrForEachStatement,
                                                       leftFor.Statements,
                                                       rightStatement.ControlVariable,
                                                       rightFor.ForOrForEachStatement,
                                                       rightFor.Statements)
                    Return True

                Case SyntaxKind.ForEachBlock
                    Dim leftFor = DirectCast(leftNode, ForOrForEachBlockSyntax)
                    Dim rightFor = DirectCast(rightNode, ForOrForEachBlockSyntax)
                    Dim leftStatement = DirectCast(leftFor.ForOrForEachStatement, ForEachStatementSyntax)
                    Dim rightStatement = DirectCast(rightFor.ForOrForEachStatement, ForEachStatementSyntax)

                    distance = ComputeWeightedDistance(leftStatement.ControlVariable,
                                                       leftFor.ForOrForEachStatement,
                                                       leftFor.Statements,
                                                       rightStatement.ControlVariable,
                                                       rightFor.ForOrForEachStatement,
                                                       rightFor.Statements)
                    Return True

                Case SyntaxKind.UsingBlock
                    Dim leftUsing = DirectCast(leftNode, UsingBlockSyntax)
                    Dim rightUsing = DirectCast(rightNode, UsingBlockSyntax)
                    distance = ComputeWeightedDistance(leftUsing.UsingStatement.Expression, leftUsing.Statements, rightUsing.UsingStatement.Expression, rightUsing.Statements)
                    Return True

                Case SyntaxKind.WithBlock
                    Dim leftWith = DirectCast(leftNode, WithBlockSyntax)
                    Dim rightWith = DirectCast(rightNode, WithBlockSyntax)
                    distance = ComputeWeightedDistance(leftWith.WithStatement.Expression, leftWith.Statements, rightWith.WithStatement.Expression, rightWith.Statements)
                    Return True

                Case SyntaxKind.SyncLockBlock
                    Dim leftLock = DirectCast(leftNode, SyncLockBlockSyntax)
                    Dim rightLock = DirectCast(rightNode, SyncLockBlockSyntax)
                    distance = ComputeWeightedDistance(leftLock.SyncLockStatement.Expression, leftLock.Statements, rightLock.SyncLockStatement.Expression, rightLock.Statements)
                    Return True

                Case SyntaxKind.VariableDeclarator
                    ' For top level syntax a variable declarator is seen for field declarations as we need to treat
                    ' them differently to local declarations, which is what is seen in statement syntax
                    If Not _compareStatementSyntax Then
                        Dim leftIdentifiers = DirectCast(leftNode, VariableDeclaratorSyntax).Names.Select(Function(n) n.Identifier)
                        Dim rightIdentifiers = DirectCast(rightNode, VariableDeclaratorSyntax).Names.Select(Function(n) n.Identifier)
                        distance = ComputeDistance(leftIdentifiers, rightIdentifiers)
                        Return True
                    End If

                    distance = ComputeDistance(DirectCast(leftNode, VariableDeclaratorSyntax).Names, DirectCast(rightNode, VariableDeclaratorSyntax).Names)
                    Return True

                Case SyntaxKind.MultiLineIfBlock,
                     SyntaxKind.SingleLineIfStatement

                    Dim ifParts = Function(node As SyntaxNode)
                                      If node.IsKind(SyntaxKind.MultiLineIfBlock) Then
                                          Dim part = DirectCast(node, MultiLineIfBlockSyntax)
                                          Return New With {part.IfStatement.Condition, part.Statements}
                                      Else
                                          Dim part = DirectCast(node, SingleLineIfStatementSyntax)
                                          Return New With {part.Condition, part.Statements}
                                      End If
                                  End Function

                    Dim leftIf = ifParts(leftNode)
                    Dim rightIf = ifParts(rightNode)
                    distance = ComputeWeightedDistance(leftIf.Condition, leftIf.Statements, rightIf.Condition, rightIf.Statements)
                    Return True

                Case SyntaxKind.ElseIfBlock
                    Dim leftElseIf = DirectCast(leftNode, ElseIfBlockSyntax)
                    Dim rightElseIf = DirectCast(rightNode, ElseIfBlockSyntax)
                    distance = ComputeWeightedDistance(leftElseIf.ElseIfStatement.Condition, leftElseIf.Statements, rightElseIf.ElseIfStatement.Condition, rightElseIf.Statements)
                    Return True

                Case SyntaxKind.ElseBlock,
                     SyntaxKind.SingleLineElseClause

                    Dim elseStatements = Function(node As SyntaxNode)
                                             If node.IsKind(SyntaxKind.ElseBlock) Then
                                                 Return DirectCast(node, ElseBlockSyntax).Statements
                                             Else
                                                 Return DirectCast(node, SingleLineElseClauseSyntax).Statements
                                             End If
                                         End Function

                    Dim leftStatements = elseStatements(leftNode)
                    Dim rightStatements = elseStatements(rightNode)
                    distance = ComputeWeightedDistance(Nothing, leftStatements, Nothing, rightStatements)
                    Return True

                Case SyntaxKind.CatchBlock
                    Dim leftCatch = DirectCast(leftNode, CatchBlockSyntax)
                    Dim rightCatch = DirectCast(rightNode, CatchBlockSyntax)
                    distance = ComputeWeightedDistance(leftCatch.CatchStatement, leftCatch.Statements, rightCatch.CatchStatement, rightCatch.Statements)
                    Return True

                Case SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression

                    Dim getParts = Function(node As SyntaxNode)
                                       Select Case node.Kind
                                           Case SyntaxKind.SingleLineSubLambdaExpression,
                                                SyntaxKind.SingleLineFunctionLambdaExpression
                                               Dim lambda = DirectCast(node, SingleLineLambdaExpressionSyntax)
                                               Return New With {lambda.SubOrFunctionHeader, .Body = lambda.Body.DescendantTokens()}

                                           Case Else
                                               Dim lambda = DirectCast(node, MultiLineLambdaExpressionSyntax)
                                               Return New With {lambda.SubOrFunctionHeader, .Body = GetDescendantTokensIgnoringSeparators(lambda.Statements)}
                                       End Select
                                   End Function

                    Dim leftParts = getParts(leftNode)
                    Dim rightParts = getParts(rightNode)
                    distance = ComputeWeightedDistanceOfLambdas(leftParts.SubOrFunctionHeader, rightParts.SubOrFunctionHeader, leftParts.Body, rightParts.Body)
                    Return True
                Case SyntaxKind.VariableDeclarator
                    Dim leftIdentifiers = DirectCast(leftNode, VariableDeclaratorSyntax).Names.Select(Function(n) n.Identifier)
                    Dim rightIdentifiers = DirectCast(rightNode, VariableDeclaratorSyntax).Names.Select(Function(n) n.Identifier)
                    distance = ComputeDistance(leftIdentifiers, rightIdentifiers)
                    Return True

                Case Else
                    Dim leftName As SyntaxNodeOrToken? = TryGetName(leftNode)
                    Dim rightName As SyntaxNodeOrToken? = TryGetName(rightNode)

                    If leftName.HasValue AndAlso rightName.HasValue Then
                        distance = ComputeDistance(leftName.Value, rightName.Value)
                        Return True
                    End If

                    distance = 0
                    Return False
            End Select
        End Function

        Private Shared Function ComputeWeightedDistanceOfLambdas(leftHeader As LambdaHeaderSyntax,
                                                          rightHeader As LambdaHeaderSyntax,
                                                          leftBody As IEnumerable(Of SyntaxToken),
                                                          rightBody As IEnumerable(Of SyntaxToken)) As Double

            If leftHeader.Modifiers.Any(SyntaxKind.AsyncKeyword) <> rightHeader.Modifiers.Any(SyntaxKind.AsyncKeyword) Then
                Return 1.0
            End If

            Dim parameterDistance = ComputeDistance(leftHeader.ParameterList, rightHeader.ParameterList)

            If leftHeader.AsClause IsNot Nothing OrElse rightHeader.AsClause IsNot Nothing Then
                Dim asClauseDistance As Double = ComputeDistance(leftHeader.AsClause, rightHeader.AsClause)
                parameterDistance = parameterDistance * 0.8 + asClauseDistance * 0.2
            End If

            Dim bodyDistance = ComputeDistance(leftBody, rightBody)

            Return parameterDistance * 0.6 + bodyDistance * 0.4
        End Function

        Private Shared Function ComputeWeightedDistance(leftHeader As SyntaxNode,
                                                        leftStatements As SyntaxList(Of StatementSyntax),
                                                        rightHeader As SyntaxNode,
                                                        rightStatements As SyntaxList(Of StatementSyntax)) As Double
            Return ComputeWeightedDistance(Nothing, leftHeader, leftStatements, Nothing, rightHeader, rightStatements)
        End Function

        Private Shared Function ComputeWeightedDistance(leftControlVariable As SyntaxNode,
                                                        leftHeader As SyntaxNode,
                                                        leftStatements As SyntaxList(Of StatementSyntax),
                                                        rightControlVariable As SyntaxNode,
                                                        rightHeader As SyntaxNode,
                                                        rightStatements As SyntaxList(Of StatementSyntax)) As Double

            Debug.Assert((leftControlVariable Is Nothing) = (rightControlVariable Is Nothing))

            Dim headerDistance As Double = ComputeDistance(leftHeader, rightHeader)
            If leftControlVariable IsNot Nothing Then
                Dim controlVariableDistance = ComputeDistance(leftControlVariable, rightControlVariable)
                headerDistance = controlVariableDistance * 0.9 + headerDistance * 0.1
            End If

            Dim statementDistance As Double = ComputeDistance(leftStatements, rightStatements)

            Dim distance As Double = headerDistance * 0.6 + statementDistance * 0.4

            Dim localsDistance As Double
            If TryComputeLocalsDistance(leftStatements, rightStatements, localsDistance) Then
                distance = localsDistance * 0.5 + distance * 0.5
            End If

            Return distance
        End Function

        Private Shared Function TryComputeLocalsDistance(left As SyntaxList(Of StatementSyntax),
                                                         right As SyntaxList(Of StatementSyntax),
                                                         ByRef distance As Double) As Boolean

            Dim leftLocals As List(Of SyntaxToken) = Nothing
            Dim rightLocals As List(Of SyntaxToken) = Nothing

            GetLocalNames(left, leftLocals)
            GetLocalNames(right, rightLocals)

            If leftLocals Is Nothing OrElse rightLocals Is Nothing Then
                distance = 0
                Return False
            End If

            distance = ComputeDistance(leftLocals, rightLocals)
            Return True
        End Function

        Private Shared Sub GetLocalNames(statements As SyntaxList(Of StatementSyntax), ByRef result As List(Of SyntaxToken))
            For Each s In statements
                If s.IsKind(SyntaxKind.LocalDeclarationStatement) Then
                    For Each declarator In DirectCast(s, LocalDeclarationStatementSyntax).Declarators
                        GetLocalNames(declarator, result)
                    Next
                End If
            Next
        End Sub

        Private Shared Sub GetLocalNames(localDecl As VariableDeclaratorSyntax, ByRef result As List(Of SyntaxToken))
            For Each local In localDecl.Names
                If result Is Nothing Then
                    result = New List(Of SyntaxToken)()
                End If

                result.Add(local.Identifier)
            Next
        End Sub

        Private Shared Function TryGetName(node As SyntaxNode) As SyntaxNodeOrToken?
            Select Case node.Kind()
                Case SyntaxKind.OptionStatement
                    Return DirectCast(node, OptionStatementSyntax).OptionKeyword

                Case SyntaxKind.NamespaceBlock
                    Return DirectCast(node, NamespaceBlockSyntax).NamespaceStatement.Name

                Case SyntaxKind.ClassBlock,
                     SyntaxKind.StructureBlock,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.ModuleBlock
                    Return DirectCast(node, TypeBlockSyntax).BlockStatement.Identifier

                Case SyntaxKind.EnumBlock
                    Return DirectCast(node, EnumBlockSyntax).EnumStatement.Identifier

                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(node, DelegateStatementSyntax).Identifier

                Case SyntaxKind.ModifiedIdentifier
                    Return DirectCast(node, ModifiedIdentifierSyntax).Identifier

                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock
                    Return DirectCast(node, MethodBlockSyntax).SubOrFunctionStatement.Identifier

                Case SyntaxKind.SubStatement,     ' interface methods
                     SyntaxKind.FunctionStatement
                    Return DirectCast(node, MethodStatementSyntax).Identifier

                Case SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(node, DeclareStatementSyntax).Identifier

                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(node, ConstructorBlockSyntax).SubNewStatement.NewKeyword

                Case SyntaxKind.OperatorBlock
                    Return DirectCast(node, OperatorBlockSyntax).OperatorStatement.OperatorToken

                Case SyntaxKind.PropertyBlock
                    Return DirectCast(node, PropertyBlockSyntax).PropertyStatement.Identifier

                Case SyntaxKind.PropertyStatement ' interface properties
                    Return DirectCast(node, PropertyStatementSyntax).Identifier

                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).EventStatement.Identifier

                Case SyntaxKind.EnumMemberDeclaration
                    Return DirectCast(node, EnumMemberDeclarationSyntax).Identifier

                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    Return DirectCast(node, AccessorBlockSyntax).BlockStatement.DeclarationKeyword

                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).Identifier

                Case SyntaxKind.TypeParameter
                    Return DirectCast(node, TypeParameterSyntax).Identifier

                Case SyntaxKind.StructureConstraint,
                     SyntaxKind.ClassConstraint,
                     SyntaxKind.NewConstraint
                    Return DirectCast(node, SpecialConstraintSyntax).ConstraintKeyword

                Case SyntaxKind.Parameter
                    Return DirectCast(node, ParameterSyntax).Identifier.Identifier

                Case SyntaxKind.Attribute
                    Return DirectCast(node, AttributeSyntax).Name

                Case Else
                    Return Nothing
            End Select
        End Function

        Public Overrides Function GetDistance(oldNode As SyntaxNode, newNode As SyntaxNode) As Double
            Debug.Assert(GetLabel(oldNode) = GetLabel(newNode) AndAlso GetLabel(oldNode) <> IgnoredNode)

            If oldNode Is newNode Then
                Return ExactMatchDist
            End If

            Dim weightedDistance As Double
            If TryComputeWeightedDistance(oldNode, newNode, weightedDistance) Then
                If weightedDistance = ExactMatchDist AndAlso Not SyntaxFactory.AreEquivalent(oldNode, newNode) Then
                    weightedDistance = EpsilonDist
                End If

                Return weightedDistance
            End If

            Return ComputeValueDistance(oldNode, newNode)
        End Function

        Friend Shared Function ComputeValueDistance(leftNode As SyntaxNode, rightNode As SyntaxNode) As Double
            If SyntaxFactory.AreEquivalent(leftNode, rightNode) Then
                Return ExactMatchDist
            End If

            Dim distance As Double = ComputeDistance(leftNode, rightNode)
            Return If(distance = ExactMatchDist, EpsilonDist, distance)
        End Function

        Friend Overloads Shared Function ComputeDistance(oldNodeOrToken As SyntaxNodeOrToken, newNodeOrToken As SyntaxNodeOrToken) As Double
            Debug.Assert(newNodeOrToken.IsToken = oldNodeOrToken.IsToken)

            Dim distance As Double
            If oldNodeOrToken.IsToken Then
                Dim leftToken = oldNodeOrToken.AsToken()
                Dim rightToken = newNodeOrToken.AsToken()

                distance = ComputeDistance(leftToken, rightToken)
                Debug.Assert(Not SyntaxFactory.AreEquivalent(leftToken, rightToken) OrElse distance = ExactMatchDist)
            Else
                Dim leftNode = oldNodeOrToken.AsNode()
                Dim rightNode = newNodeOrToken.AsNode()

                distance = ComputeDistance(leftNode, rightNode)
                Debug.Assert(Not SyntaxFactory.AreEquivalent(leftNode, rightNode) OrElse distance = ExactMatchDist)
            End If

            Return distance
        End Function

        Friend Overloads Shared Function ComputeDistance(Of TSyntaxNode As SyntaxNode)(oldList As SyntaxList(Of TSyntaxNode), newList As SyntaxList(Of TSyntaxNode)) As Double
            Return ComputeDistance(GetDescendantTokensIgnoringSeparators(oldList), GetDescendantTokensIgnoringSeparators(newList))
        End Function

        Friend Overloads Shared Function ComputeDistance(Of TSyntaxNode As SyntaxNode)(oldList As SeparatedSyntaxList(Of TSyntaxNode), newList As SeparatedSyntaxList(Of TSyntaxNode)) As Double
            Return ComputeDistance(GetDescendantTokensIgnoringSeparators(oldList), GetDescendantTokensIgnoringSeparators(newList))
        End Function

        ''' <summary>
        ''' Enumerates tokens of all nodes in the list.
        ''' </summary>
        Friend Shared Iterator Function GetDescendantTokensIgnoringSeparators(Of TSyntaxNode As SyntaxNode)(list As SyntaxList(Of TSyntaxNode)) As IEnumerable(Of SyntaxToken)
            For Each node In list
                For Each token In node.DescendantTokens()
                    Yield token
                Next
            Next
        End Function

        ''' <summary>
        ''' Enumerates tokens of all nodes in the list. Doesn't include separators.
        ''' </summary>
        Private Shared Iterator Function GetDescendantTokensIgnoringSeparators(Of TSyntaxNode As SyntaxNode)(list As SeparatedSyntaxList(Of TSyntaxNode)) As IEnumerable(Of SyntaxToken)
            For Each node In list
                For Each token In node.DescendantTokens()
                    Yield token
                Next
            Next
        End Function

        ''' <summary>
        ''' Calculates the distance between two syntax nodes, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the nodes are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldNode As SyntaxNode, newNode As SyntaxNode) As Double
            If oldNode Is Nothing OrElse newNode Is Nothing Then
                Return If(oldNode Is newNode, 0.0, 1.0)
            End If

            Return ComputeDistance(oldNode.DescendantTokens(), newNode.DescendantTokens())
        End Function

        ''' <summary>
        ''' Calculates the distance between two syntax tokens, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the tokens are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldToken As SyntaxToken, newToken As SyntaxToken) As Double
            Return LongestCommonSubstring.ComputePrefixDistance(
                oldToken.Text, Math.Min(oldToken.Text.Length, LongestCommonSubsequence.MaxSequenceLengthForDistanceCalculation),
                newToken.Text, Math.Min(newToken.Text.Length, LongestCommonSubsequence.MaxSequenceLengthForDistanceCalculation))
        End Function

        Private Shared Function CreateArrayForDistanceCalculation(Of T)(enumerable As IEnumerable(Of T)) As ImmutableArray(Of T)
            Return If(enumerable Is Nothing, ImmutableArray(Of T).Empty, enumerable.Take(LongestCommonSubsequence.MaxSequenceLengthForDistanceCalculation).ToImmutableArray())
        End Function

        ''' <summary>
        ''' Calculates the distance between two sequences of syntax tokens, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldTokens As IEnumerable(Of SyntaxToken), newTokens As IEnumerable(Of SyntaxToken)) As Double
            Return LcsTokens.Instance.ComputeDistance(CreateArrayForDistanceCalculation(oldTokens), CreateArrayForDistanceCalculation(newTokens))
        End Function

        ''' <summary>
        ''' Calculates the distance between two sequences of syntax nodes, disregarding trivia. 
        ''' </summary>
        ''' <remarks>
        ''' Distance is a number within [0, 1], the smaller the more similar the sequences are. 
        ''' </remarks>
        Public Overloads Shared Function ComputeDistance(oldTokens As IEnumerable(Of SyntaxNode), newTokens As IEnumerable(Of SyntaxNode)) As Double
            Return LcsNodes.Instance.ComputeDistance(CreateArrayForDistanceCalculation(oldTokens), CreateArrayForDistanceCalculation(newTokens))
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        ''' </summary>
        Public Shared Function GetSequenceEdits(oldNodes As IEnumerable(Of SyntaxNode), newNodes As IEnumerable(Of SyntaxNode)) As IEnumerable(Of SequenceEdit)
            Return LcsNodes.Instance.GetEdits(oldNodes.AsImmutableOrEmpty(), newNodes.AsImmutableOrEmpty())
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax nodes to another, disregarding trivia.
        ''' </summary>
        Public Shared Function GetSequenceEdits(oldNodes As ImmutableArray(Of SyntaxNode), newNodes As ImmutableArray(Of SyntaxNode)) As IEnumerable(Of SequenceEdit)
            Return LcsNodes.Instance.GetEdits(oldNodes.NullToEmpty(), newNodes.NullToEmpty())
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        ''' </summary>
        Public Shared Function GetSequenceEdits(oldTokens As IEnumerable(Of SyntaxToken), newTokens As IEnumerable(Of SyntaxToken)) As IEnumerable(Of SequenceEdit)
            Return LcsTokens.Instance.GetEdits(oldTokens.AsImmutableOrEmpty(), newTokens.AsImmutableOrEmpty())
        End Function

        ''' <summary>
        ''' Calculates the edits that transform one sequence of syntax tokens to another, disregarding trivia.
        ''' </summary>
        Public Shared Function GetSequenceEdits(oldTokens As ImmutableArray(Of SyntaxToken), newTokens As ImmutableArray(Of SyntaxToken)) As IEnumerable(Of SequenceEdit)
            Return LcsTokens.Instance.GetEdits(oldTokens.NullToEmpty(), newTokens.NullToEmpty())
        End Function

        Private NotInheritable Class LcsTokens
            Inherits LongestCommonImmutableArraySubsequence(Of SyntaxToken)

            Friend Shared ReadOnly Instance As LcsTokens = New LcsTokens()

            Protected Overrides Function Equals(oldElement As SyntaxToken, newElement As SyntaxToken) As Boolean
                Return SyntaxFactory.AreEquivalent(oldElement, newElement)
            End Function
        End Class

        Private NotInheritable Class LcsNodes
            Inherits LongestCommonImmutableArraySubsequence(Of SyntaxNode)

            Friend Shared ReadOnly Instance As LcsNodes = New LcsNodes()

            Protected Overrides Function Equals(oldElement As SyntaxNode, newElement As SyntaxNode) As Boolean
                Return SyntaxFactory.AreEquivalent(oldElement, newElement)
            End Function
        End Class
#End Region
    End Class
End Namespace
