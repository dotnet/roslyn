// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class SyntaxTreeIndex
{
    public static Task<SyntaxTreeIndex?> LoadAsync(
        IChecksummedPersistentStorageService storageService, DocumentKey documentKey, Checksum? checksum, StringTable stringTable, CancellationToken cancellationToken)
    {
        return LoadAsync(storageService, documentKey, checksum, stringTable, ReadIndex, cancellationToken);
    }

    public override void WriteTo(ObjectWriter writer)
    {
        _literalInfo.WriteTo(writer);
        _identifierInfo.WriteTo(writer);
        _contextInfo.WriteTo(writer);

        writer.WriteInt32(_aliasInfo?.Count ?? 0);
        if (_aliasInfo != null)
        {
            foreach (var (alias, name, arity, isGlobal) in _aliasInfo)
            {
                writer.WriteString(alias);
                writer.WriteString(name);
                writer.WriteInt32(arity);
                writer.WriteBoolean(isGlobal);
            }
        }

        writer.WriteInt32(_interceptsLocationInfo?.Count ?? 0);
        if (_interceptsLocationInfo is not null)
        {
            foreach (var (interceptsLocationData, span) in _interceptsLocationInfo)
            {
                writer.WriteByteArray(ImmutableCollectionsMarshal.AsArray(interceptsLocationData.ContentHash)!);
                writer.WriteInt32(interceptsLocationData.Position);
                writer.WriteInt32(span.Start);
                writer.WriteInt32(span.Length);
            }
        }
    }

    private static SyntaxTreeIndex? ReadIndex(
        StringTable stringTable, ObjectReader reader, Checksum? checksum)
    {
        var literalInfo = LiteralInfo.TryReadFrom(reader);
        var identifierInfo = IdentifierInfo.TryReadFrom(reader);
        var contextInfo = ContextInfo.TryReadFrom(reader);

        if (literalInfo == null || identifierInfo == null || contextInfo == null)
            return null;

        var aliasInfoCount = reader.ReadInt32();
        HashSet<(string alias, string name, int arity, bool isGlobal)>? aliasInfo = null;

        if (aliasInfoCount > 0)
        {
            aliasInfo = [];

            for (var i = 0; i < aliasInfoCount; i++)
            {
                aliasInfo.Add((
                    reader.ReadRequiredString(),
                    reader.ReadRequiredString(),
                    reader.ReadInt32(),
                    reader.ReadBoolean()));
            }
        }

        var interceptsLocationInfoCount = reader.ReadInt32();
        Dictionary<InterceptsLocationData, TextSpan>? interceptsLocationInfo = null;
        if (interceptsLocationInfoCount > 0)
        {
            interceptsLocationInfo = [];

            for (var i = 0; i < interceptsLocationInfoCount; i++)
            {
                interceptsLocationInfo.Add(
                    new InterceptsLocationData(
                        ImmutableCollectionsMarshal.AsImmutableArray(reader.ReadByteArray()),
                        reader.ReadInt32()),
                    new TextSpan(reader.ReadInt32(), reader.ReadInt32()));
            }
        }

        return new SyntaxTreeIndex(
            checksum,
            literalInfo.Value,
            identifierInfo.Value,
            contextInfo.Value,
            aliasInfo,
            interceptsLocationInfo);
    }
}
