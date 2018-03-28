// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioMetadataReferenceManager
    {
        private class RecoverableMetadataValueSource : ValueSource<AssemblyMetadata>
        {
            private readonly WeakReference<AssemblyMetadata> _weakValue;
            private readonly List<ITemporaryStreamStorage> _storages;
            private readonly ConditionalWeakTable<Metadata, object> _lifetimeMap;

            public RecoverableMetadataValueSource(AssemblyMetadata value, List<ITemporaryStreamStorage> storages, ConditionalWeakTable<Metadata, object> lifetimeMap)
            {
                Contract.ThrowIfFalse(storages.Count > 0);

                _weakValue = new WeakReference<AssemblyMetadata>(value);
                _storages = storages;
                _lifetimeMap = lifetimeMap;
            }

            public IEnumerable<ITemporaryStreamStorage> GetStorages()
            {
                return _storages;
            }

            public override AssemblyMetadata GetValue(CancellationToken cancellationToken)
            {
                if (_weakValue.TryGetTarget(out var value))
                {
                    return value;
                }

                return RecoverMetadata();
            }

            private AssemblyMetadata RecoverMetadata()
            {
                var moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance(_storages.Count);

                foreach (var storage in _storages)
                {
                    moduleBuilder.Add(GetModuleMetadata(storage));
                }

                var metadata = AssemblyMetadata.Create(moduleBuilder.ToImmutableAndFree());
                _weakValue.SetTarget(metadata);

                return metadata;
            }

            private ModuleMetadata GetModuleMetadata(ITemporaryStreamStorage storage)
            {
                var stream = storage.ReadStream(CancellationToken.None);

                // under VS host, direct access should be supported
                var directAccess = (ISupportDirectMemoryAccess)stream;
                var pImage = directAccess.GetPointer();

                var metadata = ModuleMetadata.CreateFromMetadata(pImage, (int)stream.Length);

                // memory management.
                _lifetimeMap.Add(metadata, stream);
                return metadata;
            }

            public override bool TryGetValue(out AssemblyMetadata value)
            {
                if (_weakValue.TryGetTarget(out value))
                {
                    return true;
                }

                value = default;
                return false;
            }

            public override Task<AssemblyMetadata> GetValueAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(this.GetValue(cancellationToken));
            }
        }
    }
}
