// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo : IObjectWritable
    {
        private const string PrefixMetadataSymbolTreeInfo = "<SymbolTreeInfo>";
        private static readonly Checksum SerializationFormatChecksum = Checksum.Create("22");

        /// <summary>
        /// Loads the SpellChecker for a given assembly symbol (metadata or project).  If the
        /// info can't be loaded, it will be created (and persisted if possible).
        /// </summary>
        private static Task<SpellChecker> LoadOrCreateSpellCheckerAsync(
            HostWorkspaceServices services,
            SolutionKey solutionKey,
            Checksum checksum,
            string filePath,
            ImmutableArray<Node> sortedNodes)
        {
            var result = TryLoadOrCreateAsync(
                services,
                solutionKey,
                checksum,
                loadOnly: false,
                createAsync: () => CreateSpellCheckerAsync(checksum, sortedNodes),
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
            HostWorkspaceServices services,
            SolutionKey solutionKey,
            Checksum checksum,
            bool loadOnly,
            Func<Task<T>> createAsync,
            string keySuffix,
            Func<ObjectReader, T> tryReadObject,
            CancellationToken cancellationToken) where T : class, IObjectWritable, IChecksummedObject
        {
            using (Logger.LogBlock(FunctionId.SymbolTreeInfo_TryLoadOrCreate, cancellationToken))
            {
                if (checksum == null)
                {
                    return loadOnly ? null : await CreateWithLoggingAsync().ConfigureAwait(false);
                }

                // Ok, we can use persistence.  First try to load from the persistence service.
                var persistentStorageService = services.GetPersistentStorageService();

                var storage = await persistentStorageService.GetStorageAsync(solutionKey, cancellationToken).ConfigureAwait(false);
                await using var _ = storage.ConfigureAwait(false);

                // Get the unique key to identify our data.
                var key = PrefixMetadataSymbolTreeInfo + keySuffix;
                using (var stream = await storage.ReadStreamAsync(key, checksum, cancellationToken).ConfigureAwait(false))
                using (var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken))
                {
                    if (reader != null)
                    {
                        // We have some previously persisted data.  Attempt to read it back.  
                        // If we're able to, and the version of the persisted data matches
                        // our version, then we can reuse this instance.
                        var read = tryReadObject(reader);
                        if (read != null)
                        {
                            // If we were able to read something in, it's checksum better
                            // have matched the checksum we expected.
                            Debug.Assert(read.Checksum == checksum);
                            return read;
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
                var result = await CreateWithLoggingAsync().ConfigureAwait(false);
                Contract.ThrowIfNull(result);

                using (var stream = SerializableBytes.CreateWritableStream())
                {
                    using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
                    {
                        result.WriteTo(writer);
                    }

                    stream.Position = 0;

                    await storage.WriteStreamAsync(key, stream, checksum, cancellationToken).ConfigureAwait(false);
                }

                return result;
            }

            async Task<T> CreateWithLoggingAsync()
            {
                using (Logger.LogBlock(FunctionId.SymbolTreeInfo_Create, cancellationToken))
                {
                    return await createAsync().ConfigureAwait(false);
                }
            }
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteInt32(_nodes.Length);
            foreach (var group in GroupByName(_nodes.AsMemory()))
            {
                writer.WriteString(group.Span[0].Name);
                writer.WriteInt32(group.Length);
                foreach (var item in group.Span)
                {
                    writer.WriteInt32(item.ParentIndex);
                }
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

            if (_receiverTypeNameToExtensionMethodMap == null)
            {
                writer.WriteInt32(0);
            }
            else
            {
                writer.WriteInt32(_receiverTypeNameToExtensionMethodMap.Count);
                foreach (var key in _receiverTypeNameToExtensionMethodMap.Keys)
                {
                    writer.WriteString(key);

                    var values = _receiverTypeNameToExtensionMethodMap[key];
                    writer.WriteInt32(values.Count);

                    foreach (var value in values)
                    {
                        writer.WriteString(value.FullyQualifiedContainerName);
                        writer.WriteString(value.Name);
                    }
                }
            }

            // sortedNodes is an array of Node instances which is often sorted by Node.Name by the caller. This method
            // produces a sequence of spans within sortedNodes for Node instances that all have the same Name, allowing
            // serialization to record the string once followed by the remaining properties for the nodes in the group.
            static IEnumerable<ReadOnlyMemory<Node>> GroupByName(ReadOnlyMemory<Node> sortedNodes)
            {
                if (sortedNodes.IsEmpty)
                    yield break;

                var startIndex = 0;
                var currentName = sortedNodes.Span[0].Name;
                for (var i = 1; i < sortedNodes.Length; i++)
                {
                    var node = sortedNodes.Span[i];
                    if (node.Name != currentName)
                    {
                        yield return sortedNodes[startIndex..i];
                        startIndex = i;
                    }
                }

                yield return sortedNodes[startIndex..sortedNodes.Length];
            }
        }

        private static SymbolTreeInfo TryReadSymbolTreeInfo(
            ObjectReader reader,
            Checksum checksum,
            Func<ImmutableArray<Node>, Task<SpellChecker>> createSpellCheckerTask)
        {
            try
            {
                var nodeCount = reader.ReadInt32();
                var nodes = ArrayBuilder<Node>.GetInstance(nodeCount);
                while (nodes.Count < nodeCount)
                {
                    var name = reader.ReadString();
                    var groupCount = reader.ReadInt32();
                    for (var i = 0; i < groupCount; i++)
                    {
                        var parentIndex = reader.ReadInt32();
                        nodes.Add(new Node(name, parentIndex));
                    }
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

                MultiDictionary<string, ExtensionMethodInfo> receiverTypeNameToExtensionMethodMap;

                var keyCount = reader.ReadInt32();
                if (keyCount == 0)
                {
                    receiverTypeNameToExtensionMethodMap = null;
                }
                else
                {
                    receiverTypeNameToExtensionMethodMap = new MultiDictionary<string, ExtensionMethodInfo>();

                    for (var i = 0; i < keyCount; i++)
                    {
                        var typeName = reader.ReadString();
                        var valueCount = reader.ReadInt32();

                        for (var j = 0; j < valueCount; j++)
                        {
                            var containerName = reader.ReadString();
                            var name = reader.ReadString();

                            receiverTypeNameToExtensionMethodMap.Add(typeName, new ExtensionMethodInfo(containerName, name));
                        }
                    }
                }

                var nodeArray = nodes.ToImmutableAndFree();
                var spellCheckerTask = createSpellCheckerTask(nodeArray);
                return new SymbolTreeInfo(
                    checksum, nodeArray, spellCheckerTask, inheritanceMap,
                    receiverTypeNameToExtensionMethodMap);
            }
            catch
            {
                Logger.Log(FunctionId.SymbolTreeInfo_ExceptionInCacheRead);
            }

            return null;
        }

        internal readonly partial struct TestAccessor
        {
            internal static SymbolTreeInfo ReadSymbolTreeInfo(
                ObjectReader reader, Checksum checksum)
            {
                return TryReadSymbolTreeInfo(reader, checksum,
                    nodes => Task.FromResult(new SpellChecker(checksum, nodes.Select(n => n.Name.AsMemory()))));
            }
        }
    }
}
