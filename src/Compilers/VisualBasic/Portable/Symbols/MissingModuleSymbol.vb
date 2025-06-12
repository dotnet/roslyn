' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A <see cref="MissingModuleSymbol"/> is a special kind of <see cref="ModuleSymbol"/> that represents
    ''' a module that couldn't be found.
    ''' </summary>
    Friend Class MissingModuleSymbol
        Inherits ModuleSymbol

        Protected ReadOnly m_Assembly As AssemblySymbol
        Protected ReadOnly m_Ordinal As Integer
        Protected ReadOnly m_GlobalNamespace As MissingNamespaceSymbol

        Public Sub New(assembly As AssemblySymbol, ordinal As Integer)
            Debug.Assert(assembly IsNot Nothing)

            m_Assembly = assembly
            m_Ordinal = ordinal
            m_GlobalNamespace = New MissingNamespaceSymbol(Me)
        End Sub

        Friend Overrides ReadOnly Property Ordinal As Integer
            Get
                Return m_Ordinal
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
                ' Once we switch to a non-hardcoded name, GetHashCode/Equals should be adjusted.
                Return "<Missing Module>"
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return m_Assembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_Assembly
            End Get
        End Property

        Public Overrides ReadOnly Property GlobalNamespace As NamespaceSymbol
            Get
                Return m_GlobalNamespace
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return m_Assembly.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If Me Is obj Then
                Return True
            End If

            Dim other = TryCast(obj, MissingModuleSymbol)

            Return other IsNot Nothing AndAlso m_Assembly.Equals(other.m_Assembly)
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property NamespaceNames As ICollection(Of String)
            Get
                Return SpecializedCollections.EmptyCollection(Of String)()
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeNames As ICollection(Of String)
            Get
                Return SpecializedCollections.EmptyCollection(Of String)()
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsMissing As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function LookupTopLevelMetadataType(ByRef emittedName As MetadataTypeName) As NamedTypeSymbol
            Return Nothing
        End Function

        Friend Overrides Function GetReferencedAssemblies() As ImmutableArray(Of AssemblyIdentity)
            Return ImmutableArray(Of AssemblyIdentity).Empty
        End Function

        Friend Overrides Function GetReferencedAssemblySymbols() As ImmutableArray(Of AssemblySymbol)
            Return ImmutableArray(Of AssemblySymbol).Empty
        End Function

        Friend Overrides Sub SetReferences(
            moduleReferences As ModuleReferences(Of AssemblySymbol),
            Optional originatingSourceAssemblyDebugOnly As SourceAssemblySymbol = Nothing)

            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Overrides ReadOnly Property HasUnifiedReferences As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetUnificationUseSiteErrorInfo(dependentType As TypeSymbol) As DiagnosticInfo
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend NotOverridable Overrides ReadOnly Property MightContainExtensionMethods As Boolean
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

        Public Overrides Function GetMetadata() As ModuleMetadata
            Return Nothing
        End Function

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

    End Class

    Friend Class MissingModuleSymbolWithName
        Inherits MissingModuleSymbol

        Private ReadOnly _name As String

        Public Sub New(assembly As AssemblySymbol, name As String)
            MyBase.New(assembly, -1)

            Debug.Assert(name IsNot Nothing)

            _name = name
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(m_Assembly.GetHashCode(), StringComparer.OrdinalIgnoreCase.GetHashCode(_name))
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If Me Is obj Then
                Return True
            End If

            Dim other = TryCast(obj, MissingModuleSymbolWithName)

            Return other IsNot Nothing AndAlso m_Assembly.Equals(other.m_Assembly) AndAlso String.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase)
        End Function
    End Class
End Namespace
