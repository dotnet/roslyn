' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A wrapper around RootSingleNamespaceDeclaration. The namespace declaration
    ''' is evaluated lazily to avoid evaluating the namespace and associated SyntaxTree
    ''' for embedded syntax trees before we can determine whether the syntax tree is needed.
    ''' </summary>
    Friend NotInheritable Class DeclarationTableEntry
        Public ReadOnly Root As Lazy(Of RootSingleNamespaceDeclaration)
        Public ReadOnly IsEmbedded As Boolean

        Public Sub New(root As Lazy(Of RootSingleNamespaceDeclaration), isEmbedded As Boolean)
            Me.Root = root
            Me.IsEmbedded = isEmbedded
        End Sub
    End Class

    ''' <summary>
    ''' A declaration table is a device which keeps track of type and namespace declarations from
    ''' parse trees. It is optimized for the case where there is one set of declarations that stays
    ''' constant, and a specific root namespace declaration corresponding to the currently edited
    ''' file which is being added and removed repeatedly. It maintains a cache of information for
    ''' "merging" the root declarations into one big summary declaration; this cache is efficiently
    ''' re-used provided that the pattern of adds and removes is as we expect.
    ''' </summary>
    Partial Friend Class DeclarationTable
        Public Shared ReadOnly Empty As DeclarationTable = New DeclarationTable(
                                                           ImmutableHashSet.Create(Of DeclarationTableEntry)(),
                                                           latestLazyRootDeclaration:=Nothing,
                                                           cache:=Nothing)

        ' All our root declarations.  We split these so we can separate out the unchanging 'older'
        ' declarations from the constantly changing 'latest' declaration.
        Private ReadOnly _allOlderRootDeclarations As ImmutableHashSet(Of DeclarationTableEntry)
        Private ReadOnly _latestLazyRootDeclaration As DeclarationTableEntry

        ' The cache of computed values for the old declarations.
        Private ReadOnly _cache As Cache

        ' The lazily computed total merged declaration.
        Private ReadOnly _mergedRoot As Lazy(Of MergedNamespaceDeclaration)

        Private ReadOnly _typeNames As Lazy(Of ICollection(Of String))
        Private ReadOnly _namespaceNames As Lazy(Of ICollection(Of String))
        Private ReadOnly _referenceDirectives As Lazy(Of ICollection(Of ReferenceDirective))

        ' Stores diagnostics related to #r directives
        Private ReadOnly _referenceDirectiveDiagnostics As Lazy(Of ICollection(Of Diagnostic))

        Private _lazyAllRootDeclarations As ImmutableArray(Of RootSingleNamespaceDeclaration)

        Private Sub New(allOlderRootDeclarations As ImmutableHashSet(Of DeclarationTableEntry),
                        latestLazyRootDeclaration As DeclarationTableEntry,
                        cache As Cache)
            Me._allOlderRootDeclarations = allOlderRootDeclarations
            Me._latestLazyRootDeclaration = latestLazyRootDeclaration
            Me._cache = If(cache, New Cache(Me))
            Me._mergedRoot = New Lazy(Of MergedNamespaceDeclaration)(AddressOf GetMergedRoot)
            Me._typeNames = New Lazy(Of ICollection(Of String))(AddressOf GetMergedTypeNames)
            Me._namespaceNames = New Lazy(Of ICollection(Of String))(AddressOf GetMergedNamespaceNames)
            Me._referenceDirectives = New Lazy(Of ICollection(Of ReferenceDirective))(AddressOf GetMergedReferenceDirectives)
            Me._referenceDirectiveDiagnostics = New Lazy(Of ICollection(Of Diagnostic))(AddressOf GetMergedDiagnostics)
        End Sub

        Public Function AddRootDeclaration(lazyRootDeclaration As DeclarationTableEntry) As DeclarationTable
            ' We can only re-use the cache if we don't already have a 'latest' item for the decl
            ' table.
            If _latestLazyRootDeclaration Is Nothing Then
                Return New DeclarationTable(_allOlderRootDeclarations, lazyRootDeclaration, Me._cache)
            Else
                ' we already had a 'latest' item.  This means we're hearing about a change to a
                ' different tree.  Realize the old latest item, add it to the 'older' collection
                ' and don't reuse the cache.
                Return New DeclarationTable(_allOlderRootDeclarations.Add(_latestLazyRootDeclaration), lazyRootDeclaration, Cache:=Nothing)
            End If
        End Function

        Public Function RemoveRootDeclaration(lazyRootDeclaration As DeclarationTableEntry) As DeclarationTable
            ' We can only reuse the cache if we're removing the decl that was just added.
            If _latestLazyRootDeclaration Is lazyRootDeclaration Then
                Return New DeclarationTable(_allOlderRootDeclarations, latestLazyRootDeclaration:=Nothing, Cache:=Me._cache)
            Else
                ' We're removing a different tree than the latest one added.  We need to realize the
                ' passed in root and remove that from our 'older' list.  We also can't reuse the
                ' cache.
                '
                ' Note: we can keep around the 'latestLazyRootDeclaration'.  There's no need to
                ' realize it if we don't have to.
                Return New DeclarationTable(_allOlderRootDeclarations.Remove(lazyRootDeclaration), _latestLazyRootDeclaration, Cache:=Nothing)
            End If
        End Function

        Public Function AllRootNamespaces() As ImmutableArray(Of RootSingleNamespaceDeclaration)
            If _lazyAllRootDeclarations.IsDefault Then
                Dim builder = ArrayBuilder(Of RootSingleNamespaceDeclaration).GetInstance()
                GetOlderNamespaces(builder)
                Dim declOpt = GetLatestRootDeclarationIfAny(includeEmbedded:=True)
                If declOpt IsNot Nothing Then
                    builder.Add(declOpt)
                End If
                ImmutableInterlocked.InterlockedInitialize(_lazyAllRootDeclarations, builder.ToImmutableAndFree())
            End If

            Return _lazyAllRootDeclarations
        End Function

        Private Sub GetOlderNamespaces(builder As ArrayBuilder(Of RootSingleNamespaceDeclaration))
            For Each olderRootDeclaration In _allOlderRootDeclarations
                Dim declOpt = olderRootDeclaration.Root.Value
                If declOpt IsNot Nothing Then
                    builder.Add(declOpt)
                End If
            Next
        End Sub

        Private Function MergeOlderNamespaces() As MergedNamespaceDeclaration
            Dim builder = ArrayBuilder(Of RootSingleNamespaceDeclaration).GetInstance()
            GetOlderNamespaces(builder)
            Dim result = MergedNamespaceDeclaration.Create(builder)
            builder.Free()
            Return result
        End Function

        Private Function SelectManyFromOlderDeclarationsNoEmbedded(Of T)(selector As Func(Of RootSingleNamespaceDeclaration, ImmutableArray(Of T))) As ImmutableArray(Of T)
            Return _allOlderRootDeclarations.Where(Function(d) Not d.IsEmbedded AndAlso d.Root.Value IsNot Nothing).SelectMany(Function(d) selector(d.Root.Value)).AsImmutable()
        End Function

#If False Then
        Public Function FindNamespace(ByVal name As String) As Declaration
            ' Linear search of contexts to find the first one with a 
            ' namespace of this name.
            Return FindNamespaces(name).FirstOrDefault()
        End Function

        Public Function FindNamespaces(ByVal name As String) As IEnumerable(Of Declaration)
            ' Not optimized. Could build a memoizer for the old items in the set and re-use that memoizer across edits.
            Return From root In AllRootNamespaces(),
                   ns In root.GetNamespaces(name)
                   Select ns
        End Function

        Public Function FindType(ByVal name As String) As Declaration
            Return FindTypes(name).FirstOrDefault()
        End Function

        Public Function FindType(ByVal name As String, ByVal arity As Integer) As Declaration
            Return FindTypes(name, arity).FirstOrDefault()
        End Function

        Public Function FindTypes(ByVal name As String) As IEnumerable(Of Declaration)
            ' Not optimized. Could build a memoizer for the old items in the set and re-use that memoizer across edits.
            Return From root In AllRootNamespaces(),
                   t In root.GetTypes(name)
                   Select t
        End Function

        Public Function FindTypes(ByVal name As String, ByVal arity As Integer) As IEnumerable(Of Declaration)
            ' Linear search of root declarations to find one with a type of this name,
            ' and then linear filtering of results. Assumption here is that
            ' there will not be very many types of wrong arity but same name.
            ' Is this assumption valid? Consider types like Func, Tuple, and so on.

            Return From type In FindTypes(name)
                   Where type.Arity = arity
                   Select type
        End Function

        Public Function FindParent(ByVal name As String) As Declaration
            Debug.Assert(name IsNot Nothing)
            Return FindParents(name).FirstOrDefault()
        End Function

        ' Find all declarations which have a member of the given name.
        Public Function FindParents(ByVal name As String) As IEnumerable(Of Declaration)
            ' Not optimized. Could build a memoizer for the old items in the set and re-use that memoizer across edits.
            Return From root In AllRootNamespaces(),
                   d In root.GetParents(name)
                   Select d
        End Function

        Private Function AllParentsCore(ByVal context As RootDeclaration, ByVal firstParent As Declaration) As List(Of Declaration)
            Dim list As New List(Of Declaration)
            Dim parent As Declaration = firstParent

            While parent IsNot Nothing
                list.Add(parent)
                parent = context.GetParent(parent)
            End While

            Return list
        End Function

        Public Function AllParents(ByVal child As Declaration) As IEnumerable(Of Declaration)
            Dim parent As Declaration = Nothing

            ' Not optimized. Could build a memoizer for the old items in the set and re-use that memoizer across edits.
            For Each root In AllRootNamespaces()
                If root.TryGetParent(child, parent) Then
                    Return AllParentsCore(root, parent)
                End If
            Next

            Debug.Fail("Someone is searching for a declaration that does not exist in this declaration table.")
            Return Enumerable.Empty(Of Declaration)()
        End Function
#End If

        ' The merged-tree-reuse story goes like this. We have a "forest" of old declarations, and
        ' possibly a lone tree of new declarations. We construct a merged declaration by merging
        ' together everything in the forest. This we can re-use from edit to edit, provided that
        ' nothing is added to or removed from the forest. We construct a merged declaration from the
        ' lone tree if there is one. (The lone tree might have nodes inside it that need merging, if
        ' there are two halves of one partial class.) Once we have two merged trees, we construct
        ' the full merged tree by merging them both together. So, diagrammatically, we have:
        '
        ' MergedRoot
        ' / \
        ' old merged root new merged root
        ' / | | | \ \
        ' old singles forest new single tree
        Private Function GetMergedRoot() As MergedNamespaceDeclaration
            Dim oldRoot = Me._cache.MergedRoot.Value
            Dim latestRoot = GetLatestRootDeclarationIfAny(includeEmbedded:=True)
            If latestRoot Is Nothing Then
                Return oldRoot
            ElseIf oldRoot Is Nothing Then
                Return MergedNamespaceDeclaration.Create(latestRoot)
            Else
                Return MergedNamespaceDeclaration.Create(oldRoot, latestRoot)
            End If
        End Function

        Private Function GetMergedTypeNames() As ICollection(Of String)
            Dim cachedTypeNames = Me._cache.TypeNames.Value
            Dim latestRoot = GetLatestRootDeclarationIfAny(includeEmbedded:=True)
            If latestRoot Is Nothing Then
                Return cachedTypeNames
            Else
                Return UnionCollection(Of String).Create(cachedTypeNames, GetTypeNames(latestRoot))
            End If
        End Function

        Private Function GetMergedNamespaceNames() As ICollection(Of String)
            Dim cachedNamespaceNames = Me._cache.NamespaceNames.Value
            Dim latestRoot = GetLatestRootDeclarationIfAny(includeEmbedded:=True)
            If latestRoot Is Nothing Then
                Return cachedNamespaceNames
            Else
                Return UnionCollection(Of String).Create(cachedNamespaceNames, GetNamespaceNames(latestRoot))
            End If
        End Function

        Private Function GetMergedReferenceDirectives() As ICollection(Of ReferenceDirective)
            Dim cachedReferenceDirectives = _cache.ReferenceDirectives.Value
            Dim latestRoot = GetLatestRootDeclarationIfAny(includeEmbedded:=False)
            If latestRoot Is Nothing Then
                Return cachedReferenceDirectives
            Else
                Return UnionCollection(Of ReferenceDirective).Create(cachedReferenceDirectives, latestRoot.ReferenceDirectives)
            End If
        End Function

        Private Function GetMergedDiagnostics() As ICollection(Of Diagnostic)
            Dim cachedDiagnostics = _cache.ReferenceDirectiveDiagnostics.Value
            Dim latestRoot = GetLatestRootDeclarationIfAny(includeEmbedded:=False)
            If latestRoot Is Nothing Then
                Return cachedDiagnostics
            Else
                Return UnionCollection(Of Diagnostic).Create(cachedDiagnostics, latestRoot.ReferenceDirectiveDiagnostics)
            End If
        End Function

        Private Function GetLatestRootDeclarationIfAny(includeEmbedded As Boolean) As RootSingleNamespaceDeclaration
            Return If((_latestLazyRootDeclaration IsNot Nothing) AndAlso (includeEmbedded OrElse Not _latestLazyRootDeclaration.IsEmbedded),
                      _latestLazyRootDeclaration.Root.Value,
                      Nothing)
        End Function

        Private Shared ReadOnly IsNamespacePredicate As Predicate(Of Declaration) = Function(d) d.Kind = DeclarationKind.Namespace
        Private Shared ReadOnly IsTypePredicate As Predicate(Of Declaration) = Function(d) d.Kind <> DeclarationKind.Namespace

        Private Shared Function GetTypeNames(declaration As Declaration) As ICollection(Of String)
            Return GetNames(declaration, IsTypePredicate)
        End Function

        Private Shared Function GetNamespaceNames(declaration As Declaration) As ICollection(Of String)
            Return GetNames(declaration, IsNamespacePredicate)
        End Function

        Private Shared Function GetNames(declaration As Declaration, predicate As Predicate(Of Declaration)) As ICollection(Of String)
            Dim result = New IdentifierCollection

            Dim stack = New Stack(Of Declaration)()
            stack.Push(declaration)
            While stack.Count > 0
                Dim current = stack.Pop()
                If current Is Nothing Then
                    Continue While
                End If

                If predicate(current) Then
                    result.AddIdentifier(current.Name)
                End If

                For Each child In current.Children
                    stack.Push(child)
                Next
            End While

            Return result.AsCaseInsensitiveCollection()
        End Function

        Public ReadOnly Property MergedRoot As MergedNamespaceDeclaration
            Get
                Return _mergedRoot.Value
            End Get
        End Property

        Public ReadOnly Property TypeNames As ICollection(Of String)
            Get
                Return _typeNames.Value
            End Get
        End Property

        Public ReadOnly Property NamespaceNames As ICollection(Of String)
            Get
                Return _namespaceNames.Value
            End Get
        End Property

        Public ReadOnly Property ReferenceDirectives As ICollection(Of ReferenceDirective)
            Get
                Return _referenceDirectives.Value
            End Get
        End Property

        Public ReadOnly Property ReferenceDirectiveDiagnostics As ICollection(Of Diagnostic)
            Get
                Return _referenceDirectiveDiagnostics.Value
            End Get
        End Property
    End Class
End Namespace
