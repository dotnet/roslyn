' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of CapturedSymbolOrExpression)

        Private ReadOnly _binder As Binder
        Private ReadOnly _lookupOptions As LookupOptions
        Private ReadOnly _asyncMethodKind As AsyncMethodKind
        Private ReadOnly _builderType As NamedTypeSymbol
        Private ReadOnly _resultType As TypeSymbol

        Private _builderField As FieldSymbol
        Private _lastExpressionCaptureNumber As Integer

        Public Sub New(body As BoundStatement,
                       method As MethodSymbol,
                       stateMachineType As AsyncStateMachine,
                       slotAllocatorOpt As VariableSlotAllocator,
                       asyncKind As AsyncMethodKind,
                       compilationState As TypeCompilationState,
                       diagnostics As DiagnosticBag)

            MyBase.New(body, method, stateMachineType, slotAllocatorOpt, compilationState, diagnostics)

            Me._binder = CreateMethodBinder(method)
            Me._lookupOptions = LookupOptions.AllMethodsOfAnyArity Or LookupOptions.IgnoreExtensionMethods Or LookupOptions.NoBaseClassLookup

            If compilationState.ModuleBuilderOpt.IgnoreAccessibility Then
                Me._binder = New IgnoreAccessibilityBinder(Me._binder)
                Me._lookupOptions = Me._lookupOptions Or LookupOptions.IgnoreAccessibility
            End If

            Debug.Assert(asyncKind <> AsyncMethodKind.None)
            Debug.Assert(asyncKind = GetAsyncMethodKind(method))
            Me._asyncMethodKind = asyncKind

            Select Case Me._asyncMethodKind
                Case AsyncMethodKind.Sub
                    Me._resultType = Me.F.SpecialType(SpecialType.System_Void)
                    Me._builderType = Me.F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder)

                Case AsyncMethodKind.TaskFunction
                    Me._resultType = Me.F.SpecialType(SpecialType.System_Void)
                    Me._builderType = Me.F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder)

                Case AsyncMethodKind.GenericTaskFunction
                    Me._resultType = DirectCast(Me.Method.ReturnType, NamedTypeSymbol).TypeArgumentsNoUseSiteDiagnostics().Single().InternalSubstituteTypeParameters(Me.TypeMap).Type
                    Me._builderType = Me.F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T).Construct(Me._resultType)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(Me._asyncMethodKind)
            End Select
        End Sub

        ''' <summary>
        ''' Rewrite an async method into a state machine class.
        ''' </summary>
        Friend Overloads Shared Function Rewrite(body As BoundBlock,
                                                 method As MethodSymbol,
                                                 methodOrdinal As Integer,
                                                 slotAllocatorOpt As VariableSlotAllocator,
                                                 compilationState As TypeCompilationState,
                                                 diagnostics As DiagnosticBag,
                                                 <Out> ByRef stateMachineType As AsyncStateMachine) As BoundBlock

            If body.HasErrors Then
                Return body
            End If

            Dim asyncMethodKind As AsyncMethodKind = GetAsyncMethodKind(method)
            If asyncMethodKind = AsyncMethodKind.None Then
                Return body
            End If

            ' The CLR doesn't support adding fields to structs, so in order to enable EnC in an async method we need to generate a class.
            Dim kind = If(compilationState.Compilation.Options.EnableEditAndContinue, TypeKind.Class, TypeKind.Struct)

            stateMachineType = New AsyncStateMachine(slotAllocatorOpt, compilationState, method, methodOrdinal, kind)

            compilationState.ModuleBuilderOpt.CompilationState.SetStateMachineType(method, stateMachineType)

            Dim rewriter As New AsyncRewriter(body, method, stateMachineType, slotAllocatorOpt, asyncMethodKind, compilationState, diagnostics)

            ' check if we have all the types we need
            If rewriter.EnsureAllSymbolsAndSignature() Then
                Return body
            End If

            Return rewriter.Rewrite()
        End Function

        Private Shared Function CreateMethodBinder(method As MethodSymbol) As Binder
            ' For source method symbol create a binder
            Dim sourceMethod = TryCast(method, SourceMethodSymbol)
            If sourceMethod IsNot Nothing Then
                Return BinderBuilder.CreateBinderForMethodBody(DirectCast(sourceMethod.ContainingModule, SourceModuleSymbol),
                                                               sourceMethod.ContainingType.Locations(0).PossiblyEmbeddedOrMySourceTree(),
                                                               sourceMethod)
            End If

            ' For all other symbols we assume that it should be a synthesized method 
            ' placed inside source named type or in one of its nested types
            Debug.Assert((TypeOf method Is SynthesizedLambdaMethod) OrElse (TypeOf method Is SynthesizedInteractiveInitializerMethod))
            Dim containingType As NamedTypeSymbol = method.ContainingType
            While containingType IsNot Nothing
                Dim syntaxTree = containingType.Locations.FirstOrDefault()?.SourceTree
                If syntaxTree IsNot Nothing Then
                    Return BinderBuilder.CreateBinderForType(
                        DirectCast(containingType.ContainingModule, SourceModuleSymbol),
                        syntaxTree,
                        containingType)
                End If
                containingType = containingType.ContainingType
            End While

            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Sub GenerateControlFields()
            ' The fields are initialized from async method, so they need to be public:
            Me.StateField = Me.F.StateMachineField(Me.F.SpecialType(SpecialType.System_Int32), Me.Method, GeneratedNames.MakeStateMachineStateFieldName(), Accessibility.Public)
            Me._builderField = Me.F.StateMachineField(Me._builderType, Me.Method, GeneratedNames.MakeStateMachineBuilderFieldName(), Accessibility.Public)
        End Sub

        Protected Overrides Sub InitializeStateMachine(bodyBuilder As ArrayBuilder(Of BoundStatement), frameType As NamedTypeSymbol, stateMachineLocal As LocalSymbol)
            If frameType.TypeKind = TypeKind.Class Then
                ' Dim stateMachineLocal = new AsyncImplementationClass()
                bodyBuilder.Add(
                    Me.F.Assignment(
                        Me.F.Local(stateMachineLocal, True),
                        Me.F.[New](StateMachineType.Constructor.AsMember(frameType))))
            Else
                ' STAT:   localStateMachine = Nothing ' Initialization
                bodyBuilder.Add(
                    Me.F.Assignment(
                        Me.F.Local(stateMachineLocal, True),
                        Me.F.Null(stateMachineLocal.Type)))
            End If
        End Sub

        Protected Overrides ReadOnly Property PreserveInitialParameterValues As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeMap As TypeSubstitution
            Get
                Return Me.StateMachineType.TypeSubstitution
            End Get
        End Property

        Protected Overrides Sub GenerateMethodImplementations()
            ' Add IAsyncStateMachine.MoveNext()
            GenerateMoveNext(
                Me.OpenMoveNextMethodImplementation(
                    WellKnownMember.System_Runtime_CompilerServices_IAsyncStateMachine_MoveNext,
                    Accessibility.Friend))

            'Add IAsyncStateMachine.SetStateMachine()
            Me.OpenMethodImplementation(
                    WellKnownMember.System_Runtime_CompilerServices_IAsyncStateMachine_SetStateMachine,
                    "System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine",
                    Accessibility.Private,
                    hasMethodBodyDependency:=False)

            ' SetStateMachine is used to initialize the underlying AsyncMethodBuilder's reference to the boxed copy of the state machine.
            ' If the state machine is a class there is no copy made and thus the initialization is not necessary. 
            ' In fact it is an error to reinitialize the builder since it already is initialized.
            If Me.F.CurrentType.TypeKind = TypeKind.Class Then
                Me.F.CloseMethod(F.Return())
            Else
                Me.CloseMethod(
                Me.F.Block(
                    Me.F.ExpressionStatement(
                        Me.GenerateMethodCall(
                            Me.F.Field(Me.F.Me(), Me._builderField, False),
                            Me._builderType,
                            "SetStateMachine",
                            {Me.F.Parameter(Me.F.CurrentMethod.Parameters(0))})),
                    Me.F.Return()))
            End If

            ' Constructor
            If StateMachineType.TypeKind = TypeKind.Class Then
                Me.F.CurrentMethod = StateMachineType.Constructor
                Me.F.CloseMethod(F.Block(ImmutableArray.Create(F.BaseInitialization(), F.Return())))
            End If

        End Sub

        Protected Overrides Function GenerateStateMachineCreation(stateMachineVariable As LocalSymbol, frameType As NamedTypeSymbol) As BoundStatement
            Dim bodyBuilder = ArrayBuilder(Of BoundStatement).GetInstance()

            ' STAT:   localStateMachine.$stateField = NotStartedStateMachine
            Dim stateFieldAsLValue As BoundExpression =
                Me.F.Field(
                    Me.F.Local(stateMachineVariable, True),
                    Me.StateField.AsMember(frameType), True)

            bodyBuilder.Add(
                Me.F.Assignment(
                    stateFieldAsLValue,
                    Me.F.Literal(StateMachineStates.NotStartedStateMachine)))

            ' STAT:   localStateMachine.$builder = System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of typeArgs).Create()
            Dim constructedBuilderField As FieldSymbol = Me._builderField.AsMember(frameType)
            Dim builderFieldAsLValue As BoundExpression = Me.F.Field(Me.F.Local(stateMachineVariable, True), constructedBuilderField, True)

            ' All properties and methods will be searched in this type
            Dim builderType As TypeSymbol = constructedBuilderField.Type

            bodyBuilder.Add(
                Me.F.Assignment(
                    builderFieldAsLValue,
                    Me.GenerateMethodCall(Nothing, builderType, "Create")))

            ' STAT:   localStateMachine.$builder.Start(ref localStateMachine) -- binding to the method AsyncTaskMethodBuilder<typeArgs>.Start(...)
            bodyBuilder.Add(
                Me.F.ExpressionStatement(
                    Me.GenerateMethodCall(
                        builderFieldAsLValue,
                        builderType,
                        "Start",
                        ImmutableArray.Create(Of TypeSymbol)(frameType),
                        Me.F.Local(stateMachineVariable, True))))

            ' STAT:   Return
            '  or
            ' STAT:   Return localStateMachine.$builder.Task
            bodyBuilder.Add(
                If(Me._asyncMethodKind = AsyncMethodKind.[Sub],
                   Me.F.Return(),
                   Me.F.Return(Me.GeneratePropertyGet(builderFieldAsLValue, builderType, "Task"))))

            Return RewriteBodyIfNeeded(Me.F.Block(ImmutableArray(Of LocalSymbol).Empty, bodyBuilder.ToImmutableAndFree()), Me.F.TopLevelMethod, Me.Method)
        End Function

        Private Sub GenerateMoveNext(moveNextMethod As MethodSymbol)
            Dim rewriter = New AsyncMethodToClassRewriter(method:=Me.Method,
                                                          F:=Me.F,
                                                          state:=Me.StateField,
                                                          builder:=Me._builderField,
                                                          hoistedVariables:=Me.hoistedVariables,
                                                          nonReusableLocalProxies:=Me.nonReusableLocalProxies,
                                                          synthesizedLocalOrdinals:=Me.SynthesizedLocalOrdinals,
                                                          slotAllocatorOpt:=Me.SlotAllocatorOpt,
                                                          nextFreeHoistedLocalSlot:=Me.nextFreeHoistedLocalSlot,
                                                          owner:=Me,
                                                          diagnostics:=Diagnostics)

            rewriter.GenerateMoveNext(Me.Body, moveNextMethod)
        End Sub

        Friend Overrides Function RewriteBodyIfNeeded(body As BoundStatement, topMethod As MethodSymbol, currentMethod As MethodSymbol) As BoundStatement
            If body.HasErrors Then
                Return body
            End If

            Dim rewrittenNodes As HashSet(Of BoundNode) = Nothing
            Dim hasLambdas As Boolean = False
            Dim symbolsCapturedWithoutCtor As ISet(Of Symbol) = Nothing

            If body.Kind <> BoundKind.Block Then
                body = Me.F.Block(body)
            End If

            Const rewritingFlags As LocalRewriter.RewritingFlags =
                LocalRewriter.RewritingFlags.AllowSequencePoints Or
                LocalRewriter.RewritingFlags.AllowEndOfMethodReturnWithExpression Or
                LocalRewriter.RewritingFlags.AllowCatchWithErrorLineNumberReference

            Return LocalRewriter.Rewrite(DirectCast(body, BoundBlock),
                                         topMethod,
                                         F.CompilationState,
                                         previousSubmissionFields:=Nothing,
                                         diagnostics:=Me.Diagnostics,
                                         rewrittenNodes:=rewrittenNodes,
                                         hasLambdas:=hasLambdas,
                                         symbolsCapturedWithoutCopyCtor:=symbolsCapturedWithoutCtor,
                                         flags:=rewritingFlags,
                                         currentMethod:=currentMethod)
        End Function

        Friend Overrides Function EnsureAllSymbolsAndSignature() As Boolean
            Dim hasErrors As Boolean = MyBase.EnsureAllSymbolsAndSignature

            EnsureSpecialType(SpecialType.System_Object, hasErrors)
            EnsureSpecialType(SpecialType.System_Void, hasErrors)
            EnsureSpecialType(SpecialType.System_ValueType, hasErrors)

            EnsureWellKnownType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine, hasErrors)
            EnsureWellKnownMember(Of MethodSymbol)(WellKnownMember.System_Runtime_CompilerServices_IAsyncStateMachine_MoveNext, hasErrors)

            ' We don't ensure those types because we don't know if they will be actually used, 
            ' use-site errors will be reported later if any of those types is actually requested
            '
            ' EnsureSpecialType(SpecialType.System_Boolean, hasErrors)
            ' EnsureWellKnownType(WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion, hasErrors)
            ' EnsureWellKnownType(WellKnownType.System_Runtime_CompilerServices_INotifyCompletion, hasErrors)

            ' NOTE: We don't ensure DebuggerStepThroughAttribute, it is just not emitted if not found
            ' EnsureWellKnownMember(Of MethodSymbol)(WellKnownMember.System_Diagnostics_DebuggerStepThroughAttribute__ctor, hasErrors)

            Select Case Me._asyncMethodKind
                Case AsyncMethodKind.GenericTaskFunction
                    EnsureWellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T, hasErrors)
                Case AsyncMethodKind.TaskFunction
                    EnsureWellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder, hasErrors)
                Case AsyncMethodKind.[Sub]
                    EnsureWellKnownType(WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder, hasErrors)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(Me._asyncMethodKind)
            End Select

            Return hasErrors
        End Function

        ''' <summary>
        ''' Specifies a kind of an Async method
        ''' 
        ''' None is returned for non-Async methods or methods with wrong return type
        ''' </summary>
        Friend Enum AsyncMethodKind
            None
            [Sub]
            TaskFunction
            GenericTaskFunction
        End Enum

        ''' <summary>
        ''' Returns method's async kind
        ''' </summary>
        Friend Shared Function GetAsyncMethodKind(method As MethodSymbol) As AsyncMethodKind
            Debug.Assert(Not method.IsPartial)
            If Not method.IsAsync Then
                Return AsyncMethodKind.None
            End If

            If method.IsSub Then
                Return AsyncMethodKind.[Sub]
            End If

            Dim compilation As VisualBasicCompilation = method.DeclaringCompilation
            Debug.Assert(compilation IsNot Nothing)

            Dim returnType As TypeSymbol = method.ReturnType
            If returnType = compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task) Then
                Return AsyncMethodKind.TaskFunction
            End If

            If returnType.Kind = SymbolKind.NamedType AndAlso
                    returnType.OriginalDefinition = compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T) Then
                Return AsyncMethodKind.GenericTaskFunction
            End If

            ' Wrong return type
            Return AsyncMethodKind.None
        End Function

        Protected Overrides Function CreateByRefLocalCapture(typeMap As TypeSubstitution,
                                                             local As LocalSymbol,
                                                             initializers As Dictionary(Of LocalSymbol, BoundExpression)) As CapturedSymbolOrExpression

            Debug.Assert(local.IsByRef)
            Return CaptureExpression(typeMap, initializers(local), initializers)
        End Function

        Private Function CaptureExpression(typeMap As TypeSubstitution,
                                           expression As BoundExpression,
                                           initializers As Dictionary(Of LocalSymbol, BoundExpression)) As CapturedSymbolOrExpression

            If expression Is Nothing Then
                Return Nothing
            End If

            Select Case expression.Kind
                Case BoundKind.Literal
                    Debug.Assert(Not expression.IsLValue)
                    ' TODO: Do we want to extend support of this constant 
                    '       folding to non-literal expressions?
                    Return New CapturedConstantExpression(expression.ConstantValueOpt,
                                                          expression.Type.InternalSubstituteTypeParameters(typeMap).Type)

                Case BoundKind.Local
                    Return CaptureLocalSymbol(typeMap, DirectCast(expression, BoundLocal).LocalSymbol, initializers)

                Case BoundKind.Parameter
                    Return CaptureParameterSymbol(typeMap, DirectCast(expression, BoundParameter).ParameterSymbol)

                Case BoundKind.MeReference
                    Return CaptureParameterSymbol(typeMap, Me.Method.MeParameter)

                Case BoundKind.MyClassReference
                    Return CaptureParameterSymbol(typeMap, Me.Method.MeParameter)

                Case BoundKind.MyBaseReference
                    Return CaptureParameterSymbol(typeMap, Me.Method.MeParameter)

                Case BoundKind.FieldAccess
                    If Not expression.IsLValue Then
                        GoTo lCaptureRValue
                    End If

                    Dim fieldAccess = DirectCast(expression, BoundFieldAccess)
                    Return New CapturedFieldAccessExpression(CaptureExpression(typeMap, fieldAccess.ReceiverOpt, initializers), fieldAccess.FieldSymbol)

                Case BoundKind.ArrayAccess
                    If Not expression.IsLValue Then
                        GoTo lCaptureRValue
                    End If

                    Dim arrayAccess = DirectCast(expression, BoundArrayAccess)

                    Dim capturedArrayPointer As CapturedSymbolOrExpression =
                        CaptureExpression(typeMap, arrayAccess.Expression, initializers)

                    Dim indices As ImmutableArray(Of BoundExpression) = arrayAccess.Indices
                    Dim indicesCount As Integer = indices.Length

                    Dim capturedIndices(indicesCount - 1) As CapturedSymbolOrExpression
                    For i = 0 To indicesCount - 1
                        capturedIndices(i) = CaptureExpression(typeMap, indices(i), initializers)
                    Next

                    Return New CapturedArrayAccessExpression(capturedArrayPointer, capturedIndices.AsImmutableOrNull)

                Case Else
lCaptureRValue:
                    Debug.Assert(Not expression.IsLValue, "Need to support LValues of type " + expression.GetType.Name)
                    Me._lastExpressionCaptureNumber += 1
                    Return New CapturedRValueExpression(
                                    Me.F.StateMachineField(
                                        expression.Type,
                                        Me.Method,
                                        StringConstants.StateMachineExpressionCapturePrefix & Me._lastExpressionCaptureNumber,
                                        Accessibility.Friend),
                                    expression)
            End Select

            Throw ExceptionUtilities.UnexpectedValue(expression.Kind)
        End Function

        Protected Overrides Sub InitializeParameterWithProxy(parameter As ParameterSymbol, proxy As CapturedSymbolOrExpression, stateMachineVariable As LocalSymbol, initializers As ArrayBuilder(Of BoundExpression))
            Debug.Assert(TypeOf proxy Is CapturedParameterSymbol)
            Dim field As FieldSymbol = DirectCast(proxy, CapturedParameterSymbol).Field

            Dim frameType As NamedTypeSymbol = If(Me.Method.IsGenericMethod,
                                                  Me.StateMachineType.Construct(Me.Method.TypeArguments),
                                                  Me.StateMachineType)

            Dim expression As BoundExpression = If(parameter.IsMe,
                                                   DirectCast(Me.F.[Me](), BoundExpression),
                                                   Me.F.Parameter(parameter).MakeRValue())
            initializers.Add(
                Me.F.AssignmentExpression(
                    Me.F.Field(
                        Me.F.Local(stateMachineVariable, True),
                        field.AsMember(frameType),
                        True),
                    expression))
        End Sub

        Protected Overrides Function CreateByValLocalCapture(field As FieldSymbol, local As LocalSymbol) As CapturedSymbolOrExpression
            Return New CapturedLocalSymbol(field, local)
        End Function

        Protected Overrides Function CreateParameterCapture(field As FieldSymbol, parameter As ParameterSymbol) As CapturedSymbolOrExpression
            Return New CapturedParameterSymbol(field)
        End Function

#Region "Method call and property access generation"

        Private Function GenerateMethodCall(receiver As BoundExpression,
                                            type As TypeSymbol,
                                            methodName As String,
                                            ParamArray arguments As BoundExpression()) As BoundExpression

            Return GenerateMethodCall(receiver, type, methodName, ImmutableArray(Of TypeSymbol).Empty, arguments)
        End Function

        Private Function GenerateMethodCall(receiver As BoundExpression,
                                            type As TypeSymbol,
                                            methodName As String,
                                            typeArgs As ImmutableArray(Of TypeSymbol),
                                            ParamArray arguments As BoundExpression()) As BoundExpression

            ' Get the method group
            Dim methodGroup = FindMethodAndReturnMethodGroup(receiver, type, methodName, typeArgs)

            If methodGroup Is Nothing OrElse
                arguments.Any(Function(a) a.HasErrors) OrElse
                (receiver IsNot Nothing AndAlso receiver.HasErrors) Then
                Return Me.F.BadExpression(arguments)
            End If

            ' Do overload resolution and bind an invocation of the method.
            Dim result = _binder.BindInvocationExpression(Me.F.Syntax,
                                                          Me.F.Syntax,
                                                          TypeCharacter.None,
                                                          methodGroup,
                                                          ImmutableArray.Create(Of BoundExpression)(arguments),
                                                          argumentNames:=Nothing,
                                                          diagnostics:=Me.Diagnostics,
                                                          callerInfoOpt:=Nothing)
            Return result
        End Function

        Private Function FindMethodAndReturnMethodGroup(receiver As BoundExpression,
                                                        type As TypeSymbol,
                                                        methodName As String,
                                                        typeArgs As ImmutableArray(Of TypeSymbol)) As BoundMethodGroup

            Dim group As BoundMethodGroup = Nothing
            Dim result = LookupResult.GetInstance()

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Me._binder.LookupMember(result, type, methodName, arity:=0, options:=_lookupOptions, useSiteDiagnostics:=useSiteDiagnostics)
            Me.Diagnostics.Add(Me.F.Syntax, useSiteDiagnostics)

            If result.IsGood Then
                Debug.Assert(result.Symbols.Count > 0)
                Dim symbol0 = result.Symbols(0)
                If result.Symbols(0).Kind = SymbolKind.Method Then
                    group = New BoundMethodGroup(Me.F.Syntax,
                                                 Me.F.TypeArguments(typeArgs),
                                                 result.Symbols.ToDowncastedImmutable(Of MethodSymbol),
                                                 result.Kind,
                                                 receiver,
                                                 QualificationKind.QualifiedViaValue)
                End If
            End If

            If group Is Nothing Then
                Me.Diagnostics.Add(If(result.HasDiagnostic,
                                      result.Diagnostic,
                                      ErrorFactory.ErrorInfo(ERRID.ERR_NameNotMember2, methodName, type)),
                                   Me.F.Syntax.GetLocation())
            End If

            result.Free()
            Return group
        End Function

        Private Function GeneratePropertyGet(receiver As BoundExpression, type As TypeSymbol, propertyName As String) As BoundExpression
            ' Get the property group
            Dim propertyGroup = FindPropertyAndReturnPropertyGroup(receiver, type, propertyName)

            If propertyGroup Is Nothing OrElse (receiver IsNot Nothing AndAlso receiver.HasErrors) Then
                Return Me.F.BadExpression()
            End If

            ' Do overload resolution and bind an invocation of the property.
            Dim result = _binder.BindInvocationExpression(Me.F.Syntax,
                                                          Me.F.Syntax,
                                                          TypeCharacter.None,
                                                          propertyGroup,
                                                          Nothing,
                                                          argumentNames:=Nothing,
                                                          diagnostics:=Me.Diagnostics,
                                                          callerInfoOpt:=Nothing)

            ' reclassify property access as Get
            If result.Kind = BoundKind.PropertyAccess Then
                result = DirectCast(result, BoundPropertyAccess).SetAccessKind(PropertyAccessKind.Get)
            End If

            Debug.Assert(Not result.IsLValue)
            Return result
        End Function

        Private Function FindPropertyAndReturnPropertyGroup(receiver As BoundExpression, type As TypeSymbol, propertyName As String) As BoundPropertyGroup
            Dim group As BoundPropertyGroup = Nothing
            Dim result = LookupResult.GetInstance()

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Me._binder.LookupMember(result, type, propertyName, arity:=0, options:=_lookupOptions, useSiteDiagnostics:=useSiteDiagnostics)
            Me.Diagnostics.Add(Me.F.Syntax, useSiteDiagnostics)

            If result.IsGood Then
                Debug.Assert(result.Symbols.Count > 0)
                Dim symbol0 = result.Symbols(0)
                If result.Symbols(0).Kind = SymbolKind.Property Then
                    group = New BoundPropertyGroup(Me.F.Syntax,
                                                   result.Symbols.ToDowncastedImmutable(Of PropertySymbol),
                                                   result.Kind,
                                                   receiver,
                                                   QualificationKind.QualifiedViaValue)
                End If
            End If

            If group Is Nothing Then
                Me.Diagnostics.Add(If(result.HasDiagnostic,
                                      result.Diagnostic,
                                      ErrorFactory.ErrorInfo(ERRID.ERR_NameNotMember2, propertyName, type)),
                                   Me.F.Syntax.GetLocation())
            End If

            result.Free()
            Return group
        End Function

#End Region

    End Class

End Namespace
