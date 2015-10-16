// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        private SemaphoreSlim _gateDoNotAccessDirectly; // Lazily created. Access via the Gate property
        private ValueSource<TextAndVersion> _initialSource;

        private RecoverableText _text;
        private VersionStamp _version;
        private string _filePath;

        public RecoverableTextAndVersion(
            ValueSource<TextAndVersion> initialTextAndVersion,
            ITemporaryStorageService storageService)
        {
            _initialSource = initialTextAndVersion;
            _storageService = storageService;
        }

        private SemaphoreSlim Gate => LazyInitialization.EnsureInitialized(ref _gateDoNotAccessDirectly, SemaphoreSlimFactory.Instance);

        public override bool TryGetValue(out TextAndVersion value)
        {
            SourceText text;
            if (_text != null && _text.TryGetValue(out text))
            {
                value = TextAndVersion.Create(text, _version, _filePath);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public bool TryGetTextVersion(out VersionStamp version)
        {
            version = _version;

            // if the TextAndVersion has not been stored yet, but it has been observed
            // then try to get version from cached value.
            if (version == default(VersionStamp))
            {
                TextAndVersion textAndVersion;
                if (this.TryGetValue(out textAndVersion))
                {
                    version = textAndVersion.Version;
                }
            }

            return version != default(VersionStamp);
        }

        public override TextAndVersion GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_text == null)
            {
                using (Gate.DisposableWait(cancellationToken))
                {
                    if (_text == null)
                    {
                        return InitRecoverable(_initialSource.GetValue(cancellationToken));
                    }
                }
            }

            return TextAndVersion.Create(_text.GetValue(cancellationToken), _version, _filePath);
        }

        public override async Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_text == null)
            {
                using (await Gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_text == null)
                    {
                        return InitRecoverable(await _initialSource.GetValueAsync(cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            var text = await _text.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return TextAndVersion.Create(text, _version, _filePath);
        }

        private TextAndVersion InitRecoverable(TextAndVersion textAndVersion)
        {
            _initialSource = null;
            _version = textAndVersion.Version;
            _filePath = textAndVersion.FilePath;
            _text = new RecoverableText(this, textAndVersion.Text);
            _text.GetValue(CancellationToken.None); // force access to trigger save
            return textAndVersion;
        }

        private sealed class RecoverableText : RecoverableWeakValueSource<SourceText>
        {
            private readonly RecoverableTextAndVersion _parent;
            private ITemporaryTextStorage _storage;

            public RecoverableText(RecoverableTextAndVersion parent, SourceText text)
                : base(new ConstantValueSource<SourceText>(text))
            {
                _parent = parent;
            }

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

            protected override Task SaveAsync(SourceText text, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(_storage == null); // Cannot save more than once

                _storage = _parent._storageService.CreateTemporaryTextStorage(CancellationToken.None);
                return _storage.WriteTextAsync(text);
            }
        }
    }
}
