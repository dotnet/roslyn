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
            return LoadAsync(storageService, documentKey, checksum, stringTable, ReadIndexAsync, cancellationToken);
        }

        public override async ValueTask WriteToAsync(ObjectWriter writer)
        {
            await _literalInfo.WriteToAsync(writer).ConfigureAwait(false);
            await _identifierInfo.WriteToAsync(writer).ConfigureAwait(false);
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

        private static async ValueTask<SyntaxTreeIndex?> ReadIndexAsync(
            StringTable stringTable, ObjectReader reader, Checksum? checksum)
        {
            var literalInfo = await LiteralInfo.TryReadFromAsync(reader).ConfigureAwait(false);
            var identifierInfo = await IdentifierInfo.TryReadFromAsync(reader).ConfigureAwait(false);
            var contextInfo = await ContextInfo.TryReadFromAsync(reader).ConfigureAwait(false);

            if (literalInfo == null || identifierInfo == null || contextInfo == null)
                return null;

            var globalAliasInfoCount = await reader.ReadInt32Async().ConfigureAwait(false);
            HashSet<(string alias, string name, int arity)>? globalAliasInfo = null;

            if (globalAliasInfoCount > 0)
            {
                globalAliasInfo = new HashSet<(string alias, string name, int arity)>();

                for (var i = 0; i < globalAliasInfoCount; i++)
                {
                    var alias = await reader.ReadStringAsync().ConfigureAwait(false);
                    var name = await reader.ReadStringAsync().ConfigureAwait(false);
                    var arity = await reader.ReadInt32Async().ConfigureAwait(false);
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
