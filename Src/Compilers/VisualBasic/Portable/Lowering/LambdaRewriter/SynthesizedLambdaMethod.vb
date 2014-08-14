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
                Return If(TypeOf ContainingType Is LambdaFrame, Accessibility.Friend, Accessibility.Private)
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

            MyBase.New(lambdaNode.Syntax, containingType, StringConstants.LAMBDA_PREFIX & tempNumber, isShared)
            Me.m_lambda = lambdaNode.LambdaSymbol
            Me.m_isShared = isShared
            Me.m_locations = ImmutableArray.Create(Of Location)(lambdaNode.Syntax.GetLocation())

            If Not enclosingMethod.IsGenericMethod Then
                Me.m_typeMap = Nothing
                Me.m_typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
            Else
                Dim containingTypeAsFrame = TryCast(containingType, LambdaFrame)
                If containingTypeAsFrame IsNot Nothing Then
                    Me.m_typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                    Me.m_typeMap = containingTypeAsFrame.TypeMap
                Else
                    Me.m_typeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(enclosingMethod.TypeParameters, Me, LambdaFrame.CreateTypeParameter)
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
                Dim binder As Binder = lambdaNode.LambdaBinderOpt
                Debug.Assert(binder IsNot Nothing)
                Dim syntax As VisualBasicSyntaxNode = lambdaNode.Syntax

                ' The CLR doesn't support adding fields to structs, so in order to enable EnC in an async method we need to generate a class.
                Dim typeKind As TypeKind = If(DeclaringCompilation.Options.EnableEditAndContinue, TypeKind.Class, TypeKind.Structure)

                Me.m_asyncStateMachineType =
                    AsyncRewriter.CreateAsyncStateMachine(
                        Me, 0, typeKind,
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
End Namespace