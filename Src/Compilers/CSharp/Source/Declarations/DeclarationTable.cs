// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            allOlderRootDeclarations: ImmutableSetWithInsertionOrder<RootSingleNamespaceDeclaration>.Empty,
            latestLazyRootDeclaration: null,
            cache: null);

        // All our root declarations.  We split these so we can separate out the unchanging 'older'
        // declarations from the constantly changing 'latest' declaration.
        private readonly ImmutableSetWithInsertionOrder<RootSingleNamespaceDeclaration> allOlderRootDeclarations;
        private readonly Lazy<RootSingleNamespaceDeclaration> latestLazyRootDeclaration;

        // The cache of computed values for the old declarations.
        private readonly Cache cache;

        // The lazily computed total merged declaration.
        private readonly Lazy<MergedNamespaceDeclaration> mergedRoot;

        private readonly Lazy<ICollection<string>> typeNames;
        private readonly Lazy<ICollection<string>> namespaceNames;
        private readonly Lazy<ICollection<ReferenceDirective>> referenceDirectives;
        private readonly Lazy<ICollection<Diagnostic>> referenceDirectiveDiagnostics;

        private DeclarationTable(
            ImmutableSetWithInsertionOrder<RootSingleNamespaceDeclaration> allOlderRootDeclarations,
            Lazy<RootSingleNamespaceDeclaration> latestLazyRootDeclaration,
            Cache cache)
        {
            this.allOlderRootDeclarations = allOlderRootDeclarations;
            this.latestLazyRootDeclaration = latestLazyRootDeclaration;
            this.cache = cache ?? new Cache(this);
            this.mergedRoot = new Lazy<MergedNamespaceDeclaration>(GetMergedRoot);
            this.typeNames = new Lazy<ICollection<string>>(GetMergedTypeNames);
            this.namespaceNames = new Lazy<ICollection<string>>(GetMergedNamespaceNames);
            this.referenceDirectives = new Lazy<ICollection<ReferenceDirective>>(GetMergedReferenceDirectives);
            this.referenceDirectiveDiagnostics = new Lazy<ICollection<Diagnostic>>(GetMergedDiagnostics);
        }

        public DeclarationTable AddRootDeclaration(Lazy<RootSingleNamespaceDeclaration> lazyRootDeclaration)
        {
            // We can only re-use the cache if we don't already have a 'latest' item for the decl
            // table.
            if (latestLazyRootDeclaration == null)
            {
                return new DeclarationTable(allOlderRootDeclarations, lazyRootDeclaration, this.cache);
            }
            else
            {
                // we already had a 'latest' item.  This means we're hearing about a change to a
                // different tree.  Realize the old latest item, add it to the 'oldest' collection
                // and don't reuse the cache.
                return new DeclarationTable(allOlderRootDeclarations.Add(latestLazyRootDeclaration.Value), lazyRootDeclaration, cache: null);
            }
        }

        public DeclarationTable RemoveRootDeclaration(Lazy<RootSingleNamespaceDeclaration> lazyRootDeclaration)
        {
            // We can only reuse the cache if we're removing the decl that was just added.
            if (latestLazyRootDeclaration == lazyRootDeclaration)
            {
                return new DeclarationTable(allOlderRootDeclarations, latestLazyRootDeclaration: null, cache: cache);
            }
            else
            {
                // We're removing a different tree than the latest one added.  We need to realize the
                // passed in root and remove that from our 'older' list.  We also can't reuse the
                // cache.
                //
                // Note: we can keep around the 'latestLazyRootDeclaration'.  There's no need to
                // realize it if we don't have to.
                return new DeclarationTable(allOlderRootDeclarations.Remove(lazyRootDeclaration.Value), latestLazyRootDeclaration, cache: null);
            }
        }

        public IEnumerable<RootSingleNamespaceDeclaration> AllRootNamespacesUnordered()
        {
            if (latestLazyRootDeclaration == null)
            {
                return allOlderRootDeclarations;
            }
            else
            {
                return allOlderRootDeclarations.Concat(SpecializedCollections.SingletonEnumerable(latestLazyRootDeclaration.Value));
            }
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

        private MergedNamespaceDeclaration GetMergedRoot()
        {
            var oldRoot = this.cache.MergedRoot.Value;
            if (latestLazyRootDeclaration == null)
            {
                return oldRoot;
            }
            else if (oldRoot == null)
            {
                return MergedNamespaceDeclaration.Create(latestLazyRootDeclaration.Value);
            }
            else
            {
                return MergedNamespaceDeclaration.Create(oldRoot, latestLazyRootDeclaration.Value);
            }
        }

        private ICollection<string> GetMergedTypeNames()
        {
            var cachedTypeNames = this.cache.TypeNames.Value;

            if (latestLazyRootDeclaration == null)
            {
                return cachedTypeNames;
            }
            else
            {
                return UnionCollection<string>.Create(cachedTypeNames, GetTypeNames(latestLazyRootDeclaration.Value));
            }
        }

        private ICollection<string> GetMergedNamespaceNames()
        {
            var cachedNamespaceNames = this.cache.NamespaceNames.Value;

            if (latestLazyRootDeclaration == null)
            {
                return cachedNamespaceNames;
            }
            else
            {
                return UnionCollection<string>.Create(cachedNamespaceNames, GetNamespaceNames(latestLazyRootDeclaration.Value));
            }
        }

        private ICollection<ReferenceDirective> GetMergedReferenceDirectives()
        {
            var cachedReferenceDirectives = this.cache.ReferenceDirectives.Value;

            if (latestLazyRootDeclaration == null)
            {
                return cachedReferenceDirectives;
            }
            else
            {
                return UnionCollection<ReferenceDirective>.Create(cachedReferenceDirectives, latestLazyRootDeclaration.Value.ReferenceDirectives);
            }
        }

        private ICollection<Diagnostic> GetMergedDiagnostics()
        {
            var cachedDiagnostics = this.cache.ReferenceDirectiveDiagnostics.Value;

            if (latestLazyRootDeclaration == null)
            {
                return cachedDiagnostics;
            }
            else
            {
                return UnionCollection<Diagnostic>.Create(cachedDiagnostics, latestLazyRootDeclaration.Value.ReferenceDirectiveDiagnostics);
            }
        }

        private static readonly Predicate<Declaration> IsNamespacePredicate = d => d.Kind == DeclarationKind.Namespace;
        private static readonly Predicate<Declaration> IsTypePredicate = d => d.Kind != DeclarationKind.Namespace;

        private static ISet<string> GetTypeNames(Declaration declaration)
        {
            return GetNames(declaration, IsTypePredicate);
        }

        private static ISet<string> GetNamespaceNames(Declaration declaration)
        {
            return GetNames(declaration, IsNamespacePredicate);
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

        public MergedNamespaceDeclaration MergedRoot
        {
            get
            {
                return mergedRoot.Value;
            }
        }

        public ICollection<string> TypeNames
        {
            get
            {
                return typeNames.Value;
            }
        }

        public ICollection<string> NamespaceNames
        {
            get
            {
                return namespaceNames.Value;
            }
        }

        public IEnumerable<ReferenceDirective> ReferenceDirectives
        {
            get
            {
                return referenceDirectives.Value;
            }
        }


        public IEnumerable<Diagnostic> Diagnostics
        {
            get
            {
                return referenceDirectiveDiagnostics.Value;
            }
        }
    }
}