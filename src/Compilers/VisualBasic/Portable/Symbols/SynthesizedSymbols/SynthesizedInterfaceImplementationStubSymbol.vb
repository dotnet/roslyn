' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' When signature of an implementing method differs (due to presence of custom modifiers)
    ''' from the signature of implemented method, we insert a synthesized explicit interface 
    ''' implementation that delegates to the method declared in source.
    ''' </summary>
    Friend NotInheritable Class SynthesizedInterfaceImplementationStubSymbol
        Inherits SynthesizedMethodBase

        Private ReadOnly m_Name As String
        Private ReadOnly m_TypeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly m_TypeParametersSubstitution As TypeSubstitution
        Private ReadOnly m_Parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly m_ReturnType As TypeSymbol
        Private ReadOnly m_CustomModifiers As ImmutableArray(Of CustomModifier)

        Private m_ExplicitInterfaceImplementationsBuilder As ArrayBuilder(Of MethodSymbol) = ArrayBuilder(Of MethodSymbol).GetInstance()
        Private m_ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)

        Private Shared ReadOnly TypeParametersSubstitutionFactory As Func(Of Symbol, TypeSubstitution) =
                Function(container) DirectCast(container, SynthesizedInterfaceImplementationStubSymbol).m_TypeParametersSubstitution

        Private Shared ReadOnly CreateTypeParameter As Func(Of TypeParameterSymbol, Symbol, TypeParameterSymbol) =
                Function(typeParameter, container) New SynthesizedClonedTypeParameterSymbol(typeParameter, container, typeParameter.Name, TypeParametersSubstitutionFactory)

        Friend Sub New(implementingMethod As MethodSymbol, implementedMethod As MethodSymbol)
            MyBase.New(implementingMethod.ContainingType)

            m_Name = "$VB$Stub_" & implementingMethod.MetadataName

            m_TypeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(implementingMethod.TypeParameters, Me, CreateTypeParameter)
            m_TypeParametersSubstitution = TypeSubstitution.Create(implementingMethod, implementingMethod.TypeParameters, StaticCast(Of TypeSymbol).From(m_TypeParameters))

            If implementedMethod.IsGenericMethod Then
                implementedMethod = implementedMethod.Construct(StaticCast(Of TypeSymbol).From(m_TypeParameters))
            End If

            Dim builder = ArrayBuilder(Of ParameterSymbol).GetInstance()
            For Each p As ParameterSymbol In implementingMethod.Parameters
                Dim implementedParameter = implementedMethod.Parameters(p.Ordinal)
                builder.Add(New SynthesizedParameterSymbolWithCustomModifiers(Me, implementedParameter.Type, p.Ordinal, p.IsByRef, p.Name,
                                                                              implementedParameter.CustomModifiers, implementedParameter.HasByRefBeforeCustomModifiers))
            Next

            m_Parameters = builder.ToImmutableAndFree()
            m_ReturnType = implementedMethod.ReturnType
            m_CustomModifiers = implementedMethod.ReturnTypeCustomModifiers
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_Name
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return m_TypeParameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return m_TypeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return StaticCast(Of TypeSymbol).From(Me.TypeParameters)
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return m_ReturnType
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return m_CustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return m_Parameters
            End Get
        End Property

        Public Sub AddImplementedMethod(implemented As MethodSymbol)
            m_ExplicitInterfaceImplementationsBuilder.Add(implemented)
        End Sub

        Public Sub Seal()
            Debug.Assert(m_ExplicitInterfaceImplementations.IsDefault)
            m_ExplicitInterfaceImplementations = m_ExplicitInterfaceImplementationsBuilder.ToImmutableAndFree()
            m_ExplicitInterfaceImplementationsBuilder = Nothing
        End Sub

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Debug.Assert(Not m_ExplicitInterfaceImplementations.IsDefault)
                Return m_ExplicitInterfaceImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Private
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return m_ReturnType.IsVoidType()
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Ordinary
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            Dim compilation = Me.DeclaringCompilation

            AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
            AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerHiddenAttribute())
        End Sub

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
