// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Represent the rename context information for the given renameLocation.
    /// </summary>
    internal readonly record struct LocationRenameContext
    {
        public RenameLocation RenameLocation { get; init; }
        public string ReplacementText { get; init; }
        public string OriginalText { get; init; }
        public bool ReplacementTextValid { get; init; }

        public LocationRenameContext(
            RenameLocation renameLocation,
            RenamedSymbolContext symbolContext)
        {
            RenameLocation = renameLocation;
            ReplacementTextValid = symbolContext.ReplacementTextValid;
            ReplacementText = symbolContext.ReplacementText;
            OriginalText = symbolContext.OriginalText;
        }
    }
}
