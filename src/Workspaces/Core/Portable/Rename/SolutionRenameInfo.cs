// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CodeCleanup;

namespace Microsoft.CodeAnalysis.Rename
{
    internal readonly struct SolutionRenameInfo
    {
        private readonly Dictionary<ProjectId, ProjectRenameInfo> _projectIdToInfoMap;
        private readonly Dictionary<ISymbol, SymbolRenameOptions> _renameSymbolToRenameOptions;
        private readonly Dictionary<ISymbol, Location> _renameSymbolToDeclarationLocations;
        private readonly Dictionary<ISymbol, DocumentId> _renameSymbolToDeclarationDocument;

        public readonly CodeCleanupOptionsProvider FallbackOptions;

        public bool TryGetProjectRenameInfo(ProjectId projectId, [NotNullWhen(true)] out ProjectRenameInfo? projectRenameInfo)
            => _projectIdToInfoMap.TryGetValue(projectId, out projectRenameInfo);

        public bool TryGetSymbolDeclarationLocation(ISymbol symbol, [NotNullWhen(true)] out Location? symbolDeclarationLocation)
            => _renameSymbolToDeclarationLocations.TryGetValue(symbol, out symbolDeclarationLocation);

        public bool TryGetSymbolDeclarationDocumentId(ISymbol symbol, [NotNullWhen(true)] out DocumentId? symbolDeclarationDocumentId)
            => _renameSymbolToDeclarationDocument.TryGetValue(symbol, out symbolDeclarationDocumentId);

        public bool TryGetSymbolRenameOptions(ISymbol symbol, out SymbolRenameOptions symbolRenameOptions)
            => _renameSymbolToRenameOptions.TryGetValue(symbol, out symbolRenameOptions);
    }
}
