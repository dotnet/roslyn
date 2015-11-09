' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Threading

Friend Interface IMockSymbol
    Sub SetContainer(container As Symbol)
End Interface

Friend Class MockNamespaceSymbol
    Inherits NamespaceSymbol
    Implements IMockSymbol

    Private _container As NamespaceSymbol
    Private _extent As NamespaceExtent
    Private _children As ImmutableArray(Of Symbol)
    Private ReadOnly _name As String

    Public Sub New(name As String, extent As NamespaceExtent, children As IEnumerable(Of Symbol))
        Me._name = name
        Me._extent = extent
        Me._children = children.AsImmutable
    End Sub

    Public Sub SetContainer(container As Symbol) Implements IMockSymbol.SetContainer
        Me._container = DirectCast(container, NamespaceSymbol)
    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return _name
        End Get
    End Property

    Friend Overrides ReadOnly Property Extent As NamespaceExtent
        Get
            Return _extent
        End Get
    End Property

    Public Overrides Function GetModuleMembers() As ImmutableArray(Of NamedTypeSymbol)
        Return GetTypeMembers().WhereAsArray(Function(t) t.TypeKind = TypeKind.Module)
    End Function

    Public Overrides Function GetModuleMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
        Return GetTypeMembers(name).WhereAsArray(Function(t) t.TypeKind = TypeKind.Module)
    End Function

    Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
        Return _children
    End Function

    Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
        Return _children.Where(Function(ns) IdentifierComparison.Equals(ns.Name, name)).ToArray().AsImmutableOrNull
    End Function

    Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
        Return (From c In _children Where TypeOf c Is NamedTypeSymbol Select DirectCast(c, NamedTypeSymbol)).ToArray().AsImmutableOrNull()
    End Function

    Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
        Return (From c In _children Where TypeOf c Is NamedTypeSymbol AndAlso IdentifierComparison.Equals(c.Name, name) Select DirectCast(c, NamedTypeSymbol)).ToArray.AsImmutableOrNull
    End Function

    Public Overrides ReadOnly Property ContainingSymbol As Symbol
        Get
            Return _container
        End Get
    End Property

    Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
        Get
            Return _container.ContainingAssembly
        End Get
    End Property

    Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
        Get
            Return ImmutableArray.Create(Of Location)()
        End Get
    End Property

    Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
        Get
            Return ImmutableArray.Create(Of SyntaxReference)()
        End Get
    End Property

    Friend Overrides ReadOnly Property DeclaredAccessibilityOfMostAccessibleDescendantType As Accessibility
        Get
            Return GetDeclaredAccessibilityOfMostAccessibleDescendantType()
        End Get
    End Property

    Friend Overrides Sub AppendProbableExtensionMethods(name As String, methods As ArrayBuilder(Of MethodSymbol))
        Throw New NotImplementedException()
    End Sub

    Friend Overrides Sub AddExtensionMethodLookupSymbolsInfo(
        nameSet As LookupSymbolsInfo,
        options As LookupOptions,
        originalBinder As Binder,
        appendThrough As NamespaceSymbol
    )
        Throw New NotImplementedException()
    End Sub

    Friend Overrides ReadOnly Property TypesToCheckForExtensionMethods As ImmutableArray(Of NamedTypeSymbol)
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Class

Friend Class MockNamedTypeSymbol
    Inherits InstanceTypeSymbol
    Implements IMockSymbol

    Private ReadOnly _name As String
    Private ReadOnly _kind As TypeKind
    Private _children As ImmutableArray(Of Symbol)
    Private _container As NamespaceOrTypeSymbol

    Public Sub New(name As String, children As IEnumerable(Of Symbol), Optional kind As TypeKind = TypeKind.Class)
        _name = name
        _kind = kind
        _children = children.AsImmutable
    End Sub

    Public Sub SetContainer(container As Symbol) Implements IMockSymbol.SetContainer
        Me._container = DirectCast(container, NamespaceOrTypeSymbol)
    End Sub

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

    Public Overrides ReadOnly Property Arity As Integer
        Get
            Return 0
        End Get
    End Property

    Friend Overrides Function MakeDeclaredBase(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As NamedTypeSymbol
        Throw New NotImplementedException()
    End Function

    Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
        Throw New NotImplementedException()
    End Function

    Friend Overrides Function MakeAcyclicBaseType(diagnostics As DiagnosticBag) As NamedTypeSymbol
        Throw New NotImplementedException()
    End Function

    Friend Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
        Throw New NotImplementedException()
    End Function

    Public Overrides ReadOnly Property ContainingSymbol As Symbol
        Get
            Return _container
        End Get
    End Property

    Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
        Get
            Return Accessibility.Public
        End Get
    End Property

    Public Overrides ReadOnly Property EnumUnderlyingType As NamedTypeSymbol
        Get
            Throw New InvalidOperationException()
        End Get
    End Property

    Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
        Return ImmutableArray.Create(Of VisualBasicAttributeData)()
    End Function

    Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
        Throw New InvalidOperationException()
    End Function

    Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
        Get
            Return False
        End Get
    End Property

    Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Cci.SecurityAttribute)
        Throw New InvalidOperationException()
    End Function

    Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overloads Overrides Function GetMembers() As ImmutableArray(Of Symbol)
        Return _children
    End Function

    Public Overloads Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
        Return (From sym In _children Where IdentifierComparison.Equals(sym.Name, name) Select sym).ToArray.AsImmutableOrNull
    End Function

    Public Overloads Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
        Return (From sym In _children Where TypeOf sym Is NamedTypeSymbol Select DirectCast(sym, NamedTypeSymbol)).ToArray().AsImmutableOrNull()
    End Function

    Public Overloads Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
        Return (From sym In _children Where TypeOf sym Is NamedTypeSymbol AndAlso IdentifierComparison.Equals(sym.Name, name) Select DirectCast(sym, NamedTypeSymbol)).ToArray.AsImmutableOrNull()
    End Function

    Public Overloads Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
        Return (From sym In _children
                Where TypeOf sym Is NamedTypeSymbol AndAlso IdentifierComparison.Equals(sym.Name, name) AndAlso DirectCast(sym, NamedTypeSymbol).Arity = arity
                Select DirectCast(sym, NamedTypeSymbol)).ToArray.AsImmutableOrNull()
    End Function

    Friend Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
        Return SpecializedCollections.EmptyEnumerable(Of FieldSymbol)()
    End Function

    Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
        Get
            Return ImmutableArray.Create(Of Location)()
        End Get
    End Property

    Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
        Get
            Return ImmutableArray.Create(Of SyntaxReference)()
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return _name
        End Get
    End Property

    Friend Overrides ReadOnly Property MangleName As Boolean
        Get
            Return Arity > 0
        End Get
    End Property

    Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
        Get
            Return ImmutableArray.Create(Of TypeParameterSymbol)()
        End Get
    End Property

    Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
        Get
            Return Me
        End Get
    End Property

    Public Overrides ReadOnly Property IsMustInherit As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overrides ReadOnly Property IsNotInheritable As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property HasEmbeddedAttribute As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property IsComImport As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property CoClassType As TypeSymbol
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
        Throw New NotImplementedException()
    End Function

    Public Overrides ReadOnly Property TypeKind As TypeKind
        Get
            Return _kind
        End Get
    End Property

    Friend Overrides ReadOnly Property IsInterface As Boolean
        Get
            Return _kind = TypeKind.Interface
        End Get
    End Property

    Friend Overrides ReadOnly Property DefaultPropertyName As String
        Get
            Return Nothing
        End Get
    End Property

    Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
        Get
            Return Nothing
        End Get
    End Property

    ''' <summary>
    ''' Force all declaration errors to be generated.
    ''' </summary>
    Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
        Throw New InvalidOperationException()
    End Sub
End Class

Friend Class MockMethodSymbol
    Inherits MethodSymbol

    Private _name As String
    Private _container As Symbol

    Public Sub New(name As String)
        _name = name
    End Sub

    Public Overrides ReadOnly Property Arity As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property AssociatedSymbol As Symbol
        Get
            Return Nothing
        End Get
    End Property

    Friend Overrides ReadOnly Property CallingConvention As Cci.CallingConvention
        Get
            Return Cci.CallingConvention.Standard
        End Get
    End Property

    Public Overrides ReadOnly Property ContainingSymbol As Symbol
        Get
            Return _container
        End Get
    End Property

    Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
        Get
            Return Accessibility.Public
        End Get
    End Property

    Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
        Get
            Return ImmutableArray.Create(Of MethodSymbol)()
        End Get
    End Property

    Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
        Return ImmutableArray.Create(Of VisualBasicAttributeData)()
    End Function

    Public Overrides ReadOnly Property IsExtensionMethod As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overrides ReadOnly Property IsExternalMethod As Boolean
        Get
            Return False
        End Get
    End Property

    Public NotOverridable Overrides Function GetDllImportData() As DllImportData
        Return Nothing
    End Function

    Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
        Get
            Return Nothing
        End Get
    End Property

    Friend Overrides ReadOnly Property ImplementationAttributes As MethodImplAttributes
        Get
            Return Nothing
        End Get
    End Property

    Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
        Get
            Return False
        End Get
    End Property

    Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Cci.SecurityAttribute)
        Throw New InvalidOperationException()
    End Function

    Public Overrides ReadOnly Property IsGenericMethod As Boolean
        Get
            Return False
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
            Return True
        End Get
    End Property

    Public Overrides ReadOnly Property IsSub As Boolean
        Get
            Return True
        End Get
    End Property

    Public Overrides ReadOnly Property IsAsync As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overrides ReadOnly Property IsIterator As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overrides ReadOnly Property IsVararg As Boolean
        Get
            Return False
        End Get
    End Property

    Friend Overrides ReadOnly Property HasSpecialName As Boolean
        Get
            Return False
        End Get
    End Property

    Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
        Throw New NotImplementedException()
    End Function

    Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
        Get
            Return ImmutableArray.Create(Of Location)()
        End Get
    End Property

    Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
        Get
            Return ImmutableArray.Create(Of SyntaxReference)()
        End Get
    End Property

    Public Overrides ReadOnly Property MethodKind As MethodKind
        Get
            Return MethodKind.Ordinary
        End Get
    End Property

    Friend Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
        Get
            Return ImmutableArray.Create(Of ParameterSymbol)()
        End Get
    End Property

    Public Overrides ReadOnly Property ReturnType As TypeSymbol
        Get
            Return Nothing ' Not really kosher, but its a MOCK...
        End Get
    End Property

    Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
        Get
            Return ImmutableArray.Create(Of CustomModifier)()
        End Get
    End Property

    Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
        Get
            Return ImmutableArray.Create(Of TypeSymbol)()
        End Get
    End Property

    Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
        Get
            Return ImmutableArray.Create(Of TypeParameterSymbol)()
        End Get
    End Property

    Friend Overrides ReadOnly Property Syntax As VisualBasicSyntaxNode
        Get
            Return Nothing
        End Get
    End Property

    Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
        Get
            Return Nothing
        End Get
    End Property

    Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
        Throw ExceptionUtilities.Unreachable
    End Function
End Class

Friend Class MockModuleSymbol
    Inherits NonMissingModuleSymbol

    Private ReadOnly _name As String
    Private ReadOnly _assembly As AssemblySymbol

    Public Sub New(name As String, assembly As AssemblySymbol)
        _name = name
        _assembly = assembly
    End Sub

    Friend Overrides ReadOnly Property Ordinal As Integer
        Get
            Return -1
        End Get
    End Property

    Friend Overrides ReadOnly Property Machine As System.Reflection.PortableExecutable.Machine
        Get
            Return System.Reflection.PortableExecutable.Machine.I386
        End Get
    End Property

    Friend Overrides ReadOnly Property Bit32Required As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return _name
        End Get
    End Property

    Public Overrides ReadOnly Property ContainingSymbol As Symbol
        Get
            Return _assembly
        End Get
    End Property

    Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
        Return ImmutableArray.Create(Of VisualBasicAttributeData)()
    End Function

    Public Overrides ReadOnly Property GlobalNamespace As NamespaceSymbol
        Get
            Return New MockNamespaceSymbol("", New NamespaceExtent(Me), Enumerable.Empty(Of Symbol))
        End Get
    End Property

    Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
        Get
            Return ImmutableArray.Create(Of Location)()
        End Get
    End Property

    Friend Overrides ReadOnly Property TypeNames As ICollection(Of String)
        Get
            Return SpecializedCollections.EmptyCollection(Of String)()
        End Get
    End Property

    Friend Overrides ReadOnly Property NamespaceNames As ICollection(Of String)
        Get
            Return SpecializedCollections.EmptyCollection(Of String)()
        End Get
    End Property

    Friend Overrides ReadOnly Property MightContainExtensionMethods As Boolean
        Get
            Return False
        End Get
    End Property

    Friend Overrides ReadOnly Property HasAssemblyCompilationRelaxationsAttribute As Boolean
        Get
            Return False
        End Get
    End Property

    Friend Overrides ReadOnly Property HasAssemblyRuntimeCompatibilityAttribute As Boolean
        Get
            Return False
        End Get
    End Property

    Friend Overrides ReadOnly Property DefaultMarshallingCharSet As CharSet?
        Get
            Return Nothing
        End Get
    End Property
End Class

Friend Class MockAssemblySymbol
    Inherits NonMissingAssemblySymbol

    Private ReadOnly _name As String
    Private ReadOnly _module As ModuleSymbol

    Public Sub New(name As String)
        _name = name
        _module = New MockModuleSymbol(name, Me)
    End Sub

    Public Overrides ReadOnly Property Identity As AssemblyIdentity
        Get
            Return New AssemblyIdentity(_name)
        End Get
    End Property

    Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
        Return ImmutableArray.Create(Of VisualBasicAttributeData)()
    End Function

    Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
        Get
            Return ImmutableArray.Create(Of Location)()
        End Get
    End Property

    Public Overrides ReadOnly Property Modules As ImmutableArray(Of ModuleSymbol)
        Get
            Return ImmutableArray.Create(_module)
        End Get
    End Property

    Friend Overrides Function GetDeclaredSpecialType(type As SpecialType) As NamedTypeSymbol
        Throw New NotImplementedException()
    End Function

    Public Overrides ReadOnly Property TypeNames As ICollection(Of String)
        Get
            Return SpecializedCollections.EmptyCollection(Of String)()
        End Get
    End Property

    Public Overrides ReadOnly Property NamespaceNames As ICollection(Of String)
        Get
            Return SpecializedCollections.EmptyCollection(Of String)()
        End Get
    End Property

    Friend Overrides Function GetNoPiaResolutionAssemblies() As ImmutableArray(Of AssemblySymbol)
        Return CType(Nothing, ImmutableArray(Of AssemblySymbol))
    End Function

    Friend Overrides Sub SetNoPiaResolutionAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
        Throw New NotImplementedException()
    End Sub

    Friend Overrides Function GetLinkedReferencedAssemblies() As ImmutableArray(Of AssemblySymbol)
        Return Nothing
    End Function

    Friend Overrides Sub SetLinkedReferencedAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
        Throw New NotImplementedException()
    End Sub

    Friend Overrides Function GetInternalsVisibleToPublicKeys(simpleName As String) As IEnumerable(Of ImmutableArray(Of Byte))
        Throw New NotImplementedException()
    End Function

    Friend Overrides Function AreInternalsVisibleToThisAssembly(potentialGiverOfAccess As AssemblySymbol) As Boolean
        Throw New NotImplementedException()
    End Function

    Friend Overrides ReadOnly Property PublicKey As ImmutableArray(Of Byte)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Friend Overrides ReadOnly Property IsLinked As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
        Get
            Return False
        End Get
    End Property

    Friend Overrides Function TryLookupForwardedMetadataTypeWithCycleDetection(ByRef emittedName As MetadataTypeName, visitedAssemblies As ConsList(Of AssemblySymbol), ignoreCase As Boolean) As NamedTypeSymbol
        Return Nothing
    End Function
End Class
