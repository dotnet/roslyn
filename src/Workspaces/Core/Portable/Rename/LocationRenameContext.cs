// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Represents the information for a single replacement in the syntax tree.
    /// </summary>
    internal readonly record struct LocationRenameContext
    {
        public RenameLocation RenameLocation { get; init; }
        public string ReplacementText { get; init; }
        public string OriginalText { get; init; }
        public bool ReplacementTextValid { get; init; }

        public LocationRenameContext(
            RenameLocation renameLocation,
            bool replacementTextValid,
            string replacementText,
            string originalText)
        {
            RenameLocation = renameLocation;
            ReplacementTextValid = replacementTextValid;
            ReplacementText = replacementText;
            OriginalText = originalText;
        }
    }
}
