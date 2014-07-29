' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class StateMachineRewriter(Of TStateMachineState As SynthesizedContainer, TProxy)

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
            Protected HasFinalizerState As Boolean = True

            ''' <summary>
            ''' If hasFinalizerState is true, this is the state for finalization from anywhere in this try block.
            ''' Initially set to -1, representing the no-op finalization required at the top level.
            ''' </summary>
            Protected CurrentFinalizerState As Integer = -1

            Public Sub New(F As SyntheticBoundNodeFactory,
                           stateField As FieldSymbol,
                           localProxies As Dictionary(Of Symbol, TProxy),
                           diagnostics As DiagnosticBag,
                           generateDebugInfo As Boolean)

                MyBase.New(F.CompilationState, diagnostics, generateDebugInfo)

                Me.F = F
                Me.StateField = stateField
                Me.CachedState = F.SynthesizedNamedLocal(F.SpecialType(SpecialType.System_Int32), TempKind.StateMachineCachedState, Nothing)

                For Each p In localProxies
                    Me.Proxies.Add(p.Key, p.Value)
                Next
            End Sub

            Friend Shared Function IsSynthetic(symbol As Symbol) As Boolean
                ' TODO: this matches C# implementation, but may need to be revised according to VB semantics

                ' Special Case: There's logic in the EE to recognize locals that have been captured by a lambda
                ' and would have been hoisted for the state machine.  Basically, we just hoist the local containing
                ' the instance of the lambda closure class as if it were user-created, rather than a compiler temp.
                ' The upshot is that an entry is created for a pseudo-local with a double-mangled name (hoisting
                ' mangling wrapping closure mangling, e.g. $VB$ResumableLocal_$VB$Closure_4$1).
                Dim name As String = symbol.Name
                Return Not (SyntaxFacts.IsValidIdentifier(name) AndAlso symbol.Locations.Length > 0 AndAlso symbol.CanBeReferencedByName) AndAlso
                       Not (name IsNot Nothing AndAlso name.StartsWith(StringConstants.ClosureVariablePrefix))
            End Function

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
                    Return Me.F.CurrentType.TypeSubstitution
                End Get
            End Property

            Protected Overrides ReadOnly Property CurrentMethod As MethodSymbol
                Get
                    Return Me.F.CurrentMethod
                End Get
            End Property

            Protected Overrides ReadOnly Property TopLevelMethod As MethodSymbol
                Get
                    Return Me.F.TopLevelMethod
                End Get
            End Property

            Friend Overrides Function FramePointer(syntax As VisualBasicSyntaxNode, frameClass As NamedTypeSymbol) As BoundExpression
                Dim oldSyntax As VisualBasicSyntaxNode = Me.F.Syntax
                Me.F.Syntax = syntax
                Dim result = Me.F.Me()
                Debug.Assert(frameClass = result.Type)
                Me.F.Syntax = oldSyntax
                Return result
            End Function

            Protected Structure StateInfo
                Public ReadOnly Number As Integer
                Public ReadOnly ResumeLabel As GeneratedLabelSymbol

                Public Sub New(stateNumber As Integer, resumeLabel As GeneratedLabelSymbol)
                    Me.Number = stateNumber
                    Me.ResumeLabel = resumeLabel
                End Sub
            End Structure

            Protected Function AddState() As StateInfo
                Dim stateNumber As Integer = Me.NextState
                Me.NextState += 1

                If Me.Dispatches Is Nothing Then
                    Me.Dispatches = New Dictionary(Of LabelSymbol, List(Of Integer))()
                End If

                If Not Me.HasFinalizerState Then
                    Me.CurrentFinalizerState = Me.NextState
                    Me.NextState += 1
                    Me.HasFinalizerState = True
                End If

                Dim resumeLabel As GeneratedLabelSymbol = Me.F.GenerateLabel(ResumeLabelName)
                Me.Dispatches.Add(resumeLabel, New List(Of Integer)() From {stateNumber})
                Me.FinalizerStateMap.Add(stateNumber, Me.CurrentFinalizerState)

                Return New StateInfo(stateNumber, resumeLabel)
            End Function

            Protected Function Dispatch() As BoundStatement
                Return Me.F.Select(
                            Me.F.Local(Me.CachedState, isLValue:=False),
                            From kv In Me.Dispatches
                            Order By kv.Value(0)
                            Select Me.F.SwitchSection(kv.Value, F.Goto(kv.Key)))
            End Function

#Region "Visitors"

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                If node Is Nothing Then
                    Return node
                End If

                Dim oldSyntax As VisualBasicSyntaxNode = Me.F.Syntax
                Me.F.Syntax = node.Syntax
                Dim result As BoundNode = MyBase.Visit(node)
                Me.F.Syntax = oldSyntax
                Return result
            End Function

            Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode
                Return PossibleStateMachineScope(node.Locals, MyBase.VisitBlock(node))
            End Function

            Private Function PossibleStateMachineScope(locals As ImmutableArray(Of LocalSymbol), wrapped As BoundNode) As BoundNode
                If locals.IsEmpty Then
                    Return wrapped
                End If

                Dim proxyFields = ArrayBuilder(Of FieldSymbol).GetInstance()
                For Each local In locals
                    If Not IsSynthetic(local) Then
                        Dim proxy As TProxy = Nothing
                        If Proxies.TryGetValue(local, proxy) Then
                            Me.AddProxyFieldsForStateMachineScope(proxy, proxyFields)
                        End If
                    End If
                Next

                If proxyFields.Count = 0 Then
                    proxyFields.Free()
                    Return wrapped
                End If

                ' Wrap it into a regular block to make sure the result of rewriting is a BoundBlock still
                Return Me.F.Block(New BoundStateMachineScope(Me.F.Syntax,
                                                             proxyFields.ToImmutableAndFree(),
                                                             DirectCast(wrapped, BoundStatement)).MakeCompilerGenerated)
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

                Dim oldDispatches As Dictionary(Of LabelSymbol, List(Of Integer)) = Me.Dispatches
                Dim oldFinalizerState As Integer = Me.CurrentFinalizerState
                Dim oldHasFinalizerState As Boolean = Me.HasFinalizerState

                Me.Dispatches = Nothing
                Me.CurrentFinalizerState = -1
                Me.HasFinalizerState = False

                Dim tryBlock As BoundBlock = Me.F.Block(DirectCast(Me.Visit(node.TryBlock), BoundStatement))
                Dim dispatchLabel As GeneratedLabelSymbol = Nothing
                If Me.Dispatches IsNot Nothing Then
                    dispatchLabel = Me.F.GenerateLabel("tryDispatch")

                    If Me.HasFinalizerState Then
                        ' cause the current finalizer state to arrive here and then "return false"
                        Dim finalizer As GeneratedLabelSymbol = Me.F.GenerateLabel("finalizer")
                        Me.Dispatches.Add(finalizer, New List(Of Integer)() From {Me.CurrentFinalizerState})

                        Dim skipFinalizer As GeneratedLabelSymbol = Me.F.GenerateLabel("skipFinalizer")
                        tryBlock = Me.F.Block(Me.F.HiddenSequencePoint(Me.GenerateDebugInfo),
                                              Me.Dispatch(),
                                              Me.F.Goto(skipFinalizer),
                                              Me.F.Label(finalizer),
                                              Me.F.Assignment(
                                                  F.Field(F.Me(), Me.StateField, True),
                                                  F.AssignmentExpression(F.Local(Me.CachedState, True), F.Literal(StateMachineStates.NotStartedStateMachine))),
                                              Me.GenerateReturn(False),
                                              Me.F.Label(skipFinalizer),
                                              tryBlock)
                    Else
                        tryBlock = Me.F.Block(Me.F.HiddenSequencePoint(Me.GenerateDebugInfo), Me.Dispatch(), tryBlock)
                    End If

                    If oldDispatches Is Nothing Then
                        oldDispatches = New Dictionary(Of LabelSymbol, List(Of Integer))()
                    End If

                    oldDispatches.Add(dispatchLabel, New List(Of Integer)(From kv In Dispatches.Values From n In kv Order By n Select n))
                End If

                Me.HasFinalizerState = oldHasFinalizerState
                Me.CurrentFinalizerState = oldFinalizerState

                Me.Dispatches = oldDispatches

                Dim catchBlocks As ImmutableArray(Of BoundCatchBlock) = Me.VisitList(node.CatchBlocks)
                Dim finallyBlockOpt As BoundBlock = If(node.FinallyBlockOpt Is Nothing, Nothing,
                                                       Me.F.Block(
                                                           Me.F.HiddenSequencePoint(Me.GenerateDebugInfo),
                                                           Me.F.If(
                                                               condition:=Me.F.IntLessThan(
                                                                   Me.F.Local(Me.CachedState, False),
                                                                   Me.F.Literal(StateMachineStates.FirstUnusedState)),
                                                               thenClause:=DirectCast(Me.Visit(node.FinallyBlockOpt), BoundBlock)),
                                                           Me.F.HiddenSequencePoint(Me.GenerateDebugInfo)))

                Dim result As BoundStatement = node.Update(tryBlock, catchBlocks, finallyBlockOpt, node.ExitLabelOpt)
                If dispatchLabel IsNot Nothing Then
                    result = Me.F.Block(Me.F.HiddenSequencePoint(Me.GenerateDebugInfo), Me.F.Label(dispatchLabel), result)
                End If

                Return result
            End Function

            Public Overrides Function VisitMeReference(node As BoundMeReference) As BoundNode
                Return Me.MaterializeProxy(node, Me.Proxies(Me.TopLevelMethod.MeParameter))
            End Function

            Public Overrides Function VisitMyClassReference(node As BoundMyClassReference) As BoundNode
                Return Me.MaterializeProxy(node, Me.Proxies(Me.TopLevelMethod.MeParameter))
            End Function

            Public Overrides Function VisitMyBaseReference(node As BoundMyBaseReference) As BoundNode
                Return Me.MaterializeProxy(node, Me.Proxies(Me.TopLevelMethod.MeParameter))
            End Function

            Public Overrides Function VisitCatchBlock(node As BoundCatchBlock) As BoundNode
                ' Neither Async nor Iterator function allows await/yield in Catch block,
                ' thus, it should not capture/hoist catch block local
                Dim rewrittenCatchLocal As LocalSymbol = Nothing

                Dim origLocal As LocalSymbol = node.LocalOpt
                If origLocal IsNot Nothing Then
                    ' local may be either the original local of a frame reference
                    Debug.Assert(Not Me.Proxies.ContainsKey(origLocal), "captured local should not need rewriting")

                    Dim newType = VisitType(origLocal.Type)
                    If newType = origLocal.Type Then
                        ' keeping same local
                        rewrittenCatchLocal = origLocal

                    Else
                        ' need a local of a different type
                        rewrittenCatchLocal = CreateReplacementLocalOrReturnSelf(origLocal, newType)
                        Me.LocalMap.Add(origLocal, rewrittenCatchLocal)
                    End If
                End If

                Dim rewrittenExceptionVariable As BoundExpression = DirectCast(Me.Visit(node.ExceptionSourceOpt), BoundExpression)

                ' rewrite filter and body
                ' NOTE: this will proxy all accesses to exception local if that got hoisted.
                Dim rewrittenErrorLineNumber = DirectCast(Me.Visit(node.ErrorLineNumberOpt), BoundExpression)
                Dim rewrittenFilter = DirectCast(Me.Visit(node.ExceptionFilterOpt), BoundExpression)
                Dim rewrittenBody = DirectCast(Me.Visit(node.Body), BoundBlock)

                ' rebuild the node.
                Return node.Update(rewrittenCatchLocal,
                                   rewrittenExceptionVariable,
                                   rewrittenErrorLineNumber,
                                   rewrittenFilter,
                                   rewrittenBody)
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
