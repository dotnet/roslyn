// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities;

internal readonly partial struct BKTree
{
    internal void WriteTo(ObjectWriter writer)
    {
        writer.WriteInt32(_concatenatedLowerCaseWords.Length);
        foreach (var c in _concatenatedLowerCaseWords)
            writer.WriteChar(c);

        writer.WriteInt32(_nodes.Length);
        foreach (var node in _nodes)
            node.WriteTo(writer);

        writer.WriteInt32(_edges.Length);
        foreach (var edge in _edges)
            edge.WriteTo(writer);
    }

    internal static BKTree? ReadFrom(ObjectReader reader)
    {
        try
        {
            var concatenatedLowerCaseWords = new char[reader.ReadInt32()];
            for (var i = 0; i < concatenatedLowerCaseWords.Length; i++)
                concatenatedLowerCaseWords[i] = reader.ReadChar();

            var nodeCount = reader.ReadInt32();
            using var _1 = ArrayBuilder<Node>.GetInstance(nodeCount, out var nodes);
            for (var i = 0; i < nodeCount; i++)
                nodes.Add(Node.ReadFrom(reader));

            var edgeCount = reader.ReadInt32();
            using var _2 = ArrayBuilder<Edge>.GetInstance(edgeCount, out var edges);
            for (var i = 0; i < edgeCount; i++)
                edges.Add(Edge.ReadFrom(reader));

            return new BKTree(concatenatedLowerCaseWords, nodes.ToImmutableAndClear(), edges.ToImmutableAndClear());
        }
        catch
        {
            Logger.Log(FunctionId.BKTree_ExceptionInCacheRead);
            return null;
        }
    }
}
