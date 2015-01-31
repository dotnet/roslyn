// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A text retainer that will save text to temporary storage when it is evicted from the
    /// text cache and reload from that storage if and when it is needed again.
    /// </summary>
    internal class RecoverableTextAndVersion : RecoverableCachedObjectSource<TextAndVersion>
    {
        private readonly ITemporaryStorageService _storageService;

        // these fields are assigned once during call to SaveAsync
        private ITemporaryTextStorage _storage;
        private VersionStamp _storedVersion;
        private string _storedFilePath;

        public RecoverableTextAndVersion(
            ValueSource<TextAndVersion> initialTextAndVersion,
            ITemporaryStorageService storageService)
            : base(initialTextAndVersion)
        {
            _storageService = storageService;
        }

        public bool TryGetTextVersion(out VersionStamp version)
        {
            version = _storedVersion;
            return version != default(VersionStamp);
        }

        protected override async Task<TextAndVersion> RecoverAsync(CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_storage);

            using (Logger.LogBlock(FunctionId.Workspace_Recoverable_RecoverTextAsync, _storedFilePath, cancellationToken))
            {
                var text = await _storage.ReadTextAsync(cancellationToken).ConfigureAwait(false);
                return TextAndVersion.Create(text, _storedVersion, _storedFilePath);
            }
        }

        protected override TextAndVersion Recover(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Workspace_Recoverable_RecoverText, _storedFilePath, cancellationToken))
            {
                var text = _storage.ReadText(cancellationToken);
                return TextAndVersion.Create(text, _storedVersion, _storedFilePath);
            }
        }

        protected override Task SaveAsync(TextAndVersion textAndVersion, CancellationToken cancellationToken)
        {
            _storage = _storageService.CreateTemporaryTextStorage(CancellationToken.None);
            _storedVersion = textAndVersion.Version;
            _storedFilePath = textAndVersion.FilePath;
            return _storage.WriteTextAsync(textAndVersion.Text);
        }
    }
}
