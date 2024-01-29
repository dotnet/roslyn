' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A <see cref="MissingAssemblySymbol"/> is a special kind of <see cref="AssemblySymbol"/> that represents
    ''' an assembly that couldn't be found.
    ''' </summary>
    Friend Class MissingAssemblySymbol
        Inherits AssemblySymbol

        Protected ReadOnly m_Identity As AssemblyIdentity
        Protected ReadOnly m_ModuleSymbol As MissingModuleSymbol

        Private _lazyModules As ImmutableArray(Of ModuleSymbol)

        Public Sub New(identity As AssemblyIdentity)
            Debug.Assert(identity IsNot Nothing)
            m_Identity = identity
            m_ModuleSymbol = New MissingModuleSymbol(Me, 0)
        End Sub

        Friend NotOverridable Overrides ReadOnly Property IsMissing As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property IsLinked As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetDeclaredSpecialTypeMember(member As SpecialMember) As Symbol
            Return Nothing
        End Function

        Public Overrides ReadOnly Property Identity As AssemblyIdentity
            Get
                Return m_Identity
            End Get
        End Property

        Public Overrides ReadOnly Property AssemblyVersionPattern As Version
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property PublicKey As ImmutableArray(Of Byte)
            Get
                Return Identity.PublicKey
            End Get
        End Property

        Public Overrides ReadOnly Property Modules As ImmutableArray(Of ModuleSymbol)
            Get
                If _lazyModules.IsDefault Then
                    _lazyModules = ImmutableArray.Create(Of ModuleSymbol)(m_ModuleSymbol)
                End If

                Return _lazyModules
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property GlobalNamespace As NamespaceSymbol
            Get
                Return m_ModuleSymbol.GlobalNamespace
            End Get
        End Property

        Public Overrides ReadOnly Property HasImportedFromTypeLibAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property HasPrimaryInteropAssemblyAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return m_Identity.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, MissingAssemblySymbol))
        End Function

        Public Overloads Function Equals(other As MissingAssemblySymbol) As Boolean
            Return other IsNot Nothing AndAlso (Me Is other OrElse m_Identity.Equals(other.m_Identity))
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Friend Overrides Sub SetLinkedReferencedAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Overrides Function GetLinkedReferencedAssemblies() As ImmutableArray(Of AssemblySymbol)
            Return ImmutableArray(Of AssemblySymbol).Empty
        End Function

        Friend Overrides Sub SetNoPiaResolutionAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Overrides Function GetNoPiaResolutionAssemblies() As ImmutableArray(Of AssemblySymbol)
            Return ImmutableArray(Of AssemblySymbol).Empty
        End Function

        Friend Overrides Function GetInternalsVisibleToPublicKeys(simpleName As String) As IEnumerable(Of ImmutableArray(Of Byte))
            Return SpecializedCollections.EmptyEnumerable(Of ImmutableArray(Of Byte))()
        End Function

        Friend Overrides Function GetInternalsVisibleToAssemblyNames() As IEnumerable(Of String)
            Return SpecializedCollections.EmptyEnumerable(Of String)()
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

        Friend Overrides Function AreInternalsVisibleToThisAssembly(other As AssemblySymbol) As Boolean
            Return False
        End Function

        Friend Overrides Function LookupDeclaredOrForwardedTopLevelMetadataType(ByRef emittedName As MetadataTypeName, visitedAssemblies As ConsList(Of AssemblySymbol)) As NamedTypeSymbol
            Return New MissingMetadataTypeSymbol.TopLevel(m_ModuleSymbol, emittedName)
        End Function

        Friend Overrides Function LookupDeclaredTopLevelMetadataType(ByRef emittedName As MetadataTypeName) As NamedTypeSymbol
            Return Nothing
        End Function

        Friend NotOverridable Overrides Function GetAllTopLevelForwardedTypes() As IEnumerable(Of NamedTypeSymbol)
            Return SpecializedCollections.EmptyEnumerable(Of NamedTypeSymbol)()
        End Function

        Friend Overrides Function GetDeclaredSpecialType(type As SpecialType) As NamedTypeSymbol
            Throw ExceptionUtilities.Unreachable
        End Function

        Public NotOverridable Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function GetMetadata() As AssemblyMetadata
            Return Nothing
        End Function

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetGuidString(ByRef guidString As String) As Boolean
            guidString = Nothing
            Return False
        End Function
    End Class

    ''' <summary>
    ''' AssemblySymbol to represent missing, for whatever reason, CorLibrary.
    ''' The symbol is created by ReferenceManager on as needed basis and is shared by all compilations
    ''' with missing CorLibraries.
    ''' </summary>
    Friend NotInheritable Class MissingCorLibrarySymbol
        Inherits MissingAssemblySymbol

        Friend Shared ReadOnly Instance As MissingCorLibrarySymbol = New MissingCorLibrarySymbol()

        ''' <summary>
        ''' An array of cached Cor types defined in this assembly.
        ''' Lazily filled by GetDeclaredSpecialType method.
        ''' </summary>
        Private _lazySpecialTypes() As NamedTypeSymbol

        Private Sub New()
            MyBase.New(New AssemblyIdentity("<Missing Core Assembly>"))
            Me.SetCorLibrary(Me)
        End Sub

        ''' <summary>
        ''' Lookup declaration for predefined CorLib type in this Assembly. Only should be
        ''' called if it is know that this is the Cor Library (mscorlib).
        ''' </summary>
        ''' <param name="type"></param>
        Friend Overrides Function GetDeclaredSpecialType(type As SpecialType) As NamedTypeSymbol
#If DEBUG Then
            For Each [module] In Me.Modules
                Debug.Assert([module].GetReferencedAssemblies().Length = 0)
            Next
#End If

            If _lazySpecialTypes Is Nothing Then
                Interlocked.CompareExchange(_lazySpecialTypes, New NamedTypeSymbol(SpecialType.Count) {}, Nothing)
            End If

            If _lazySpecialTypes(type) Is Nothing Then
                Dim emittedFullName As MetadataTypeName = MetadataTypeName.FromFullName(SpecialTypes.GetMetadataName(type), useCLSCompliantNameArityEncoding:=True)
                Dim corType As NamedTypeSymbol = New MissingMetadataTypeSymbol.TopLevel(m_ModuleSymbol, emittedFullName, type)
                Interlocked.CompareExchange(_lazySpecialTypes(type), corType, Nothing)
            End If

            Return _lazySpecialTypes(type)

        End Function
    End Class
End Namespace
