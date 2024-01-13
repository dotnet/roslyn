// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    internal sealed partial class RenameTrackingTaggerProvider
    {
        /// <summary>
        /// Tracks the solution before and after rename.
        /// </summary>
        private class RenameTrackingSolutionSet(
            ISymbol symbolToRename,
            Solution originalSolution,
            Solution renamedSolution)
        {
            public ISymbol Symbol { get; } = symbolToRename;
            public Solution OriginalSolution { get; } = originalSolution;
            public Solution RenamedSolution { get; } = renamedSolution;
        }
    }
}
