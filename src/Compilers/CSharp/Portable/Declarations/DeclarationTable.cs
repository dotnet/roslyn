// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A declaration table is a device which keeps track of type and namespace declarations from
    /// parse trees. It is optimized for the case where there is one set of declarations that stays
    /// constant, and a specific root namespace declaration corresponding to the currently edited
    /// file which is being added and removed repeatedly. It maintains a cache of information for
    /// "merging" the root declarations into one big summary declaration; this cache is efficiently
    /// re-used provided that the pattern of adds and removes is as we expect.
    /// </summary>
    internal sealed partial class DeclarationTable
    {
        public static readonly DeclarationTable Empty = new DeclarationTable(
            allOlderRootDeclarations: ImmutableSetWithInsertionOrder<Lazy<RootSingleNamespaceDeclaration>>.Empty,
            latestLazyRootDeclaration: null,
            cache: null);

        // All our root declarations.  We split these so we can separate out the unchanging 'older'
        // declarations from the constantly changing 'latest' declaration.
        private readonly ImmutableSetWithInsertionOrder<Lazy<RootSingleNamespaceDeclaration>> _allOlderRootDeclarations;
        private readonly Lazy<RootSingleNamespaceDeclaration>? _latestLazyRootDeclaration;

        // The cache of computed values for the old declarations.
        private readonly Cache _cache;

        // The lazily computed total merged declaration.
        private MergedNamespaceDeclaration? _mergedRoot;

        private ICollection<string>? _typeNames;
        private ICollection<string>? _namespaceNames;
        private ICollection<ReferenceDirective>? _referenceDirectives;

        private DeclarationTable(
            ImmutableSetWithInsertionOrder<Lazy<RootSingleNamespaceDeclaration>> allOlderRootDeclarations,
            Lazy<RootSingleNamespaceDeclaration>? latestLazyRootDeclaration,
            Cache? cache)
        {
            _allOlderRootDeclarations = allOlderRootDeclarations;
            _latestLazyRootDeclaration = latestLazyRootDeclaration;
            _cache = cache ?? new Cache(this);
        }

        // The merged-tree-reuse story goes like this. We have a "forest" of old declarations, and
        // possibly a lone tree of new declarations. We construct a merged declaration by merging
        // together everything in the forest. This we can re-use from edit to edit, provided that
        // nothing is added to or removed from the forest. We construct a merged declaration from
        // the lone tree if there is one. (The lone tree might have nodes inside it that need
        // merging, if there are two halves of one partial class.)  Once we have two merged trees, we
        // construct the full merged tree by merging them both together. So, diagrammatically, we
        // have:
        //
        //                   MergedRoot
        //                  /          \
        //   old merged root            new merged root
        //  /   |   |   |   \                \
        // old singles forest                 new single tree
        public MergedNamespaceDeclaration GetMergedRoot(CSharpCompilation compilation)
        {
            Debug.Assert(compilation.Declarations == this);
            if (_mergedRoot == null)
            {
                Interlocked.CompareExchange(ref _mergedRoot, CalculateMergedRoot(compilation), null);
            }
            return _mergedRoot;
        }

        // Internal for unit tests only.
        internal MergedNamespaceDeclaration CalculateMergedRoot(CSharpCompilation compilation)
        {
            var oldRoot = _cache.MergedRoot;
            if (_latestLazyRootDeclaration == null)
            {
                return oldRoot;
            }
            else if (oldRoot == null)
            {
                return MergedNamespaceDeclaration.Create(_latestLazyRootDeclaration.Value);
            }
            else
            {
                var oldRootDeclarations = oldRoot.Declarations;
                var builder = ArrayBuilder<SingleNamespaceDeclaration>.GetInstance(oldRootDeclarations.Length + 1);
                builder.AddRange(oldRootDeclarations);
                builder.Add(_latestLazyRootDeclaration.Value);
                // Sort the root namespace declarations to match the order of SyntaxTrees.
                if (compilation != null)
                {
                    builder.Sort(new RootNamespaceLocationComparer(compilation));
                }
                return MergedNamespaceDeclaration.Create(builder.ToImmutableAndFree());
            }
        }

        private sealed class RootNamespaceLocationComparer : IComparer<SingleNamespaceDeclaration>
        {
            private readonly CSharpCompilation _compilation;

            internal RootNamespaceLocationComparer(CSharpCompilation compilation)
            {
                _compilation = compilation;
            }

            [PerformanceSensitive(
                "https://github.com/dotnet/roslyn/issues/23582",
                Constraint = "Avoid " + nameof(SingleNamespaceOrTypeDeclaration.Location) + " since it has a costly allocation on this fast path.")]
            public int Compare(SingleNamespaceDeclaration? x, SingleNamespaceDeclaration? y)
            {
                return _compilation.CompareSourceLocations(x!.SyntaxReference, y!.SyntaxReference);
            }
        }

        private ICollection<string> GetMergedTypeNames()
        {
            var cachedTypeNames = _cache.TypeNames;

            if (_latestLazyRootDeclaration == null)
            {
                return cachedTypeNames;
            }
            else
            {
                return UnionCollection<string>.Create(cachedTypeNames, GetTypeNames(_latestLazyRootDeclaration.Value));
            }
        }

        private ICollection<string> GetMergedNamespaceNames()
        {
            var cachedNamespaceNames = _cache.NamespaceNames;

            if (_latestLazyRootDeclaration == null)
            {
                return cachedNamespaceNames;
            }
            else
            {
                return UnionCollection<string>.Create(cachedNamespaceNames, GetNamespaceNames(_latestLazyRootDeclaration.Value));
            }
        }

        private ICollection<ReferenceDirective> GetMergedReferenceDirectives()
        {
            var cachedReferenceDirectives = _cache.ReferenceDirectives;

            if (_latestLazyRootDeclaration == null)
            {
                return cachedReferenceDirectives;
            }
            else
            {
                return UnionCollection<ReferenceDirective>.Create(cachedReferenceDirectives, _latestLazyRootDeclaration.Value.ReferenceDirectives);
            }
        }

        private static readonly Predicate<Declaration> s_isNamespacePredicate = d => d.Kind == DeclarationKind.Namespace;
        private static readonly Predicate<Declaration> s_isTypePredicate = d => d.Kind != DeclarationKind.Namespace;

        private static ISet<string> GetTypeNames(Declaration declaration)
        {
            return GetNames(declaration, s_isTypePredicate);
        }

        private static ISet<string> GetNamespaceNames(Declaration declaration)
        {
            return GetNames(declaration, s_isNamespacePredicate);
        }

        private static ISet<string> GetNames(Declaration declaration, Predicate<Declaration> predicate)
        {
            var set = new HashSet<string>();
            var stack = new Stack<Declaration>();
            stack.Push(declaration);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == null)
                {
                    continue;
                }

                if (predicate(current))
                {
                    set.Add(current.Name);
                }

                foreach (var child in current.Children)
                {
                    stack.Push(child);
                }
            }

            return SpecializedCollections.ReadOnlySet(set);
        }

        public ICollection<string> TypeNames
        {
            get
            {
                if (_typeNames is null)
                    Interlocked.CompareExchange(ref _typeNames, GetMergedTypeNames(), comparand: null);

                return _typeNames;
            }
        }

        public ICollection<string> NamespaceNames
        {
            get
            {
                if (_namespaceNames is null)
                    Interlocked.CompareExchange(ref _namespaceNames, GetMergedNamespaceNames(), comparand: null);

                return _namespaceNames;
            }
        }

        public IEnumerable<ReferenceDirective> ReferenceDirectives
        {
            get
            {
                if (_referenceDirectives is null)
                    Interlocked.CompareExchange(ref _referenceDirectives, GetMergedReferenceDirectives(), comparand: null);

                return _referenceDirectives;
            }
        }

        public static bool ContainsName(
            MergedNamespaceDeclaration mergedRoot,
            string name,
            SymbolFilter filter,
            CancellationToken cancellationToken)
        {
            return ContainsNameHelper(
                mergedRoot,
                n => n == name,
                filter,
                t => t.MemberNames.Value.Contains(name),
                cancellationToken);
        }

        public static bool ContainsName(
            MergedNamespaceDeclaration mergedRoot,
            Func<string, bool> predicate,
            SymbolFilter filter,
            CancellationToken cancellationToken)
        {
            return ContainsNameHelper(
                mergedRoot, predicate, filter,
                t =>
                {
                    foreach (var name in t.MemberNames.Value)
                    {
                        if (predicate(name))
                        {
                            return true;
                        }
                    }

                    return false;
                }, cancellationToken);
        }

        private static bool ContainsNameHelper(
            MergedNamespaceDeclaration mergedRoot,
            Func<string, bool> predicate,
            SymbolFilter filter,
            Func<SingleTypeDeclaration, bool> typePredicate,
            CancellationToken cancellationToken)
        {
            var includeNamespace = (filter & SymbolFilter.Namespace) == SymbolFilter.Namespace;
            var includeType = (filter & SymbolFilter.Type) == SymbolFilter.Type;
            var includeMember = (filter & SymbolFilter.Member) == SymbolFilter.Member;

            var stack = new Stack<MergedNamespaceOrTypeDeclaration>();
            stack.Push(mergedRoot);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var current = stack.Pop();
                if (current == null)
                {
                    continue;
                }

                if (current.Kind == DeclarationKind.Namespace)
                {
                    if (includeNamespace && predicate(current.Name))
                    {
                        return true;
                    }
                }
                else
                {
                    if (includeType && predicate(current.Name))
                    {
                        return true;
                    }

                    if (includeMember)
                    {
                        var mergedType = (MergedTypeDeclaration)current;
                        foreach (var typeDecl in mergedType.Declarations)
                        {
                            if (typePredicate(typeDecl))
                            {
                                return true;
                            }
                        }
                    }
                }

                foreach (var child in current.Children)
                {
                    if (child is MergedNamespaceOrTypeDeclaration childNamespaceOrType)
                    {
                        if (includeMember || includeType || childNamespaceOrType.Kind == DeclarationKind.Namespace)
                        {
                            stack.Push(childNamespaceOrType);
                        }
                    }
                }
            }

            return false;
        }
    }
}
