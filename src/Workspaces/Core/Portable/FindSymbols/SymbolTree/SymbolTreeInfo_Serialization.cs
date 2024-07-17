// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal partial class SymbolTreeInfo
{
    private const string PrefixSymbolTreeInfo = "<SymbolTreeInfo>";
    private static readonly Checksum SerializationFormatChecksum = Checksum.Create("25");

    /// <summary>
    /// Generalized function for loading/creating/persisting data.  Used as the common core code for serialization
    /// of source and metadata SymbolTreeInfos.
    /// </summary>
    private static async Task<SymbolTreeInfo> LoadOrCreateAsync(
        SolutionServices services,
        SolutionKey solutionKey,
        Checksum checksum,
        Func<Checksum, ValueTask<SymbolTreeInfo>> createAsync,
        string keySuffix,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.SymbolTreeInfo_TryLoadOrCreate, cancellationToken))
        {
            // Ok, we can use persistence.  First try to load from the persistence service. The data in the
            // persistence store must match the checksum passed in.

            var read = await LoadAsync(services, solutionKey, checksum, checksumMustMatch: true, keySuffix, cancellationToken).ConfigureAwait(false);
            if (read != null)
            {
                // If we were able to read something in, it's checksum better
                // have matched the checksum we expected.
                Debug.Assert(read.Checksum == checksum);
                return read;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Now, try to create a new instance and write it to the persistence service.
            SymbolTreeInfo result;
            using (Logger.LogBlock(FunctionId.SymbolTreeInfo_Create, cancellationToken))
            {
                result = await createAsync(checksum).ConfigureAwait(false);
                Contract.ThrowIfNull(result);
            }

            var persistentStorageService = services.GetPersistentStorageService();

            var storage = await persistentStorageService.GetStorageAsync(solutionKey, cancellationToken).ConfigureAwait(false);

            using (var stream = SerializableBytes.CreateWritableStream())
            {
                using (var writer = new ObjectWriter(stream, leaveOpen: true))
                {
                    result.WriteTo(writer);
                }

                stream.Position = 0;

                var key = PrefixSymbolTreeInfo + keySuffix;
                await storage.WriteStreamAsync(key, stream, checksum, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
    }

    private static async Task<SymbolTreeInfo?> LoadAsync(
        SolutionServices services,
        SolutionKey solutionKey,
        Checksum checksum,
        bool checksumMustMatch,
        string keySuffix,
        CancellationToken cancellationToken)
    {
        var persistentStorageService = services.GetPersistentStorageService();

        var storage = await persistentStorageService.GetStorageAsync(solutionKey, cancellationToken).ConfigureAwait(false);

        // Get the unique key to identify our data.
        var key = PrefixSymbolTreeInfo + keySuffix;

        // If the checksum doesn't need to match, then we can pass in 'null' here allowing any result to be found.
        using var stream = await storage.ReadStreamAsync(key, checksumMustMatch ? checksum : null, cancellationToken).ConfigureAwait(false);
        using var reader = ObjectReader.TryGetReader(stream);

        // We have some previously persisted data.  Attempt to read it back.  
        // If we're able to, and the version of the persisted data matches
        // our version, then we can reuse this instance.
        return TryReadSymbolTreeInfo(reader, checksum);
    }

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

        var spellChecker = _spellChecker;

        if (spellChecker is null)
        {
            writer.WriteBoolean(false);
        }
        else
        {
            writer.WriteBoolean(true);
            spellChecker.WriteTo(writer);
        }

        return;

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

    private static SymbolTreeInfo? TryReadSymbolTreeInfo(
        ObjectReader? reader, Checksum checksum)
    {
        if (reader == null)
            return null;

        try
        {
            var nodeCount = reader.ReadInt32();
            using var _ = ArrayBuilder<Node>.GetInstance(nodeCount, out var nodes);

            for (var i = 0; i < nodeCount; i++)
            {
                var name = reader.ReadRequiredString();
                var groupCount = reader.ReadInt32();
                for (var j = 0; j < groupCount; j++)
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

            MultiDictionary<string, ExtensionMethodInfo>? receiverTypeNameToExtensionMethodMap;

            var keyCount = reader.ReadInt32();
            if (keyCount == 0)
            {
                receiverTypeNameToExtensionMethodMap = null;
            }
            else
            {
                receiverTypeNameToExtensionMethodMap = [];

                for (var i = 0; i < keyCount; i++)
                {
                    var typeName = reader.ReadRequiredString();
                    var valueCount = reader.ReadInt32();

                    for (var j = 0; j < valueCount; j++)
                    {
                        var containerName = reader.ReadRequiredString();
                        var name = reader.ReadRequiredString();

                        receiverTypeNameToExtensionMethodMap.Add(typeName, new ExtensionMethodInfo(containerName, name));
                    }
                }
            }

            // if we can't read in the spell checker, that's ok.  This should never happen in practice (it would
            // mean someone tweaked the data in the database), and we can just regenerate it from the information
            // stored in 'nodes' anyways.
            var spellCheckerPersisted = reader.ReadBoolean();
            var spellChecker = spellCheckerPersisted
                ? SpellChecker.TryReadFrom(reader)
                : null;

            return new SymbolTreeInfo(
                checksum, nodes.ToImmutableAndClear(), spellChecker, inheritanceMap, receiverTypeNameToExtensionMethodMap);
        }
        catch
        {
            Logger.Log(FunctionId.SymbolTreeInfo_ExceptionInCacheRead);
        }

        return null;
    }

    internal readonly partial struct TestAccessor
    {
        public static SymbolTreeInfo? ReadSymbolTreeInfo(ObjectReader reader, Checksum checksum)
            => TryReadSymbolTreeInfo(reader, checksum);
    }
}
