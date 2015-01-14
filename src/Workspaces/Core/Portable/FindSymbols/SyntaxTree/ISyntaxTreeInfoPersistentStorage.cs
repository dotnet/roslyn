// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface ISyntaxTreeInfoPersistentStorage
    {
        VersionStamp GetIdentifierSetVersion(Document document);
        bool ReadIdentifierPositions(Document document, VersionStamp version, string identifier, List<int> positions, CancellationToken cancellationToken);
        bool WriteIdentifierLocations(Document document, VersionStamp version, SyntaxNode root, CancellationToken cancellationToken);
    }
}
