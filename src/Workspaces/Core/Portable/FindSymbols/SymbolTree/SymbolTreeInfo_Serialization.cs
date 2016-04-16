// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo : IObjectWritable
    {
        private const string PrefixMetadataSymbolTreeInfo = "<MetadataSymbolTreeInfoPersistence>_";
        private const string SerializationFormat = "10";

        /// <summary>
        /// Loads the SymbolTreeInfo for a given assembly symbol (metadata or project).  If the
        /// info can't be loaded, it will be created (and persisted if possible).
        /// </summary>
        private static Task<SymbolTreeInfo> LoadOrCreateSymbolTreeInfoAsync(
            Solution solution,
            IAssemblySymbol assembly,
            string filePath,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            return LoadOrCreateAsync(
                solution,
                assembly,
                filePath,
                loadOnly,
                create: version => CreateSymbolTreeInfo(solution, version, assembly, filePath, cancellationToken),
                keySuffix: "",
                getVersion: info => info._version,
                readObject: reader => ReadSymbolTreeInfo(reader, (version, nodes) => GetSpellCheckerTask(solution, version, assembly, filePath, nodes)),
                writeObject: (w, i) => i.WriteTo(w),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Loads the SpellChecker for a given assembly symbol (metadata or project).  If the
        /// info can't be loaded, it will be created (and persisted if possible).
        /// </summary>
        private static Task<SpellChecker> LoadOrCreateSpellCheckerAsync(
            Solution solution,
            IAssemblySymbol assembly,
            string filePath,
            Func<VersionStamp, SpellChecker> create)
        {
            return LoadOrCreateAsync(
                solution,
                assembly,
                filePath,
                loadOnly: false,
                create: create,
                keySuffix: "SpellChecker",
                getVersion: s => s.Version,
                readObject: SpellChecker.ReadFrom,
                writeObject: (w, i) => i.WriteTo(w),
                cancellationToken: CancellationToken.None);
        }

        /// <summary>
        /// Generalized function for loading/creating/persisting data.  Used as the common core
        /// code for serialization of SymbolTreeInfos and SpellCheckers.
        /// </summary>
        private static async Task<T> LoadOrCreateAsync<T>(
            Solution solution,
            IAssemblySymbol assembly,
            string filePath,
            bool loadOnly,
            Func<VersionStamp, T> create,
            string keySuffix,
            Func<T, VersionStamp> getVersion,
            Func<ObjectReader, T> readObject,
            Action<ObjectWriter, T> writeObject,
            CancellationToken cancellationToken) where T : class
        {
            // See if we can even use serialization.  If not, we'll just have to make the value
            // from scratch.
            string prefix;
            VersionStamp version;
            if (ShouldCreateFromScratch(solution, assembly, filePath, out prefix, out version, cancellationToken))
            {
                return loadOnly ? null : create(VersionStamp.Default);
            }

            // Ok, we can use persistence.  First try to load from the persistence service.
            var persistentStorageService = solution.Workspace.Services.GetService<IPersistentStorageService>();

            T result;
            using (var storage = persistentStorageService.GetStorage(solution))
            {
                // Get the unique key to identify our data.
                var key = PrefixMetadataSymbolTreeInfo + prefix + keySuffix;
                using (var stream = await storage.ReadStreamAsync(key, cancellationToken).ConfigureAwait(false))
                {
                    if (stream != null)
                    {
                        using (var reader = new ObjectReader(stream))
                        {
                            // We have some previously persisted data.  Attempt to read it back.  
                            // If we're able to, and the version of the persisted data matches
                            // our version, then we can reuse this instance.
                            result = readObject(reader);
                            if (result != null && VersionStamp.CanReusePersistedVersion(version, getVersion(result)))
                            {
                                return result;
                            }
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Couldn't read from the persistence service.  If we've been asked to only load
                // data and not create new instances in their absense, then there's nothing left
                // to do at this point.
                if (loadOnly)
                {
                    return null;
                }

                // Now, try to create a new instance and write it to the persistence service.
                result = create(version);
                if (result != null)
                {
                    using (var stream = SerializableBytes.CreateWritableStream())
                    using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                    {
                        writeObject(writer, result);
                        stream.Position = 0;

                        await storage.WriteStreamAsync(key, stream, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return result;
        }

        private static bool ShouldCreateFromScratch(
            Solution solution,
            IAssemblySymbol assembly,
            string filePath,
            out string prefix,
            out VersionStamp version,
            CancellationToken cancellationToken)
        {
            prefix = null;
            version = default(VersionStamp);

            var service = solution.Workspace.Services.GetService<IAssemblySerializationInfoService>();
            if (service == null)
            {
                return true;
            }

            // check whether the assembly that belong to a solution is something we can serialize
            if (!service.Serializable(solution, filePath))
            {
                return true;
            }

            if (!service.TryGetSerializationPrefixAndVersion(solution, filePath, out prefix, out version))
            {
                return true;
            }

            return false;
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
        }

        internal static SymbolTreeInfo ReadSymbolTreeInfo_ForTestingPurposesOnly(ObjectReader reader)
        {
            return ReadSymbolTreeInfo(reader, 
                (version, nodes) => Task.FromResult(new SpellChecker(version, nodes.Select(n => n.Name))));
        }

        private static SymbolTreeInfo ReadSymbolTreeInfo(
            ObjectReader reader, Func<VersionStamp, Node[], Task<SpellChecker>> createSpellCheckerTask)
        {
            try
            {
                var formatVersion = reader.ReadString();
                if (string.Equals(formatVersion, SerializationFormat, StringComparison.Ordinal))
                {
                    var version = VersionStamp.ReadFrom(reader);

                    var count = reader.ReadInt32();
                    if (count == 0)
                    {
                        return new SymbolTreeInfo(version, ImmutableArray<Node>.Empty,
                            Task.FromResult(new SpellChecker(version, BKTree.Empty)));
                    }

                    var nodes = new Node[count];
                    for (var i = 0; i < count; i++)
                    {
                        var name = reader.ReadString();
                        var parentIndex = reader.ReadInt32();

                        nodes[i] = new Node(name, parentIndex);
                    }

                    var spellCheckerTask = createSpellCheckerTask(version, nodes);
                    return new SymbolTreeInfo(version, nodes, spellCheckerTask);
                }
            }
            catch
            {
                Logger.Log(FunctionId.SymbolTreeInfo_ExceptionInCacheRead);
            }

            return null;
        }
    }
}
