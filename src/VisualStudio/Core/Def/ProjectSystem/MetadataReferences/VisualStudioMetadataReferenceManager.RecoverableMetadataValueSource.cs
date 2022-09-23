// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
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
        private sealed class RecoverableMetadataValueSource : ValueSource<Optional<AssemblyMetadata>>
        {
            private readonly WeakReference<AssemblyMetadata> _weakValue;
            private readonly List<TemporaryStorageService.TemporaryStreamStorage> _storages;

            public RecoverableMetadataValueSource(AssemblyMetadata value, List<TemporaryStorageService.TemporaryStreamStorage> storages)
            {
                Contract.ThrowIfFalse(storages.Count > 0);

                _weakValue = new WeakReference<AssemblyMetadata>(value);
                _storages = storages;
            }

            public IEnumerable<ITemporaryStreamStorageInternal> GetStorages()
                => _storages;

            public override bool TryGetValue(out Optional<AssemblyMetadata> value)
            {
                if (_weakValue.TryGetTarget(out var target))
                {
                    value = target;
                    return true;
                }

                value = default;
                return false;
            }

            public override Task<Optional<AssemblyMetadata>> GetValueAsync(CancellationToken cancellationToken)
                => Task.FromResult(GetValue(cancellationToken));

            public override Optional<AssemblyMetadata> GetValue(CancellationToken cancellationToken)
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

            private static ModuleMetadata GetModuleMetadata(
                TemporaryStorageService.TemporaryStreamStorage storage)
            {
                // For an unmanaged memory stream, ModuleMetadata can take ownership directly.
                var stream = storage.ReadStream(CancellationToken.None);
                unsafe
                {
                    return ModuleMetadata.CreateFromMetadata((IntPtr)stream.PositionPointer, (int)stream.Length, stream.Dispose);
                }
            }
        }
    }
}
