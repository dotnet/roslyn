// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Roslyn.Utilities;

internal readonly partial struct BKTree
{
    internal void WriteTo(ObjectWriter writer)
    {
        writer.WriteValue(_concatenatedLowerCaseWords);
        writer.WriteArray(_nodes, static (w, n) => n.WriteTo(w));
        writer.WriteArray(_edges, static (w, n) => n.WriteTo(w));
    }

    internal static async ValueTask<BKTree?> ReadFromAsync(ObjectReader reader)
    {
        try
        {
            return new BKTree(
                (char[])await reader.ReadValueAsync().ConfigureAwait(false),
                await reader.ReadArrayAsync(Node.ReadFromAsync).ConfigureAwait(false),
                await reader.ReadArrayAsync(Edge.ReadFromAsync).ConfigureAwait(false));
        }
        catch
        {
            Logger.Log(FunctionId.BKTree_ExceptionInCacheRead);
            return null;
        }
    }
}
