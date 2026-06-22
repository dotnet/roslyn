// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class NavigateToSearchIndex
{
    public static Task<NavigateToSearchIndex?> LoadAsync(
        IChecksummedPersistentStorageService storageService, DocumentKey documentKey, Checksum? checksum, StringTable stringTable, CancellationToken cancellationToken)
    {
        return LoadAsync(storageService, documentKey, checksum, stringTable, ReadIndex, cancellationToken);
    }

    public override void WriteTo(ObjectWriter writer)
    {
        _navigateToSearchInfo.WriteTo(writer);
    }

    private static NavigateToSearchIndex? ReadIndex(
        StringTable stringTable, ObjectReader reader, Checksum? checksum)
    {
        var navigateToSearchInfo = NavigateToSearchInfo.TryReadFrom(reader);

        if (navigateToSearchInfo == null)
            return null;

        return new NavigateToSearchIndex(
            checksum,
            navigateToSearchInfo.Value);
    }
}
