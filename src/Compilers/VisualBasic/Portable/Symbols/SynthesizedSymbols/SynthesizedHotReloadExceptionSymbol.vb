' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SynthesizedHotReloadExceptionSymbol
        Inherits InstanceTypeSymbol

        Public Const NamespaceName As String = "System.Runtime.CompilerServices"
        Public Const TypeName As String = "HotReloadException"
        Public Const CodeFieldName As String = "Code"

        Private ReadOnly _baseType As NamedTypeSymbol
        Private ReadOnly _namespace As NamespaceSymbol
        Private ReadOnly _members As ImmutableArray(Of Symbol)

        Friend Sub New(
            containingNamespace As NamespaceSymbol,
            baseType As NamedTypeSymbol,
            stringType As TypeSymbol,
            intType As TypeSymbol)

            _baseType = baseType
            _namespace = containingNamespace
            _members = ImmutableArray.Create(Of Symbol)(
                New SynthesizedHotReloadExceptionConstructorSymbol(Me, stringType, intType),
                New SynthesizedFieldSymbol(Me, implicitlyDefinedBy:=Me, intType, CodeFieldName, Accessibility.Public, isReadOnly:=True, isShared:=False))
        End Sub

        Public ReadOnly Property Constructor As MethodSymbol
            Get
                Return DirectCast(_members(0), MethodSymbol)
            End Get
        End Property

        Public ReadOnly Property CodeField As FieldSymbol
            Get
                Return DirectCast(_members(1), FieldSymbol)
            End Get
        End Property

        Public Overloads Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Return _members
        End Function

        Public Overloads Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return (From m In GetMembers() Where IdentifierComparison.Equals(m.Name, name)).AsImmutable
        End Function

        Public Overloads Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overloads Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overloads Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
            Get
                Return New HashSet(Of String)(From member In GetMembers() Select member.Name)
            End Get
        End Property

        Friend Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            Return SpecializedCollections.SingletonEnumerable(CodeField)
        End Function

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _namespace
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Friend
            End Get
        End Property

        Friend Overrides ReadOnly Property DefaultPropertyName As String
            Get
                Return Nothing
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

        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return False
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

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
            Return MakeDeclaredBase(Nothing, diagnostics)
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
            Return _baseType
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasCodeAnalysisEmbeddedAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasVisualBasicEmbeddedAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasCompilerLoweringPreserveAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
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

        Friend Overrides Function GetGuidString(ByRef guidString As String) As Boolean
            guidString = Nothing
            Return False
        End Function

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return TypeName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSerializable As Boolean
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

        Public Overrides ReadOnly Property TypeKind As TypeKind
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
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        ''' <summary>
        ''' Force all declaration errors to be generated.
        ''' </summary>
        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
        End Sub

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                Return EmbeddedSymbolKind.None
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)
            Return SpecializedCollections.EmptyEnumerable(Of PropertySymbol)()
        End Function

        Friend Overrides ReadOnly Property HasAnyDeclaredRequiredMembers As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
