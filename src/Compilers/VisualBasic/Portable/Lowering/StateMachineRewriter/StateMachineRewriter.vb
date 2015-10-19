' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' </summary>
    ''' <typeparam name="TProxy">
    ''' Type used by State Machine rewriter to represent symbol proxy. Lambda rewriter as 
    ''' well as iterator rewriter use simplified form of proxy as they only capture 
    ''' locals as r-values in fields, async rewriter uses a different structure as a proxy
    ''' because it has to capture l-values on stack as well
    ''' </typeparam>
    Partial Friend MustInherit Class StateMachineRewriter(Of TProxy)

        Protected ReadOnly Body As BoundStatement
        Protected ReadOnly Method As MethodSymbol
        Protected ReadOnly Diagnostics As DiagnosticBag
        Protected ReadOnly F As SyntheticBoundNodeFactory
        Protected ReadOnly StateMachineType As SynthesizedContainer
        Protected ReadOnly SlotAllocatorOpt As VariableSlotAllocator
        Protected ReadOnly SynthesizedLocalOrdinals As SynthesizedLocalOrdinalsDispenser

        Protected StateField As FieldSymbol
        Protected nonReusableLocalProxies As Dictionary(Of Symbol, TProxy)
        Protected nextFreeHoistedLocalSlot As Integer
        Protected hoistedVariables As IReadOnlySet(Of Symbol)
        Protected InitialParameters As Dictionary(Of Symbol, TProxy)

        Protected Sub New(body As BoundStatement,
                          method As MethodSymbol,
                          stateMachineType As StateMachineTypeSymbol,
                          slotAllocatorOpt As VariableSlotAllocator,
                          compilationState As TypeCompilationState,
                          diagnostics As DiagnosticBag)

            Debug.Assert(body IsNot Nothing)
            Debug.Assert(method IsNot Nothing)
            Debug.Assert(compilationState IsNot Nothing)
            Debug.Assert(diagnostics IsNot Nothing)
            Debug.Assert(stateMachineType IsNot Nothing)

            Me.Body = body
            Me.Method = method
            Me.StateMachineType = stateMachineType
            Me.SlotAllocatorOpt = slotAllocatorOpt
            Me.Diagnostics = diagnostics
            Me.SynthesizedLocalOrdinals = New SynthesizedLocalOrdinalsDispenser()
            Me.nonReusableLocalProxies = New Dictionary(Of Symbol, TProxy)()

            Me.F = New SyntheticBoundNodeFactory(method, method, method.ContainingType, body.Syntax, compilationState, diagnostics)
        End Sub

        ''' <summary>
        ''' True if the initial values of locals in the rewritten method need to be preserved. (e.g. enumerable iterator methods)
        ''' </summary>
        Protected MustOverride ReadOnly Property PreserveInitialParameterValues As Boolean

        ''' <summary>
        ''' Type substitution if applicable or Nothing
        ''' </summary>
        Friend MustOverride ReadOnly Property TypeMap As TypeSubstitution

        ''' <summary>
        ''' Add fields to the state machine class that control the state machine.
        ''' </summary>
        Protected MustOverride Sub GenerateControlFields()

        ''' <summary>
        ''' Initialize the state machine class.
        ''' </summary>
        Protected MustOverride Sub InitializeStateMachine(bodyBuilder As ArrayBuilder(Of BoundStatement), frameType As NamedTypeSymbol, stateMachineLocal As LocalSymbol)

        ''' <summary>
        ''' Generate implementation-specific state machine initialization for the kickoff method body.
        ''' </summary>
        Protected MustOverride Function GenerateStateMachineCreation(stateMachineVariable As LocalSymbol, frameType As NamedTypeSymbol) As BoundStatement

        ''' <summary>
        ''' Generate implementation-specific state machine member method implementations.
        ''' </summary>
        Protected MustOverride Sub GenerateMethodImplementations()

        Protected Function Rewrite() As BoundBlock
            Debug.Assert(Not Me.PreserveInitialParameterValues OrElse Method.IsIterator)

            Me.F.OpenNestedType(Me.StateMachineType)
            Me.F.CompilationState.StateMachineImplementationClass(Me.Method) = Me.StateMachineType

            Me.GenerateControlFields()

            ' and fields for the initial values of all the parameters of the method
            If Me.PreserveInitialParameterValues Then
                Me.InitialParameters = New Dictionary(Of Symbol, TProxy)()
            End If

            ' add fields for the captured variables of the method
            Dim variablesToHoist = IteratorAndAsyncCaptureWalker.Analyze(New FlowAnalysisInfo(F.CompilationState.Compilation, Me.Method, Me.Body), Me.Diagnostics)

            CreateNonReusableLocalProxies(variablesToHoist, Me.nextFreeHoistedLocalSlot)

            Me.hoistedVariables = New OrderedSet(Of Symbol)(variablesToHoist.CapturedLocals)

            GenerateMethodImplementations()

            ' Return a replacement body for the kickoff method
            Return GenerateKickoffMethodBody()
        End Function

        Private Function GenerateKickoffMethodBody() As BoundBlock
            Me.F.CurrentMethod = Me.Method
            Dim bodyBuilder = ArrayBuilder(Of BoundStatement).GetInstance()
            bodyBuilder.Add(Me.F.HiddenSequencePoint())

            Dim frameType As NamedTypeSymbol = If(Me.Method.IsGenericMethod, Me.StateMachineType.Construct(Method.TypeArguments), Me.StateMachineType)
            Dim stateMachineVariable As LocalSymbol = F.SynthesizedLocal(frameType)
            InitializeStateMachine(bodyBuilder, frameType, stateMachineVariable)

            ' Plus code to initialize all of the parameter proxies result
            Dim proxies = If(PreserveInitialParameterValues, Me.InitialParameters, Me.nonReusableLocalProxies)
            Dim initializers = ArrayBuilder(Of BoundExpression).GetInstance()

            ' starting with the "Me" proxy
            If Not Me.Method.IsShared AndAlso Me.Method.MeParameter IsNot Nothing Then
                Dim proxy As TProxy = Nothing
                If proxies.TryGetValue(Me.Method.MeParameter, proxy) Then
                    InitializeParameterWithProxy(Me.Method.MeParameter, proxy, stateMachineVariable, initializers)
                End If
            End If

            ' then all the parameters
            For Each parameter In Me.Method.Parameters
                Dim proxy As TProxy = Nothing
                If proxies.TryGetValue(parameter, proxy) Then
                    InitializeParameterWithProxy(parameter, proxy, stateMachineVariable, initializers)
                End If
            Next

            If initializers.Count > 0 Then
                bodyBuilder.Add(F.ExpressionStatement(F.Sequence(initializers.ToArray())))
            End If

            initializers.Free()

            bodyBuilder.Add(GenerateStateMachineCreation(stateMachineVariable, frameType))
            Return Me.F.Block(
                ImmutableArray.Create(Of LocalSymbol)(stateMachineVariable),
                bodyBuilder.ToImmutableAndFree())
        End Function

        Private Sub CreateNonReusableLocalProxies(captured As IteratorAndAsyncCaptureWalker.Result,
                                                  ByRef nextFreeHoistedLocalSlot As Integer)

            Dim typeMap As TypeSubstitution = StateMachineType.TypeSubstitution
            Dim isDebugBuild As Boolean = F.Compilation.Options.OptimizationLevel = OptimizationLevel.Debug
            Dim mapToPreviousFields = isDebugBuild AndAlso SlotAllocatorOpt IsNot Nothing
            Me.nextFreeHoistedLocalSlot = If(mapToPreviousFields, SlotAllocatorOpt.PreviousHoistedLocalSlotCount, 0)

            For Each variable In captured.CapturedLocals
                Debug.Assert(variable.Kind = SymbolKind.Local OrElse variable.Kind = SymbolKind.Parameter)

                Select Case variable.Kind
                    Case SymbolKind.Local
                        Dim local = DirectCast(variable, LocalSymbol)

                        ' No need to hoist constants
                        If local.IsConst Then
                            Continue For
                        End If

                        If local.SynthesizedKind = SynthesizedLocalKind.ConditionalBranchDiscriminator Then
                            Continue For
                        End If

                        CaptureLocalSymbol(typeMap, DirectCast(variable, LocalSymbol), captured.ByRefLocalsInitializers)

                    Case SymbolKind.Parameter
                        CaptureParameterSymbol(typeMap, DirectCast(variable, ParameterSymbol))
                End Select
            Next
        End Sub

        Protected Function CaptureParameterSymbol(typeMap As TypeSubstitution,
                                                  parameter As ParameterSymbol) As TProxy

            Dim proxy As TProxy = Nothing
            If Me.nonReusableLocalProxies.TryGetValue(parameter, proxy) Then
                ' This proxy may have already be added while processing 
                ' previous ByRef local
                Return proxy
            End If

            If parameter.IsMe Then
                Dim typeName As String = parameter.ContainingSymbol.ContainingType.Name
                Dim isMeOfClosureType As Boolean = typeName.StartsWith(StringConstants.DisplayClassPrefix, StringComparison.Ordinal)

                ' NOTE: even though 'Me' is 'ByRef' in structures, Dev11 does capture it by value
                ' NOTE: without generation of any errors/warnings. Roslyn has to match this behavior

                proxy = CreateParameterCapture(
                            Me.F.StateMachineField(
                                Method.ContainingType,
                                Me.Method,
                                If(isMeOfClosureType,
                                    GeneratedNames.MakeStateMachineCapturedClosureMeName(typeName),
                                    GeneratedNames.MakeStateMachineCapturedMeName()),
                                Accessibility.Friend),
                            parameter)
                Me.nonReusableLocalProxies.Add(parameter, proxy)

                If Me.PreserveInitialParameterValues Then
                    Dim initialMe As TProxy = If(Me.Method.ContainingType.IsStructureType(),
                                                 CreateParameterCapture(
                                                     Me.F.StateMachineField(
                                                         Me.Method.ContainingType,
                                                         Me.Method,
                                                         GeneratedNames.MakeIteratorParameterProxyName(GeneratedNames.MakeStateMachineCapturedMeName()),
                                                         Accessibility.Friend),
                                                     parameter),
                                                 Me.nonReusableLocalProxies(parameter))

                    Me.InitialParameters.Add(parameter, initialMe)
                End If

            Else
                Dim paramType As TypeSymbol = parameter.Type.InternalSubstituteTypeParameters(typeMap).Type

                Debug.Assert(Not parameter.IsByRef)
                proxy = CreateParameterCapture(
                            F.StateMachineField(
                                paramType,
                                Me.Method,
                                GeneratedNames.MakeStateMachineParameterName(parameter.Name),
                                Accessibility.Friend),
                            parameter)
                Me.nonReusableLocalProxies.Add(parameter, proxy)

                If Me.PreserveInitialParameterValues Then
                    Me.InitialParameters.Add(parameter,
                                             CreateParameterCapture(
                                                 Me.F.StateMachineField(
                                                     paramType,
                                                     Me.Method,
                                                     GeneratedNames.MakeIteratorParameterProxyName(parameter.Name),
                                                     Accessibility.Friend),
                                                 parameter))
                End If
            End If

            Return proxy
        End Function

        Protected Function CaptureLocalSymbol(typeMap As TypeSubstitution,
                                              local As LocalSymbol,
                                              initializers As Dictionary(Of LocalSymbol, BoundExpression)) As TProxy

            Dim proxy As TProxy = Nothing
            If nonReusableLocalProxies.TryGetValue(local, proxy) Then
                ' This proxy may have already be added while processing 
                ' previous ByRef local
                Return proxy
            End If

            If local.IsByRef Then
                ' We'll create proxies for these variable later:
                ' TODO: so, we have to check if it is already in or not. See the early impl.

                Debug.Assert(initializers.ContainsKey(local))
                proxy = CreateByRefLocalCapture(typeMap, local, initializers)
                nonReusableLocalProxies.Add(local, proxy)

                Return proxy
            End If

            ' Variable needs to be hoisted.
            Dim fieldType = local.Type.InternalSubstituteTypeParameters(typeMap).Type

            Dim id As LocalDebugId = LocalDebugId.None
            Dim slotIndex As Integer = -1

            If Not local.SynthesizedKind.IsSlotReusable(F.Compilation.Options.OptimizationLevel) Then
                ' Calculate local debug id
                '
                ' EnC: When emitting the baseline (gen 0) the id is stored in a custom debug information attached to the kickoff method.
                '      When emitting a delta the id is only used to map to the existing field in the previous generation.

                Dim declaratorSyntax As SyntaxNode = local.GetDeclaratorSyntax()
                Dim syntaxOffset As Integer = Me.Method.CalculateLocalSyntaxOffset(declaratorSyntax.SpanStart, declaratorSyntax.SyntaxTree)
                Dim ordinal As Integer = SynthesizedLocalOrdinals.AssignLocalOrdinal(local.SynthesizedKind, syntaxOffset)
                id = New LocalDebugId(syntaxOffset, ordinal)

                Dim previousSlotIndex = -1
                If SlotAllocatorOpt IsNot Nothing AndAlso SlotAllocatorOpt.TryGetPreviousHoistedLocalSlotIndex(declaratorSyntax, F.CompilationState.ModuleBuilderOpt.Translate(fieldType, declaratorSyntax, Diagnostics), local.SynthesizedKind, id, previousSlotIndex) Then
                    slotIndex = previousSlotIndex
                End If
            End If

            If slotIndex = -1 Then
                slotIndex = Me.nextFreeHoistedLocalSlot
                Me.nextFreeHoistedLocalSlot = Me.nextFreeHoistedLocalSlot + 1
            End If

            proxy = CreateByValLocalCapture(MakeHoistedFieldForLocal(local, fieldType, slotIndex, id), local)
            nonReusableLocalProxies.Add(local, proxy)

            Return proxy
        End Function

        Protected MustOverride Sub InitializeParameterWithProxy(parameter As ParameterSymbol, proxy As TProxy, stateMachineVariable As LocalSymbol, initializers As ArrayBuilder(Of BoundExpression))

        Protected MustOverride Function CreateByValLocalCapture(field As FieldSymbol, local As LocalSymbol) As TProxy

        Protected MustOverride Function CreateParameterCapture(field As FieldSymbol, parameter As ParameterSymbol) As TProxy

        Protected Overridable Function CreateByRefLocalCapture(typeMap As TypeSubstitution,
                                                               local As LocalSymbol,
                                                               initializers As Dictionary(Of LocalSymbol, BoundExpression)) As TProxy

            ' This is only supposed to be reachable in Async rewriter
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Function MakeHoistedFieldForLocal(local As LocalSymbol, localType As TypeSymbol, slotIndex As Integer, id As LocalDebugId) As FieldSymbol
            Dim proxyName As String

            Select Case local.SynthesizedKind
                Case SynthesizedLocalKind.LambdaDisplayClass
                    proxyName = StringConstants.StateMachineHoistedUserVariablePrefix & StringConstants.ClosureVariablePrefix & "$" & slotIndex
                Case SynthesizedLocalKind.UserDefined
                    proxyName = StringConstants.StateMachineHoistedUserVariablePrefix & local.Name & "$" & slotIndex
                Case SynthesizedLocalKind.With
                    proxyName = StringConstants.HoistedWithLocalPrefix & slotIndex
                Case Else
                    proxyName = StringConstants.HoistedSynthesizedLocalPrefix & slotIndex
            End Select

            Return F.StateMachineField(localType, Me.Method, proxyName, New LocalSlotDebugInfo(local.SynthesizedKind, id), slotIndex, Accessibility.Friend)
        End Function

        ''' <summary>
        ''' If any required special/well-known type/member is not found or has use-site errors
        ''' we should not continue with transformation because it may have unwanted consequences;
        ''' e.g. we do return Nothing if well-known member symbol is not found. This method should 
        ''' check all required symbols and return False if any of them are missing or have use-site errors.
        ''' We will also return True if signature is definitely bad - contains parameters that are ByRef or have error types
        ''' </summary>
        Friend Overridable Function EnsureAllSymbolsAndSignature() As Boolean
            If Me.Method.ReturnType.IsErrorType Then
                Return True
            End If

            For Each parameter In Me.Method.Parameters
                If parameter.IsByRef OrElse parameter.Type.IsErrorType Then
                    Return True
                End If
            Next

            Return False
        End Function

        Friend Sub EnsureSpecialType(type As SpecialType, <[In], Out> ByRef hasErrors As Boolean)
            Dim sType = Me.F.SpecialType(type)
            If sType.GetUseSiteErrorInfo IsNot Nothing Then
                hasErrors = True
            End If
        End Sub

        Friend Sub EnsureWellKnownType(type As WellKnownType, <[In], Out> ByRef hasErrors As Boolean)
            Dim wkType = Me.F.WellKnownType(type)
            If wkType.GetUseSiteErrorInfo IsNot Nothing Then
                hasErrors = True
            End If
        End Sub

        Friend Sub EnsureWellKnownMember(Of T As Symbol)(member As WellKnownMember, <[In], Out> ByRef hasErrors As Boolean)
            Dim wkMember = Me.F.WellKnownMember(Of T)(member)
            If wkMember Is Nothing OrElse wkMember.GetUseSiteErrorInfo IsNot Nothing Then
                hasErrors = True
            End If
        End Sub

        Friend Function OpenMethodImplementation(interfaceMethod As WellKnownMember, name As String, accessibility As Accessibility, Optional hasMethodBodyDependency As Boolean = False, Optional associatedProperty As PropertySymbol = Nothing) As SynthesizedMethod
            Dim methodToImplement As MethodSymbol = Me.F.WellKnownMember(Of MethodSymbol)(interfaceMethod)

            Return OpenMethodImplementation(methodToImplement, name, accessibility, hasMethodBodyDependency, associatedProperty)
        End Function

        Friend Function OpenMethodImplementation(interfaceMethod As SpecialMember, name As String, accessibility As Accessibility, Optional hasMethodBodyDependency As Boolean = False, Optional associatedProperty As PropertySymbol = Nothing) As SynthesizedMethod
            Dim methodToImplement As MethodSymbol = DirectCast(Me.F.SpecialMember(interfaceMethod), MethodSymbol)

            Return OpenMethodImplementation(methodToImplement, name, accessibility, hasMethodBodyDependency, associatedProperty)
        End Function

        Friend Function OpenMethodImplementation(interfaceType As NamedTypeSymbol, interfaceMethod As SpecialMember, name As String, accessibility As Accessibility, Optional hasMethodBodyDependency As Boolean = False, Optional associatedProperty As PropertySymbol = Nothing) As SynthesizedMethod
            Dim methodToImplement As MethodSymbol = DirectCast(Me.F.SpecialMember(interfaceMethod), MethodSymbol).AsMember(interfaceType)

            Return OpenMethodImplementation(methodToImplement, name, accessibility, hasMethodBodyDependency, associatedProperty)
        End Function

        Private Function OpenMethodImplementation(methodToImplement As MethodSymbol,
                                                  methodName As String,
                                                  accessibility As Accessibility,
                                                  Optional hasMethodBodyDependency As Boolean = False,
                                                  Optional associatedProperty As PropertySymbol = Nothing) As SynthesizedMethod

            ' Errors must be reported before and if any this point should not be reachable
            Debug.Assert(methodToImplement IsNot Nothing AndAlso methodToImplement.GetUseSiteErrorInfo Is Nothing)

            Dim result As New SynthesizedStateMachineDebuggerNonUserCodeMethod(DirectCast(Me.F.CurrentType, StateMachineTypeSymbol),
                                                                               methodName,
                                                                               methodToImplement,
                                                                               Me.F.Syntax,
                                                                               accessibility,
                                                                               hasMethodBodyDependency,
                                                                               associatedProperty)

            Me.F.AddMethod(Me.F.CurrentType, result)
            Me.F.CurrentMethod = result
            Return result
        End Function

        Friend Function OpenPropertyImplementation(interfaceProperty As SpecialMember, name As String, accessibility As Accessibility) As MethodSymbol
            Dim methodToImplement As MethodSymbol = DirectCast(Me.F.SpecialMember(interfaceProperty), PropertySymbol).GetMethod

            Return OpenPropertyImplementation(methodToImplement, name, accessibility)
        End Function

        Friend Function OpenPropertyImplementation(interfaceType As NamedTypeSymbol, interfaceMethod As SpecialMember, name As String, accessibility As Accessibility) As MethodSymbol
            Dim methodToImplement As MethodSymbol = DirectCast(Me.F.SpecialMember(interfaceMethod), PropertySymbol).GetMethod.AsMember(interfaceType)

            Return OpenPropertyImplementation(methodToImplement, name, accessibility)
        End Function

        Private Function OpenPropertyImplementation(getterToImplement As MethodSymbol, name As String, accessibility As Accessibility) As MethodSymbol

            Dim prop As New SynthesizedStateMachineProperty(DirectCast(Me.F.CurrentType, StateMachineTypeSymbol),
                                                            name,
                                                            getterToImplement,
                                                            Me.F.Syntax,
                                                            accessibility)

            Me.F.AddProperty(Me.F.CurrentType, prop)

            Dim getter = prop.GetMethod
            Me.F.AddMethod(Me.F.CurrentType, getter)

            Me.F.CurrentMethod = getter
            Return getter
        End Function

        Friend Sub CloseMethod(body As BoundStatement)
            Me.F.CloseMethod(RewriteBodyIfNeeded(body, Me.F.TopLevelMethod, Me.F.CurrentMethod))
        End Sub

        Friend Overridable Function RewriteBodyIfNeeded(body As BoundStatement, topMethod As MethodSymbol, currentMethod As MethodSymbol) As BoundStatement
            Return body
        End Function

        Friend Function OpenMoveNextMethodImplementation(interfaceMethod As WellKnownMember, accessibility As Accessibility) As SynthesizedMethod
            Dim methodToImplement As MethodSymbol = Me.F.WellKnownMember(Of MethodSymbol)(interfaceMethod)

            Return OpenMoveNextMethodImplementation(methodToImplement, accessibility)
        End Function

        Friend Function OpenMoveNextMethodImplementation(interfaceMethod As SpecialMember, accessibility As Accessibility) As SynthesizedMethod
            Dim methodToImplement As MethodSymbol = DirectCast(Me.F.SpecialMember(interfaceMethod), MethodSymbol)

            Return OpenMoveNextMethodImplementation(methodToImplement, accessibility)
        End Function

        Private Function OpenMoveNextMethodImplementation(methodToImplement As MethodSymbol, accessibility As Accessibility) As SynthesizedMethod

            ' Errors must be reported before and if any this point should not be reachable
            Debug.Assert(methodToImplement IsNot Nothing AndAlso methodToImplement.GetUseSiteErrorInfo Is Nothing)

            Dim result As New SynthesizedStateMachineMoveNextMethod(DirectCast(Me.F.CurrentType, StateMachineTypeSymbol),
                                                                    methodToImplement,
                                                                    Me.F.Syntax,
                                                                    accessibility)

            Me.F.AddMethod(Me.F.CurrentType, result)
            Me.F.CurrentMethod = result
            Return result
        End Function
    End Class

End Namespace
