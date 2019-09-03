' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    'This portion of the binder converts StatementSyntax nodes into BoundStatements

    Partial Friend Class Binder

        ' !!! PLEASE KEEP BindStatement FUNCTION AT THE TOP !!!

        ''' <summary>
        ''' The dispatcher method that handles syntax nodes for all stand-alone statements.
        ''' </summary>
        Public Overridable Function BindStatement(node As StatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Debug.Assert(node IsNot Nothing)
            Select Case node.Kind
                Case SyntaxKind.SimpleAssignmentStatement,
                     SyntaxKind.AddAssignmentStatement,
                     SyntaxKind.SubtractAssignmentStatement,
                     SyntaxKind.MultiplyAssignmentStatement,
                     SyntaxKind.DivideAssignmentStatement,
                     SyntaxKind.IntegerDivideAssignmentStatement,
                     SyntaxKind.ExponentiateAssignmentStatement,
                     SyntaxKind.LeftShiftAssignmentStatement,
                     SyntaxKind.RightShiftAssignmentStatement,
                     SyntaxKind.ConcatenateAssignmentStatement
                    Return BindAssignmentStatement(DirectCast(node, AssignmentStatementSyntax), diagnostics)

                Case SyntaxKind.MidAssignmentStatement
                    Return BindMidAssignmentStatement(DirectCast(node, AssignmentStatementSyntax), diagnostics)

                Case SyntaxKind.AddHandlerStatement,
                    SyntaxKind.RemoveHandlerStatement
                    Return BindAddRemoveHandlerStatement(DirectCast(node, AddRemoveHandlerStatementSyntax), diagnostics)

                Case SyntaxKind.RaiseEventStatement
                    Return BindRaiseEventStatement(DirectCast(node, RaiseEventStatementSyntax), diagnostics)

                Case SyntaxKind.PrintStatement
                    Return BindPrintStatement(DirectCast(node, PrintStatementSyntax), diagnostics)

                Case SyntaxKind.ExpressionStatement
                    Return BindExpressionStatement(DirectCast(node, ExpressionStatementSyntax), diagnostics)

                Case SyntaxKind.CallStatement
                    Return BindCallStatement(DirectCast(node, CallStatementSyntax), diagnostics)

                Case SyntaxKind.GoToStatement
                    Return BindGoToStatement(DirectCast(node, GoToStatementSyntax), diagnostics)

                Case SyntaxKind.LabelStatement
                    Return BindLabelStatement(DirectCast(node, LabelStatementSyntax), diagnostics)

                Case SyntaxKind.SingleLineIfStatement
                    Return BindSingleLineIfStatement(DirectCast(node, SingleLineIfStatementSyntax), diagnostics)

                Case SyntaxKind.MultiLineIfBlock
                    Return BindMultiLineIfBlock(DirectCast(node, MultiLineIfBlockSyntax), diagnostics)

                Case SyntaxKind.ElseIfStatement
                    ' ElseIf without a preceding If.
                    Debug.Assert(IsSemanticModelBinder OrElse node.ContainsDiagnostics)
                    Dim condition = BindBooleanExpression(DirectCast(node, ElseIfStatementSyntax).Condition, diagnostics)
                    Return New BoundBadStatement(node, ImmutableArray.Create(Of BoundNode)(condition), hasErrors:=True)

                Case SyntaxKind.SelectBlock
                    Return BindSelectBlock(DirectCast(node, SelectBlockSyntax), diagnostics)

                Case SyntaxKind.CaseStatement
                    Return BindStandAloneCaseStatement(DirectCast(node, CaseStatementSyntax), diagnostics)

                Case SyntaxKind.LocalDeclarationStatement
                    Return BindLocalDeclaration(DirectCast(node, LocalDeclarationStatementSyntax), diagnostics)

                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock
                    Return BindDoLoop(DirectCast(node, DoLoopBlockSyntax), diagnostics)

                Case SyntaxKind.WhileBlock
                    Return BindWhileBlock(DirectCast(node, WhileBlockSyntax), diagnostics)

                Case SyntaxKind.ForBlock
                    Return BindForToBlock(DirectCast(node, ForOrForEachBlockSyntax), diagnostics)

                Case SyntaxKind.ForEachBlock
                    Return BindForEachBlock(DirectCast(node, ForOrForEachBlockSyntax), diagnostics)

                Case SyntaxKind.WithBlock
                    Return BindWithBlock(DirectCast(node, WithBlockSyntax), diagnostics)

                Case SyntaxKind.UsingBlock
                    Return BindUsingBlock(DirectCast(node, UsingBlockSyntax), diagnostics)

                Case SyntaxKind.SyncLockBlock
                    Return BindSyncLockBlock(DirectCast(node, SyncLockBlockSyntax), diagnostics)

                Case SyntaxKind.TryBlock
                    Return BindTryBlock(DirectCast(node, TryBlockSyntax), diagnostics)

                Case SyntaxKind.ExitDoStatement,
                    SyntaxKind.ExitForStatement,
                    SyntaxKind.ExitSelectStatement,
                    SyntaxKind.ExitTryStatement,
                    SyntaxKind.ExitWhileStatement,
                    SyntaxKind.ExitFunctionStatement,
                    SyntaxKind.ExitSubStatement,
                    SyntaxKind.ExitPropertyStatement
                    Return BindExitStatement(DirectCast(node, ExitStatementSyntax), diagnostics)

                Case SyntaxKind.ContinueDoStatement, SyntaxKind.ContinueForStatement, SyntaxKind.ContinueWhileStatement
                    Return BindContinueStatement(DirectCast(node, ContinueStatementSyntax), diagnostics)

                Case SyntaxKind.ReturnStatement
                    Return BindReturn(DirectCast(node, ReturnStatementSyntax), diagnostics)

                Case SyntaxKind.YieldStatement
                    Return BindYield(DirectCast(node, YieldStatementSyntax), diagnostics)

                Case SyntaxKind.ThrowStatement
                    Return BindThrow(DirectCast(node, ThrowStatementSyntax), diagnostics)

                Case SyntaxKind.ErrorStatement
                    Return BindError(DirectCast(node, ErrorStatementSyntax), diagnostics)

                Case SyntaxKind.EmptyStatement
                    Return New BoundNoOpStatement(node)

                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock,
                     SyntaxKind.OperatorBlock
                    Return BindMethodBlock(DirectCast(node, MethodBlockBaseSyntax), diagnostics)

                Case SyntaxKind.ReDimStatement, SyntaxKind.ReDimPreserveStatement
                    Return BindRedimStatement(DirectCast(node, ReDimStatementSyntax), diagnostics)

                Case SyntaxKind.EraseStatement
                    Return BindEraseStatement(DirectCast(node, EraseStatementSyntax), diagnostics)

                Case SyntaxKind.NextStatement,
                     SyntaxKind.EndIfStatement,
                     SyntaxKind.EndSelectStatement,
                     SyntaxKind.EndTryStatement,
                     SyntaxKind.EndUsingStatement,
                     SyntaxKind.EndWhileStatement,
                     SyntaxKind.EndWithStatement,
                     SyntaxKind.EndSyncLockStatement,
                     SyntaxKind.EndNamespaceStatement,
                     SyntaxKind.EndModuleStatement,
                     SyntaxKind.EndClassStatement,
                     SyntaxKind.EndStructureStatement,
                     SyntaxKind.EndInterfaceStatement,
                     SyntaxKind.EndEnumStatement,
                     SyntaxKind.EndSubStatement,
                     SyntaxKind.EndFunctionStatement,
                     SyntaxKind.EndOperatorStatement,
                     SyntaxKind.EndPropertyStatement,
                     SyntaxKind.EndGetStatement,
                     SyntaxKind.EndSetStatement,
                     SyntaxKind.EndEventStatement,
                     SyntaxKind.EndAddHandlerStatement,
                     SyntaxKind.EndRemoveHandlerStatement,
                     SyntaxKind.EndRaiseEventStatement,
                     SyntaxKind.FinallyStatement,
                     SyntaxKind.IncompleteMember

                    ' This can happen for two reasons:
                    '  1. if there are more end block statements than block statements in source
                    '  2. we have nested blocks where the inner one contains a statement that closes a block above the
                    '     nested block (e.g. two nested SyncLocks + end sub in the inner one). Then there will be parser
                    '     generated "End XXX" statements which are only marked as "missing", because the diagnostic will be
                    '     attached to the begin of the block. Both of these missing end statements have a 0 width which causes
                    '     them to be in the same region of a textspan. In that situation the inner end statement may be bound 
                    '     separately (outside of the block context) and we need to relax the assertion below to allow missing 
                    '     end statements as well (the list of statements was taken from the parser method CreateMissingEnd, 
                    '     where only the ones that can appear in a method body have been selected).
                    '
                    '   We simply need to ignore this, the error is already created by the parser.
                    Debug.Assert(IsSemanticModelBinder OrElse node.ContainsDiagnostics OrElse
                                 (node.IsMissing AndAlso
                                  (node.Parent.Kind = SyntaxKind.MultiLineSubLambdaExpression OrElse
                                   node.Parent.Kind = SyntaxKind.MultiLineFunctionLambdaExpression OrElse
                                   node.Parent.Kind = SyntaxKind.AddHandlerAccessorBlock OrElse
                                   node.Parent.Kind = SyntaxKind.RemoveHandlerAccessorBlock OrElse
                                   node.Parent.Kind = SyntaxKind.RaiseEventAccessorBlock OrElse
                                   node.Parent.Kind = SyntaxKind.MultiLineIfBlock OrElse
                                   node.Parent.Kind = SyntaxKind.ElseIfBlock OrElse
                                   node.Parent.Kind = SyntaxKind.ElseBlock OrElse
                                   node.Parent.Kind = SyntaxKind.SimpleDoLoopBlock OrElse
                                   node.Parent.Kind = SyntaxKind.DoWhileLoopBlock OrElse
                                   node.Parent.Kind = SyntaxKind.DoUntilLoopBlock OrElse
                                   node.Parent.Kind = SyntaxKind.WhileBlock OrElse
                                   node.Parent.Kind = SyntaxKind.WithBlock OrElse
                                   node.Parent.Kind = SyntaxKind.ForBlock OrElse
                                   node.Parent.Kind = SyntaxKind.ForEachBlock OrElse
                                   node.Parent.Kind = SyntaxKind.SyncLockBlock OrElse
                                   node.Parent.Kind = SyntaxKind.SelectBlock OrElse
                                   node.Parent.Kind = SyntaxKind.TryBlock OrElse
                                   node.Parent.Kind = SyntaxKind.UsingBlock)))

                    Return New BoundBadStatement(node, ImmutableArray(Of BoundNode).Empty, hasErrors:=True)

                Case SyntaxKind.SimpleLoopStatement,
                     SyntaxKind.LoopWhileStatement,
                     SyntaxKind.LoopUntilStatement

                    ' a loop statement is legal as long as it is part of a do loop block
                    If Not SyntaxFacts.IsDoLoopBlock(node.Parent.Kind) Then
                        Debug.Assert(node.ContainsDiagnostics)
                        Dim whileOrUntilClause = DirectCast(node, LoopStatementSyntax).WhileOrUntilClause
                        Dim childNodes = If(whileOrUntilClause Is Nothing,
                            ImmutableArray(Of BoundNode).Empty,
                            ImmutableArray.Create(Of BoundNode)(BindBooleanExpression(whileOrUntilClause.Condition, diagnostics)))
                        Return New BoundBadStatement(node, childNodes, hasErrors:=True)
                    End If

                Case SyntaxKind.CatchStatement
                    ' a catch statement is legal as long as it is part of a catch block
                    If Not node.Parent.Kind = SyntaxKind.CatchBlock Then
                        Debug.Assert(node.ContainsDiagnostics)
                        Dim whenClause = DirectCast(node, CatchStatementSyntax).WhenClause
                        Dim childNodes = If(whenClause Is Nothing,
                            ImmutableArray(Of BoundNode).Empty,
                            ImmutableArray.Create(Of BoundNode)(BindBooleanExpression(whenClause.Filter, diagnostics)))
                        Return New BoundBadStatement(node, childNodes, hasErrors:=True)
                    End If

                Case SyntaxKind.ResumeStatement, SyntaxKind.ResumeNextStatement, SyntaxKind.ResumeLabelStatement
                    Return BindResumeStatement(DirectCast(node, ResumeStatementSyntax), diagnostics)

                Case SyntaxKind.OnErrorGoToZeroStatement, SyntaxKind.OnErrorGoToMinusOneStatement,
                     SyntaxKind.OnErrorGoToLabelStatement, SyntaxKind.OnErrorResumeNextStatement
                    Return BindOnErrorStatement(node, diagnostics)

                Case SyntaxKind.StopStatement
                    Return BindStopStatement(DirectCast(node, StopOrEndStatementSyntax))

                Case SyntaxKind.EndStatement
                    Return BindEndStatement(DirectCast(node, StopOrEndStatementSyntax), diagnostics)

            End Select

            ' NOTE: Our normal pattern would be to add cases for all of the SyntaxKinds that we know we're
            ' not handling here and then throwing ExceptionUtilities.UnexpectedValue in the else case, but
            ' there are just too many statement SyntaxKinds in VB (e.g. declarations, statements corresponding
            ' to blocks handled above, etc).
            Debug.Assert(IsSemanticModelBinder OrElse node.ContainsDiagnostics)
            Return New BoundBadStatement(node, ImmutableArray(Of BoundNode).Empty, hasErrors:=True)
        End Function

        Private Function BindStandAloneCaseStatement(caseStatement As CaseStatementSyntax, diagnostics As DiagnosticBag) As BoundBadStatement
            ' Valid Case statement within Select Case statement is handled in BindSelectBlock.
            ' We should reach here only for invalid Case statements which are not inside any SelectBlock.
            ' Parser must have already reported error ERRID.ERR_CaseNoSelect or ERRID.ERR_SubRequiresSingleStatement.
            Debug.Assert(caseStatement.ContainsDiagnostics)
            Dim statement As BoundCaseStatement = BindCaseStatement(caseStatement, selectExpressionOpt:=Nothing, convertCaseElements:=False, diagnostics:=diagnostics)
            Dim children = ArrayBuilder(Of BoundNode).GetInstance(statement.CaseClauses.Length)

            For Each clause As BoundCaseClause In statement.CaseClauses
                Select Case clause.Kind
                    Case BoundKind.SimpleCaseClause, BoundKind.RelationalCaseClause
                        children.Add(DirectCast(clause, BoundSingleValueCaseClause).ValueOpt)
                    Case BoundKind.RangeCaseClause
                        Dim range = DirectCast(clause, BoundRangeCaseClause)
                        children.Add(range.LowerBoundOpt)
                        children.Add(range.UpperBoundOpt)
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(clause.Kind)
                End Select
            Next

            Return New BoundBadStatement(caseStatement, children.ToImmutableAndFree(), hasErrors:=True)
        End Function

        Private Function BindMethodBlock(methodBlock As MethodBlockBaseSyntax, diagnostics As DiagnosticBag) As BoundBlock
            Dim statements As ArrayBuilder(Of BoundStatement) = ArrayBuilder(Of BoundStatement).GetInstance
            Dim locals As ImmutableArray(Of LocalSymbol) = ImmutableArray(Of LocalSymbol).Empty

            Dim methodSymbol = DirectCast(ContainingMember, MethodSymbol)
            Dim localForFunctionValue As LocalSymbol

            If methodSymbol.IsIterator OrElse (methodSymbol.IsAsync AndAlso methodSymbol.ReturnType.Equals(Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task))) Then
                ' We are actually not using FunctionValue of such method in Return statements and referencing it explicitly is an error. 
                localForFunctionValue = Nothing
            Else
                localForFunctionValue = Me.GetLocalForFunctionValue()
            End If

            If localForFunctionValue IsNot Nothing Then
                ' Declare local variable for function return 
                Dim localDeclaration = New BoundLocalDeclaration(methodBlock.BlockStatement,
                                                                 localForFunctionValue,
                                                                 Nothing)
                localDeclaration.SetWasCompilerGenerated()
                statements.Add(localDeclaration)
            End If

            Dim blockBinder = Me.GetBinder(DirectCast(methodBlock, VisualBasicSyntaxNode))
            Dim body = blockBinder.BindBlock(methodBlock, methodBlock.Statements, diagnostics)

            ' Implicit label to branch to for Exit Sub/Exit Function statements.
            Dim exitLabelStatement = New BoundLabelStatement(methodBlock.EndBlockStatement, blockBinder.GetReturnLabel())

            If body IsNot Nothing Then
                ' See if we have to generate OnError handler
                Dim containsAwait As Boolean
                Dim containsOnError As Boolean ' The block contains an [On Error] statement.
                Dim containsResume As Boolean ' The block contains a [Resume [...]] or an [On Error Resume Next] statement.
                Dim resumeWithoutLabel As StatementSyntax = Nothing  ' The first [Resume], [Resume Next] or [On Error Resume Next] statement, if any.
                Dim containsLineNumberLabel As Boolean
                Dim containsCatch As Boolean
                Dim reportedAnError As Boolean

                CheckOnErrorAndAwaitWalker.VisitBlock(blockBinder, body, diagnostics,
                                                      containsAwait, containsOnError, containsResume, resumeWithoutLabel,
                                                      containsLineNumberLabel, containsCatch,
                                                      reportedAnError)

                If blockBinder.IsInAsyncContext() AndAlso Not blockBinder.IsInIteratorContext() AndAlso
                   Not containsAwait AndAlso Not body.HasErrors AndAlso
                   TypeOf methodBlock.BlockStatement Is MethodStatementSyntax Then
                    ReportDiagnostic(diagnostics, DirectCast(methodBlock.BlockStatement, MethodStatementSyntax).Identifier, ERRID.WRN_AsyncLacksAwaits)
                End If

                If Not reportedAnError AndAlso
                   (containsOnError OrElse containsResume OrElse (containsCatch AndAlso containsLineNumberLabel)) Then
                    ' This method uses Unstructured Exception-Handling or needs to track line number

                    ' Note that in constructors this handler does not extend over the call to New at the beginning
                    ' of the constructor.
                    If methodSymbol.MethodKind = MethodKind.Constructor Then
                        Dim hasMyBaseConstructorCall As Boolean = False

                        If InitializerRewriter.HasExplicitMeConstructorCall(body, ContainingMember.ContainingType, hasMyBaseConstructorCall) OrElse hasMyBaseConstructorCall Then
                            ' Move the explicit constructor call out of the block
                            statements.Add(body.Statements(0))
                            body = body.Update(body.StatementListSyntax, body.Locals, body.Statements.RemoveAt(0))
                        End If
                    End If

                    ' The implicit exitLabelStatement should be the last statement inside BoundUnstructuredExceptionHandlingStatement
                    ' in order to make sure that explicit returns do not bypass a call to Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError.
                    body = body.Update(body.StatementListSyntax, body.Locals, body.Statements.Add(exitLabelStatement))

                    statements.Add(New BoundUnstructuredExceptionHandlingStatement(methodBlock,
                                                                                   containsOnError,
                                                                                   containsResume,
                                                                                   resumeWithoutLabel,
                                                                                   containsLineNumberLabel,
                                                                                   body.MakeCompilerGenerated()).MakeCompilerGenerated())
                Else
                    locals = body.Locals
                    statements.AddRange(body.Statements)
                    statements.Add(exitLabelStatement)
                End If

                ' Don't allow any further declaration of implicit variables (by speculative binding, say).
                DisallowFurtherImplicitVariableDeclaration(diagnostics)

                ' Add implicitly declared variables, if any.
                Dim implicitLocals = Me.ImplicitlyDeclaredVariables
                If implicitLocals.Length > 0 Then
                    If locals.IsEmpty Then
                        locals = implicitLocals
                    Else
                        locals = implicitLocals.Concat(locals)
                    End If
                End If

                ' Report conflicts between Static variables.
                ReportNameConfictsBetweenStaticLocals(blockBinder, diagnostics)
            Else
                statements.Add(exitLabelStatement)
            End If

            ' Add a Return statement at the end of the function, with a label to branch to for Exit Sub/Exit Function statements.
            ' The code rewriter turns all returns inside the method body to a jump to the exit label.  These returns are the only
            ' ones that will become real returns in the method body.

            ' add indirect return sequence
            ' and maybe an indirect result local (if this is a function)
            If localForFunctionValue IsNot Nothing Then
                If locals.IsEmpty Then
                    locals = ImmutableArray.Create(localForFunctionValue)
                Else
                    Dim localBuilder = ArrayBuilder(Of LocalSymbol).GetInstance()
                    localBuilder.Add(localForFunctionValue)
                    localBuilder.AddRange(locals)
                    locals = localBuilder.ToImmutableAndFree()
                End If

                statements.Add(New BoundReturnStatement(methodBlock.EndBlockStatement,
                                                        New BoundLocal(methodBlock.EndBlockStatement, localForFunctionValue, isLValue:=False, type:=localForFunctionValue.Type).MakeCompilerGenerated(),
                                                        Nothing, Nothing))
            Else
                statements.Add(New BoundReturnStatement(methodBlock.EndBlockStatement, Nothing, Nothing, Nothing))
            End If

            Return New BoundBlock(methodBlock, If(methodBlock IsNot Nothing, methodBlock.Statements, Nothing), locals, statements.ToImmutableAndFree())
        End Function

        ''' <summary>
        ''' Check presence of [On Error]/[Resume] statements and report diagnostics based on presence of other
        ''' "incompatible" statements.
        ''' Report Async/Await diagnostics, which depends on surrounding context.
        ''' </summary>
        Private Class CheckOnErrorAndAwaitWalker
            Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

            Private ReadOnly _binder As Binder
            Private ReadOnly _diagnostics As DiagnosticBag
            Private _containsOnError As Boolean ' The block contains an [On Error] statement. 
            Private _containsTry As Boolean ' The block contains a Try block.
            Private _containsResume As Boolean ' The block contains a [Resume [...]] or an [On Error Resume Next] statement. And this is a syntax node for the first of them.
            Private _resumeWithoutLabel As StatementSyntax ' The first [Resume], [Resume Next] or [On Error Resume Next] statement, if any.
            Private _containsLineNumberLabel As Boolean
            Private _containsCatch As Boolean
            Private _reportedAnError As Boolean
            Private _enclosingSyncLockOrUsing As BoundStatement
            Private _isInCatchFinallyOrSyncLock As Boolean
            Private _containsAwait As Boolean
            Private ReadOnly _tryOnErrorResume As New ArrayBuilder(Of BoundStatement)

            Private Sub New(binder As Binder, diagnostics As DiagnosticBag)
                _diagnostics = diagnostics
                _binder = binder
            End Sub

            Public Shared Shadows Sub VisitBlock(
                binder As Binder,
                block As BoundBlock,
                diagnostics As DiagnosticBag,
                <Out> ByRef containsAwait As Boolean,
                <Out> ByRef containsOnError As Boolean,
                <Out> ByRef containsResume As Boolean,
                <Out> ByRef resumeWithoutLabel As StatementSyntax,
                <Out> ByRef containsLineNumberLabel As Boolean,
                <Out> ByRef containsCatch As Boolean,
                <Out> ByRef reportedAnError As Boolean
            )
                Dim walker As New CheckOnErrorAndAwaitWalker(binder, diagnostics)

                Try
                    walker.Visit(block)
                    Debug.Assert(walker._enclosingSyncLockOrUsing Is Nothing)
                    Debug.Assert(Not walker._isInCatchFinallyOrSyncLock)
                Catch ex As CancelledByStackGuardException
                    ex.AddAnError(diagnostics)
                    reportedAnError = True
                End Try

                containsAwait = walker._containsAwait
                containsOnError = walker._containsOnError
                containsResume = walker._containsResume
                reportedAnError = walker._reportedAnError
                resumeWithoutLabel = walker._resumeWithoutLabel
                containsLineNumberLabel = walker._containsLineNumberLabel
                containsCatch = walker._containsCatch

                If (containsOnError OrElse containsResume) AndAlso walker._containsTry Then
                    For Each node In walker._tryOnErrorResume
                        binder.ReportDiagnostic(diagnostics, node.Syntax, ERRID.ERR_TryAndOnErrorDoNotMix)
                    Next

                    reportedAnError = True
                End If
            End Sub

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                ' Do not dive into expressions, unless we are in Async context
                If Not _binder.IsInAsyncContext() AndAlso TypeOf node Is BoundExpression Then
                    Return Nothing
                End If

                Return MyBase.Visit(node)
            End Function

            Public Overrides Function VisitTryStatement(node As BoundTryStatement) As BoundNode
                Debug.Assert(Not node.WasCompilerGenerated)

                _containsTry = True
                _tryOnErrorResume.Add(node)

                Visit(node.TryBlock)

                Dim save_m_isInCatchFinallyOrSyncLock As Boolean = _isInCatchFinallyOrSyncLock
                _isInCatchFinallyOrSyncLock = True

                VisitList(node.CatchBlocks)
                Visit(node.FinallyBlockOpt)

                _isInCatchFinallyOrSyncLock = save_m_isInCatchFinallyOrSyncLock
                Return Nothing
            End Function

            Public Overrides Function VisitOnErrorStatement(node As BoundOnErrorStatement) As BoundNode
                Debug.Assert(Not node.WasCompilerGenerated)
                _containsOnError = True
                _tryOnErrorResume.Add(node)

                If node.OnErrorKind = OnErrorStatementKind.ResumeNext Then
                    _containsResume = True

                    If _resumeWithoutLabel Is Nothing Then
                        _resumeWithoutLabel = DirectCast(node.Syntax, StatementSyntax)
                    End If
                End If

                If _enclosingSyncLockOrUsing IsNot Nothing Then
                    ReportDiagnostic(_diagnostics, node.Syntax,
                                     If(_enclosingSyncLockOrUsing.Kind = BoundKind.UsingStatement,
                                        ERRID.ERR_OnErrorInUsing,
                                        ERRID.ERR_OnErrorInSyncLock))
                    _reportedAnError = True
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitResumeStatement(node As BoundResumeStatement) As BoundNode
                Debug.Assert(Not node.WasCompilerGenerated)
                _containsResume = True

                If node.ResumeKind <> ResumeStatementKind.Label AndAlso _resumeWithoutLabel Is Nothing Then
                    _resumeWithoutLabel = DirectCast(node.Syntax, StatementSyntax)
                End If

                _tryOnErrorResume.Add(node)
                Return Nothing
            End Function

            Public Overrides Function VisitSyncLockStatement(node As BoundSyncLockStatement) As BoundNode
                Debug.Assert(Not node.WasCompilerGenerated)
                Dim save = _enclosingSyncLockOrUsing
                Dim save_m_isInCatchFinallyOrSyncLock As Boolean = _isInCatchFinallyOrSyncLock
                _enclosingSyncLockOrUsing = node
                _isInCatchFinallyOrSyncLock = True

                MyBase.VisitSyncLockStatement(node)

                _enclosingSyncLockOrUsing = save
                _isInCatchFinallyOrSyncLock = save_m_isInCatchFinallyOrSyncLock
                Return Nothing
            End Function

            Public Overrides Function VisitUsingStatement(node As BoundUsingStatement) As BoundNode
                Debug.Assert(Not node.WasCompilerGenerated)
                Dim save = _enclosingSyncLockOrUsing
                _enclosingSyncLockOrUsing = node
                MyBase.VisitUsingStatement(node)
                _enclosingSyncLockOrUsing = save
                Return Nothing
            End Function

            Public Overrides Function VisitAwaitOperator(node As BoundAwaitOperator) As BoundNode
                Debug.Assert(_binder.IsInAsyncContext())

                _containsAwait = True

                If _isInCatchFinallyOrSyncLock Then
                    ReportDiagnostic(_diagnostics, node.Syntax, ERRID.ERR_BadAwaitInTryHandler)
                    _reportedAnError = True
                End If

                Return MyBase.VisitAwaitOperator(node)
            End Function

            Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
                Debug.Assert(_binder.IsInAsyncContext())

                ' Do not dive into the lambdas.
                Return Nothing
            End Function

            Public Overrides Function VisitCatchBlock(node As BoundCatchBlock) As BoundNode
                _containsCatch = True
                Return MyBase.VisitCatchBlock(node)
            End Function

            Public Overrides Function VisitLabelStatement(node As BoundLabelStatement) As BoundNode
                If Not node.WasCompilerGenerated AndAlso node.Syntax.Kind = SyntaxKind.LabelStatement AndAlso
                   DirectCast(node.Syntax, LabelStatementSyntax).LabelToken.Kind = SyntaxKind.IntegerLiteralToken Then
                    _containsLineNumberLabel = True
                End If

                Return MyBase.VisitLabelStatement(node)
            End Function
        End Class

        Private Shared Sub ReportNameConfictsBetweenStaticLocals(methodBlockBinder As Binder, diagnostics As DiagnosticBag)
            Dim currentBinder As Binder = methodBlockBinder
            Dim bodyBinder As MethodBodyBinder

            Do
                bodyBinder = TryCast(currentBinder, MethodBodyBinder)

                If bodyBinder IsNot Nothing Then
                    Exit Do
                End If

                currentBinder = currentBinder.ContainingBinder
            Loop While currentBinder IsNot Nothing

            Debug.Assert(bodyBinder IsNot Nothing)

            If bodyBinder IsNot Nothing Then
                Dim staticLocals As Dictionary(Of String, ArrayBuilder(Of LocalSymbol)) = Nothing

                For Each binder As BlockBaseBinder In bodyBinder.StmtListToBinderMap.Values
                    For Each local In binder.Locals
                        If local.IsStatic Then
                            Dim array As ArrayBuilder(Of LocalSymbol) = Nothing

                            If staticLocals Is Nothing Then
                                staticLocals = New Dictionary(Of String, ArrayBuilder(Of LocalSymbol))(CaseInsensitiveComparison.Comparer)
                                array = New ArrayBuilder(Of LocalSymbol)()
                                staticLocals.Add(local.Name, array)
                            ElseIf Not staticLocals.TryGetValue(local.Name, array) Then
                                array = New ArrayBuilder(Of LocalSymbol)()
                                staticLocals.Add(local.Name, array)
                            End If

                            array.Add(local)
                        End If
                    Next
                Next

                If staticLocals IsNot Nothing Then
                    For Each nameToArray In staticLocals
                        Dim array = nameToArray.Value
                        If array.Count > 1 Then
                            Dim lexicallyFirst As LocalSymbol = array(0)

                            For i As Integer = 1 To array.Count - 1
                                If lexicallyFirst.IdentifierToken.Position > array(i).IdentifierToken.Position Then
                                    lexicallyFirst = array(i)
                                End If
                            Next

                            For Each local In array
                                If lexicallyFirst IsNot local Then
                                    ReportDiagnostic(diagnostics, local.IdentifierToken, ERRID.ERR_DuplicateLocalStatic1, local.Name)
                                End If
                            Next
                        End If
                    Next
                End If
            End If
        End Sub

        ''' <summary> Defines max allowed rank of the array </summary>
        ''' <remarks> Currently set to 32 because of COM+ array type limits </remarks>
        Public Const ArrayRankLimit = 32

        Private Function BindRedimStatement(node As ReDimStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim operands = ArrayBuilder(Of BoundRedimClause).GetInstance()
            Dim hasPreserveClause = node.Kind = SyntaxKind.ReDimPreserveStatement

            For Each redimOperand As RedimClauseSyntax In node.Clauses

                Dim redimClauseHasErrors As Boolean = False

                '  bind operand expression as an assignment target
                Dim redimTarget = BindAssignmentTarget(redimOperand.Expression, diagnostics)
                If redimTarget.HasErrors Then
                    redimClauseHasErrors = True
                End If

                '  check for validity of an assignment target, report diagnostics, discard the result
                AdjustAssignmentTarget(redimOperand.Expression, redimTarget, diagnostics, redimClauseHasErrors)

                '  in case it is a Redim Preserve we will make an r-value of it in rewrite phase;
                '      in initial binding we report diagnostics, but throw away the result
                If Not redimClauseHasErrors AndAlso hasPreserveClause Then
                    Dim temp = MakeRValue(redimTarget, diagnostics)
                    If temp.HasErrors Then
                        redimClauseHasErrors = True
                    End If
                End If

                '  bind arguments/indices
                Dim boundIndices As ImmutableArray(Of BoundExpression) = ImmutableArray(Of BoundExpression).Empty
                If redimOperand.ArrayBounds IsNot Nothing Then
                    boundIndices = BindArrayBounds(redimOperand.ArrayBounds, diagnostics, errorOnEmptyBound:=True)
                    For Each arg In boundIndices
                        If arg.HasErrors Then
                            redimClauseHasErrors = True
                        End If
                    Next
                End If

                '  check for the resulting type of the redim target expression: it should be either an array or an object
                Dim arrayType As ArrayTypeSymbol = Nothing
                If Not redimClauseHasErrors Then
                    Dim redimTargetType = redimTarget.Type
                    Debug.Assert(redimTargetType IsNot Nothing)
                    If redimTargetType.IsArrayType Then
                        arrayType = DirectCast(redimTargetType, ArrayTypeSymbol)
                    ElseIf redimTargetType.IsObjectType() Then
                        If boundIndices.Length > 0 Then '  missing redim size error will be reported later
                            arrayType = ArrayTypeSymbol.CreateVBArray(redimTargetType, Nothing, boundIndices.Length, Compilation)
                        End If
                    Else
                        ReportDiagnostic(diagnostics, redimOperand.Expression, ERRID.ERR_ExpectedArray1, "Redim")
                        redimClauseHasErrors = True
                    End If
                End If

                '  check redim array rank 
                If Not redimClauseHasErrors Then
                    If boundIndices.Length = 0 Then
                        ' redim rank cannot be 0
                        ReportDiagnostic(diagnostics, redimOperand, ERRID.ERR_RedimNoSizes)
                        redimClauseHasErrors = True

                    Else
                        '  otherwise the number of indices should match the array rank
                        If arrayType.Rank <> boundIndices.Length Then
                            ReportDiagnostic(diagnostics, redimOperand, ERRID.ERR_RedimRankMismatch)
                            redimClauseHasErrors = True
                        End If
                    End If
                End If

                Debug.Assert(redimClauseHasErrors OrElse arrayType IsNot Nothing)

                '  check for an array rank limit value
                If Not redimClauseHasErrors AndAlso boundIndices.Length > ArrayRankLimit Then
                    ReportDiagnostic(diagnostics, redimOperand, ERRID.ERR_ArrayRankLimit)
                    redimClauseHasErrors = True
                End If

                operands.Add(
                    New BoundRedimClause(
                        redimOperand,
                        redimTarget,
                        boundIndices,
                        arrayType,
                        hasPreserveClause,
                        redimClauseHasErrors)
                    )
            Next

            '  done with all clauses, build a bound redim statement 
            Return New BoundRedimStatement(node, operands.ToImmutableAndFree())
        End Function

        Private Function BindEraseStatement(node As EraseStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim clauses = ArrayBuilder(Of BoundAssignmentOperator).GetInstance()

            For Each operand As ExpressionSyntax In node.Expressions
                Dim target As BoundExpression = BindAssignmentTarget(operand, diagnostics)
                Debug.Assert(target IsNot Nothing)

                Dim nothingLiteral = New BoundLiteral(operand, ConstantValue.Nothing, Nothing).MakeCompilerGenerated()
                Dim clause As BoundAssignmentOperator

                If target.HasErrors Then
                    clause = New BoundAssignmentOperator(operand, target, nothingLiteral, False, target.Type, hasErrors:=True).MakeCompilerGenerated()

                ElseIf Not target.Type.IsErrorType() AndAlso
                       Not target.Type.IsArrayType() AndAlso
                       target.Type.SpecialType <> SpecialType.System_Array AndAlso
                       target.Type.SpecialType <> SpecialType.System_Object Then
                    ReportDiagnostic(diagnostics, operand, ERRID.ERR_ExpectedArray1, "Erase")

                    clause = New BoundAssignmentOperator(operand, target, nothingLiteral, False, target.Type, hasErrors:=True).MakeCompilerGenerated()

                Else
                    clause = BindAssignment(operand, target,
                                            ApplyImplicitConversion(operand, target.Type, nothingLiteral, diagnostics).MakeCompilerGenerated(),
                                            diagnostics).MakeCompilerGenerated()
                End If

                clauses.Add(clause)
            Next

            Return New BoundEraseStatement(node, clauses.ToImmutableAndFree())
        End Function

        Private Function BindGoToStatement(node As GoToStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim symbol As LabelSymbol = Nothing

            Dim boundLabelExpression As BoundExpression = BindExpression(node.Label, diagnostics)

            If boundLabelExpression.Kind = BoundKind.Label Then
                Dim boundLabel = DirectCast(boundLabelExpression, boundLabel)
                symbol = boundLabel.Label

                Dim hasErrors As Boolean = boundLabel.HasErrors

                ' Found label now verify that it is OK to jump to the location.
                hasErrors = hasErrors OrElse
                            Not IsValidLabelForGoto(symbol, node.Label, diagnostics)

                Return New BoundGotoStatement(node, symbol, boundLabel, hasErrors:=hasErrors)
            Else
                ' if the bound label is e.g. a bad bound expression because of a non-existent label, 
                ' make this a bad statement.
                Return New BoundBadStatement(node, ImmutableArray.Create(Of BoundNode)(boundLabelExpression), hasErrors:=True)
            End If
        End Function

        Private Function IsValidLabelForGoto(label As LabelSymbol, labelSyntax As LabelSyntax, diagnostics As DiagnosticBag) As Boolean
            Dim hasError As Boolean = False

            Dim labelParent = DirectCast(label.LabelName.Parent, VisualBasicSyntaxNode)

            ' Determine if the reference is a branch that crosses
            ' into a Try/Catch/Finally or With statement.

            ' once a method or lambda block is found we can stop searching
            Dim errorID = ERRID.ERR_None
            While labelParent IsNot Nothing

                Select Case labelParent.Kind
                    Case SyntaxKind.SubBlock,
                        SyntaxKind.FunctionBlock,
                        SyntaxKind.MultiLineFunctionLambdaExpression,
                        SyntaxKind.MultiLineSubLambdaExpression
                        Exit While

                    Case SyntaxKind.TryBlock,
                        SyntaxKind.CatchBlock,
                        SyntaxKind.FinallyBlock
                        errorID = ERRID.ERR_GotoIntoTryHandler

                    Case SyntaxKind.UsingBlock
                        errorID = ERRID.ERR_GotoIntoUsing

                    Case SyntaxKind.SyncLockBlock
                        errorID = ERRID.ERR_GotoIntoSyncLock

                    Case SyntaxKind.WithBlock
                        errorID = ERRID.ERR_GotoIntoWith

                    Case SyntaxKind.ForBlock,
                        SyntaxKind.ForEachBlock
                        errorID = ERRID.ERR_GotoIntoFor
                End Select

                If errorID <> ERRID.ERR_None Then
                    If Not IsValidBranchTarget(labelParent, labelSyntax) Then
                        ReportDiagnostic(diagnostics, labelSyntax, ErrorFactory.ErrorInfo(errorID, label.Name))
                        hasError = True
                    End If

                    Exit While
                End If

                labelParent = labelParent.Parent
            End While

            Return Not hasError
        End Function

        Private Shared Function IsValidBranchTarget(block As VisualBasicSyntaxNode, labelSyntax As LabelSyntax) As Boolean
            Debug.Assert(block.Kind = SyntaxKind.TryBlock OrElse
                         block.Kind = SyntaxKind.CatchBlock OrElse
                         block.Kind = SyntaxKind.FinallyBlock OrElse
                         block.Kind = SyntaxKind.UsingBlock OrElse
                         block.Kind = SyntaxKind.SyncLockBlock OrElse
                         block.Kind = SyntaxKind.WithBlock OrElse
                         block.Kind = SyntaxKind.ForBlock OrElse
                         block.Kind = SyntaxKind.ForEachBlock)

            Dim parent = labelSyntax.Parent
            While parent IsNot Nothing
                If parent Is block Then
                    Return True
                End If

                parent = parent.Parent
            End While

            Return False
        End Function

        Private Function BindLabelStatement(node As LabelStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim labelToken As SyntaxToken = node.LabelToken
            Dim labelName = labelToken.ValueText

            ' A label symbol will always be found because all labels without syntax errors are put into the 
            ' label map in the blockbasebinder.
            Dim result = LookupResult.GetInstance()
            Lookup(result, labelName, 0, LookupOptions.LabelsOnly, useSiteDiagnostics:=Nothing)
            Debug.Assert(result.HasSingleSymbol AndAlso result.IsGood)

            Dim symbol = DirectCast(result.SingleSymbol, SourceLabelSymbol)

            ' Check for duplicate goto label

            Dim hasError = False
            If symbol.LabelName <> labelToken Then
                ' If symbol's token does not match the node's token then this is not the label target but a duplicate definition
                ReportDiagnostic(diagnostics, labelToken, ERRID.ERR_MultiplyDefined1, labelName)
                hasError = True
            End If
            result.Free()

            Return New BoundLabelStatement(node, symbol, hasErrors:=hasError)
        End Function

        ''' <summary>
        ''' Decodes a set of local declaration modifier flags and reports any errors with the flags.
        ''' </summary>
        ''' <param name="syntax">The syntax list of the modifiers.</param>
        ''' <param name="diagBag">returns True if any errors are reported</param>
        Private Sub DecodeLocalModifiersAndReportErrors(syntax As SyntaxTokenList, diagBag As DiagnosticBag)

            Const localModifiersMask = SourceMemberFlags.Const Or SourceMemberFlags.Dim Or SourceMemberFlags.Static
            Dim foundModifiers As SourceMemberFlags = Nothing

            ' Go through each modifiers, accumulating flags of what we've seen and reporting errors.
            Dim firstDim As SyntaxToken = Nothing
            Dim firstStatic As SyntaxToken = Nothing

            For Each keywordSyntax In syntax
                Dim currentModifier As SourceMemberFlags = MapKeywordToFlag(keywordSyntax)
                If currentModifier = SourceMemberFlags.None Then
                    Continue For
                End If

                ' Report errors with the modifier
                If (currentModifier And localModifiersMask) = 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_BadLocalDimFlags1, keywordSyntax.ToString())
                ElseIf (currentModifier And foundModifiers) <> 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_DuplicateSpecifier)
                Else

                    Select Case currentModifier
                        Case SourceMemberFlags.Dim
                            firstDim = keywordSyntax
                        Case SourceMemberFlags.Static
                            firstStatic = keywordSyntax
                    End Select

                    foundModifiers = foundModifiers Or currentModifier
                End If
            Next

            If (foundModifiers And SourceMemberFlags.Const) <> 0 Then

                '  Const incompatible with Dim or Static
                If (foundModifiers And SourceMemberFlags.Dim) <> 0 Then
                    ReportDiagnostic(diagBag, firstDim, ERRID.ERR_BadLocalConstFlags1, firstDim.ToString())
                ElseIf (foundModifiers And SourceMemberFlags.Static) <> 0 Then
                    ReportDiagnostic(diagBag, firstStatic, ERRID.ERR_BadLocalConstFlags1, firstStatic.ToString())
                End If

            ElseIf (foundModifiers And SourceMemberFlags.Static) <> 0 Then

                ' 'Static' keyword is only allowed in class methods, but not in structure methods
                If Me.ContainingType IsNot Nothing AndAlso Me.ContainingType.TypeKind = TYPEKIND.Structure Then
                    '  Local variables within methods of structures cannot be declared 'Static'
                    ReportDiagnostic(diagBag, firstStatic, ERRID.ERR_BadStaticLocalInStruct)
                ElseIf Me.IsInLambda Then
                    ReportDiagnostic(diagBag, firstStatic, ERRID.ERR_StaticInLambda)
                ElseIf Me.ContainingMember.Kind = SymbolKind.Method AndAlso DirectCast(Me.ContainingMember, MethodSymbol).IsGenericMethod Then
                    ReportDiagnostic(diagBag, firstStatic, ERRID.ERR_BadStaticLocalInGenericMethod)
                End If
            End If

        End Sub

        Private Function BindLocalDeclaration(node As LocalDeclarationStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement

            DecodeLocalModifiersAndReportErrors(node.Modifiers, diagnostics)

            Dim boundLocalDeclarations As ImmutableArray(Of BoundLocalDeclarationBase) = BindVariableDeclarators(node.Declarators, diagnostics)

            ' NOTE: Always create bound Dim statement to make 
            '       sure syntax node has a bound node associated with it
            Return New BoundDimStatement(node, boundLocalDeclarations, Nothing)
        End Function

        Private Function BindVariableDeclarators(
            declarators As SeparatedSyntaxList(Of VariableDeclaratorSyntax),
            diagnostics As DiagnosticBag
        ) As ImmutableArray(Of BoundLocalDeclarationBase)

            Dim builder = ArrayBuilder(Of BoundLocalDeclarationBase).GetInstance()

            For Each varDecl In declarators

                Dim asClauseOpt = varDecl.AsClause
                Dim initializerOpt As EqualsValueSyntax = varDecl.Initializer

                If initializerOpt IsNot Nothing Then
                    If varDecl.Names.Count > 1 Then
                        ' Can't combine an initializer with multiple variables
                        ReportDiagnostic(diagnostics, varDecl, ERRID.ERR_InitWithMultipleDeclarators)
                    End If
                End If

                Dim names = varDecl.Names

                Dim asNewVariablePlaceholder As BoundWithLValueExpressionPlaceholder = Nothing

                If names.Count = 1 Then
                    ' Dim x as integer = 1 OR Dim x as New Integer
                    builder.Add(BindVariableDeclaration(varDecl, names(0), asClauseOpt, initializerOpt, diagnostics))

                ElseIf asClauseOpt Is Nothing OrElse asClauseOpt.Kind <> SyntaxKind.AsNewClause Then
                    ' Dim x,y,z as integer
                    For i = 0 To names.Count - 1
                        Dim var = BindVariableDeclaration(varDecl, names(i), asClauseOpt, If(i = names.Count - 1, initializerOpt, Nothing), diagnostics)
                        builder.Add(var)
                    Next
                Else
                    ' Dim x,y,z as New C
                    Dim nameCount = names.Count
                    Dim locals = ArrayBuilder(Of BoundLocalDeclaration).GetInstance(nameCount)
                    For i = 0 To nameCount - 1
                        ' Pass the asClause to each local declaration so local knows its type and for error reporting.
                        Dim var = BindVariableDeclaration(varDecl, names(i), asClauseOpt, Nothing, diagnostics, i > 0)
                        locals.Add(var)
                    Next
                    ' At this point all of the local declarations have an initializer. Remove the initializers from the individual local declarations
                    ' and put the initializer on the BoundAsNewDeclaration. The local declarations are marked as initialized by the as-new.
                    Dim var0 As BoundLocalDeclaration = locals(0)
                    Dim asNewInitializer = var0.InitializerOpt
                    locals(0) = var0.Update(var0.LocalSymbol, Nothing, var0.IdentifierInitializerOpt, True)
#If DEBUG Then
                    For i = 0 To names.Count - 1
                        Debug.Assert(locals(i).InitializedByAsNew)
                        ' The assert below is disabled due to https://github.com/dotnet/roslyn/issues/27533, need to follow up
                        'Debug.Assert(locals(i).InitializerOpt Is Nothing OrElse locals(i).InitializerOpt.Kind = BoundKind.BadExpression OrElse locals(i).InitializerOpt.Kind = BoundKind.ArrayCreation)
                    Next
#End If

                    builder.Add(New BoundAsNewLocalDeclarations(varDecl, locals.ToImmutableAndFree(), asNewInitializer))
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Private Function GetLocalForDeclaration(identifier As SyntaxToken) As LocalSymbol
            ' We cannot rely on lookup to find the local because there could be locals with duplicate names.

            Dim current As Binder = Me
            Dim blockBinder As BlockBaseBinder

            Do
                blockBinder = TryCast(current, BlockBaseBinder)

                If blockBinder IsNot Nothing Then
                    Exit Do
                End If

                current = current.ContainingBinder
            Loop

            For Each local In blockBinder.Locals
                If local.IdentifierToken = identifier Then
                    Return local
                End If
            Next

            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overridable Function BindVariableDeclaration(
            tree As VisualBasicSyntaxNode,
            name As ModifiedIdentifierSyntax,
            asClauseOpt As AsClauseSyntax,
            equalsValueOpt As EqualsValueSyntax,
            diagnostics As DiagnosticBag,
            Optional skipAsNewInitializer As Boolean = False
        ) As BoundLocalDeclaration

            Dim symbol As LocalSymbol = GetLocalForDeclaration(name.Identifier)

            Dim declarationInitializer As BoundExpression = Nothing
            Dim declType As TypeSymbol = Nothing
            Dim boundArrayBounds As ImmutableArray(Of BoundExpression) = Nothing

            If name.ArrayBounds IsNot Nothing Then
                ' So as not to trigger order of simple name binding checks, must bind array bounds before initializer.
                boundArrayBounds = BindArrayBounds(name.ArrayBounds, diagnostics)
            End If

            ' We don't bind the value here and pass it into ComputeVariableType because there is a chicken and egg problem.  
            ' If the symbol has a type either from the type character or the as clause then we must first set the symbol 
            ' to that type before binding the expression.  For example,
            ' 
            ' Dim i% = i 

            ' is valid.  If  "i" were bound before the "i%" was resolved then i would not have a type and the expression 
            ' would have an error. ComputeVariableType will only bind the value if the symbol does not have an explicit 
            ' type.

            Dim type As TypeSymbol = ComputeVariableType(symbol,
                                                         name,
                                                         asClauseOpt,
                                                         equalsValueOpt,
                                                         declarationInitializer,
                                                         declType,
                                                         diagnostics)

            ' Now that we know the type go ahead and set it.
            VerifyLocalSymbolNameAndSetType(symbol, type, name, name.Identifier, diagnostics)

            Debug.Assert(type IsNot Nothing)

            Dim isInitializedByAsNew As Boolean = asClauseOpt IsNot Nothing AndAlso asClauseOpt.Kind = SyntaxKind.AsNewClause
            Dim errSyntax = If(asClauseOpt Is Nothing, DirectCast(equalsValueOpt, VisualBasicSyntaxNode), asClauseOpt.Type)

            Dim restrictedType As TypeSymbol = Nothing
            If type.IsRestrictedArrayType(restrictedType) Then
                If Not isInitializedByAsNew OrElse Not skipAsNewInitializer Then
                    ReportDiagnostic(diagnostics, errSyntax, ERRID.ERR_RestrictedType1, restrictedType)
                End If
            ElseIf symbol.IsStatic Then
                If type.IsRestrictedType() Then
                    If Not isInitializedByAsNew OrElse Not skipAsNewInitializer Then
                        ReportDiagnostic(diagnostics, errSyntax, ERRID.ERR_RestrictedType1, type)
                    End If
                ElseIf IsInAsyncContext() OrElse IsInIteratorContext() Then
                    ReportDiagnostic(diagnostics, name, ERRID.ERR_BadStaticInitializerInResumable)
                End If
            ElseIf IsInAsyncContext() OrElse IsInIteratorContext() Then
                If type.IsRestrictedType() Then
                    If Not isInitializedByAsNew OrElse Not skipAsNewInitializer Then
                        ReportDiagnostic(diagnostics, errSyntax, ERRID.ERR_CannotLiftRestrictedTypeResumable1, type)
                    End If
                End If
            End If

            If declarationInitializer Is Nothing Then

                ' We computed the type without needing to do type inference so bind the expression now.
                ' Because this symbol has a type, there is no danger of infinite recursion so we don't need
                ' a special binder.

                If symbol.IsConst Then
                    declarationInitializer = symbol.GetConstantExpression(Me)

                ElseIf equalsValueOpt IsNot Nothing Then
                    Dim valueSyntax = equalsValueOpt.Value
                    declarationInitializer = BindValue(valueSyntax, diagnostics)
                End If

            End If

            If declarationInitializer IsNot Nothing AndAlso Not symbol.IsConst Then
                ' Only apply the conversion for non constants.  Conversions for constants are handled in GetConstantExpression.
                declarationInitializer = ApplyImplicitConversion(declarationInitializer.Syntax, type, declarationInitializer, diagnostics)
            End If

            If isInitializedByAsNew Then
                Dim asNew = DirectCast(asClauseOpt, AsNewClauseSyntax)

                If symbol.IsConst Then
                    ReportDiagnostic(diagnostics, asNew.NewExpression.NewKeyword, ERRID.ERR_BadLocalConstFlags1, asNew.NewExpression.NewKeyword.ToString())
                Else

                    ' If there is an AsNew clause then create the object as well.
                    Select Case asNew.NewExpression.Kind
                        Case SyntaxKind.ObjectCreationExpression
                            Debug.Assert(declarationInitializer Is Nothing)

                            If Not skipAsNewInitializer Then
                                DisallowNewOnTupleType(asNew.Type, diagnostics)

                                Dim objectCreationExpressionSyntax = DirectCast(asNew.NewExpression, ObjectCreationExpressionSyntax)
                                Dim asNewVariablePlaceholder As New BoundWithLValueExpressionPlaceholder(asClauseOpt, symbol.Type)
                                asNewVariablePlaceholder.SetWasCompilerGenerated()

                                declarationInitializer = BindObjectCreationExpression(asNew.Type,
                                                                               objectCreationExpressionSyntax.ArgumentList,
                                                                               declType,
                                                                               objectCreationExpressionSyntax,
                                                                               diagnostics,
                                                                               asNewVariablePlaceholder)

                                Debug.Assert(declarationInitializer.Type.IsSameTypeIgnoringAll(declType))
                            End If

                        Case SyntaxKind.AnonymousObjectCreationExpression
                            ' Is supposed to be already bound by ComputeVariableType
                            Debug.Assert(declarationInitializer IsNot Nothing)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(asNew.NewExpression.Kind)
                    End Select

                    If type.IsArrayType Then
                        ' Arrays cannot be declared with AsNew syntax
                        ReportDiagnostic(diagnostics, asNew.NewExpression.NewKeyword, ERRID.ERR_AsNewArray)
                        declarationInitializer = BadExpression(asNew, declarationInitializer, type).MakeCompilerGenerated()
                    ElseIf declarationInitializer IsNot Nothing AndAlso Not declarationInitializer.HasErrors AndAlso
                           Not type.IsSameTypeIgnoringAll(declarationInitializer.Type) Then
                        ' An error must have been reported elsewhere.    
                        declarationInitializer = BadExpression(asNew, declarationInitializer, declarationInitializer.Type).MakeCompilerGenerated()
                    End If
                End If

            End If

            Dim identifierInitializer As BoundArrayCreation = Nothing
            If name.ArrayBounds IsNot Nothing Then
                ' It is an error to have both array bounds and an initializer expression
                identifierInitializer = New BoundArrayCreation(name, boundArrayBounds, Nothing, type).MakeCompilerGenerated()
                If declarationInitializer IsNot Nothing Then
                    If Not isInitializedByAsNew Then
                        ReportDiagnostic(diagnostics, name, ERRID.ERR_InitWithExplicitArraySizes)
                    Else
                        ' Must have reported ERR_AsNewArray already.
                        Debug.Assert(declarationInitializer.Kind = BoundKind.BadExpression)
                    End If
                End If
            End If

            If symbol.IsConst Then
                If Not type.IsErrorType() Then
                    If Not type.IsValidTypeForConstField() Then
                        ' "Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type."
                        ' arrays get the squiggles under the identifier name
                        ' other data types get the squiggles under the type part of the as clause 
                        errSyntax = If(asClauseOpt IsNot Nothing AndAlso Not type.IsArrayType, DirectCast(asClauseOpt.Type, VisualBasicSyntaxNode), name)
                        ReportDiagnostic(diagnostics, errSyntax, ERRID.ERR_ConstAsNonConstant)
                    Else
                        Dim bag = symbol.GetConstantValueDiagnostics(Me)
                        If bag IsNot Nothing Then
                            diagnostics.AddRange(bag)
                        End If
                    End If
                End If
            End If

            Return New BoundLocalDeclaration(name, symbol, declarationInitializer, identifierInitializer, isInitializedByAsNew)
        End Function

        ''' <summary>
        ''' Compute the type of a local symbol using the type character, as clause and equals value expression.
        ''' 1. Try to compute the type based on the identifier/modified identifier and as clause.  If there is a type then we're done.
        ''' 2. If OptionInfer is on then evaluate the expression and use that to infer the type.
        ''' 
        ''' ComputeVariableType will only bind the value if the symbol does not have an explicit type.
        ''' </summary>
        ''' <param name="symbol">The local symbol</param>
        ''' <param name="modifiedIdentifierOpt">The symbols modified identifier is there is one</param>
        ''' <param name="asClauseOpt">The optional as clause</param>
        ''' <param name="equalsValueOpt">The optional initializing expression</param>
        ''' <param name="valueExpression">The bound initializing expression</param>
        ''' <param name="asClauseType">The bound as clause type</param>
        Friend Function ComputeVariableType(symbol As LocalSymbol,
                                    modifiedIdentifierOpt As ModifiedIdentifierSyntax,
                                    asClauseOpt As AsClauseSyntax,
                                    equalsValueOpt As EqualsValueSyntax,
                                    <Out()> ByRef valueExpression As BoundExpression,
                                    <Out()> ByRef asClauseType As TypeSymbol,
                                    diagnostics As DiagnosticBag) As TypeSymbol

            valueExpression = Nothing

            Dim typeDiagnostic As Func(Of DiagnosticInfo) = Nothing

            If symbol.IsStatic Then
                If OptionStrict = OptionStrict.On Then
                    typeDiagnostic = ErrorFactory.GetErrorInfo_ERR_StrictDisallowImplicitObject

                ElseIf OptionStrict = OptionStrict.Custom Then
                    typeDiagnostic = ErrorFactory.GetErrorInfo_WRN_ObjectAssumedVar1_WRN_StaticLocalNoInference
                End If

            ElseIf Not (OptionInfer AndAlso equalsValueOpt IsNot Nothing) Then
                If OptionStrict = OptionStrict.On Then
                    typeDiagnostic = ErrorFactory.GetErrorInfo_ERR_StrictDisallowImplicitObject

                ElseIf OptionStrict = OptionStrict.Custom Then
                    typeDiagnostic = ErrorFactory.GetErrorInfo_WRN_ObjectAssumedVar1_WRN_MissingAsClauseinVarDecl
                End If
            End If

            Dim type As TypeSymbol
            Dim hasExplicitType As Boolean

            If modifiedIdentifierOpt IsNot Nothing Then

                If asClauseOpt IsNot Nothing AndAlso asClauseOpt.Kind = SyntaxKind.AsNewClause Then
                    Dim asNewClause = DirectCast(asClauseOpt, AsNewClauseSyntax)
                    Dim newExpression As NewExpressionSyntax = asNewClause.NewExpression
                    Debug.Assert(newExpression IsNot Nothing)

                    If newExpression.Kind = SyntaxKind.AnonymousObjectCreationExpression Then

                        ' Bind anonymous type creation to define it's type
                        Dim binder = New LocalInProgressBinder(Me, symbol)
                        valueExpression = binder.BindAnonymousObjectCreationExpression(
                                                    DirectCast(newExpression, AnonymousObjectCreationExpressionSyntax), diagnostics)
                        asClauseType = valueExpression.Type
                        Return asClauseType
                    End If
                End If

                ' Adjust type because the modified identifier can change the type to array or make it nullable.
                ' DecodeModifiedIdentifierType returns the explicit type or the default type based on object. i.e. object or object()
                type = DecodeModifiedIdentifierType(modifiedIdentifierOpt, asClauseOpt, equalsValueOpt,
                                                        typeDiagnostic,
                                                        asClauseType,
                                                        diagnostics,
                                                        If(symbol.IsStatic,
                                                           ModifiedIdentifierTypeDecoderContext.StaticLocalType Or ModifiedIdentifierTypeDecoderContext.LocalType,
                                                           ModifiedIdentifierTypeDecoderContext.LocalType))

                hasExplicitType = Not HasDefaultType(modifiedIdentifierOpt, asClauseOpt)
            Else
                Dim identifier = symbol.IdentifierToken
                type = DecodeIdentifierType(identifier, asClauseOpt,
                                        typeDiagnostic,
                                        asClauseType,
                                        diagnostics)

                hasExplicitType = Not HasDefaultType(identifier, asClauseOpt)
            End If

            If hasExplicitType AndAlso Not (symbol.IsConst AndAlso type.SpecialType = SpecialType.System_Object) Then
                ' There is an explicit type or TypeCharacter.  Return the type. 
                ' Constants are special.  Don't return here when a constant is typed as object.
                Return type
            End If

            ' The default type is Object.  Infer a type if OptionInfer is on.
            ' Don't infer types for static locals.
            ' Always infer type of constant. 
            If OptionInfer AndAlso Not symbol.IsStatic AndAlso Not symbol.IsConst Then
                If equalsValueOpt IsNot Nothing Then
                    Dim valueSyntax As ExpressionSyntax = equalsValueOpt.Value

                    ' Use an LocalInProgressBinder to detect cycles using locals.
                    Dim binder = New LocalInProgressBinder(Me, symbol)
                    valueExpression = binder.BindValue(valueSyntax, diagnostics)

                    Dim inferFrom As BoundExpression = valueExpression

                    ' Dig through parenthesized in case this expression is one of the special expressions
                    ' that does not have a type such as lambda's and array literals.
                    If Not inferFrom.IsNothingLiteral Then
                        inferFrom = inferFrom.GetMostEnclosedParenthesizedExpression()
                    End If

                    Dim inferredType As TypeSymbol = Nothing
                    Dim arrayLiteral As BoundArrayLiteral = Nothing

                    Select Case inferFrom.Kind
                        Case BoundKind.UnboundLambda
                            inferredType = DirectCast(inferFrom, UnboundLambda).InferredAnonymousDelegate.Key

                        Case BoundKind.ArrayLiteral
                            arrayLiteral = DirectCast(inferFrom, BoundArrayLiteral)
                            inferredType = arrayLiteral.InferredType

                        Case BoundKind.TupleLiteral
                            Dim tupleLiteral = DirectCast(inferFrom, BoundTupleLiteral)
                            inferredType = tupleLiteral.InferredType

                        Case Else
                            inferredType = inferFrom.Type
                    End Select

                    If inferredType IsNot Nothing Then
                        ' Infer the type from the expression. When the identifier has modifiers, the expression type
                        ' and the modifiers need to be compatible. Without modifiers just use the expression type.
                        Dim localDiagnostics As DiagnosticBag = If(inferFrom.HasErrors, New DiagnosticBag(), diagnostics)

                        If modifiedIdentifierOpt IsNot Nothing Then
                            type = InferVariableType(type, modifiedIdentifierOpt, valueSyntax, inferredType, inferFrom, typeDiagnostic, localDiagnostics)

                            If type IsNot inferredType AndAlso arrayLiteral IsNot Nothing Then

                                ' Normally ReportArrayLiteralInferredElementTypeDiagnostics is handled in ReclassifyArrayLiteralExpression.  
                                ' ReportArrayLiteralInferredElementTypeDiagnostics depends on being able to compare the variable type with 
                                ' the inferred type. When the symbols are the same, it assumes the variable got its type from the expression.
                                ' Because the modified identifier created a new array type symbol, we report errors now.  
                                ReportArrayLiteralInferredTypeDiagnostics(arrayLiteral, localDiagnostics)
                            End If

                        Else
                            type = inferredType
                        End If
                    End If
                End If
            ElseIf symbol.IsConst Then
                ' If we arrive here it is because the constant does not have an explicit type or the type is object.
                ' In either case, the type will always be the type of the expression.
                valueExpression = symbol.GetConstantExpression(Me)
                Dim valueType = valueExpression.Type
                If valueType IsNot Nothing AndAlso valueType.GetEnumUnderlyingTypeOrSelf.IsIntrinsicType Then
                    type = valueExpression.Type
                End If
            ElseIf Not symbol.IsStatic AndAlso OptionStrict <> OptionStrict.On AndAlso
                   Not hasExplicitType AndAlso type.IsObjectType() AndAlso
                   modifiedIdentifierOpt IsNot Nothing AndAlso
                   modifiedIdentifierOpt.Nullable.Node IsNot Nothing AndAlso
                   equalsValueOpt IsNot Nothing Then
                Debug.Assert(Not symbol.IsConst AndAlso Not symbol.IsStatic AndAlso Not OptionInfer)

                ReportDiagnostic(diagnostics, modifiedIdentifierOpt, ERRID.ERR_NullableTypeInferenceNotSupported)
            End If

            ' Return the inferred type or the default type
            Return type

        End Function

        ''' <summary>
        '''  Infer the type of a for-from-to control variable.
        ''' </summary>
        Friend Function InferForFromToVariableType(symbol As LocalSymbol,
                                 fromValueSyntax As ExpressionSyntax,
                                 toValueSyntax As ExpressionSyntax,
                                 stepClauseSyntaxOpt As ForStepClauseSyntax,
                                 <Out()> ByRef fromValueExpression As BoundExpression,
                                 <Out()> ByRef toValueExpression As BoundExpression,
                                 <Out()> ByRef stepValueExpression As BoundExpression,
                                 diagnostics As DiagnosticBag) As TypeSymbol

            fromValueExpression = Nothing
            toValueExpression = Nothing
            stepValueExpression = Nothing

            Dim identifier = symbol.IdentifierToken

            Dim type = DecodeIdentifierType(identifier, Nothing,
              Nothing,
              Nothing,
              diagnostics)

            Dim hasExplicitType = Not HasDefaultType(identifier, Nothing)

            If hasExplicitType Then
                Return type
            End If

            ' Use an ImplicitlyTypedLocalBinder to detect cycles using locals.
            Dim binder = New LocalInProgressBinder(Me, symbol)

            fromValueExpression = binder.BindRValue(fromValueSyntax, diagnostics)

            toValueExpression = binder.BindRValue(toValueSyntax, diagnostics)

            If stepClauseSyntaxOpt IsNot Nothing Then
                stepValueExpression = binder.BindRValue(stepClauseSyntaxOpt.StepValue, diagnostics)
            End If

            If toValueExpression.HasErrors OrElse fromValueExpression.HasErrors OrElse
                (stepValueExpression IsNot Nothing AndAlso stepValueExpression.HasErrors) Then
                Return type
            End If

            Dim numCandidates As Integer = 0
            Dim array = ArrayBuilder(Of BoundExpression).GetInstance(2)
            array.Add(fromValueExpression)
            array.Add(toValueExpression)
            If stepValueExpression IsNot Nothing Then
                array.Add(stepValueExpression)
            End If

            Dim identifierName = DirectCast(identifier.Parent, IdentifierNameSyntax)
            Dim errorReasons As InferenceErrorReasons = InferenceErrorReasons.Other

            Dim dominantType = InferDominantTypeOfExpressions(identifierName, array, diagnostics, numCandidates, errorReasons)
            array.Free()

            '  check the resulting type
            If numCandidates = 0 OrElse (numCandidates > 1 AndAlso (errorReasons And InferenceErrorReasons.Ambiguous) = 0) Then
                ReportDiagnostic(diagnostics, identifierName, ERRID.ERR_NoSuitableWidestType1, identifierName.Identifier.ValueText)
                Return type

            ElseIf numCandidates > 1 Then
                ReportDiagnostic(diagnostics, identifierName, ERRID.ERR_AmbiguousWidestType3, identifierName.Identifier.ValueText)
                Return type
            End If

            If dominantType IsNot Nothing Then
                Return dominantType
            End If

            Return type
        End Function

        ''' <summary>
        '''  Infer the type of a for-each control variable.
        ''' </summary>
        Friend Function InferForEachVariableType(symbol As LocalSymbol,
                         collectionSyntax As ExpressionSyntax,
                         <Out()> ByRef collectionExpression As BoundExpression,
                         <Out()> ByRef currentType As TypeSymbol,
                         <Out()> ByRef elementType As TypeSymbol,
                         <Out()> ByRef isEnumerable As Boolean,
                         <Out()> ByRef boundGetEnumeratorCall As BoundExpression,
                         <Out()> ByRef boundEnumeratorPlaceholder As BoundLValuePlaceholder,
                         <Out()> ByRef boundMoveNextCall As BoundExpression,
                         <Out()> ByRef boundCurrentAccess As BoundExpression,
                         <Out()> ByRef collectionPlaceholder As BoundRValuePlaceholder,
                         <Out()> ByRef needToDispose As Boolean,
                         <Out()> ByRef isOrInheritsFromOrImplementsIDisposable As Boolean,
                         diagnostics As DiagnosticBag) As TypeSymbol

            collectionExpression = Nothing
            currentType = Nothing
            elementType = Nothing
            isEnumerable = False
            boundGetEnumeratorCall = Nothing
            boundEnumeratorPlaceholder = Nothing
            boundMoveNextCall = Nothing
            boundCurrentAccess = Nothing
            collectionPlaceholder = Nothing
            needToDispose = False
            isOrInheritsFromOrImplementsIDisposable = False

            Dim identifier = symbol.IdentifierToken

            Dim type = DecodeIdentifierType(identifier, Nothing,
                        Nothing,
                        Nothing,
                        diagnostics)

            Dim hasExplicitType = Not HasDefaultType(identifier, Nothing)

            If hasExplicitType Then
                Return type
            End If

            ' Use an ImplicitlyTypedLocalBinder to detect cycles using locals.
            Dim binder = New LocalInProgressBinder(Me, symbol)

            ' bind the expression that describes the collection to iterate over
            collectionExpression = binder.BindValue(collectionSyntax, diagnostics)

            ' collection will be a target to a GetEnumerator call, so we need to adjust
            ' it to RValue if it is not an LValue already.
            If Not collectionExpression.IsLValue AndAlso Not collectionExpression.IsNothingLiteral Then
                collectionExpression = MakeRValue(collectionExpression, diagnostics)
            End If

            ' check if the collection is valid for a for each
            collectionExpression = InterpretForEachStatementCollection(collectionExpression,
                                                                       currentType,
                                                                       elementType,
                                                                       isEnumerable,
                                                                       boundGetEnumeratorCall,
                                                                       boundEnumeratorPlaceholder,
                                                                       boundMoveNextCall,
                                                                       boundCurrentAccess,
                                                                       collectionPlaceholder,
                                                                       needToDispose,
                                                                       isOrInheritsFromOrImplementsIDisposable,
                                                                       diagnostics)

            If elementType IsNot Nothing Then
                Return elementType
            End If

            Return type
        End Function


        ''' <summary>
        ''' Infer the type of a variable declared with an initializing expression.
        ''' </summary>
        Private Function InferVariableType(defaultType As TypeSymbol,
                                           name As ModifiedIdentifierSyntax,
                                           valueSyntax As ExpressionSyntax,
                                           valueType As TypeSymbol,
                                           valueExpression As BoundExpression,
                                           getRequireTypeDiagnosticInfoFunc As Func(Of DiagnosticInfo),
                                           diagnostics As DiagnosticBag) As TypeSymbol

            Dim nameIsArrayType = IsArrayType(name)
            Dim nameHasNullable = name.Nullable.Node IsNot Nothing
            Dim identifier = name.Identifier

            If nameHasNullable AndAlso
                valueType.IsArrayType AndAlso
                Not nameIsArrayType Then

                ' if name is nullable and expression is an array then the name must also be an array
                ReportDiagnostic(diagnostics, identifier, ERRID.ERR_CannotInferNullableForVariable1, name.Identifier.ToString())

            ElseIf nameIsArrayType AndAlso Not valueType.IsArrayType Then

                ' if name is an array then the expression must be an array
                ReportDiagnostic(diagnostics, identifier, ERRID.ERR_InferringNonArrayType1, valueType)

            ElseIf Not nameIsArrayType Then
                If nameHasNullable Then

                    ' name is nullable but not an array therefore the value must be a value type.
                    If Not valueType.IsValueType Then

                        ReportDiagnostic(diagnostics, identifier, ERRID.ERR_CannotInferNullableForVariable1, identifier.ToString())

                    ElseIf valueType.IsNullableType Then

                        Return valueType

                    Else

                        Dim modifiedValueType = DecodeModifiedIdentifierType(name,
                                                            valueType,
                                                            Nothing,
                                                            valueSyntax,
                                                            getRequireTypeDiagnosticInfoFunc,
                                                            diagnostics)
                        Return modifiedValueType
                    End If

                Else
                    ' name does not add array or nullable so return the expression type.
                    Return valueType
                End If

            Else
                Debug.Assert(nameIsArrayType AndAlso valueType.IsArrayType, "both the name and the value should be arrays")

                ' Both the name and the expression are arrays
                ' Check that the name's array is compatible with the expression array type

                If nameHasNullable Then
                    ' Verify that the rhs element type is also a nullable type

                    Dim rhsElementType As TypeSymbol = DirectCast(valueType, ArrayTypeSymbol).ElementType
                    Do
                        If Not rhsElementType.IsArrayType Then
                            Exit Do
                        End If
                        rhsElementType = DirectCast(rhsElementType, ArrayTypeSymbol).ElementType
                    Loop

                    If Not rhsElementType.IsNullableType Then
                        ReportDiagnostic(diagnostics, identifier, ERRID.ERR_CannotInferNullableForVariable1, identifier.ToString())
                        Return defaultType
                    End If
                End If

                If DirectCast(defaultType, ArrayTypeSymbol).Rank <> DirectCast(valueType, ArrayTypeSymbol).Rank Then
                    Dim arrayLiteral = TryCast(valueExpression, BoundArrayLiteral)
                    ' Arrays on both sides but ranks are not the same.  This is an error unless rhs is an empty literal.
                    If (arrayLiteral Is Nothing OrElse Not arrayLiteral.IsEmptyArrayLiteral) Then
                        ReportDiagnostic(diagnostics, name, ERRID.ERR_TypeInferenceArrayRankMismatch1, name.Identifier.ToString())
                    End If

                Else
                    ' For arrays, if both the LHS and RHS specify an array shape, we
                    ' have to allow inference to infer "arrays" when possible. That is, if
                    ' we consider matching the shape from left to right, if we "consume" the arrays
                    ' of the RHS and have "extra" arrays left over, we infer that.
                    '
                    ' For example:
                    ' dim x() = new Integer()() - we infer integer()()
                    ' dim x(,)() = new Integer(,)()(,) - we infer integer(,)()(,)
                    '
                    ' The algorithm used is to match the array rank until we exhaust the LHS,
                    ' and if the RHS has more arrays, simply infer the RHS into the LHS. If the RHS
                    ' "runs out of arrays" before the LHS, then we just infer the base type and give
                    ' a conversion error.

                    Dim lhsType As TypeSymbol = defaultType
                    Dim rhsType As TypeSymbol = valueType

                    Do
                        If lhsType.IsArrayType AndAlso rhsType.IsArrayType Then

                            Dim lhsArrayType = DirectCast(lhsType, ArrayTypeSymbol)
                            Dim rhsArrayType = DirectCast(rhsType, ArrayTypeSymbol)

                            If lhsArrayType.Rank = rhsArrayType.Rank Then
                                lhsType = lhsArrayType.ElementType
                                rhsType = rhsArrayType.ElementType
                            Else
                                Exit Do
                            End If

                        ElseIf rhsType.IsArrayType OrElse Not lhsType.IsArrayType Then
                            Return valueType
                        Else
                            Exit Do
                        End If
                    Loop

                    Dim modifiedValueType = DecodeModifiedIdentifierType(name,
                              rhsType,
                              Nothing,
                              valueSyntax,
                              getRequireTypeDiagnosticInfoFunc,
                              diagnostics)

                    Return modifiedValueType
                End If
            End If

            Return defaultType
        End Function


        'TODO: override in MethodBodySemanticModel similarly to BindVariableDeclaration.
        Friend Overridable Function BindCatchVariableDeclaration(name As IdentifierNameSyntax,
                                                    asClause As AsClauseSyntax,
                                                    diagnostics As DiagnosticBag) As BoundLocal

            Dim identifier = name.Identifier
            Dim symbol As LocalSymbol = GetLocalForDeclaration(identifier)

            ' Attempt to bind the type
            Dim type As TypeSymbol = BindTypeSyntax(asClause.Type, diagnostics)

            VerifyLocalSymbolNameAndSetType(symbol, type, name, identifier, diagnostics)
            Return New BoundLocal(name, symbol, symbol.Type)
        End Function

        ''' <summary>
        ''' Verifies that declaration of a local symbol does not cause name clashes.
        ''' </summary>
        Private Sub VerifyLocalSymbolNameAndSetType(local As LocalSymbol,
                                    type As TypeSymbol,
                                    nameSyntax As VisualBasicSyntaxNode,
                                    identifier As SyntaxToken,
                                    diagnostics As DiagnosticBag)

            Dim localForFunctionValue = GetLocalForFunctionValue()
            Dim name = identifier.ValueText
            ' Set the type of the symbol, so we don't have to re-compute it later.
            local.SetType(type)

            If localForFunctionValue IsNot Nothing AndAlso CaseInsensitiveComparison.Equals(local.Name, localForFunctionValue.Name) Then
                ' Does name conflict with the function name?
                ReportDiagnostic(diagnostics, nameSyntax, ERRID.ERR_LocalSameAsFunc)

            Else
                Dim result = LookupResult.GetInstance()
                Lookup(result, identifier.ValueText, 0, Nothing, useSiteDiagnostics:=Nothing)

                ' A local symbol will always be found because all local declarations are put into the 
                ' local map in the blockbasebinder. 
                Debug.Assert(result.HasSingleSymbol AndAlso result.IsGood)
                Dim lookupSymbol = DirectCast(result.SingleSymbol, LocalSymbol)
                result.Free()

                If lookupSymbol.IdentifierToken.FullSpan <> identifier.FullSpan Then
                    If lookupSymbol.CanBeReferencedByName Then
                        ' Static Locals have there own diagnostic. ERR_DuplicateLocalStatic1
                        If Not lookupSymbol.IsStatic OrElse Not local.IsStatic Then
                            ' If location does not match then this is a duplicate local, unless it has an invalid name (the syntax was bad)
                            ReportDiagnostic(diagnostics, nameSyntax, ERRID.ERR_DuplicateLocals1, name)

                            ' Difference to Dev10:
                            ' For r as Integer in New Integer() {}
                            '     Dim r as Integer = 23
                            ' next
                            ' Does not give a BC30288 "Local variable 'r' is already declared in the current block.", but a
                            ' BC30616 "Variable 'r' hides a variable in an enclosing block." with the same location.
                            ' The reason is the binder hierarchy, where the ForOrForEachBlockBinder and the StatementListBinder both have
                            ' r in their locals set. When looking up the symbol one gets the inner symbol and then the FullSpans match
                            ' which then does not trigger the condition above.
                        End If
                    End If
                Else
                    ' Look up in container binders for name clashes with other locals, parameters and type parameters.
                    ' Stop at the named type binder

                    Dim container = Me.ContainingBinder

                    If container IsNot Nothing Then
                        container.VerifyNameShadowingInMethodBody(local, nameSyntax, identifier, diagnostics)
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Should be called on the binder, at which the check should begin.
        ''' </summary>
        Private Sub VerifyNameShadowingInMethodBody(
            symbol As Symbol,
            nameSyntax As SyntaxNodeOrToken,
            identifier As SyntaxNodeOrToken,
            diagnostics As DiagnosticBag
        )
            Debug.Assert(symbol.Kind = SymbolKind.Local OrElse symbol.Kind = SymbolKind.RangeVariable OrElse
                         (symbol.Kind = SymbolKind.Parameter AndAlso
                          TypeOf symbol Is UnboundLambdaParameterSymbol))

            Dim name As String = symbol.Name

            ' Look up in container binders for name clashes with other locals, parameters and type parameters.
            ' Stop at the named type binder

            Dim container = Me
            Dim result = LookupResult.GetInstance
            Dim err As ERRID

            Do

                Dim namedTypeBinder = TryCast(container, namedTypeBinder)
                If namedTypeBinder IsNot Nothing Then
                    Exit Do
                End If

                result.Clear()
                container.LookupInSingleBinder(result, name, 0, Nothing, Me, useSiteDiagnostics:=Nothing)

                If result.HasSingleSymbol Then

                    Dim altSymbol = result.SingleSymbol
                    If altSymbol IsNot symbol Then

                        Select Case altSymbol.Kind
                            Case SymbolKind.Local, SymbolKind.RangeVariable
                                If symbol.Kind = SymbolKind.Parameter Then
                                    err = ERRID.ERR_LambdaParamShadowLocal1
                                ElseIf symbol.Kind <> SymbolKind.RangeVariable Then
                                    err = ERRID.ERR_BlockLocalShadowing1
                                ElseIf Me.ImplicitVariableDeclarationAllowed Then
                                    err = ERRID.ERR_IterationVariableShadowLocal2
                                Else
                                    err = ERRID.ERR_IterationVariableShadowLocal1
                                End If

                                ReportDiagnostic(diagnostics, nameSyntax, err, name)

                            Case SymbolKind.Parameter

                                If symbol.Kind = SymbolKind.Parameter Then
                                    err = ERRID.ERR_LambdaParamShadowLocal1
                                ElseIf symbol.Kind <> SymbolKind.RangeVariable Then
                                    If DirectCast(altSymbol.ContainingSymbol, MethodSymbol).MethodKind = MethodKind.LambdaMethod Then
                                        err = ERRID.ERR_LocalNamedSameAsParamInLambda1
                                    Else
                                        err = ERRID.ERR_LocalNamedSameAsParam1
                                    End If
                                ElseIf Me.ImplicitVariableDeclarationAllowed Then
                                    err = ERRID.ERR_IterationVariableShadowLocal2
                                Else
                                    err = ERRID.ERR_IterationVariableShadowLocal1

                                    ' Change from Dev10: we're now also correctly reporting this error for control variables
                                    ' of for/for each loops when they shadow variables that are parameters.
                                End If

                                ReportDiagnostic(diagnostics, nameSyntax, err, name)

                            Case SymbolKind.TypeParameter
                                ' this diagnostic will not be shown if the symbol is for or for each local has an inferred type
                                Dim local = TryCast(symbol, LocalSymbol)
                                If local Is Nothing OrElse
                                    Not (local.IsFor OrElse local.IsForEach) OrElse
                                    Not local.HasInferredType Then
                                    ReportDiagnostic(diagnostics, nameSyntax, ERRID.ERR_NameSameAsMethodTypeParam1, name)
                                End If
                        End Select

                    End If

                    Exit Do

                End If

                Dim implicitVariableBinder = TryCast(container, implicitVariableBinder)
                If implicitVariableBinder IsNot Nothing AndAlso Not implicitVariableBinder.AllImplicitVariableDeclarationsAreHandled Then
                    ' If an implicit local comes into being later, then report an error here.
                    If symbol.Kind = SymbolKind.Parameter Then
                        err = ERRID.ERR_LambdaParamShadowLocal1
                    ElseIf symbol.Kind <> SymbolKind.RangeVariable Then
                        err = ERRID.ERR_BlockLocalShadowing1
                    Else
                        err = ERRID.ERR_IterationVariableShadowLocal2
                    End If

                    implicitVariableBinder.RememberPossibleShadowingVariable(name, identifier, err)
                End If

                container = container.ContainingBinder
            Loop While container IsNot Nothing

            result.Free()
        End Sub

        Friend Function AdjustAssignmentTarget(node As SyntaxNode, op1 As BoundExpression, diagnostics As DiagnosticBag, ByRef isError As Boolean) As BoundExpression
            Select Case op1.Kind
                Case BoundKind.XmlMemberAccess
                    Dim memberAccess = DirectCast(op1, BoundXmlMemberAccess)
                    Dim expr = AdjustAssignmentTarget(node, memberAccess.MemberAccess, diagnostics, isError)
                    Return memberAccess.Update(expr)

                Case BoundKind.PropertyAccess
                    Dim propertyAccess As BoundPropertyAccess = DirectCast(op1, BoundPropertyAccess)
                    Dim propertySymbol As PropertySymbol = propertyAccess.PropertySymbol

                    If propertyAccess.IsLValue Then
                        Debug.Assert(propertySymbol.ReturnsByRef)
                        WarnOnRecursiveAccess(propertyAccess, PropertyAccessKind.Get, diagnostics)
                        Return propertyAccess.SetAccessKind(PropertyAccessKind.Get)
                    End If

                    Debug.Assert(propertyAccess.AccessKind <> PropertyAccessKind.Get)

                    If Not propertyAccess.IsWriteable Then
                        ReportDiagnostic(diagnostics, node, ERRID.ERR_NoSetProperty1, CustomSymbolDisplayFormatter.ShortErrorName(propertySymbol))
                        isError = True
                    Else
                        Dim setMethod = propertySymbol.GetMostDerivedSetMethod()

                        ' NOTE: the setMethod could not be present, while it would still be
                        '       possible to write to the property in a case
                        '       where the property is a getter-only autoproperty 
                        '       and the writing is happening in the corresponding constructor or initializer
                        If setMethod IsNot Nothing Then
                            ReportDiagnosticsIfObsoleteOrNotSupportedByRuntime(diagnostics, setMethod, node)

                            If ReportUseSiteError(diagnostics, op1.Syntax, setMethod) Then
                                isError = True
                            Else
                                Dim accessThroughType = GetAccessThroughType(propertyAccess.ReceiverOpt)
                                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                                If Not IsAccessible(setMethod, useSiteDiagnostics, accessThroughType) AndAlso
                                   IsAccessible(propertySymbol, useSiteDiagnostics, accessThroughType) Then
                                    ReportDiagnostic(diagnostics, node, ERRID.ERR_NoAccessibleSet, CustomSymbolDisplayFormatter.ShortErrorName(propertySymbol))
                                    isError = True
                                End If

                                diagnostics.Add(node, useSiteDiagnostics)
                            End If
                        End If
                    End If

                    WarnOnRecursiveAccess(propertyAccess, PropertyAccessKind.Set, diagnostics)
                    Return propertyAccess.SetAccessKind(PropertyAccessKind.Set)

                Case BoundKind.LateMemberAccess
                    Debug.Assert((DirectCast(op1, BoundLateMemberAccess).AccessKind And (LateBoundAccessKind.Get Or LateBoundAccessKind.Call)) = 0)
                    Return DirectCast(op1, BoundLateMemberAccess).SetAccessKind(LateBoundAccessKind.Set)

                Case BoundKind.LateInvocation
                    Debug.Assert((DirectCast(op1, BoundLateInvocation).AccessKind And (LateBoundAccessKind.Get Or LateBoundAccessKind.Call)) = 0)
                    Return DirectCast(op1, BoundLateInvocation).SetAccessKind(LateBoundAccessKind.Set)

                Case Else
                    Return op1

            End Select
        End Function

        Private Function BindAssignment(node As SyntaxNode, op1 As BoundExpression, op2 As BoundExpression, diagnostics As DiagnosticBag) As BoundAssignmentOperator

            Dim isError As Boolean = False
            op1 = AdjustAssignmentTarget(node, op1, diagnostics, isError)

            Dim targetType As TypeSymbol = op1.Type
            Debug.Assert(targetType IsNot Nothing OrElse isError)

            If targetType IsNot Nothing Then
                op2 = ApplyImplicitConversion(op2.Syntax, targetType, op2, diagnostics)
            Else
                ' Try to reclassify op2 if we still can.
                op2 = MakeRValueAndIgnoreDiagnostics(op2)
            End If

            Return New BoundAssignmentOperator(node, op1, op2, False, hasErrors:=isError)
        End Function

        Private Function BindCompoundAssignment(
            node As VisualBasicSyntaxNode,
            left As BoundExpression,
            right As BoundExpression,
            operatorTokenKind As SyntaxKind,
            operatorKind As BinaryOperatorKind,
            diagnostics As DiagnosticBag
        ) As BoundAssignmentOperator

            Dim isError As Boolean = False

            ' We should be able to use the 'left' as an assignment target and as an RValue.
            ' Let's verify that
            Dim assignmentTarget As BoundExpression = AdjustAssignmentTarget(node, left, diagnostics, isError)

            Dim rValue As BoundExpression

            If isError Then
                rValue = MakeRValueAndIgnoreDiagnostics(left)
            Else
                rValue = MakeRValue(left, diagnostics)
                isError = rValue.HasErrors
            End If

            Dim targetType As TypeSymbol = assignmentTarget.Type
            Debug.Assert(targetType IsNot Nothing OrElse isError)

            Dim placeholder As BoundCompoundAssignmentTargetPlaceholder = Nothing

            If isError Then
                ' Suppress all additional diagnostics. This ensures that we still generate the appropriate tree shape
                ' even in error scenarios
                diagnostics = New DiagnosticBag()
            End If

            placeholder = New BoundCompoundAssignmentTargetPlaceholder(left.Syntax, targetType).MakeCompilerGenerated()
            right = BindBinaryOperator(node, placeholder, right, operatorTokenKind, operatorKind, isOperandOfConditionalBranch:=False, diagnostics:=diagnostics)
            right.SetWasCompilerGenerated()
            right = ApplyImplicitConversion(node, targetType, right, diagnostics)

            left = left.SetGetSetAccessKindIfAppropriate()

            Return New BoundAssignmentOperator(node, left, placeholder, right, False, hasErrors:=isError)
        End Function

        ''' <summary>
        ''' Binds a list of statements and puts in a scope.
        ''' </summary>
        Friend Function BindBlock(syntax As SyntaxNode, stmtList As SyntaxList(Of StatementSyntax), diagnostics As DiagnosticBag) As BoundBlock
            Dim stmtListBinder = Me.GetBinder(stmtList)
            Return BindBlock(syntax, stmtList, diagnostics, stmtListBinder)
        End Function

        ''' <summary>
        ''' Binds a list of statements and puts in a scope.
        ''' </summary>
        Friend Function BindBlock(syntax As SyntaxNode, stmtList As SyntaxList(Of StatementSyntax), diagnostics As DiagnosticBag, stmtListBinder As Binder) As BoundBlock
            Dim boundStatements(stmtList.Count - 1) As BoundStatement
            Dim locals As ArrayBuilder(Of LocalSymbol) = Nothing

            For i = 0 To boundStatements.Length - 1
                Dim boundStatement As BoundStatement = stmtListBinder.BindStatement(stmtList(i), diagnostics)
                boundStatements(i) = boundStatement

                Select Case boundStatement.Kind
                    Case BoundKind.LocalDeclaration
                        Dim localDecl As BoundLocalDeclaration = DirectCast(boundStatement, BoundLocalDeclaration)
                        DeclareLocal(locals, localDecl)

                    Case BoundKind.AsNewLocalDeclarations
                        Dim asNewLocalDecls As BoundAsNewLocalDeclarations = DirectCast(boundStatement, BoundAsNewLocalDeclarations)
                        DeclareLocal(locals, asNewLocalDecls)

                    Case BoundKind.DimStatement
                        Dim multipleDecl As BoundDimStatement = DirectCast(boundStatement, BoundDimStatement)
                        For Each localDecl In multipleDecl.LocalDeclarations
                            DeclareLocal(locals, localDecl)
                        Next
                End Select
            Next i

            If locals Is Nothing Then
                Return New BoundBlock(syntax, stmtList, ImmutableArray(Of LocalSymbol).Empty, boundStatements.AsImmutableOrNull())
            End If

            Return New BoundBlock(syntax, stmtList, locals.ToImmutableAndFree, boundStatements.AsImmutableOrNull())
        End Function

        Private Shared Sub DeclareLocal(ByRef locals As ArrayBuilder(Of LocalSymbol), localDecl As BoundLocalDeclarationBase)
            If locals Is Nothing Then
                locals = ArrayBuilder(Of LocalSymbol).GetInstance
            End If

            Select Case localDecl.Kind
                Case BoundKind.LocalDeclaration
                    locals.Add(DirectCast(localDecl, BoundLocalDeclaration).LocalSymbol)

                Case BoundKind.AsNewLocalDeclarations
                    For Each decl In DirectCast(localDecl, BoundAsNewLocalDeclarations).LocalDeclarations
                        locals.Add(DirectCast(decl, BoundLocalDeclaration).LocalSymbol)
                    Next

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(localDecl.Kind)
            End Select

        End Sub

        Private Function BindAssignmentStatement(node As AssignmentStatementSyntax, diagnostics As DiagnosticBag) As BoundExpressionStatement
            Debug.Assert(node IsNot Nothing)

            Dim op1 As BoundExpression = Me.BindAssignmentTarget(node.Left, diagnostics)
            Dim op2 As BoundExpression = Me.BindValue(node.Right, diagnostics)
            Debug.Assert(op1 IsNot Nothing)
            Debug.Assert(op2 IsNot Nothing)

            Dim expr As BoundAssignmentOperator
            If (op1.HasErrors OrElse op2.HasErrors) Then
                If Not op1.HasErrors AndAlso node.Kind = SyntaxKind.SimpleAssignmentStatement Then
                    expr = New BoundAssignmentOperator(node, op1,
                                                       ApplyImplicitConversion(node.Right, op1.Type, op2, diagnostics, False),
                                                       False,
                                                       op1.Type,
                                                       hasErrors:=True)
                Else
                    ' Try to reclassify op2 if we still can.
                    op2 = MakeRValueAndIgnoreDiagnostics(op2)

                    expr = New BoundAssignmentOperator(node, op1, op2, False, op1.Type, hasErrors:=True)
                End If
            ElseIf node.Kind = SyntaxKind.SimpleAssignmentStatement Then
                expr = BindAssignment(node, op1, op2, diagnostics)
            Else
                Dim operatorKind As BinaryOperatorKind
                Dim binaryTokenKind As SyntaxKind

                Select Case node.Kind
                    Case SyntaxKind.AddAssignmentStatement
                        operatorKind = BinaryOperatorKind.Add
                        binaryTokenKind = SyntaxKind.PlusToken
                    Case SyntaxKind.SubtractAssignmentStatement
                        operatorKind = BinaryOperatorKind.Subtract
                        binaryTokenKind = SyntaxKind.MinusToken
                    Case SyntaxKind.MultiplyAssignmentStatement
                        operatorKind = BinaryOperatorKind.Multiply
                        binaryTokenKind = SyntaxKind.AsteriskToken
                    Case SyntaxKind.DivideAssignmentStatement
                        operatorKind = BinaryOperatorKind.Divide
                        binaryTokenKind = SyntaxKind.SlashToken
                    Case SyntaxKind.IntegerDivideAssignmentStatement
                        operatorKind = BinaryOperatorKind.IntegerDivide
                        binaryTokenKind = SyntaxKind.BackslashToken
                    Case SyntaxKind.ExponentiateAssignmentStatement
                        operatorKind = BinaryOperatorKind.Power
                        binaryTokenKind = SyntaxKind.CaretToken
                    Case SyntaxKind.LeftShiftAssignmentStatement
                        operatorKind = BinaryOperatorKind.LeftShift
                        binaryTokenKind = SyntaxKind.LessThanLessThanToken
                    Case SyntaxKind.RightShiftAssignmentStatement
                        operatorKind = BinaryOperatorKind.RightShift
                        binaryTokenKind = SyntaxKind.GreaterThanGreaterThanToken
                    Case SyntaxKind.ConcatenateAssignmentStatement
                        operatorKind = BinaryOperatorKind.Concatenate
                        binaryTokenKind = SyntaxKind.AmpersandToken
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(node.Kind)
                End Select

                expr = BindCompoundAssignment(node, op1, op2, binaryTokenKind, operatorKind, diagnostics)
            End If

            Return New BoundExpressionStatement(node, expr.MakeCompilerGenerated())
        End Function

        Private Function BindMidAssignmentStatement(node As AssignmentStatementSyntax, diagnostics As DiagnosticBag) As BoundExpressionStatement
            Debug.Assert(node IsNot Nothing AndAlso node.Kind = SyntaxKind.MidAssignmentStatement AndAlso node.Left.Kind = SyntaxKind.MidExpression)

            Dim midExpression = DirectCast(node.Left, MidExpressionSyntax)
            Dim midArguments As SeparatedSyntaxList(Of ArgumentSyntax) = midExpression.ArgumentList.Arguments
            Debug.Assert(midArguments.Count = 2 OrElse midArguments.Count = 3)

            Dim targetSyntax = DirectCast(midArguments(0), SimpleArgumentSyntax).Expression
            Dim target As BoundExpression = BindAssignmentTarget(targetSyntax, diagnostics)

            Dim int32 As NamedTypeSymbol = GetSpecialType(SpecialType.System_Int32, midExpression, diagnostics)

            Dim startSyntax = DirectCast(midArguments(1), SimpleArgumentSyntax).Expression
            Dim start As BoundExpression = ApplyImplicitConversion(startSyntax, int32, BindValue(startSyntax, diagnostics), diagnostics)

            Dim lengthOpt As BoundExpression = Nothing

            If midArguments.Count > 2 Then
                Dim lengthSyntax = DirectCast(midArguments(2), SimpleArgumentSyntax).Expression
                lengthOpt = ApplyImplicitConversion(lengthSyntax, int32, BindValue(lengthSyntax, diagnostics), diagnostics)
            End If

            Dim stringType As NamedTypeSymbol = GetSpecialType(SpecialType.System_String, midExpression, diagnostics)

            VerifyTypeCharacterConsistency(midExpression.Mid, stringType, midExpression.Mid.GetTypeCharacter(), diagnostics)

            Dim source As BoundExpression = ApplyImplicitConversion(node.Right, stringType, BindValue(node.Right, diagnostics), diagnostics)

            ' We should be able to use the 'target' as an assignment target and as an RValue.
            ' Let's verify that
            Dim isError As Boolean
            Dim assignmentTarget As BoundExpression = AdjustAssignmentTarget(targetSyntax, target, diagnostics, isError)

            If Not isError Then
                isError = MakeRValue(target, diagnostics).HasErrors
            End If

            Dim targetType As TypeSymbol = assignmentTarget.Type
            Debug.Assert(targetType IsNot Nothing OrElse isError)

            Dim placeholder As BoundCompoundAssignmentTargetPlaceholder
            Dim original As BoundExpression

            If Not isError Then
                placeholder = New BoundCompoundAssignmentTargetPlaceholder(targetSyntax, targetType).MakeCompilerGenerated()
                original = ApplyImplicitConversion(targetSyntax, stringType, placeholder, diagnostics)
            Else
                placeholder = Nothing
                original = BadExpression(targetSyntax, stringType).MakeCompilerGenerated()
            End If

            ' SemanticModel should be able to find node corresponding to the MidExpressionSyntax, which has type String and no associated symbols. 
            ' Wrapping 'original' with BoundParenthesized node tied to MidExpressionSyntax should suffice for this purpose.
            Dim right As BoundExpression = New BoundMidResult(node,
                                                              New BoundParenthesized(midExpression, original, original.Type),
                                                              start, lengthOpt, source, stringType).MakeCompilerGenerated()

            If Not isError Then
                right = ApplyImplicitConversion(node, targetType, right, diagnostics).MakeCompilerGenerated()

                ' Either we have both "in" and "out" conversions or none.
                Debug.Assert((original.Kind = BoundKind.CompoundAssignmentTargetPlaceholder) = (right.Kind = BoundKind.MidResult) OrElse original.HasErrors OrElse right.HasErrors)
            End If

            target = target.SetGetSetAccessKindIfAppropriate()

            Return New BoundExpressionStatement(node, New BoundAssignmentOperator(node, target, placeholder, right, False,
                                                                                  Compilation.GetSpecialType(SpecialType.System_Void),
                                                                                  hasErrors:=isError).MakeCompilerGenerated())
        End Function

        Private Function BindAddRemoveHandlerStatement(node As AddRemoveHandlerStatementSyntax, diagnostics As DiagnosticBag) As BoundAddRemoveHandlerStatement
            Dim eventSymbol As eventSymbol = Nothing
            Dim actualEventAccess As BoundEventAccess = Nothing
            Dim eventAccess As BoundExpression = BindEventAccess(node.EventExpression, diagnostics, actualEventAccess, eventSymbol)

            Dim handlerExpression = BindValue(node.DelegateExpression, diagnostics)

            Dim isRemoveHandler As Boolean = node.Kind = SyntaxKind.RemoveHandlerStatement

            If isRemoveHandler Then
                Dim toCheck As BoundExpression = handlerExpression

                ' Dig through parenthesized.
                toCheck = toCheck.GetMostEnclosedParenthesizedExpression()

                If toCheck.Kind = BoundKind.UnboundLambda Then
                    ReportDiagnostic(diagnostics, node.DelegateExpression, ERRID.WRN_LambdaPassedToRemoveHandler)
                End If
            End If

            ' we may need to convert handler to the accessor parameter type.
            ' in a case where handler is a lambda or AddressOf we definitely need a conversion
            ' NOTE that handler may not be a delegate, but could have a user defined conversion to a delegate.
            Dim hasErrors As Boolean = True
            If eventSymbol IsNot Nothing Then
                Dim method = If(node.Kind = SyntaxKind.AddHandlerStatement, eventSymbol.AddMethod, eventSymbol.RemoveMethod)

                If method Is Nothing Then
                    If eventSymbol.DeclaringCompilation IsNot Me.Compilation Then
                        ReportDiagnostic(diagnostics, node.EventExpression, ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedEvent1, eventSymbol))
                    End If

                Else
                    Dim targetType = eventSymbol.Type

                    handlerExpression = ApplyImplicitConversion(node.DelegateExpression, targetType, handlerExpression, diagnostics)

                    If isRemoveHandler AndAlso
                        handlerExpression.Kind = BoundKind.DelegateCreationExpression AndAlso
                        node.DelegateExpression.Kind = SyntaxKind.AddressOfExpression Then

                        Dim delCreation = DirectCast(handlerExpression, BoundDelegateCreationExpression)
                        If delCreation.RelaxationLambdaOpt IsNot Nothing Then
                            Dim addrOffExpr = DirectCast(node.DelegateExpression, UnaryExpressionSyntax)

                            ReportDiagnostic(diagnostics, addrOffExpr.Operand, ERRID.WRN_RelDelegatePassedToRemoveHandler)
                        End If
                    End If

                    hasErrors = False

                    If method = Me.ContainingMember Then
                        If method.IsShared OrElse actualEventAccess.ReceiverOpt.IsMeReference Then
                            'Statement recursively calls the containing '{0}' for event '{1}'.
                            ReportDiagnostic(diagnostics,
                                             node,
                                             ERRID.WRN_RecursiveAddHandlerCall,
                                             node.AddHandlerOrRemoveHandlerKeyword.ToString,
                                             eventSymbol.Name)
                        End If
                    End If

                    If Not targetType.IsDelegateType() Then
                        If eventSymbol.DeclaringCompilation IsNot Me.Compilation AndAlso TypeOf targetType IsNot MissingMetadataTypeSymbol Then
                            ReportDiagnostic(diagnostics, node.EventExpression, ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedEvent1, eventSymbol))
                        End If

                        hasErrors = True
                    Else
                        Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                        Dim accessThroughType = GetAccessThroughType(actualEventAccess.ReceiverOpt)
                        If Not Me.IsAccessible(method, useSiteDiagnostics, accessThroughType) Then
                            Debug.Assert(eventSymbol.DeclaringCompilation IsNot Me.Compilation)
                            ReportDiagnostic(diagnostics, node.EventExpression, GetInaccessibleErrorInfo(method))
                        End If

                        diagnostics.Add(node.EventExpression, useSiteDiagnostics)

                        Dim badShape As Boolean = False
                        Dim useSiteError As DiagnosticInfo = Nothing

                        ' Decrease noise in diagnostics, if event is "bad", we already complained about it. 
                        If eventSymbol.GetUseSiteErrorInfo() Is Nothing Then
                            useSiteError = method.GetUseSiteErrorInfo()
                        End If

                        If useSiteError IsNot Nothing Then
                            Debug.Assert(eventSymbol.DeclaringCompilation IsNot Me.Compilation)
                            ReportDiagnostic(diagnostics, node.EventExpression, useSiteError)
                            hasErrors = True

                        ElseIf method.ParameterCount <> 1 OrElse method.Parameters(0).IsByRef Then
                            badShape = True

                        ElseIf eventSymbol.IsWindowsRuntimeEvent Then
                            Dim tokenType As NamedTypeSymbol = Me.Compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken)

                            If node.Kind = SyntaxKind.AddHandlerStatement Then
                                If Not method.Parameters(0).Type.IsSameTypeIgnoringAll(targetType) OrElse
                                   Not method.ReturnType.IsSameTypeIgnoringAll(tokenType) Then
                                    badShape = True
                                End If
                            ElseIf Not method.Parameters(0).Type.IsSameTypeIgnoringAll(tokenType) OrElse Not method.IsSub Then
                                badShape = True
                            End If

                        ElseIf Not method.Parameters(0).Type.IsSameTypeIgnoringAll(targetType) Then
                            badShape = True
                        End If

                        If badShape Then
                            If eventSymbol.DeclaringCompilation IsNot Me.Compilation Then
                                ReportDiagnostic(diagnostics, node.EventExpression, ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, method))
                            End If

                            hasErrors = True
                        End If
                    End If
                End If
            End If

            If node.Kind = SyntaxKind.AddHandlerStatement Then
                Return New BoundAddHandlerStatement(node, eventAccess, handlerExpression, hasErrors:=hasErrors)
            Else
                Return New BoundRemoveHandlerStatement(node, eventAccess, handlerExpression, hasErrors:=hasErrors)
            End If
        End Function

        Private Function BindEventAccess(node As ExpressionSyntax,
                                         diagnostics As DiagnosticBag,
                                         <Out()> ByRef actualEventAccess As BoundEventAccess,
                                         <Out()> ByRef eventSymbol As EventSymbol) As BoundExpression

            ' event must be a simple name that could be qualified and perhaps parenthesized
            ' Examples:  goo , (goo) , (bar.goo) , baz.moo(of T).goo
            Dim notParenthesizedSyntax = node
            While notParenthesizedSyntax.Kind = SyntaxKind.ParenthesizedExpression
                notParenthesizedSyntax = DirectCast(notParenthesizedSyntax, ParenthesizedExpressionSyntax).Expression
            End While

            Dim notQualifiedSyntax = notParenthesizedSyntax
            If notQualifiedSyntax.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                notQualifiedSyntax = DirectCast(notQualifiedSyntax, MemberAccessExpressionSyntax).Name
            End If

            If notQualifiedSyntax.Kind <> SyntaxKind.IdentifierName Then
                ReportDiagnostic(diagnostics, notQualifiedSyntax, ERRID.ERR_AddOrRemoveHandlerEvent)

                Dim ignoreDiagnostics = DiagnosticBag.GetInstance()
                Dim errorRecovery As BoundExpression = BindRValue(node, ignoreDiagnostics)
                ignoreDiagnostics.Free()

                Return errorRecovery
            End If

            'NOTE this may bind to extension methods. 
            'It could be unnecessary perf hit, but it will happen only in error cases (event not found)
            'so is not a big concern.
            Dim result As BoundExpression

            ' when binding a simple name event we know that it must be a member since methods do not declare events
            If notParenthesizedSyntax.Kind = SyntaxKind.IdentifierName Then
                Dim simpleNameSyntax As simpleNameSyntax = DirectCast(notParenthesizedSyntax, IdentifierNameSyntax)
                result = BindSimpleName(simpleNameSyntax, False, diagnostics, skipLocalsAndParameters:=True)
            Else
                result = BindExpression(node, isInvocationOrAddressOf:=False, isOperandOfConditionalBranch:=False, eventContext:=True, diagnostics:=diagnostics)
            End If

            Debug.Assert(result IsNot Nothing)

            ' Dev10 allows parenthesizing of the event
            Dim notParenthesized = result.GetMostEnclosedParenthesizedExpression()

            If notParenthesized.Kind = BoundKind.EventAccess Then
                actualEventAccess = DirectCast(notParenthesized, BoundEventAccess)
                eventSymbol = actualEventAccess.EventSymbol
            Else
                ' TODO: Should try to report ERR_NameNotEvent2 without making notParenthesized an RValue,
                '       it might cause errors for write-only property, etc.
                notParenthesized = MakeRValue(notParenthesized, diagnostics)

                ' this is not an event. Add diagnostics if node is not already bad
                ' NOTE that we are not marking the node as bad if it is not
                '      in such case there is nothing wrong with the event access node.
                '      However since we cannot provide event symbol, the AddRemovehandler
                '      node will be marked as HasErrors.
                If Not notParenthesized.HasErrors Then
                    Dim name = DirectCast(notQualifiedSyntax, IdentifierNameSyntax).Identifier.ValueText
                    Dim exprSymbol = notParenthesized.ExpressionSymbol
                    Dim container = If(exprSymbol IsNot Nothing, exprSymbol.ContainingSymbol, Compilation.GetSpecialType(SpecialType.System_Object))
                    ReportDiagnostic(diagnostics, notQualifiedSyntax, ERRID.ERR_NameNotEvent2, name, container)
                End If
            End If

            Return result
        End Function

        Private Function BindRaiseEventStatement(node As RaiseEventStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim hasErrors = False

            Dim target As BoundExpression = BindSimpleName(node.Name, False, diagnostics, skipLocalsAndParameters:=True)

            If target.Kind <> BoundKind.EventAccess Then
                If Not target.HasErrors Then
                    ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_NameNotEvent2, node.Name.ToString, Me.ContainingType)
                End If

                Return New BoundBadStatement(node, ImmutableArray.Create(Of BoundNode)(target), True)
            End If

            Dim targetAsEvent = DirectCast(target, BoundEventAccess)

            ' Get the bound arguments and the argument names.
            Dim boundArguments As ImmutableArray(Of BoundExpression) = Nothing
            Dim argumentNames As ImmutableArray(Of String) = Nothing
            Dim argumentNamesLocations As ImmutableArray(Of Location) = Nothing

            BindArgumentsAndNames(node.ArgumentList, boundArguments, argumentNames, argumentNamesLocations, diagnostics)

            Dim eventSym = targetAsEvent.EventSymbol
            Dim receiver As BoundExpression
            Dim fireMethod As MethodSymbol

            If eventSym.HasAssociatedField Then
                ' field is not nothing when event IsFieldLike
                Dim eventField = eventSym.AssociatedField
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                If Not IsAccessible(eventField, useSiteDiagnostics, Me.ContainingType) Then
                    ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_CantRaiseBaseEvent)
                    hasErrors = True
                End If

                diagnostics.Add(node.Name, useSiteDiagnostics)

                Debug.Assert(TypeSymbol.Equals(targetAsEvent.Type, eventField.Type, TypeCompareKind.ConsiderEverything) OrElse eventSym.IsWindowsRuntimeEvent, "non-WinRT event should have the same type as its backing field")

                receiver = New BoundFieldAccess(node.Name,
                                                targetAsEvent.ReceiverOpt,
                                                eventField,
                                                False,
                                                eventField.Type).MakeCompilerGenerated

                Dim eventType = TryCast(eventSym.Type, NamedTypeSymbol)

                ' Detect circumstances where we can't continue.
                If eventType Is Nothing OrElse eventType.DelegateInvokeMethod Is Nothing Then

                    ' If we have a delegate type and it has no Invoke method, then we should give a diagnostic.
                    ' Dev10 gives no diagnostics here, but we should.
                    ' If something else went wrong (i.e. we don't have a delegate type), then a diagnostic was reported elsewhere.
                    If eventType IsNot Nothing AndAlso eventType.IsDelegateType Then
                        ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_EventTypeNotDelegate)
                    End If

                    Return New BoundBadStatement(node, StaticCast(Of BoundNode).From(boundArguments).Add(target), True)
                End If

                fireMethod = eventType.DelegateInvokeMethod

                If ReportUseSiteError(diagnostics, node.Name, fireMethod) Then
                    hasErrors = True
                End If

                If Not fireMethod.IsSub Then
                    ' // Something is bad somewhere.
                    ' Dev10 gives no diagnostics here, but we should.
                    ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_EventsCantBeFunctions)
                    Return New BoundBadStatement(node, StaticCast(Of BoundNode).From(boundArguments).Add(target), True)
                End If
            Else
                receiver = targetAsEvent.ReceiverOpt
                fireMethod = eventSym.RaiseMethod

                If fireMethod Is Nothing Then
                    ' // Something is bad somewhere.
                    ' Dev10 gives no diagnostics here, but we should.
                    ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_MissingRaiseEventDef1, node.Name.ToString)
                    Return New BoundBadStatement(node, StaticCast(Of BoundNode).From(boundArguments).Add(target), True)
                End If

                If ReportUseSiteError(diagnostics, node.Name, fireMethod) Then
                    hasErrors = True
                End If

                If Not fireMethod.IsSub Then
                    ' // Something is bad somewhere.
                    ' Dev10 gives no diagnostics here, but we should.
                    ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_EventsCantBeFunctions)
                    Return New BoundBadStatement(node, StaticCast(Of BoundNode).From(boundArguments).Add(target), True)
                End If

                If Not TypeSymbol.Equals(fireMethod.ContainingType, Me.ContainingType, TypeCompareKind.ConsiderEverything) Then
                    ' Re: Dev10
                    ' // UNDONE: harishk - note that this is different from the check for an
                    ' // accessible event field for non-block events. This is because there
                    ' // is a bug for non-block events which contrary to the spec does allow
                    ' // base class events to be raised in some scenarios. Sent email to
                    ' // paulv and amandas to check if we can update all of this according
                    ' // to spec. After a final answer is received, we will need to make
                    ' // both consistent. More work is required if we need to make block
                    ' // events' behavior be the same as that of non-block events. Also for
                    ' // both cases, it would be hard to get the raising of base events
                    ' // working for metadata events.
                    '
                    ' 'RaiseEvent' definition missing for event '{0}'.
                    ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_CantRaiseBaseEvent, node.Name.ToString)
                    Return New BoundBadStatement(node, StaticCast(Of BoundNode).From(boundArguments).Add(target), True)
                End If
            End If

            Debug.Assert(fireMethod.IsSub, "we got this far, we must have a valid fireMethod")

            If fireMethod = Me.ContainingMember Then
                If fireMethod.IsShared OrElse receiver.IsMeReference Then
                    'Statement recursively calls the containing '{0}' for event '{1}'.
                    ReportDiagnostic(diagnostics,
                                     node,
                                     ERRID.WRN_RecursiveAddHandlerCall,
                                     node.RaiseEventKeyword.ToString,
                                     eventSym.Name)
                End If
            End If

            ' eventDelegateField.Invoke
            Dim methodGroup = New BoundMethodGroup(node,
                                                   Nothing,
                                                   ImmutableArray.Create(fireMethod),
                                                   LookupResultKind.Good,
                                                   receiver,
                                                   QualificationKind.QualifiedViaValue).MakeCompilerGenerated()

            'NOTE: Dev10 allows and ignores type characters on the event here.
            Dim invocation = BindInvocationExpression(node,
                                                      node.Name,
                                                      TypeCharacter.None,
                                                      methodGroup,
                                                      boundArguments,
                                                      argumentNames,
                                                      diagnostics,
                                                      callerInfoOpt:=Nothing,
                                                      representCandidateInDiagnosticsOpt:=eventSym).MakeCompilerGenerated

            Return New BoundRaiseEventStatement(node, eventSym, invocation, hasErrors)
        End Function

        Private Function BindExpressionStatement(statement As ExpressionStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement

            Dim expression = statement.Expression

            Dim boundExpression As BoundExpression

            Select Case expression.Kind
                Case SyntaxKind.InvocationExpression,
                     SyntaxKind.ConditionalAccessExpression
                    boundExpression = BindInvocationExpressionAsStatement(expression, diagnostics)

                Case SyntaxKind.AwaitExpression
                    boundExpression = BindAwait(DirectCast(expression, AwaitExpressionSyntax), diagnostics, bindAsStatement:=True)
                Case Else
                    ' TODO(ADGreen): This case covers top-level expressions in interactive.
                    ' Otherwise it should be an error.
                    boundExpression = BindRValue(expression, diagnostics)
            End Select

            WarnOnUnobservedCallThatReturnsAnAwaitable(statement, boundExpression, diagnostics)

            Return New BoundExpressionStatement(statement, boundExpression)
        End Function

        Private Sub WarnOnUnobservedCallThatReturnsAnAwaitable(statement As ExpressionStatementSyntax, boundExpression As BoundExpression, diagnostics As DiagnosticBag)
            If boundExpression.Kind = BoundKind.ConditionalAccess Then
                WarnOnUnobservedCallThatReturnsAnAwaitable(statement, DirectCast(boundExpression, BoundConditionalAccess).AccessExpression, diagnostics)
                Return
            End If

            If Not boundExpression.HasErrors AndAlso
                           Not boundExpression.Kind = BoundKind.AwaitOperator AndAlso
                           Not boundExpression.Type.IsErrorType() AndAlso
                           Not boundExpression.Type.IsVoidType() AndAlso
                           Not boundExpression.Type.IsObjectType() Then

                ' Check if we should warn for an unobserved call that returns an awaitable value.

                ' We will show a warning:
                '   1. In any method, when invoking a method that is marked
                '      "Async" and returns a type other than void. Note that
                '      this requires the caller and callee to be in the same
                '      compilation unit, as "Async" is not propagated into
                '      metadata.
                '   2. In any method, when invoking a method that returns one
                '      of the Windows Runtime async types:
                '        IAsyncAction
                '        IAsyncActionWithProgress(Of T)
                '        IAsyncOperation(Of T)
                '        IAsyncOperationWithProgress(Of T, U)
                '   3. In an async method, when invoking a method that returns
                '      any awaitable type.

                Dim warn As Boolean = False

                If boundExpression.Kind = BoundKind.Call Then
                    Dim [call] = DirectCast(boundExpression, BoundCall)
                    ' 1.
                    warn = [call].Method.IsAsync AndAlso [call].Method.ContainingAssembly Is Me.Compilation.Assembly
                End If

                If Not warn Then
                    ' 2. 
                    If IsOrInheritsFromOrImplementsInterface(boundExpression.Type, WellKnownType.Windows_Foundation_IAsyncAction, useSiteDiagnostics:=Nothing) OrElse
                       IsOrInheritsFromOrImplementsInterface(boundExpression.Type, WellKnownType.Windows_Foundation_IAsyncActionWithProgress_T, useSiteDiagnostics:=Nothing) OrElse
                       IsOrInheritsFromOrImplementsInterface(boundExpression.Type, WellKnownType.Windows_Foundation_IAsyncOperation_T, useSiteDiagnostics:=Nothing) OrElse
                       IsOrInheritsFromOrImplementsInterface(boundExpression.Type, WellKnownType.Windows_Foundation_IAsyncOperationWithProgress_T2, useSiteDiagnostics:=Nothing) Then
                        warn = True

                    ElseIf IsInAsyncContext() Then
                        ' 3.
                        Dim diagBag = DiagnosticBag.GetInstance()

                        If Not BindAwait(statement, boundExpression, diagBag, bindAsStatement:=True).HasErrors AndAlso Not diagBag.HasAnyErrors Then
                            warn = True
                        End If

                        diagBag.Free()
                    End If
                End If

                If warn Then
                    ReportDiagnostic(diagnostics, statement, ERRID.WRN_UnobservedAwaitableExpression)
                End If
            End If
        End Sub

        Private Function IsOrInheritsFromOrImplementsInterface(derivedType As TypeSymbol, interfaceType As WellKnownType, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
            Dim type As NamedTypeSymbol = Compilation.GetWellKnownType(interfaceType)
            Return Not type.IsErrorType() AndAlso type.IsInterfaceType() AndAlso
                   IsOrInheritsFromOrImplementsInterface(derivedType, type, useSiteDiagnostics)
        End Function

        Private Function BindPrintStatement(printStmt As PrintStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim boundExpression As BoundExpression = BindRValue(printStmt.Expression, diagnostics)
            Return New BoundExpressionStatement(printStmt, boundExpression)
        End Function

        Private Function BindCallStatement(callStmt As CallStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim boundInvocation As BoundExpression = BindInvocationExpressionAsStatement(callStmt.Invocation, diagnostics)

            Return New BoundExpressionStatement(callStmt, boundInvocation)
        End Function

        Private Function BindInvocationExpressionAsStatement(expression As ExpressionSyntax, diagnostics As DiagnosticBag) As BoundExpression
            Return ReclassifyInvocationExpressionAsStatement(BindExpression(expression, diagnostics), diagnostics)
        End Function

        Friend Function ReclassifyInvocationExpressionAsStatement(boundInvocation As BoundExpression, diagnostics As DiagnosticBag) As BoundExpression
            Select Case boundInvocation.Kind
                Case BoundKind.PropertyAccess
                    boundInvocation = MakeRValue(boundInvocation, diagnostics)

                    '  specially for properties being called in Call statement context
                    If Not boundInvocation.HasErrors Then
                        ReportDiagnostic(diagnostics, boundInvocation.Syntax, ERRID.ERR_PropertyAccessIgnored)
                    End If

                Case BoundKind.LateMemberAccess
                    ' matches setting ExprResultNotNeeded flag in StatementSemantics/Semantics::InterpretCall
                    boundInvocation = DirectCast(boundInvocation, BoundLateMemberAccess).SetAccessKind(LateBoundAccessKind.Call)

                Case BoundKind.LateInvocation
                    ' matches setting ExprResultNotNeeded flag in StatementSemantics/Semantics::InterpretCall
                    Dim lateInvocation = DirectCast(boundInvocation, BoundLateInvocation).SetAccessKind(LateBoundAccessKind.Call)
                    boundInvocation = lateInvocation

                    '  specially for properties being called in Call statement context
                    If Not lateInvocation.HasErrors AndAlso
                        TryCast(lateInvocation.MethodOrPropertyGroupOpt, BoundPropertyGroup) IsNot Nothing Then

                        ReportDiagnostic(diagnostics, boundInvocation.Syntax, ERRID.ERR_PropertyAccessIgnored)
                    End If

                Case BoundKind.ConditionalAccess
                    Debug.Assert(boundInvocation.Type Is Nothing)
                    Dim conditionalAccess = DirectCast(boundInvocation, BoundConditionalAccess)
                    boundInvocation = conditionalAccess.Update(conditionalAccess.Receiver,
                                                               conditionalAccess.Placeholder,
                                                               ReclassifyInvocationExpressionAsStatement(conditionalAccess.AccessExpression, diagnostics),
                                                               GetSpecialType(SpecialType.System_Void, conditionalAccess.Syntax, diagnostics))
            End Select

            Return boundInvocation
        End Function

        Private Function BindSingleLineIfStatement(node As SingleLineIfStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Debug.Assert(node IsNot Nothing)

            Dim condition As BoundExpression
            Dim consequence As BoundBlock
            Dim alternative As BoundStatement = Nothing

            condition = BindBooleanExpression(node.Condition, diagnostics)
            consequence = BindBlock(node, node.Statements, diagnostics).MakeCompilerGenerated()
            If node.ElseClause IsNot Nothing Then
                alternative = BindBlock(node.ElseClause, node.ElseClause.Statements, diagnostics)
            End If

            Return New BoundIfStatement(node, condition, consequence, alternative)
        End Function

        Private Function BindMultiLineIfBlock(node As MultiLineIfBlockSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Debug.Assert(node IsNot Nothing)

            ' We need to bind the conditions and blocks in lexical order (so that Option Explicit binding
            ' works right), but build the bound tree in reverse order.

            Dim blocks As ArrayBuilder(Of BoundStatement) = ArrayBuilder(Of BoundStatement).GetInstance()
            Dim conditions As ArrayBuilder(Of BoundExpression) = ArrayBuilder(Of BoundExpression).GetInstance()

            conditions.Add(BindBooleanExpression(node.IfStatement.Condition, diagnostics))
            blocks.Add(BindBlock(node, node.Statements, diagnostics).MakeCompilerGenerated())

            For i = 0 To node.ElseIfBlocks.Count - 1
                Dim elseIfBlock = node.ElseIfBlocks(i)
                conditions.Add(BindBooleanExpression(elseIfBlock.ElseIfStatement.Condition, diagnostics))
                blocks.Add(BindBlock(elseIfBlock, elseIfBlock.Statements, diagnostics).MakeCompilerGenerated())
            Next

            Dim currentAlternative As BoundStatement = Nothing

            If node.ElseBlock IsNot Nothing Then
                currentAlternative = BindBlock(node.ElseBlock, node.ElseBlock.Statements, diagnostics)
            End If

            For i = conditions.Count - 1 To 0 Step -1
                Dim syntax As VisualBasicSyntaxNode
                If i = 0 Then
                    syntax = node
                Else
                    syntax = node.ElseIfBlocks(i - 1)
                End If

                currentAlternative = New BoundIfStatement(syntax, conditions(i), blocks(i), currentAlternative)
            Next

            blocks.Free()
            conditions.Free()

            Return currentAlternative
        End Function

        Private Function BindDoLoop(node As DoLoopBlockSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Debug.Assert(node IsNot Nothing)

            Dim topCondition As BoundExpression = Nothing
            Dim bottomCondition As BoundExpression = Nothing
            Dim isTopUntil As Boolean = False
            Dim isBottomUntil As Boolean = False

            ' Bind the top condition, if any.
            Dim topConditionSyntax = node.DoStatement.WhileOrUntilClause
            If topConditionSyntax IsNot Nothing Then
                topCondition = BindBooleanExpression(topConditionSyntax.Condition, diagnostics)
                isTopUntil = (topConditionSyntax.Kind = SyntaxKind.UntilClause)
            End If

            ' Get the binder for the body of the loop. This defines the break and continue labels.
            Dim loopBodyBinder = GetBinder(DirectCast(node, VisualBasicSyntaxNode))

            ' Bind the body of the loop.
            Dim loopBody As BoundBlock = loopBodyBinder.BindBlock(node, node.Statements, diagnostics).MakeCompilerGenerated()

            ' Bind the bottom condition, if any.
            Dim bottomConditionSyntax = node.LoopStatement.WhileOrUntilClause
            If bottomConditionSyntax IsNot Nothing Then
                bottomCondition = BindBooleanExpression(bottomConditionSyntax.Condition, diagnostics)
                isBottomUntil = (bottomConditionSyntax.Kind = SyntaxKind.UntilClause)
            End If

            ' Create the bound node.
            Return New BoundDoLoopStatement(node, topCondition, bottomCondition, isTopUntil, isBottomUntil, loopBody,
                                            continueLabel:=loopBodyBinder.GetContinueLabel(SyntaxKind.ContinueDoStatement),
                                            exitLabel:=loopBodyBinder.GetExitLabel(SyntaxKind.ExitDoStatement),
                                            hasErrors:=topCondition IsNot Nothing AndAlso bottomCondition IsNot Nothing)
        End Function

        Private Function BindWhileBlock(node As WhileBlockSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Debug.Assert(node IsNot Nothing)

            ' Bind condition
            Dim condition As BoundExpression = BindBooleanExpression(node.WhileStatement.Condition, diagnostics)

            ' Get the binder for the body of the loop. This defines the break and continue labels.
            Dim loopBodyBinder = GetBinder(node)

            ' Bind the body of the loop.
            Dim loopBody As BoundBlock = loopBodyBinder.BindBlock(node, node.Statements, diagnostics).MakeCompilerGenerated()

            ' Create the bound node.
            Return New BoundWhileStatement(node, condition, loopBody,
                                            continueLabel:=loopBodyBinder.GetContinueLabel(SyntaxKind.ContinueWhileStatement),
                                            exitLabel:=loopBodyBinder.GetExitLabel(SyntaxKind.ExitWhileStatement))
        End Function

        Public Function BindForToBlock(node As ForOrForEachBlockSyntax, diagnostics As DiagnosticBag) As BoundStatement
            ' For statement has its own binding scope since it may introduce iteration variable
            ' that is visible through the entire For block. It also needs to support Continue/Exit
            ' Interestingly, control variable is in scope when Limit and Step or the collection are bound,
            ' but initialized after Limit and Step or collection are evaluated...
            Dim loopBinder = Me.GetBinder(node)
            Debug.Assert(loopBinder IsNot Nothing)

            Dim declaredOrInferredLocalOpt As LocalSymbol = Nothing
            Dim isInferredLocal As Boolean = False
            Dim controlVariable As BoundExpression = Nothing
            Dim hasErrors As Boolean = False

            ' bind common parts of a for block
            hasErrors = loopBinder.BindForBlockParts(node,
                                                     node.ForOrForEachStatement.ControlVariable,
                                                     declaredOrInferredLocalOpt,
                                                     controlVariable,
                                                     isInferredLocal,
                                                     diagnostics)

            ' return the specific BoundForToStatement
            Return loopBinder.BindForToBlockParts(node,
                                                  declaredOrInferredLocalOpt,
                                                  controlVariable,
                                                  isInferredLocal,
                                                  hasErrors,
                                                  diagnostics)
        End Function

        Public Function BindForEachBlock(node As ForOrForEachBlockSyntax, diagnostics As DiagnosticBag) As BoundStatement
            ' For statement has its own binding scope since it may introduce iteration variable
            ' that is visible through the entire For block. It also needs to support Continue/Exit
            ' Interestingly, control variable is in scope when Limit and Step or the collection are bound,
            ' but initialized after Limit and Step or collection are evaluated...
            Dim loopBinder = Me.GetBinder(node)
            Debug.Assert(loopBinder IsNot Nothing)

            Dim declaredOrInferredLocalOpt As LocalSymbol = Nothing
            Dim isInferredLocal As Boolean = False
            Dim controlVariable As BoundExpression = Nothing
            Dim loopBody As BoundBlock = Nothing
            Dim nextVariables As ImmutableArray(Of BoundExpression) = Nothing
            Dim hasErrors As Boolean = False

            ' bind common parts of a for block
            hasErrors = loopBinder.BindForBlockParts(node,
                                                     DirectCast(node.ForOrForEachStatement, ForEachStatementSyntax).ControlVariable,
                                                     declaredOrInferredLocalOpt,
                                                     controlVariable,
                                                     isInferredLocal,
                                                     diagnostics)

            ' return the specific BoundForEachStatement
            Return loopBinder.BindForEachBlockParts(node,
                                                    declaredOrInferredLocalOpt,
                                                    controlVariable,
                                                    isInferredLocal,
                                                    diagnostics)
        End Function

        ''' <summary>
        ''' Binds all the common part for ForTo and ForEach loops except the loop body and the next variables.
        ''' </summary>
        ''' <param name="node">The node.</param>
        ''' <param name="controlVariableSyntax">The control variable syntax.</param>
        ''' <param name="declaredOrInferredLocalOpt">The declared or inferred local symbol.</param>
        ''' <param name="controlVariable">The control variable.</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        ''' <returns>true if there were errors; otherwise false</returns>
        Private Function BindForBlockParts(node As ForOrForEachBlockSyntax,
                                           controlVariableSyntax As VisualBasicSyntaxNode,
                                           <Out()> ByRef declaredOrInferredLocalOpt As LocalSymbol,
                                           <Out()> ByRef controlVariable As BoundExpression,
                                           <Out()> ByRef isInferredLocal As Boolean,
                                           diagnostics As DiagnosticBag) As Boolean
            ' Bind control variable

            ' There are two forms of control variables -
            ' 1) Control variable declared within the ForStatement
            ' 2) Expression reference that points to something (local, field...) declared outside of the loop.

            Dim hasErrors As Boolean = False
            declaredOrInferredLocalOpt = Nothing
            isInferredLocal = False

            If controlVariableSyntax.Kind = SyntaxKind.VariableDeclarator Then
                ' This case handles control variables for
                ' for x as integer = 0 to n
                ' for each x? in collection

                Dim declaratorSyntax = DirectCast(controlVariableSyntax, VariableDeclaratorSyntax)
                hasErrors = Not VerifyForControlVariableDeclaration(declaratorSyntax, diagnostics)

                ' 10.9.3
                ' 1. If the loop control variable is an identifier with an As clause, the identifier defines a new local variable of 
                ' the type specified in the As clause, scoped to the entire For Each loop.
                Dim asClauseOpt = declaratorSyntax.AsClause
                Dim names = declaratorSyntax.Names

                Debug.Assert(declaratorSyntax.Initializer Is Nothing, "should not have initializer")
                Debug.Assert(names.Count = 1, "should be exactly one control variable")

                Dim localDeclaration = BindVariableDeclaration(declaratorSyntax,
                                                           names(0),
                                                           asClauseOpt,
                                                           equalsValueOpt:=Nothing,
                                                           diagnostics:=diagnostics)
                declaredOrInferredLocalOpt = localDeclaration.LocalSymbol
                controlVariable = New BoundLocal(declaratorSyntax, declaredOrInferredLocalOpt, declaredOrInferredLocalOpt.Type)
            Else
                ' This case handles control variables for
                ' for i = 0 to n
                ' for each Me.MyField in collection

                ' if it is a simple identifier and Option Infer is on, this might be new local declaration with an
                ' inferred type.
                If controlVariableSyntax.Kind = SyntaxKind.IdentifierName Then
                    Dim identifier = DirectCast(controlVariableSyntax, IdentifierNameSyntax).Identifier
                    Dim name = identifier.ValueText
                    Dim result As LookupResult = LookupResult.GetInstance
                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    Lookup(result, name, 0, LookupOptions.AllMethodsOfAnyArity, useSiteDiagnostics)
                    diagnostics.Add(node, useSiteDiagnostics)

                    ' If a local symbol is found then check whether this local corresponds to this identifier.  If it does then
                    ' this is local is an inferred local for the for block.  This local does not have a type yet. Return the 
                    ' local and infer the type when the from/to or for-each collection expressions are available for binding.

                    If result.IsGood AndAlso
                        result.Symbols(0).Kind = SymbolKind.Local Then

                        Dim localSymbol = DirectCast(result.Symbols(0), LocalSymbol)
                        If localSymbol.IdentifierToken = identifier Then
                            ' This is an inferred local, we will need to compute its type.
                            isInferredLocal = True
                            declaredOrInferredLocalOpt = localSymbol
                        End If
                    End If

                    result.Free()
                End If

                ' it's something else than a simple name (e.g. qualified name ...), so we're going to bind it and 
                ' check if the bound node meets the expectation (e.g. is a field or a local)
                If Not isInferredLocal Then
                    If Not TryBindLoopControlVariable(controlVariableSyntax, controlVariable, diagnostics) Then
                        hasErrors = True
                    End If
                Else
                    controlVariable = Nothing
                End If

            End If

            ' we cannot bind the loop body and the next variables here, although these nodes are shared between for and for each
            ' or we loose diagnostics.

            Return hasErrors
        End Function

        ''' <summary>
        ''' Binds loop body and the next variables for ForTo and ForEach loops.
        ''' </summary>
        ''' <remarks>
        ''' The binding of the loop body and the next variables cannot happen before the local type inference has
        ''' completed, which happens in the specialized binding functions for foreach and for loops. Otherwise we would
        ''' loose the diagnostics from the type inference.
        ''' </remarks>
        ''' <param name="loopBody">The loop body.</param>
        ''' <param name="nextVariables">The next variables.</param>
        Private Sub BindForLoopBodyAndNextControlVariables(
            node As ForOrForEachBlockSyntax,
            <Out()> ByRef nextVariables As ImmutableArray(Of BoundExpression),
            <Out()> ByRef loopBody As BoundBlock,
            diagnostics As DiagnosticBag)

            ' Bind the body of the loop.
            loopBody = BindBlock(node, node.Statements, diagnostics).MakeCompilerGenerated()

            ' bind the variables of the next statement.

            ' if there is no next statement in this block, the array will be nothing,
            ' if there is a next statement without variables, the array will be empty, otherwise
            ' it contains all bound expression in declaration order.
            nextVariables = Nothing
            Dim endOptSyntax = node.NextStatement
            If endOptSyntax IsNot Nothing Then
                Dim controlVariableList As SeparatedSyntaxList(Of ExpressionSyntax) = endOptSyntax.ControlVariables
                If controlVariableList.IsEmpty Then
                    ' should be the most common case: a next without a variable
                    nextVariables = ImmutableArray(Of BoundExpression).Empty
                ElseIf controlVariableList.Count = 1 Then
                    ' avoid creating an ArrayBuilder for the second most common case, where the control variable 
                    ' of the current loop is used.
                    Dim boundVariable = BindExpression(controlVariableList(0), diagnostics)
                    boundVariable = ReclassifyAsValue(boundVariable, diagnostics)
                    nextVariables = ImmutableArray.Create(boundVariable)
                Else
                    Dim nextVariableCount = controlVariableList.Count
                    Dim nextVariableBuilder As ArrayBuilder(Of BoundExpression) = ArrayBuilder(Of BoundExpression).GetInstance
                    Dim currentBinder = Me
                    For nextVariableIndex = 0 To nextVariableCount - 1
                        Dim boundVariable = currentBinder.BindExpression(controlVariableList(nextVariableIndex), diagnostics)
                        boundVariable = ReclassifyAsValue(boundVariable, diagnostics)
                        nextVariableBuilder.Add(boundVariable)

                        ' due to incremental binding, there might be other binders in between Me and the containing 
                        ' StatementListBinder.
                        ' each next variable will be bound with the responsible binder for this for loop
                        Do
                            currentBinder = currentBinder.ContainingBinder
                        Loop While currentBinder IsNot Nothing AndAlso Not (TypeOf (currentBinder) Is StatementListBinder)

                        If currentBinder IsNot Nothing Then
                            currentBinder = currentBinder.ContainingBinder
                        End If

                        If Not TypeOf currentBinder Is ForOrForEachBlockBinder Then
                            ' this happens for broken code, e.g.
                            ' for each a in arr1
                            '   if goo() then
                            '     for each b in arr2
                            '     next b, a
                            '   end if
                            Exit For
                        End If
                    Next

                    nextVariables = nextVariableBuilder.ToImmutableAndFree
                End If
            End If
        End Sub

        Private Function BindForToBlockParts(
            node As ForOrForEachBlockSyntax,
            declaredOrInferredLocalOpt As LocalSymbol,
            controlVariableOpt As BoundExpression,
            isInferredLocal As Boolean,
            hasErrors As Boolean,
            diagnostics As DiagnosticBag
        ) As BoundForStatement
            Dim forStatement = DirectCast(node.ForOrForEachStatement, ForStatementSyntax)

            Dim initialValue As BoundExpression = Nothing
            Dim limit As BoundExpression = Nothing
            Dim stepValue As BoundExpression = Nothing

            If isInferredLocal Then
                Debug.Assert(declaredOrInferredLocalOpt IsNot Nothing)
                Debug.Assert(controlVariableOpt Is Nothing)

                Dim type = InferForFromToVariableType(declaredOrInferredLocalOpt,
                                                        forStatement.FromValue,
                                                        forStatement.ToValue,
                                                        forStatement.StepClause,
                                                        initialValue,
                                                        limit,
                                                        stepValue,
                                                        diagnostics)

                ' Now that we know the type go ahead and set it.

                Dim identifier = declaredOrInferredLocalOpt.IdentifierToken
                VerifyLocalSymbolNameAndSetType(declaredOrInferredLocalOpt, type, DirectCast(identifier.Parent, VisualBasicSyntaxNode), identifier, diagnostics)

                controlVariableOpt = New BoundLocal(forStatement.ControlVariable, declaredOrInferredLocalOpt, type)
            End If

            Dim targetType = controlVariableOpt.Type
            Dim targetTypeIsValid As Boolean = False

            If targetType IsNot Nothing AndAlso Not targetType.IsErrorType Then
                targetTypeIsValid = IsValidForControlVariableType(node, targetType.GetEnumUnderlyingTypeOrSelf(), diagnostics)
                hasErrors = (Not targetTypeIsValid) Or hasErrors
            End If

            ' Bind initial value
            If initialValue Is Nothing Then
                initialValue = BindValue(forStatement.FromValue, diagnostics)
            End If

            ' Bind limit
            If limit Is Nothing Then
                limit = BindValue(forStatement.ToValue, diagnostics)
            End If

            ' Bind step
            If stepValue Is Nothing Then
                Dim stepClause = forStatement.StepClause

                If stepClause IsNot Nothing Then
                    stepValue = BindValue(stepClause.StepValue, diagnostics)
                Else
                    ' Spec: "If the step value is omitted, it is implicitly the literal 1, converted to the type of the loop control variable. "
                    'If there is an error, a special handling required to explain where 1 came from.
                    stepValue = New BoundLiteral(node,
                                                 ConstantValue.Create(1),
                                                 Me.GetSpecialType(SpecialType.System_Int32, forStatement, diagnostics))
                    stepValue.SetWasCompilerGenerated()
                End If
            End If

            If targetTypeIsValid Then
                initialValue = ApplyImplicitConversion(initialValue.Syntax, targetType, initialValue, diagnostics)
                limit = ApplyImplicitConversion(limit.Syntax, targetType, limit, diagnostics)
                Dim stepValueBeforeConversion = stepValue
                stepValue = ApplyConversion(stepValue.Syntax, targetType, stepValue,
                                            isExplicit:=forStatement.StepClause Is Nothing,
                                            diagnostics:=diagnostics)

                If stepValue IsNot stepValueBeforeConversion AndAlso stepValue.Kind = BoundKind.Conversion AndAlso
                   forStatement.StepClause Is Nothing Then
                    stepValue.MakeCompilerGenerated()
                End If
            Else
                initialValue = MakeRValueAndIgnoreDiagnostics(initialValue)
                limit = MakeRValueAndIgnoreDiagnostics(limit)
                stepValue = MakeRValueAndIgnoreDiagnostics(stepValue)
            End If

            ' get special types that are used by the rewriters later on to report use site errors     
            ' TODO: update the types to the ones that get really used by the rewriter. In case of using an index of an user 
            ' defined type there is no need to get the integer type.
            Dim integerType = GetSpecialType(SpecialType.System_Int32, node, diagnostics)
            Dim booleanType = GetSpecialType(SpecialType.System_Boolean, node, diagnostics)

            Dim udfOperators As BoundForToUserDefinedOperators = Nothing
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            ' normalize initValue, limit and step to the common type
            If Not hasErrors Then
                If Not (initialValue.HasErrors OrElse limit.HasErrors OrElse stepValue.HasErrors) AndAlso
                   targetType.CanContainUserDefinedOperators(useSiteDiagnostics) Then
                    ' Bind user-defined operators that we need.
                    Dim syntax As VisualBasicSyntaxNode = node.ForOrForEachStatement

                    Dim leftOperandPlaceholder = New BoundRValuePlaceholder(syntax, targetType).MakeCompilerGenerated()
                    Dim rightOperandPlaceholder = New BoundRValuePlaceholder(syntax, targetType).MakeCompilerGenerated()

                    Dim addition As BoundUserDefinedBinaryOperator = BindForLoopUserDefinedOperator(syntax, BinaryOperatorKind.Add, leftOperandPlaceholder, rightOperandPlaceholder, diagnostics)
                    Dim subtraction As BoundUserDefinedBinaryOperator = BindForLoopUserDefinedOperator(syntax, BinaryOperatorKind.Subtract, leftOperandPlaceholder, rightOperandPlaceholder, diagnostics)

                    Dim lessThanOrEqual As BoundExpression = BindForLoopUserDefinedOperator(syntax, BinaryOperatorKind.LessThanOrEqual, leftOperandPlaceholder, rightOperandPlaceholder, diagnostics)

                    If lessThanOrEqual IsNot Nothing Then
                        lessThanOrEqual = ApplyImplicitConversion(syntax, booleanType, lessThanOrEqual, diagnostics, isOperandOfConditionalBranch:=True).MakeCompilerGenerated()
                    End If

                    Dim greaterThanOrEqual As BoundExpression = BindForLoopUserDefinedOperator(syntax, BinaryOperatorKind.GreaterThanOrEqual, leftOperandPlaceholder, rightOperandPlaceholder, diagnostics)

                    If greaterThanOrEqual IsNot Nothing Then
                        ' Suppress errors if we already reported them for LessThanOrEqual.
                        greaterThanOrEqual = ApplyImplicitConversion(syntax, booleanType, greaterThanOrEqual,
                                                                     If(lessThanOrEqual IsNot Nothing AndAlso lessThanOrEqual.HasErrors, New DiagnosticBag(), diagnostics),
                                                                     isOperandOfConditionalBranch:=True).MakeCompilerGenerated()
                    End If

                    If addition IsNot Nothing AndAlso subtraction IsNot Nothing AndAlso lessThanOrEqual IsNot Nothing AndAlso greaterThanOrEqual IsNot Nothing Then
                        udfOperators = New BoundForToUserDefinedOperators(syntax,
                                                                          leftOperandPlaceholder, rightOperandPlaceholder,
                                                                          addition, subtraction,
                                                                          lessThanOrEqual, greaterThanOrEqual)
                    Else
                        hasErrors = True
                    End If
                End If
            End If

            diagnostics.Add(node, useSiteDiagnostics)

            hasErrors = hasErrors OrElse
                        targetType.IsErrorType OrElse
                        initialValue.HasErrors OrElse
                        stepValue.HasErrors OrElse
                        controlVariableOpt.HasErrors

            ' Bind the loop body and the next variables 
            Dim loopBody As BoundBlock = Nothing
            Dim nextVariables As ImmutableArray(Of BoundExpression) = Nothing
            Me.BindForLoopBodyAndNextControlVariables(node, nextVariables, loopBody, diagnostics)

            ' Create the bound node.
            Return New BoundForToStatement(
                node,
                initialValue,
                limit,
                stepValue,
                CheckOverflow,
                udfOperators,
                declaredOrInferredLocalOpt,
                controlVariableOpt,
                loopBody,
                nextVariables,
                continueLabel:=GetContinueLabel(SyntaxKind.ContinueForStatement),
                exitLabel:=GetExitLabel(SyntaxKind.ExitForStatement),
                hasErrors:=hasErrors)
        End Function

        ''' <summary>
        ''' Can return Nothing in case of failure.
        ''' </summary>
        Private Function BindForLoopUserDefinedOperator(
            syntax As VisualBasicSyntaxNode,
            opCode As BinaryOperatorKind,
            left As BoundExpression,
            right As BoundExpression,
            diagnostics As DiagnosticBag
        ) As BoundUserDefinedBinaryOperator

            Debug.Assert(opCode = BinaryOperatorKind.Add OrElse opCode = BinaryOperatorKind.Subtract OrElse opCode = BinaryOperatorKind.LessThanOrEqual OrElse opCode = BinaryOperatorKind.GreaterThanOrEqual)

            Dim isRelational As Boolean = (opCode = BinaryOperatorKind.LessThanOrEqual OrElse opCode = BinaryOperatorKind.GreaterThanOrEqual)

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Dim userDefinedOperator As OverloadResolution.OverloadResolutionResult = OverloadResolution.ResolveUserDefinedBinaryOperator(left, right, opCode, Me, includeEliminatedCandidates:=False,
                                                                                                                                         useSiteDiagnostics:=useSiteDiagnostics)

            If diagnostics.Add(syntax, useSiteDiagnostics) Then
                ' Suppress additional diagnostics
                diagnostics = New DiagnosticBag()
            End If

            If userDefinedOperator.ResolutionIsLateBound OrElse Not userDefinedOperator.BestResult.HasValue Then
                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_ForLoopOperatorRequired2, left.Type, SyntaxFacts.GetText(OverloadResolution.GetOperatorTokenKind(opCode)))
                Return Nothing
            End If

            Dim bestCandidate As OverloadResolution.Candidate = userDefinedOperator.BestResult.Value.Candidate

            If Not bestCandidate.Parameters(0).Type.IsSameTypeIgnoringAll(left.Type) OrElse
               Not bestCandidate.Parameters(1).Type.IsSameTypeIgnoringAll(left.Type) OrElse
               (Not isRelational AndAlso Not bestCandidate.ReturnType.IsSameTypeIgnoringAll(left.Type)) Then

                If isRelational Then
                    ReportDiagnostic(diagnostics, syntax, ERRID.ERR_UnacceptableForLoopRelOperator2, bestCandidate.UnderlyingSymbol,
                                     If(bestCandidate.IsLifted,
                                        left.Type.GetNullableUnderlyingTypeOrSelf(),
                                        left.Type))
                Else
                    ReportDiagnostic(diagnostics, syntax, ERRID.ERR_UnacceptableForLoopOperator2, bestCandidate.UnderlyingSymbol,
                                     If(bestCandidate.IsLifted,
                                        left.Type.GetNullableUnderlyingTypeOrSelf(),
                                        left.Type))
                End If

                Return Nothing
            End If

            Dim result As BoundUserDefinedBinaryOperator = BindUserDefinedNonShortCircuitingBinaryOperator(syntax, opCode, left, right, userDefinedOperator, diagnostics).MakeCompilerGenerated()
            result.UnderlyingExpression.MakeCompilerGenerated()

            Return result
        End Function

        ' Verify that control variable can actually be used as a control variable
        Private Shared Function IsValidForControlVariableType(node As ForOrForEachBlockSyntax,
                                    targetType As TypeSymbol,
                                    diagnostics As DiagnosticBag) As Boolean

            ' if it's a nullable type, simply unwrap it (no recursion needed because nullables cannot be nested)
            If targetType.IsNullableType Then
                targetType = targetType.GetNullableUnderlyingType.GetEnumUnderlyingTypeOrSelf
            End If

            If targetType.IsNumericType Then
                Return True
            End If

            If targetType.IsObjectType Then
                Return True
            End If

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            If targetType.IsIntrinsicOrEnumType OrElse Not targetType.CanContainUserDefinedOperators(useSiteDiagnostics) Then
                diagnostics.Add(DirectCast(node.ForOrForEachStatement, ForStatementSyntax).ControlVariable, useSiteDiagnostics)
                ReportDiagnostic(diagnostics, DirectCast(node.ForOrForEachStatement, ForStatementSyntax).ControlVariable, ERRID.ERR_ForLoopType1, targetType)

                Return False
            End If

            diagnostics.Add(DirectCast(node.ForOrForEachStatement, ForStatementSyntax).ControlVariable, useSiteDiagnostics)

            Return True
        End Function

        Private Function BindForEachBlockParts(
            node As ForOrForEachBlockSyntax,
            declaredOrInferredLocalOpt As LocalSymbol,
            controlVariableOpt As BoundExpression,
            isInferredLocal As Boolean,
            diagnostics As DiagnosticBag
        ) As BoundForEachStatement
            Dim forEachStatement = DirectCast(node.ForOrForEachStatement, ForEachStatementSyntax)

            Dim currentType As TypeSymbol = Nothing
            Dim elementType As TypeSymbol = Nothing
            Dim isEnumerable As Boolean = False
            Dim needToDispose As Boolean = False
            Dim isOrInheritsFromOrImplementsIDisposable As Boolean = False

            Dim boundGetEnumeratorCall As BoundExpression = Nothing
            Dim boundEnumeratorPlaceholder As BoundLValuePlaceholder = Nothing
            Dim boundMoveNextCall As BoundExpression = Nothing
            Dim boundCurrentAccess As BoundExpression = Nothing
            Dim collectionPlaceholder As BoundRValuePlaceholder = Nothing

            Dim boundDisposeCondition As BoundExpression = Nothing
            Dim boundDisposeCast As BoundExpression = Nothing

            Dim boundCurrentConversion As BoundExpression = Nothing
            Dim boundCurrentPlaceholder As BoundRValuePlaceholder = Nothing

            Dim collection As BoundExpression = Nothing

            If isInferredLocal Then
                Debug.Assert(declaredOrInferredLocalOpt IsNot Nothing)
                Debug.Assert(controlVariableOpt Is Nothing)

                Dim type = InferForEachVariableType(declaredOrInferredLocalOpt,
                                                    forEachStatement.Expression,
                                                    collection,
                                                    currentType,
                                                    elementType,
                                                    isEnumerable,
                                                    boundGetEnumeratorCall,
                                                    boundEnumeratorPlaceholder,
                                                    boundMoveNextCall,
                                                    boundCurrentAccess,
                                                    collectionPlaceholder,
                                                    needToDispose,
                                                    isOrInheritsFromOrImplementsIDisposable,
                                                    diagnostics)

                ' Now that we know the type go ahead and set it.

                Dim identifier = declaredOrInferredLocalOpt.IdentifierToken
                VerifyLocalSymbolNameAndSetType(declaredOrInferredLocalOpt, type, DirectCast(identifier.Parent, VisualBasicSyntaxNode), identifier, diagnostics)

                controlVariableOpt = New BoundLocal(forEachStatement.ControlVariable, declaredOrInferredLocalOpt, type)
            End If

            If collection Is Nothing Then
                ' bind the expression that describes the collection to iterate over
                collection = BindValue(forEachStatement.Expression, diagnostics)

                If Not collection.IsLValue AndAlso Not collection.IsNothingLiteral Then
                    collection = MakeRValue(collection, diagnostics)
                End If

                ' check if the collection is valid for a for each statement
                collection = InterpretForEachStatementCollection(collection,
                                                                 currentType,
                                                                 elementType,
                                                                 isEnumerable,
                                                                 boundGetEnumeratorCall,
                                                                 boundEnumeratorPlaceholder,
                                                                 boundMoveNextCall,
                                                                 boundCurrentAccess,
                                                                 collectionPlaceholder,
                                                                 needToDispose,
                                                                 isOrInheritsFromOrImplementsIDisposable,
                                                                 diagnostics)
            End If

            '
            ' Now we're already creating some bound nodes that will be used in the local rewriter to report possible
            ' diagnostics early and to reuse the code that exists in the binder.
            '

            Dim collectionSyntax = collection.Syntax

            ' Note: the conversion is from the array's element type (if rank = 1), char, or the return type of 
            ' the current's get method
            If currentType IsNot Nothing AndAlso Not controlVariableOpt.HasErrors Then
                Dim controlVariableType = controlVariableOpt.Type
                If Not (controlVariableType.IsErrorType OrElse currentType.IsErrorType OrElse elementType.IsErrorType) Then
                    Dim boundElement As BoundExpression

                    ' "Current" is converted to the type of the control variable as if
                    ' it were an explicit cast. This language rule exists because there is
                    ' no way to write a cast, and the type of the IEnumerator.Current is Object,
                    ' which would make
                    '
                    '  Dim I, A() As Integer : For Each I in A : Next
                    '
                    ' invalid in strict mode.

                    If Conversions.IsIdentityConversion(Conversions.ClassifyConversion(elementType, currentType, useSiteDiagnostics:=Nothing).Key) Then
                        boundCurrentPlaceholder = New BoundRValuePlaceholder(collectionSyntax, elementType)
                        boundElement = boundCurrentPlaceholder
                    Else
                        boundCurrentPlaceholder = New BoundRValuePlaceholder(collectionSyntax, currentType)
                        boundElement = ApplyConversion(collectionSyntax,
                                                       elementType,
                                                       boundCurrentPlaceholder,
                                                       isExplicit:=True,
                                                       diagnostics:=diagnostics)
                        boundElement.SetWasCompilerGenerated()
                    End If

                    If boundElement Is boundCurrentPlaceholder OrElse
                       Not Conversions.IsIdentityConversion(Conversions.ClassifyConversion(controlVariableType, elementType, useSiteDiagnostics:=Nothing).Key) Then
                        boundCurrentConversion = ApplyConversion(collectionSyntax,
                                                                 controlVariableType,
                                                                 boundElement,
                                                                 isExplicit:=True,
                                                                 diagnostics:=diagnostics)
                        boundCurrentConversion.SetWasCompilerGenerated()
                    Else
                        boundCurrentConversion = boundElement
                    End If
                End If
            End If

            ' in case the enumerator needs to be possibly disposed, create the bound nodes for the condition that checks
            ' if Dispose() needs to be called. This bound expression will contain a placeholder for the enumerator that gets
            ' replaced in the local rewriting.
            If needToDispose Then
                ' no need to report use site errors here, this was already done in InterpretForEachStatementCollection
                Dim idisposableType = Compilation.GetSpecialType(SpecialType.System_IDisposable)

                ' needToDispose can only be true if boundGetEnumeratorCall had no errors (see InterpretForEachStatementCollection)
                Dim enumeratorType = boundGetEnumeratorCall.Type

                ' IDisposable is implemented, which means there must be a check if the enumerator is not nothing
                ' will be used in code: "If e IsNot Nothing Then"
                If isOrInheritsFromOrImplementsIDisposable Then

                    If Not (enumeratorType.IsValueType) Then
                        boundDisposeCondition = BindIsExpression(boundEnumeratorPlaceholder,
                                                                 New BoundLiteral(collectionSyntax, ConstantValue.Nothing, Nothing),
                                                                 collectionSyntax, True, diagnostics)
                        boundDisposeCast = ApplyConversion(collectionSyntax, idisposableType, boundEnumeratorPlaceholder, isExplicit:=True, diagnostics:=diagnostics)
                    End If
                Else
                    Debug.Assert(enumeratorType.SpecialType = SpecialType.System_Collections_IEnumerator)

                    ' Instead of if TypeOf(e) is IDisposable, we'll do a TryCast(e, IDisposable) and call Dispose if the result 
                    ' was not Nothing.

                    ' create TryCast
                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    Dim conversionKind As ConversionKind = Conversions.ClassifyTryCastConversion(enumeratorType, idisposableType, useSiteDiagnostics)

                    If diagnostics.Add(collectionSyntax, useSiteDiagnostics) Then
                        ' Suppress additional diagnostics
                        diagnostics = New DiagnosticBag()
                    End If

                    boundDisposeCast = New BoundTryCast(collectionSyntax, boundEnumeratorPlaceholder.MakeRValue(), conversionKind, idisposableType, Nothing)

                    boundDisposeCondition = BindIsExpression(boundDisposeCast,
                                         New BoundLiteral(collectionSyntax, ConstantValue.Nothing, Nothing),
                                         collectionSyntax, True, diagnostics)
                End If
            End If

            ' Bind the loop body and the next variables 
            Dim loopBody As BoundBlock = Nothing
            Dim nextVariables As ImmutableArray(Of BoundExpression) = Nothing
            Me.BindForLoopBodyAndNextControlVariables(node, nextVariables, loopBody, diagnostics)

            Dim enumeratorInfo = New ForEachEnumeratorInfo(boundGetEnumeratorCall,
                                                           boundMoveNextCall,
                                                           boundCurrentAccess,
                                                           elementType,
                                                           needToDispose,
                                                           isOrInheritsFromOrImplementsIDisposable,
                                                           boundDisposeCondition,
                                                           boundDisposeCast,
                                                           boundCurrentConversion,
                                                           boundEnumeratorPlaceholder,
                                                           boundCurrentPlaceholder,
                                                           collectionPlaceholder)

            Return New BoundForEachStatement(node,
                                             collection,
                                             enumeratorInfo,
                                             declaredOrInferredLocalOpt, controlVariableOpt, loopBody, nextVariables,
                                             continueLabel:=GetContinueLabel(SyntaxKind.ContinueForStatement),
                                             exitLabel:=GetExitLabel(SyntaxKind.ExitForStatement))
        End Function

        ''' <summary>
        ''' Verifies for control variable declaration and outputs diagnostics as needed.
        ''' </summary>
        ''' <param name="variableDeclarator">The variable declarator.</param>
        ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
        Private Shared Function VerifyForControlVariableDeclaration(variableDeclarator As VariableDeclaratorSyntax, diagnostics As DiagnosticBag) As Boolean
            ' Check variable declaration syntax if present
            Debug.Assert(variableDeclarator.Names.Count = 1, "should be exactly one control variable")
            Dim identifier = variableDeclarator.Names(0)

            ' nullable type inference is not supported
            If variableDeclarator.AsClause Is Nothing AndAlso
                identifier.Nullable.Node IsNot Nothing Then
                ReportDiagnostic(diagnostics, identifier, ERRID.ERR_NullableTypeInferenceNotSupported)

                Return False
            End If

            ' specifying array bounds is not valid for control variable declarations
            If identifier.ArrayBounds IsNot Nothing Then
                ReportDiagnostic(diagnostics, identifier, ERRID.ERR_ForCtlVarArraySizesSpecified)

                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' This function tries to bind the given controlVariableSyntax. 
        ''' If it was an identifier of a valid target, the bound node is written to controlVariable and true is returned.
        ''' If something else was bound, that is not legal as a control variable (e.g. a property), a BoundBadNode is written 
        ''' to controlVariable and false is returned.
        ''' If nothing declared was found, false is returned and controlVariable is set to nothing. In this case it's safe to
        ''' create a new local for the loop node.
        ''' </summary>
        Private Function TryBindLoopControlVariable(
            controlVariableSyntax As VisualBasicSyntaxNode,
            <Out()> ByRef controlVariable As BoundExpression,
            diagnostics As DiagnosticBag
        ) As Boolean
            Debug.Assert(controlVariableSyntax.Kind <> SyntaxKind.VariableDeclarator)

            controlVariable = BindExpression(DirectCast(controlVariableSyntax, ExpressionSyntax), diagnostics)
            controlVariable = ReclassifyAsValue(controlVariable, diagnostics)

            If controlVariable.HasErrors Then
                ' VB Spec 10.9.3 2.2: 2.2.	Otherwise, if it is classified as anything other than a type
                ' or a variable, it is a compile-error.
                controlVariable = BadExpression(controlVariable)
                Return False
            End If

            ' VB Spec 10.9.3 2.1: If the result of this resolution is classified as a variable, then 
            ' the loop control variable is that variable.
            If Not VerifyForLoopControlReference(controlVariable, diagnostics) Then
                controlVariable = New BoundBadExpression(controlVariableSyntax,
                                                         LookupResultKind.NotAVariable,
                                                         ImmutableArray(Of Symbol).Empty,
                                                         ImmutableArray.Create(controlVariable),
                                                         controlVariable.Type,
                                                         hasErrors:=True)
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' If the control variable was bound to a non bad expression, this function checks if the 
        ''' bound expression is a variable and reports diagnostics appropriately.
        ''' It reports the errors from 10.9.3 2.2
        ''' </summary>
        ''' <param name="controlVariable">The control variable.</param>
        ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
        Private Function VerifyForLoopControlReference(controlVariable As BoundExpression, diagnostics As DiagnosticBag) As Boolean

            Dim isLValue As Boolean
            ' A property reference is not allowed as the control variable of any
            ' kind of For statement.
            If controlVariable.IsPropertyOrXmlPropertyAccess() Then
                ReportDiagnostic(diagnostics, controlVariable.Syntax, ERRID.ERR_LoopControlMustNotBeProperty)
                Return False
            Else
                isLValue = controlVariable.IsLValue()
            End If

            If Not isLValue Then
                If Not controlVariable.HasErrors Then
                    ReportAssignmentToRValue(controlVariable, diagnostics)
                End If
                Return False
            End If

            If Not controlVariable.HasErrors AndAlso IsInAsyncContext() AndAlso
               SeenAwaitVisitor.SeenAwaitIn(controlVariable, diagnostics) Then
                ReportDiagnostic(diagnostics, controlVariable.Syntax, ERRID.ERR_LoopControlMustNotAwait)
                Return False
            End If

            Return True
        End Function

        Private Class SeenAwaitVisitor
            Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

            Private _seenAwait As Boolean

            Private Sub New()
            End Sub

            Public Shared Function SeenAwaitIn(node As BoundNode, diagnostics As DiagnosticBag) As Boolean
                Dim visitor = New SeenAwaitVisitor()
                Try
                    visitor.Visit(node)
                Catch ex As CancelledByStackGuardException
                    ex.AddAnError(diagnostics)
                End Try

                Return visitor._seenAwait
            End Function

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                If _seenAwait Then
                    Return Nothing
                End If

                Return MyBase.Visit(node)
            End Function

            Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
                ' Do not dive into lambdas.
                Return Nothing
            End Function

            Public Overrides Function VisitAwaitOperator(node As BoundAwaitOperator) As BoundNode
                _seenAwait = True
                Return Nothing
            End Function
        End Class

        ''' <summary>
        ''' Verifies that the collection is either a string, and array or matches the design pattern criteria and reports 
        ''' diagnostics appropriately.
        ''' </summary>
        ''' <param name="collection">The collection of the for each statement.</param>
        ''' <param name="currentType">If the collection meets all criteria, currentType contains the type of the element from 
        ''' the collection that get's returned by the current property.</param>
        ''' <param name="elementType">Element type of the collection, could be different from <paramref name="currentType"/>. 
        ''' For example, based on the pattern <paramref name="currentType"/> for an array is Object, but the <paramref name="elementType"/>
        ''' is the element type of the array.</param>
        ''' <param name="isEnumerable">if set to <c>true</c>, the collection is enumerable (matches design pattern, IEnumerable 
        ''' or IEnumerable(Of T); otherwise (string or arrays) it's set to false.</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        ''' <returns>The collection which might have been converted to IEnumerable or IEnumerable(Of T) if needed.</returns>
        Private Function InterpretForEachStatementCollection(
            collection As BoundExpression,
            <Out()> ByRef currentType As TypeSymbol,
            <Out()> ByRef elementType As TypeSymbol,
            <Out()> ByRef isEnumerable As Boolean,
            <Out()> ByRef boundGetEnumeratorCall As BoundExpression,
            <Out()> ByRef boundEnumeratorPlaceholder As BoundLValuePlaceholder,
            <Out()> ByRef boundMoveNextCall As BoundExpression,
            <Out()> ByRef boundCurrentAccess As BoundExpression,
            <Out()> ByRef collectionPlaceholder As BoundRValuePlaceholder,
            <Out()> ByRef needToDispose As Boolean,
            <Out()> ByRef isOrInheritsFromOrImplementsIDisposable As Boolean,
            diagnostics As DiagnosticBag
        ) As BoundExpression

            currentType = Nothing
            elementType = Nothing
            isEnumerable = False
            needToDispose = False
            isOrInheritsFromOrImplementsIDisposable = False
            boundGetEnumeratorCall = Nothing
            boundEnumeratorPlaceholder = Nothing
            boundMoveNextCall = Nothing
            boundCurrentAccess = Nothing
            collectionPlaceholder = Nothing

            If collection.HasErrors Then
                Return collection
            End If

            Dim collectionType As TypeSymbol = collection.Type
            Dim collectionSyntax = collection.Syntax

            Dim targetCollectionType As NamedTypeSymbol = Nothing

            ' If the collection matches the design pattern, use that.
            ' Otherwise, if the collection implements IEnumerable, convert it
            ' to IEnumerable (which matches the design pattern).
            '
            ' The design pattern is preferred to the interface implementation
            ' because it is more efficient.

            Dim interfaceSpecialType As SpecialType = SpecialType.None
            Dim detailedDiagnostics = DiagnosticBag.GetInstance

            If MatchesForEachCollectionDesignPattern(collectionType, collection,
                                                     currentType,
                                                     boundGetEnumeratorCall,
                                                     boundEnumeratorPlaceholder,
                                                     boundMoveNextCall,
                                                     boundCurrentAccess,
                                                     collectionPlaceholder,
                                                     detailedDiagnostics) Then

                diagnostics.AddRange(detailedDiagnostics)
                elementType = currentType
                ' TODO(rbeckers) check if the long note about spurious errors in Dev10 (statement_semantics.cpp, line 5250) needs 
                ' to be copied. 
                ' We only pass in a temporary diagnostic bag into this method. The method itself only adds to it in case of 
                ' ambiguous lookups or a failed overload resolution for the current property access. 
                isEnumerable = True
            Else
                ' using a temporary diagnostic bag to only report use site errors for IEnumerable or IEnumerable(Of T) if they are used.
                Dim ienumerableUseSiteDiagnostics = DiagnosticBag.GetInstance
                Dim genericIEnumerable = GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T, collectionSyntax, ienumerableUseSiteDiagnostics)
                Dim matchingInterfaces As New HashSet(Of NamedTypeSymbol)(EqualsIgnoringComparer.InstanceIgnoringTupleNames)
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                If Not collection.IsNothingLiteral AndAlso
                   Not collectionType.IsArrayType AndAlso
                   IsOrInheritsFromOrImplementsInterface(collectionType, genericIEnumerable, useSiteDiagnostics, matchingInterfaces) Then

                    diagnostics.Add(collectionSyntax, useSiteDiagnostics)

                    Debug.Assert(matchingInterfaces.Count > 0)
                    isEnumerable = True
                    targetCollectionType = matchingInterfaces(0)

                    ' merge diagnostics for IEnumerable(Of T)
                    diagnostics.AddRange(ienumerableUseSiteDiagnostics)
                    ienumerableUseSiteDiagnostics.Free()

                    If matchingInterfaces.Count > 1 Then
                        ' matchingInterfaces is a hash set, so it's enough to check if the count is more than one. 
                        ' Duplicates found while analyzing type parameter constraints do not occur this way like in Dev10.
                        ReportDiagnostic(diagnostics,
                                         collectionSyntax,
                                         ErrorFactory.ErrorInfo(ERRID.ERR_ForEachAmbiguousIEnumerable1,
                                                                collectionType))

                        detailedDiagnostics.Free()

                        Return New BoundBadExpression(collectionSyntax,
                                                      LookupResultKind.Empty,
                                                      ImmutableArray(Of Symbol).Empty,
                                                      ImmutableArray.Create(collection),
                                                      collectionType,
                                                      hasErrors:=True)
                    End If
                    interfaceSpecialType = SpecialType.System_Collections_Generic_IEnumerable_T

                Else
                    ienumerableUseSiteDiagnostics.Clear()

                    Dim ienumerable = GetSpecialType(SpecialType.System_Collections_IEnumerable, collectionSyntax, ienumerableUseSiteDiagnostics)
                    If ((collection.IsNothingLiteral OrElse collectionType.IsObjectType) AndAlso Me.OptionStrict <> OptionStrict.On) OrElse
                       (Not collection.IsNothingLiteral AndAlso Not collectionType.IsArrayType AndAlso IsOrInheritsFromOrImplementsInterface(collectionType, ienumerable, useSiteDiagnostics, matchingInterfaces)) Then

                        Debug.Assert(collection.IsNothingLiteral OrElse collectionType.IsObjectType OrElse (TypeSymbol.Equals(matchingInterfaces.First, ienumerable, TypeCompareKind.ConsiderEverything) AndAlso matchingInterfaces.Count = 1))

                        diagnostics.Add(collectionSyntax, useSiteDiagnostics)

                        isEnumerable = True
                        targetCollectionType = ienumerable
                        interfaceSpecialType = SpecialType.System_Collections_IEnumerable

                        ' merge diagnostics for IEnumerable
                        diagnostics.AddRange(ienumerableUseSiteDiagnostics)
                        ienumerableUseSiteDiagnostics.Free()

                    Else
                        Debug.Assert(collectionType IsNot Nothing OrElse collection.IsNothingLiteral AndAlso Me.OptionStrict = OptionStrict.On)

                        diagnostics.Add(collectionSyntax, useSiteDiagnostics)

                        If collection.IsNothingLiteral Then
                            ' in case of option strict on we need to reclassify the nothing literal
                            collection = MakeRValue(collection, diagnostics)
                            collectionType = collection.Type
                        End If

                        ' Show detailed errors for ambiguous lookups or failed overload resolution in case they are available,
                        ' otherwise report the default error "xyz does not match design pattern".
                        If detailedDiagnostics.HasAnyErrors Then
                            diagnostics.AddRange(detailedDiagnostics)
                        Else
                            ReportDiagnostic(diagnostics,
                                             collectionSyntax,
                                             ErrorFactory.ErrorInfo(ERRID.ERR_ForEachCollectionDesignPattern1,
                                                                    collectionType))
                        End If

                        detailedDiagnostics.Free()
                        ienumerableUseSiteDiagnostics.Free()

                        Return New BoundBadExpression(collectionSyntax,
                                                      LookupResultKind.Empty,
                                                      ImmutableArray(Of Symbol).Empty,
                                                      ImmutableArray.Create(collection),
                                                      collectionType,
                                                      hasErrors:=True)
                    End If

                End If
            End If

            detailedDiagnostics.Free()

            ' in case one of the IEnumerable interfaces are used, we'll need to cast to it.
            ' this needs to happen before creating the bound calls, to force a boxing of collections that are value types.
            If targetCollectionType IsNot Nothing Then
                collection = ApplyImplicitConversion(collectionSyntax, targetCollectionType, collection, diagnostics)
            End If

            If isEnumerable Then
                If interfaceSpecialType <> SpecialType.None Then
                    Dim member As Symbol
                    Dim specialTypeMember As Symbol

                    '
                    ' GetEnumerator
                    '
                    If interfaceSpecialType = SpecialType.System_Collections_Generic_IEnumerable_T Then
                        specialTypeMember = GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator,
                                                                 collectionSyntax,
                                                                 diagnostics)
                        If specialTypeMember IsNot Nothing AndAlso specialTypeMember.GetUseSiteErrorInfo Is Nothing AndAlso Not targetCollectionType.IsErrorType Then
                            member = DirectCast(targetCollectionType, SubstitutedNamedType).GetMemberForDefinition(specialTypeMember)
                        Else
                            member = Nothing
                        End If
                    Else
                        member = GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator,
                                                      collectionSyntax,
                                                      diagnostics)
                    End If

                    If member IsNot Nothing AndAlso member.GetUseSiteErrorInfo Is Nothing Then
                        collectionPlaceholder = New BoundRValuePlaceholder(collectionSyntax,
                                                                           If(collectionType IsNot Nothing AndAlso collectionType.IsStringType, collectionType, collection.Type))
                        Dim methodOrPropertyGroup As BoundMethodOrPropertyGroup
                        methodOrPropertyGroup = New BoundMethodGroup(collectionSyntax,
                                                                     Nothing,
                                                                     ImmutableArray.Create(DirectCast(member, MethodSymbol)),
                                                                     LookupResultKind.Good,
                                                                     collectionPlaceholder,
                                                                     QualificationKind.QualifiedViaValue)

                        boundGetEnumeratorCall = CreateBoundInvocationExpressionFromMethodOrPropertyGroup(collectionSyntax,
                                                                                                          methodOrPropertyGroup,
                                                                                                          diagnostics)
                        Dim enumeratorType = boundGetEnumeratorCall.Type
                        boundEnumeratorPlaceholder = New BoundLValuePlaceholder(collectionSyntax,
                                                                               enumeratorType)

                        '
                        ' MoveNext
                        '
                        member = GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext,
                                                      collectionSyntax,
                                                      diagnostics)
                        If member IsNot Nothing AndAlso member.GetUseSiteErrorInfo Is Nothing Then
                            methodOrPropertyGroup = New BoundMethodGroup(collectionSyntax,
                                                                         Nothing,
                                                                         ImmutableArray.Create(DirectCast(member, MethodSymbol)),
                                                                         LookupResultKind.Good,
                                                                         boundEnumeratorPlaceholder,
                                                                         QualificationKind.QualifiedViaValue)

                            boundMoveNextCall = CreateBoundInvocationExpressionFromMethodOrPropertyGroup(collectionSyntax,
                                                                                                         methodOrPropertyGroup,
                                                                                                         diagnostics)
                        End If

                        '
                        ' Current
                        '
                        If interfaceSpecialType = SpecialType.System_Collections_Generic_IEnumerable_T Then
                            specialTypeMember = GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerator_T__Current,
                                                                     collectionSyntax,
                                                                     diagnostics)

                            If specialTypeMember IsNot Nothing AndAlso specialTypeMember.GetUseSiteErrorInfo Is Nothing AndAlso Not enumeratorType.IsErrorType Then
                                member = DirectCast(enumeratorType, SubstitutedNamedType).GetMemberForDefinition(specialTypeMember)
                            Else
                                member = Nothing
                            End If
                        Else
                            member = GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current,
                                                          collectionSyntax,
                                                          diagnostics)
                        End If

                        If member IsNot Nothing AndAlso member.GetUseSiteErrorInfo Is Nothing Then
                            methodOrPropertyGroup = New BoundPropertyGroup(collectionSyntax,
                                                                           ImmutableArray.Create(DirectCast(member, PropertySymbol)),
                                                                           LookupResultKind.Good,
                                                                           boundEnumeratorPlaceholder,
                                                                           QualificationKind.QualifiedViaValue)

                            boundCurrentAccess = CreateBoundInvocationExpressionFromMethodOrPropertyGroup(collectionSyntax,
                                                                                                          methodOrPropertyGroup,
                                                                                                          diagnostics)

                            currentType = boundCurrentAccess.Type
                            elementType = currentType
                        End If
                    End If
                End If

                If collectionType IsNot Nothing Then
                    If collectionType.IsArrayType() Then
                        Dim arrayType = DirectCast(collectionType, ArrayTypeSymbol)
                        elementType = arrayType.ElementType

                        If arrayType.IsSZArray Then
                            currentType = elementType
                        End If
                    ElseIf collectionType.IsStringType() Then
                        elementType = GetSpecialType(SpecialType.System_Char, collectionSyntax, diagnostics)
                        currentType = elementType
                    End If
                End If

                ' if it's enumerable, we'll need to check if the enumerator is disposable.
                Dim idisposable = GetSpecialType(SpecialType.System_IDisposable, collectionSyntax, diagnostics)

                If (idisposable IsNot Nothing AndAlso Not idisposable.IsErrorType) AndAlso
                    Not boundGetEnumeratorCall.HasErrors AndAlso Not boundGetEnumeratorCall.Type.IsErrorType Then

                    Dim getEnumeratorReturnType = boundGetEnumeratorCall.Type
                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    Dim conversionKind = Conversions.ClassifyDirectCastConversion(getEnumeratorReturnType, idisposable, useSiteDiagnostics)

                    diagnostics.Add(collectionSyntax, useSiteDiagnostics)

                    isOrInheritsFromOrImplementsIDisposable = Conversions.IsWideningConversion(conversionKind)

                    If isOrInheritsFromOrImplementsIDisposable OrElse
                        getEnumeratorReturnType.SpecialType = SpecialType.System_Collections_IEnumerator Then

                        ' do not actually generate dispose calls in IL
                        ' Dev10 does the same thing (StatementSemantics.cpp, Line 5678++)
                        ' this is true even for multidimensional arrays that are handled through the design pattern.
                        Debug.Assert(collectionType IsNot Nothing OrElse OptionStrict <> OptionStrict.On AndAlso collection.Kind = BoundKind.Conversion AndAlso DirectCast(collection, BoundConversion).Operand.IsNothingLiteral)
                        If collectionType Is Nothing OrElse Not collectionType.IsArrayType Then
                            needToDispose = True
                        End If
                    End If
                End If
            End If

            Return collection
        End Function

        ''' <summary>
        ''' Checks if the type of the collection matches the for each collection design pattern.
        ''' </summary>
        ''' <remarks>
        ''' The rules are that the collection type must have an accessible GetEnumerator method that takes no parameters and
        ''' returns a type that has both:
        '''  - an accessible MoveNext method that takes no parameters and returns a Boolean
        '''  - an accessible Current property that takes no parameters and is not WriteOnly
        '''
        ''' NOTE: this function ONLY checks for a function named "GetEnumerator" with the appropriate properties.
        ''' In the spec $10.9 it has these conditions: a type C is a "collection type" if one of
        '''    (1) it satisfies MatchesForEachCollectionDesignPattern (i.e. has a method named GetEnumerator() which
        '''        returns a type with MoveNext/Current); or
        '''    (2) it implements System.Collections.Generic.IEnumerable(Of T); or
        '''    (3) it implements System.Collections.IEnumerable.
        '''
        ''' This function ONLY checks for part (1). Callers are expected to check for (2)/(3) themselves. The
        ''' scenario where something satisfies (2/3) but not (1) is
        '''   Class C 
        '''       Implements IEnumerable
        '''       Function g1() as IEnumerator implements IEnumerable.GetEnumerator : End Function
        ''' 
        ''' Clearly this class does not have a method _named_ GetEnumerator, but it does implement IEnumerable.
        ''' </remarks>
        ''' <param name="collectionType">The type of the for each collection.</param>
        ''' <param name="collection">The bound collection expression.</param>
        ''' <param name="currentType">Return type of the property named "Current" if found.</param>
        ''' <param name="boundGetEnumeratorCall">A bound call to GetEnumerator on the collection if found.</param>
        ''' <param name="boundEnumeratorPlaceholder">A bound placeholder value for the collection local if GetEnumerator 
        ''' was bound successful</param>
        ''' <param name="boundMoveNextCall">A bound call to MoveNext on the instance returned by GetEnumerator if found.</param>
        ''' <param name="boundCurrentAccess">A bound property access for "Current" on the instance returned by GetEnumerator if found.</param>
        ''' <param name="collectionPlaceholder">A placeholder for the collection expression.</param>
        ''' <param name="temporaryDiagnostics">An empty diagnostic bag to capture diagnostics that have to be reported if the
        ''' collection matches the design pattern and that can be used instead of the generic error message in case non of the
        ''' for each collection criteria match.</param>
        ''' <returns>If all required methods have been successfully looked up and bound, true is being returned; otherwise false.
        ''' </returns>
        Private Function MatchesForEachCollectionDesignPattern(
            collectionType As TypeSymbol,
            collection As BoundExpression,
            <Out()> ByRef currentType As TypeSymbol,
            <Out()> ByRef boundGetEnumeratorCall As BoundExpression,
            <Out()> ByRef boundEnumeratorPlaceholder As BoundLValuePlaceholder,
            <Out()> ByRef boundMoveNextCall As BoundExpression,
            <Out()> ByRef boundCurrentAccess As BoundExpression,
            <Out()> ByRef collectionPlaceholder As BoundRValuePlaceholder,
            temporaryDiagnostics As DiagnosticBag
        ) As Boolean
            Debug.Assert(temporaryDiagnostics.IsEmptyWithoutResolution)

            currentType = Nothing
            boundGetEnumeratorCall = Nothing
            boundEnumeratorPlaceholder = Nothing
            boundMoveNextCall = Nothing
            boundCurrentAccess = Nothing

            ' This method will add diagnostics (errors and warnings) to the given diagnostic bag. It might clean it while processing
            ' the collection type to reduce noise which is why only a new/empty diagnostic bag should be passed to this method.
            ' 
            ' The diagnostics returned by this method should be added to the general diagnostic bag in case of a successful match
            ' of the collection design pattern to e.g. report warnings. 
            ' If the collection does not match the collection design pattern and the collection is not and does not implement or 
            ' inherits IEnumerable(Of T) or IEnumerable then the diagnostics can be used to give a more detailed reason instead
            ' of the generic "collection {0} does not match design pattern".
            ' To provide more detailed information that is close to the output of Dev10 we are collection diagnostics from
            ' ambiguous lookups and from binding the current property get access.

            Dim collectionSyntax = collection.Syntax

            If collection.IsNothingLiteral OrElse
               (collectionType.Kind <> SymbolKind.ArrayType AndAlso
                collectionType.Kind <> SymbolKind.NamedType AndAlso
                collectionType.Kind <> SymbolKind.TypeParameter) Then
                ' Dev10 checked for !IsClassInterfaceRecordOrGenericParamType which is equivalent to a named type in Roslyn
                Return False
            End If

            '
            ' GetEnumerator
            '
            ' first, get GetEnumerator function that takes no arguments, also search in extension methods
            Dim lookupResult As New LookupResult()
            If Not GetMemberIfMatchesRequirements(WellKnownMemberNames.GetEnumeratorMethodName,
                                                   collectionType,
                                                   s_isFunctionWithoutArguments,
                                                   lookupResult,
                                                   collectionSyntax,
                                                   temporaryDiagnostics) Then
                Return False
            End If

            Debug.Assert(lookupResult.IsGood)

            collectionPlaceholder = New BoundRValuePlaceholder(collectionSyntax, collection.Type)

            ' bind the call to GetEnumerator (incl. overload resolution, handling of param arrays, optional parameters, ...)
            Dim methodOrPropertyGroup As BoundMethodOrPropertyGroup = CreateBoundMethodGroup(collectionSyntax,
                                                                                             lookupResult,
                                                                                             LookupOptions.AllMethodsOfAnyArity,
                                                                                             collectionPlaceholder,
                                                                                             Nothing,
                                                                                             QualificationKind.QualifiedViaValue)

            boundGetEnumeratorCall = CreateBoundInvocationExpressionFromMethodOrPropertyGroup(collectionSyntax,
                                                                                              methodOrPropertyGroup,
                                                                                              temporaryDiagnostics)
            If boundGetEnumeratorCall.HasErrors Then
                temporaryDiagnostics.Clear()
                Return False
            End If

            Dim enumeratorType As TypeSymbol = boundGetEnumeratorCall.Type
            boundEnumeratorPlaceholder = New BoundLValuePlaceholder(collectionSyntax, enumeratorType)

            '
            ' MoveNext
            '
            ' try lookup an accessible MoveNext function in the return type of GetEnumerator that takes no parameters and
            ' returns a boolean.
            If Not GetMemberIfMatchesRequirements(WellKnownMemberNames.MoveNextMethodName,
                                                   enumeratorType,
                                                   s_isFunctionWithoutArguments,
                                                   lookupResult,
                                                   collectionSyntax,
                                                   temporaryDiagnostics) Then
                Return False
            End If

            Debug.Assert(lookupResult.IsGood)

            ' bind the call to MoveNext (incl. overload resolution, handling of param arrays, optional parameters, ...)
            methodOrPropertyGroup = CreateBoundMethodGroup(collectionSyntax,
                                                               lookupResult,
                                                               LookupOptions.AllMethodsOfAnyArity,
                                                               boundEnumeratorPlaceholder,
                                                               Nothing,
                                                               QualificationKind.QualifiedViaValue)
            boundMoveNextCall = CreateBoundInvocationExpressionFromMethodOrPropertyGroup(collectionSyntax,
                                                                                         methodOrPropertyGroup,
                                                                                         temporaryDiagnostics)

            If boundMoveNextCall.HasErrors OrElse
                boundMoveNextCall.Kind <> BoundKind.Call OrElse
                DirectCast(boundMoveNextCall, BoundCall).Method.OriginalDefinition.ReturnType.SpecialType <> SpecialType.System_Boolean Then

                ' Dev10 does not accept a MoveNext with a constructed return type, even if it is a boolean.
                temporaryDiagnostics.Clear()
                Return False
            End If

            '
            ' Current
            '
            ' try lookup an accessible and readable property named Current that takes no parameters.
            If Not GetMemberIfMatchesRequirements(WellKnownMemberNames.CurrentPropertyName,
                                                   enumeratorType,
                                                   s_isReadablePropertyWithoutArguments,
                                                   lookupResult,
                                                   collectionSyntax,
                                                   temporaryDiagnostics) Then
                Return False
            End If

            Debug.Assert(lookupResult.IsGood)

            ' bind the call to Current (incl. overload resolution, handling of param arrays, optional parameters, ...)
            methodOrPropertyGroup = New BoundPropertyGroup(collectionSyntax,
                                                           lookupResult.Symbols.ToDowncastedImmutable(Of PropertySymbol),
                                                           lookupResult.Kind,
                                                           boundEnumeratorPlaceholder,
                                                           QualificationKind.QualifiedViaValue)
            boundCurrentAccess = CreateBoundInvocationExpressionFromMethodOrPropertyGroup(collectionSyntax,
                                                                                          methodOrPropertyGroup,
                                                                                          temporaryDiagnostics)

            ' the requirement is a "readable" property that takes no parameters, but the get property could be inaccessible
            ' and then binding a property access will fail.
            If boundCurrentAccess.HasErrors Then
                Return False
            End If

            currentType = boundCurrentAccess.Type

            Return True
        End Function

        ''' <summary>
        ''' Creates a BoundCall or BoundPropertyAccess from a MethodOrPropertyGroup.
        ''' </summary>
        ''' <remarks>
        ''' This is not a general purpose helper!
        ''' </remarks>
        ''' <param name="syntax">The syntax node.</param>
        ''' <param name="methodOrPropertyGroup">The method or property group.</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        Private Function CreateBoundInvocationExpressionFromMethodOrPropertyGroup(
            syntax As SyntaxNode,
            methodOrPropertyGroup As BoundMethodOrPropertyGroup,
            diagnostics As DiagnosticBag
        ) As BoundExpression
            Dim boundCall = BindInvocationExpression(syntax, syntax,
                                                     TypeCharacter.None,
                                                     methodOrPropertyGroup,
                                                     ImmutableArray(Of BoundExpression).Empty,
                                                     Nothing,
                                                     diagnostics,
                                                     callerInfoOpt:=syntax)

            Return MakeRValue(boundCall, diagnostics)
        End Function

        ''' <summary>
        ''' Checks if a given symbol is a function that takes no parameters.
        ''' </summary>
        Private Shared ReadOnly s_isFunctionWithoutArguments As Func(Of Symbol, Boolean) = Function(sym)
                                                                                               If sym.Kind = SymbolKind.Method Then
                                                                                                   Dim method = DirectCast(sym, MethodSymbol)
                                                                                                   Return Not method.IsSub() AndAlso
                                                                                                          Not method.IsGenericMethod AndAlso
                                                                                                          method.CanBeCalledWithNoParameters
                                                                                               End If
                                                                                               Return False
                                                                                           End Function

        ''' <summary>
        ''' Checks if a given symbol is a property that is readable.
        ''' </summary>
        Private Shared ReadOnly s_isReadablePropertyWithoutArguments As Func(Of Symbol, Boolean) = Function(sym)
                                                                                                       If sym.Kind = SymbolKind.Property Then
                                                                                                           Dim prop = DirectCast(sym, PropertySymbol)
                                                                                                           Return prop.IsReadable AndAlso
                                                                                                                  Not prop.GetMostDerivedGetMethod().IsGenericMethod AndAlso
                                                                                                                  prop.GetCanBeCalledWithNoParameters
                                                                                                       End If
                                                                                                       Return False
                                                                                                   End Function

        ''' <summary>
        ''' Returns the lookup result if at least one found symbol matches the requirements that are verified
        ''' by using the given symbolChecker. Extension methods will be considered in this check.
        ''' </summary>
        ''' <param name="name">The name of the method or property to look for.</param>
        ''' <param name="container">The container to look in.</param>
        ''' <param name="symbolChecker">The symbol checker which performs additional checks.</param>
        Private Function GetMemberIfMatchesRequirements(
            name As String,
            container As TypeSymbol,
            symbolChecker As Func(Of Symbol, Boolean),
            result As LookupResult,
            syntax As SyntaxNode,
            diagnostics As DiagnosticBag
        ) As Boolean
            result.Clear()

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            LookupMember(result, container, name, 0, LookupOptions.AllMethodsOfAnyArity, useSiteDiagnostics)

            diagnostics.Add(syntax, useSiteDiagnostics)
            useSiteDiagnostics = Nothing

            If result.IsGood Then
                For Each candidateSymbol In result.Symbols
                    If symbolChecker(candidateSymbol) Then
                        Return True
                    End If
                Next

                ' if there are instance methods in the results, there will be no extension methods. 
                ' Therefore we need to check separately.
                If result.Symbols(0).Kind = SymbolKind.Method AndAlso
                    Not DirectCast(result.Symbols(0), MethodSymbol).IsReducedExtensionMethod Then
                    result.Clear()
                    LookupExtensionMethods(result,
                                           container,
                                           name,
                                           0,
                                           LookupOptions.AllMethodsOfAnyArity,
                                           useSiteDiagnostics)

                    diagnostics.Add(syntax, useSiteDiagnostics)

                    If result.IsGood Then
                        For Each candidateSymbol In result.Symbols
                            If symbolChecker(candidateSymbol) Then
                                Return True
                            End If
                        Next
                    End If

                    Debug.Assert(Not result.IsAmbiguous)
                End If
            ElseIf result.IsAmbiguous Then
                ' save these diagnostics to report them if the collection does not match the design pattern at all instead of the
                ' generic diagnostic
                Debug.Assert(result.HasDiagnostic)
                diagnostics.Clear()
                diagnostics.Add(New VBDiagnostic(result.Diagnostic, syntax.GetLocation()))
            End If

            result.Clear()

            Return False
        End Function

        ''' <summary>
        ''' Determines whether derivedType is, inherits from or implements the given interface.
        ''' </summary>
        ''' <param name="derivedType">The possible derived type.</param>
        ''' <param name="interfaceType">Type of the interface.</param>
        ''' <param name="useSiteDiagnostics"/> 
        ''' <param name="matchingInterfaces">A list of matching interfaces.</param>
        ''' <returns>
        '''   <c>true</c> if derivedType is, inherits from or implements the interface; otherwise, <c>false</c>.
        ''' </returns>
        Friend Shared Function IsOrInheritsFromOrImplementsInterface(
            derivedType As TypeSymbol,
            interfaceType As NamedTypeSymbol,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
            Optional matchingInterfaces As HashSet(Of NamedTypeSymbol) = Nothing
        ) As Boolean

            ' this is a more specialized version of the Dev10 code for IsOrInheritsFromOrImplements (type_helpers.cpp 
            ' line 1210++) just covering Interfaces. 

            Debug.Assert(interfaceType.IsDefinition)

            If derivedType.IsTypeParameter Then
                Dim derivedTypeParameter = DirectCast(derivedType, TypeParameterSymbol)

                ' check constraints if they have an appropriate relation to the given interface

                ' if it has a value type constraint, check if system.valuetype satisfies the requirements
                If derivedTypeParameter.HasValueTypeConstraint Then
                    Dim valueTypeSymbol = interfaceType.ContainingAssembly.GetSpecialType(SpecialType.System_ValueType)
                    If IsOrInheritsFromOrImplementsInterface(valueTypeSymbol, interfaceType, useSiteDiagnostics, matchingInterfaces) AndAlso matchingInterfaces Is Nothing Then
                        Return True
                    End If
                End If

                ' check if any interface constraint has the appropriate relation to the base type
                For Each typeConstraint In derivedTypeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                    If IsOrInheritsFromOrImplementsInterface(typeConstraint, interfaceType, useSiteDiagnostics, matchingInterfaces) AndAlso matchingInterfaces Is Nothing Then
                        Return True
                    End If
                Next
            Else
                ' derivedType could be an interface
                If TypeSymbol.Equals(derivedType.OriginalDefinition, interfaceType, TypeCompareKind.ConsiderEverything) Then
                    If matchingInterfaces Is Nothing Then
                        Return True
                    End If

                    RecordMatchForIsOrInheritsFromOrImplementsInterface(matchingInterfaces, DirectCast(derivedType, NamedTypeSymbol))
                End If

                ' implements or inherits interface
                For Each interfaceOfDerived In derivedType.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                    If TypeSymbol.Equals(interfaceOfDerived.OriginalDefinition, interfaceType, TypeCompareKind.ConsiderEverything) Then
                        If matchingInterfaces Is Nothing Then
                            Return True
                        End If

                        RecordMatchForIsOrInheritsFromOrImplementsInterface(matchingInterfaces, interfaceOfDerived)
                    End If
                Next
            End If

            Return matchingInterfaces IsNot Nothing AndAlso matchingInterfaces.Count > 0
        End Function

        Private Shared Sub RecordMatchForIsOrInheritsFromOrImplementsInterface(matchingInterfaces As HashSet(Of NamedTypeSymbol), interfaceOfDerived As NamedTypeSymbol)
            Debug.Assert(matchingInterfaces.Comparer Is EqualsIgnoringComparer.InstanceIgnoringTupleNames OrElse
                         matchingInterfaces.Comparer Is EqualityComparer(Of NamedTypeSymbol).Default)

            If Not matchingInterfaces.Add(interfaceOfDerived) AndAlso
               matchingInterfaces.Comparer Is EqualsIgnoringComparer.InstanceIgnoringTupleNames AndAlso
               Not interfaceOfDerived.IsDefinition Then

                ' Keep the last match in the set
                matchingInterfaces.Remove(interfaceOfDerived)
                matchingInterfaces.Add(interfaceOfDerived)
            End If
        End Sub

        Public Function BindWithBlock(node As WithBlockSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim binder As Binder = Me.GetBinder(DirectCast(node, VisualBasicSyntaxNode))
            Return binder.CreateBoundWithBlock(node, binder, diagnostics)
        End Function

        Protected Overridable Function CreateBoundWithBlock(node As WithBlockSyntax, boundBlockBinder As Binder, diagnostics As DiagnosticBag) As BoundStatement
            Return Me.ContainingBinder.CreateBoundWithBlock(node, boundBlockBinder, diagnostics)
        End Function

        ''' <summary>
        ''' Initially binding using blocks.
        ''' A Using statement names a resource that is supposed to be disposed on completion.
        ''' The resource can be an expression or a list of local variables with initializers.
        ''' the type of the resource must implement System.IDispose
        ''' A using statement of the form:
        '''      using Expression
        '''          list_of_statements
        '''      end using
        '''
        ''' when the resource is a using locally declared variable no temporary is generated but the variable is read-only
        ''' A using statement of the form:
        '''      using v as new myDispose
        '''          list_of_statements
        '''      end using
        ''' It is also possible to use multiple variable resources:
        '''      using v1 as new myDispose, v2 as myDispose = new myDispose()
        '''          list_of_statements
        '''      end using
        '''</summary>
        Public Function BindUsingBlock(node As UsingBlockSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim usingBinder = Me.GetBinder(node)
            Debug.Assert(usingBinder IsNot Nothing)

            Dim resourceList As ImmutableArray(Of BoundLocalDeclarationBase) = Nothing
            Dim resourceExpression As BoundExpression = Nothing

            Dim usingStatement = node.UsingStatement
            Dim usingVariableDeclarations = usingStatement.Variables
            Dim usingVariableDeclarationCount = usingVariableDeclarations.Count

            Dim iDisposable = GetSpecialType(SpecialType.System_IDisposable,
                                             node,
                                             diagnostics)

            Dim placeholderInfo = New Dictionary(Of TypeSymbol, ValueTuple(Of BoundRValuePlaceholder, BoundExpression, BoundExpression))()

            If usingVariableDeclarationCount > 0 Then
                ' this is the case of a using statement with one or more variable declarations.

                ' this will bind the declaration, infer the local's type if needed and binds the initialization expression.
                ' implicit variable declarations are handled in that method as well (Option Explicit Off)
                resourceList = usingBinder.BindVariableDeclarators(usingVariableDeclarations, diagnostics)

                ' now that the variable declarations and initialization expression are bound, report
                ' using statement related diagnostics.
                For resourceIndex = 0 To usingVariableDeclarationCount - 1
                    Dim localDeclarations = resourceList(resourceIndex)
                    Dim declarationSyntax = localDeclarations.Syntax

                    ' bound locals have the identifier syntax as their syntax, but for error messages we want to 
                    ' show squiggles under the whole declaration.
                    ' There is one exception to this rule: the parent is a declarator and has multiple names then 
                    ' the identifier syntax should be used to be able to distinguish between the error locations.
                    ' e.g. dim x, y as Integer = 23
                    Dim syntaxNodeForErrors As SyntaxNode
                    If localDeclarations.Kind <> BoundKind.AsNewLocalDeclarations Then
                        syntaxNodeForErrors = declarationSyntax.Parent

                        If syntaxNodeForErrors Is Nothing OrElse
                            DirectCast(syntaxNodeForErrors, VariableDeclaratorSyntax).Names.Count > 1 Then
                            syntaxNodeForErrors = declarationSyntax
                        End If
                    Else
                        syntaxNodeForErrors = declarationSyntax
                    End If

                    ' check if all declared variables are initialized                    
                    If localDeclarations.Kind = BoundKind.LocalDeclaration Then
                        Dim boundLocalDeclaration = DirectCast(localDeclarations, BoundLocalDeclaration)

                        Dim initializerExpression = boundLocalDeclaration.InitializerOpt
                        If initializerExpression Is Nothing Then
                            ReportDiagnostic(diagnostics,
                                             syntaxNodeForErrors,
                                             ErrorFactory.ErrorInfo(ERRID.ERR_UsingResourceVarNeedsInitializer))
                        End If

                        VerifyUsingVariableDeclarationAndBuildUsingInfo(syntaxNodeForErrors,
                                                                        boundLocalDeclaration.LocalSymbol,
                                                                        iDisposable,
                                                                        placeholderInfo,
                                                                        diagnostics)
                    Else
                        Dim boundAsNewDeclarations = DirectCast(localDeclarations, BoundAsNewLocalDeclarations)

                        ' there can be multiple variables be declared in an "As New"
                        For declarationIndex = 0 To boundAsNewDeclarations.LocalDeclarations.Length - 1
                            Dim localDeclaration As BoundLocalDeclaration = boundAsNewDeclarations.LocalDeclarations(declarationIndex)

                            VerifyUsingVariableDeclarationAndBuildUsingInfo(localDeclaration.Syntax,
                                                                            localDeclaration.LocalSymbol,
                                                                            iDisposable,
                                                                            placeholderInfo,
                                                                            diagnostics)
                        Next
                    End If
                Next

            Else
                ' the using block has an expression as resource
                Debug.Assert(usingStatement.Expression IsNot Nothing)

                Dim resourceExpressionSyntax = usingStatement.Expression
                resourceExpression = BindRValue(resourceExpressionSyntax, diagnostics)

                Dim resourceExpressionType = resourceExpression.Type
                If Not resourceExpressionType.IsErrorType AndAlso Not iDisposable.IsErrorType Then
                    BuildAndVerifyUsingInfo(resourceExpressionSyntax,
                                            resourceExpression.Type,
                                            placeholderInfo,
                                            iDisposable,
                                            diagnostics)
                End If
            End If

            ' Bind the body of the using statement.
            Dim usingBody As BoundBlock = BindBlock(node, node.Statements, diagnostics).MakeCompilerGenerated()
            Dim usingInfo As New UsingInfo(node, placeholderInfo)
            Dim locals As ImmutableArray(Of LocalSymbol) = GetUsingBlockLocals(usingBinder)

            Return New BoundUsingStatement(node, resourceList, resourceExpression, usingBody, usingInfo, locals)
        End Function

        Private Function GetUsingBlockLocals(currentBinder As Binder) As ImmutableArray(Of LocalSymbol)
            Dim usingBlockBinder As UsingBlockBinder

            Do
                usingBlockBinder = TryCast(currentBinder, UsingBlockBinder)

                If usingBlockBinder IsNot Nothing Then
                    Return usingBlockBinder.Locals
                End If

                currentBinder = currentBinder.ContainingBinder
            Loop While currentBinder IsNot Nothing

            Debug.Fail("Failed to find UsingBlockBinder")
            Return ImmutableArray(Of LocalSymbol).Empty
        End Function

        Private Sub VerifyUsingVariableDeclarationAndBuildUsingInfo(
            syntaxNode As SyntaxNode,
            localSymbol As LocalSymbol,
            iDisposable As TypeSymbol,
            placeholderInfo As Dictionary(Of TypeSymbol, ValueTuple(Of BoundRValuePlaceholder, BoundExpression, BoundExpression)),
            diagnostics As DiagnosticBag
        )
            Dim declarationType As TypeSymbol = localSymbol.Type

            ' Explicit array sizes in declarators imply creating an array this is not supported.
            ' The resource variable type needs to implement System.IDisposable and System.Array is known to not implement it.
            If declarationType.IsArrayType Then
                ' this diagnostic will be reported even if a missing initializer was reported before. This is a change 
                ' from Dev10 which stopped "binding" at the first error
                ReportDiagnostic(diagnostics,
                                 syntaxNode,
                                 ErrorFactory.ErrorInfo(ERRID.ERR_UsingResourceVarCantBeArray))

            ElseIf Not declarationType.IsErrorType AndAlso Not iDisposable.IsErrorType Then
                BuildAndVerifyUsingInfo(syntaxNode,
                                        declarationType,
                                        placeholderInfo,
                                        iDisposable,
                                        diagnostics)

                ' TODO: Dev10 shows the error on the symbol name, not the whole declaration. See bug 10720

                ' TODO: we could suppress this warning if the type does not implement IDisposable and is not late bound.
                ' BuildAndVerifyUsingInfo would need to return a boolean then, it has all the information available.
                ReportMutableStructureConstraintsInUsing(declarationType,
                                                         localSymbol.Name,
                                                         syntaxNode,
                                                         diagnostics)
            End If
        End Sub

        Private Sub BuildAndVerifyUsingInfo(
            syntaxNode As SyntaxNode,
            resourceType As TypeSymbol,
            placeholderInfo As Dictionary(Of TypeSymbol, ValueTuple(Of BoundRValuePlaceholder, BoundExpression, BoundExpression)),
            iDisposable As TypeSymbol,
            diagnostics As DiagnosticBag
        )
            If Not placeholderInfo.ContainsKey(resourceType) Then
                ' TODO: add late binding, see statementsemantics.cpp lines 6765++
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                Dim conversionKind = Conversions.ClassifyDirectCastConversion(resourceType, iDisposable, useSiteDiagnostics)

                If diagnostics.Add(syntaxNode, useSiteDiagnostics) Then
                    ' Suppress additional diagnostics
                    diagnostics = New DiagnosticBag()
                End If

                Dim isValidDispose = Conversions.IsWideningConversion(conversionKind)

                If isValidDispose OrElse
                   (resourceType.IsObjectType() AndAlso OptionStrict <> OptionStrict.On) Then
                    Dim resourcePlaceholder = New BoundRValuePlaceholder(syntaxNode, resourceType)
                    Dim disposeConversion As BoundExpression = Nothing
                    Dim disposeCondition As BoundExpression = Nothing

                    If Not resourceType.IsValueType Then
                        disposeConversion = ApplyImplicitConversion(syntaxNode, iDisposable, resourcePlaceholder, diagnostics)

                        disposeCondition = BindIsExpression(resourcePlaceholder,
                                                            New BoundLiteral(syntaxNode, ConstantValue.Nothing, Nothing),
                                                            syntaxNode, True, diagnostics)
                    End If

                    placeholderInfo.Add(resourceType,
                                        New ValueTuple(Of BoundRValuePlaceholder, BoundExpression, BoundExpression)(
                                                resourcePlaceholder,
                                                disposeConversion,
                                                disposeCondition))
                Else
                    ReportDiagnostic(diagnostics,
                                     syntaxNode,
                                     ErrorFactory.ErrorInfo(ERRID.ERR_UsingRequiresDisposePattern, resourceType))
                End If
            End If
        End Sub

        ''' <summary>Check the given type of and report WRN_MutableGenericStructureInUsing if needed.</summary>
        ''' <remarks>This function should only be called for a type of a using variable.</remarks>
        Private Sub ReportMutableStructureConstraintsInUsing(type As TypeSymbol, symbolName As String, syntaxNode As SyntaxNode, diagnostics As DiagnosticBag)
            ' Dev10 #666593: Warn if the type of the variable is not a reference type or an immutable structure.
            If Not type.IsReferenceType Then
                If type.IsTypeParameter Then
                    If type.IsValueType Then
                        Dim typeParameter = DirectCast(type, TypeParameterSymbol)

                        ' there is currently no way to only get the type constraint from a type parameter. So we're
                        ' iterating over all of them 
                        Dim processedValueTypes As Boolean = False
                        For Each constraintType In DirectCast(type, TypeParameterSymbol).ConstraintTypesNoUseSiteDiagnostics
                            ' TODO: if this constraint is a type parameter as well, dig into its constraints as well.

                            ' there is a case where a type constraint of a type parameter is a concrete structure, 
                            ' therefore we'll need to check the type constraint as well.
                            ' See TypeParameter.IsValueType for more information
                            If constraintType.IsValueType Then
                                processedValueTypes = True
                                If ShouldReportMutableStructureInUsing(constraintType) Then
                                    ReportDiagnostic(diagnostics,
                                                     syntaxNode,
                                                     ErrorFactory.ErrorInfo(ERRID.WRN_MutableStructureInUsing, symbolName))
                                    Return
                                End If
                            End If
                        Next

                        ' only show this message if there was no class constraint which is a value type
                        ' this can be the case of a "Structure" constraint with only interface constraints
                        If Not processedValueTypes Then
                            ' the type parameter only has a structure constraint
                            Debug.Assert(typeParameter.HasValueTypeConstraint)
                            ReportDiagnostic(diagnostics,
                                             syntaxNode,
                                             ErrorFactory.ErrorInfo(ERRID.WRN_MutableStructureInUsing, symbolName))
                        End If
                    Else
                        ' it's just a generic type parameter, show generic diagnostics
                        ReportDiagnostic(diagnostics,
                                         syntaxNode,
                                         ErrorFactory.ErrorInfo(ERRID.WRN_MutableGenericStructureInUsing, symbolName))
                    End If
                ElseIf ShouldReportMutableStructureInUsing(type) Then
                    ReportDiagnostic(diagnostics,
                                     syntaxNode,
                                     ErrorFactory.ErrorInfo(ERRID.WRN_MutableStructureInUsing, symbolName))
                End If
            End If
        End Sub

        ' This method decides if a WRN_MutableStructureInUsing should be shown for a given type of the Using variable.
        ' The purpose of this function is to avoid code duplication in 'CheckForMutableStructureConstraints'. 
        ' This is not a general purpose helper.
        Private Shared Function ShouldReportMutableStructureInUsing(structureType As TypeSymbol) As Boolean
            Debug.Assert(structureType.IsValueType)

            If structureType.Kind = SymbolKind.NamedType Then
                If structureType.IsStructureType AndAlso Not structureType.IsEnumType AndAlso Not structureType.IsIntrinsicType Then

                    For Each member In structureType.GetMembersUnordered
                        If member.Kind = SymbolKind.Field AndAlso
                            Not member.IsShared AndAlso Not DirectCast(member, FieldSymbol).IsReadOnly Then

                            Return True
                        End If
                    Next
                Else
                    Debug.Assert(Not structureType.IsVoidType)
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Binds a sync lock block.
        ''' A SyncLock come in the following form:
        ''' 
        ''' SyncLock &lt;expression&gt;
        '''     &lt;body&gt;
        ''' End SyncLock
        ''' </summary>
        ''' <param name="node">The node.</param>
        ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
        Public Function BindSyncLockBlock(node As SyncLockBlockSyntax, diagnostics As DiagnosticBag) As BoundSyncLockStatement

            ' bind the expression
            Dim lockExpression As BoundExpression = BindRValue(node.SyncLockStatement.Expression, diagnostics)
            Dim lockExpressionType = lockExpression.Type
            If Not lockExpression.HasErrors Then
                If Not lockExpressionType.IsReferenceType Then
                    ReportDiagnostic(diagnostics,
                                     lockExpression.Syntax,
                                     ErrorFactory.ErrorInfo(ERRID.ERR_SyncLockRequiresReferenceType1, lockExpressionType))
                End If
            End If

            Dim boundBody = BindBlock(node, node.Statements, diagnostics).MakeCompilerGenerated()
            Return New BoundSyncLockStatement(node, lockExpression, boundBody)
        End Function

        Public Function BindTryBlock(node As TryBlockSyntax, diagnostics As DiagnosticBag) As BoundTryStatement
            Debug.Assert(node IsNot Nothing)

            Dim tryBlock As BoundBlock = BindBlock(node, node.Statements, diagnostics).MakeCompilerGenerated()
            Dim catchBlocks As ImmutableArray(Of BoundCatchBlock) = BindCatchBlocks(node.CatchBlocks, diagnostics)

            Dim finallyBlockOpt As BoundBlock
            If node.FinallyBlock IsNot Nothing Then
                Dim finallyBinder = GetBinder(node.FinallyBlock)
                finallyBlockOpt = finallyBinder.BindBlock(node.FinallyBlock, node.FinallyBlock.Statements, diagnostics)
            Else
                finallyBlockOpt = Nothing
            End If

            If catchBlocks.IsEmpty AndAlso finallyBlockOpt Is Nothing Then
                ReportDiagnostic(diagnostics, node.TryStatement, ERRID.ERR_TryWithoutCatchOrFinally)
            End If

            Dim tryBinder As Binder = GetBinder(node)
            Return New BoundTryStatement(node, tryBlock, catchBlocks, finallyBlockOpt, tryBinder.GetExitLabel(SyntaxKind.ExitTryStatement))
        End Function

        Public Function BindCatchBlocks(catchClauses As SyntaxList(Of CatchBlockSyntax), diagnostics As DiagnosticBag) As ImmutableArray(Of BoundCatchBlock)
            Dim n As Integer = catchClauses.Count
            If n = 0 Then
                Return ImmutableArray(Of BoundCatchBlock).Empty
            End If

            Dim catchBlocks = ArrayBuilder(Of BoundCatchBlock).GetInstance(n)

            For Each catchSyntax In catchClauses
                Dim catchBinder As Binder = GetBinder(catchSyntax)
                Dim catchBlock As BoundCatchBlock = catchBinder.BindCatchBlock(catchSyntax, catchBlocks, diagnostics)
                catchBlocks.Add(catchBlock)
            Next

            Return catchBlocks.ToImmutableAndFree
        End Function

        Private Function BindCatchBlock(node As CatchBlockSyntax, previousBlocks As ArrayBuilder(Of BoundCatchBlock), diagnostics As DiagnosticBag) As BoundCatchBlock

            ' we need to compute the following parts
            Dim catchLocal As LocalSymbol = Nothing
            Dim exceptionSource As BoundExpression = Nothing
            Dim exceptionFilter As BoundExpression = Nothing

            Dim exceptionType As TypeSymbol
            Dim hasError = False
            Dim declaration = node.CatchStatement

            Dim name As IdentifierNameSyntax = declaration.IdentifierName

            Dim asClauseOpt = declaration.AsClause
            ' if we have "as" we need to declare a local
            If asClauseOpt IsNot Nothing Then
                Dim localAccess As BoundLocal = BindCatchVariableDeclaration(name, asClauseOpt, diagnostics)
                exceptionSource = localAccess
                catchLocal = localAccess.LocalSymbol
            ElseIf name IsNot Nothing Then
                exceptionSource = BindSimpleName(name, False, diagnostics)
            End If

            ' verify the catch variable.
            ' 1) must be a local or parameter (byref parameters are ok, static locals are not).
            ' 2) must be or derive from System.Exception

            If exceptionSource IsNot Nothing Then

                Dim originalExceptionValue As BoundExpression = exceptionSource

                If Not exceptionSource.IsValue OrElse exceptionSource.Type Is Nothing OrElse exceptionSource.Type.IsVoidType Then
                    exceptionSource = BadExpression(exceptionSource.Syntax, exceptionSource, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                End If

                exceptionType = exceptionSource.Type

                If originalExceptionValue.HasErrors Then
                    hasError = True
                Else
                    Dim exprKind = exceptionSource.Kind
                    If Not (exprKind = BoundKind.Parameter OrElse
                            exprKind = BoundKind.Local AndAlso Not DirectCast(exceptionSource, BoundLocal).LocalSymbol.IsStatic) Then

                        ReportDiagnostic(diagnostics, name, ERRID.ERR_CatchVariableNotLocal1, name.ToString())
                        hasError = True
                    Else
                        ' type of the catch variable must derive from Exception
                        Debug.Assert(exceptionType IsNot Nothing)

                        Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                        If exceptionType.IsErrorType() Then
                            hasError = True
                        ElseIf Not exceptionType.IsOrDerivedFromWellKnownClass(WellKnownType.System_Exception, Compilation, useSiteDiagnostics) Then
                            ReportDiagnostic(diagnostics,
                                             If(asClauseOpt IsNot Nothing, asClauseOpt.Type, name),
                                             ERRID.ERR_CatchNotException1,
                                             exceptionType)
                            hasError = True
                            diagnostics.Add(If(asClauseOpt IsNot Nothing, asClauseOpt.Type, name), useSiteDiagnostics)
                        End If
                    End If
                End If
            Else
                exceptionType = GetWellKnownType(WellKnownType.System_Exception, node, diagnostics)
            End If

            Dim whenSyntax = declaration.WhenClause
            If whenSyntax IsNot Nothing Then
                exceptionFilter = BindBooleanExpression(whenSyntax.Filter, diagnostics)
            End If

            If Not hasError Then
                Debug.Assert(exceptionType.IsOrDerivedFromWellKnownClass(WellKnownType.System_Exception, Compilation, Nothing))

                For Each previousBlock In previousBlocks
                    If previousBlock.ExceptionFilterOpt IsNot Nothing Then
                        ' filters are considered to have behavior defined at run time
                        ' therefore catches with filters are not considered in this analysis
                        ' the fact that filter may be a compile-time constant is ignored.
                        Continue For
                    End If

                    Dim previousType As TypeSymbol
                    If previousBlock.ExceptionSourceOpt IsNot Nothing Then
                        previousType = previousBlock.ExceptionSourceOpt.Type
                    Else
                        ' do not report diagnostics if type is missing, it should have been already recorded.
                        previousType = Compilation.GetWellKnownType(WellKnownType.System_Exception)
                    End If

                    If Not previousType.IsErrorType() Then
                        If TypeSymbol.Equals(previousType, exceptionType, TypeCompareKind.ConsiderEverything) Then
                            ReportDiagnostic(diagnostics, declaration, ERRID.WRN_DuplicateCatch, exceptionType)
                            Exit For
                        End If

                        Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                        Dim isBaseType As Boolean = exceptionType.IsOrDerivedFrom(previousType, useSiteDiagnostics)

                        diagnostics.Add(declaration, useSiteDiagnostics)

                        If isBaseType Then
                            ReportDiagnostic(diagnostics, declaration, ERRID.WRN_OverlappingCatch, exceptionType, previousType)
                            Exit For
                        End If
                    End If
                Next
            End If

            Dim block = Me.BindBlock(node, node.Statements, diagnostics).MakeCompilerGenerated()
            Return New BoundCatchBlock(node, catchLocal, exceptionSource,
                                       errorLineNumberOpt:=Nothing,
                                       exceptionFilterOpt:=exceptionFilter,
                                       body:=block,
                                       hasErrors:=hasError,
                                       isSynthesizedAsyncCatchAll:=False)
        End Function


        Private Function BindExitStatement(node As ExitStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim targetLabel As LabelSymbol = GetExitLabel(node.Kind)

            If targetLabel Is Nothing Then
                Dim id As ERRID
                Select Case node.Kind
                    Case SyntaxKind.ExitWhileStatement : id = ERRID.ERR_ExitWhileNotWithinWhile
                    Case SyntaxKind.ExitTryStatement : id = ERRID.ERR_ExitTryNotWithinTry
                    Case SyntaxKind.ExitDoStatement : id = ERRID.ERR_ExitDoNotWithinDo
                    Case SyntaxKind.ExitForStatement : id = ERRID.ERR_ExitForNotWithinFor
                    Case SyntaxKind.ExitSelectStatement : id = ERRID.ERR_ExitSelectNotWithinSelect
                    Case SyntaxKind.ExitSubStatement : id = ERRID.ERR_ExitSubOfFunc
                    Case SyntaxKind.ExitFunctionStatement : id = ERRID.ERR_ExitFuncOfSub
                    Case SyntaxKind.ExitPropertyStatement : id = ERRID.ERR_ExitPropNot
                    Case Else : ExceptionUtilities.UnexpectedValue(node.Kind)
                End Select

                ReportDiagnostic(diagnostics, node, id)

                Return New BoundBadStatement(node, ImmutableArray(Of BoundNode).Empty, hasErrors:=True)
            Else
                Return New BoundExitStatement(node, targetLabel)
            End If

        End Function

        Private Function BindContinueStatement(node As ContinueStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim targetLabel As LabelSymbol = GetContinueLabel(node.Kind)

            If targetLabel Is Nothing Then
                Dim id As ERRID
                Select Case node.Kind
                    Case SyntaxKind.ContinueWhileStatement : id = ERRID.ERR_ContinueWhileNotWithinWhile
                    Case SyntaxKind.ContinueDoStatement : id = ERRID.ERR_ContinueDoNotWithinDo
                    Case SyntaxKind.ContinueForStatement : id = ERRID.ERR_ContinueForNotWithinFor
                    Case Else : ExceptionUtilities.UnexpectedValue(node.Kind)
                End Select

                ReportDiagnostic(diagnostics, node, id)
                Return New BoundBadStatement(node, ImmutableArray(Of BoundNode).Empty, hasErrors:=True)
            Else
                Return New BoundContinueStatement(node, targetLabel)
            End If

        End Function

        Private Function BindBooleanExpression(node As ExpressionSyntax, diagnostics As DiagnosticBag) As BoundExpression
            ' 11.19 Boolean Expressions
            Dim expr As BoundExpression = Me.BindValue(node, diagnostics, isOperandOfConditionalBranch:=True)
            Dim boolSymbol As NamedTypeSymbol = GetSpecialType(SpecialType.System_Boolean, node, diagnostics)

            ' NOTE: Errors in 'expr' will be handles properly by ApplyImplicitConversion(...)
            Return Me.ApplyImplicitConversion(node, boolSymbol, expr, diagnostics, isOperandOfConditionalBranch:=True)
        End Function

        Private Function GetCurrentReturnType(<Out()> ByRef isAsync As Boolean,
                                            <Out()> ByRef isIterator As Boolean,
                                            <Out()> ByRef methodReturnType As TypeSymbol) As TypeSymbol
            isAsync = False
            isIterator = False

            Dim method As MethodSymbol = TryCast(Me.ContainingMember, MethodSymbol)

            If method IsNot Nothing Then
                methodReturnType = method.ReturnType
                isAsync = method.IsAsync

                ' method cannot be both iterator and async, so async will win here.
                isIterator = Not isAsync AndAlso method.IsIterator

                If Not method.IsSub Then
                    If isAsync AndAlso method.ReturnType.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T)) Then
                        Return DirectCast(method.ReturnType, NamedTypeSymbol).TypeArgumentsNoUseSiteDiagnostics(0)

                    ElseIf isAsync AndAlso method.ReturnType.Equals(Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task)) Then
                        ' I don't believe we need to report use-site error here because we are using System.Void just as an indicator that we don't expect any value to be returned.
                        Return Compilation.GetSpecialType(SpecialType.System_Void)

                    ElseIf isIterator Then
                        ' I don't believe we need to report use-site error here because we are using System.Void just as an indicator that we don't expect any value to be returned.
                        Return Compilation.GetSpecialType(SpecialType.System_Void)
                    End If
                End If

                Return methodReturnType
            End If

            methodReturnType = ErrorTypeSymbol.UnknownResultType
            Return methodReturnType
        End Function

        Private Function BindReturn(originalSyntax As ReturnStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim expressionSyntax As expressionSyntax = originalSyntax.Expression

            ' UNDONE - Handle ERRID_ReturnFromEventMethod

            ' If we are in a lambda binding, then the original syntax could be just the expression.
            ' If we are not in a lambda binding then we have a real return statement.

            Debug.Assert(originalSyntax IsNot Nothing)
            Debug.Assert(expressionSyntax Is originalSyntax OrElse originalSyntax.Expression Is expressionSyntax)

            Dim isAsync As Boolean
            Dim isIterator As Boolean

            Dim methodReturnType As TypeSymbol = Nothing
            Dim retType As TypeSymbol = Me.GetCurrentReturnType(isAsync, isIterator, methodReturnType)
            Dim returnLabel = GetReturnLabel()

            If BindingTopLevelScriptCode Then
                ReportDiagnostic(diagnostics, originalSyntax, ERRID.ERR_KeywordNotAllowedInScript, SyntaxFacts.GetText(SyntaxKind.ReturnKeyword))
                Return New BoundReturnStatement(originalSyntax, Nothing, Nothing, returnLabel, hasErrors:=True)
            End If

            If retType.SpecialType = SpecialType.System_Void Then
                ' For subs just generate a return
                If expressionSyntax IsNot Nothing Then
                    ReportDiagnostic(diagnostics, originalSyntax,
                                     If(isIterator,
                                          ERRID.ERR_BadReturnValueInIterator,
                                          If(isAsync AndAlso Not methodReturnType.SpecialType = SpecialType.System_Void, ERRID.ERR_ReturnFromNonGenericTaskAsync, ERRID.ERR_ReturnFromNonFunction)))

                    Dim arg As BoundExpression = Me.BindValue(expressionSyntax, diagnostics)
                    arg = MakeRValueAndIgnoreDiagnostics(arg)
                    Return New BoundReturnStatement(originalSyntax, arg, Nothing, returnLabel, hasErrors:=True)
                End If

                Return New BoundReturnStatement(originalSyntax, Nothing, Nothing, returnLabel)
            Else
                Dim arg As BoundExpression = Nothing
                If expressionSyntax IsNot Nothing Then
                    arg = Me.BindValue(expressionSyntax, diagnostics)
                End If

                If arg IsNot Nothing Then
                    If retType Is LambdaSymbol.ReturnTypeIsUnknown Then
                        ' We will have LambdaSymbol.ReturnTypeIsUnknown as the target return type
                        ' if we are inside a lambda, for which we failed to infer the return type.
                        Debug.Assert(Me.ContainingMember.Kind = SymbolKind.Method AndAlso DirectCast(Me.ContainingMember, MethodSymbol).MethodKind = MethodKind.LambdaMethod)

                        ' We want to make sure that the return expressions are as bound as they can be.
                        ' For example, if return expression is a lambda, we want it to be a BoundLambda, not
                        ' an UnboundLambda node (that is what BindValue stops at). In order to get to BoundLambda,
                        ' we need to call MakeRValue, but we will ignore any diagnostics it will produce because
                        ' an inference error has been reported earlier.
                        arg = MakeRValueAndIgnoreDiagnostics(arg)
                    ElseIf retType Is LambdaSymbol.ReturnTypeIsBeingInferred Then
                        ' Target retType can be LambdaSymbol.ReturnTypeIsBeingInferred if we are inferring return type of a lambda.
                        ' We should leave expression unmodified, to allow more accurate inference.
                        Debug.Assert(Me.ContainingMember.Kind = SymbolKind.Method AndAlso DirectCast(Me.ContainingMember, MethodSymbol).MethodKind = MethodKind.LambdaMethod)
                    Else
                        ' If we were in an async method that returned Task<T> for some T,
                        ' and the return expression can't be converted to T but is identical to Task<T>,
                        ' then don't give the normal error message about "there is no conversion from T to Task<T>"
                        ' and instead say "Since this is async, the return expression must be 'T' rather than 'Task<T>'."
                        Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                        If isAsync AndAlso Not retType.IsErrorType() AndAlso methodReturnType.Equals(arg.Type) AndAlso
                            methodReturnType.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T)) AndAlso
                            Not Conversions.ConversionExists(Conversions.ClassifyConversion(arg, retType, Me, useSiteDiagnostics).Key) Then

                            If Not diagnostics.Add(arg, useSiteDiagnostics) Then
                                ReportDiagnostic(diagnostics, arg.Syntax, ERRID.ERR_BadAsyncReturnOperand1, retType)
                            End If

                            arg = MakeRValueAndIgnoreDiagnostics(arg)
                        Else
                            arg = ApplyImplicitConversion(arg.Syntax, retType, arg, diagnostics)
                        End If
                    End If
                End If

                ' For functions generate return expression with local return value symbol
                If arg IsNot Nothing Then
                    Return New BoundReturnStatement(originalSyntax, arg, GetLocalForFunctionValue(), returnLabel)
                Else
                    If isAsync AndAlso retType Is LambdaSymbol.ReturnTypeIsBeingInferred Then
                        ' Target retType can be LambdaSymbol.ReturnTypeIsBeingInferred if we are inferring return type of a lambda.
                        ' We should not require expression, to allow more accurate inference.
                        Debug.Assert(Me.ContainingMember.Kind = SymbolKind.Method AndAlso DirectCast(Me.ContainingMember, MethodSymbol).MethodKind = MethodKind.LambdaMethod)
                        Return New BoundReturnStatement(originalSyntax, Nothing, Nothing, returnLabel, hasErrors:=False)
                    Else
                        ReportDiagnostic(diagnostics, originalSyntax, ERRID.ERR_ReturnWithoutValue)
                        Return New BoundReturnStatement(originalSyntax, Nothing, Nothing, returnLabel, hasErrors:=True)
                    End If
                End If
            End If
        End Function

        Private Function GetCurrentYieldType(node As YieldStatementSyntax,
                                             diagnostics As DiagnosticBag) As TypeSymbol

            Dim method As MethodSymbol = TryCast(Me.ContainingMember, MethodSymbol)

            If method IsNot Nothing Then
                Dim methodReturnType = method.ReturnType

                If Not method.IsIterator Then
                    ' no idea how we can get here since parsing of Yield is contextual, but there is an error ID for this
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_BadYieldInNonIteratorMethod)
                End If

                If Not method.IsSub Then
                    Dim returnNamedType = TryCast(methodReturnType.OriginalDefinition, NamedTypeSymbol)
                    Dim returnSpecialType As SpecialType = If(returnNamedType IsNot Nothing, returnNamedType.SpecialType, Nothing)

                    If returnSpecialType = SpecialType.System_Collections_Generic_IEnumerable_T OrElse
                        returnSpecialType = SpecialType.System_Collections_Generic_IEnumerator_T Then

                        Return DirectCast(methodReturnType, NamedTypeSymbol).TypeArgumentsNoUseSiteDiagnostics(0)
                    End If

                    If returnSpecialType = SpecialType.System_Collections_IEnumerable OrElse
                        returnSpecialType = SpecialType.System_Collections_IEnumerator Then

                        Return GetSpecialType(SpecialType.System_Object, node, diagnostics)
                    End If

                End If

                If methodReturnType Is LambdaSymbol.ReturnTypeIsUnknown OrElse
                    methodReturnType Is LambdaSymbol.ReturnTypeIsBeingInferred Then

                    Return methodReturnType
                End If

            End If

            ' It is either nongeneric iterator or an error (which should be already reported). 
            Return ErrorTypeSymbol.UnknownResultType
        End Function

        Private Function BindYield(originalSyntax As YieldStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim expressionSyntax As expressionSyntax = originalSyntax.Expression
            Dim arg As BoundExpression = Me.BindValue(expressionSyntax, diagnostics)

            If BindingTopLevelScriptCode Then
                ReportDiagnostic(diagnostics, originalSyntax, ERRID.ERR_KeywordNotAllowedInScript, SyntaxFacts.GetText(SyntaxKind.YieldKeyword))
                arg = MakeRValueAndIgnoreDiagnostics(arg)
                Return New BoundYieldStatement(originalSyntax, arg, hasErrors:=True)
            End If

            Dim retType As TypeSymbol = Me.GetCurrentYieldType(originalSyntax, diagnostics)

            If retType Is LambdaSymbol.ReturnTypeIsUnknown Then
                ' We will have LambdaSymbol.ReturnTypeIsUnknown as the target return type
                ' if we are inside a lambda, for which we failed to infer the return type.
                Debug.Assert(Me.ContainingMember.Kind = SymbolKind.Method AndAlso DirectCast(Me.ContainingMember, MethodSymbol).MethodKind = MethodKind.LambdaMethod)

                ' We want to make sure that the return expressions are as bound as they can be.
                ' For example, if return expression is a lambda, we want it to be a BoundLambda, not
                ' an UnboundLambda node (that is what BindValue stops at). In order to get to BoundLambda,
                ' we need to call MakeRValue, but we will ignore any diagnostics it will produce because
                ' an inference error has been reported earlier.
                arg = MakeRValueAndIgnoreDiagnostics(arg)

            ElseIf retType Is LambdaSymbol.ReturnTypeIsBeingInferred Then
                ' Target retType can be LambdaSymbol.ReturnTypeIsBeingInferred if we are inferring return type of a lambda.
                ' We should leave expression unmodified, to allow more accurate inference.
                Debug.Assert(Me.ContainingMember.Kind = SymbolKind.Method AndAlso DirectCast(Me.ContainingMember, MethodSymbol).MethodKind = MethodKind.LambdaMethod)
                Return New BoundYieldStatement(originalSyntax, arg, hasErrors:=False, returnTypeIsBeingInferred:=True)
            Else
                arg = ApplyImplicitConversion(arg.Syntax, retType, arg, diagnostics)
            End If

            Return New BoundYieldStatement(originalSyntax, arg)
        End Function

        Private Function BindThrow(node As ThrowStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim expressionSyntax As expressionSyntax = node.Expression
            Dim hasError As Boolean = False

            If expressionSyntax Is Nothing Then
                Dim curSyntax As VisualBasicSyntaxNode = node.Parent
                Dim canRethrow As Boolean = False

                While curSyntax IsNot Nothing
                    Select Case curSyntax.Kind
                        Case SyntaxKind.CatchBlock
                            canRethrow = True
                            Exit While

                        Case SyntaxKind.FinallyBlock
                            ' CLI spec (with Microsoft specific implementation notes).
                            ' 12.4.2.8.2.2 rethrow:
                            ' The Microsoft implementation requires that either the catch handler is the innermost enclosing
                            ' exception-handling block, or all intervening exception-handling blocks are protected regions.
                            Exit While

                        Case SyntaxKind.SingleLineFunctionLambdaExpression
                        Case SyntaxKind.MultiLineFunctionLambdaExpression
                        Case SyntaxKind.SingleLineSubLambdaExpression
                        Case SyntaxKind.MultiLineSubLambdaExpression
                            Exit While

                        Case Else
                            If TypeOf curSyntax Is MethodBlockBaseSyntax Then
                                Exit While
                            End If

                    End Select
                    curSyntax = curSyntax.Parent
                End While

                If Not canRethrow Then
                    hasError = True
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_MustBeInCatchToRethrow)
                End If

                Return New BoundThrowStatement(node, Nothing, hasError)
            Else
                Dim value As BoundExpression = BindRValue(expressionSyntax, diagnostics)

                Dim exceptionType = value.Type
                If Not exceptionType.IsErrorType Then
                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                    If Not exceptionType.IsOrDerivedFromWellKnownClass(WellKnownType.System_Exception, Compilation, useSiteDiagnostics) Then
                        hasError = True
                        ReportDiagnostic(diagnostics, node, ERRID.ERR_CantThrowNonException, exceptionType)
                        diagnostics.Add(node, useSiteDiagnostics)
                    End If
                Else
                    hasError = True
                End If

                Return New BoundThrowStatement(node, value, hasError)
            End If
        End Function

        Private Function BindError(node As ErrorStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            Dim value As BoundExpression = ApplyImplicitConversion(node.ErrorNumber,
                                                                   GetSpecialType(SpecialType.System_Int32, node.ErrorNumber, diagnostics),
                                                                   BindValue(node.ErrorNumber, diagnostics),
                                                                   diagnostics)

            Return New BoundThrowStatement(node, value)
        End Function

        Private Function BindResumeStatement(node As ResumeStatementSyntax, diagnostics As DiagnosticBag) As BoundResumeStatement

            If IsInLambda Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_MultilineLambdasCannotContainOnError)
            ElseIf IsInAsyncContext() OrElse IsInIteratorContext() Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_ResumablesCannotContainOnError)
            End If

            Select Case node.Kind
                Case SyntaxKind.ResumeStatement
                    Return New BoundResumeStatement(node)

                Case SyntaxKind.ResumeNextStatement
                    Return New BoundResumeStatement(node, isNext:=True)

                Case SyntaxKind.ResumeLabelStatement
                    Dim symbol As LabelSymbol = Nothing

                    Dim boundLabelExpression As BoundExpression = BindExpression(node.Label, diagnostics)

                    If boundLabelExpression.Kind = BoundKind.Label Then
                        Dim boundLabel = DirectCast(boundLabelExpression, boundLabel)
                        symbol = boundLabel.Label

                        Return New BoundResumeStatement(node, symbol, boundLabel, hasErrors:=Not IsValidLabelForGoto(symbol, node.Label, diagnostics))
                    Else
                        ' if the bound label is e.g. a bad bound expression because of a non-existent label, 
                        ' make this a bad statement.
                        Return New BoundResumeStatement(node, Nothing, boundLabelExpression, hasErrors:=True)
                    End If
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

        Private Function BindOnErrorStatement(node As StatementSyntax, diagnostics As DiagnosticBag) As BoundOnErrorStatement

            If IsInLambda Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_MultilineLambdasCannotContainOnError)
            ElseIf IsInAsyncContext() OrElse IsInIteratorContext() Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_ResumablesCannotContainOnError)
            End If

            Select Case node.Kind
                Case SyntaxKind.OnErrorGoToMinusOneStatement
                    Return New BoundOnErrorStatement(node, OnErrorStatementKind.GoToMinusOne, Nothing, Nothing)

                Case SyntaxKind.OnErrorGoToZeroStatement
                    Return New BoundOnErrorStatement(node, OnErrorStatementKind.GoToZero, Nothing, Nothing)

                Case SyntaxKind.OnErrorResumeNextStatement
                    Return New BoundOnErrorStatement(node, OnErrorStatementKind.ResumeNext, Nothing, Nothing)

                Case SyntaxKind.OnErrorGoToLabelStatement
                    Dim onError = DirectCast(node, OnErrorGoToStatementSyntax)
                    Dim symbol As LabelSymbol = Nothing

                    Dim boundLabelExpression As BoundExpression = BindExpression(onError.Label, diagnostics)

                    If boundLabelExpression.Kind = BoundKind.Label Then
                        Dim boundLabel = DirectCast(boundLabelExpression, boundLabel)
                        symbol = boundLabel.Label

                        Return New BoundOnErrorStatement(node, symbol, boundLabel, hasErrors:=Not IsValidLabelForGoto(symbol, onError.Label, diagnostics))
                    Else
                        ' if the bound label is e.g. a bad bound expression because of a non-existent label, 
                        ' make this a bad statement.
                        Return New BoundOnErrorStatement(node, Nothing, boundLabelExpression, hasErrors:=True)
                    End If
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

        Private Function BindStopStatement(stopStatementSyntax As StopOrEndStatementSyntax) As BoundStatement
            Return New BoundStopStatement(stopStatementSyntax)
        End Function

        Private Function BindEndStatement(endStatementSyntax As StopOrEndStatementSyntax, diagnostics As DiagnosticBag) As BoundStatement
            If Not Compilation.Options.OutputKind.IsApplication() Then
                ReportDiagnostic(diagnostics, endStatementSyntax, ERRID.ERR_EndDisallowedInDllProjects)
            End If

            Return New BoundEndStatement(endStatementSyntax)
        End Function
    End Class
End Namespace

