' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' When signature of an implementing method differs (due to presence of custom modifiers)
    ''' from the signature of implemented method, we insert a synthesized explicit interface 
    ''' implementation that delegates to the method declared in source.
    ''' </summary>
    Friend NotInheritable Class SynthesizedInterfaceImplementationStubSymbol
        Inherits SynthesizedMethodBase

        Private ReadOnly _name As String
        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly _typeParametersSubstitution As TypeSubstitution
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _returnType As TypeSymbol
        Private ReadOnly _customModifiers As ImmutableArray(Of CustomModifier)

        Private _explicitInterfaceImplementationsBuilder As ArrayBuilder(Of MethodSymbol) = ArrayBuilder(Of MethodSymbol).GetInstance()
        Private _explicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)

        Private Shared ReadOnly s_typeParametersSubstitutionFactory As Func(Of Symbol, TypeSubstitution) =
                Function(container) DirectCast(container, SynthesizedInterfaceImplementationStubSymbol)._typeParametersSubstitution

        Private Shared ReadOnly s_createTypeParameter As Func(Of TypeParameterSymbol, Symbol, TypeParameterSymbol) =
                Function(typeParameter, container) New SynthesizedClonedTypeParameterSymbol(typeParameter, container, typeParameter.Name, s_typeParametersSubstitutionFactory)

        Friend Sub New(implementingMethod As MethodSymbol, implementedMethod As MethodSymbol)
            MyBase.New(implementingMethod.ContainingType)

            _name = "$VB$Stub_" & implementingMethod.MetadataName

            _typeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(implementingMethod.TypeParameters, Me, s_createTypeParameter)
            _typeParametersSubstitution = TypeSubstitution.Create(implementingMethod, implementingMethod.TypeParameters, StaticCast(Of TypeSymbol).From(_typeParameters))

            If implementedMethod.IsGenericMethod Then
                implementedMethod = implementedMethod.Construct(StaticCast(Of TypeSymbol).From(_typeParameters))
            End If

            Dim builder = ArrayBuilder(Of ParameterSymbol).GetInstance()
            For Each p As ParameterSymbol In implementingMethod.Parameters
                Dim implementedParameter = implementedMethod.Parameters(p.Ordinal)
                builder.Add(SynthesizedParameterSymbol.Create(Me, implementedParameter.Type, p.Ordinal, p.IsByRef, p.Name,
                                                              implementedParameter.CustomModifiers, implementedParameter.RefCustomModifiers))
            Next

            _parameters = builder.ToImmutableAndFree()
            _returnType = implementedMethod.ReturnType
            _customModifiers = implementedMethod.ReturnTypeCustomModifiers
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return _typeParameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _typeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return StaticCast(Of TypeSymbol).From(Me.TypeParameters)
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _customModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Sub AddImplementedMethod(implemented As MethodSymbol)
            _explicitInterfaceImplementationsBuilder.Add(implemented)
        End Sub

        Public Sub Seal()
            Debug.Assert(_explicitInterfaceImplementations.IsDefault)
            _explicitInterfaceImplementations = _explicitInterfaceImplementationsBuilder.ToImmutableAndFree()
            _explicitInterfaceImplementationsBuilder = Nothing
        End Sub

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Debug.Assert(Not _explicitInterfaceImplementations.IsDefault)
                Return _explicitInterfaceImplementations
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
                Return _returnType.IsVoidType()
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

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            Dim compilation = Me.DeclaringCompilation

            AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
            AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerHiddenAttribute())
        End Sub

        Friend Overrides Sub AddSynthesizedReturnTypeAttributes(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedReturnTypeAttributes(attributes)

            Dim compilation = Me.DeclaringCompilation
            If Me.ReturnType.ContainsTupleNames() AndAlso
                compilation.HasTupleNamesAttributes Then

                AddSynthesizedAttribute(attributes, compilation.SynthesizeTupleNamesAttribute(Me.ReturnType))
            End If
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
