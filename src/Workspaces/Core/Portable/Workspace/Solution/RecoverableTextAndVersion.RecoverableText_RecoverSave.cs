// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed partial class RecoverableTextAndVersion
    {
        private sealed partial class RecoverableText
        {
            private readonly ITemporaryStorageServiceInternal _storageService;
            public readonly VersionStamp Version;
            public readonly Diagnostic? LoadDiagnostic;
            public readonly ITextAndVersionSource? InitialSource;
            public readonly LoadTextOptions LoadTextOptions;

            public ITemporaryTextStorageInternal? _storage;

            public RecoverableText(ITextAndVersionSource source, TextAndVersion textAndVersion, LoadTextOptions options, SolutionServices services)
            {
                _initialValue = textAndVersion.Text;
                _storageService = services.GetRequiredService<ITemporaryStorageServiceInternal>();

                Version = textAndVersion.Version;
                LoadDiagnostic = textAndVersion.LoadDiagnostic;
                LoadTextOptions = options;

                if (source.CanReloadText)
                {
                    // reloadable source must not cache results
                    Contract.ThrowIfTrue(source is LoadableTextAndVersionSource { CacheResult: true });

                    InitialSource = source;
                }
            }

            public TextAndVersion ToTextAndVersion(SourceText text)
                => TextAndVersion.Create(text, Version, LoadDiagnostic);

            public ITemporaryTextStorageInternal? Storage => _storage;

            private async Task<SourceText> RecoverAsync(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_storage);

                using (Logger.LogBlock(FunctionId.Workspace_Recoverable_RecoverTextAsync, cancellationToken))
                {
                    return await _storage.ReadTextAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            private SourceText Recover(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_storage);

                using (Logger.LogBlock(FunctionId.Workspace_Recoverable_RecoverText, cancellationToken))
                {
                    return _storage.ReadText(cancellationToken);
                }
            }

            private async Task SaveAsync(SourceText text, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(_storage == null); // Cannot save more than once

                var storage = _storageService.CreateTemporaryTextStorage();
                await storage.WriteTextAsync(text, cancellationToken).ConfigureAwait(false);

                // make sure write is done before setting _storage field
                Interlocked.CompareExchange(ref _storage, storage, null);
            }

            public bool TryGetTextVersion(LoadTextOptions options, out VersionStamp version)
            {
                version = Version;
                return options == LoadTextOptions;
            }
        }
    }
}
