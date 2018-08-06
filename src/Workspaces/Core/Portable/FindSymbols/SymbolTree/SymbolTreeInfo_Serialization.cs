﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo : IObjectWritable
    {
        private const string PrefixMetadataSymbolTreeInfo = "<SymbolTreeInfo>";
        private const string SerializationFormat = "17";

        /// <summary>
        /// Loads the SpellChecker for a given assembly symbol (metadata or project).  If the
        /// info can't be loaded, it will be created (and persisted if possible).
        /// </summary>
        private static Task<SpellChecker> LoadOrCreateSpellCheckerAsync(
            Solution solution,
            Checksum checksum,
            string filePath,
            string concatenatedNames,
            ImmutableArray<Node> sortedNodes)
        {
            var result = TryLoadOrCreateAsync(
                solution,
                checksum,
                loadOnly: false,
                createAsync: () => CreateSpellCheckerAsync(checksum, concatenatedNames, sortedNodes),
                keySuffix: "_SpellChecker_" + filePath,
                tryReadObject: SpellChecker.TryReadFrom,
                cancellationToken: CancellationToken.None);
            Contract.ThrowIfNull(result, "Result should never be null as we passed 'loadOnly: false'.");
            return result;
        }

        /// <summary>
        /// Generalized function for loading/creating/persisting data.  Used as the common core
        /// code for serialization of SymbolTreeInfos and SpellCheckers.
        /// </summary>
        private static async Task<T> TryLoadOrCreateAsync<T>(
            Solution solution,
            Checksum checksum,
            bool loadOnly,
            Func<Task<T>> createAsync,
            string keySuffix,
            Func<ObjectReader, T> tryReadObject,
            CancellationToken cancellationToken) where T : class, IObjectWritable, IChecksummedObject
        {
            if (checksum == null) 
            {
                return loadOnly ? null : await createAsync().ConfigureAwait(false);
            }

            // Ok, we can use persistence.  First try to load from the persistence service.
            var persistentStorageService = (IPersistentStorageService2)solution.Workspace.Services.GetService<IPersistentStorageService>();

            T result;
            using (var storage = persistentStorageService.GetStorage(solution, checkBranchId: false))
            {
                // Get the unique key to identify our data.
                var key = PrefixMetadataSymbolTreeInfo + keySuffix;
                using (var stream = await storage.ReadStreamAsync(key, cancellationToken).ConfigureAwait(false))
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    if (reader != null)
                    {
                        // We have some previously persisted data.  Attempt to read it back.  
                        // If we're able to, and the version of the persisted data matches
                        // our version, then we can reuse this instance.
                        result = tryReadObject(reader);
                        if (result?.Checksum == checksum)
                        {
                            return result;
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Couldn't read from the persistence service.  If we've been asked to only load
                // data and not create new instances in their absence, then there's nothing left
                // to do at this point.
                if (loadOnly)
                {
                    return null;
                }

                // Now, try to create a new instance and write it to the persistence service.
                result = await createAsync().ConfigureAwait(false);
                Contract.ThrowIfNull(result);

                using (var stream = SerializableBytes.CreateWritableStream())
                using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                {
                    result.WriteTo(writer);
                    stream.Position = 0;

                    await storage.WriteStreamAsync(key, stream, cancellationToken).ConfigureAwait(false);
                }
            }

            return result;
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteString(SerializationFormat);
            Checksum.WriteTo(writer);

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

        internal static SymbolTreeInfo ReadSymbolTreeInfo_ForTestingPurposesOnly(
            ObjectReader reader, Checksum checksum)
        {
            return TryReadSymbolTreeInfo(reader, 
                (names, nodes) => Task.FromResult(
                    new SpellChecker(checksum, nodes.Select(n => new StringSlice(names, n.NameSpan)))));
        }

        private static SymbolTreeInfo TryReadSymbolTreeInfo(
            ObjectReader reader,
            Func<string, ImmutableArray<Node>, Task<SpellChecker>> createSpellCheckerTask)
        {
            try
            {
                var formatVersion = reader.ReadString();
                if (string.Equals(formatVersion, SerializationFormat, StringComparison.Ordinal))
                {
                    var checksum = Checksum.ReadFrom(reader);

                    var concatenatedNames = reader.ReadString();

                    var nodeCount = reader.ReadInt32();
                    var nodes = ArrayBuilder<Node>.GetInstance(nodeCount);
                    for (var i = 0; i < nodeCount; i++)
                    {
                        var start = reader.ReadInt32();
                        var length = reader.ReadInt32();
                        var parentIndex = reader.ReadInt32();

                        nodes.Add(new Node(new TextSpan(start, length), parentIndex));
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

                    var nodeArray = nodes.ToImmutableAndFree();
                    var spellCheckerTask = createSpellCheckerTask(concatenatedNames, nodeArray);
                    return new SymbolTreeInfo(checksum, concatenatedNames, nodeArray, spellCheckerTask, inheritanceMap);
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
