// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A MergedNamespaceSymbol represents a namespace that merges the contents of two or more other
    /// namespaces. Any sub-namespaces with the same names are also merged if they have two or more
    /// instances.
    /// 
    /// Merged namespaces are used to merge the symbols from multiple metadata modules and the
    /// source "module" into a single symbol tree that represents all the available symbols. The
    /// compiler resolves names against this merged set of symbols.
    /// 
    /// Typically there will not be very many merged namespaces in a Compilation: only the root
    /// namespaces and namespaces that are used in multiple referenced modules. (Microsoft, System,
    /// System.Xml, System.Diagnostics, System.Threading, ...)
    /// </summary>
    internal sealed class MergedNamespaceSymbol : NamespaceSymbol
    {
        private readonly NamespaceExtent _extent;
        private readonly ImmutableArray<NamespaceSymbol> _namespacesToMerge;
        private readonly NamespaceSymbol _containingNamespace;

        // used when this namespace is constructed as the result of an extern alias directive
        private readonly string _nameOpt;

        // The cachedLookup caches results of lookups on the constituent namespaces so that
        // subsequent lookups for the same name are much faster than having to ask each of the
        // constituent namespaces.
        private readonly CachingDictionary<ReadOnlyMemory<char>, Symbol> _cachedLookup;

        // GetMembers() is repeatedly called on merged namespaces in some IDE scenarios.
        // This caches the result that is built by asking the 'cachedLookup' for a concatenated
        // view of all of its values.
        private ImmutableArray<Symbol> _allMembers;

        /// <summary>
        /// Create a possibly merged namespace symbol. If only a single namespace is passed it, it
        /// is just returned directly. If two or more namespaces are passed in, then a new merged
        /// namespace is created with the given extent and container.
        /// </summary>
        /// <param name="extent">The namespace extent to use, IF a merged namespace is created.</param>
        /// <param name="containingNamespace">The containing namespace to used, IF a merged
        /// namespace is created.</param>
        /// <param name="namespacesToMerge">One or more namespaces to merged. If just one, then it
        /// is returned. The merged namespace symbol may hold onto the array.</param>
        /// <param name="nameOpt">An optional name to give the resulting namespace.</param>
        /// <returns>A namespace symbol representing the merged namespace.</returns>
        internal static NamespaceSymbol Create(
            NamespaceExtent extent,
            NamespaceSymbol containingNamespace,
            ImmutableArray<NamespaceSymbol> namespacesToMerge,
            string nameOpt = null)
        {
            // Currently, if we are just merging 1 namespace, we just return the namespace itself.
            // This is by far the most efficient, because it means that we don't create merged
            // namespaces (which have a fair amount of memory overhead) unless there is actual
            // merging going on. However, it means that the child namespace of a Compilation extent
            // namespace may be a Module extent namespace, and the containing of that module extent
            // namespace will be another module extent namespace. This is basically no different
            // than type members of namespaces, so it shouldn't be TOO unexpected.

            // EDMAURER if the caller is supplying a name, then produce the merged namespace with
            // the new name even if only a single namespace was provided. This behavior was introduced
            // to support nice extern alias error reporting.

            Debug.Assert(namespacesToMerge.Length != 0);

            return (namespacesToMerge.Length == 1 && nameOpt == null)
                ? namespacesToMerge[0]
                : new MergedNamespaceSymbol(extent, containingNamespace, namespacesToMerge, nameOpt);
        }

        // Constructor. Use static Create method to create instances.
        private MergedNamespaceSymbol(NamespaceExtent extent, NamespaceSymbol containingNamespace, ImmutableArray<NamespaceSymbol> namespacesToMerge, string nameOpt)
        {
            _extent = extent;
            _namespacesToMerge = namespacesToMerge;
            _containingNamespace = containingNamespace;
            _cachedLookup = new CachingDictionary<ReadOnlyMemory<char>, Symbol>(SlowGetChildrenOfName, SlowGetChildNames, ReadOnlyMemoryOfCharComparer.Instance);
            _nameOpt = nameOpt;

#if DEBUG
            // We shouldn't merged namespaces that are already merged.
            foreach (NamespaceSymbol ns in namespacesToMerge)
            {
                Debug.Assert(ns.ConstituentNamespaces.Length == 1);
            }
#endif
        }

        internal NamespaceSymbol GetConstituentForCompilation(CSharpCompilation compilation)
        {
            //return namespacesToMerge.FirstOrDefault(n => n.IsFromSource);
            //Replace above code with that below to eliminate allocation of array enumerator.

            foreach (var n in _namespacesToMerge)
            {
                if (n.IsFromCompilation(compilation))
                    return n;
            }

            return null;
        }

        internal override void ForceComplete(SourceLocation locationOpt, Predicate<Symbol> filter, CancellationToken cancellationToken)
        {
            foreach (var part in _namespacesToMerge)
            {
                cancellationToken.ThrowIfCancellationRequested();
                part.ForceComplete(locationOpt, filter, cancellationToken);
            }
        }

        /// <summary>
        /// Method that is called from the CachingLookup to lookup the children of a given name.
        /// Looks in all the constituent namespaces.
        /// </summary>
        private ImmutableArray<Symbol> SlowGetChildrenOfName(ReadOnlyMemory<char> name)
        {
            ArrayBuilder<NamespaceSymbol> namespaceSymbols = null;
            var otherSymbols = ArrayBuilder<Symbol>.GetInstance();

            // Accumulate all the child namespaces and types.
            foreach (NamespaceSymbol namespaceSymbol in _namespacesToMerge)
            {
                foreach (Symbol childSymbol in namespaceSymbol.GetMembers(name))
                {
                    if (childSymbol.Kind == SymbolKind.Namespace)
                    {
                        namespaceSymbols = namespaceSymbols ?? ArrayBuilder<NamespaceSymbol>.GetInstance();
                        namespaceSymbols.Add((NamespaceSymbol)childSymbol);
                    }
                    else
                    {
                        otherSymbols.Add(childSymbol);
                    }
                }
            }

            if (namespaceSymbols != null)
            {
                otherSymbols.Add(MergedNamespaceSymbol.Create(_extent, this, namespaceSymbols.ToImmutableAndFree()));
            }

            return otherSymbols.ToImmutableAndFree();
        }

        /// <summary>
        /// Method that is called from the CachingLookup to get all child names. Looks in all
        /// constituent namespaces.
        /// </summary>
        private SegmentedHashSet<ReadOnlyMemory<char>> SlowGetChildNames(IEqualityComparer<ReadOnlyMemory<char>> comparer)
        {
            // compute an upper bound for the final capacity of the set we'll return, to reduce heap churn
            int childCount = 0;

            foreach (var ns in _namespacesToMerge)
            {
                childCount += ns.GetMembersUnordered().Length;
            }

            var childNames = new SegmentedHashSet<ReadOnlyMemory<char>>(childCount, comparer);

            foreach (var ns in _namespacesToMerge)
            {
                foreach (var child in ns.GetMembersUnordered())
                {
                    childNames.Add(child.Name.AsMemory());
                }
            }

            return childNames;
        }

        public override string Name
        {
            get
            {
                return _nameOpt ?? _namespacesToMerge[0].Name;
            }
        }

        internal override NamespaceExtent Extent
        {
            get
            {
                return _extent;
            }
        }

        public override ImmutableArray<NamespaceSymbol> ConstituentNamespaces
        {
            get
            {
                return _namespacesToMerge;
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            // Return all the elements from every IGrouping in the ILookup.
            if (_allMembers.IsDefault)
            {
                var builder = ArrayBuilder<Symbol>.GetInstance();
                _cachedLookup.AddValues(builder);
                _allMembers = builder.ToImmutableAndFree();
            }

            return _allMembers;
        }

        public override ImmutableArray<Symbol> GetMembers(ReadOnlyMemory<char> name)
        {
            return _cachedLookup[name];
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            return ImmutableArray.CreateRange<NamedTypeSymbol>(GetMembersUnordered().OfType<NamedTypeSymbol>());
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray.CreateRange<NamedTypeSymbol>(GetMembers().OfType<NamedTypeSymbol>());
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            // TODO - This is really inefficient. Creating a new array on each lookup needs to fixed!
            return ImmutableArray.CreateRange<NamedTypeSymbol>(_cachedLookup[name].OfType<NamedTypeSymbol>());
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingNamespace;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                if (_extent.Kind == NamespaceKind.Module)
                {
                    return _extent.Module.ContainingAssembly;
                }
                else if (_extent.Kind == NamespaceKind.Assembly)
                {
                    return _extent.Assembly;
                }
                else
                {
                    return null;
                }
            }
        }

        public override ImmutableArray<Location> Locations
        {
            // Merge the locations of all constituent namespaces.
            get
            {
                //TODO: cache
                return _namespacesToMerge.SelectMany(namespaceSymbol => namespaceSymbol.Locations).AsImmutable();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _namespacesToMerge.SelectMany(namespaceSymbol => namespaceSymbol.DeclaringSyntaxReferences).AsImmutable();
            }
        }

        internal override void GetExtensionMethods(ArrayBuilder<MethodSymbol> methods, string name, int arity, LookupOptions options)
        {
            foreach (NamespaceSymbol namespaceSymbol in _namespacesToMerge)
            {
                namespaceSymbol.GetExtensionMethods(methods, name, arity, options);
            }
        }

#nullable enable
        // Overridden to avoid NamespaceSymbol.GetExtensionContainers call to GetTypeMembersUnordered. The combination of the
        // CreateRange and OfType Linq calls in MergedNamespaceSymbol.GetTypeMembersUnordered causes a full array allocation.
        internal sealed override void GetExtensionMembers(ArrayBuilder<Symbol> members, string? name, string? alternativeName, int arity, LookupOptions options, ConsList<FieldSymbol> fieldsBeingBound)
        {
            foreach (var member in GetMembersUnordered())
            {
                if (member is NamedTypeSymbol type)
                {
                    type.GetExtensionMembers(members, name, alternativeName, arity, options, fieldsBeingBound);
                }
            }
        }
    }
}
