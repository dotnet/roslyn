' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend NotInheritable Class EENamedTypeSymbol
        Inherits InstanceTypeSymbol

        Friend ReadOnly SubstitutedSourceType As NamedTypeSymbol
        Friend ReadOnly SourceTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Private ReadOnly _container As NamespaceSymbol
        Private ReadOnly _baseType As NamedTypeSymbol
        Private ReadOnly _name As String
        Private ReadOnly _syntax As VisualBasicSyntaxNode
        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly _methods As ImmutableArray(Of MethodSymbol)

        Friend Sub New(
            container As NamespaceSymbol,
            baseType As NamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            currentFrame As MethodSymbol,
            typeName As String,
            methodName As String,
            context As CompilationContext,
            generateMethodBody As GenerateMethodBody)

            MyClass.New(container, baseType, syntax, currentFrame, typeName, Function(m, t) ImmutableArray.Create(Of MethodSymbol)(context.CreateMethod(t, methodName, syntax, generateMethodBody)))
        End Sub

        Friend Sub New(
            container As NamespaceSymbol,
            baseType As NamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            currentFrame As MethodSymbol,
            typeName As String,
            getMethods As Func(Of MethodSymbol, EENamedTypeSymbol, ImmutableArray(Of MethodSymbol)),
            sourceTypeParameters As ImmutableArray(Of TypeParameterSymbol),
            getTypeParameters As Func(Of NamedTypeSymbol, EENamedTypeSymbol, ImmutableArray(Of TypeParameterSymbol)))

            _container = container
            _baseType = baseType
            _syntax = syntax
            _name = typeName
            Me.SourceTypeParameters = sourceTypeParameters
            _typeParameters = getTypeParameters(currentFrame.ContainingType, Me)
            VerifyTypeParameters(Me, _typeParameters)
            _methods = getMethods(currentFrame, Me)
        End Sub

        Friend Sub New(
            container As NamespaceSymbol,
            baseType As NamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            currentFrame As MethodSymbol,
            typeName As String,
            getMethods As Func(Of MethodSymbol, EENamedTypeSymbol, ImmutableArray(Of MethodSymbol)))

            _container = container
            _baseType = baseType
            _syntax = syntax
            _name = typeName

            ' What we want is to map all original type parameters to the corresponding new type parameters
            ' (since the old ones have the wrong owners).  Unfortunately, we have a circular dependency:
            '   1) Each new type parameter requires the entire map in order to be able to construct its constraint list.
            '   2) The map cannot be constructed until all new type parameters exist.
            ' Our solution is to pass each new type parameter a lazy reference to the type map.  We then 
            ' initialize the map as soon as the new type parameters are available - and before they are 
            ' handed out - so that there is never a period where they can require the type map and find
            ' it uninitialized.

            Dim sourceType = currentFrame.ContainingType
            Me.SourceTypeParameters = sourceType.GetAllTypeParameters()

            Dim typeMap As TypeSubstitution = Nothing
            Dim getTypeMap = New Func(Of TypeSubstitution)(Function() typeMap)
            _typeParameters = SourceTypeParameters.SelectAsArray(
                Function(tp As TypeParameterSymbol, i As Integer, arg As Object) DirectCast(New EETypeParameterSymbol(Me, tp, i, getTypeMap), TypeParameterSymbol),
                DirectCast(Nothing, Object))

            typeMap = TypeSubstitution.Create(sourceType, SourceTypeParameters, ImmutableArrayExtensions.Cast(Of TypeParameterSymbol, TypeSymbol)(_typeParameters))

            VerifyTypeParameters(Me, _typeParameters)

            Me.SubstitutedSourceType = typeMap.SubstituteNamedType(sourceType)
            TypeParameterChecker.Check(Me.SubstitutedSourceType, _typeParameters)

            _methods = getMethods(currentFrame, Me)
        End Sub

        Friend ReadOnly Property Methods As ImmutableArray(Of MethodSymbol)
            Get
                Return _methods
            End Get
        End Property

        Friend Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            Return SpecializedCollections.EmptyEnumerable(Of FieldSymbol)()
        End Function

        Friend Overrides Function GetMethodsToEmit() As IEnumerable(Of MethodSymbol)
            Return _methods
        End Function

        Friend Overrides Function GetInterfacesToEmit() As IEnumerable(Of NamedTypeSymbol)
            Return SpecializedCollections.EmptyEnumerable(Of NamedTypeSymbol)()
        End Function

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

        Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
            Get
                Return Me
            End Get
        End Property

        Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                ' No additional name mangling since CompileExpression
                ' is providing an explicit type name.
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Return ImmutableArrayExtensions.Cast(Of MethodSymbol, Symbol)(_methods)
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            ' Should not be requesting generated members by name other than constructors.
            Debug.Assert(name = WellKnownMemberNames.InstanceConstructorName OrElse name = WellKnownMemberNames.StaticConstructorName)
            Return GetMembers().WhereAsArray(Function(m) m.Name = name)
        End Function

        Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Internal
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property IsComImport As Boolean
            Get
                Return False
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
                Return CharSet.Ansi
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return TypeKind.Module
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _container
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(_syntax.GetLocation())
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

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

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As DiagnosticBag) As NamedTypeSymbol
            Return _baseType
        End Function

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As NamedTypeSymbol
            Return _baseType
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides ReadOnly Property CoClassType As TypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property DefaultPropertyName As String
            Get
                Return Nothing
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

        Friend Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            Throw ExceptionUtilities.Unreachable
        End Function

        <Conditional("DEBUG")>
        Friend Shared Sub VerifyTypeParameters(container As Symbol, typeParameters As ImmutableArray(Of TypeParameterSymbol))
            For i = 0 To typeParameters.Length - 1
                Dim typeParameter = typeParameters(i)
                Debug.Assert(typeParameter.ContainingSymbol Is container)
                Debug.Assert(typeParameter.Ordinal = i)
            Next
        End Sub

    End Class

End Namespace