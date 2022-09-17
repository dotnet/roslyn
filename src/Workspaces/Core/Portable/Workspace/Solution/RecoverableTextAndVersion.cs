// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A recoverable TextAndVersion source that saves its text to temporary storage.
    /// </summary>
    internal sealed class RecoverableTextAndVersion : ValueSource<TextAndVersion>, ITextVersionable
    {
        private readonly SolutionServices _services;

        // Starts as ValueSource<TextAndVersion> and is replaced with RecoverableText when the TextAndVersion value is requested.
        // At that point the initial source is no longer referenced and can be garbage collected.
        private object _initialSourceOrRecoverableText;

        public RecoverableTextAndVersion(ValueSource<TextAndVersion> initialSource, SolutionServices services)
        {
            _initialSourceOrRecoverableText = initialSource;
            _services = services;
        }

        public ITemporaryTextStorageInternal? Storage
            => (_initialSourceOrRecoverableText as RecoverableText)?.Storage;

        public override bool TryGetValue([MaybeNullWhen(false)] out TextAndVersion value)
        {
            // store to local to avoid race:
            var sourceOrRecoverableText = _initialSourceOrRecoverableText;

            if (sourceOrRecoverableText is RecoverableText recoverableText)
            {
                if (recoverableText.TryGetValue(out var text))
                {
                    value = TextAndVersion.Create(text, recoverableText.Version, recoverableText.LoadDiagnostic);
                    return true;
                }
            }
            else
            {
                return ((ValueSource<TextAndVersion>)sourceOrRecoverableText).TryGetValue(out value);
            }

            value = null;
            return false;
        }

        public bool TryGetTextVersion(out VersionStamp version)
        {
            if (_initialSourceOrRecoverableText is ITextVersionable textVersionable)
            {
                return textVersionable.TryGetTextVersion(out version);
            }

            if (TryGetValue(out var textAndVersion))
            {
                version = textAndVersion.Version;
                return true;
            }

            version = default;
            return false;
        }

        public override TextAndVersion GetValue(CancellationToken cancellationToken = default)
        {
            if (_initialSourceOrRecoverableText is ValueSource<TextAndVersion> source)
            {
                // replace initial source with recovarable text if it hasn't been replaced already:
                Interlocked.CompareExchange(
                    ref _initialSourceOrRecoverableText,
                    value: new RecoverableText(source.GetValue(cancellationToken), _services),
                    comparand: source);
            }

            var recoverableText = (RecoverableText)_initialSourceOrRecoverableText;
            return recoverableText.ToTextAndVersion(recoverableText.GetValue(cancellationToken));
        }

        public override async Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken = default)
        {
            if (_initialSourceOrRecoverableText is ValueSource<TextAndVersion> source)
            {
                // replace initial source with recovarable text if it hasn't been replaced already:
                Interlocked.CompareExchange(
                    ref _initialSourceOrRecoverableText,
                    value: new RecoverableText(await source.GetValueAsync(cancellationToken).ConfigureAwait(false), _services),
                    comparand: source);
            }

            var recoverableText = (RecoverableText)_initialSourceOrRecoverableText;
            return recoverableText.ToTextAndVersion(await recoverableText.GetValueAsync(cancellationToken).ConfigureAwait(false));
        }

        private sealed class RecoverableText : WeaklyCachedRecoverableValueSource<SourceText>, ITextVersionable
        {
            private readonly ITemporaryStorageServiceInternal _storageService;
            public readonly VersionStamp Version;
            public readonly Diagnostic? LoadDiagnostic;

            public ITemporaryTextStorageInternal? _storage;

            public RecoverableText(TextAndVersion textAndVersion, SolutionServices services)
                : base(new ConstantValueSource<SourceText>(textAndVersion.Text))
            {
                _storageService = services.GetRequiredService<ITemporaryStorageServiceInternal>();

                Version = textAndVersion.Version;
                LoadDiagnostic = textAndVersion.LoadDiagnostic;
            }

            public TextAndVersion ToTextAndVersion(SourceText text)
                => TextAndVersion.Create(text, Version, LoadDiagnostic);

            public ITemporaryTextStorageInternal? Storage => _storage;

            protected override async Task<SourceText> RecoverAsync(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_storage);

                using (Logger.LogBlock(FunctionId.Workspace_Recoverable_RecoverTextAsync, cancellationToken))
                {
                    return await _storage.ReadTextAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            protected override SourceText Recover(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_storage);

                using (Logger.LogBlock(FunctionId.Workspace_Recoverable_RecoverText, cancellationToken))
                {
                    return _storage.ReadText(cancellationToken);
                }
            }

            protected override async Task SaveAsync(SourceText text, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(_storage == null); // Cannot save more than once

                var storage = _storageService.CreateTemporaryTextStorage();
                await storage.WriteTextAsync(text, cancellationToken).ConfigureAwait(false);

                // make sure write is done before setting _storage field
                Interlocked.CompareExchange(ref _storage, storage, null);
            }

            public bool TryGetTextVersion(out VersionStamp version)
            {
                version = Version;
                return true;
            }
        }
    }
}
