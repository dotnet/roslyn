' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

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

            _containingType = topLevelMethod.ContainingType
            _name = typeName

            _baseType = baseType

            If Not topLevelMethod.IsGenericMethod Then
                _typeMap = Nothing
                _typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                _interfaces = originalInterfaces
            Else
                _typeParameters =
                    SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(
                        topLevelMethod.OriginalDefinition.TypeParameters, Me, s_createTypeParameter)

                Dim typeArgs(_typeParameters.Length - 1) As TypeSymbol
                For ind = 0 To _typeParameters.Length - 1
                    typeArgs(ind) = _typeParameters(ind)
                Next

                Dim newConstructedWrappedMethod As MethodSymbol = topLevelMethod.Construct(typeArgs.AsImmutableOrNull())

                _typeMap = TypeSubstitution.Create(newConstructedWrappedMethod.OriginalDefinition,
                                                     newConstructedWrappedMethod.OriginalDefinition.TypeParameters,
                                                     typeArgs.AsImmutableOrNull())

                _interfaces = originalInterfaces.SelectAsArray(Function(i) DirectCast(i.InternalSubstituteTypeParameters(_typeMap).AsTypeSymbolOnly(), NamedTypeSymbol))
            End If
        End Sub

        Protected Friend MustOverride ReadOnly Property Constructor As MethodSymbol

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property MangleName As Boolean
            Get
                Return _typeParameters.Length > 0
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
                Return _typeParameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _typeParameters
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
            Return _baseType
        End Function

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As NamedTypeSymbol
            Return MakeAcyclicBaseType(diagnostics)
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return _interfaces
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return MakeAcyclicInterfaces(diagnostics)
        End Function

        Public Overloads Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Debug.Assert(Constructor IsNot Nothing)
            Return ImmutableArray.Create(Of Symbol)(Constructor)
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return If(CaseInsensitiveComparison.Equals(name, WellKnownMemberNames.InstanceConstructorName),
                      ImmutableArray.Create(Of Symbol)(Constructor), ImmutableArray(Of Symbol).Empty)
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
                Return _containingType
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _containingType
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
                Return _typeMap
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            Dim compilation = DeclaringCompilation

            Debug.Assert(
                WellKnownMembers.IsSynthesizedAttributeOptional(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
            AddSynthesizedAttribute(attributes,
                                    compilation.TrySynthesizeAttribute(
                                        WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
        End Sub

    End Class
End Namespace
