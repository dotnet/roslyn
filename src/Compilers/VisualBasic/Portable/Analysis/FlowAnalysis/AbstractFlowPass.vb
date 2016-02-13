' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports System.Runtime.InteropServices

' NOTE: VB does not support constant expressions in flow analysis during command-line compilation, but supports them when 
'       analysis is being called via public API. This distinction is governed by 'suppressConstantExpressions' flag

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class AbstractFlowPass(Of LocalState As AbstractLocalState)
        Inherits BoundTreeVisitor

        ''' <summary>
        ''' The compilation in which the analysis is taking place.  This is needed to determine which
        ''' conditional methods will be compiled and which will be omitted.
        ''' </summary>
        Protected ReadOnly compilation As VisualBasicCompilation

        ''' <summary>
        ''' The symbol of method whose body is being analyzed or field or property whose 
        ''' initializer is being analyzed
        ''' </summary>
        Public symbol As Symbol

        ''' <summary>
        ''' The bound code of the method or initializer being analyzed
        ''' </summary>
        Private ReadOnly _methodOrInitializerMainNode As BoundNode

        ''' <summary>
        ''' The flow analysis state at each label, computed by merging the state from branches to
        ''' that label with the state when we fall into the label.  Entries are created when the
        ''' label is encountered.  One case deserves special attention: when the destination of the
        ''' branch is a label earlier in the code, it is possible (though rarely occurs in practice)
        ''' that we are changing the state at a label that we've already analyzed. In that case we
        ''' run another pass of the analysis to allow those changes to propagate. This repeats until
        ''' no further changes to the state of these labels occurs.  This can result in quadratic
        ''' performance in unlikely but possible code such as this: "int x; if (cond) goto l1; x =
        ''' 3; l5: print x; l4: goto l5; l3: goto l4; l2: goto l3; l1: goto l2;"
        ''' </summary>
        Private ReadOnly _labels As New Dictionary(Of LabelSymbol, LabelStateAndNesting)

        ''' <summary> All of the labels seen so far in this forward scan of the body </summary>
        Private _labelsSeen As New HashSet(Of LabelSymbol)

        Private _placeholderReplacementMap As Dictionary(Of BoundValuePlaceholderBase, BoundExpression)

        ''' <summary>
        ''' Set to true after an analysis scan if the analysis was incomplete due to a backward
        ''' "goto" branch changing some analysis result.  In this case the caller scans again (until
        ''' this is false). Since the analysis proceeds by monotonically changing the state computed
        ''' at each label, this must terminate.
        ''' </summary>
        Friend backwardBranchChanged As Boolean = False

        ''' <summary> Actual storage for PendingBranches </summary>
        Private _pendingBranches As ArrayBuilder(Of PendingBranch) = ArrayBuilder(Of PendingBranch).GetInstance()

        ''' <summary> The definite assignment and/or reachability state at the point currently being analyzed. </summary>
        Protected State As LocalState
        Protected StateWhenTrue As LocalState
        Protected StateWhenFalse As LocalState
        Protected IsConditionalState As Boolean

        ''' <summary>
        ''' 'Me' parameter, relevant for methods, fields, properties, otherwise Nothing
        ''' </summary>
        Protected ReadOnly MeParameter As ParameterSymbol

        ''' <summary>
        ''' Used only in the data flows out walker, we track unassignments as well as assignments
        ''' </summary>
        Protected ReadOnly TrackUnassignments As Boolean

        ''' <summary>
        ''' The current lexical nesting in the BoundTree. 
        ''' </summary>
        ''' <remarks></remarks>
        Private _nesting As ArrayBuilder(Of Integer)

        ''' <summary>
        ''' Where all diagnostics are deposited.
        ''' </summary>
        Protected ReadOnly diagnostics As DiagnosticBag = DiagnosticBag.GetInstance()

        ''' <summary> Indicates whether or not support of constant expressions (boolean and nothing)
        ''' is enabled in this analyzer. In general, constant expressions support is enabled in analysis
        ''' exposed to public API consumer and disabled when used from command-line compiler. </summary>
        Private ReadOnly _suppressConstantExpressions As Boolean

        Protected _recursionDepth As Integer

        ''' <summary>
        ''' Construct an object for outside-region analysis
        ''' </summary>
        Protected Sub New(_info As FlowAnalysisInfo, suppressConstExpressionsSupport As Boolean)
            MyClass.New(_info, Nothing, suppressConstExpressionsSupport, False)
        End Sub

        ''' <summary>
        ''' Construct an object for region-aware analysis
        ''' </summary>
        Protected Sub New(_info As FlowAnalysisInfo, _region As FlowAnalysisRegionInfo, suppressConstExpressionsSupport As Boolean, trackUnassignments As Boolean)

            Debug.Assert(_info.Symbol.Kind = SymbolKind.Field OrElse
                                  _info.Symbol.Kind = SymbolKind.Property OrElse
                                  _info.Symbol.Kind = SymbolKind.Method OrElse
                                  _info.Symbol.Kind = SymbolKind.Parameter)

            Me.compilation = _info.Compilation
            Me.symbol = _info.Symbol
            Me.MeParameter = Me.symbol.GetMeParameter()
            Me._methodOrInitializerMainNode = _info.Node

            Me._firstInRegion = _region.FirstInRegion
            Me._lastInRegion = _region.LastInRegion
            Me._region = _region.Region

            Me.TrackUnassignments = trackUnassignments
            Me._loopHeadState = If(trackUnassignments, New Dictionary(Of BoundLoopStatement, LocalState)(), Nothing)
            Me._suppressConstantExpressions = suppressConstExpressionsSupport
        End Sub

        Protected Overridable Sub InitForScan()
        End Sub

        Protected MustOverride Function ReachableState() As LocalState

        Protected MustOverride Function UnreachableState() As LocalState

        ''' <summary> Set conditional state </summary>
        Private Sub SetConditionalState(_whenTrue As LocalState, _whenFalse As LocalState)
            Me.State = Nothing
            Me.StateWhenTrue = _whenTrue
            Me.StateWhenFalse = _whenFalse
            Me.IsConditionalState = True
        End Sub

        ''' <summary> Set unconditional state </summary>
        Protected Sub SetState(_state As LocalState)
            Me.State = _state
            If Me.IsConditionalState Then
                Me.StateWhenTrue = Nothing
                Me.StateWhenFalse = Nothing
                Me.IsConditionalState = False
            End If
        End Sub

        ''' <summary> Split state </summary>
        Protected Sub Split()
            If Not Me.IsConditionalState Then
                SetConditionalState(Me.State, Me.State.Clone())
            End If
        End Sub

        ''' <summary> Intersect and unsplit state </summary>
        Protected Sub Unsplit()
            If Me.IsConditionalState Then
                Me.IntersectWith(Me.StateWhenTrue, Me.StateWhenFalse)
                Me.SetState(Me.StateWhenTrue)
            End If
        End Sub

        ''' <summary>
        ''' Pending escapes generated in the current scope (or more deeply nested scopes). When jump
        ''' statements (goto, break, continue, return) are processed, they are placed in the
        ''' Me._pendingBranches buffer to be processed later by the code handling the destination
        ''' statement. As a special case, the processing of try-finally statements might modify the
        ''' contents of the Me._pendingBranches buffer to take into account the behavior of
        ''' "intervening" finally clauses.
        ''' </summary>
        Protected ReadOnly Property PendingBranches As ImmutableArray(Of PendingBranch)
            Get
                Return _pendingBranches.ToImmutable
            End Get
        End Property

        ''' <summary>
        ''' Perform a single pass of flow analysis.  Note that after this pass,
        ''' this.backwardBranchChanged indicates if a further pass is required.
        ''' </summary>
        ''' <returns>False if the region is invalid</returns>
        Protected Overridable Function Scan() As Boolean
            ' Clear diagnostics reported in the previous iteration
            Me.diagnostics.Clear()

            ' initialize
            Me._regionPlace = RegionPlace.Before
            Me.SetState(ReachableState())
            Me.backwardBranchChanged = False

            If Me._nesting IsNot Nothing Then
                Me._nesting.Free()
            End If
            Me._nesting = ArrayBuilder(Of Integer).GetInstance()

            InitForScan()

            ' pending branches should be restored after each iteration
            Dim oldPending As SavedPending = Me.SavePending()
            Visit(Me._methodOrInitializerMainNode)
            Me.RestorePending(oldPending)
            Me._labelsSeen.Clear()

            ' if we are tracking regions, we must have left the region by now;
            ' otherwise the region was erroneous which must have been detected earlier
            Return Me._firstInRegion Is Nothing OrElse Me._regionPlace = RegionPlace.After
        End Function

        ''' <returns>False if the region is invalid</returns>
        Protected Overridable Function Analyze() As Boolean
            Do
                If Not Me.Scan() Then
                    Return False
                End If
            Loop While Me.backwardBranchChanged
            Return True
        End Function

        Protected Overridable Sub Free()
            If Me._nesting IsNot Nothing Then
                Me._nesting.Free()
            End If
            Me.diagnostics.Free()
            Me._pendingBranches.Free()
        End Sub

        ''' <summary>
        ''' If analysis is being performed in a context of a method returns method's parameters, 
        ''' otherwise returns an empty array
        ''' </summary>
        Protected ReadOnly Property MethodParameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return If(Me.symbol.Kind = SymbolKind.Method, DirectCast(Me.symbol, MethodSymbol).Parameters, ImmutableArray(Of ParameterSymbol).Empty)
            End Get
        End Property

        ''' <summary>
        ''' Specifies whether or not method's ByRef parameters should be analyzed. If there's more than one location in
        ''' the method being analyzed, then the method is partial and we prefer to report an out parameter in partial
        ''' method error.
        ''' Note: VB doesn't support "out" so it doesn't warn for unassigned parameters. However, check variables passed
        ''' byref are assigned so that data flow analysis detects parameters flowing out.
        ''' </summary>
        ''' <returns>true if the out parameters of the method should be analyzed</returns>
        Protected ReadOnly Property ShouldAnalyzeByRefParameters As Boolean
            Get
                Return Me.symbol.Kind = SymbolKind.Method AndAlso DirectCast(Me.symbol, MethodSymbol).Locations.Length = 1
            End Get
        End Property

        ''' <summary>
        ''' Method symbol or nothing
        ''' TODO: Need to try and get rid of this property
        ''' </summary>
        Protected ReadOnly Property MethodSymbol As MethodSymbol
            Get
                Return If(Me.symbol.Kind = SymbolKind.Method, DirectCast(Me.symbol, MethodSymbol), Nothing)
            End Get
        End Property

        ''' <summary>
        ''' If analysis is being performed in a context of a method returns method's return type, 
        ''' otherwise returns Nothing
        ''' </summary>
        Protected ReadOnly Property MethodReturnType As TypeSymbol
            Get
                Return If(Me.symbol.Kind = SymbolKind.Method, DirectCast(Me.symbol, MethodSymbol).ReturnType, Nothing)
            End Get
        End Property

        ''' <summary>
        ''' Return the flow analysis state associated with a label.
        ''' </summary>
        ''' <param name="label"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function LabelState(label As LabelSymbol) As LocalState
            Dim result As LabelStateAndNesting = Nothing
            If _labels.TryGetValue(label, result) Then
                Return result.State
            End If
            Return UnreachableState()
        End Function

        ''' <summary>
        ''' Set the current state to one that indicates that it is unreachable.
        ''' </summary>
        Protected Sub SetUnreachable()
            Me.SetState(UnreachableState())
        End Sub

        Private Function IsConstantTrue(node As BoundExpression) As Boolean
            If Me._suppressConstantExpressions Then
                Return False
            End If

            If Not node.IsConstant Then
                Return False
            End If
            Dim constantValue = node.ConstantValueOpt
            If constantValue.Discriminator <> ConstantValueTypeDiscriminator.Boolean Then
                Return False
            End If
            Return constantValue.BooleanValue
        End Function

        Private Function IsConstantFalse(node As BoundExpression) As Boolean
            If Me._suppressConstantExpressions Then
                Return False
            End If

            If Not node.IsConstant Then
                Return False
            End If
            Dim constantValue = node.ConstantValueOpt
            If constantValue.Discriminator <> ConstantValueTypeDiscriminator.Boolean Then
                Return False
            End If
            Return Not constantValue.BooleanValue
        End Function

        Private Function IsConstantNull(node As BoundExpression) As Boolean
            If Me._suppressConstantExpressions Then
                Return False
            End If

            If Not node.IsConstant Then
                Return False
            End If
            Return node.ConstantValueOpt.IsNull
        End Function

        Protected Shared Function IsNonPrimitiveValueType(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            If Not type.IsValueType Then
                Return False
            End If
            Select Case type.SpecialType
                Case SpecialType.None,
                     SpecialType.System_Nullable_T,
                     SpecialType.System_IntPtr,
                     SpecialType.System_UIntPtr
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Called at the point in a loop where the backwards branch would go to.
        ''' </summary>
        ''' <param name="node"></param>
        ''' <remarks></remarks>
        Private Sub LoopHead(node As BoundLoopStatement)
            If Me.TrackUnassignments Then
                Dim previousState As LocalState
                If Me._loopHeadState.TryGetValue(node, previousState) Then
                    IntersectWith(Me.State, previousState)
                End If
                Me._loopHeadState(node) = Me.State.Clone()
            End If
        End Sub

        ''' <summary>
        ''' Called at the point in a loop where the backward branch is placed.
        ''' </summary>
        ''' <param name="node"></param>
        ''' <remarks></remarks>
        Private Sub LoopTail(node As BoundLoopStatement)
            If Me.TrackUnassignments Then
                Dim oldState = Me._loopHeadState(node)
                If IntersectWith(oldState, Me.State) Then
                    Me._loopHeadState(node) = oldState
                    Me.backwardBranchChanged = True
                End If
            End If
        End Sub

        ''' <summary>
        ''' Used to resolve exit statements in each statement form that has an Exit statement
        ''' (loops, switch).
        ''' </summary>
        Private Sub ResolveBreaks(breakState As LocalState, breakLabel As LabelSymbol)
            Dim newPendingBranches = ArrayBuilder(Of PendingBranch).GetInstance()
            For Each pending In Me.PendingBranches
                Select Case pending.Branch.Kind
                    Case BoundKind.ExitStatement
                        Dim exitStmt = TryCast(pending.Branch, BoundExitStatement)

                        If exitStmt.Label = breakLabel Then
                            IntersectWith(breakState, pending.State)
                        Else
                            ' If it doesn't match then it is for an outer block
                            newPendingBranches.Add(pending)
                        End If

                    Case Else
                        newPendingBranches.Add(pending)
                End Select
            Next
            ResetPendingBranches(newPendingBranches)
            Me.SetState(breakState)
        End Sub

        ''' <summary>
        ''' Used to resolve continue statements in each statement form that supports it.
        ''' </summary>
        ''' <param name = "continueLabel"></param>
        Private Sub ResolveContinues(continueLabel As LabelSymbol)
            Dim newPendingBranches = ArrayBuilder(Of PendingBranch).GetInstance()
            For Each pending In Me.PendingBranches
                Select Case pending.Branch.Kind
                    Case BoundKind.ContinueStatement

                        ' When the continue XXX does not match an enclosing XXX block then no label exists
                        Dim continueStmt = TryCast(pending.Branch, BoundContinueStatement)

                        ' Technically, nothing in the language specification depends on the state
                        ' at the continue label, so we could just discard them instead of merging
                        ' the states. In fact, we need not have added continue statements to the
                        ' pending jump queue in the first place if we were interested solely in the
                        ' flow analysis.  However, region analysis (in support of extract method)
                        ' depends on continue statements appearing in the pending branch queue, so
                        ' we process them from the queue here.
                        If continueStmt.Label = continueLabel Then
                            IntersectWith(Me.State, pending.State)
                        Else
                            ' If it doesn't match then it is for an outer block
                            newPendingBranches.Add(pending)
                        End If

                    Case Else
                        newPendingBranches.Add(pending)
                End Select
            Next
            ResetPendingBranches(newPendingBranches)
        End Sub

        ''' <summary>
        ''' Subclasses override this if they want to take special actions on processing a goto
        ''' statement, when both the jump and the label have been located.
        ''' </summary>
        Protected Overridable Sub NoteBranch(pending As PendingBranch, stmt As BoundStatement, labelStmt As BoundLabelStatement)
        End Sub

        Private Function GetBranchTargetLabel(branch As BoundStatement, gotoOnly As Boolean) As LabelSymbol
            Select Case branch.Kind
                Case BoundKind.GotoStatement
                    Return DirectCast(branch, BoundGotoStatement).Label
                Case BoundKind.ConditionalGoto
                    Return DirectCast(branch, BoundConditionalGoto).Label
                Case BoundKind.ExitStatement
                    Return If(gotoOnly, Nothing, DirectCast(branch, BoundExitStatement).Label)
                Case BoundKind.ReturnStatement
                    Return If(gotoOnly, Nothing, DirectCast(branch, BoundReturnStatement).ExitLabelOpt)
                Case BoundKind.ContinueStatement
                    Return Nothing
                Case BoundKind.YieldStatement
                    Return Nothing
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(branch.Kind)
            End Select
            Return Nothing
        End Function

        Protected Overridable Sub ResolveBranch(pending As PendingBranch, label As LabelSymbol, target As BoundLabelStatement, ByRef labelStateChanged As Boolean)
            Dim _state = LabelState(target.Label)
            NoteBranch(pending, pending.Branch, target)
            Dim changed = IntersectWith(_state, pending.State)
            If changed Then
                labelStateChanged = True
                Me._labels(target.Label) = New LabelStateAndNesting(target, _state, Me._nesting)
            End If
        End Sub

        ''' <summary>
        ''' To handle a label, we resolve all pending forward references to branches to that label.  Returns true if the state of
        ''' the label changes as a result. 
        ''' </summary>
        ''' <param name = "target"></param>
        ''' <returns></returns>
        Private Function ResolveBranches(target As BoundLabelStatement) As Boolean
            Dim labelStateChanged As Boolean = False

            If Me.PendingBranches.Length > 0 Then
                Dim newPendingBranches = ArrayBuilder(Of PendingBranch).GetInstance()

                For Each pending In Me.PendingBranches
                    Dim label As LabelSymbol = GetBranchTargetLabel(pending.Branch, False)
                    If label IsNot Nothing AndAlso label = target.Label Then
                        ResolveBranch(pending, label, target, labelStateChanged)
                    Else
                        newPendingBranches.Add(pending)
                    End If

                Next

                ResetPendingBranches(newPendingBranches)
            End If

            Return labelStateChanged
        End Function

        Protected Class SavedPending
            Public ReadOnly PendingBranches As ArrayBuilder(Of PendingBranch)
            Public ReadOnly LabelsSeen As HashSet(Of LabelSymbol)

            Public Sub New(ByRef _pendingBranches As ArrayBuilder(Of PendingBranch), ByRef _labelsSeen As HashSet(Of LabelSymbol))
                Me.PendingBranches = _pendingBranches
                Me.LabelsSeen = _labelsSeen

                _pendingBranches = ArrayBuilder(Of PendingBranch).GetInstance()
                _labelsSeen = New HashSet(Of LabelSymbol)
            End Sub
        End Class


        ''' <summary>
        ''' When branching into constructs that don't support jumps into/out of (i.e. lambdas), 
        ''' we save the pending branches when visiting more nested constructs.
        ''' </summary>
        Protected Function SavePending() As SavedPending
            Return New SavedPending(Me._pendingBranches, Me._labelsSeen)
        End Function

        Private Sub ResetPendingBranches(newPendingBranches As ArrayBuilder(Of PendingBranch))
            Debug.Assert(newPendingBranches IsNot Nothing)
            Debug.Assert(newPendingBranches IsNot Me._pendingBranches)
            Me._pendingBranches.Free()
            Me._pendingBranches = newPendingBranches
        End Sub

        ''' <summary>
        ''' We use this to restore the old set of pending branches and labels after visiting a construct that contains nested statements.
        ''' </summary>
        ''' <param name="oldPending">The old pending branches/labels, which are to be merged with the current ones</param>
        Protected Sub RestorePending(oldPending As SavedPending, Optional mergeLabelsSeen As Boolean = False)
            If ResolveBranches(Me._labelsSeen) Then
                Me.backwardBranchChanged = True
            End If

            oldPending.PendingBranches.AddRange(Me.PendingBranches)
            ResetPendingBranches(oldPending.PendingBranches)

            ' We only use SavePending/RestorePending when there could be no branch into the region between them.
            ' So there is no need to save the labels seen between the calls.  If there were such a need, we would
            ' do "this.labelsSeen.UnionWith(oldPending.LabelsSeen);" instead of the following assignment
            If mergeLabelsSeen Then
                Me._labelsSeen.AddAll(oldPending.LabelsSeen)
            Else
                Me._labelsSeen = oldPending.LabelsSeen
            End If
        End Sub

        ''' <summary>
        ''' We look at all pending branches and attempt to resolve the branches with labels if the nesting of the 
        ''' block is the nearest common parent to the branch and the label. Because the code is evaluated recursively 
        ''' outward we only need to check if the current nesting is a prefix of both the branch and the label nesting.
        ''' </summary>
        Private Function ResolveBranches(labelsFilter As HashSet(Of LabelSymbol)) As Boolean
            Dim labelStateChanged As Boolean = False

            If Me.PendingBranches.Length > 0 Then
                Dim newPendingBranches = ArrayBuilder(Of PendingBranch).GetInstance()

                For Each pending In Me.PendingBranches
                    Dim labelSymbol As LabelSymbol = Nothing
                    Dim labelAndNesting As LabelStateAndNesting = Nothing
                    If BothBranchAndLabelArePrefixedByNesting(pending, labelsFilter, labelSymbol:=labelSymbol, labelAndNesting:=labelAndNesting) Then
                        Dim changed As Boolean
                        ResolveBranch(pending, labelSymbol, labelAndNesting.Target, changed)
                        If changed Then
                            labelStateChanged = True
                        End If
                        Continue For
                    End If
                    newPendingBranches.Add(pending)
                Next

                ResetPendingBranches(newPendingBranches)
            End If

            Return labelStateChanged
        End Function

        Private Function BothBranchAndLabelArePrefixedByNesting(branch As PendingBranch,
                                                                Optional labelsFilter As HashSet(Of LabelSymbol) = Nothing,
                                                                Optional ignoreLast As Boolean = False,
                                                                <Out()> Optional ByRef labelSymbol As LabelSymbol = Nothing,
                                                                <Out()> Optional ByRef labelAndNesting As LabelStateAndNesting = Nothing) As Boolean

            Dim branchStatement As BoundStatement = branch.Branch
            If branchStatement IsNot Nothing AndAlso branch.Nesting.IsPrefixedBy(Me._nesting, ignoreLast) Then
                labelSymbol = GetBranchTargetLabel(branchStatement, gotoOnly:=True)
                If labelSymbol IsNot Nothing AndAlso (labelsFilter Is Nothing OrElse labelsFilter.Contains(labelSymbol)) Then
                    Return Me._labels.TryGetValue(labelSymbol, labelAndNesting) AndAlso
                           labelAndNesting.Nesting.IsPrefixedBy(Me._nesting, ignoreLast)
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' Report an unimplemented language construct.
        ''' </summary>
        ''' <param name = "node"></param>
        ''' <param name = "feature"></param>
        ''' <returns></returns>
        Protected Overridable Function Unimplemented(node As BoundNode, feature As [String]) As BoundNode
            Return Nothing
        End Function

        Protected Overridable Function AllBitsSet() As LocalState
            Return Nothing
        End Function

        Protected Sub SetPlaceholderSubstitute(placeholder As BoundValuePlaceholderBase, newSubstitute As BoundExpression)
            Debug.Assert(placeholder IsNot Nothing)

            If _placeholderReplacementMap Is Nothing Then
                _placeholderReplacementMap = New Dictionary(Of BoundValuePlaceholderBase, BoundExpression)()
            End If

            Debug.Assert(Not _placeholderReplacementMap.ContainsKey(placeholder))
            _placeholderReplacementMap(placeholder) = newSubstitute
        End Sub

        Protected Sub RemovePlaceholderSubstitute(placeholder As BoundValuePlaceholderBase)
            Debug.Assert(placeholder IsNot Nothing)
            Debug.Assert(_placeholderReplacementMap.ContainsKey(placeholder))
            Me._placeholderReplacementMap.Remove(placeholder)
        End Sub

        Protected ReadOnly Property GetPlaceholderSubstitute(placeholder As BoundValuePlaceholderBase) As BoundExpression
            Get
                Dim value As BoundExpression = Nothing
                If Me._placeholderReplacementMap IsNot Nothing AndAlso Me._placeholderReplacementMap.TryGetValue(placeholder, value) Then
                    Return value
                End If
                Return Nothing
            End Get
        End Property

        Protected MustOverride Function Dump(state As LocalState) As String

#Region "Visitors"

        Public NotOverridable Overrides Function Visit(node As BoundNode) As BoundNode
            Visit(node, dontLeaveRegion:=False)
            Return Nothing
        End Function

        ''' <summary>
        ''' Visit a node.
        ''' </summary>
        Protected Overridable Overloads Sub Visit(node As BoundNode, dontLeaveRegion As Boolean)
            VisitAlways(node, dontLeaveRegion:=dontLeaveRegion)
        End Sub

        ''' <summary>
        ''' Visit a node, process 
        ''' </summary>
        Protected Sub VisitAlways(node As BoundNode, Optional dontLeaveRegion As Boolean = False)
            If Me._firstInRegion Is Nothing Then
                VisitWithStackGuard(node)
            Else

                If node Is Me._firstInRegion AndAlso Me._regionPlace = RegionPlace.Before Then
                    Me.EnterRegion()
                End If

                VisitWithStackGuard(node)

                If Not dontLeaveRegion AndAlso node Is Me._lastInRegion AndAlso IsInside Then
                    Me.LeaveRegion()
                End If

            End If
        End Sub

        Private Shadows Function VisitWithStackGuard(node As BoundNode) As BoundNode
            Dim expression = TryCast(node, BoundExpression)

            If expression IsNot Nothing Then
                Return VisitExpressionWithStackGuard(_recursionDepth, expression)
            End If

            Return MyBase.Visit(node)
        End Function

        Protected Overrides Function VisitExpressionWithoutStackGuard(node As BoundExpression) As BoundExpression
            Return DirectCast(MyBase.Visit(node), BoundExpression)
        End Function

        Protected Overrides Function ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException() As Boolean
            Return False ' just let the original exception to bubble up.
        End Function

        Protected Overridable Sub VisitLvalue(node As BoundExpression, Optional dontLeaveRegion As Boolean = False)
            ' NOTE: we can skip checking if Me._firstInRegion is nothing because 'node' is not nothing
            If node Is Me._firstInRegion AndAlso Me._regionPlace = RegionPlace.Before Then
                Me.EnterRegion()
            End If

            Select Case node.Kind
                Case BoundKind.Local
                    Dim local = DirectCast(node, BoundLocal)
                    If local.LocalSymbol.IsByRef Then
                        VisitRvalue(local)
                    End If

                Case BoundKind.Parameter, BoundKind.MeReference, BoundKind.MyClassReference, BoundKind.MyBaseReference
                    ' no need for it to be previously assigned: it is on the left.

                Case BoundKind.FieldAccess
                    VisitFieldAccessInternal(DirectCast(node, BoundFieldAccess))

                Case BoundKind.WithLValueExpressionPlaceholder,
                        BoundKind.WithRValueExpressionPlaceholder,
                        BoundKind.LValuePlaceholder,
                        BoundKind.RValuePlaceholder
                    ' TODO: Other placeholder kinds?

                    Dim substitute As BoundExpression = GetPlaceholderSubstitute(DirectCast(node, BoundValuePlaceholderBase))
                    If substitute IsNot Nothing Then
                        VisitLvalue(substitute)
                    Else
                        VisitRvalue(node, dontLeaveRegion:=True)
                    End If

                Case Else
                    VisitRvalue(node, dontLeaveRegion:=True)

            End Select

            ' NOTE: we can skip checking if Me._firstInRegion is nothing because 'node' is not nothing
            If Not dontLeaveRegion AndAlso node Is Me._lastInRegion AndAlso IsInside Then
                Me.LeaveRegion()
            End If
        End Sub

        ''' <summary>
        ''' Visit a boolean condition expression.
        ''' </summary>
        Protected Sub VisitCondition(node As BoundExpression)
            ' NOTE: we can skip checking if Me._firstInRegion is nothing because 'node' is not nothing
            If node Is Me._firstInRegion AndAlso Me._regionPlace = RegionPlace.Before Then
                Me.EnterRegion()
            End If

            Visit(node, dontLeaveRegion:=True)

            AdjustConditionalState(node)

            ' NOTE: we can skip checking if Me._firstInRegion is nothing because 'node' is not nothing
            If node Is Me._lastInRegion AndAlso IsInside Then
                Me.LeaveRegion()
            End If
        End Sub

        Private Sub AdjustConditionalState(node As BoundExpression)
            If IsConstantTrue(node) Then
                Me.Unsplit()
                Me.SetConditionalState(Me.State, UnreachableState())
            ElseIf IsConstantFalse(node) Then
                Me.Unsplit()
                Me.SetConditionalState(UnreachableState(), Me.State)
            Else
                Me.Split()
            End If
        End Sub

        ''' <summary>
        ''' Visit a general expression, where we will only need to determine if variables are
        ''' assigned (or not). That is, we will not be needing AssignedWhenTrue and
        ''' AssignedWhenFalse.
        ''' </summary>
        Protected Sub VisitRvalue(node As BoundExpression, Optional rwContext As ReadWriteContext = ReadWriteContext.None, Optional dontLeaveRegion As Boolean = False)
            ' NOTE: we can skip checking if Me._firstInRegion is nothing because 'node' is not nothing
            If node Is Me._firstInRegion AndAlso Me._regionPlace = RegionPlace.Before Then
                Me.EnterRegion()
            End If

            If rwContext <> ReadWriteContext.None Then
                Select Case node.Kind

                    Case BoundKind.Local
                        VisitLocalInReadWriteContext(DirectCast(node, BoundLocal), rwContext)
                        GoTo lUnsplitAndFinish

                    Case BoundKind.FieldAccess
                        VisitFieldAccessInReadWriteContext(DirectCast(node, BoundFieldAccess), rwContext)
                        GoTo lUnsplitAndFinish

                End Select
            End If

            Visit(node, dontLeaveRegion:=True)

lUnsplitAndFinish:
            Me.Unsplit()

            ' NOTE: we can skip checking if Me._firstInRegion is nothing because 'node' is not nothing
            If Not dontLeaveRegion AndAlso node Is Me._lastInRegion AndAlso IsInside Then
                Me.LeaveRegion()
            End If
        End Sub

        Public Overrides Function VisitSequence(node As BoundSequence) As BoundNode
            Dim sideEffects = node.SideEffects
            If Not sideEffects.IsEmpty Then
                For Each sideEffect In node.SideEffects
                    VisitExpressionAsStatement(sideEffect)
                Next
            End If
            Debug.Assert(node.ValueOpt IsNot Nothing OrElse node.HasErrors OrElse node.Type.SpecialType = SpecialType.System_Void)
            If node.ValueOpt IsNot Nothing Then
                VisitRvalue(node.ValueOpt)
            End If
            Return Nothing
        End Function

        Public Overrides Function VisitByRefArgumentPlaceholder(node As BoundByRefArgumentPlaceholder) As BoundNode
            Dim replacement As BoundExpression = GetPlaceholderSubstitute(node)

            If replacement IsNot Nothing Then
                VisitRvalue(replacement, ReadWriteContext.ByRefArgument)
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitByRefArgumentWithCopyBack(node As BoundByRefArgumentWithCopyBack) As BoundNode
            Me.SetPlaceholderSubstitute(node.InPlaceholder, node.OriginalArgument)
            VisitRvalue(node.InConversion)
            Me.RemovePlaceholderSubstitute(node.InPlaceholder)
            Return Nothing
        End Function

        Protected Overridable Sub VisitStatement(statement As BoundStatement)
            Visit(statement)
        End Sub

        Public Overrides Function VisitLValueToRValueWrapper(node As BoundLValueToRValueWrapper) As BoundNode
            Visit(node.UnderlyingLValue)
            Return Nothing
        End Function

        Public Overrides Function VisitWithLValueExpressionPlaceholder(node As BoundWithLValueExpressionPlaceholder) As BoundNode
            Visit(GetPlaceholderSubstitute(node))
            Return Nothing
        End Function

        Public Overrides Function VisitWithStatement(node As BoundWithStatement) As BoundNode

            ' In case we perform _region_ flow analysis processing of the region inside With 
            ' statement expression is a little subtle. The original expression may be 
            ' rewritten into a placeholder substitution expression and a set of initializers.
            ' The placeholder will be processed when we visit left-omitted part of member or 
            ' dictionary access in the statements inside the With statement body, but the 
            ' initializers are to be processed with BoundWithStatement itself.

            ' True if it is a _region_ flow analysis and the region is defined inside the original expression
            Dim origExpressionContainsRegion As Boolean = False
            ' True if origExpressionContainsRegion is True and region encloses all the initializers
            Dim regionEnclosesInitializers As Boolean = False

            If Me._firstInRegion IsNot Nothing AndAlso Me._regionPlace = RegionPlace.Before Then
                Debug.Assert(Me._lastInRegion IsNot Nothing)

                ' Check if the region defining node is somewhere inside OriginalExpression
                If BoundNodeFinder.ContainsNode(node.OriginalExpression, Me._firstInRegion, _recursionDepth, ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()) Then
                    Debug.Assert(BoundNodeFinder.ContainsNode(node.OriginalExpression, Me._lastInRegion, _recursionDepth, ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()))

                    origExpressionContainsRegion = True

                    ' Does any of initializers contain the node or the node contains initializers?
                    For Each initializer In node.DraftInitializers
                        Debug.Assert(initializer.Kind = BoundKind.AssignmentOperator)
                        Dim initializerExpr As BoundExpression = DirectCast(initializer, BoundAssignmentOperator).Right

                        If initializerExpr.Kind = BoundKind.LValueToRValueWrapper Then
                            initializerExpr = DirectCast(initializerExpr, BoundLValueToRValueWrapper).UnderlyingLValue
                        End If

                        regionEnclosesInitializers = True
                        If Me._firstInRegion Is initializerExpr OrElse Not BoundNodeFinder.ContainsNode(Me._firstInRegion, initializerExpr, _recursionDepth, ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()) Then
                            regionEnclosesInitializers = False
                            Exit For
                        End If
                    Next
                End If
            End If

            ' NOTE: If origExpressionContainsRegion is True and regionEnclosesInitializers is *False*,
            '       we should expect that the region is to be properly entered/left while appropriate
            '       initializer is visited *OR* the node is a literal/local which was not captured, 
            '       but simply reused when needed in With statement body
            '
            '       The assumption above is guarded by the following assert
#If DEBUG Then
            If origExpressionContainsRegion AndAlso Not regionEnclosesInitializers Then
                Dim containedByInitializer As Boolean = False
                For Each initializer In node.DraftInitializers
                    Debug.Assert(initializer.Kind = BoundKind.AssignmentOperator)
                    Dim initializerExpr As BoundExpression = DirectCast(initializer, BoundAssignmentOperator).Right

                    If initializerExpr.Kind = BoundKind.LValueToRValueWrapper Then
                        initializerExpr = DirectCast(initializerExpr, BoundLValueToRValueWrapper).UnderlyingLValue
                    End If

                    containedByInitializer = False
                    Debug.Assert(initializerExpr IsNot Nothing)
                    If BoundNodeFinder.ContainsNode(initializerExpr, Me._firstInRegion, _recursionDepth, ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()) Then
                        Debug.Assert(Not containedByInitializer)
                        containedByInitializer = True
                        Exit For
                    End If
                Next

                Debug.Assert(containedByInitializer OrElse IsNotCapturedExpression(Me._firstInRegion))
            End If
#End If

            If origExpressionContainsRegion AndAlso regionEnclosesInitializers Then
                Me.EnterRegion()
            End If

            ' Visit initializers
            For Each initializer In node.DraftInitializers
                VisitRvalue(initializer)
            Next

            If origExpressionContainsRegion Then
                If regionEnclosesInitializers Then
                    Me.LeaveRegion() ' This call also asserts that we are inside the region

                Else
                    ' If any of the initializers contained region, we must have exited the region by now or the node is a literal 
                    ' which was not captured in initializers, but just reused across when/if needed in With statement body
#If DEBUG Then
                    Debug.Assert(Me._regionPlace = RegionPlace.After OrElse IsNotCapturedExpression(Me._firstInRegion))
#End If
                End If

            End If

            ' Visit body 
            VisitBlock(node.Body)

            If origExpressionContainsRegion AndAlso Me._regionPlace <> RegionPlace.After Then
                ' The region was a part of the expression, but was not property processed/visited during
                ' analysis of expression and body; this *may* indicate a bug in flow analysis, otherwise 
                ' it is the case when a struct-typed lvalue expression was never used inside the body AND 
                ' the region was defined by nodes which were not part of initializers; thus, we never
                ' emitted/visited the node
                Debug.Assert(Me._regionPlace = AbstractFlowPass(Of LocalState).RegionPlace.Before)
                Me.SetInvalidRegion()
            End If

            Return Nothing
        End Function

#If DEBUG Then
        Private Shared Function IsNotCapturedExpression(node As BoundNode) As Boolean
            If node Is Nothing Then
                Return True
            End If

            Select Case node.Kind
                Case BoundKind.Local
                    Return DirectCast(node, BoundLocal).Type.IsValueType

                Case BoundKind.Parameter
                    Return DirectCast(node, BoundParameter).Type.IsValueType

                Case BoundKind.Literal
                    Return True

                Case BoundKind.MeReference
                    Return True

                Case BoundKind.FieldAccess
                    Return IsNotCapturedExpression(DirectCast(node, BoundFieldAccess).ReceiverOpt)

                Case Else
                    Return False
            End Select
        End Function
#End If

        Public Overrides Function VisitAnonymousTypeCreationExpression(node As BoundAnonymousTypeCreationExpression) As BoundNode
            For Each argument In node.Arguments
                VisitRvalue(argument)
            Next
            Return Nothing
        End Function

        Public Overrides Function VisitAnonymousTypeFieldInitializer(node As BoundAnonymousTypeFieldInitializer) As BoundNode
            VisitRvalue(node.Value)
            Return Nothing
        End Function

        ''' <summary>
        ''' Since each language construct must be handled according to the rules of the language specification,
        ''' the default visitor reports that the construct for the node is not implemented in the compiler.
        ''' </summary>
        Public Overrides Function DefaultVisit(node As BoundNode) As BoundNode
            Return Unimplemented(node, "flow analysis")
        End Function

        Protected Enum ReadWriteContext
            None
            CompoundAssignmentTarget
            ByRefArgument
        End Enum

        Protected Overridable Sub VisitLocalInReadWriteContext(node As BoundLocal, rwContext As ReadWriteContext)
        End Sub

        Protected Overridable Sub VisitFieldAccessInReadWriteContext(node As BoundFieldAccess, rwContext As ReadWriteContext)
            VisitFieldAccessInternal(node)
        End Sub

        Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
            ' Control-flow analysis does NOT dive into a lambda, while data-flow analysis does.
            Return Nothing
        End Function

        Public Overrides Function VisitQueryExpression(node As BoundQueryExpression) As BoundNode
            ' Control-flow analysis does NOT dive into a query expression, while data-flow analysis does.
            Return Nothing
        End Function

        Public Overrides Function VisitLocal(node As BoundLocal) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitLocalDeclaration(node As BoundLocalDeclaration) As BoundNode
            If node.InitializerOpt IsNot Nothing Then
                VisitRvalue(node.InitializerOpt) ' analyze the expression
            End If
            Return Nothing
        End Function

        Private Function IntroduceBlock() As Integer
            Dim level = Me._nesting.Count
            Me._nesting.Add(0)
            Return level
        End Function

        Private Sub FinalizeBlock(level As Integer)
            Me._nesting.RemoveAt(level)
        End Sub

        Private Sub InitializeBlockStatement(level As Integer, ByRef index As Integer)
            Me._nesting(level) = index
            index += 1
        End Sub

        Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode
            Dim level = IntroduceBlock()

            Dim i As Integer = 0
            For Each statement In node.Statements
                InitializeBlockStatement(level, i)
                VisitStatement(statement)
            Next

            FinalizeBlock(level)

            Return Nothing
        End Function

        Public Overrides Function VisitExpressionStatement(node As BoundExpressionStatement) As BoundNode
            VisitExpressionAsStatement(node.Expression)
            Return Nothing
        End Function

        Private Sub VisitExpressionAsStatement(node As BoundExpression)
            VisitRvalue(node)
        End Sub

        Public Overrides Function VisitLateMemberAccess(node As BoundLateMemberAccess) As BoundNode
            ' receiver of a latebound access is never modified
            VisitRvalue(node.ReceiverOpt)
            Return Nothing
        End Function

        Public Overrides Function VisitLateInvocation(node As BoundLateInvocation) As BoundNode
            Dim member = node.Member

            Me.Visit(node.Member)

            Dim arguments = node.ArgumentsOpt
            If Not arguments.IsEmpty Then
                Dim isByRef As Boolean

                If member.Kind <> BoundKind.LateMemberAccess Then

                    ' this is a Set, Get or Indexing.
                    isByRef = False
                Else
                    ' otherwise assume it is ByRef
                    isByRef = True
                End If

                VisitLateBoundArguments(arguments, isByRef)
            End If

            Return Nothing
        End Function

        Private Sub VisitLateBoundArguments(arguments As ImmutableArray(Of BoundExpression), isByRef As Boolean)
            For Each argument In arguments
                VisitLateBoundArgument(argument, isByRef)
            Next
            If isByRef Then
                For Each argument In arguments
                    WriteArgument(argument, False)
                Next
            End If
        End Sub

        Protected Overridable Sub VisitLateBoundArgument(arg As BoundExpression, isByRef As Boolean)
            If isByRef Then
                VisitLvalue(arg)
            Else
                VisitRvalue(arg)
            End If
        End Sub

        Public Overrides Function VisitCall(node As BoundCall) As BoundNode
            ' If the method being called is a partial method without a definition, or is a conditional method
            ' whose condition is not true at the given syntax location, then the call has no effect and it is ignored for the purposes of
            ' definite assignment analysis.  
            Dim callsAreOmitted As Boolean = node.Method.CallsAreOmitted(node.Syntax, node.SyntaxTree)
            Dim savedState As LocalState = Nothing
            If callsAreOmitted Then
                Debug.Assert(Not Me.IsConditionalState)
                savedState = Me.State.Clone()
                Me.SetUnreachable()
            End If

            Dim methodGroup As BoundMethodGroup = node.MethodGroupOpt
            Dim receiverOpt As BoundExpression = node.ReceiverOpt
            Dim method As MethodSymbol = node.Method

            ' if method group is present, check if we need to enter region
            If methodGroup IsNot Nothing AndAlso Me._firstInRegion Is methodGroup AndAlso
                    Me._regionPlace = RegionPlace.Before Then
                Me.EnterRegion()
            End If

            If receiverOpt IsNot Nothing Then
                VisitCallReceiver(receiverOpt, method)
            Else
                ' If receiver is nothing it means that the method being
                ' accessed is shared; we still want to visit the original receiver
                ' to handle shared methods called on instance receiver.
                Dim originalReceiver As BoundExpression = If(methodGroup IsNot Nothing, methodGroup.ReceiverOpt, Nothing)
                If originalReceiver IsNot Nothing AndAlso Not originalReceiver.WasCompilerGenerated Then
                    Debug.Assert(method.IsShared)

                    ' Do not visit originalReceiver if it is Type/Namespace/TypeOrValueExpression and the method 
                    ' is shared, we want region flow analysis to return Succeeded = False in this case
                    Dim kind As BoundKind = originalReceiver.Kind
                    If (kind <> BoundKind.TypeExpression) AndAlso
                        (kind <> BoundKind.NamespaceExpression) AndAlso
                        (kind <> BoundKind.TypeOrValueExpression) Then

                        VisitUnreachableReceiver(originalReceiver)
                    End If
                End If

            End If

            ' if method group is present, check if we need to leave region
            If methodGroup IsNot Nothing AndAlso Me._lastInRegion Is methodGroup AndAlso IsInside Then
                Me.LeaveRegion()
            End If

            VisitArguments(node.Arguments, method.Parameters)

            If receiverOpt IsNot Nothing AndAlso receiverOpt.IsLValue Then
                WriteLValueCallReceiver(receiverOpt, method)
            End If

            If callsAreOmitted Then
                Me.SetState(savedState)
            End If

            Return Nothing
        End Function

        Private Sub VisitCallReceiver(receiver As BoundExpression, method As MethodSymbol)
            Debug.Assert(receiver IsNot Nothing)
            Debug.Assert(method IsNot Nothing)

            If Not method.IsReducedExtensionMethod OrElse Not receiver.IsValue() Then
                Debug.Assert(method.Kind <> MethodKind.Constructor)
                VisitRvalue(receiver)
            Else
                VisitArgument(receiver, method.CallsiteReducedFromMethod.Parameters(0))
            End If
        End Sub

        Private Sub WriteLValueCallReceiver(receiver As BoundExpression, method As MethodSymbol)
            Debug.Assert(receiver IsNot Nothing)
            Debug.Assert(receiver.IsLValue)
            Debug.Assert(method IsNot Nothing)

            If receiver.Type.IsReferenceType Then
                ' If a receiver is of reference type, it will not be modified if the 
                ' method is not an extension method, note that extension method may 
                ' write to ByRef parameter

                Dim reducedFrom As MethodSymbol = method.CallsiteReducedFromMethod
                If reducedFrom Is Nothing OrElse reducedFrom.ParameterCount = 0 OrElse Not reducedFrom.Parameters(0).IsByRef Then
                    Return
                End If
            End If

            WriteArgument(receiver, False)
        End Sub

        Private Sub VisitArguments(arguments As ImmutableArray(Of BoundExpression), parameters As ImmutableArray(Of ParameterSymbol))
            If parameters.IsDefault Then
                For Each arg In arguments
                    VisitRvalue(arg)
                Next
            Else
                Dim n As Integer = Math.Min(parameters.Length, arguments.Length)
                ' The first loop reflects passing arguments to the method/property
                For i = 0 To n - 1
                    VisitArgument(arguments(i), parameters(i))
                Next
                ' The second loop reflects writing to ByRef arguments after the method returned
                For i = 0 To n - 1
                    If parameters(i).IsByRef Then
                        WriteArgument(arguments(i), parameters(i).IsOut)
                    End If
                Next
            End If
        End Sub

        Protected Overridable Sub WriteArgument(arg As BoundExpression, isOut As Boolean)
        End Sub

        Protected Overridable Sub VisitArgument(arg As BoundExpression, p As ParameterSymbol)
            If p.IsByRef Then
                VisitLvalue(arg)
            Else
                VisitRvalue(arg)
            End If
        End Sub

        Public Overrides Function VisitDelegateCreationExpression(node As BoundDelegateCreationExpression) As BoundNode
            Dim methodGroup As BoundMethodGroup = node.MethodGroupOpt

            ' if method group is present, check if we need to enter region
            If methodGroup IsNot Nothing AndAlso Me._firstInRegion Is methodGroup AndAlso
                    Me._regionPlace = AbstractFlowPass(Of LocalState).RegionPlace.Before Then
                Me.EnterRegion()
            End If

            Dim receiverOpt As BoundExpression = node.ReceiverOpt
            Dim method As MethodSymbol = node.Method

            If receiverOpt IsNot Nothing Then
                If Not method.IsReducedExtensionMethod OrElse Not receiverOpt.IsValue() Then
                    Debug.Assert(method.Kind <> MethodKind.Constructor)
                    VisitRvalue(receiverOpt)
                Else
                    VisitArgument(receiverOpt, method.CallsiteReducedFromMethod.Parameters(0))
                End If

            Else
                ' If receiver is nothing it means that the method being
                ' accessed is shared; we still want to visit the original receiver
                ' to handle shared methods called on instance receiver.
                Dim originalReceiver As BoundExpression = If(methodGroup IsNot Nothing, methodGroup.ReceiverOpt, Nothing)
                If originalReceiver IsNot Nothing AndAlso Not originalReceiver.WasCompilerGenerated Then
                    Debug.Assert(method.IsShared)

                    ' Do not visit originalReceiver if it is Type/Namespace/TypeOrValueExpression and the method 
                    ' is shared, we want region flow analysis to return Succeeded = False in this case
                    Dim kind As BoundKind = originalReceiver.Kind
                    If (kind <> BoundKind.TypeExpression) AndAlso
                        (kind <> BoundKind.NamespaceExpression) AndAlso
                        (kind <> BoundKind.TypeOrValueExpression) Then

                        VisitUnreachableReceiver(originalReceiver)
                    End If
                End If

            End If

            ' if method group is present, check if we need to leave region
            If methodGroup IsNot Nothing AndAlso Me._lastInRegion Is methodGroup AndAlso IsInside Then
                Me.LeaveRegion()
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitBadExpression(node As BoundBadExpression) As BoundNode
            ' Visit the child nodes in bad expressions so that uses of locals in bad expressions are recorded. This
            ' suppresses warnings about unused locals. 

            For Each child In node.ChildBoundNodes
                VisitRvalue(TryCast(child, BoundExpression))
            Next

            Return Nothing
        End Function

        Public Overrides Function VisitBadStatement(node As BoundBadStatement) As BoundNode
            ' Visit the child nodes of the bad statement
            For Each child In node.ChildBoundNodes
                Dim statement = TryCast(child, BoundStatement)
                If statement IsNot Nothing Then
                    VisitStatement(TryCast(child, BoundStatement))
                Else
                    Dim expression = TryCast(child, BoundExpression)

                    If expression IsNot Nothing Then
                        VisitExpressionAsStatement(expression)
                    Else
                        Visit(child)
                    End If
                End If
            Next
            Return Nothing
        End Function

        Public Overrides Function VisitBadVariable(node As BoundBadVariable) As BoundNode
            If node.IsLValue Then
                VisitLvalue(node.Expression)
            Else
                VisitRvalue(node.Expression)
            End If
            Return Nothing
        End Function

        Public Overrides Function VisitTypeExpression(node As BoundTypeExpression) As BoundNode
            ' Visit the receiver even though the expression value
            ' is ignored since the region may be within the receiver.
            VisitUnreachableReceiver(node.UnevaluatedReceiverOpt)
            Return Nothing
        End Function

        Public Overrides Function VisitLiteral(node As BoundLiteral) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitConversion(node As BoundConversion) As BoundNode
            VisitRvalue(node.Operand)
            Return Nothing
        End Function

        Public Overrides Function VisitUserDefinedConversion(node As BoundUserDefinedConversion) As BoundNode
            VisitRvalue(node.UnderlyingExpression)
            Return Nothing
        End Function

        Public Overrides Function VisitIfStatement(node As BoundIfStatement) As BoundNode
            VisitCondition(node.Condition)
            Dim trueState As LocalState = Me.StateWhenTrue
            Dim falseState As LocalState = Me.StateWhenFalse
            Me.SetState(trueState)
            VisitStatement(node.Consequence)
            trueState = Me.State
            Me.SetState(falseState)
            If node.AlternativeOpt IsNot Nothing Then
                VisitStatement(node.AlternativeOpt)
            End If
            Me.IntersectWith(Me.State, trueState)
            Return Nothing
        End Function

        Public Overrides Function VisitTernaryConditionalExpression(node As BoundTernaryConditionalExpression) As BoundNode
            VisitCondition(node.Condition)

            Dim trueState As LocalState = Me.StateWhenTrue
            Dim falseState As LocalState = Me.StateWhenFalse

            If IsConstantTrue(node.Condition) Then
                Me.SetState(falseState)
                VisitRvalue(node.WhenFalse)
                Me.SetState(trueState)
                VisitRvalue(node.WhenTrue)

            ElseIf IsConstantFalse(node.Condition) Then
                Me.SetState(trueState)
                VisitRvalue(node.WhenTrue)
                Me.SetState(falseState)
                VisitRvalue(node.WhenFalse)

            Else
                Me.SetState(trueState)
                VisitRvalue(node.WhenTrue)
                Me.Unsplit()
                trueState = Me.State
                Me.SetState(falseState)
                VisitRvalue(node.WhenFalse)
                Me.Unsplit()
                Me.IntersectWith(Me.State, trueState)
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitBinaryConditionalExpression(node As BoundBinaryConditionalExpression) As BoundNode
            VisitRvalue(node.TestExpression)
            If node.TestExpression.IsConstant AndAlso node.TestExpression.ConstantValueOpt.IsNothing Then
                '  this may be something like 'If(CType(Nothing, String), AnyExpression)'
                VisitRvalue(node.ElseExpression)
            Else
                '  all other cases including 'If("const", AnyExpression)'
                Dim savedState As LocalState = Me.State.Clone()
                VisitRvalue(node.ElseExpression)
                Me.SetState(savedState)
            End If
            Return Nothing
        End Function

        Public Overrides Function VisitConditionalAccess(node As BoundConditionalAccess) As BoundNode
            VisitRvalue(node.Receiver)

            If node.Receiver.IsConstant Then
                If node.Receiver.ConstantValueOpt.IsNothing Then
                    Dim savedState As LocalState = Me.State.Clone()
                    SetUnreachable()
                    VisitRvalue(node.AccessExpression)
                    Me.SetState(savedState)
                Else
                    VisitRvalue(node.AccessExpression)
                End If
            Else
                Dim savedState As LocalState = Me.State.Clone()
                VisitRvalue(node.AccessExpression)
                IntersectWith(Me.State, savedState)
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitLoweredConditionalAccess(node As BoundLoweredConditionalAccess) As BoundNode
            VisitRvalue(node.ReceiverOrCondition)
            Dim savedState As LocalState = Me.State.Clone()

            VisitRvalue(node.WhenNotNull)
            IntersectWith(Me.State, savedState)

            If node.WhenNullOpt IsNot Nothing Then
                savedState = Me.State.Clone()
                VisitRvalue(node.WhenNullOpt)
                IntersectWith(Me.State, savedState)
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitComplexConditionalAccessReceiver(node As BoundComplexConditionalAccessReceiver) As BoundNode
            Dim savedState As LocalState = Me.State.Clone()

            VisitLvalue(node.ValueTypeReceiver)
            IntersectWith(Me.State, savedState)

            savedState = Me.State.Clone()
            VisitRvalue(node.ReferenceTypeReceiver)
            IntersectWith(Me.State, savedState)

            Return Nothing
        End Function

        Public Overrides Function VisitConditionalAccessReceiverPlaceholder(node As BoundConditionalAccessReceiverPlaceholder) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitReturnStatement(node As BoundReturnStatement) As BoundNode
            ' Set unreachable and pending branch for all returns except for the final return that is auto generated
            If Not node.IsEndOfMethodReturn Then
                VisitRvalue(node.ExpressionOpt)
                Me._pendingBranches.Add(New PendingBranch(node, Me.State, Me._nesting))
                SetUnreachable()
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitYieldStatement(node As BoundYieldStatement) As BoundNode
            VisitRvalue(node.Expression)
            Me._pendingBranches.Add(New PendingBranch(node, Me.State, Me._nesting))

            Return Nothing
        End Function

        Public Overrides Function VisitStopStatement(node As BoundStopStatement) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitEndStatement(node As BoundEndStatement) As BoundNode
            SetUnreachable()
            Return Nothing
        End Function

        Public Overrides Function VisitMeReference(node As BoundMeReference) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitParameter(node As BoundParameter) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitObjectCreationExpression(node As BoundObjectCreationExpression) As BoundNode
            For Each e In node.Arguments
                VisitRvalue(e)
            Next

            VisitObjectCreationExpressionInitializer(node.InitializerOpt)

            Return Nothing
        End Function

        Public Overrides Function VisitNoPiaObjectCreationExpression(node As BoundNoPiaObjectCreationExpression) As BoundNode
            VisitObjectCreationExpressionInitializer(node.InitializerOpt)

            Return Nothing
        End Function

        Protected Overridable Sub VisitObjectCreationExpressionInitializer(node As BoundObjectInitializerExpressionBase)
            Visit(node)
        End Sub

        Private Function VisitObjectInitializerExpressionBase(node As BoundObjectInitializerExpressionBase) As BoundNode
            For Each initializer In node.Initializers
                VisitExpressionAsStatement(initializer)
            Next

            Return Nothing
        End Function

        Public Overrides Function VisitCollectionInitializerExpression(node As BoundCollectionInitializerExpression) As BoundNode
            Return Me.VisitObjectInitializerExpressionBase(node)
        End Function

        Public Overrides Function VisitObjectInitializerExpression(node As BoundObjectInitializerExpression) As BoundNode
            Return Me.VisitObjectInitializerExpressionBase(node)
        End Function

        Public Overrides Function VisitNewT(node As BoundNewT) As BoundNode
            Visit(node.InitializerOpt)

            Return Nothing
        End Function

        Public Overrides Function VisitRedimStatement(node As BoundRedimStatement) As BoundNode
            For Each clause In node.Clauses
                Visit(clause)
            Next
            Return Nothing
        End Function

        Public Overrides Function VisitEraseStatement(node As BoundEraseStatement) As BoundNode
            For Each clause In node.Clauses
                Visit(clause)
            Next
            Return Nothing
        End Function

        Protected Overridable ReadOnly Property SuppressRedimOperandRvalueOnPreserve As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function VisitRedimClause(node As BoundRedimClause) As BoundNode
            If node.Preserve AndAlso Not SuppressRedimOperandRvalueOnPreserve Then
                VisitRvalue(node.Operand)
            End If

            VisitLvalue(node.Operand)
            For Each index In node.Indices
                VisitRvalue(index)
            Next
            Return Nothing
        End Function

        Public Overrides Function VisitAssignmentOperator(node As BoundAssignmentOperator) As BoundNode
            If node.LeftOnTheRightOpt Is Nothing Then
                VisitLvalue(node.Left)
            Else
                SetPlaceholderSubstitute(node.LeftOnTheRightOpt, node.Left)
            End If

            VisitRvalue(node.Right)

            If node.LeftOnTheRightOpt IsNot Nothing Then
                RemovePlaceholderSubstitute(node.LeftOnTheRightOpt)
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitMidResult(node As BoundMidResult) As BoundNode
            VisitRvalue(node.Original)
            VisitRvalue(node.Start)

            If node.LengthOpt IsNot Nothing Then
                VisitRvalue(node.LengthOpt)
            End If

            VisitRvalue(node.Source)

            Return Nothing
        End Function

        Public Overrides Function VisitReferenceAssignment(node As BoundReferenceAssignment) As BoundNode
            VisitRvalue(node.ByRefLocal)
            VisitRvalue(node.LValue)
            Return Nothing
        End Function

        Public Overrides Function VisitCompoundAssignmentTargetPlaceholder(node As BoundCompoundAssignmentTargetPlaceholder) As BoundNode
            Dim replacement As BoundExpression = GetPlaceholderSubstitute(node)

            If replacement IsNot Nothing Then
                VisitRvalue(replacement, ReadWriteContext.CompoundAssignmentTarget)
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitFieldAccess(node As BoundFieldAccess) As BoundNode
            VisitFieldAccessInternal(node)
            Return Nothing
        End Function

        Private Function VisitFieldAccessInternal(node As BoundFieldAccess) As BoundNode
            Dim receiverOpt = node.ReceiverOpt
            If FieldAccessMayRequireTracking(node) Then
                VisitLvalue(receiverOpt)
            ElseIf node.FieldSymbol.IsShared Then
                VisitUnreachableReceiver(receiverOpt)
            Else
                VisitRvalue(receiverOpt)
            End If
            Return Nothing
        End Function

        ''' <summary> Bound field access passed may require tracking if it is an access to a non-shared structure field </summary>
        Protected Function FieldAccessMayRequireTracking(fieldAccess As BoundFieldAccess) As Boolean
            If fieldAccess.FieldSymbol.IsShared Then
                Return False
            End If

            Dim receiver = fieldAccess.ReceiverOpt

            ' Receiver must exist
            If receiver Is Nothing Then
                Return False
            End If

            ' Receiver is of non-primitive structure type 
            Dim receiverType = receiver.Type
            Return IsNonPrimitiveValueType(receiverType)
        End Function

        Public Overrides Function VisitPropertyAccess(node As BoundPropertyAccess) As BoundNode

            Dim propertyGroup As BoundPropertyGroup = node.PropertyGroupOpt

            ' if property group is present, check if we need to enter region
            If propertyGroup IsNot Nothing AndAlso Me._firstInRegion Is propertyGroup AndAlso
                    Me._regionPlace = RegionPlace.Before Then
                Me.EnterRegion()
            End If

            If node.ReceiverOpt IsNot Nothing Then
                VisitRvalue(node.ReceiverOpt)

            Else
                ' If receiver is nothing it means that the property being
                ' accessed is shared; we still want to visit the original receiver
                ' to handle shared properties accessed on instance receiver.
                Dim originalReceiver As BoundExpression = If(propertyGroup IsNot Nothing, propertyGroup.ReceiverOpt, Nothing)
                If originalReceiver IsNot Nothing AndAlso Not originalReceiver.WasCompilerGenerated Then
                    Debug.Assert(node.PropertySymbol.IsShared)

                    ' Do not visit originalReceiver if it is Type/Namespace/TypeOrValueExpression and the property 
                    ' is shared, we want region flow analysis to return Succeeded = False in this case
                    Dim kind As BoundKind = originalReceiver.Kind
                    If (kind <> BoundKind.TypeExpression) AndAlso
                        (kind <> BoundKind.NamespaceExpression) AndAlso
                        (kind <> BoundKind.TypeOrValueExpression) Then

                        VisitUnreachableReceiver(originalReceiver)
                    End If
                End If
            End If

            ' if property group is present, check if we need to leave region
            If propertyGroup IsNot Nothing AndAlso Me._lastInRegion Is propertyGroup AndAlso IsInside Then
                Me.LeaveRegion()
            End If

            For Each argument In node.Arguments
                VisitRvalue(argument)
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' If a receiver is included in cases where the receiver will not be
        ''' evaluated (an instance for a shared method for instance), we
        ''' still want to visit the receiver but treat it as unreachable code.
        ''' </summary>
        Private Sub VisitUnreachableReceiver(receiver As BoundExpression)
            Debug.Assert(Not Me.IsConditionalState)

            If receiver IsNot Nothing Then
                Dim saved As LocalState = Me.State.Clone()
                Me.SetUnreachable()
                Me.VisitRvalue(receiver)
                Me.SetState(saved)
            End If
        End Sub

        Public Overrides Function VisitAsNewLocalDeclarations(node As BoundAsNewLocalDeclarations) As BoundNode
            VisitRvalue(node.Initializer)
            For Each v In node.LocalDeclarations
                Visit(v)
            Next
            Return Nothing
        End Function

        Public Overrides Function VisitDimStatement(node As BoundDimStatement) As BoundNode
            For Each v In node.LocalDeclarations
                VisitStatement(v)
            Next
            Return Nothing
        End Function

        Public Overrides Function VisitWhileStatement(node As BoundWhileStatement) As BoundNode
            ' while node.Condition  node.Body node.ContinueLabel:  node.BreakLabel:
            LoopHead(node)
            VisitCondition(node.Condition)
            Dim bodyState As LocalState = Me.StateWhenTrue
            Dim breakState As LocalState = Me.StateWhenFalse
            Me.SetState(bodyState)
            VisitStatement(node.Body)
            ResolveContinues(node.ContinueLabel)
            LoopTail(node)
            ResolveBreaks(breakState, node.ExitLabel)
            Return Nothing
        End Function

        Public Overrides Function VisitSelectStatement(node As BoundSelectStatement) As BoundNode
            VisitStatement(node.ExpressionStatement)

            Dim caseBlocks = node.CaseBlocks
            If caseBlocks.Any() Then
                VisitCaseBlocks(caseBlocks)
                ResolveBreaks(Me.State, node.ExitLabel)
            End If

            Return Nothing
        End Function

        Private Sub VisitCaseBlocks(caseBlocks As ImmutableArray(Of BoundCaseBlock))
            Debug.Assert(caseBlocks.Any())
            Dim caseBlockStateBuilder = ArrayBuilder(Of LocalState).GetInstance(caseBlocks.Length)

            Dim hasCaseElse = False
            Dim curIndex As Integer = 0
            Dim lastIndex As Integer = caseBlocks.Length - 1

            For Each caseBlock In caseBlocks
                ' Visit case statement
                VisitStatement(caseBlock.CaseStatement)

                ' Case statement might have a non-null conditionOpt for the condition expression.
                ' However, conditionOpt cannot be a compile time constant expression.
                ' VisitCaseStatement must have unsplit the states into a non-conditional state for this scenario.
                Debug.Assert(Not Me.IsConditionalState)

                ' save the current state for next case block.
                Dim savedState As LocalState = Me.State.Clone()

                ' Visit case block body
                VisitStatement(caseBlock.Body)

                hasCaseElse = hasCaseElse OrElse caseBlock.Syntax.Kind = SyntaxKind.CaseElseBlock

                ' If the select statement had a case else block, then the state at the end
                ' of select statement is the merge of states at the end of all case blocks.
                ' Otherwise, the end state is merge of states at the end of all case blocks
                ' and saved state prior to visiting the last case block body (i.e. no matching case state)

                If curIndex <> lastIndex OrElse Not hasCaseElse Then
                    caseBlockStateBuilder.Add(Me.State.Clone())
                    Me.SetState(savedState)
                End If

                curIndex = curIndex + 1
            Next

            For Each localState In caseBlockStateBuilder
                Me.IntersectWith(Me.State, localState)
            Next

            caseBlockStateBuilder.Free()
        End Sub

        Public Overrides Function VisitCaseBlock(node As BoundCaseBlock) As BoundNode
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides Function VisitCaseStatement(node As BoundCaseStatement) As BoundNode
            If node.ConditionOpt IsNot Nothing Then
                ' Case clause expressions cannot be constant expression.
                Debug.Assert(node.ConditionOpt.ConstantValueOpt Is Nothing)
                VisitRvalue(node.ConditionOpt)
            Else
                For Each clause In node.CaseClauses
                    Select Case clause.Kind
                        Case BoundKind.RelationalCaseClause
                            VisitRelationalCaseClause(DirectCast(clause, BoundRelationalCaseClause))
                        Case BoundKind.SimpleCaseClause
                            VisitSimpleCaseClause(DirectCast(clause, BoundSimpleCaseClause))
                        Case BoundKind.RangeCaseClause
                            VisitRangeCaseClause(DirectCast(clause, BoundRangeCaseClause))
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(clause.Kind)
                    End Select
                Next
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitRelationalCaseClause(node As BoundRelationalCaseClause) As BoundNode
            ' Exactly one of the operand or condition must be non-null
            Debug.Assert(node.OperandOpt IsNot Nothing Xor node.ConditionOpt IsNot Nothing)

            If node.OperandOpt IsNot Nothing Then
                VisitRvalue(node.OperandOpt)
            Else
                VisitRvalue(node.ConditionOpt)
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitSimpleCaseClause(node As BoundSimpleCaseClause) As BoundNode
            ' Exactly one of the value or condition must be non-null
            Debug.Assert(node.ValueOpt IsNot Nothing Xor node.ConditionOpt IsNot Nothing)

            If node.ValueOpt IsNot Nothing Then
                VisitRvalue(node.ValueOpt)
            Else
                VisitRvalue(node.ConditionOpt)
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitRangeCaseClause(node As BoundRangeCaseClause) As BoundNode
            ' Exactly one of the LowerBoundOpt or LowerBoundConditionOpt must be non-null
            Debug.Assert(node.LowerBoundOpt IsNot Nothing Xor node.LowerBoundConditionOpt IsNot Nothing)

            If node.LowerBoundOpt IsNot Nothing Then
                VisitRvalue(node.LowerBoundOpt)
            Else
                VisitRvalue(node.LowerBoundConditionOpt)
            End If

            ' Exactly one of the UpperBoundOpt or UpperBoundConditionOpt must be non-null
            Debug.Assert(node.UpperBoundOpt IsNot Nothing Xor node.UpperBoundConditionOpt IsNot Nothing)

            If node.UpperBoundOpt IsNot Nothing Then
                VisitRvalue(node.UpperBoundOpt)
            Else
                VisitRvalue(node.UpperBoundConditionOpt)
            End If

            Return Nothing
        End Function

        Protected Overridable Sub VisitForControlInitialization(node As BoundForToStatement)
            VisitLvalue(node.ControlVariable)
        End Sub

        Protected Overridable Sub VisitForControlInitialization(node As BoundForEachStatement)
            VisitLvalue(node.ControlVariable)
        End Sub

        Protected Overridable Sub VisitForInitValues(node As BoundForToStatement)
            VisitRvalue(node.InitialValue)
            VisitRvalue(node.LimitValue)
            VisitRvalue(node.StepValue)
        End Sub

        Protected Overridable Sub VisitForStatementVariableDeclaration(node As BoundForStatement)
        End Sub

        Public Overrides Function VisitForEachStatement(node As BoundForEachStatement) As BoundNode

            ' This is a bit subtle.
            ' initialization of For each control variable happens after the collection is evaluated.
            ' For example the following statement should give a warning:
            '          For each i As Object in Me.GetSomeCollection(i)        ' <- use of uninitialized i

            VisitForStatementVariableDeclaration(node)

            VisitRvalue(node.Collection)

            LoopHead(node)
            Me.Split()
            Dim bodyState As LocalState = Me.StateWhenTrue
            Dim breakState As LocalState = Me.StateWhenFalse

            Me.SetState(bodyState)

            ' The control Variable is only considered initialized if the body was entered.
            VisitForControlInitialization(node)

            VisitStatement(node.Body)

            ResolveContinues(node.ContinueLabel)
            LoopTail(node)
            ResolveBreaks(breakState, node.ExitLabel)

            Return Nothing
        End Function

        Public Overrides Function VisitUsingStatement(node As BoundUsingStatement) As BoundNode
            Debug.Assert(Not node.ResourceList.IsDefault OrElse node.ResourceExpressionOpt IsNot Nothing)

            ' A using statement can come in two flavors: using <expression> and using <variable declarations>

            ' visit possible resource expression
            If node.ResourceExpressionOpt IsNot Nothing Then
                VisitRvalue(node.ResourceExpressionOpt)
            Else
                ' visit all declarations
                For Each variableDeclaration In node.ResourceList
                    Visit(variableDeclaration)
                Next
            End If

            VisitStatement(node.Body)

            Return Nothing
        End Function

        Public Overrides Function VisitForToStatement(node As BoundForToStatement) As BoundNode

            ' This is a bit subtle.
            ' initialization of For control variable happens after initial value, limit and step are evaluated.
            ' For example the following statement should give a warning:
            '          For i As Object = 0 To i        ' <- use of uninitialized i

            VisitForStatementVariableDeclaration(node)

            VisitForInitValues(node)

            VisitForControlInitialization(node)

            LoopHead(node)
            Me.Split()
            Dim bodyState As LocalState = Me.StateWhenTrue
            Dim breakState As LocalState = Me.StateWhenFalse

            Me.SetState(bodyState)
            VisitStatement(node.Body)

            ResolveContinues(node.ContinueLabel)
            LoopTail(node)
            ResolveBreaks(breakState, node.ExitLabel)

            Return Nothing
        End Function

        Public Overrides Function VisitTryStatement(node As BoundTryStatement) As BoundNode
            Dim oldPending = SavePending()

            Dim level = IntroduceBlock()
            Dim i As Integer = 0

            Dim initialState = Me.State.Clone()
            InitializeBlockStatement(level, i)
            VisitTryBlock(node.TryBlock, node, initialState)
            Dim finallyState = initialState.Clone()
            Dim endState = Me.State

            For Each catchBlock In node.CatchBlocks
                Me.SetState(initialState.Clone())
                InitializeBlockStatement(level, i)
                VisitCatchBlock(catchBlock, finallyState)
                IntersectWith(endState, Me.State)
            Next

            If node.FinallyBlockOpt IsNot Nothing Then
                Dim tryAndCatchPending As SavedPending = SavePending()

                Me.SetState(finallyState)
                Dim unsetInFinally = AllBitsSet()
                InitializeBlockStatement(level, i)
                VisitFinallyBlock(node.FinallyBlockOpt, unsetInFinally)
                For Each pend In tryAndCatchPending.PendingBranches
                    ' Do not union if branch is a Yield statement
                    Dim unionBranchWithFinallyState As Boolean = pend.Branch.Kind <> BoundKind.YieldStatement

                    ' or if the branch goes from Catch to Try block
                    If unionBranchWithFinallyState Then
                        If BothBranchAndLabelArePrefixedByNesting(pend, ignoreLast:=True) Then
                            unionBranchWithFinallyState = False
                        End If
                    End If

                    If unionBranchWithFinallyState Then
                        Me.UnionWith(pend.State, Me.State)
                        If Me.TrackUnassignments Then
                            Me.IntersectWith(pend.State, unsetInFinally)
                        End If
                    End If
                Next

                RestorePending(tryAndCatchPending)
                Me.UnionWith(endState, Me.State)
                If Me.TrackUnassignments Then
                    Me.IntersectWith(endState, unsetInFinally)
                End If
            End If

            Me.SetState(endState)

            FinalizeBlock(level)

            If node.ExitLabelOpt IsNot Nothing Then
                ResolveBreaks(endState, node.ExitLabelOpt)
            End If

            RestorePending(oldPending)
            Return Nothing
        End Function

        Protected Overridable Sub VisitTryBlock(tryBlock As BoundStatement, node As BoundTryStatement, ByRef tryState As LocalState)
            VisitStatement(tryBlock)
        End Sub

        Protected Overridable Overloads Sub VisitCatchBlock(catchBlock As BoundCatchBlock, ByRef finallyState As LocalState)
            If catchBlock.ExceptionSourceOpt IsNot Nothing Then
                VisitLvalue(catchBlock.ExceptionSourceOpt)
            End If

            If catchBlock.ErrorLineNumberOpt IsNot Nothing Then
                VisitRvalue(catchBlock.ErrorLineNumberOpt)
            End If

            If catchBlock.ExceptionFilterOpt IsNot Nothing Then
                VisitRvalue(catchBlock.ExceptionFilterOpt)
            End If

            VisitBlock(catchBlock.Body)
        End Sub

        Protected Overridable Sub VisitFinallyBlock(finallyBlock As BoundStatement, ByRef unsetInFinally As LocalState)
            VisitStatement(finallyBlock)
        End Sub

        Public Overrides Function VisitArrayAccess(node As BoundArrayAccess) As BoundNode
            VisitRvalue(node.Expression)
            For Each i In node.Indices
                VisitRvalue(i)
            Next
            Return Nothing
        End Function

        Public NotOverridable Overrides Function VisitBinaryOperator(node As BoundBinaryOperator) As BoundNode
            ' Do not blow the stack due to a deep recursion on the left. 

            Dim stack = ArrayBuilder(Of BoundBinaryOperator).GetInstance()

            Dim binary As BoundBinaryOperator = node
            Dim child As BoundExpression = node.Left

            Do
                If child.Kind <> BoundKind.BinaryOperator Then
                    Exit Do
                End If

                stack.Push(binary)
                binary = DirectCast(child, BoundBinaryOperator)
                child = binary.Left
            Loop

            ' As we emulate visiting, enter the region if needed. 
            ' We shouldn't do this for the top most node, 
            ' VisitBinaryOperator caller such as VisitRvalue(...) does this.
            If Me._regionPlace = RegionPlace.Before Then
                If stack.Count > 0 Then
                    ' Skipping the first node
                    Debug.Assert(stack(0) Is node)

                    For i As Integer = 1 To stack.Count - 1
                        If stack(i) Is Me._firstInRegion Then
                            Me.EnterRegion()
                            GoTo EnteredRegion
                        End If
                    Next

                    ' Note, the last binary operator is not pushed to the stack, it is stored in [binary].
                    Debug.Assert(binary IsNot node)

                    If binary Is Me._firstInRegion Then
                        Me.EnterRegion()
                    End If
EnteredRegion:
                Else
                    Debug.Assert(binary Is node)
                End If
            End If

            Select Case binary.OperatorKind And BinaryOperatorKind.OpMask
                Case BinaryOperatorKind.AndAlso,
                     BinaryOperatorKind.OrElse
                    VisitCondition(child)
                Case Else
                    VisitRvalue(child)
            End Select

            Do
                Select Case binary.OperatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.AndAlso
                        Dim leftTrue As LocalState = Me.StateWhenTrue
                        Dim leftFalse As LocalState = Me.StateWhenFalse
                        Me.SetState(leftTrue)
                        VisitCondition(binary.Right)
                        Dim resultTrue As LocalState = Me.StateWhenTrue
                        Dim resultFalse As LocalState = leftFalse
                        IntersectWith(resultFalse, Me.StateWhenFalse)
                        Me.SetConditionalState(resultTrue, resultFalse)
                        Exit Select
                    Case BinaryOperatorKind.OrElse
                        Dim leftTrue As LocalState = Me.StateWhenTrue
                        Dim leftFalse As LocalState = Me.StateWhenFalse
                        Me.SetState(leftFalse)
                        VisitCondition(binary.Right)
                        Dim resultTrue As LocalState = Me.StateWhenTrue
                        IntersectWith(resultTrue, leftTrue)
                        Dim resultFalse As LocalState = Me.StateWhenFalse
                        Me.SetConditionalState(resultTrue, resultFalse)
                        Exit Select
                    Case Else
                        VisitRvalue(binary.Right)
                End Select

                If stack.Count > 0 Then
                    child = binary
                    binary = stack.Pop()

                    ' Do things that VisitCondition/VisitRvalue would have done for us if we were to call them
                    ' for the child
                    Select Case binary.OperatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.AndAlso,
                             BinaryOperatorKind.OrElse
                            ' Do things that VisitCondition would have done for us if we were to call it
                            ' for the child
                            AdjustConditionalState(child) ' VisitCondition does this
                        Case Else
                            Me.Unsplit() ' VisitRvalue does this
                    End Select

                    ' VisitCondition/VisitRvalue do this
                    If child Is Me._lastInRegion AndAlso IsInside Then
                        Me.LeaveRegion()
                    End If
                Else
                    Exit Do
                End If
            Loop

            Debug.Assert(binary Is node)
            stack.Free()

            Return Nothing
        End Function

        Public Overrides Function VisitUserDefinedBinaryOperator(node As BoundUserDefinedBinaryOperator) As BoundNode
            VisitRvalue(node.UnderlyingExpression)
            Return Nothing
        End Function

        Public Overrides Function VisitUserDefinedShortCircuitingOperator(node As BoundUserDefinedShortCircuitingOperator) As BoundNode
            If node.LeftOperand IsNot Nothing Then
                VisitRvalue(node.LeftOperand)
            End If

            VisitRvalue(node.BitwiseOperator)
            Return Nothing
        End Function

        Public Overrides Function VisitAddHandlerStatement(node As BoundAddHandlerStatement) As BoundNode
            Return VisitAddRemoveHandlerStatement(node)
        End Function

        Public Overrides Function VisitRemoveHandlerStatement(node As BoundRemoveHandlerStatement) As BoundNode
            Return VisitAddRemoveHandlerStatement(node)
        End Function

        Public Overrides Function VisitAddressOfOperator(node As BoundAddressOfOperator) As BoundNode
            Visit(node.MethodGroup)
            Return Nothing
        End Function

        Public Overrides Function VisitLateAddressOfOperator(node As BoundLateAddressOfOperator) As BoundNode
            Visit(node.MemberAccess)
            Return Nothing
        End Function

        Private Function VisitAddRemoveHandlerStatement(node As BoundAddRemoveHandlerStatement) As BoundNode
            ' from the data/control flow prospective AddRemoveHandler
            ' statement is just a trivial binary operator.
            VisitRvalue(node.EventAccess)
            VisitRvalue(node.Handler)
            Return Nothing
        End Function

        Public Overrides Function VisitEventAccess(node As BoundEventAccess) As BoundNode
            Dim receiver = node.ReceiverOpt
            If receiver IsNot Nothing Then
                VisitRvalue(node.ReceiverOpt)
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitRaiseEventStatement(node As BoundRaiseEventStatement) As BoundNode
            Me.VisitExpressionAsStatement(node.EventInvocation)
            Return Nothing
        End Function

        Public Overrides Function VisitParenthesized(node As BoundParenthesized) As BoundNode
            VisitRvalue(node.Expression)
            Return Nothing
        End Function

        Public Overrides Function VisitUnaryOperator(node As BoundUnaryOperator) As BoundNode
            If node.OperatorKind = UnaryOperatorKind.Not Then
                VisitCondition(node.Operand)
                Me.SetConditionalState(Me.StateWhenFalse, Me.StateWhenTrue)
            Else
                VisitRvalue(node.Operand)
            End If
            Return Nothing
        End Function

        Public Overrides Function VisitUserDefinedUnaryOperator(node As BoundUserDefinedUnaryOperator) As BoundNode
            VisitRvalue(node.UnderlyingExpression)
            Return Nothing
        End Function

        Public Overrides Function VisitNullableIsTrueOperator(node As BoundNullableIsTrueOperator) As BoundNode
            VisitRvalue(node.Operand)
            Return Nothing
        End Function

        Public Overrides Function VisitArrayCreation(node As BoundArrayCreation) As BoundNode
            For Each e1 In node.Bounds
                VisitRvalue(e1)
            Next
            If node.InitializerOpt IsNot Nothing AndAlso Not node.InitializerOpt.Initializers.IsDefault Then
                For Each element In node.InitializerOpt.Initializers
                    VisitRvalue(element)
                Next
            End If
            Return Nothing
        End Function

        Public Overrides Function VisitArrayInitialization(node As BoundArrayInitialization) As BoundNode
            For Each initializer In node.Initializers
                VisitRvalue(initializer)
            Next
            Return Nothing
        End Function

        Public Overrides Function VisitArrayLiteral(node As BoundArrayLiteral) As BoundNode
            For Each arrayBound In node.Bounds
                VisitRvalue(arrayBound)
            Next
            VisitRvalue(node.Initializer)
            Return Nothing
        End Function

        Public Overrides Function VisitDirectCast(node As BoundDirectCast) As BoundNode
            VisitRvalue(node.Operand)
            Return Nothing
        End Function

        Public Overrides Function VisitTryCast(node As BoundTryCast) As BoundNode
            VisitRvalue(node.Operand)
            Return Nothing
        End Function

        Public Overrides Function VisitTypeOf(node As BoundTypeOf) As BoundNode
            VisitRvalue(node.Operand)
            Return Nothing
        End Function

        Public Overrides Function VisitMethodGroup(node As BoundMethodGroup) As BoundNode
            VisitRvalue(node.ReceiverOpt)
            Return Nothing
        End Function

        Public Overrides Function VisitPropertyGroup(node As BoundPropertyGroup) As BoundNode
            VisitRvalue(node.ReceiverOpt)
            Return Nothing
        End Function

        Public Overrides Function VisitGetType(node As BoundGetType) As BoundNode
            VisitTypeExpression(node.SourceType)
            Return Nothing
        End Function

        Public Overrides Function VisitSequencePoint(node As BoundSequencePoint) As BoundNode
            VisitStatement(node.StatementOpt)
            Return Nothing
        End Function

        Public Overrides Function VisitSequencePointWithSpan(node As BoundSequencePointWithSpan) As BoundNode
            VisitStatement(node.StatementOpt)
            Return Nothing
        End Function

        Public Overrides Function VisitStatementList(node As BoundStatementList) As BoundNode
            For Each statement In node.Statements
                Visit(statement)
            Next
            Return Nothing
        End Function

        Public Overrides Function VisitSyncLockStatement(node As BoundSyncLockStatement) As BoundNode
            ' visit SyncLock expression
            VisitRvalue(node.LockExpression)

            ' visit body
            VisitStatement(node.Body)

            Return Nothing
        End Function

        Public Overrides Function VisitUnboundLambda(node As UnboundLambda) As BoundNode
            Dim boundLambda As BoundLambda = node.BindForErrorRecovery()
            Debug.Assert(boundLambda IsNot Nothing)

            ' NOTE: we can skip checking if Me._firstInRegion is nothing because 'boundLambda' is not nothing
            If boundLambda Is Me._firstInRegion AndAlso Me._regionPlace = RegionPlace.Before Then
                Me.EnterRegion()
            End If

            VisitLambda(node.BindForErrorRecovery())

            ' NOTE: we can skip checking if Me._firstInRegion is nothing because 'boundLambda' is not nothing
            If boundLambda Is Me._lastInRegion AndAlso IsInside Then
                Me.LeaveRegion()
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitExitStatement(node As BoundExitStatement) As BoundNode
            Me._pendingBranches.Add(New PendingBranch(node, Me.State, Me._nesting))
            SetUnreachable()
            Return Nothing
        End Function

        Public Overrides Function VisitContinueStatement(node As BoundContinueStatement) As BoundNode
            Me._pendingBranches.Add(New PendingBranch(node, Me.State, Me._nesting))
            SetUnreachable()
            Return Nothing
        End Function

        Public Overrides Function VisitMyBaseReference(node As BoundMyBaseReference) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitDoLoopStatement(node As BoundDoLoopStatement) As BoundNode
            If node.ConditionOpt IsNot Nothing Then
                If node.ConditionIsTop Then
                    VisitDoLoopTopConditionStatement(node)
                Else
                    VisitDoLoopBottomConditionStatement(node)
                End If
            Else
                VisitUnconditionalDoLoopStatement(node)
            End If
            Return Nothing
        End Function

        Public Sub VisitDoLoopTopConditionStatement(node As BoundDoLoopStatement)
            Debug.Assert(node.ConditionIsTop AndAlso node.ConditionOpt IsNot Nothing)

            ' do while | until node.Condition statements node.ContinueLabel: loop node.BreakLabel:
            LoopHead(node)
            VisitCondition(node.ConditionOpt)
            Dim exitState As LocalState
            If node.ConditionIsUntil Then
                exitState = Me.StateWhenTrue
                Me.SetState(Me.StateWhenFalse)
            Else
                exitState = Me.StateWhenFalse
                Me.SetState(Me.StateWhenTrue)
            End If
            VisitStatement(node.Body)
            ResolveContinues(node.ContinueLabel)
            LoopTail(node)
            ResolveBreaks(exitState, node.ExitLabel)
        End Sub

        Public Sub VisitDoLoopBottomConditionStatement(node As BoundDoLoopStatement)
            Debug.Assert(Not node.ConditionIsTop AndAlso node.ConditionOpt IsNot Nothing)

            ' do statements node.ContinueLabel:  loop while|until node.Condition node.ExitLabel:
            LoopHead(node)
            VisitStatement(node.Body)
            ResolveContinues(node.ContinueLabel)
            VisitCondition(node.ConditionOpt)
            Dim exitState As LocalState
            If node.ConditionIsUntil Then
                exitState = Me.StateWhenTrue
                Me.SetState(Me.StateWhenFalse)
            Else
                exitState = Me.StateWhenFalse
                Me.SetState(Me.StateWhenTrue)
            End If
            LoopTail(node)
            ResolveBreaks(exitState, node.ExitLabel)
        End Sub

        Private Overloads Sub VisitUnconditionalDoLoopStatement(node As BoundDoLoopStatement)
            Debug.Assert(node.ConditionOpt Is Nothing)

            ' do  statements; node.ContinueLabel: loop node.BreakLabel:
            LoopHead(node)
            VisitStatement(node.Body)
            ResolveContinues(node.ContinueLabel)
            Dim exitState = UnreachableState()
            LoopTail(node)
            ResolveBreaks(exitState, node.ExitLabel)
        End Sub

        Public Overrides Function VisitGotoStatement(node As BoundGotoStatement) As BoundNode
            Me._pendingBranches.Add(New PendingBranch(node, Me.State, Me._nesting))
            SetUnreachable()
            Return Nothing
        End Function

        Public Overrides Function VisitLabelStatement(node As BoundLabelStatement) As BoundNode
            If ResolveBranches(node) Then
                Me.backwardBranchChanged = True
            End If
            Dim label As LabelSymbol = node.Label
            Dim _state As LocalState = LabelState(label)
            Me.IntersectWith(Me.State, _state)
            Me._labels(label) = New LabelStateAndNesting(node, Me.State.Clone(), Me._nesting)
            Me._labelsSeen.Add(label)
            Return Nothing
        End Function

        Public Overrides Function VisitThrowStatement(node As BoundThrowStatement) As BoundNode
            Dim expr As BoundExpression = node.ExpressionOpt
            If expr IsNot Nothing Then
                VisitRvalue(expr)
            End If

            SetUnreachable()
            Return Nothing
        End Function

        Public Overrides Function VisitNoOpStatement(node As BoundNoOpStatement) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitNamespaceExpression(node As BoundNamespaceExpression) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitFieldInitializer(node As BoundFieldInitializer) As BoundNode
            VisitRvalue(node.InitialValue)
            Return Nothing
        End Function

        Public Overrides Function VisitPropertyInitializer(node As BoundPropertyInitializer) As BoundNode
            VisitRvalue(node.InitialValue)
            Return Nothing
        End Function

        Public Overrides Function VisitArrayLength(node As BoundArrayLength) As BoundNode
            VisitRvalue(node.Expression)
            Return Nothing
        End Function

        Public Overrides Function VisitConditionalGoto(node As BoundConditionalGoto) As BoundNode
            VisitRvalue(node.Condition)
            Me._pendingBranches.Add(New PendingBranch(node, Me.State, Me._nesting))
            Return Nothing
        End Function

        Public Overrides Function VisitXmlDocument(node As BoundXmlDocument) As BoundNode
            VisitRvalue(node.Declaration)
            For Each child In node.ChildNodes
                VisitRvalue(child)
            Next
            Return Nothing
        End Function

        Public Overrides Function VisitXmlElement(node As BoundXmlElement) As BoundNode
            VisitRvalue(node.Argument)
            For Each child In node.ChildNodes
                VisitRvalue(child)
            Next
            Return Nothing
        End Function

        Public Overrides Function VisitXmlAttribute(node As BoundXmlAttribute) As BoundNode
            VisitRvalue(node.Name)
            VisitRvalue(node.Value)
            Return Nothing
        End Function

        Public Overrides Function VisitXmlName(node As BoundXmlName) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitXmlNamespace(node As BoundXmlNamespace) As BoundNode
            ' VisitRvalue(node.XmlNamespace) is supposed to be included in node.ObjectCreation
            VisitRvalue(node.ObjectCreation)
            Return Nothing
        End Function

        Public Overrides Function VisitXmlEmbeddedExpression(node As BoundXmlEmbeddedExpression) As BoundNode
            VisitRvalue(node.Expression)
            Return Nothing
        End Function

        Public Overrides Function VisitXmlMemberAccess(node As BoundXmlMemberAccess) As BoundNode
            VisitRvalue(node.MemberAccess)
            Return Nothing
        End Function

        Public Overrides Function VisitUnstructuredExceptionHandlingStatement(node As BoundUnstructuredExceptionHandlingStatement) As BoundNode
            Visit(node.Body)
            Return Nothing
        End Function

        Public Overrides Function VisitAwaitOperator(node As BoundAwaitOperator) As BoundNode
            VisitRvalue(node.Operand)
            Return Nothing
        End Function

        Public Overrides Function VisitNameOfOperator(node As BoundNameOfOperator) As BoundNode
            Dim savedState As LocalState = Me.State.Clone()
            SetUnreachable()
            VisitRvalue(node.Argument)
            Me.SetState(savedState)
            Return Nothing
        End Function

        Public Overrides Function VisitTypeAsValueExpression(node As BoundTypeAsValueExpression) As BoundNode
            Visit(node.Expression)
            Return Nothing
        End Function

        Public Overrides Function VisitInterpolatedStringExpression(node As BoundInterpolatedStringExpression) As BoundNode
            For Each item In node.Contents
                Debug.Assert(item.Kind = BoundKind.Literal OrElse item.Kind = BoundKind.Interpolation)
                Visit(item)
            Next

            Return Nothing
        End Function

        Public Overrides Function VisitInterpolation(node As BoundInterpolation) As BoundNode
            VisitRvalue(node.Expression)
            VisitRvalue(node.AlignmentOpt)
            VisitRvalue(node.FormatStringOpt)
            Return Nothing
        End Function

        Public Overrides Function VisitParameterEqualsValue(node As BoundParameterEqualsValue) As BoundNode
            VisitRvalue(node.Value)
            Return Nothing
        End Function

#End Region

    End Class

End Namespace
