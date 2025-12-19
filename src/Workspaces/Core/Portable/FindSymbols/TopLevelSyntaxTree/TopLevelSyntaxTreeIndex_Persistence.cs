// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class TopLevelSyntaxTreeIndex
{
    public static Task<TopLevelSyntaxTreeIndex?> LoadAsync(
        IChecksummedPersistentStorageService storageService, DocumentKey documentKey, Checksum? checksum, StringTable stringTable, CancellationToken cancellationToken)
    {
        return LoadAsync(storageService, documentKey, checksum, stringTable, ReadIndex, cancellationToken);
    }

    public override void WriteTo(ObjectWriter writer)
    {
        writer.WriteBoolean(IsGeneratedCode);
        _declarationInfo.WriteTo(writer);
        _extensionMemberInfo.WriteTo(writer);
    }

    private static TopLevelSyntaxTreeIndex? ReadIndex(
        StringTable stringTable, ObjectReader reader, Checksum? checksum)
    {
        var isGeneratedCode = reader.ReadBoolean();
        var declarationInfo = DeclarationInfo.TryReadFrom(stringTable, reader);
        var extensionMemberInfo = ExtensionMemberInfo.TryReadFrom(reader);

        if (declarationInfo == null || extensionMemberInfo == null)
            return null;

        return new TopLevelSyntaxTreeIndex(
            checksum,
            isGeneratedCode,
            declarationInfo.Value,
            extensionMemberInfo.Value);
    }
}
