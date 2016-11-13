// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal abstract class AbstractPersistableState
    {
        public readonly VersionStamp Version;

        protected AbstractPersistableState(VersionStamp version)
        {
            this.Version = version;
        }

        protected void WriteVersion(ObjectWriter writer, string formatVersion)
        {
            writer.WriteString(formatVersion);
            this.Version.WriteTo(writer);
        }

        private static bool TryReadVersion(ObjectReader reader, string formatVersion, out VersionStamp version)
        {
            version = VersionStamp.Default;
            if (reader.ReadString() != formatVersion)
            {
                return false;
            }

            version = VersionStamp.ReadFrom(reader);
            return true;
        }

        protected static async Task<T> LoadAsync<T>(
            Document document, string persistenceName, string formatVersion,
            Func<ObjectReader, VersionStamp, T> readFrom, CancellationToken cancellationToken) where T : AbstractPersistableState
        {
            var persistentStorageService = document.Project.Solution.Workspace.Services.GetService<IPersistentStorageService>();
            var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

            // attempt to load from persisted state
            using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
            using (var stream = await storage.ReadStreamAsync(document, persistenceName, cancellationToken).ConfigureAwait(false))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var reader = new ObjectReader(stream))
                {
                    VersionStamp persistVersion;
                    if (TryReadVersion(reader, formatVersion, out persistVersion) &&
                        document.CanReusePersistedSyntaxTreeVersion(syntaxVersion, persistVersion))
                    {
                        return readFrom(reader, syntaxVersion);
                    }
                }
            }

            return null;
        }

        protected static async Task<bool> SaveAsync<T>(
            Document document, string persistenceName, string formatVersion, T data, CancellationToken cancellationToken) where T : AbstractPersistableState, IObjectWritable
        {
            Contract.Requires(!await document.IsForkedDocumentWithSyntaxChangesAsync(cancellationToken).ConfigureAwait(false));

            var persistentStorageService = document.Project.Solution.Workspace.Services.GetService<IPersistentStorageService>();

            // attempt to load from persisted state
            using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
            {
                data.WriteVersion(writer, formatVersion);
                data.WriteTo(writer);

                stream.Position = 0;
                return await storage.WriteStreamAsync(document, persistenceName, stream, cancellationToken).ConfigureAwait(false);
            }
        }

        protected static async Task<bool> PrecalculatedAsync(Document document, string persistenceName, string formatVersion, CancellationToken cancellationToken)
        {
            Contract.Requires(document.IsFromPrimaryBranch());

            var persistentStorageService = document.Project.Solution.Workspace.Services.GetService<IPersistentStorageService>();
            var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

            // check whether we already have info for this document
            using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
            using (var stream = await storage.ReadStreamAsync(document, persistenceName, cancellationToken).ConfigureAwait(false))
            {
                if (stream != null)
                {
                    using (var reader = new ObjectReader(stream))
                    {
                        VersionStamp persistVersion;
                        return TryReadVersion(reader, formatVersion, out persistVersion) &&
                               document.CanReusePersistedSyntaxTreeVersion(syntaxVersion, persistVersion);
                    }
                }
            }

            return false;
        }
    }
}
