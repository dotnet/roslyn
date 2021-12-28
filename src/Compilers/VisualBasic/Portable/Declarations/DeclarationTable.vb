' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects

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
                                                           ImmutableSetWithInsertionOrder(Of DeclarationTableEntry).Empty,
                                                           latestLazyRootDeclaration:=Nothing,
                                                           cache:=Nothing)

        ' All our root declarations.  We split these so we can separate out the unchanging 'older'
        ' declarations from the constantly changing 'latest' declaration.
        Private ReadOnly _allOlderRootDeclarations As ImmutableSetWithInsertionOrder(Of DeclarationTableEntry)
        Private ReadOnly _latestLazyRootDeclaration As DeclarationTableEntry

        ' The cache of computed values for the old declarations.
        Private ReadOnly _cache As Cache

        ' The lazily computed total merged declaration.
        Private _mergedRoot As MergedNamespaceDeclaration

        Private ReadOnly _typeNames As Lazy(Of ICollection(Of String))
        Private ReadOnly _namespaceNames As Lazy(Of ICollection(Of String))
        Private ReadOnly _referenceDirectives As Lazy(Of ICollection(Of ReferenceDirective))

        Private _lazyAllRootDeclarations As ImmutableArray(Of RootSingleNamespaceDeclaration)

        Private Sub New(allOlderRootDeclarations As ImmutableSetWithInsertionOrder(Of DeclarationTableEntry),
                        latestLazyRootDeclaration As DeclarationTableEntry,
                        cache As Cache)
            Me._allOlderRootDeclarations = allOlderRootDeclarations
            Me._latestLazyRootDeclaration = latestLazyRootDeclaration
            Me._cache = If(cache, New Cache(Me))
            Me._typeNames = New Lazy(Of ICollection(Of String))(AddressOf GetMergedTypeNames)
            Me._namespaceNames = New Lazy(Of ICollection(Of String))(AddressOf GetMergedNamespaceNames)
            Me._referenceDirectives = New Lazy(Of ICollection(Of ReferenceDirective))(AddressOf GetMergedReferenceDirectives)
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
                Return New DeclarationTable(_allOlderRootDeclarations.Add(_latestLazyRootDeclaration), lazyRootDeclaration, cache:=Nothing)
            End If
        End Function

        Public Function Contains(rootDeclaration As DeclarationTableEntry) As Boolean
            Return rootDeclaration IsNot Nothing AndAlso
                 (_allOlderRootDeclarations.Contains(rootDeclaration) OrElse _latestLazyRootDeclaration Is rootDeclaration)
        End Function

        Public Function RemoveRootDeclaration(lazyRootDeclaration As DeclarationTableEntry) As DeclarationTable
            ' We can only reuse the cache if we're removing the decl that was just added.
            If _latestLazyRootDeclaration Is lazyRootDeclaration Then
                Return New DeclarationTable(_allOlderRootDeclarations, latestLazyRootDeclaration:=Nothing, cache:=Me._cache)
            Else
                ' We're removing a different tree than the latest one added.  We need to realize the
                ' passed in root and remove that from our 'older' list.  We also can't reuse the
                ' cache.
                '
                ' Note: we can keep around the 'latestLazyRootDeclaration'.  There's no need to
                ' realize it if we don't have to.
                Return New DeclarationTable(_allOlderRootDeclarations.Remove(lazyRootDeclaration), _latestLazyRootDeclaration, cache:=Nothing)
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
            For Each olderRootDeclaration In _allOlderRootDeclarations.InInsertionOrder
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
            Return _allOlderRootDeclarations.InInsertionOrder.Where(Function(d) Not d.IsEmbedded AndAlso d.Root.Value IsNot Nothing).SelectMany(Function(d) selector(d.Root.Value)).AsImmutable()
        End Function

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
        Public Function GetMergedRoot(compilation As VisualBasicCompilation) As MergedNamespaceDeclaration
            Debug.Assert(compilation.Declarations Is Me)
            If _mergedRoot Is Nothing Then
                Interlocked.CompareExchange(_mergedRoot, CalculateMergedRoot(compilation), Nothing)
            End If
            Return _mergedRoot
        End Function

        ' Internal for unit tests only.
        Friend Function CalculateMergedRoot(compilation As VisualBasicCompilation) As MergedNamespaceDeclaration
            Dim oldRoot = Me._cache.MergedRoot.Value
            Dim latestRoot = GetLatestRootDeclarationIfAny(includeEmbedded:=True)
            If latestRoot Is Nothing Then
                Return oldRoot
            ElseIf oldRoot Is Nothing Then
                Return MergedNamespaceDeclaration.Create(latestRoot)
            Else
                Dim oldRootDeclarations = oldRoot.Declarations
                Dim builder = ArrayBuilder(Of SingleNamespaceDeclaration).GetInstance(oldRootDeclarations.Length + 1)
                builder.AddRange(oldRootDeclarations)
                builder.Add(_latestLazyRootDeclaration.Root.Value)
                ' Sort the root namespace declarations to match the order of SyntaxTrees.
                If compilation IsNot Nothing Then
                    builder.Sort(New RootNamespaceLocationComparer(compilation))
                End If
                Return MergedNamespaceDeclaration.Create(builder.ToImmutableAndFree())
            End If
        End Function

        Private NotInheritable Class RootNamespaceLocationComparer
            Implements IComparer(Of SingleNamespaceDeclaration)

            Private ReadOnly _compilation As VisualBasicCompilation

            Friend Sub New(compilation As VisualBasicCompilation)
                _compilation = compilation
            End Sub

            <PerformanceSensitive(
                "https://github.com/dotnet/roslyn/issues/23582",
                Constraint:="Avoid " + NameOf(SingleNamespaceOrTypeDeclaration.Location) + " since it probably also has a costly allocation on this fast path (VB equivalent of issue found in C# code).")>
            Public Function Compare(x As SingleNamespaceDeclaration, y As SingleNamespaceDeclaration) As Integer Implements IComparer(Of SingleNamespaceDeclaration).Compare
                Return _compilation.CompareSourceLocations(x.SyntaxReference, y.SyntaxReference)
            End Function
        End Class

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

        Private Function GetLatestRootDeclarationIfAny(includeEmbedded As Boolean) As RootSingleNamespaceDeclaration
            Return If((_latestLazyRootDeclaration IsNot Nothing) AndAlso (includeEmbedded OrElse Not _latestLazyRootDeclaration.IsEmbedded),
                      _latestLazyRootDeclaration.Root.Value,
                      Nothing)
        End Function

        Private Shared ReadOnly s_isNamespacePredicate As Predicate(Of Declaration) = Function(d) d.Kind = DeclarationKind.Namespace
        Private Shared ReadOnly s_isTypePredicate As Predicate(Of Declaration) = Function(d) d.Kind <> DeclarationKind.Namespace

        Private Shared Function GetTypeNames(declaration As Declaration) As ICollection(Of String)
            Return GetNames(declaration, s_isTypePredicate)
        End Function

        Private Shared Function GetNamespaceNames(declaration As Declaration) As ICollection(Of String)
            Return GetNames(declaration, s_isNamespacePredicate)
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

        Public Shared Function ContainsName(
            mergedRoot As MergedNamespaceDeclaration,
            name As String,
            filter As SymbolFilter,
            cancellationToken As CancellationToken) As Boolean

            Return ContainsNameHelper(
                mergedRoot,
                Function(n) IdentifierComparison.Equals(n, name),
                filter,
                Function(t) t.MemberNames.Contains(name),
                cancellationToken)
        End Function

        Public Shared Function ContainsName(
            mergedRoot As MergedNamespaceDeclaration,
            predicate As Func(Of String, Boolean),
            filter As SymbolFilter,
            cancellationToken As CancellationToken) As Boolean

            Return ContainsNameHelper(
                mergedRoot,
                predicate,
                filter,
                Function(t)
                    For Each name In t.MemberNames
                        If predicate(name) Then
                            Return True
                        End If
                    Next

                    Return False
                End Function,
                cancellationToken)
        End Function

        Public Shared Function ContainsNameHelper(
            mergedRoot As MergedNamespaceDeclaration,
            predicate As Func(Of String, Boolean),
            filter As SymbolFilter,
            typePredicate As Func(Of SingleTypeDeclaration, Boolean),
            cancellationToken As CancellationToken) As Boolean

            Dim includeNamespace = (filter And SymbolFilter.Namespace) = SymbolFilter.Namespace
            Dim includeType = (filter And SymbolFilter.Type) = SymbolFilter.Type
            Dim includeMember = (filter And SymbolFilter.Member) = SymbolFilter.Member

            Dim stack = New Stack(Of MergedNamespaceOrTypeDeclaration)()
            stack.Push(mergedRoot)

            While stack.Count > 0
                cancellationToken.ThrowIfCancellationRequested()

                Dim current = stack.Pop()
                If current Is Nothing Then
                    Continue While
                End If

                If current.Kind = DeclarationKind.Namespace Then
                    If includeNamespace AndAlso predicate(current.Name) Then
                        Return True
                    End If
                Else
                    If includeType AndAlso predicate(current.Name) Then
                        Return True
                    End If

                    If includeMember Then
                        Dim mergedType = DirectCast(current, MergedTypeDeclaration)
                        For Each childType In mergedType.Declarations
                            If typePredicate(childType) Then
                                Return True
                            End If
                        Next
                    End If
                End If

                For Each child In current.Children
                    Dim childNamespaceOrType = DirectCast(child, MergedNamespaceOrTypeDeclaration)

                    If includeMember OrElse includeType OrElse childNamespaceOrType.Kind = DeclarationKind.Namespace Then
                        stack.Push(childNamespaceOrType)
                    End If
                Next
            End While

            Return False
        End Function
    End Class
End Namespace
