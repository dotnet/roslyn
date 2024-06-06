' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The base class to represent a namespace imported from a PE/module.
    ''' Namespaces that differ only by casing in name are merged.
    ''' </summary>
    Friend MustInherit Class PENamespaceSymbol
        Inherits PEOrSourceOrMergedNamespaceSymbol

        ''' <summary>
        ''' A map of namespaces immediately contained within this namespace 
        ''' grouped by their name (case-insensitively).
        ''' </summary>
        Protected m_lazyMembers As Dictionary(Of String, ImmutableArray(Of Symbol))

        ''' <summary>
        ''' A map of types immediately contained within this namespace 
        ''' grouped by their name (case-insensitively).
        ''' </summary>
        Protected m_lazyTypes As Dictionary(Of String, ImmutableArray(Of PENamedTypeSymbol))

        ''' <summary>
        ''' A map of NoPia local types immediately contained in this assembly.
        ''' Maps fully-qualified type name to the row id.
        ''' </summary>
        Private _lazyNoPiaLocalTypes As Dictionary(Of String, TypeDefinitionHandle)

        ' Lazily filled in collection of all contained modules.
        Private _lazyModules As ImmutableArray(Of NamedTypeSymbol)

        ' Lazily filled in collection of all contained types.
        Private _lazyFlattenedTypes As ImmutableArray(Of NamedTypeSymbol)

        ' Lazily filled in collection of all contained namespaces and types.
        Private _lazyFlattenedNamespacesAndTypes As ImmutableArray(Of Symbol)

        Friend NotOverridable Overrides ReadOnly Property Extent As NamespaceExtent
            Get
                Return New NamespaceExtent(Me.ContainingPEModule)
            End Get
        End Property

        Public Overrides Function GetModuleMembers() As ImmutableArray(Of NamedTypeSymbol)
            ' Since this gets called a LOT during binding, it's worth caching the result.

            If _lazyModules.IsDefault Then
                ' We have to read all the types to discover which ones are modules, so I'm not 
                ' sure there is any better strategy on first call then getting all type members
                ' and filtering them.
                ' Ordered.
                Dim modules = GetTypeMembers().WhereAsArray(Function(t) t.TypeKind = TYPEKIND.Module)
                ImmutableInterlocked.InterlockedCompareExchange(_lazyModules, modules, Nothing)
            End If

            Return _lazyModules
        End Function

        Public Overrides Function GetModuleMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            ' This is not called during binding, so caching isn't very critical.
            Return GetTypeMembers(name).WhereAsArray(Function(t) t.TypeKind = TYPEKIND.Module)
        End Function

        Public NotOverridable Overloads Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            If _lazyFlattenedNamespacesAndTypes.IsDefault Then
                EnsureAllMembersLoaded()
                _lazyFlattenedNamespacesAndTypes = m_lazyMembers.Flatten()
            End If

            Return _lazyFlattenedNamespacesAndTypes
        End Function

        Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                Return EmbeddedSymbolKind.None
            End Get
        End Property

        Public NotOverridable Overloads Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            EnsureAllMembersLoaded()

            Dim m As ImmutableArray(Of Symbol) = Nothing

            If m_lazyMembers.TryGetValue(name, m) Then
                Return m
            End If

            Return ImmutableArray(Of Symbol).Empty
        End Function

        Public NotOverridable Overloads Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Dim result = _lazyFlattenedTypes
            If Not result.IsDefault Then
                Return result
            End If

            EnsureAllMembersLoaded()
            result = StaticCast(Of NamedTypeSymbol).From(m_lazyTypes.Flatten())

            _lazyFlattenedTypes = result
            Return result
        End Function

        Public NotOverridable Overloads Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            EnsureAllMembersLoaded()

            Dim t As ImmutableArray(Of PENamedTypeSymbol) = Nothing

            If m_lazyTypes.TryGetValue(name, t) Then
                Return StaticCast(Of NamedTypeSymbol).From(t)
            End If

            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overloads Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return GetTypeMembers(name).WhereAsArray(Function(type, arity_) type.Arity = arity_, arity)
        End Function

        Public NotOverridable Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return StaticCast(Of Location).From(ContainingPEModule.MetadataLocation)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        ''' <summary>
        ''' Returns PEModuleSymbol containing the namespace.
        ''' </summary>
        ''' <returns>PEModuleSymbol containing the namespace.</returns>
        Friend MustOverride ReadOnly Property ContainingPEModule As PEModuleSymbol

        Protected MustOverride Sub EnsureAllMembersLoaded()

        ''' <summary>
        ''' Initializes m_Namespaces and m_Types maps with information about 
        ''' namespaces and types immediately contained within this namespace.
        ''' </summary>
        ''' <param name="typesByNS">
        ''' The sequence of groups of TypeDef row ids for types contained within the namespace, 
        ''' recursively including those from nested namespaces. The row ids must be grouped by the 
        ''' fully-qualified namespace name in case-sensitive manner. There could be multiple groups 
        ''' for each fully-qualified namespace name. The groups must be sorted by their key in 
        ''' case-insensitive manner. Empty string must be used as namespace name for types 
        ''' immediately contained within Global namespace. Therefore, all types in THIS namespace, 
        ''' if any, must be in several first IGroupings.
        ''' </param>
        Protected Sub LoadAllMembers(typesByNS As IEnumerable(Of IGrouping(Of String, TypeDefinitionHandle)))
            Debug.Assert(typesByNS IsNot Nothing)

            ' A sequence of TypeDef handles for types immediately contained within this namespace.
            Dim nestedTypes As IEnumerable(Of IGrouping(Of String, TypeDefinitionHandle)) = Nothing

            ' A sequence with information about namespaces immediately contained within this namespace.
            ' For each pair:
            '    Key - contains simple name of a child namespace.
            '    Value - contains a sequence similar to the one passed to this function, but
            '            calculated for the child namespace. 
            Dim nestedNamespaces As IEnumerable(Of KeyValuePair(Of String, IEnumerable(Of IGrouping(Of String, TypeDefinitionHandle)))) = Nothing

            'TODO: Perhaps there is a cheaper way to calculate the length of the name without actually building it with ToDisplayString.
            Dim isGlobalNamespace As Boolean = Me.IsGlobalNamespace

            MetadataHelpers.GetInfoForImmediateNamespaceMembers(
                isGlobalNamespace,
                If(isGlobalNamespace, 0, ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat).Length),
                typesByNS,
                CaseInsensitiveComparison.Comparer,
                nestedTypes, nestedNamespaces)

            LazyInitializeTypes(nestedTypes)

            LazyInitializeNamespaces(nestedNamespaces)
        End Sub

        ''' <summary>
        ''' Create symbols for nested namespaces and initialize m_Namespaces map.
        ''' </summary>
        Private Sub LazyInitializeNamespaces(
            childNamespaces As IEnumerable(Of KeyValuePair(Of String, IEnumerable(Of IGrouping(Of String, TypeDefinitionHandle))))
        )
            If m_lazyMembers Is Nothing Then

                Dim members As New Dictionary(Of String, ImmutableArray(Of Symbol))(CaseInsensitiveComparison.Comparer)

                ' Add namespaces
                For Each child In childNamespaces
                    Dim ns = New PENestedNamespaceSymbol(child.Key, Me, child.Value)
                    members.Add(ns.Name, ImmutableArray.Create(Of Symbol)(ns))
                Next

                ' Merge in the types

                For Each typeSymbols As ImmutableArray(Of PENamedTypeSymbol) In m_lazyTypes.Values
                    Dim name = typeSymbols(0).Name
                    Dim symbols As ImmutableArray(Of Symbol) = Nothing

                    If Not members.TryGetValue(name, symbols) Then
                        members.Add(name, StaticCast(Of Symbol).From(typeSymbols))
                    Else
                        members(name) = symbols.Concat(StaticCast(Of Symbol).From(typeSymbols))
                    End If
                Next

                Interlocked.CompareExchange(m_lazyMembers, members, Nothing)
            End If
        End Sub

        ''' <summary>
        ''' Create symbols for nested types and initialize m_Types map.
        ''' </summary>
        Private Sub LazyInitializeTypes(typeGroups As IEnumerable(Of IGrouping(Of String, TypeDefinitionHandle)))

            If m_lazyTypes Is Nothing Then

                Dim moduleSymbol = ContainingPEModule
                Dim children = ArrayBuilder(Of PENamedTypeSymbol).GetInstance()
                Dim skipCheckForPiaType = Not moduleSymbol.Module.ContainsNoPiaLocalTypes()
                Dim noPiaLocalTypes As Dictionary(Of String, TypeDefinitionHandle) = Nothing
                Dim isGlobal = Me.IsGlobalNamespace

                For Each g In typeGroups
                    For Each t In g
                        If skipCheckForPiaType OrElse Not moduleSymbol.Module.IsNoPiaLocalType(t) Then
                            Dim type = If(isGlobal,
                                          New PENamedTypeSymbol(moduleSymbol, Me, t),
                                          New PENamedTypeSymbolWithEmittedNamespaceName(moduleSymbol, Me, t, g.Key))
                            children.Add(type)
                        Else
                            ' The dictionary of NoPIA local types must be indexed by fully-qualified names
                            ' (namespace + type name), and key comparison must be case-sensitive. The
                            ' reason is this PENamespaceSymbol is a merged namespace of all namespaces
                            ' from the same module in metadata but with potentially different casing.
                            ' When resolving a NoPIA type, the casing must match however, so it is not
                            ' sufficient to simply use the type name. In C#, namespaces with different
                            ' casing are not merged so type name is sufficient there.
                            Try
                                Dim typeDefName As String = moduleSymbol.Module.GetTypeDefNameOrThrow(t)

                                If noPiaLocalTypes Is Nothing Then
                                    noPiaLocalTypes = New Dictionary(Of String, TypeDefinitionHandle)()
                                End If
                                Dim qualifiedName = MetadataHelpers.BuildQualifiedName(g.Key, typeDefName)
                                noPiaLocalTypes(qualifiedName) = t
                            Catch mrEx As BadImageFormatException
                            End Try
                        End If
                    Next
                Next

                Dim typesDict As Dictionary(Of String, ImmutableArray(Of PENamedTypeSymbol)) =
                    children.ToDictionary(Function(c) c.Name, CaseInsensitiveComparison.Comparer)
                children.Free()

                If _lazyNoPiaLocalTypes Is Nothing Then
                    Interlocked.CompareExchange(_lazyNoPiaLocalTypes, noPiaLocalTypes, Nothing)
                End If

                If Interlocked.CompareExchange(m_lazyTypes, typesDict, Nothing) Is Nothing Then
                    ' Build cache of TypeDef Tokens
                    ' Potentially this can be done in the background.
                    moduleSymbol.OnNewTypeDeclarationsLoaded(typesDict)
                End If
            End If
        End Sub

        ''' <summary>
        ''' For test purposes only.
        ''' </summary>
        Friend ReadOnly Property AreTypesLoaded As Boolean
            Get
                Return m_lazyTypes IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' Return the set of types that should be checked for presence of extension methods in order to build
        ''' a map of extension methods for the namespace. 
        ''' </summary>
        Friend Overrides ReadOnly Property TypesToCheckForExtensionMethods As ImmutableArray(Of NamedTypeSymbol)
            Get
                If ContainingPEModule.MightContainExtensionMethods Then
                    ' Note that we are using GetTypeMembers rather than GetModuleMembers because non-Modules imported 
                    ' from metadata can contain extension methods.
                    Return Me.GetTypeMembers() ' Ordered.
                End If

                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Get
        End Property

        Friend Function UnifyIfNoPiaLocalType(ByRef emittedTypeName As MetadataTypeName) As NamedTypeSymbol
            EnsureAllMembersLoaded()
            Dim typeDef As TypeDefinitionHandle = Nothing

            ' See if this is a NoPia local type, which we should unify.
            If _lazyNoPiaLocalTypes IsNot Nothing AndAlso
                _lazyNoPiaLocalTypes.TryGetValue(emittedTypeName.FullName, typeDef) Then

                Dim isNoPiaLocalType As Boolean
                Dim result = DirectCast(New MetadataDecoder(ContainingPEModule).GetTypeOfToken(typeDef, isNoPiaLocalType), NamedTypeSymbol)
                Debug.Assert(isNoPiaLocalType)
                Debug.Assert(result IsNot Nothing)
                Return result
            End If

            Return Nothing
        End Function

    End Class

End Namespace
