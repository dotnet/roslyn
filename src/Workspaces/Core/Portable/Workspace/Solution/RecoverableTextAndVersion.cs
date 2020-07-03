// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    internal class RecoverableTextAndVersion : ValueSource<TextAndVersion>, ITextVersionable
    {
        private readonly ITemporaryStorageService _storageService;

        private SemaphoreSlim? _lazyGate;
        private ValueSource<TextAndVersion>? _initialSource;

        private RecoverableText? _text;
        private VersionStamp _version;
        private string? _filePath;
        private Diagnostic? _loadDiagnostic;

        public RecoverableTextAndVersion(
            ValueSource<TextAndVersion> initialTextAndVersion,
            ITemporaryStorageService storageService)
        {
            _initialSource = initialTextAndVersion;
            _storageService = storageService;
        }

        private SemaphoreSlim Gate => LazyInitialization.EnsureInitialized(ref _lazyGate, SemaphoreSlimFactory.Instance);

        public ITemporaryTextStorage? Storage => _text?.Storage;

        public override bool TryGetValue([MaybeNullWhen(false)] out TextAndVersion value)
        {
            if (_text != null && _text.TryGetValue(out var text))
            {
                value = TextAndVersion.Create(text, _version, _filePath, _loadDiagnostic);
                return true;
            }
            else
            {
                value = null!;
                return false;
            }
        }

        public bool TryGetTextVersion(out VersionStamp version)
        {
            version = _version;

            // if the TextAndVersion has not been stored yet, but it has been observed
            // then try to get version from cached value.
            if (version == default)
            {
                if (TryGetValue(out var textAndVersion))
                {
                    version = textAndVersion.Version;
                }
                else if (_initialSource is ITextVersionable textVersionable)
                {
                    return textVersionable.TryGetTextVersion(out version);
                }
            }

            return version != default;
        }

        public override TextAndVersion GetValue(CancellationToken cancellationToken = default)
        {
            if (_text == null)
            {
                using (Gate.DisposableWait(cancellationToken))
                {
                    if (_text == null)
                    {
                        return InitRecoverable(_initialSource!.GetValue(cancellationToken));
                    }
                }
            }

            return TextAndVersion.Create(_text.GetValue(cancellationToken), _version, _filePath, _loadDiagnostic);
        }

        public override async Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken = default)
        {
            if (_text == null)
            {
                using (await Gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_text == null)
                    {
                        return InitRecoverable(await _initialSource!.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            var text = await _text.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return TextAndVersion.Create(text, _version, _filePath, _loadDiagnostic);
        }

        private TextAndVersion InitRecoverable(TextAndVersion textAndVersion)
        {
            _initialSource = null;
            _version = textAndVersion.Version;
            _filePath = textAndVersion.FilePath;
            _loadDiagnostic = textAndVersion.LoadDiagnostic;
            _text = new RecoverableText(this, textAndVersion.Text);
            _text.GetValue(CancellationToken.None); // force access to trigger save
            return textAndVersion;
        }

        private sealed class RecoverableText : WeaklyCachedRecoverableValueSource<SourceText>
        {
            private readonly RecoverableTextAndVersion _parent;
            private ITemporaryTextStorage? _storage;

            public RecoverableText(RecoverableTextAndVersion parent, SourceText text)
                : base(new ConstantValueSource<SourceText>(text))
            {
                _parent = parent;
            }

            public ITemporaryTextStorage? Storage => _storage;

            protected override async Task<SourceText> RecoverAsync(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_storage);

                using (Logger.LogBlock(FunctionId.Workspace_Recoverable_RecoverTextAsync, _parent._filePath, cancellationToken))
                {
                    return await _storage.ReadTextAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            protected override SourceText Recover(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_storage);

                using (Logger.LogBlock(FunctionId.Workspace_Recoverable_RecoverText, _parent._filePath, cancellationToken))
                {
                    return _storage.ReadText(cancellationToken);
                }
            }

            protected override async Task SaveAsync(SourceText text, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(_storage == null); // Cannot save more than once

                var storage = _parent._storageService.CreateTemporaryTextStorage(cancellationToken);
                await storage.WriteTextAsync(text, cancellationToken).ConfigureAwait(false);

                // make sure write is done before setting _storage field
                Interlocked.CompareExchange(ref _storage, storage, null);
            }
        }
    }
}
