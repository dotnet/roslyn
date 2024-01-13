﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class TopLevelSyntaxTreeIndex
    {
        public static Task<TopLevelSyntaxTreeIndex?> LoadAsync(
            IChecksummedPersistentStorageService storageService, DocumentKey documentKey, Checksum? checksum, StringTable stringTable, CancellationToken cancellationToken)
        {
            return LoadAsync(storageService, documentKey, checksum, stringTable, ReadIndex, cancellationToken);
        }

        public override void WriteTo(ObjectWriter writer)
        {
            _declarationInfo.WriteTo(writer);
            _extensionMethodInfo.WriteTo(writer);
        }

        private static TopLevelSyntaxTreeIndex? ReadIndex(
            StringTable stringTable, ObjectReader reader, Checksum? checksum)
        {
            var declarationInfo = DeclarationInfo.TryReadFrom(stringTable, reader);
            var extensionMethodInfo = ExtensionMethodInfo.TryReadFrom(reader);

            if (declarationInfo == null || extensionMethodInfo == null)
                return null;

            return new TopLevelSyntaxTreeIndex(
                checksum,
                declarationInfo.Value,
                extensionMethodInfo.Value);
        }
    }
}
