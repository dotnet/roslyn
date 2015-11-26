' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class StateMachineRewriter(Of TProxy)

        Friend MustInherit Class StateMachineMethodToClassRewriter
            Inherits MethodToClassRewriter(Of TProxy)

            Protected Friend ReadOnly F As SyntheticBoundNodeFactory
            Protected NextState As Integer = 0

            ''' <summary>
            ''' The "state" of the state machine that is the translation of the iterator method.
            ''' </summary>
            Protected ReadOnly StateField As FieldSymbol

            ''' <summary>
            ''' Cached "state" of the state machine within the MoveNext method.  We work with a copy of
            ''' the state to avoid shared mutable state between threads.  (Two threads can be executing
            ''' in a Task's MoveNext method because an awaited task may complete after the awaiter has
            ''' tested whether the subtask is complete but before the awaiter has returned)
            ''' </summary>
            Protected ReadOnly CachedState As LocalSymbol

            ''' <summary>
            ''' For each distinct label, the set of states that need to be dispatched to that label.
            ''' Note that there is a dispatch occurring at every try-finally statement, so this
            ''' variable takes on a new set of values inside each try block.
            ''' </summary>
            Protected Dispatches As New Dictionary(Of LabelSymbol, List(Of Integer))

            ''' <summary>
            ''' A mapping from each state of the state machine to the new state that will be used to execute
            ''' finally blocks in case the state machine is disposed.  The Dispose method computes the new 
            ''' state and then runs MoveNext.
            ''' </summary>
            Protected ReadOnly FinalizerStateMap As New Dictionary(Of Integer, Integer)()

            ''' <summary>
            ''' A try block might have no state (transitions) within it, in which case it does not need
            ''' to have a state to represent finalization.  This flag tells us whether the current try
            ''' block that we are within has a finalizer state.  Initially true as we have the (trivial)
            ''' finalizer state of -1 at the top level.
            ''' </summary>
            Private _hasFinalizerState As Boolean = True

            ''' <summary>
            ''' If hasFinalizerState is true, this is the state for finalization from anywhere in this try block.
            ''' Initially set to -1, representing the no-op finalization required at the top level.
            ''' </summary>
            Private _currentFinalizerState As Integer = -1

            ''' <summary>
            ''' The set of local variables and parameters that were hoisted and need a proxy.
            ''' </summary>
            Private ReadOnly _hoistedVariables As IReadOnlySet(Of Symbol) = Nothing

            Private ReadOnly _synthesizedLocalOrdinals As SynthesizedLocalOrdinalsDispenser
            Private _nextFreeHoistedLocalSlot As Integer

            Public Sub New(F As SyntheticBoundNodeFactory,
                           stateField As FieldSymbol,
                           hoistedVariables As IReadOnlySet(Of Symbol),
                           initialProxies As Dictionary(Of Symbol, TProxy),
                           synthesizedLocalOrdinals As SynthesizedLocalOrdinalsDispenser,
                           slotAllocatorOpt As VariableSlotAllocator,
                           nextFreeHoistedLocalSlot As Integer,
                           Diagnostics As DiagnosticBag)

                MyBase.New(slotAllocatorOpt, F.CompilationState, Diagnostics, preserveOriginalLocals:=False)

                Debug.Assert(F IsNot Nothing)
                Debug.Assert(stateField IsNot Nothing)
                Debug.Assert(hoistedVariables IsNot Nothing)
                Debug.Assert(initialProxies IsNot Nothing)
                Debug.Assert(nextFreeHoistedLocalSlot >= 0)
                Debug.Assert(Diagnostics IsNot Nothing)

                Me.F = F
                Me.StateField = stateField
                CachedState = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int32), SynthesizedLocalKind.StateMachineCachedState, F.Syntax)
                _hoistedVariables = hoistedVariables
                _synthesizedLocalOrdinals = synthesizedLocalOrdinals
                _nextFreeHoistedLocalSlot = nextFreeHoistedLocalSlot

                For Each p In initialProxies
                    Proxies.Add(p.Key, p.Value)
                Next
            End Sub

            ''' <summary>
            ''' Implementation-specific name for labels to mark state machine resume points.
            ''' </summary>
            Protected MustOverride ReadOnly Property ResumeLabelName As String

            ''' <summary>
            ''' Generate return statements from the state machine method body.
            ''' </summary>
            Protected MustOverride Function GenerateReturn(finished As Boolean) As BoundStatement

            Protected Overrides ReadOnly Property TypeMap As TypeSubstitution
                Get
                    Return F.CurrentType.TypeSubstitution
                End Get
            End Property

            Protected Overrides ReadOnly Property CurrentMethod As MethodSymbol
                Get
                    Return F.CurrentMethod
                End Get
            End Property

            Protected Overrides ReadOnly Property TopLevelMethod As MethodSymbol
                Get
                    Return F.TopLevelMethod
                End Get
            End Property

            Friend Overrides Function FramePointer(syntax As VisualBasicSyntaxNode, frameClass As NamedTypeSymbol) As BoundExpression
                Dim oldSyntax As VisualBasicSyntaxNode = F.Syntax
                F.Syntax = syntax
                Dim result = F.Me()
                Debug.Assert(frameClass = result.Type)
                F.Syntax = oldSyntax
                Return result
            End Function

            Protected Structure StateInfo
                Public ReadOnly Number As Integer
                Public ReadOnly ResumeLabel As GeneratedLabelSymbol

                Public Sub New(stateNumber As Integer, resumeLabel As GeneratedLabelSymbol)
                    Number = stateNumber
                    Me.ResumeLabel = resumeLabel
                End Sub
            End Structure

            Protected Function AddState() As StateInfo
                Dim stateNumber As Integer = NextState
                NextState += 1

                If Dispatches Is Nothing Then
                    Dispatches = New Dictionary(Of LabelSymbol, List(Of Integer))()
                End If

                If Not _hasFinalizerState Then
                    _currentFinalizerState = NextState
                    NextState += 1
                    _hasFinalizerState = True
                End If

                Dim resumeLabel As GeneratedLabelSymbol = F.GenerateLabel(ResumeLabelName)
                Dispatches.Add(resumeLabel, New List(Of Integer)() From {stateNumber})
                FinalizerStateMap.Add(stateNumber, _currentFinalizerState)

                Return New StateInfo(stateNumber, resumeLabel)
            End Function

            Protected Function Dispatch() As BoundStatement
                Return F.Select(
                            F.Local(CachedState, isLValue:=False),
                            From kv In Dispatches
                            Order By kv.Value(0)
                            Select F.SwitchSection(kv.Value, F.Goto(kv.Key)))
            End Function

#Region "Visitors"

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                If node Is Nothing Then
                    Return node
                End If

                Dim oldSyntax As VisualBasicSyntaxNode = F.Syntax
                F.Syntax = node.Syntax
                Dim result As BoundNode = MyBase.Visit(node)
                F.Syntax = oldSyntax
                Return result
            End Function

            Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode
                Return PossibleStateMachineScope(node.Locals, MyBase.VisitBlock(node))
            End Function

            Private Function PossibleStateMachineScope(locals As ImmutableArray(Of LocalSymbol), wrapped As BoundNode) As BoundNode
                If locals.IsEmpty Then
                    Return wrapped
                End If

                Dim hoistedLocalsWithDebugScopes = ArrayBuilder(Of FieldSymbol).GetInstance()
                For Each local In locals
                    If Not NeedsProxy(local) Then
                        Continue For
                    End If

                    ' Ref synthesized variables have proxies that are allocated in VisitAssignmentOperator.
                    If local.IsByRef Then
                        Debug.Assert(local.SynthesizedKind = SynthesizedLocalKind.AwaitSpill)
                        Continue For
                    End If

                    Debug.Assert(local.SynthesizedKind.IsLongLived())

                    ' We need to produce hoisted local scope debug information for user locals as well as 
                    ' lambda display classes, since Dev12 EE uses them to determine which variables are displayed 
                    ' in Locals window.
                    If local.SynthesizedKind = SynthesizedLocalKind.UserDefined OrElse local.SynthesizedKind = SynthesizedLocalKind.LambdaDisplayClass Then
                        Dim proxy As TProxy = Nothing
                        If Proxies.TryGetValue(local, proxy) Then
                            AddProxyFieldsForStateMachineScope(proxy, hoistedLocalsWithDebugScopes)
                        End If
                    End If
                Next

                ' Wrap the node in a StateMachineScope for debugging
                Dim translatedStatement As BoundNode = wrapped
                If hoistedLocalsWithDebugScopes.Count > 0 Then
                    translatedStatement = MakeStateMachineScope(hoistedLocalsWithDebugScopes.ToImmutable(), DirectCast(translatedStatement, BoundStatement))
                End If

                hoistedLocalsWithDebugScopes.Free()

                Return translatedStatement
            End Function

            ''' <remarks>
            ''' Must remain in sync with <see cref="TryUnwrapBoundStateMachineScope"/>.
            ''' </remarks>
            Friend Function MakeStateMachineScope(hoistedLocals As ImmutableArray(Of FieldSymbol), statement As BoundStatement) As BoundBlock
                Return F.Block(New BoundStateMachineScope(F.Syntax, hoistedLocals, statement).MakeCompilerGenerated)
            End Function

            ''' <remarks>
            ''' Must remain in sync with <see cref="MakeStateMachineScope"/>.
            ''' </remarks>
            Friend Function TryUnwrapBoundStateMachineScope(ByRef statement As BoundStatement, <Out> ByRef hoistedLocals As ImmutableArray(Of FieldSymbol)) As Boolean
                If statement.Kind = BoundKind.Block Then
                    Dim rewrittenBlock = DirectCast(statement, BoundBlock)
                    Dim rewrittenStatements = rewrittenBlock.Statements
                    If rewrittenStatements.Length = 1 AndAlso rewrittenStatements(0).Kind = BoundKind.StateMachineScope Then
                        Dim stateMachineScope = DirectCast(rewrittenStatements(0), BoundStateMachineScope)
                        statement = stateMachineScope.Statement
                        hoistedLocals = stateMachineScope.Fields
                        Return True
                    End If
                End If

                hoistedLocals = ImmutableArray(Of FieldSymbol).Empty
                Return False
            End Function

            Private Function NeedsProxy(localOrParameter As Symbol) As Boolean
                Debug.Assert(localOrParameter.Kind = SymbolKind.Local OrElse localOrParameter.Kind = SymbolKind.Parameter)
                Return _hoistedVariables.Contains(localOrParameter)
            End Function

            Friend MustOverride Sub AddProxyFieldsForStateMachineScope(proxy As TProxy, proxyFields As ArrayBuilder(Of FieldSymbol))

            ''' <summary>
            ''' The try statement is the most complex part of the state machine transformation.
            ''' Since the CLR will not allow a 'goto' into the scope of a try statement, we must
            ''' generate the dispatch to the state's label stepwise.  That is done by translating
            ''' the try statements from the inside to the outside.  Within a try statement, we
            ''' start with an empty dispatch table (representing the mapping from state numbers
            ''' to labels).  During translation of the try statement's body, the dispatch table
            ''' will be filled in with the data necessary to dispatch once we're inside the try
            ''' block.  We generate that at the head of the translated try statement.  Then, we
            ''' copy all of the states from that table into the table for the enclosing construct,
            ''' but associate them with a label just before the translated try block.  That way
            ''' the enclosing construct will generate the code necessary to get control into the
            ''' try block for all of those states.
            ''' </summary>
            Public Overrides Function VisitTryStatement(node As BoundTryStatement) As BoundNode

                Dim oldDispatches As Dictionary(Of LabelSymbol, List(Of Integer)) = Dispatches
                Dim oldFinalizerState As Integer = _currentFinalizerState
                Dim oldHasFinalizerState As Boolean = _hasFinalizerState

                Dispatches = Nothing
                _currentFinalizerState = -1
                _hasFinalizerState = False

                Dim tryBlock As BoundBlock = F.Block(DirectCast(Visit(node.TryBlock), BoundStatement))
                Dim dispatchLabel As GeneratedLabelSymbol = Nothing
                If Dispatches IsNot Nothing Then
                    dispatchLabel = F.GenerateLabel("tryDispatch")

                    If _hasFinalizerState Then
                        ' cause the current finalizer state to arrive here and then "return false"
                        Dim finalizer As GeneratedLabelSymbol = F.GenerateLabel("finalizer")
                        Dispatches.Add(finalizer, New List(Of Integer)() From {_currentFinalizerState})

                        Dim skipFinalizer As GeneratedLabelSymbol = F.GenerateLabel("skipFinalizer")
                        tryBlock = F.Block(F.HiddenSequencePoint(),
                                              Dispatch(),
                                              F.Goto(skipFinalizer),
                                              F.Label(finalizer),
                                              F.Assignment(
                                                  F.Field(F.Me(), StateField, True),
                                                  F.AssignmentExpression(F.Local(CachedState, True), F.Literal(StateMachineStates.NotStartedStateMachine))),
                                              GenerateReturn(False),
                                              F.Label(skipFinalizer),
                                              tryBlock)
                    Else
                        tryBlock = F.Block(F.HiddenSequencePoint(), Dispatch(), tryBlock)
                    End If

                    If oldDispatches Is Nothing Then
                        oldDispatches = New Dictionary(Of LabelSymbol, List(Of Integer))()
                    End If

                    oldDispatches.Add(dispatchLabel, New List(Of Integer)(From kv In Dispatches.Values From n In kv Order By n Select n))
                End If

                _hasFinalizerState = oldHasFinalizerState
                _currentFinalizerState = oldFinalizerState

                Dispatches = oldDispatches

                Dim catchBlocks As ImmutableArray(Of BoundCatchBlock) = VisitList(node.CatchBlocks)
                Dim finallyBlockOpt As BoundBlock = If(node.FinallyBlockOpt Is Nothing, Nothing,
                                                       F.Block(
                                                           F.HiddenSequencePoint(),
                                                           F.If(
                                                               condition:=F.IntLessThan(
                                                                   F.Local(CachedState, False),
                                                                   F.Literal(StateMachineStates.FirstUnusedState)),
                                                               thenClause:=DirectCast(Visit(node.FinallyBlockOpt), BoundBlock)),
                                                           F.HiddenSequencePoint()))

                Dim result As BoundStatement = node.Update(tryBlock, catchBlocks, finallyBlockOpt, node.ExitLabelOpt)
                If dispatchLabel IsNot Nothing Then
                    result = F.Block(F.HiddenSequencePoint(), F.Label(dispatchLabel), result)
                End If

                Return result
            End Function

            Public Overrides Function VisitMeReference(node As BoundMeReference) As BoundNode
                Return MaterializeProxy(node, Proxies(TopLevelMethod.MeParameter))
            End Function

            Public Overrides Function VisitMyClassReference(node As BoundMyClassReference) As BoundNode
                Return MaterializeProxy(node, Proxies(TopLevelMethod.MeParameter))
            End Function

            Public Overrides Function VisitMyBaseReference(node As BoundMyBaseReference) As BoundNode
                Return MaterializeProxy(node, Proxies(TopLevelMethod.MeParameter))
            End Function

            Private Function TryRewriteLocal(local As LocalSymbol) As LocalSymbol
                If NeedsProxy(local) Then
                    ' no longer a local symbol
                    Return Nothing
                End If

                Dim newLocal As LocalSymbol = Nothing
                If Not LocalMap.TryGetValue(local, newLocal) Then
                    Dim newType = VisitType(local.Type)
                    If newType = local.Type Then
                        ' keeping same local
                        newLocal = local
                    Else
                        ' need a local of a different type
                        newLocal = LocalSymbol.Create(local, newType)
                        LocalMap.Add(local, newLocal)
                    End If
                End If

                Return newLocal
            End Function

            Public Overrides Function VisitCatchBlock(node As BoundCatchBlock) As BoundNode
                ' Yield/Await aren't supported in Catch block, but we need to
                ' rewrite the type of the variable owned by the catch block.
                ' Note that this variable might be a closure frame reference.
                Dim rewrittenCatchLocal As LocalSymbol = Nothing

                Dim origLocal As LocalSymbol = node.LocalOpt
                If origLocal IsNot Nothing Then
                    rewrittenCatchLocal = TryRewriteLocal(origLocal)
                End If

                Dim rewrittenExceptionVariable As BoundExpression = DirectCast(Visit(node.ExceptionSourceOpt), BoundExpression)

                ' rewrite filter and body
                ' NOTE: this will proxy all accesses to exception local if that got hoisted.
                Dim rewrittenErrorLineNumber = DirectCast(Visit(node.ErrorLineNumberOpt), BoundExpression)
                Dim rewrittenFilter = DirectCast(Visit(node.ExceptionFilterOpt), BoundExpression)
                Dim rewrittenBody = DirectCast(Visit(node.Body), BoundBlock)

                ' rebuild the node.
                Return node.Update(rewrittenCatchLocal,
                                   rewrittenExceptionVariable,
                                   rewrittenErrorLineNumber,
                                   rewrittenFilter,
                                   rewrittenBody,
                                   isSynthesizedAsyncCatchAll:=node.IsSynthesizedAsyncCatchAll)
            End Function

#End Region

#Region "Nodes not existing by the time State Machine rewriter is involved"

            Public NotOverridable Overrides Function VisitUserDefinedConversion(node As BoundUserDefinedConversion) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitUserDefinedShortCircuitingOperator(node As BoundUserDefinedShortCircuitingOperator) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitUserDefinedBinaryOperator(node As BoundUserDefinedBinaryOperator) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitUserDefinedUnaryOperator(node As BoundUserDefinedUnaryOperator) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitBadExpression(node As BoundBadExpression) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitBadVariable(node As BoundBadVariable) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitParenthesized(node As BoundParenthesized) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitUnboundLambda(node As UnboundLambda) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitAttribute(node As BoundAttribute) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitAnonymousTypeCreationExpression(node As BoundAnonymousTypeCreationExpression) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitAnonymousTypePropertyAccess(node As BoundAnonymousTypePropertyAccess) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitAnonymousTypeFieldInitializer(node As BoundAnonymousTypeFieldInitializer) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitLateMemberAccess(node As BoundLateMemberAccess) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitLateInvocation(node As BoundLateInvocation) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitLateAddressOfOperator(node As BoundLateAddressOfOperator) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitLateBoundArgumentSupportingAssignmentWithCapture(node As BoundLateBoundArgumentSupportingAssignmentWithCapture) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitGroupTypeInferenceLambda(node As GroupTypeInferenceLambda) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitLambda(node As BoundLambda) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitQueryLambda(node As BoundQueryLambda) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlNamespace(node As BoundXmlNamespace) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlName(node As BoundXmlName) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlDocument(node As BoundXmlDocument) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlDeclaration(node As BoundXmlDeclaration) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlProcessingInstruction(node As BoundXmlProcessingInstruction) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlComment(node As BoundXmlComment) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlAttribute(node As BoundXmlAttribute) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlElement(node As BoundXmlElement) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlMemberAccess(node As BoundXmlMemberAccess) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlCData(node As BoundXmlCData) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitXmlEmbeddedExpression(node As BoundXmlEmbeddedExpression) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitByRefArgumentPlaceholder(node As BoundByRefArgumentPlaceholder) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitByRefArgumentWithCopyBack(node As BoundByRefArgumentWithCopyBack) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitCompoundAssignmentTargetPlaceholder(node As BoundCompoundAssignmentTargetPlaceholder) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitWithLValueExpressionPlaceholder(node As BoundWithLValueExpressionPlaceholder) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitWithRValueExpressionPlaceholder(node As BoundWithRValueExpressionPlaceholder) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitMethodGroup(node As BoundMethodGroup) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitPropertyGroup(node As BoundPropertyGroup) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitValueTypeMeReference(node As BoundValueTypeMeReference) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitObjectInitializerExpression(node As BoundObjectInitializerExpression) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitCollectionInitializerExpression(node As BoundCollectionInitializerExpression) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitPropertyAccess(node As BoundPropertyAccess) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitArrayLiteral(node As BoundArrayLiteral) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitNullableIsTrueOperator(node As BoundNullableIsTrueOperator) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitNewT(node As BoundNewT) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitDup(node As BoundDup) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitOmittedArgument(node As BoundOmittedArgument) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitTypeArguments(node As BoundTypeArguments) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitTypeOrValueExpression(node As BoundTypeOrValueExpression) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public NotOverridable Overrides Function VisitAddressOfOperator(node As BoundAddressOfOperator) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

#End Region

        End Class

    End Class

End Namespace
