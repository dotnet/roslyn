' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' </summary>
    ''' <typeparam name="TStateMachineState">
    ''' Type for representing State Machine type symbol 
    ''' </typeparam>
    ''' <typeparam name="TProxy">
    ''' Type used by State Machine rewriter to represent symbol proxy. Lambda rewriter as 
    ''' well as iterator rewriter use simplified form of proxy as they only capture 
    ''' locals as r-values in fields, async rewriter uses a different structure as a proxy
    ''' because it has to capture l-values on stack as well
    ''' </typeparam>
    Partial Friend MustInherit Class StateMachineRewriter(Of TStateMachineState As SynthesizedContainer, TProxy)

        Protected ReadOnly Body As BoundStatement
        Protected ReadOnly Method As MethodSymbol
        Protected ReadOnly SlotAllocatorOpt As VariableSlotAllocator
        Protected ReadOnly CompilationState As TypeCompilationState
        Protected ReadOnly Diagnostics As DiagnosticBag
        Protected ReadOnly F As SyntheticBoundNodeFactory

        Protected StateField As FieldSymbol
        Protected variableProxies As Dictionary(Of Symbol, TProxy)
        Protected InitialParameters As Dictionary(Of Symbol, TProxy)

        Private nextLocalNumber As Integer = 1
        Private nextTempNumber As Integer = 1

        Protected Sub New(body As BoundStatement,
                          method As MethodSymbol,
                          slotAllocatorOpt As VariableSlotAllocator,
                          compilationState As TypeCompilationState,
                          diagnostics As DiagnosticBag)

            Debug.Assert(body IsNot Nothing)
            Debug.Assert(method IsNot Nothing)
            Debug.Assert(compilationState IsNot Nothing)
            Debug.Assert(diagnostics IsNot Nothing)

            Me.Body = body
            Me.Method = method
            Me.SlotAllocatorOpt = slotAllocatorOpt
            Me.CompilationState = compilationState
            Me.Diagnostics = diagnostics

            Me.F = New SyntheticBoundNodeFactory(method, method, method.ContainingType, body.Syntax, compilationState, diagnostics)
        End Sub

        ''' <summary>
        ''' True if the initial values of locals in the rewritten method need to be preserved. (e.g. enumerable iterator methods)
        ''' </summary>
        Protected MustOverride ReadOnly Property PreserveInitialParameterValues As Boolean

        ''' <summary>
        ''' State machine type
        ''' </summary>
        Protected MustOverride ReadOnly Property StateMachineClass As TStateMachineState

        ''' <summary>
        ''' Type substitution if applicable or Nothing
        ''' </summary>
        Friend MustOverride ReadOnly Property TypeMap As TypeSubstitution

        ''' <summary>
        ''' Add fields to the state machine class that are unique to async or iterator methods.
        ''' </summary>
        Protected MustOverride Sub GenerateFields()

        ''' <summary>
        ''' Initialize the state machine class.
        ''' </summary>
        Protected MustOverride Sub InitializeStateMachine(bodyBuilder As ArrayBuilder(Of BoundStatement), frameType As NamedTypeSymbol, stateMachineLocal As LocalSymbol)

        ''' <summary>
        ''' Generate implementation-specific state machine initialization for the replacement method body.
        ''' </summary>
        Protected MustOverride Function GenerateReplacementBody(stateMachineVariable As LocalSymbol, frameType As NamedTypeSymbol) As BoundStatement

        ''' <summary>
        ''' Generate implementation-specific state machine member method implementations.
        ''' </summary>
        Protected MustOverride Sub GenerateMethodImplementations()

        Protected Function Rewrite() As BoundBlock
            Debug.Assert(Not Me.PreserveInitialParameterValues OrElse Method.IsIterator)

            Me.F.OpenNestedType(Me.StateMachineClass)
            Me.F.CompilationState.StateMachineImplementationClass(Me.Method) = Me.StateMachineClass

            ' Add a field: int _state
            Dim intType = Me.F.SpecialType(SpecialType.System_Int32)
            Me.StateField = Me.F.StateMachineField(intType, Me.Method, GeneratedNames.MakeStateMachineStateFieldName(), Accessibility.Friend)

            Me.GenerateFields()

            ' and fields for the initial values of all the parameters of the method
            If Me.PreserveInitialParameterValues Then
                Me.InitialParameters = New Dictionary(Of Symbol, TProxy)()
            End If

            ' add fields for the captured variables of the method
            Dim captured = IteratorAndAsyncCaptureWalker.Analyze(New FlowAnalysisInfo(Me.CompilationState.Compilation, Me.Method, Me.Body))

            Me.variableProxies = New Dictionary(Of Symbol, TProxy)()

            CreateInitialProxies(captured)

            GenerateMethodImplementations()

            ' Return a replacement body for the original iterator method
            Return ReplaceOriginalMethod()
        End Function

        Private Function ReplaceOriginalMethod() As BoundBlock
            Me.F.CurrentMethod = Me.Method
            Dim bodyBuilder = ArrayBuilder(Of BoundStatement).GetInstance()
            bodyBuilder.Add(Me.F.HiddenSequencePoint())

            Dim frameType As NamedTypeSymbol = SubstituteTypeIfNeeded(Me.StateMachineClass)

            Dim stateMachineVariable As LocalSymbol = F.SynthesizedLocal(frameType)

            InitializeStateMachine(bodyBuilder, frameType, stateMachineVariable)

            InitializeProxies(bodyBuilder, If(Me.PreserveInitialParameterValues, Me.InitialParameters, variableProxies), stateMachineVariable)

            Return Me.F.Block(
                ImmutableArray.Create(Of LocalSymbol)(stateMachineVariable),
                bodyBuilder.ToImmutableAndFree().Add(
                    GenerateReplacementBody(stateMachineVariable, frameType)))
        End Function

        Protected Function SubstituteTypeIfNeeded(type As NamedTypeSymbol) As NamedTypeSymbol
            Return If(Me.Method.IsGenericMethod, type.OriginalDefinition.Construct(Me.Method.TypeArguments), type)
        End Function

        Private Sub InitializeProxies(bodyBuilder As ArrayBuilder(Of BoundStatement),
                                      copyDest As Dictionary(Of Symbol, TProxy),
                                      stateMachineVariable As LocalSymbol)

            Dim initializers = ArrayBuilder(Of BoundExpression).GetInstance()

            ' starting with the "Me" proxy
            If Not Me.Method.IsShared AndAlso Me.Method.MeParameter IsNot Nothing Then
                InitializeParameter(Me.Method.MeParameter, copyDest, stateMachineVariable, initializers)
            End If

            ' then all the parameters
            For Each parameter In Me.Method.Parameters
                InitializeParameter(parameter, copyDest, stateMachineVariable, initializers)
            Next

            If initializers.Count > 0 Then
                bodyBuilder.Add(F.ExpressionStatement(F.Sequence(initializers.ToArray())))
            End If

            initializers.Free()
        End Sub

        Private Sub InitializeParameter(parameter As ParameterSymbol,
                                        copyDest As Dictionary(Of Symbol, TProxy),
                                        stateMachineVariable As LocalSymbol,
                                        initializers As ArrayBuilder(Of BoundExpression))

            Dim proxy As TProxy = Nothing
            If copyDest.TryGetValue(parameter, proxy) Then
                InitializeParameterWithProxy(parameter, proxy, stateMachineVariable, initializers)
            End If
        End Sub

        Private Sub CreateInitialProxies(captured As IteratorAndAsyncCaptureWalker.Result)

            Dim typeMap As TypeSubstitution = StateMachineClass.TypeSubstitution

            Dim orderedCaptured As IEnumerable(Of Symbol) =
                                From local In captured.CapturedLocals
                                Order By local.Name,
                                         If(local.Locations.Length = 0, 0, local.Locations(0).SourceSpan.Start)
                                Select local

            For Each sym In orderedCaptured
                Select Case sym.Kind
                    Case SymbolKind.Local
                        CaptureLocalSymbol(typeMap, DirectCast(sym, LocalSymbol), captured.ByRefLocalsInitializers)

                    Case SymbolKind.Parameter
                        CaptureParameterSymbol(typeMap, DirectCast(sym, ParameterSymbol))
                End Select
            Next
        End Sub

        Protected Function CaptureParameterSymbol(typeMap As TypeSubstitution,
                                                  parameter As ParameterSymbol) As TProxy

            Dim proxy As TProxy = Nothing
            If Me.variableProxies.TryGetValue(parameter, proxy) Then
                ' This proxy may have already be added while processing 
                ' previous ByRef local
                Return proxy
            End If

            If parameter.IsMe Then
                Dim typeName As String = parameter.ContainingSymbol.ContainingType.Name
                Dim isMeOfClosureType As Boolean = typeName.StartsWith(StringConstants.ClosureClassPrefix)

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
                Me.variableProxies.Add(parameter, proxy)

                If Me.PreserveInitialParameterValues Then
                    Dim initialMe As TProxy = If(Me.Method.ContainingType.IsStructureType(),
                                                 CreateParameterCapture(
                                                     Me.F.StateMachineField(
                                                         Me.Method.ContainingType,
                                                         Me.Method,
                                                         GeneratedNames.MakeIteratorParameterProxyName(GeneratedNames.MakeStateMachineCapturedMeName()),
                                                         Accessibility.Friend),
                                                     parameter),
                                                 Me.variableProxies(parameter))

                    Me.InitialParameters.Add(parameter, initialMe)
                End If

            Else
                Dim paramType As TypeSymbol = parameter.Type.InternalSubstituteTypeParameters(typeMap)

                Debug.Assert(Not parameter.IsByRef)
                proxy = CreateParameterCapture(
                            F.StateMachineField(
                                paramType,
                                Me.Method,
                                GeneratedNames.MakeStateMachineParameterName(parameter.Name),
                                Accessibility.Friend),
                            parameter)
                Me.variableProxies.Add(parameter, proxy)

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
            If Me.variableProxies.TryGetValue(local, proxy) Then
                ' This proxy may have already be added while processing 
                ' previous ByRef local
                Return proxy
            End If

            Dim localType As TypeSymbol = local.Type.InternalSubstituteTypeParameters(typeMap)

            If local.IsByRef Then
                Debug.Assert(initializers.ContainsKey(local))
                proxy = CreateByRefLocalCapture(typeMap, local, initializers)
            Else
                proxy = CreateByValLocalCapture(MakeHoistedFieldForLocal(local, localType), local)
            End If

            Me.variableProxies.Add(local, proxy)
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

        Protected Function MakeHoistedFieldForLocal(local As LocalSymbol, localType As TypeSymbol) As FieldSymbol
            Dim proxyName As String

            If local.SynthesizedKind = SynthesizedLocalKind.LambdaDisplayClass Then
                ' Special Case: There's logic in the EE to recognize locals that have been captured by a lambda
                ' and would have been hoisted for the state machine.  Basically, we just hoist the local containing
                ' the instance of the lambda closure class as if it were user-created, rather than a compiler temp.
                proxyName = GeneratedNames.MakeStateMachineLocalName(Me.nextLocalNumber, GeneratedNames.MakeHoistedLocalFieldName(local.SynthesizedKind, localType, Me.nextLocalNumber))
                Me.nextLocalNumber += 1
            ElseIf local.SynthesizedKind <> SynthesizedLocalKind.UserDefined Then
                proxyName = GeneratedNames.MakeStateMachineLocalName(Me.nextTempNumber,
                 If(local.SynthesizedKind.IsLongLived(), GeneratedNames.MakeHoistedLocalFieldName(local.SynthesizedKind, localType, Me.nextTempNumber), Nothing))
                Me.nextTempNumber += 1
            Else
                proxyName = GeneratedNames.MakeStateMachineLocalName(Me.nextLocalNumber, local.Name)
                Me.nextLocalNumber += 1
            End If

            Return F.StateMachineField(localType, Me.Method, proxyName, Accessibility.Friend)
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

        Friend Function OpenMethodImplementation(interfaceMethod As WellKnownMember, name As String, dbgAttrs As DebugAttributes, accessibility As Accessibility, enableDebugInfo As Boolean, Optional hasMethodBodyDependency As Boolean = False, Optional associatedProperty As PropertySymbol = Nothing) As SynthesizedStateMachineMethod
            Dim methodToImplement As MethodSymbol = Me.F.WellKnownMember(Of MethodSymbol)(interfaceMethod)

            Return OpenMethodImplementation(methodToImplement, name, dbgAttrs, accessibility, enableDebugInfo, hasMethodBodyDependency, associatedProperty)
        End Function

        Friend Function OpenMethodImplementation(interfaceMethod As SpecialMember, name As String, dbgAttrs As DebugAttributes, accessibility As Accessibility, enableDebugInfo As Boolean, Optional hasMethodBodyDependency As Boolean = False, Optional associatedProperty As PropertySymbol = Nothing) As SynthesizedStateMachineMethod
            Dim methodToImplement As MethodSymbol = DirectCast(Me.F.SpecialMember(interfaceMethod), MethodSymbol)

            Return OpenMethodImplementation(methodToImplement, name, dbgAttrs, accessibility, enableDebugInfo, hasMethodBodyDependency, associatedProperty)
        End Function

        Friend Function OpenMethodImplementation(interfaceType As NamedTypeSymbol, interfaceMethod As SpecialMember, name As String, dbgAttrs As DebugAttributes, accessibility As Accessibility, enableDebugInfo As Boolean, Optional hasMethodBodyDependency As Boolean = False, Optional associatedProperty As PropertySymbol = Nothing) As SynthesizedStateMachineMethod
            Dim methodToImplement As MethodSymbol = DirectCast(Me.F.SpecialMember(interfaceMethod), MethodSymbol).AsMember(interfaceType)

            Return OpenMethodImplementation(methodToImplement, name, dbgAttrs, accessibility, enableDebugInfo, hasMethodBodyDependency, associatedProperty)
        End Function

        Private Function OpenMethodImplementation(methodToImplement As MethodSymbol,
                                                  methodName As String,
                                                  debugAttributes As DebugAttributes,
                                                  accessibility As Accessibility,
                                                  enableDebugInfo As Boolean,
                                                  Optional hasMethodBodyDependency As Boolean = False,
                                                  Optional associatedProperty As PropertySymbol = Nothing) As SynthesizedStateMachineMethod

            ' Errors must be reported before and if any thispoint should not be reachable
            Debug.Assert(methodToImplement IsNot Nothing AndAlso methodToImplement.GetUseSiteErrorInfo Is Nothing)

            Dim result As New SynthesizedStateMachineMethod(DirectCast(Me.F.CurrentType, StateMachineTypeSymbol),
                                                            methodName,
                                                            methodToImplement,
                                                            Me.F.Syntax,
                                                            debugAttributes,
                                                            accessibility,
                                                            enableDebugInfo,
                                                            hasMethodBodyDependency,
                                                            associatedProperty)

            Me.F.AddMethod(Me.F.CurrentType, result)
            Me.F.CurrentMethod = result
            Return result
        End Function

        Friend Function OpenPropertyImplementation(interfaceProperty As SpecialMember, name As String, dbgAttrs As DebugAttributes, accessibility As Accessibility, enableDebugInfo As Boolean) As MethodSymbol
            Dim methodToImplement As MethodSymbol = DirectCast(Me.F.SpecialMember(interfaceProperty), PropertySymbol).GetMethod

            Return OpenPropertyImplementation(methodToImplement, name, dbgAttrs, accessibility, enableDebugInfo)
        End Function

        Friend Function OpenPropertyImplementation(interfaceType As NamedTypeSymbol, interfaceMethod As SpecialMember, name As String, dbgAttrs As DebugAttributes, accessibility As Accessibility, enableDebugInfo As Boolean) As MethodSymbol
            Dim methodToImplement As MethodSymbol = DirectCast(Me.F.SpecialMember(interfaceMethod), PropertySymbol).GetMethod.AsMember(interfaceType)

            Return OpenPropertyImplementation(methodToImplement, name, dbgAttrs, accessibility, enableDebugInfo)
        End Function

        Private Function OpenPropertyImplementation(getterToImplement As MethodSymbol,
                                                    name As String,
                                                    debugAttributes As DebugAttributes,
                                                    accessibility As Accessibility,
                                                    enableDebugInfo As Boolean,
                                                    Optional hasMethodBodyDependency As Boolean = False) As MethodSymbol

            Dim prop As New SynthesizedStateMachineProperty(DirectCast(Me.F.CurrentType, StateMachineTypeSymbol),
                                                            name,
                                                            getterToImplement,
                                                            Me.F.Syntax,
                                                            debugAttributes,
                                                            accessibility,
                                                            enableDebugInfo,
                                                            hasMethodBodyDependency)

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

        Protected Function IsDebuggerHidden(method As MethodSymbol) As Boolean
            Dim debuggerHiddenAttribute = Me.CompilationState.Compilation.GetWellKnownType(WellKnownType.System_Diagnostics_DebuggerHiddenAttribute)
            For Each a In Me.Method.GetAttributes()
                If a.AttributeClass = debuggerHiddenAttribute Then Return True
            Next

            Return False
        End Function
    End Class

End Namespace
