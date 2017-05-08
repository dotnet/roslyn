// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    using FieldSymbolsCollection = System.Collections.Concurrent.ConcurrentDictionary<MethodSymbol, ModuleScopedDelegateCacheContainerField>;

    /// <summary>
    /// This symbol is created while lowering, and is collected and have name assigned before emit.
    /// </summary>
    internal class ModuleScopedDelegateCacheContainer : DelegateCacheContainer, IComparer<ModuleScopedDelegateCacheContainerField>
    {
        // Why the _name is not readonly? Why the _sortKey?
        // The container is a top level type, and the compilation can be parallel.
        // To ensure simple & deterministic output, we need a unique and deterministic sort key.
        // When all methods are compiled thus all module scoped containers and fields are created, and when things are happening serially before emitting IL, 
        // we sort the symbols using the smallest location of the conversion that caused them to be created, then use the indices to name them.
        private string _name;

        private Location _sortKey;

        private bool _frozen;

        private readonly NamedTypeSymbol _delegateType;

        private readonly FieldSymbolsCollection _delegateFields = new FieldSymbolsCollection();

        private readonly Symbol _containingSymbol;

        /// <remarks>This is only intended to be used from <see cref="ModuleScopedDelegateCacheManager"/></remarks>
        internal ModuleScopedDelegateCacheContainer(NamespaceSymbol globalNamespace, NamedTypeSymbol delegateType)
            : base(DelegateCacheContainerKind.ModuleScopedConcrete)
        {
            _containingSymbol = globalNamespace;
            _delegateType = delegateType;
        }

        public override Symbol ContainingSymbol => _containingSymbol;

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public override string Name => _name;

        public Location SortKey => _sortKey;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        internal override FieldSymbol GetOrAddCacheField(SyntheticBoundNodeFactory factory, NamedTypeSymbol delegateType, MethodSymbol targetMethod)
        {
            Debug.Assert(!_frozen);
            Debug.Assert(_delegateType == delegateType);

            var field = _delegateFields.GetOrAdd(targetMethod, m => new ModuleScopedDelegateCacheContainerField(this, m.Name, _delegateType));

            field.AddLocation(factory.Syntax.Location);

            return field;
        }

        internal void EnsureSortKey()
        {
            Debug.Assert(HasFields);

            if (_sortKey != null)
            {
                return;
            }

            Location sortKey = null;

            foreach (var field in _delegateFields.Values)
            {
                field.EnsureSortKey();

                if (sortKey == null)
                {
                    sortKey = field.SortKey;
                }
                else if (DeclaringCompilation.CompareSourceLocations(sortKey, field.SortKey) > 0)
                {
                    sortKey = field.SortKey;
                }
            }

            Debug.Assert(sortKey != null);
            _sortKey = sortKey;
        }

        internal void AssignNamesAndFreeze(string moduleId, int index, int generation)
        {
            Debug.Assert(!_frozen);

            _name = GeneratedNames.MakeDelegateCacheContainerName(index, generation, moduleId);

            var fs = CollectAllCreatedFields();
            for (int i = 0; i < fs.Length; i++)
            {
                var f = fs[i];
                f.AssignName(GeneratedNames.MakeDelegateCacheContainerFieldName(f.TargetMethodName, i));
            }

            _frozen = true;
        }

        /// <remarks>The order should be fixed.</remarks>
        private ImmutableArray<ModuleScopedDelegateCacheContainerField> CollectAllCreatedFields()
        {
            Debug.Assert(HasFields);

            foreach (var field in _delegateFields.Values)
            {
                field.EnsureSortKey();
            }

            var builder = ArrayBuilder<ModuleScopedDelegateCacheContainerField>.GetInstance();

            builder.AddRange(_delegateFields.Values);
            builder.Sort(this);

            return builder.ToImmutableAndFree();
        }

        private ImmutableArray<ModuleScopedDelegateCacheContainerField> GetAllCreatedFields()
        {
            Debug.Assert(_frozen);

            return CollectAllCreatedFields();
        }

        private bool HasFields => _delegateFields != null && _delegateFields.Count != 0;

        public int Compare(ModuleScopedDelegateCacheContainerField x, ModuleScopedDelegateCacheContainerField y) => DeclaringCompilation.CompareSourceLocations(x.SortKey, y.SortKey);

        public override ImmutableArray<Symbol> GetMembers() => StaticCast<Symbol>.From(GetAllCreatedFields());
    }
}
