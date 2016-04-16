' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend MustInherit Class SynthesizedContainer
        Inherits InstanceTypeSymbol

        Private ReadOnly _containingType As NamedTypeSymbol
        Private ReadOnly _baseType As NamedTypeSymbol
        Private ReadOnly _name As String
        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly _interfaces As ImmutableArray(Of NamedTypeSymbol)
        Private ReadOnly _typeMap As TypeSubstitution

        Private Shared ReadOnly s_typeSubstitutionFactory As Func(Of Symbol, TypeSubstitution) =
            Function(container) DirectCast(container, SynthesizedContainer).TypeSubstitution

        Private Shared ReadOnly s_createTypeParameter As Func(Of TypeParameterSymbol, Symbol, TypeParameterSymbol) =
            Function(typeParameter, container) New SynthesizedClonedTypeParameterSymbol(
                                                            typeParameter, container,
                                                            StringConstants.StateMachineTypeParameterPrefix & typeParameter.Name,
                                                            s_typeSubstitutionFactory)

        Protected Friend Sub New(topLevelMethod As MethodSymbol,
                                 typeName As String,
                                 baseType As NamedTypeSymbol,
                                 originalInterfaces As ImmutableArray(Of NamedTypeSymbol))

            Me._containingType = topLevelMethod.ContainingType
            Me._name = typeName

            Me._baseType = baseType

            If Not topLevelMethod.IsGenericMethod Then
                Me._typeMap = Nothing
                Me._typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                Me._interfaces = originalInterfaces
            Else
                Me._typeParameters =
                    SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(
                        topLevelMethod.OriginalDefinition.TypeParameters, Me, s_createTypeParameter)

                Dim typeArgs(Me._typeParameters.Length - 1) As TypeSymbol
                For ind = 0 To Me._typeParameters.Length - 1
                    typeArgs(ind) = Me._typeParameters(ind)
                Next

                Dim newConstructedWrappedMethod As MethodSymbol = topLevelMethod.Construct(typeArgs.AsImmutableOrNull())

                Me._typeMap = TypeSubstitution.Create(newConstructedWrappedMethod.OriginalDefinition,
                                                     newConstructedWrappedMethod.OriginalDefinition.TypeParameters,
                                                     typeArgs.AsImmutableOrNull())

                Me._interfaces = originalInterfaces.SelectAsArray(Function(i) DirectCast(i.InternalSubstituteTypeParameters(Me._typeMap).AsTypeSymbolOnly(), NamedTypeSymbol))
            End If
        End Sub

        Protected Friend MustOverride ReadOnly Property Constructor As MethodSymbol

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return Me._name
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property MangleName As Boolean
            Get
                Return Me._typeParameters.Length > 0
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property Layout As TypeLayout
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property MarshallingCharSet As System.Runtime.InteropServices.CharSet
            Get
                Return DefaultMarshallingCharSet
            End Get
        End Property

        Public MustOverride Overrides ReadOnly Property TypeKind As TypeKind

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return Me._typeParameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return Me._typeParameters
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsMustInherit As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasEmbeddedAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsComImport As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property CoClassType As TypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return ImmutableArray(Of String).Empty
        End Function

        Friend NotOverridable Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
            Throw ExceptionUtilities.Unreachable()
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend NotOverridable Overrides ReadOnly Property DefaultPropertyName As String
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As DiagnosticBag) As NamedTypeSymbol
            Return Me._baseType
        End Function

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As NamedTypeSymbol
            Return MakeAcyclicBaseType(diagnostics)
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return Me._interfaces
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return MakeAcyclicInterfaces(diagnostics)
        End Function

        Public Overloads Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Debug.Assert(Me.Constructor IsNot Nothing)
            Return ImmutableArray.Create(Of Symbol)(Me.Constructor)
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return If(CaseInsensitiveComparison.Equals(name, WellKnownMemberNames.InstanceConstructorName),
                      ImmutableArray.Create(Of Symbol)(Me.Constructor), ImmutableArray(Of Symbol).Empty)
        End Function

        Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
            Get
                Return SpecializedCollections.SingletonEnumerable(Of String)(WellKnownMemberNames.InstanceConstructorName)
            End Get
        End Property

        Friend Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            Return SpecializedCollections.EmptyEnumerable(Of FieldSymbol)()
        End Function

        Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me._containingType
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return Me._containingType
            End Get
        End Property

        Public NotOverridable Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public NotOverridable Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public NotOverridable Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Private
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            Throw ExceptionUtilities.Unreachable
        End Sub

        Public NotOverridable Overrides ReadOnly Property IsNotInheritable As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return Me._typeMap
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(compilationState as ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            Dim compilation = Me.DeclaringCompilation

            Debug.Assert(
                WellKnownMembers.IsSynthesizedAttributeOptional(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
            AddSynthesizedAttribute(attributes,
                                    compilation.TrySynthesizeAttribute(
                                        WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
        End Sub

    End Class
End Namespace
