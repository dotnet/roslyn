// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly ITemporaryStorageService storageService;

        // these fields are assigned once during call to SaveAsync
        private ITemporaryTextStorage storage;
        private VersionStamp storedVersion;
        private string storedFilePath;

        public RecoverableTextAndVersion(
            ValueSource<TextAndVersion> initialTextAndVersion,
            ITemporaryStorageService storageService,
            ITextCacheService textCache)
            : base(initialTextAndVersion, textCache)
        {
            this.storageService = storageService;
        }

        public bool TryGetTextVersion(out VersionStamp version)
        {
            version = this.storedVersion;
            return version != default(VersionStamp);
        }

        protected override async Task<TextAndVersion> RecoverAsync(CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(this.storage);

            using (Logger.LogBlock(FunctionId.Workspace_Recoverable_RecoverTextAsync, this.storedFilePath, cancellationToken))
            {
                var text = await this.storage.ReadTextAsync(cancellationToken).ConfigureAwait(false);
                return TextAndVersion.Create(text, this.storedVersion, this.storedFilePath);
            }
        }

        protected override TextAndVersion Recover(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Workspace_Recoverable_RecoverText, this.storedFilePath, cancellationToken))
            {
                var text = this.storage.ReadText(cancellationToken);
                return TextAndVersion.Create(text, this.storedVersion, this.storedFilePath);
            }
        }

        protected override async Task SaveAsync(TextAndVersion textAndVersion, CancellationToken cancellationToken)
        {
            this.storage = this.storageService.CreateTemporaryTextStorage(CancellationToken.None);
            this.storedVersion = textAndVersion.Version;
            this.storedFilePath = textAndVersion.FilePath;
            await storage.WriteTextAsync(textAndVersion.Text).ConfigureAwait(false);
        }
    }
}