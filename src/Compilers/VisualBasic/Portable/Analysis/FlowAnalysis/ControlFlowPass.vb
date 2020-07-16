' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class ControlFlowPass
        Inherits AbstractFlowPass(Of LocalState)

        Protected _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException As Boolean = False ' By default, just let the original exception to bubble up.

        Friend Sub New(info As FlowAnalysisInfo, suppressConstExpressionsSupport As Boolean)
            MyBase.New(info, suppressConstExpressionsSupport)
        End Sub

        Friend Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo, suppressConstantExpressionsSupport As Boolean)
            MyBase.New(info, region, suppressConstantExpressionsSupport, False)
        End Sub

        Protected Overrides Function ReachableState() As LocalState
            Return New LocalState(True, False)
        End Function

        Protected Overrides Function UnreachableState() As LocalState
            Return New LocalState(False, Me.State.Reported)
        End Function

        Protected Overrides Sub Visit(node As BoundNode, dontLeaveRegion As Boolean)
            ' Expressions must be visited if regions can be on expression boundaries. 
            If Not (TypeOf node Is BoundExpression) Then
                MyBase.Visit(node, dontLeaveRegion)
            End If
        End Sub

        ''' <summary>
        ''' Perform control flow analysis, reporting all necessary diagnostics.  Returns true if the end of
        ''' the body might be reachable..
        ''' </summary>
        ''' <param name = "diagnostics"></param>
        ''' <returns></returns>
        Public Overloads Shared Function Analyze(info As FlowAnalysisInfo, diagnostics As DiagnosticBag, suppressConstantExpressionsSupport As Boolean) As Boolean
            Dim walker = New ControlFlowPass(info, suppressConstantExpressionsSupport)

            If diagnostics IsNot Nothing Then
                walker._convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = True
            End If

            Try
                walker.Analyze()
                If diagnostics IsNot Nothing Then
                    diagnostics.AddRange(walker.diagnostics)
                End If
                Return walker.State.Alive
            Catch ex As CancelledByStackGuardException When diagnostics IsNot Nothing
                ex.AddAnError(diagnostics)
                Return True
            Finally
                walker.Free()
            End Try
        End Function

        Protected Overrides Function ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException() As Boolean
            Return _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException
        End Function

        Protected Overrides Sub VisitStatement(statement As BoundStatement)

            Select Case statement.Kind
                Case BoundKind.LabelStatement, BoundKind.NoOpStatement, BoundKind.Block

                Case Else

                    If Not Me.State.Alive AndAlso Not Me.State.Reported Then

                        Select Case statement.Kind
                            Case BoundKind.LocalDeclaration
                                ' Declarations by themselves are not executable. Only report one as unreachable if it has an initializer.
                                Dim decl = TryCast(statement, BoundLocalDeclaration)
                                If decl.InitializerOpt IsNot Nothing Then
                                    ' TODO: uncomment the following line 
                                    'Me.diagnostics.Add(ERRID.WRN_UnreachableCode, decl.InitializerOpt.Syntax.GetLocation())
                                    Me.State.Reported = True
                                End If

                            Case BoundKind.ReturnStatement
                                ' VB always adds a return at the end of all methods. It may end up being
                                ' marked as unreachable if the code has an explicit return in it. The final return is 
                                ' always reachable because all returns jump to the final synthetic return.
                                Dim returnStmt = TryCast(statement, BoundReturnStatement)
                                If Not returnStmt.IsEndOfMethodReturn Then
                                    ' TODO: uncomment the following line 
                                    'Me.diagnostics.Add(ERRID.WRN_UnreachableCode, statement.Syntax.GetLocation())
                                    Me.State.Reported = True
                                End If

                            Case BoundKind.DimStatement
                                ' Don't report anything, warnings will be reported when 
                                ' declarations inside this Dim statement are processed

                            Case Else
                                ' TODO: uncomment the following line 
                                'Me.diagnostics.Add(ERRID.WRN_UnreachableCode, statement.Syntax.GetLocation())
                                Me.State.Reported = True
                        End Select

                    End If

            End Select
            MyBase.VisitStatement(statement)
        End Sub

        Protected Overrides Sub VisitTryBlock(tryBlock As BoundStatement, node As BoundTryStatement, ByRef tryState As LocalState)
            If node.CatchBlocks.IsEmpty Then
                MyBase.VisitTryBlock(tryBlock, node, tryState)

            Else
                Dim oldPendings As SavedPending = Me.SavePending()
                MyBase.VisitTryBlock(tryBlock, node, tryState)

                ' NOTE: C# generates errors for 'yield return' inside try statement here;
                '       it is valid in VB though.

                Me.RestorePending(oldPendings, mergeLabelsSeen:=True)
            End If
        End Sub

        Protected Overrides Sub VisitCatchBlock(node As BoundCatchBlock, ByRef finallyState As LocalState)
            Dim oldPendings As SavedPending = Me.SavePending()
            MyBase.VisitCatchBlock(node, finallyState)

            For Each branch In Me.PendingBranches
                if branch.Branch.Kind = BoundKind.YieldStatement
                    Me.diagnostics.Add(ERRID.ERR_BadYieldInTryHandler, branch.Branch.Syntax.GetLocation)
                End If
            Next

            ' NOTE: VB generates error ERR_GotoIntoTryHandler in binding, but
            '       we still want to 'nest' pendings' state for catch statements

            Me.RestorePending(oldPendings)
        End Sub

        Protected Overrides Sub VisitFinallyBlock(finallyBlock As BoundStatement, ByRef endState As LocalState)
            Dim oldPending1 As SavedPending = SavePending() ' we do not support branches into a finally block
            Dim oldPending2 As SavedPending = SavePending() ' track only the branches out of the finally block
            MyBase.VisitFinallyBlock(finallyBlock, endState)
            RestorePending(oldPending2) ' resolve branches that remain within the finally block
            For Each branch In Me.PendingBranches

                Dim syntax = branch.Branch.Syntax
                Dim errorLocation As SyntaxNodeOrToken
                Dim errId As ERRID

                If branch.Branch.Kind = BoundKind.YieldStatement
                    errId = errId.ERR_BadYieldInTryHandler
                    errorLocation = syntax

                else
                    errId = errId.ERR_BranchOutOfFinally

                    If syntax.Kind = SyntaxKind.GoToStatement Then
                        errorLocation = DirectCast(syntax, GoToStatementSyntax).Label
                    Else
                        errorLocation = syntax
                    End If

                End If

                Me.diagnostics.Add(errId, errorLocation.GetLocation())
            Next

            RestorePending(oldPending1)
        End Sub

    End Class

End Namespace
