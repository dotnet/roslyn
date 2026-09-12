' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class IteratorRewriter
        Inherits StateMachineRewriter(Of FieldSymbol)

        Private ReadOnly _elementType As TypeSymbol
        Private ReadOnly _isEnumerable As Boolean

        Private _currentField As FieldSymbol
        Private _initialThreadIdField As FieldSymbol

        Public Sub New(body As BoundStatement,
                       method As MethodSymbol,
                       isEnumerable As Boolean,
                       stateMachineType As IteratorStateMachine,
                       stateMachineStateDebugInfoBuilder As ArrayBuilder(Of StateMachineStateDebugInfo),
                       slotAllocatorOpt As VariableSlotAllocator,
                       compilationState As TypeCompilationState,
                       diagnostics As BindingDiagnosticBag)

            MyBase.New(body, method, stateMachineType, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, compilationState, diagnostics)

            Me._isEnumerable = isEnumerable

            Dim methodReturnType = method.ReturnType
            If methodReturnType.GetArity = 0 Then
                Me._elementType = method.ContainingAssembly.GetSpecialType(SpecialType.System_Object)
            Else
                ' the element type may contain method type parameters, which are now alpha-renamed into type parameters of the generated class
                Me._elementType = DirectCast(methodReturnType, NamedTypeSymbol).TypeArgumentsNoUseSiteDiagnostics().Single().InternalSubstituteTypeParameters(Me.TypeMap).Type
            End If
        End Sub

        ''' <summary>
        ''' Rewrite an iterator method into a state machine class.
        ''' </summary>
        Friend Overloads Shared Function Rewrite(body As BoundBlock,
                                                 method As MethodSymbol,
                                                 methodOrdinal As Integer,
                                                 stateMachineStateDebugInfoBuilder As ArrayBuilder(Of StateMachineStateDebugInfo),
                                                 slotAllocatorOpt As VariableSlotAllocator,
                                                 compilationState As TypeCompilationState,
                                                 diagnostics As BindingDiagnosticBag,
                                                 <Out> ByRef stateMachineType As IteratorStateMachine) As BoundBlock

            If body.HasErrors Or Not method.IsIterator Then
                Return body
            End If

            Dim methodReturnType As TypeSymbol = method.ReturnType

            Dim retSpecialType = method.ReturnType.OriginalDefinition.SpecialType
            Dim isEnumerable As Boolean = retSpecialType = SpecialType.System_Collections_Generic_IEnumerable_T OrElse
                                          retSpecialType = SpecialType.System_Collections_IEnumerable

            Dim elementType As TypeSymbol
            If method.ReturnType.IsDefinition Then
                elementType = method.ContainingAssembly.GetSpecialType(SpecialType.System_Object)
            Else
                elementType = DirectCast(methodReturnType, NamedTypeSymbol).TypeArgumentsNoUseSiteDiagnostics(0)
            End If

            stateMachineType = New IteratorStateMachine(slotAllocatorOpt, compilationState, method, methodOrdinal, elementType, isEnumerable)

            compilationState.ModuleBuilderOpt.CompilationState.SetStateMachineType(method, stateMachineType)

            Dim rewriter As New IteratorRewriter(body, method, isEnumerable, stateMachineType, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, compilationState, diagnostics)

            ' check if we have all the types we need
            If rewriter.EnsureAllSymbolsAndSignature() Then
                Return body
            End If

            Return rewriter.Rewrite()
        End Function

        ''' <returns>
        ''' Returns true if any members that we need are missing or have use-site errors.
        ''' </returns>
        Friend Overrides Function EnsureAllSymbolsAndSignature() As Boolean
            If MyBase.EnsureAllSymbolsAndSignature OrElse Me.Method.IsSub OrElse Me._elementType.IsErrorType Then
                Return True
            End If

            Dim bag = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=Me.Diagnostics.AccumulatesDependencies)

            ' NOTE: We don't ensure DebuggerStepThroughAttribute, it is just not emitted if not found
            ' EnsureWellKnownMember(Of MethodSymbol)(WellKnownMember.System_Diagnostics_DebuggerStepThroughAttribute__ctor, hasErrors)

            ' TODO: do we need these here? They are used on the actual iterator method.
            ' EnsureWellKnownMember(Of MethodSymbol)(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachine__ctor, hasErrors)

            EnsureSpecialType(SpecialType.System_Object, bag)
            EnsureSpecialType(SpecialType.System_Boolean, bag)
            EnsureSpecialType(SpecialType.System_Int32, bag)
            EnsureSpecialType(SpecialType.System_IDisposable, bag)
            EnsureSpecialMember(SpecialMember.System_IDisposable__Dispose, bag)

            ' IEnumerator
            EnsureSpecialType(SpecialType.System_Collections_IEnumerator, bag)
            EnsureSpecialPropertyGetter(SpecialMember.System_Collections_IEnumerator__Current, bag)
            EnsureSpecialMember(SpecialMember.System_Collections_IEnumerator__MoveNext, bag)
            EnsureSpecialMember(SpecialMember.System_Collections_IEnumerator__Reset, bag)

            ' IEnumerator(Of T)
            EnsureSpecialType(SpecialType.System_Collections_Generic_IEnumerator_T, bag)
            EnsureSpecialPropertyGetter(SpecialMember.System_Collections_Generic_IEnumerator_T__Current, bag)

            If Me._isEnumerable Then
                EnsureSpecialType(SpecialType.System_Collections_IEnumerable, bag)
                EnsureSpecialMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator, bag)
                EnsureSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T, bag)
                EnsureSpecialMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator, bag)
            End If

            Dim hasErrors As Boolean = bag.HasAnyErrors
            If hasErrors Then
                Me.Diagnostics.AddRange(bag)
            Else
                Me.Diagnostics.AddDependencies(bag)
            End If

            bag.Free()
            Return hasErrors
        End Function

        Protected Overrides Sub GenerateControlFields()
            ' Add a field: int _state
            Me.StateField = Me.F.StateMachineField(Me.F.SpecialType(SpecialType.System_Int32), Me.Method, GeneratedNames.MakeStateMachineStateFieldName(), Accessibility.Public)

            ' Add a field: T current
            _currentField = F.StateMachineField(_elementType, Me.Method, GeneratedNames.MakeIteratorCurrentFieldName(), Accessibility.Public)

            ' if it is an Enumerable, add a field: initialThreadId As Integer
            _initialThreadIdField = If(_isEnumerable,
                F.StateMachineField(F.SpecialType(SpecialType.System_Int32), Me.Method, GeneratedNames.MakeIteratorInitialThreadIdName(), Accessibility.Public),
                Nothing)

        End Sub

        Protected Overrides Sub GenerateMethodImplementations()
            Dim managedThreadId As BoundExpression = Nothing  ' Thread.CurrentThread.ManagedThreadId

            ' Add bool IEnumerator.MoveNext() and void IDisposable.Dispose()
            Dim disposeMethod = Me.OpenMethodImplementation(SpecialMember.System_IDisposable__Dispose,
                                                             "Dispose",
                                                             Accessibility.Private,
                                                             hasMethodBodyDependency:=True)

            Dim moveNextMethod = Me.OpenMoveNextMethodImplementation(SpecialMember.System_Collections_IEnumerator__MoveNext,
                                                                     Accessibility.Private)

            GenerateMoveNextAndDispose(moveNextMethod, disposeMethod)
            F.CurrentMethod = moveNextMethod

            If _isEnumerable Then
                ' generate the code for GetEnumerator()
                '    IEnumerable<elementType> result;
                '    if (this.initialThreadId == Thread.CurrentThread.ManagedThreadId && this.state == -2)
                '    {
                '        this.state = 0;
                '        result = this;
                '    }
                '    else
                '    {
                '        result = new Ints0_Impl(0);
                '    }
                '    result.parameter = this.parameterProxy; ' copy all of the parameter proxies

                ' Add IEnumerator<int> IEnumerable<int>.GetEnumerator()
                Dim getEnumeratorGeneric = Me.OpenMethodImplementation(F.SpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(_elementType),
                                                            SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator,
                                                            "GetEnumerator",
                                                            Accessibility.Private,
                                                            hasMethodBodyDependency:=False)

                Dim bodyBuilder = ArrayBuilder(Of BoundStatement).GetInstance()
                Dim resultVariable = F.SynthesizedLocal(StateMachineType)      ' iteratorClass result;

                Dim currentManagedThreadIdMethod As MethodSymbol = Nothing

                Dim currentManagedThreadIdProperty As PropertySymbol = F.WellKnownMember(Of PropertySymbol)(WellKnownMember.System_Environment__CurrentManagedThreadId, isOptional:=True)

                If (currentManagedThreadIdProperty IsNot Nothing) Then
                    currentManagedThreadIdMethod = currentManagedThreadIdProperty.GetMethod()
                End If

                If (currentManagedThreadIdMethod IsNot Nothing) Then
                    managedThreadId = F.Call(Nothing, currentManagedThreadIdMethod)
                Else
                    managedThreadId = F.Property(F.Property(WellKnownMember.System_Threading_Thread__CurrentThread), WellKnownMember.System_Threading_Thread__ManagedThreadId)
                End If

                ' if (this.state == -2 && this.initialThreadId == Thread.CurrentThread.ManagedThreadId)
                '    this.state = 0;
                '    result = this;
                '    goto thisInitialized
                ' else
                '    result = new IteratorClass(0)
                '    ' initialize [Me] if needed
                '    thisInitialized:
                '    ' initialize other fields
                Dim thisInitialized = F.GenerateLabel("thisInitialized")
                bodyBuilder.Add(
                    F.If(
                    condition:=
                        F.LogicalAndAlso(
                            F.IntEqual(F.Field(F.Me, StateField, False), F.Literal(StateMachineState.InitialEnumerableState)),
                            F.IntEqual(F.Field(F.Me, _initialThreadIdField, False), managedThreadId)),
                    thenClause:=
                        F.Block(
                            F.Assignment(F.Field(F.Me, StateField, True), F.Literal(StateMachineState.FirstUnusedState)),
                            F.Assignment(F.Local(resultVariable, True), F.Me),
                            If(Method.IsShared OrElse Method.MeParameter.Type.IsReferenceType,
                                    F.Goto(thisInitialized),
                                    DirectCast(F.StatementList(), BoundStatement))
                        ),
                    elseClause:=
                        F.Assignment(F.Local(resultVariable, True), F.[New](StateMachineType.Constructor, F.Literal(StateMachineState.FirstUnusedState)))
                    ))

                ' Initialize all the parameter copies
                Dim copySrc = InitialParameters
                Dim copyDest = nonReusableLocalProxies
                If Not Method.IsShared Then
                    ' starting with "this"
                    Dim proxy As FieldSymbol = Nothing
                    If (copyDest.TryGetValue(Method.MeParameter, proxy)) Then
                        bodyBuilder.Add(
                            F.Assignment(
                                F.Field(F.Local(resultVariable, True), proxy.AsMember(StateMachineType), True),
                                F.Field(F.Me, copySrc(Method.MeParameter).AsMember(F.CurrentType), False)))
                    End If
                End If

                bodyBuilder.Add(F.Label(thisInitialized))

                For Each parameter In Method.Parameters
                    Dim proxy As FieldSymbol = Nothing
                    If (copyDest.TryGetValue(parameter, proxy)) Then
                        bodyBuilder.Add(
                            F.Assignment(
                                F.Field(F.Local(resultVariable, True), proxy.AsMember(StateMachineType), True),
                                F.Field(F.Me, copySrc(parameter).AsMember(F.CurrentType), False)))
                    End If
                Next

                bodyBuilder.Add(F.Return(F.Local(resultVariable, False)))
                F.CloseMethod(F.Block(ImmutableArray.Create(resultVariable), bodyBuilder.ToImmutableAndFree()))

                ' Generate IEnumerable.GetEnumerator
                ' NOTE: this is a private implementing method. Its name is irrelevant
                '       but must be different from another GetEnumerator. Dev11 uses GetEnumerator0 here.
                '       IEnumerable.GetEnumerator seems better -
                '       it is clear why we have the property, and "Current" suffix will be shared in metadata with another Current.
                '       It is also consistent with the naming of IEnumerable.Current (see below).
                Me.OpenMethodImplementation(SpecialMember.System_Collections_IEnumerable__GetEnumerator,
                                            "IEnumerable.GetEnumerator",
                                            Accessibility.Private,
                                            hasMethodBodyDependency:=False)
                F.CloseMethod(F.Return(F.Call(F.Me, getEnumeratorGeneric)))
            End If

            ' Add T IEnumerator<T>.Current
            Me.OpenPropertyImplementation(F.SpecialType(SpecialType.System_Collections_Generic_IEnumerator_T).Construct(_elementType),
                                          SpecialMember.System_Collections_Generic_IEnumerator_T__Current,
                                          "Current",
                                          Accessibility.Private)
            F.CloseMethod(F.Return(F.Field(F.Me, _currentField, False)))

            ' Add void IEnumerator.Reset()
            Me.OpenMethodImplementation(SpecialMember.System_Collections_IEnumerator__Reset,
                                        "Reset",
                                        Accessibility.Private,
                                        hasMethodBodyDependency:=False)
            F.CloseMethod(F.Throw(F.[New](F.WellKnownType(WellKnownType.System_NotSupportedException))))

            ' Add object IEnumerator.Current
            ' NOTE: this is a private implementing property. Its name is irrelevant
            '       but must be different from another Current.
            '       Dev11 uses fully qualified and substituted name here (System.Collections.Generic.IEnumerator(Of Object).Current), 
            '       It may be an overkill and may lead to metadata bloat. 
            '       IEnumerable.Current seems better -
            '       it is clear why we have the property, and "Current" suffix will be shared in metadata with another Current.
            Me.OpenPropertyImplementation(SpecialMember.System_Collections_IEnumerator__Current,
                                          "IEnumerator.Current",
                                          Accessibility.Private)
            F.CloseMethod(F.Return(F.Field(F.Me, _currentField, False)))

            ' Add a body for the constructor
            If True Then
                F.CurrentMethod = StateMachineType.Constructor
                Dim bodyBuilder = ArrayBuilder(Of BoundStatement).GetInstance()
                bodyBuilder.Add(F.BaseInitialization())
                bodyBuilder.Add(F.Assignment(F.Field(F.Me, StateField, True), F.Parameter(F.CurrentMethod.Parameters(0)).MakeRValue))    ' this.state = state

                If _isEnumerable Then
                    ' this.initialThreadId = Thread.CurrentThread.ManagedThreadId;
                    bodyBuilder.Add(F.Assignment(F.Field(F.Me, _initialThreadIdField, True), managedThreadId))
                End If

                bodyBuilder.Add(F.Return())
                F.CloseMethod(F.Block(bodyBuilder.ToImmutableAndFree()))
                bodyBuilder = Nothing
            End If

        End Sub

        Protected Overrides Function GenerateStateMachineCreation(stateMachineVariable As LocalSymbol, frameType As NamedTypeSymbol) As BoundStatement
            Return F.Return(F.Local(stateMachineVariable, False))
        End Function

        Protected Overrides Sub InitializeStateMachine(bodyBuilder As ArrayBuilder(Of BoundStatement), frameType As NamedTypeSymbol, stateMachineLocal As LocalSymbol)
            ' Dim stateMachineLocal As new IteratorImplementationClass(N)
            ' where N is either 0 (if we're producing an enumerator) or -2 (if we're producing an enumerable)
            Dim initialState = If(_isEnumerable, StateMachineState.InitialEnumerableState, StateMachineState.FirstUnusedState)
            bodyBuilder.Add(
                F.Assignment(
                    F.Local(stateMachineLocal, True),
                    F.[New](StateMachineType.Constructor.AsMember(frameType), F.Literal(initialState))))
        End Sub

        Protected Overrides ReadOnly Property PreserveInitialParameterValues As Boolean
            Get
                Return Me._isEnumerable
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeMap As TypeSubstitution
            Get
                Return StateMachineType.TypeSubstitution
            End Get
        End Property

        Private Sub GenerateMoveNextAndDispose(moveNextMethod As SynthesizedMethod, disposeMethod As SynthesizedMethod)
            Dim rewriter = New IteratorMethodToClassRewriter(
                F:=F,
                state:=StateField,
                current:=_currentField,
                hoistedVariables:=hoistedVariables,
                localProxies:=nonReusableLocalProxies,
                StateDebugInfoBuilder,
                slotAllocatorOpt:=SlotAllocatorOpt,
                diagnostics:=Diagnostics)

            rewriter.GenerateMoveNextAndDispose(Body, moveNextMethod, disposeMethod)
        End Sub

        Protected Overrides Function CreateByValLocalCapture(field As FieldSymbol, local As LocalSymbol) As FieldSymbol
            Return field
        End Function

        Protected Overrides Function CreateParameterCapture(field As FieldSymbol, parameter As ParameterSymbol) As FieldSymbol
            Return field
        End Function

        Protected Overrides Sub InitializeParameterWithProxy(parameter As ParameterSymbol, proxy As FieldSymbol, stateMachineVariable As LocalSymbol, initializers As ArrayBuilder(Of BoundExpression))
            Debug.Assert(proxy IsNot Nothing)

            Dim frameType As NamedTypeSymbol = If(Me.Method.IsGenericMethod,
                                                  Me.StateMachineType.Construct(Me.Method.TypeArguments),
                                                  Me.StateMachineType)

            Dim expression As BoundExpression = If(parameter.IsMe,
                                                   DirectCast(Me.F.Me, BoundExpression),
                                                   Me.F.Parameter(parameter).MakeRValue())
            initializers.Add(
                Me.F.AssignmentExpression(
                    Me.F.Field(
                        Me.F.Local(stateMachineVariable, True),
                        proxy.AsMember(frameType),
                        True),
                    expression))
        End Sub
    End Class
End Namespace

