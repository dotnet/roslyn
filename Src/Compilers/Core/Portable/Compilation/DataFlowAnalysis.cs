// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides information about how data flows into and out of a region. This information is
    /// returned from a call to
    /// <see cref="M:Microsoft.CodeAnalysis.SemanticModel.AnalyzeRegionDataFlow" />.
    /// </summary>
    public abstract class DataFlowAnalysis
    {
        /// <summary>
        /// An enumerator for the set of local variables that are declared within a region. Note
        /// that the region must be bounded by a method's body or a field's initializer, so
        /// parameter symbols are never included in the result.
        /// </summary>
        public abstract IEnumerable<ISymbol> VariablesDeclared { get; }

        /// <summary>
        /// An enumerator for the set of local variables which are assigned a value outside a region
        /// that may be used inside the region.
        /// </summary>
        public abstract IEnumerable<ISymbol> DataFlowsIn { get; }

        /// <summary>
        /// An enumerator for the set of local variables which are assigned a value inside a region
        /// that may be used outside the region.
        /// </summary>
        public abstract IEnumerable<ISymbol> DataFlowsOut { get; }

        /// <summary>
        /// An enumerator for the set of local variables for which a value is always assigned inside
        /// a region.
        /// </summary>
        public abstract IEnumerable<ISymbol> AlwaysAssigned { get; }

        /// <summary>
        /// An enumerator for the set of local variables that are read inside a region.
        /// </summary>
        public abstract IEnumerable<ISymbol> ReadInside { get; }

        /// <summary>
        /// An enumerator for the set of local variables that are written inside a region.
        /// </summary>
        public abstract IEnumerable<ISymbol> WrittenInside { get; }

        /// <summary>
        /// An enumerator for the set of the local variables that are read outside a region.
        /// </summary>
        public abstract IEnumerable<ISymbol> ReadOutside { get; }

        /// <summary>
        /// An enumerator for the set of local variables that are written outside a region.
        /// </summary>
        public abstract IEnumerable<ISymbol> WrittenOutside { get; }

        /// <summary>
        /// An enumerator for the set of the local variables that have been referenced in anonymous
        /// functions within a region and therefore must be moved to a field of a frame class.
        /// </summary>
        public abstract IEnumerable<ISymbol> Captured { get; }

        /// <summary>
        /// A collection of the non-constant local variables and parameters that have had their
        /// address (or the address of one of their fields) taken.
        /// </summary>
        public abstract IEnumerable<ISymbol> UnsafeAddressTaken { get; }

        /// <summary>
        /// Returns true iff analysis was successful.  Analysis can fail if the region does not
        /// properly span a single expression, a single statement, or a contiguous series of
        /// statements within the enclosing block.
        /// </summary>
        public abstract bool Succeeded { get; }
    }
}
