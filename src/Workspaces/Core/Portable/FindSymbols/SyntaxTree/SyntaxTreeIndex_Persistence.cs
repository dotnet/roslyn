// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
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

            if (_globalAliasInfo == null)
            {
                writer.WriteInt32(0);
            }
            else
            {
                writer.WriteInt32(_globalAliasInfo.Count);
                foreach (var (alias, name, arity) in _globalAliasInfo)
                {
                    writer.WriteString(alias);
                    writer.WriteString(name);
                    writer.WriteInt32(arity);
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

            var globalAliasInfoCount = reader.ReadInt32();
            HashSet<(string alias, string name, int arity)>? globalAliasInfo = null;

            if (globalAliasInfoCount > 0)
            {
                globalAliasInfo = new HashSet<(string alias, string name, int arity)>();

                for (var i = 0; i < globalAliasInfoCount; i++)
                {
                    var alias = reader.ReadString();
                    var name = reader.ReadString();
                    var arity = reader.ReadInt32();
                    globalAliasInfo.Add((alias, name, arity));
                }
            }

            return new SyntaxTreeIndex(
                checksum,
                literalInfo.Value,
                identifierInfo.Value,
                contextInfo.Value,
                globalAliasInfo);
        }
    }
}
