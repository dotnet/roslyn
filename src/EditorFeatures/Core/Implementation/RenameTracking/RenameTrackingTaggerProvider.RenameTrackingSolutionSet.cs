﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
