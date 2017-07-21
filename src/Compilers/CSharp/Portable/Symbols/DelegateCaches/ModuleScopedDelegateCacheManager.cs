// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    using ModuleScopedContainerCollection = ConcurrentDictionary<NamedTypeSymbol, ModuleScopedDelegateCacheContainer>;

    internal sealed class ModuleScopedDelegateCacheManager : IComparer<ModuleScopedDelegateCacheContainer>
    {
        private bool _frozen;

        private readonly PEModuleBuilder _moduleBuilder;

        private ModuleScopedContainerCollection _lazyModuleScopedContainers;

        public ModuleScopedDelegateCacheManager(PEModuleBuilder moduleBuilder)
        {
            _moduleBuilder = moduleBuilder;
        }

        private ModuleScopedContainerCollection ModuleScopedContainers
        {
            get
            {
                if (_lazyModuleScopedContainers == null)
                {
                    Interlocked.CompareExchange(ref _lazyModuleScopedContainers, new ModuleScopedContainerCollection(), null);
                }

                return _lazyModuleScopedContainers;
            }
        }

        internal DelegateCacheContainer GetOrAddContainer(NamedTypeSymbol delegateType)
        {
            Debug.Assert(!_frozen);

            var globalNamespace = _moduleBuilder.SourceModule.GlobalNamespace;
            return ModuleScopedContainers.GetOrAdd(delegateType, t => new ModuleScopedDelegateCacheContainer(globalNamespace, t));
        }

        internal void AssignNamesAndFreeze(DiagnosticBag diagnostics)
        {
            Debug.Assert(!_frozen);

            if (_lazyModuleScopedContainers != null)
            {
                var moduleId = _moduleBuilder.GetModuleIdForSynthesizedTopLevelTypes();
                var generation = _moduleBuilder.CurrentGenerationOrdinal;
                var containers = CollectAllCreatedContainers();

                for (int i = 0; i < containers.Length; i++)
                {
                    containers[i].AssignNamesAndFreeze(moduleId, i, generation);
                }
            }

            _frozen = true;
        }

        internal ImmutableArray<ModuleScopedDelegateCacheContainer> GetAllCreatedContainers()
        {
            Debug.Assert(_frozen);

            return CollectAllCreatedContainers();
        }

        /// <remarks>The order should be fixed.</remarks>
        private ImmutableArray<ModuleScopedDelegateCacheContainer> CollectAllCreatedContainers()
        {
            var containers = _lazyModuleScopedContainers;
            if (containers == null)
            {
                return ImmutableArray<ModuleScopedDelegateCacheContainer>.Empty;
            }

            foreach (var container in containers.Values)
            {
                container.EnsureSortKey();
            }

            var builder = ArrayBuilder<ModuleScopedDelegateCacheContainer>.GetInstance();

            builder.AddRange(containers.Values);
            builder.Sort(this);

            return builder.ToImmutableAndFree();
        }

        public int Compare(ModuleScopedDelegateCacheContainer x, ModuleScopedDelegateCacheContainer y) => _moduleBuilder.Compilation.CompareSourceLocations(x.SortKey, y.SortKey);
    }
}
