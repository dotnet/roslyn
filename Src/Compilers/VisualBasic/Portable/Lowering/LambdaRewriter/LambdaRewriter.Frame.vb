' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Class LambdaRewriter

        '// !! Do not change this string. Other teams (FxCop) use this string to identify lambda functions in its analysis
        '// If you have to change this string, please contact the VB language PM and consider the impact of that break.
        Private Const LAMBDA_PREFIX As String = "_Lambda$__"
        Private Const CLOSURE_GENERICPARAM_PREFIX As String = "$CLS"
        Private Const CLOSURE_MYSTUB_PREFIX As String = "$VB$ClosureStub_"

        ''' <summary>
        ''' A class that represents the set of variables in a scope that have been
        ''' captured by lambdas within that scope.
        ''' </summary>
        Friend NotInheritable Class Frame
            Inherits InstanceTypeSymbol
            Implements ISynthesizedMethodBodyImplementationSymbol

            Private ReadOnly m_containingSymbol As Symbol
            Private ReadOnly m_name As String
            Private ReadOnly m_typeParameters As ImmutableArray(Of TypeParameterSymbol)
            Private ReadOnly m_topLevelMethod As MethodSymbol

            'NOTE: this does not include captured parent frame references 
            Friend ReadOnly m_captured_locals As New ArrayBuilder(Of CapturedVariable)

            Friend ReadOnly Constructor As SynthesizedLambdaConstructor
            Friend ReadOnly TypeMap As TypeSubstitution

            Private Shared ReadOnly TypeSubstitutionFactory As Func(Of Symbol, TypeSubstitution) =
                Function(container)
                    Dim f = TryCast(container, Frame)
                    Return If(f IsNot Nothing, f.TypeMap, DirectCast(container, SynthesizedMethod).TypeMap)
                End Function

            Friend Shared ReadOnly CreateTypeParameter As Func(Of TypeParameterSymbol, Symbol, TypeParameterSymbol) =
                Function(typeParameter, container) New SynthesizedClonedTypeParameterSymbol(typeParameter,
                                                                                            container,
                                                                                            CLOSURE_GENERICPARAM_PREFIX & typeParameter.Ordinal,
                                                                                            TypeSubstitutionFactory)

            ''' <summary>
            ''' Creates a Frame definition
            ''' </summary>
            ''' <param name="containingType">Type that contains Frame type.</param>
            ''' <param name="enclosingMethod">Method that contains lambda expression for which we do the rewrite.</param>
            ''' <param name="copyConstructor">Specifies whether the Frame needs a copy-constructor.</param>
            Friend Sub New(
                syntaxNode As VisualBasicSyntaxNode,
                containingType As NamedTypeSymbol,
                enclosingMethod As MethodSymbol,
                copyConstructor As Boolean,
                tempNumber As Integer
            )
                Me.m_containingSymbol = containingType
                Me.m_name = StringConstants.ClosureClassPrefix & tempNumber

                If copyConstructor Then
                    Me.Constructor = New SynthesizedLambdaCopyConstructor(syntaxNode, Me)
                Else
                    Me.Constructor = New SynthesizedLambdaConstructor(syntaxNode, Me)
                End If

                Me.m_typeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(enclosingMethod.TypeParameters, Me, CreateTypeParameter)
                Me.TypeMap = TypeSubstitution.Create(enclosingMethod, enclosingMethod.TypeParameters, Me.TypeArgumentsNoUseSiteDiagnostics)
                Me.m_topLevelMethod = enclosingMethod
            End Sub

            Public Overrides ReadOnly Property Arity As Integer
                Get
                    Return m_typeParameters.Length
                End Get
            End Property

            Friend Overrides ReadOnly Property MangleName As Boolean
                Get
                    Return Arity > 0
                End Get
            End Property

            Friend Overrides ReadOnly Property HasSpecialName As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property IsSerializable As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property Layout As TypeLayout
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides ReadOnly Property MarshallingCharSet As CharSet
                Get
                    Return DefaultMarshallingCharSet
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return m_containingSymbol
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
                Get
                    Return TryCast(m_containingSymbol, NamedTypeSymbol)
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(attributes)

                ' Attribute: System.Runtime.CompilerServices.CompilerGeneratedAttribute()
                Dim sourceModule = TryCast(m_containingSymbol.ContainingModule, SourceModuleSymbol)

                ' this can only happen if a frame is nested in another frame (so far we do not do this)
                ' if this happens for whatever reason, we do not need "CompilerGenerated" anyways
                Debug.Assert(sourceModule IsNot Nothing, "Frame is not contained in a source module?")

                Dim compilation = sourceModule.ContainingSourceAssembly.DeclaringCompilation

                AddSynthesizedAttribute(attributes, compilation.SynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
            End Sub

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    ' Dev11 uses "assembly" here. No need to be different.
                    Return Accessibility.Friend
                End Get
            End Property

            Public Overloads Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
                Return ImmutableArray(Of Symbol).Empty
            End Function

            Public Overloads Overrides Function GetMembers() As ImmutableArray(Of Symbol)
                Return StaticCast(Of Symbol).From(m_captured_locals.AsImmutable())
            End Function

            Public Overloads Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Public Overloads Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Public Overloads Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Friend Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
                Return m_captured_locals
            End Function

            Public Overrides ReadOnly Property IsMustInherit As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsNotInheritable As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray(Of Location).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return ImmutableArray(Of SyntaxReference).Empty
                End Get
            End Property

            Friend Overrides Function MakeAcyclicBaseType(diagnostics As DiagnosticBag) As NamedTypeSymbol
                Dim type = ContainingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Object)
                ' WARN: We assume that if System_Object was not found we would never reach 
                '       this point because the error should have been/processed generated earlier
                Debug.Assert(type.GetUseSiteErrorInfo() Is Nothing)
                Return type
            End Function

            Friend Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Friend Overrides Function MakeDeclaredBase(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As NamedTypeSymbol
                Dim type = ContainingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Object)
                ' WARN: We assume that if System_Object was not found we would never reach 
                '       this point because the error should have been/processed generated earlier
                Debug.Assert(type.GetUseSiteErrorInfo() Is Nothing)
                Return type
            End Function

            Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
                Get
                    Return SpecializedCollections.EmptyEnumerable(Of String)()
                End Get
            End Property

            Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property HasEmbeddedAttribute As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property Name As String
                Get
                    Return m_name
                End Get
            End Property

            Public Overrides ReadOnly Property TypeKind As TYPEKIND
                Get
                    Return TypeKind.Class
                End Get
            End Property

            Friend Overrides ReadOnly Property IsInterface As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return m_typeParameters
                End Get
            End Property

            Friend Overrides ReadOnly Property DefaultPropertyName As String
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property IsComImport As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property CoClassType As TypeSymbol
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
                Return ImmutableArray(Of String).Empty
            End Function

            Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
                Throw ExceptionUtilities.Unreachable
            End Function

            Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
                Throw ExceptionUtilities.Unreachable
            End Function

            ''' <summary>
            ''' Force all declaration errors to be generated.
            ''' </summary>
            Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
                Throw ExceptionUtilities.Unreachable
            End Sub

            Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
                Get
                    ' This method contains user code from the lamda
                    Return True
                End Get
            End Property

            Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
                Get
                    Return m_topLevelMethod
                End Get
            End Property
        End Class

        ''' <summary>
        ''' A field of a frame class that represents a variable that has been captured in a lambda.
        ''' </summary>
        Friend NotInheritable Class CapturedVariable
            Inherits FieldSymbol

            Private ReadOnly m_frame As Frame
            Private ReadOnly m_name As String
            Private ReadOnly m_type As TypeSymbol
            Private ReadOnly m_constantValue As ConstantValue
            Private ReadOnly m_isMe As Boolean

            Friend Sub New(frame As Frame, captured As Symbol)
                Me.m_frame = frame

                ' captured symbol is either a local or parameter.
                Dim local = TryCast(captured, LocalSymbol)

                If local IsNot Nothing Then
                    ' it is a local variable
                    Dim localTypeAsFrame = TryCast(local.Type.OriginalDefinition, Frame)
                    If localTypeAsFrame IsNot Nothing Then
                        ' if we're capturing a generic frame pointer, construct it with the new frame's type parameters
                        Me.m_type = ConstructFrameType(localTypeAsFrame, frame.TypeArgumentsNoUseSiteDiagnostics)
                    Else
                        Me.m_type = local.Type.InternalSubstituteTypeParameters(frame.TypeMap)
                    End If

                    If local.IsCompilerGenerated Then
                        Me.m_name = StringConstants.LiftedNonLocalPrefix & captured.Name
                    Else
                        Me.m_name = StringConstants.LiftedLocalPrefix & captured.Name
                    End If

                    If local.IsConst Then
                        Me.m_constantValue = local.GetConstantValue(Nothing)
                    End If
                Else
                    ' it must be a parameter
                    Dim parameter = DirectCast(captured, ParameterSymbol)
                    Me.m_type = parameter.Type.InternalSubstituteTypeParameters(frame.TypeMap)

                    If parameter.IsMe Then
                        Me.m_name = StringConstants.LiftedMeName
                        Me.m_isMe = True
                    Else
                        Me.m_name = StringConstants.LiftedLocalPrefix & captured.Name
                    End If
                End If
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return m_name
                End Get
            End Property

            Friend Overrides ReadOnly Property HasSpecialName As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property IsNotSerialized As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
                Get
                    Return Nothing
                End Get
            End Property

            Public Overrides ReadOnly Property IsShared As Boolean
                Get
                    Return IsConst
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    Return Accessibility.Public
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray(Of Location).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return ImmutableArray(Of SyntaxReference).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return m_frame
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
                Get
                    Return m_frame
                End Get
            End Property

            Public Overrides ReadOnly Property IsConst As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    Return m_constantValue IsNot Nothing
                End Get
            End Property

            Public Overrides ReadOnly Property AssociatedSymbol As Symbol
                Get
                    Return Nothing
                End Get
            End Property

            Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return ImmutableArray(Of CustomModifier).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property Type As TypeSymbol
                Get
                    Return m_type
                End Get
            End Property

            Friend Overrides Function GetConstantValue(inProgress As SymbolsInProgress(Of FieldSymbol)) As ConstantValue
                Return m_constantValue
            End Function

            Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides ReadOnly Property IsCapturedFrame As Boolean
                Get
                    Return Me.m_isMe
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Copy constructor has one parameter of the same type as the enclosing type.
        ''' The purpose is to copy all the lifted values from previous version of the 
        ''' frame if there was any into the new one.
        ''' </summary>
        Friend Class SynthesizedLambdaCopyConstructor
            Inherits SynthesizedLambdaConstructor

            Private ReadOnly m_parameters As ImmutableArray(Of ParameterSymbol)

            Friend Sub New(syntaxNode As VisualBasicSyntaxNode, containingType As Frame)
                MyBase.New(syntaxNode, containingType)

                m_parameters = ImmutableArray.Create(Of ParameterSymbol)(New SourceSimpleParameterSymbol(Me, "arg0", 0, containingType, Nothing))
            End Sub

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return m_parameters
                End Get
            End Property

            Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
                Get
                    Return False
                End Get
            End Property
        End Class

        Friend Class SynthesizedLambdaConstructor
            Inherits SynthesizedMethod
            Implements ISynthesizedMethodBodyImplementationSymbol

            Friend Sub New(
                syntaxNode As VisualBasicSyntaxNode,
                containingType As Frame
            )
                MyBase.New(syntaxNode, containingType, WellKnownMemberNames.InstanceConstructorName, False)
            End Sub

            Public Overrides ReadOnly Property MethodKind As MethodKind
                Get
                    Return MethodKind.Constructor
                End Get
            End Property

            Friend Function AsMember(frameType As NamedTypeSymbol) As MethodSymbol
                ' ContainingType is always a Frame here which is a type definition so we can use "Is"
                If frameType Is ContainingType Then
                    Return Me
                End If

                Dim substituted = DirectCast(frameType, SubstitutedNamedType)
                Return DirectCast(substituted.GetMemberForDefinition(Me), MethodSymbol)
            End Function

            Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
                Get
                    Return True
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(attributes)

                ' Dev11 adds DebuggerNonUserCode; there is no reason to do so since:
                ' - we emit no debug info for the body
                ' - the code doesn't call any user code that could inspect the stack and find the accessor's frame
                ' - the code doesn't throw exceptions whose stack frames we would need to hide
                ' 
                ' C# also doesn't add DebuggerHidden nor DebuggerNonUserCode attributes.
            End Sub

            Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
                Return False
            End Function

            Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
                Get
                    Return False
                End Get
            End Property

            Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
                Get
                    Dim symbol As ISynthesizedMethodBodyImplementationSymbol = CType(ContainingSymbol, ISynthesizedMethodBodyImplementationSymbol)
                    Return symbol.Method
                End Get
            End Property
        End Class

        ''' <summary>
        ''' A method that results from the translation of a single lambda expression.
        ''' </summary>
        Friend NotInheritable Class SynthesizedLambdaMethod
            Inherits SynthesizedMethod

            Private ReadOnly m_lambda As LambdaSymbol
            Private ReadOnly m_isShared As Boolean
            Private ReadOnly m_parameters As ImmutableArray(Of ParameterSymbol)
            Private ReadOnly m_locations As ImmutableArray(Of Location)
            Private ReadOnly m_typeParameters As ImmutableArray(Of TypeParameterSymbol)
            Private ReadOnly m_typeMap As TypeSubstitution

            ''' <summary>
            ''' In case the lambda is an 'Async' lambda, stores the reference to a state machine type 
            ''' synthesized in AsyncRewriter. 
            ''' </summary>
            Private ReadOnly m_asyncStateMachineType As NamedTypeSymbol = Nothing

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    Return If(TypeOf ContainingType Is Frame, Accessibility.Friend, Accessibility.Private)
                End Get
            End Property

            Friend Overrides ReadOnly Property TypeMap As TypeSubstitution
                Get
                    Return Me.m_typeMap
                End Get
            End Property

            ''' <summary>
            ''' Creates a symbol for a synthesized lambda method
            ''' </summary>
            ''' <param name="containingType">Type that contains lambda method 
            ''' - it is either Frame or enclosing class in a case if we do not lift anything.</param>
            ''' <param name="enclosingMethod">Method that contains lambda expression for which we do the rewrite.</param>
            ''' <param name="lambdaNode">Lambda expression which is represented by this method.</param>
            ''' <param name="isShared">Specifies whether lambda method should be shared.</param>
            ''' <remarks></remarks>
            Friend Sub New(containingType As InstanceTypeSymbol,
                           enclosingMethod As MethodSymbol,
                           lambdaNode As BoundLambda,
                           isShared As Boolean,
                           tempNumber As Integer,
                           diagnostics As DiagnosticBag)

                MyBase.New(lambdaNode.Syntax, containingType, LAMBDA_PREFIX & tempNumber, isShared)
                Me.m_lambda = lambdaNode.LambdaSymbol
                Me.m_isShared = isShared
                Me.m_locations = ImmutableArray.Create(Of Location)(lambdaNode.Syntax.GetLocation())

                If Not enclosingMethod.IsGenericMethod Then
                    Me.m_typeMap = Nothing
                    Me.m_typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                Else
                    Dim containingTypeAsFrame = TryCast(containingType, Frame)
                    If containingTypeAsFrame IsNot Nothing Then
                        Me.m_typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                        Me.m_typeMap = containingTypeAsFrame.TypeMap
                    Else
                        Me.m_typeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(enclosingMethod.TypeParameters, Me, Frame.CreateTypeParameter)
                        Me.m_typeMap = TypeSubstitution.Create(enclosingMethod, enclosingMethod.TypeParameters, Me.TypeArguments)
                    End If
                End If

                Dim params = ArrayBuilder(Of ParameterSymbol).GetInstance

                Dim ordinalAdjustment = 0
                If isShared Then
                    ' add dummy "this"
                    params.Add(New SynthesizedParameterSymbol(Me, DeclaringCompilation.GetSpecialType(SpecialType.System_Object), 0, False))
                    ordinalAdjustment = 1
                End If

                For Each curParam In m_lambda.Parameters
                    params.Add(
                        WithNewContainerAndType(
                        Me,
                        curParam.Type.InternalSubstituteTypeParameters(TypeMap),
                        curParam,
                        ordinalAdjustment:=ordinalAdjustment))
                Next

                Me.m_parameters = params.ToImmutableAndFree

                If Me.m_lambda.IsAsync Then
                    Dim binder As binder = lambdaNode.LambdaBinderOpt
                    Debug.Assert(binder IsNot Nothing)
                    Dim syntax As VisualBasicSyntaxNode = lambdaNode.Syntax
                    Me.m_asyncStateMachineType =
                        AsyncRewriter.CreateAsyncStateMachineTypeSymbol(
                            Me, 0,
                            binder.GetSpecialType(SpecialType.System_ValueType, syntax, diagnostics),
                            binder.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine, syntax, diagnostics))
                End If
            End Sub

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return m_typeParameters
                End Get
            End Property

            Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
                Get
                    ' This is always a method definition, so the type arguments are the same as the type parameters.
                    If Arity > 0 Then
                        Return StaticCast(Of TypeSymbol).From(Me.TypeParameters)
                    Else
                        Return ImmutableArray(Of TypeSymbol).Empty
                    End If
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return m_locations
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return m_parameters
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnType As TypeSymbol
                Get
                    Return m_lambda.ReturnType.InternalSubstituteTypeParameters(TypeMap)
                End Get
            End Property

            Public Overrides ReadOnly Property IsShared As Boolean
                Get
                    Return m_isShared
                End Get
            End Property

            Public Overrides ReadOnly Property IsVararg As Boolean
                Get
                    Debug.Assert(Not m_lambda.IsVararg)
                    Return False
                End Get

            End Property

            Public Overrides ReadOnly Property Arity As Integer
                Get
                    Return m_typeParameters.Length
                End Get
            End Property

            Friend Overrides ReadOnly Property HasSpecialName As Boolean
                Get
                    Return True
                End Get
            End Property

            Friend Function AsMember(constructedFrame As NamedTypeSymbol) As MethodSymbol
                ' ContainingType is always a Frame here which is a type definition so we can use "Is"
                If constructedFrame Is ContainingType Then
                    Return Me
                End If

                Dim substituted = DirectCast(constructedFrame, SubstitutedNamedType)
                Return DirectCast(substituted.GetMemberForDefinition(Me), MethodSymbol)
            End Function

            Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
                Get
                    Return m_lambda.GenerateDebugInfoImpl
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(attributes)

                ' Lambda that doesn't contain user code may still call to a user code (e.g. delegate relaxation stubs). We want the stack frame to be hidden.
                ' Dev11 marks such lambda with DebuggerStepThrough attribute but that seems to be useless. Rather we hide the frame completely.
                If Not GenerateDebugInfoImpl Then
                    AddSynthesizedAttribute(attributes, DeclaringCompilation.SynthesizeDebuggerHiddenAttribute())
                End If

                If Me.m_asyncStateMachineType IsNot Nothing Then
                    Dim compilation = Me.DeclaringCompilation

                    Debug.Assert(
                        WellKnownMembers.IsSynthesizedAttributeOptional(
                            WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor))
                    AddSynthesizedAttribute(attributes,
                                            compilation.SynthesizeAttribute(
                                                WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor,
                                                ImmutableArray.Create(Of TypedConstant)(
                                                    New TypedConstant(
                                                        compilation.GetWellKnownType(WellKnownType.System_Type),
                                                        TypedConstantKind.Type,
                                                        If(Me.m_asyncStateMachineType.IsGenericType,
                                                           Me.m_asyncStateMachineType.AsUnboundGenericType,
                                                           Me.m_asyncStateMachineType)))))

                    AddSynthesizedAttribute(attributes, compilation.SynthesizeOptionalDebuggerStepThroughAttribute())

                ElseIf Me.IsIterator Then
                    AddSynthesizedAttribute(attributes, Me.DeclaringCompilation.SynthesizeOptionalDebuggerStepThroughAttribute())

                End If
            End Sub

            Public Overrides ReadOnly Property IsAsync As Boolean
                Get
                    Return Me.m_lambda.IsAsync
                End Get
            End Property

            Public Overrides ReadOnly Property IsIterator As Boolean
                Get
                    Return Me.m_lambda.IsIterator
                End Get
            End Property

            Friend Overrides Function GetAsyncStateMachineType() As NamedTypeSymbol
                Return Me.m_asyncStateMachineType
            End Function

            Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
                Return False
            End Function

        End Class

    End Class
End Namespace

