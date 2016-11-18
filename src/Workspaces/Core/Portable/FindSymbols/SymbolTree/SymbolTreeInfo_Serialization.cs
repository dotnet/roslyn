// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        private const string PrefixMetadataSymbolTreeInfo = "<MetadataSymbolTreeInfoPersistence>_";
        private const string SerializationFormat = "15";

        /// <summary>
        /// Loads the SymbolTreeInfo for a given assembly symbol (metadata or project).  If the
        /// info can't be loaded, it will be created (and persisted if possible).
        /// </summary>
        private static Task<SymbolTreeInfo> LoadOrCreateSourceSymbolTreeInfoAsync(
            Solution solution,
            IAssemblySymbol assembly,
            string filePath,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            return LoadOrCreateAsync(
                solution,
                filePath,
                loadOnly,
                create: version => CreateSourceSymbolTreeInfo(solution, version, assembly, filePath, cancellationToken),
                keySuffix: "",
                getVersion: info => info._version,
                readObject: reader => ReadSymbolTreeInfo(reader, (version, names, nodes) => GetSpellCheckerTask(solution, version, filePath, names, nodes)),
                writeObject: (w, i) => i.WriteTo(w),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Loads the SpellChecker for a given assembly symbol (metadata or project).  If the
        /// info can't be loaded, it will be created (and persisted if possible).
        /// </summary>
        private static Task<SpellChecker> LoadOrCreateSpellCheckerAsync(
            Solution solution,
            string filePath,
            Func<VersionStamp, SpellChecker> create)
        {
            return LoadOrCreateAsync(
                solution,
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
            if (ShouldCreateFromScratch(solution, filePath, out prefix, out version, cancellationToken))
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

            writer.WriteString(_concatenatedNames);

            writer.WriteInt32(_nodes.Length);
            foreach (var node in _nodes)
            {
                writer.WriteInt32(node.NameSpan.Start);
                writer.WriteInt32(node.NameSpan.Length);
                writer.WriteInt32(node.ParentIndex);
            }

            writer.WriteInt32(_inheritanceMap.Keys.Count);
            foreach (var kvp in _inheritanceMap)
            {
                writer.WriteInt32(kvp.Key);
                writer.WriteInt32(kvp.Value.Count);

                foreach (var v in kvp.Value)
                {
                    writer.WriteInt32(v);
                }
            }
        }

        internal static SymbolTreeInfo ReadSymbolTreeInfo_ForTestingPurposesOnly(ObjectReader reader)
        {
            return ReadSymbolTreeInfo(reader, 
                (version, names, nodes) => Task.FromResult(
                    new SpellChecker(version, nodes.Select(n => new StringSlice(names, n.NameSpan)))));
        }

        private static SymbolTreeInfo ReadSymbolTreeInfo(
            ObjectReader reader,
            Func<VersionStamp, string, Node[], Task<SpellChecker>> createSpellCheckerTask)
        {
            try
            {
                var formatVersion = reader.ReadString();
                if (string.Equals(formatVersion, SerializationFormat, StringComparison.Ordinal))
                {
                    var version = VersionStamp.ReadFrom(reader);

                    var concatenatedNames = reader.ReadString();

                    var nodeCount = reader.ReadInt32();
                    var nodes = new Node[nodeCount];
                    for (var i = 0; i < nodeCount; i++)
                    {
                        var start = reader.ReadInt32();
                        var length = reader.ReadInt32();
                        var parentIndex = reader.ReadInt32();

                        nodes[i] = new Node(new TextSpan(start, length), parentIndex);
                    }

                    var inheritanceMap = new OrderPreservingMultiDictionary<int, int>();
                    var inheritanceMapKeyCount = reader.ReadInt32();
                    for (var i = 0; i < inheritanceMapKeyCount; i++)
                    {
                        var key = reader.ReadInt32();
                        var valueCount = reader.ReadInt32();

                        for (var j = 0; j < valueCount; j++)
                        {
                            var value = reader.ReadInt32();
                            inheritanceMap.Add(key, value);
                        }
                    }

                    var spellCheckerTask = createSpellCheckerTask(version, concatenatedNames, nodes);
                    return new SymbolTreeInfo(version, concatenatedNames, nodes, spellCheckerTask, inheritanceMap);
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
