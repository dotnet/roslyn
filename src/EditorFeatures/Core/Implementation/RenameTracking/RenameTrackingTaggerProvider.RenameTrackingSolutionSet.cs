// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    internal sealed partial class RenameTrackingTaggerProvider
    {
        /// <summary>
        /// Tracks the solution before and after rename.
        /// </summary>
        private class RenameTrackingSolutionSet
        {
            public ISymbol Symbol { get; }
            public Solution OriginalSolution { get; }
            public Solution RenamedSolution { get; }

            public RenameTrackingSolutionSet(
                ISymbol symbolToRename,
                Solution originalSolution,
                Solution renamedSolution)
            {
                Symbol = symbolToRename;
                OriginalSolution = originalSolution;
                RenamedSolution = renamedSolution;
            }
        }
    }
}
