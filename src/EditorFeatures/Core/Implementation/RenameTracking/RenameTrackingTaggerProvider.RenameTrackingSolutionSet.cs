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
            private readonly ISymbol _symbolToRename;
            private readonly Solution _originalSolution;
            private readonly Solution _renamedSolution;

            public ISymbol Symbol { get { return _symbolToRename; } }
            public Solution OriginalSolution { get { return _originalSolution; } }
            public Solution RenamedSolution { get { return _renamedSolution; } }

            public RenameTrackingSolutionSet(
                ISymbol symbolToRename,
                Solution originalSolution,
                Solution renamedSolution)
            {
                _symbolToRename = symbolToRename;
                _originalSolution = originalSolution;
                _renamedSolution = renamedSolution;
            }
        }
    }
}
