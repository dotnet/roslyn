// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;
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

        WriteTo(writer, _aliasInfo);
        WriteTo(writer, _globalAliasInfo);

        static void WriteTo(ObjectWriter writer, HashSet<(string alias, string name, int arity)>? aliasInfo)
        {
            writer.WriteInt32(aliasInfo?.Count ?? 0);
            if (aliasInfo != null)
            {
                foreach (var (alias, name, arity) in aliasInfo)
                {
                    writer.WriteString(alias);
                    writer.WriteString(name);
                    writer.WriteInt32(arity);
                }
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

        return new SyntaxTreeIndex(
            checksum,
            literalInfo.Value,
            identifierInfo.Value,
            contextInfo.Value,
            ReadAliasInfo(reader),
            ReadAliasInfo(reader));

        static HashSet<(string alias, string name, int arity)>? ReadAliasInfo(ObjectReader reader)
        {
            var aliasInfoCount = reader.ReadInt32();
            HashSet<(string alias, string name, int arity)>? aliasInfo = null;

            if (aliasInfoCount > 0)
            {
                aliasInfo = [];

                for (var i = 0; i < aliasInfoCount; i++)
                {
                    var alias = reader.ReadRequiredString();
                    var name = reader.ReadRequiredString();
                    var arity = reader.ReadInt32();
                    aliasInfo.Add((alias, name, arity));
                }
            }

            return aliasInfo;
        }
    }
}
