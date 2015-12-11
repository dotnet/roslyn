// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo : IObjectWritable
    {
        private const string PrefixMetadataSymbolTreeInfo = "<MetadataSymbolTreeInfoPersistence>_";
        private const string SerializationFormat = "6";

        /// <summary>
        /// this is for a metadata reference in a solution
        /// </summary>
        private static async Task<SymbolTreeInfo> LoadOrCreateAsync(Solution solution, IAssemblySymbol assembly, string filePath, CancellationToken cancellationToken)
        {
            var service = solution.Workspace.Services.GetService<IAssemblySerializationInfoService>();
            if (service == null)
            {
                return Create(VersionStamp.Default, assembly, cancellationToken);
            }

            // check whether the assembly that belong to a solution is something we can serialize
            if (!service.Serializable(solution, filePath))
            {
                return Create(VersionStamp.Default, assembly, cancellationToken);
            }

            string prefix;
            VersionStamp version;
            if (!service.TryGetSerializationPrefixAndVersion(solution, filePath, out prefix, out version))
            {
                return Create(VersionStamp.Default, assembly, cancellationToken);
            }

            var persistentStorageService = solution.Workspace.Services.GetService<IPersistentStorageService>();

            // okay, see whether we can get one from persistence service.
            // attempt to load from persisted state. metadata reference is solution wise information
            SymbolTreeInfo info;
            using (var storage = persistentStorageService.GetStorage(solution))
            {
                var key = PrefixMetadataSymbolTreeInfo + prefix;
                using (var stream = await storage.ReadStreamAsync(key, cancellationToken).ConfigureAwait(false))
                {
                    if (stream != null)
                    {
                        using (var reader = new ObjectReader(stream))
                        {
                            info = ReadFrom(reader);
                            if (info != null && VersionStamp.CanReusePersistedVersion(version, info._version))
                            {
                                return info;
                            }
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // compute it if we couldn't load it from cache
                info = Create(version, assembly, cancellationToken);
                if (info != null)
                {
                    using (var stream = SerializableBytes.CreateWritableStream())
                    using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                    {
                        info.WriteTo(writer);
                        stream.Position = 0;

                        await storage.WriteStreamAsync(key, stream, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return info;
        }

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteString(SerializationFormat);
            _version.WriteTo(writer);

            writer.WriteInt32(_nodes.Count);
            foreach (var node in _nodes)
            {
                writer.WriteString(node.Name);
                writer.WriteInt32(node.ParentIndex);
            }

            _bkTree.WriteTo(writer);
        }

        internal static SymbolTreeInfo ReadFrom(ObjectReader reader)
        {
            try
            {
                var formatVersion = reader.ReadString();
                if (!string.Equals(formatVersion, SerializationFormat, StringComparison.Ordinal))
                {
                    return null;
                }

                var version = VersionStamp.ReadFrom(reader);

                var count = reader.ReadInt32();
                if (count == 0)
                {
                    return new SymbolTreeInfo(version, ImmutableArray<Node>.Empty, BKTree.Empty);
                }

                var nodes = new Node[count];
                for (var i = 0; i < count; i++)
                {
                    var name = reader.ReadString();
                    var parentIndex = reader.ReadInt32();

                    nodes[i] = new Node(name, parentIndex);
                }

                return new SymbolTreeInfo(version, nodes, BKTree.ReadFrom(reader));
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
