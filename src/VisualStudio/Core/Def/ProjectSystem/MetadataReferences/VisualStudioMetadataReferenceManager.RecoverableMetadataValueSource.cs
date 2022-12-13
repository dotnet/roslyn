// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
        private sealed class RecoverableMetadataValueSource : ValueSource<AssemblyMetadata>
        {
            private readonly WeakReference<AssemblyMetadata> _weakValue;
            private readonly ImmutableArray<TemporaryStorageService.TemporaryStreamStorage> _storages;

            public RecoverableMetadataValueSource(AssemblyMetadata value, ImmutableArray<TemporaryStorageService.TemporaryStreamStorage> storages)
            {
                Contract.ThrowIfFalse(storages.Length > 0);

                _weakValue = new WeakReference<AssemblyMetadata>(value);
                _storages = storages;
            }

            public override bool TryGetValue([MaybeNullWhen(false)] out AssemblyMetadata value)
            {
                if (_weakValue.TryGetTarget(out var target))
                {
                    value = target;
                    return true;
                }

                value = null;
                return false;
            }

            public override Task<AssemblyMetadata> GetValueAsync(CancellationToken cancellationToken)
                => Task.FromResult(GetValue(cancellationToken));

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
                using var _ = ArrayBuilder<ModuleMetadata>.GetInstance(_storages.Length, out var moduleBuilder);

                foreach (var storage in _storages)
                    moduleBuilder.Add(GetModuleMetadata(storage));

                var metadata = AssemblyMetadata.Create(moduleBuilder.ToImmutable());
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
