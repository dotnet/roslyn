' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class LocalRewriter

        Private Const s_activeHandler_None As Integer = 0
        Private Const s_activeHandler_ResumeNext As Integer = 1
        Private Const s_activeHandler_FirstNonReservedOnErrorGotoIndex As Integer = 2
        Private Const s_activeHandler_FirstOnErrorResumeNextIndex As Integer = -2

        Private Structure UnstructuredExceptionHandlingState
            Public Context As BoundUnstructuredExceptionHandlingStatement
            Public ExceptionHandlers As ArrayBuilder(Of BoundGotoStatement)
            Public ResumeTargets As ArrayBuilder(Of BoundGotoStatement)
            Public OnErrorResumeNextCount As Integer
            Public ActiveHandlerTemporary As LocalSymbol
            Public ResumeTargetTemporary As LocalSymbol
            Public CurrentStatementTemporary As LocalSymbol
            Public ResumeNextLabel As LabelSymbol
            Public ResumeLabel As LabelSymbol
        End Structure

        Public Overrides Function VisitUnstructuredExceptionHandlingStatement(node As BoundUnstructuredExceptionHandlingStatement) As BoundNode
            Debug.Assert(_currentLineTemporary Is Nothing)

            If Not node.TrackLineNumber Then
                Return RewriteUnstructuredExceptionHandlingStatementIntoBlock(node)
            End If

            Dim nodeFactory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)
            Dim int32 = nodeFactory.SpecialType(SpecialType.System_Int32)
            _currentLineTemporary = New SynthesizedLocal(_topMethod, int32, SynthesizedLocalKind.OnErrorCurrentLine, DirectCast(nodeFactory.Syntax, StatementSyntax))

            Dim body As BoundBlock

            If node.ContainsOnError OrElse node.ContainsResume Then
                body = RewriteUnstructuredExceptionHandlingStatementIntoBlock(node)
            Else
                body = DirectCast(VisitBlock(node.Body), BoundBlock)
            End If

            body = body.Update(body.StatementListSyntax,
                               If(body.Locals.IsEmpty,
                                  ImmutableArray.Create(Of LocalSymbol)(_currentLineTemporary),
                                  body.Locals.Add(_currentLineTemporary)),
                               body.Statements)

            _currentLineTemporary = Nothing

            Return body
        End Function

        Private Function RewriteUnstructuredExceptionHandlingStatementIntoBlock(node As BoundUnstructuredExceptionHandlingStatement) As BoundBlock
            Debug.Assert(node.ContainsOnError OrElse node.ContainsResume)

            Debug.Assert(_unstructuredExceptionHandling.Context Is Nothing)
            Debug.Assert(_unstructuredExceptionHandling.ExceptionHandlers Is Nothing)
            Debug.Assert(_unstructuredExceptionHandling.ResumeTargets Is Nothing)
            Debug.Assert(_unstructuredExceptionHandling.OnErrorResumeNextCount = 0)
            Debug.Assert(_unstructuredExceptionHandling.ActiveHandlerTemporary Is Nothing)
            Debug.Assert(_unstructuredExceptionHandling.ResumeTargetTemporary Is Nothing)
            Debug.Assert(_unstructuredExceptionHandling.CurrentStatementTemporary Is Nothing)
            Debug.Assert(_unstructuredExceptionHandling.ResumeNextLabel Is Nothing)
            Debug.Assert(_unstructuredExceptionHandling.ResumeLabel Is Nothing)

            Debug.Assert(_currentMethodOrLambda Is _topMethod)

            ' We should emit code that is equivalent to the following:
            '-----------------------------------------------------------------------
            '     Dim VB$ActiveHandler As Integer
            '     Dim VB$ResumeTarget As Integer
            '     Dim VB$CurrentStatement As Integer
            '     
            '     Try
            '         <Body>
            '         Goto Done
            '
            ' ResumeSwitch: ' Destination of ResumeNext and Resume jumps
            '         Select Case If(<Resume>, VB$ResumeTarget, VB$ResumeTarget + 1)
            '             Case 0:
            '                 Goto ResumeSwitchFallThrough
            '             Case 1:
            '                 Goto <resume target #1>
            '             ...
            '         End Select
            '
            ' ResumeSwitchFallThrough:
            '         Goto OnErrorFailure
            '
            ' OnError:
            '         VB$ResumeTarget = VB$CurrentStatement
            '
            '         Select Case VB$ActiveHandler
            '             Case 0:
            '                 Goto OnErrorSwitchFallThrough
            '             Case 1:
            '                 Goto ResumeNext
            '             Case 2:
            '                 Goto <OnError Handler #1>
            '             ...
            '         End Select
            '
            ' OnErrorSwitchFallThrough:
            '         Goto OnErrorFailure
            '
            '     Catch e As Exception When (VB$ActiveHandler <> 0) And (VB$ResumeTarget = 0)
            '         Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(e)
            '         Goto OnError
            '     End Try
            '
            ' OnErrorFailure:
            '     Throw Microsoft.VisualBasic.CompilerServices.ProjectData.CreateProjectError(&H800A0033)
            '
            ' Done:
            '     If VB$ResumeTarget <> 0
            '         Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()
            '     End If
            '-----------------------------------------------------------------------
            '
            ' When a resumable statement is rewritten, code that changes VB$CurrentStatement local is 
            ' injected before it and corresponding resume target is registered in unstructuredExceptionHandling.ResumeTargets.
            '
            ' When [On Error] statement is rewritten, code that changes VB$ActiveHandler local is injected and corresponding
            ' handler is registered in unstructuredExceptionHandling.ExceptionHandlers.

            Dim locals = ArrayBuilder(Of LocalSymbol).GetInstance()

            _unstructuredExceptionHandling.Context = node
            _unstructuredExceptionHandling.ExceptionHandlers = ArrayBuilder(Of BoundGotoStatement).GetInstance()
            _unstructuredExceptionHandling.OnErrorResumeNextCount = 0

            Dim nodeFactory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)
            Dim int32 = nodeFactory.SpecialType(SpecialType.System_Int32)
            Dim bool = nodeFactory.SpecialType(SpecialType.System_Boolean)

            _unstructuredExceptionHandling.ActiveHandlerTemporary = New SynthesizedLocal(_topMethod, int32, SynthesizedLocalKind.OnErrorActiveHandler, DirectCast(nodeFactory.Syntax, StatementSyntax))
            locals.Add(_unstructuredExceptionHandling.ActiveHandlerTemporary)
            _unstructuredExceptionHandling.ResumeTargetTemporary = New SynthesizedLocal(_topMethod, int32, SynthesizedLocalKind.OnErrorResumeTarget, DirectCast(nodeFactory.Syntax, StatementSyntax))
            locals.Add(_unstructuredExceptionHandling.ResumeTargetTemporary)

            If node.ResumeWithoutLabelOpt IsNot Nothing Then
                _unstructuredExceptionHandling.CurrentStatementTemporary = New SynthesizedLocal(_topMethod, int32, SynthesizedLocalKind.OnErrorCurrentStatement, DirectCast(nodeFactory.Syntax, StatementSyntax))
                locals.Add(_unstructuredExceptionHandling.CurrentStatementTemporary)
                _unstructuredExceptionHandling.ResumeNextLabel = New GeneratedLabelSymbol("$VB$UnstructuredExceptionHandling_ResumeNext")
                _unstructuredExceptionHandling.ResumeLabel = New GeneratedLabelSymbol("$VB$UnstructuredExceptionHandling_Resume")
                _unstructuredExceptionHandling.ResumeTargets = ArrayBuilder(Of BoundGotoStatement).GetInstance()
            End If

            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance()

            ' Rewrite the body.
            statements.Add(DirectCast(Visit(node.Body), BoundBlock))

            ' We reach this statement if there were no exceptions. 
            If Instrument Then
                statements.Add(SyntheticBoundNodeFactory.HiddenSequencePoint())
            End If

            If _unstructuredExceptionHandling.CurrentStatementTemporary IsNot Nothing Then
                RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, False, statements)
            End If

            Dim doneLabel = New GeneratedLabelSymbol("$VB$UnstructuredExceptionHandling_Done")
            statements.Add(nodeFactory.Goto(doneLabel))

            Dim onErrorFailureLabel = New GeneratedLabelSymbol("$VB$UnstructuredExceptionHandling_OnErrorFailure")

            If node.ResumeWithoutLabelOpt IsNot Nothing Then
                Dim resumeSwitchFallThroughLabel = New GeneratedLabelSymbol("$VB$UnstructuredExceptionHandling_ResumeSwitchFallThrough")
                Dim resumeSwitchJumps(1 + _unstructuredExceptionHandling.ResumeTargets.Count - 1) As BoundGotoStatement

                ' The 0th Resume table entry falls through and
                ' branches to failure because the ResumeTarget should never be 0.
                resumeSwitchJumps(s_activeHandler_None) = nodeFactory.Goto(resumeSwitchFallThroughLabel)

                For i As Integer = 0 To _unstructuredExceptionHandling.ResumeTargets.Count - 1
                    resumeSwitchJumps(i + 1) = _unstructuredExceptionHandling.ResumeTargets(i)
                Next

                statements.Add(New BoundUnstructuredExceptionResumeSwitch(node.Syntax,
                                                                          nodeFactory.Local(_unstructuredExceptionHandling.ResumeTargetTemporary, isLValue:=False),
                                                                          nodeFactory.Label(_unstructuredExceptionHandling.ResumeLabel),
                                                                          nodeFactory.Label(_unstructuredExceptionHandling.ResumeNextLabel),
                                                                          resumeSwitchJumps.AsImmutableOrNull()))

                ' The fall through case will branch to On Error Failure which throws an internal error exception.
                ' Should never get here at runtime unless something has gone wrong.
                statements.Add(nodeFactory.Label(resumeSwitchFallThroughLabel))
                statements.Add(nodeFactory.Goto(onErrorFailureLabel))
            End If

            Dim onErrorLabel = New GeneratedLabelSymbol("$VB$UnstructuredExceptionHandling_OnError")
            statements.Add(nodeFactory.Label(onErrorLabel))

            ' Remember the current resume target.
            ' If we are not keeping track of the current line, then just
            ' store a non-zero into resumeTargetTemporary so we know we're in a
            ' handler. Otherwise, store the current line into the resume local
            ' so we know where to resume and to indicate that we're currently
            ' in a handler.
            '
            statements.Add(nodeFactory.AssignmentExpression(nodeFactory.Local(_unstructuredExceptionHandling.ResumeTargetTemporary, isLValue:=True),
                                                            If(_unstructuredExceptionHandling.CurrentStatementTemporary Is Nothing,
                                                               DirectCast(nodeFactory.Literal(-1), BoundExpression),
                                                               nodeFactory.Local(_unstructuredExceptionHandling.CurrentStatementTemporary, isLValue:=False))).ToStatement())

            ' Generate switch that jumps to the active handler
            Dim onErrorSwitchFallThroughLabel = New GeneratedLabelSymbol("$VB$UnstructuredExceptionHandling_OnErrorSwitchFallThrough")
            Dim onErrorSwitchJumps(2 + _unstructuredExceptionHandling.ExceptionHandlers.Count - 1) As BoundGotoStatement

            onErrorSwitchJumps(s_activeHandler_None) = nodeFactory.Goto(onErrorSwitchFallThroughLabel)
            onErrorSwitchJumps(s_activeHandler_ResumeNext) = nodeFactory.Goto(If(node.ResumeWithoutLabelOpt IsNot Nothing,
                                                                               _unstructuredExceptionHandling.ResumeNextLabel,
                                                                               onErrorSwitchFallThroughLabel))

            For i As Integer = 0 To _unstructuredExceptionHandling.ExceptionHandlers.Count - 1
                onErrorSwitchJumps(s_activeHandler_FirstNonReservedOnErrorGotoIndex + i) = _unstructuredExceptionHandling.ExceptionHandlers(i)
            Next

            ' When resume is present and we are not optimizing:
            ' Determine if the handler index is less than or equal to -2 (ActiveHandler_FirstOnErrorResumeNextIndex):
            ' If so, replace it with ActiveHandler_ResumeNext and jump to the switch.
            statements.Add(New BoundUnstructuredExceptionOnErrorSwitch(node.Syntax,
                                                                       If(node.ResumeWithoutLabelOpt IsNot Nothing AndAlso OptimizationLevelIsDebug,
                                                                          nodeFactory.Conditional(nodeFactory.Binary(BinaryOperatorKind.GreaterThan,
                                                                                                                     bool,
                                                                                                                     nodeFactory.Local(_unstructuredExceptionHandling.ActiveHandlerTemporary, isLValue:=False),
                                                                                                                     nodeFactory.Literal(s_activeHandler_FirstOnErrorResumeNextIndex)),
                                                                                                  nodeFactory.Local(_unstructuredExceptionHandling.ActiveHandlerTemporary, isLValue:=False),
                                                                                                  nodeFactory.Literal(s_activeHandler_ResumeNext),
                                                                                                  int32),
                                                                          DirectCast(nodeFactory.Local(_unstructuredExceptionHandling.ActiveHandlerTemporary, isLValue:=False), BoundExpression)),
                                                                       onErrorSwitchJumps.AsImmutableOrNull()))

            ' Something has gone wrong with the On Error mechanism if execution
            ' makes it here.  Branch to the Failure block which will throw an internal
            ' error exception.
            statements.Add(nodeFactory.Label(onErrorSwitchFallThroughLabel))
            statements.Add(nodeFactory.Goto(onErrorFailureLabel))

            ' Done with content of the try block.
            Dim tryBlock = nodeFactory.Block(statements.ToImmutable())
            statements.Clear()

            statements.Add(RewriteTryStatement(
                node.Syntax,
                tryBlock,
                ImmutableArray.Create(New BoundCatchBlock(
                    node.Syntax,
                    Nothing,
                    Nothing,
                    If(_currentLineTemporary IsNot Nothing,
                        New BoundLocal(node.Syntax, _currentLineTemporary, isLValue:=False, type:=_currentLineTemporary.Type),
                        Nothing),
                    New BoundUnstructuredExceptionHandlingCatchFilter(node.Syntax,
                        nodeFactory.Local(_unstructuredExceptionHandling.ActiveHandlerTemporary, isLValue:=False),
                        nodeFactory.Local(_unstructuredExceptionHandling.ResumeTargetTemporary, isLValue:=False),
                        bool),
                    nodeFactory.Block(ImmutableArray.Create(Of BoundStatement)(nodeFactory.Goto(onErrorLabel))),
                    isSynthesizedAsyncCatchAll:=False)),
                Nothing,
                Nothing))

            ' Something has gone wrong with the On Error mechanism if execution
            ' makes it here.  
            statements.Add(nodeFactory.Label(onErrorFailureLabel))
            Dim createProjectError As MethodSymbol = nodeFactory.WellKnownMember(Of MethodSymbol)(WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__CreateProjectError)

            If createProjectError IsNot Nothing Then
                Const E_INTERNALERROR As Integer = &H800A0033 ' 51

                statements.Add(nodeFactory.Throw(New BoundCall(node.Syntax, createProjectError, Nothing, Nothing,
                                                               ImmutableArray.Create(Of BoundExpression)(nodeFactory.Literal(E_INTERNALERROR)),
                                                               Nothing, createProjectError.ReturnType)))
            End If

            statements.Add(nodeFactory.Label(doneLabel))

            ' Upon exiting the method, the Err object is cleared, but only if an error has occurred and it
            ' has not been "handled" (i.e., a resume, resume next, or On Error Goto -1 has not been encountered).
            ' Generate a check of the resume local;  if it's non-zero, then clear the Err object.
            Dim clearProjectError As MethodSymbol = nodeFactory.WellKnownMember(Of MethodSymbol)(WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__ClearProjectError)

            If clearProjectError IsNot Nothing Then
                statements.Add(RewriteIfStatement(node.Syntax,
                                                  nodeFactory.Binary(BinaryOperatorKind.NotEquals,
                                                                     bool,
                                                                     nodeFactory.Local(_unstructuredExceptionHandling.ResumeTargetTemporary, isLValue:=False),
                                                                     nodeFactory.Literal(0)),
                                                  New BoundCall(node.Syntax, clearProjectError, Nothing, Nothing, ImmutableArray(Of BoundExpression).Empty, Nothing, clearProjectError.ReturnType).ToStatement(),
                                                  Nothing,
                                                  instrumentationTargetOpt:=Nothing))
            End If

            _unstructuredExceptionHandling.Context = Nothing
            _unstructuredExceptionHandling.ExceptionHandlers.Free()
            _unstructuredExceptionHandling.ExceptionHandlers = Nothing

            If _unstructuredExceptionHandling.ResumeTargets IsNot Nothing Then
                _unstructuredExceptionHandling.ResumeTargets.Free()
                _unstructuredExceptionHandling.ResumeTargets = Nothing
            End If

            _unstructuredExceptionHandling.ActiveHandlerTemporary = Nothing
            _unstructuredExceptionHandling.ResumeTargetTemporary = Nothing
            _unstructuredExceptionHandling.CurrentStatementTemporary = Nothing
            _unstructuredExceptionHandling.ResumeNextLabel = Nothing
            _unstructuredExceptionHandling.ResumeLabel = Nothing
            _unstructuredExceptionHandling.OnErrorResumeNextCount = 0

            Return nodeFactory.Block(locals.ToImmutableAndFree(), statements.ToImmutableAndFree())
        End Function

        Public Overrides Function VisitOnErrorStatement(node As BoundOnErrorStatement) As BoundNode
            Dim nodeFactory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)

            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance()

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, False, statements)
            End If

            ' Reset the error object
            Dim clearProjectError As MethodSymbol = nodeFactory.WellKnownMember(Of MethodSymbol)(WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__ClearProjectError)

            If clearProjectError IsNot Nothing Then
                statements.Add(New BoundCall(node.Syntax, clearProjectError, Nothing, Nothing, ImmutableArray(Of BoundExpression).Empty, Nothing, clearProjectError.ReturnType).ToStatement)
            End If

            Dim newErrorHandlerIndex As Integer

            Select Case node.OnErrorKind
                Case OnErrorStatementKind.GoToMinusOne
                    ' Undocumented feature. -1 means to reset the handler. So, if
                    ' we're currently in a handler, this instruction resets state
                    ' to normal.
                    statements.Add(nodeFactory.AssignmentExpression(nodeFactory.Local(_unstructuredExceptionHandling.ResumeTargetTemporary, isLValue:=True),
                                                                    nodeFactory.Literal(0)).ToStatement())
                    GoTo Done

                Case OnErrorStatementKind.GoToLabel
                    newErrorHandlerIndex = s_activeHandler_FirstNonReservedOnErrorGotoIndex + _unstructuredExceptionHandling.ExceptionHandlers.Count
                    _unstructuredExceptionHandling.ExceptionHandlers.Add(nodeFactory.Goto(node.LabelOpt, setWasCompilerGenerated:=False))

                Case OnErrorStatementKind.ResumeNext
                    If OptimizationLevelIsDebug Then
                        newErrorHandlerIndex = s_activeHandler_FirstOnErrorResumeNextIndex - _unstructuredExceptionHandling.OnErrorResumeNextCount
                    Else
                        newErrorHandlerIndex = s_activeHandler_ResumeNext
                    End If

                    _unstructuredExceptionHandling.OnErrorResumeNextCount += 1

                Case Else
                    Debug.Assert(node.OnErrorKind = OnErrorStatementKind.GoToZero)
                    newErrorHandlerIndex = s_activeHandler_None
            End Select

            statements.Add(nodeFactory.AssignmentExpression(nodeFactory.Local(_unstructuredExceptionHandling.ActiveHandlerTemporary, isLValue:=True),
                                                            nodeFactory.Literal(newErrorHandlerIndex)).ToStatement())
Done:
            Debug.Assert(Not node.WasCompilerGenerated)
            Dim rewritten As BoundStatement = New BoundStatementList(node.Syntax, statements.ToImmutableAndFree())

            If Instrument(node, rewritten) Then
                rewritten = _instrumenterOpt.InstrumentOnErrorStatement(node, rewritten)
            End If

            Return rewritten
        End Function

        Public Overrides Function VisitResumeStatement(node As BoundResumeStatement) As BoundNode
            Dim nodeFactory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)

            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance()

            Dim generateUnstructuredExceptionHandlingResumeCode As Boolean = ShouldGenerateUnstructuredExceptionHandlingResumeCode(node)
            If generateUnstructuredExceptionHandlingResumeCode Then
                RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, True, statements)
            End If

            ' Reset the error object
            Dim clearProjectError As MethodSymbol = nodeFactory.WellKnownMember(Of MethodSymbol)(WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__ClearProjectError)

            If clearProjectError IsNot Nothing Then
                statements.Add(New BoundCall(node.Syntax, clearProjectError, Nothing, Nothing, ImmutableArray(Of BoundExpression).Empty, Nothing, clearProjectError.ReturnType).ToStatement)
            End If

            ' Emit code to check to see if we're in a handler. Compare resumeTargetTemporary against 0.
            Dim createProjectError As MethodSymbol = nodeFactory.WellKnownMember(Of MethodSymbol)(WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__CreateProjectError)

            If createProjectError IsNot Nothing Then
                Const E_RESUMEWITHOUTERROR As Integer = &H800A0014 ' 20

                statements.Add(RewriteIfStatement(node.Syntax,
                                                  nodeFactory.Binary(BinaryOperatorKind.Equals,
                                                                     nodeFactory.SpecialType(SpecialType.System_Boolean),
                                                                     nodeFactory.Local(_unstructuredExceptionHandling.ResumeTargetTemporary, isLValue:=False),
                                                                     nodeFactory.Literal(0)),
                                                  nodeFactory.Throw(New BoundCall(node.Syntax, createProjectError, Nothing, Nothing,
                                                                                  ImmutableArray.Create(Of BoundExpression)(nodeFactory.Literal(E_RESUMEWITHOUTERROR)),
                                                                                  Nothing, createProjectError.ReturnType)),
                                                  Nothing,
                                                  instrumentationTargetOpt:=Nothing))
            End If

            ' Now generate code based on what kind of Resume we have
            Select Case node.ResumeKind
                Case ResumeStatementKind.Label
                    ' Resume label. Reset resume local
                    statements.Add(nodeFactory.AssignmentExpression(nodeFactory.Local(_unstructuredExceptionHandling.ResumeTargetTemporary, isLValue:=True),
                                                                    nodeFactory.Literal(0)).ToStatement())

                    If generateUnstructuredExceptionHandlingResumeCode Then
                        RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, False, statements)
                    End If

                    statements.Add(nodeFactory.Goto(node.LabelOpt, setWasCompilerGenerated:=False))

                Case ResumeStatementKind.Next
                    statements.Add(nodeFactory.Goto(_unstructuredExceptionHandling.ResumeNextLabel))

                Case Else
                    Debug.Assert(node.ResumeKind = ResumeStatementKind.Plain)
                    statements.Add(nodeFactory.Goto(_unstructuredExceptionHandling.ResumeLabel))
            End Select

            Debug.Assert(Not node.WasCompilerGenerated)
            Dim rewritten As BoundStatement = New BoundStatementList(node.Syntax, statements.ToImmutableAndFree())

            If Instrument(node, rewritten) Then
                rewritten = _instrumenterOpt.InstrumentResumeStatement(node, rewritten)
            End If

            Return rewritten
        End Function

        Private Function AddResumeTargetLabel(syntax As SyntaxNode) As BoundLabelStatement
            Debug.Assert(InsideValidUnstructuredExceptionHandlingResumeContext())
            Dim targetResumeLabel = New GeneratedUnstructuredExceptionHandlingResumeLabel(_unstructuredExceptionHandling.Context.ResumeWithoutLabelOpt)

            _unstructuredExceptionHandling.ResumeTargets.Add(New BoundGotoStatement(syntax, targetResumeLabel, Nothing))
            Return New BoundLabelStatement(syntax, targetResumeLabel)
        End Function

        Private Sub AddResumeTargetLabelAndUpdateCurrentStatementTemporary(syntax As SyntaxNode, canThrow As Boolean, statements As ArrayBuilder(Of BoundStatement))
            Debug.Assert(InsideValidUnstructuredExceptionHandlingResumeContext())
            statements.Add(AddResumeTargetLabel(syntax))

            If canThrow Then
                Dim nodeFactory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, syntax, _compilationState, _diagnostics)
                statements.Add(nodeFactory.AssignmentExpression(nodeFactory.Local(_unstructuredExceptionHandling.CurrentStatementTemporary, isLValue:=True),
                                                                nodeFactory.Literal(_unstructuredExceptionHandling.ResumeTargets.Count)).ToStatement())
            End If
        End Sub

        Private Function ShouldGenerateUnstructuredExceptionHandlingResumeCode(statement As BoundStatement) As Boolean
            If statement.WasCompilerGenerated Then
                Return False
            End If

#If Not DEBUG Then
            If Not InsideValidUnstructuredExceptionHandlingResumeContext() Then
                Return False
            End If
#End If

            If Not (TypeOf statement.Syntax Is StatementSyntax) Then
                If statement.Syntax.Parent IsNot Nothing AndAlso statement.Syntax.Parent.Kind = SyntaxKind.EraseStatement Then
                    Debug.Assert(TypeOf statement.Syntax Is ExpressionSyntax)
                Else
                    Select Case statement.Syntax.Kind
                        Case SyntaxKind.ElseIfBlock
                            If statement.Kind <> BoundKind.IfStatement Then
                                Return False
                            End If

                        Case SyntaxKind.CaseBlock, SyntaxKind.CaseElseBlock
                            If statement.Kind <> BoundKind.CaseBlock Then
                                Return False
                            End If

                        Case SyntaxKind.ModifiedIdentifier
                            If statement.Kind <> BoundKind.LocalDeclaration OrElse
                               statement.Syntax.Parent Is Nothing OrElse
                               statement.Syntax.Parent.Kind <> SyntaxKind.VariableDeclarator OrElse
                               statement.Syntax.Parent.Parent Is Nothing OrElse
                               statement.Syntax.Parent.Parent.Kind <> SyntaxKind.LocalDeclarationStatement Then
                                Return False
                            End If

                        Case SyntaxKind.RedimClause
                            If statement.Kind <> BoundKind.ExpressionStatement OrElse
                               Not (TypeOf statement.Syntax.Parent Is ReDimStatementSyntax) Then
                                Return False
                            End If

                        Case Else
                            Return False
                    End Select
                End If
            End If

            If TypeOf statement.Syntax Is DeclarationStatementSyntax Then
                Return False
            End If

#If DEBUG Then
            If _currentMethodOrLambda Is _topMethod Then
                If Not (TypeOf statement.Syntax Is ExecutableStatementSyntax) Then
                    Select Case statement.Kind
                        Case BoundKind.IfStatement
                            Debug.Assert(statement.Syntax.Kind = SyntaxKind.ElseIfBlock AndAlso
                                         statement.Syntax.Parent IsNot Nothing AndAlso
                                         statement.Syntax.Parent.Kind = SyntaxKind.MultiLineIfBlock AndAlso
                                         _unstructuredExceptionHandlingResumableStatements.ContainsKey(statement.Syntax.Parent))

                        Case BoundKind.CaseBlock
                            Debug.Assert((statement.Syntax.Kind = SyntaxKind.CaseBlock OrElse statement.Syntax.Kind = SyntaxKind.CaseElseBlock) AndAlso
                                         statement.Syntax.Parent IsNot Nothing AndAlso
                                         statement.Syntax.Parent.Kind = SyntaxKind.SelectBlock AndAlso
                                         _unstructuredExceptionHandlingResumableStatements.ContainsKey(statement.Syntax.Parent))

                        Case BoundKind.LocalDeclaration
                            Debug.Assert(statement.Syntax.Kind = SyntaxKind.ModifiedIdentifier AndAlso
                                         statement.Syntax.Parent IsNot Nothing AndAlso
                                         statement.Syntax.Parent.Kind = SyntaxKind.VariableDeclarator AndAlso
                                         statement.Syntax.Parent.Parent IsNot Nothing AndAlso
                                         statement.Syntax.Parent.Parent.Kind = SyntaxKind.LocalDeclarationStatement)

                        Case BoundKind.ExpressionStatement
                            Debug.Assert((statement.Syntax.Kind = SyntaxKind.RedimClause AndAlso
                                                TypeOf statement.Syntax.Parent Is ReDimStatementSyntax) OrElse
                                         (TypeOf statement.Syntax Is ExpressionSyntax AndAlso
                                                TypeOf statement.Syntax.Parent Is EraseStatementSyntax))

                        Case Else
                            Debug.Assert(TypeOf statement.Syntax Is ExecutableStatementSyntax)
                    End Select
                End If

                ' We want to throw if this function has been called for this StatementSyntax earlier.
                ' BoundStatement statement is stored to help with debugging.
                _unstructuredExceptionHandlingResumableStatements.Add(statement.Syntax, statement)
            End If

            Return InsideValidUnstructuredExceptionHandlingResumeContext()
#Else
            Return True
#End If
        End Function

        Private Structure UnstructuredExceptionHandlingContext
            Public Context As BoundUnstructuredExceptionHandlingStatement
        End Structure

        Private Function LeaveUnstructuredExceptionHandlingContext(node As BoundNode) As UnstructuredExceptionHandlingContext
#If DEBUG Then
            _leaveRestoreUnstructuredExceptionHandlingContextTracker.Push(node)
#End If
            Dim result As UnstructuredExceptionHandlingContext
            result.Context = _unstructuredExceptionHandling.Context
            _unstructuredExceptionHandling.Context = Nothing
            Return result
        End Function

        Private Sub RestoreUnstructuredExceptionHandlingContext(node As BoundNode, saved As UnstructuredExceptionHandlingContext)
#If DEBUG Then
            If _leaveRestoreUnstructuredExceptionHandlingContextTracker.Peek Is node Then
                _leaveRestoreUnstructuredExceptionHandlingContextTracker.Pop()
            Else
                Debug.Assert(_leaveRestoreUnstructuredExceptionHandlingContextTracker.Peek Is node)
            End If
#End If
            _unstructuredExceptionHandling.Context = saved.Context
        End Sub

        Private Function InsideValidUnstructuredExceptionHandlingResumeContext() As Boolean
            Return _unstructuredExceptionHandling.Context IsNot Nothing AndAlso
                   _unstructuredExceptionHandling.CurrentStatementTemporary IsNot Nothing AndAlso _currentMethodOrLambda Is _topMethod
        End Function

        Private Function InsideValidUnstructuredExceptionHandlingOnErrorContext() As Boolean
            Return _currentMethodOrLambda Is _topMethod AndAlso _unstructuredExceptionHandling.Context IsNot Nothing AndAlso _unstructuredExceptionHandling.Context.ContainsOnError
        End Function

        Private Sub RegisterUnstructuredExceptionHandlingResumeTarget(syntax As SyntaxNode, canThrow As Boolean, statements As ArrayBuilder(Of BoundStatement))
            AddResumeTargetLabelAndUpdateCurrentStatementTemporary(syntax, canThrow, statements)
        End Sub

        Private Function RegisterUnstructuredExceptionHandlingResumeTarget(syntax As SyntaxNode, node As BoundStatement, canThrow As Boolean) As BoundStatement
            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance()
            AddResumeTargetLabelAndUpdateCurrentStatementTemporary(syntax, canThrow, statements)
            statements.Add(node)
            Return New BoundStatementList(syntax, statements.ToImmutableAndFree())
        End Function

        Private Function RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(syntax As SyntaxNode) As BoundLabelStatement
            Return AddResumeTargetLabel(syntax)
        End Function

        Private Function RegisterUnstructuredExceptionHandlingResumeTarget(syntax As SyntaxNode, canThrow As Boolean) As ImmutableArray(Of BoundStatement)
            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance()
            AddResumeTargetLabelAndUpdateCurrentStatementTemporary(syntax, canThrow, statements)
            Return statements.ToImmutableAndFree()
        End Function

    End Class
End Namespace
