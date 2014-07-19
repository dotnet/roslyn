// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This class implements the region data flow analysis operations.  Region data flow analysis
    /// provides information how data flows into and out of a region.  The analysis is done lazily.
    /// When created, it performs no analysis, but simply caches the arguments. Then, the first time
    /// one of the analysis results is used it computes that one result and caches it. Each result
    /// is computed using a custom algorithm.
    /// </summary>
    internal class CSharpDataFlowAnalysis : DataFlowAnalysis
    {
        private readonly RegionAnalysisContext context;

        private ImmutableArray<ISymbol> variablesDeclared;
        private HashSet<Symbol> unassignedVariables;
        private ImmutableArray<ISymbol> dataFlowsIn;
        private ImmutableArray<ISymbol> dataFlowsOut;
        private ImmutableArray<ISymbol> alwaysAssigned;
        private ImmutableArray<ISymbol> readInside;
        private ImmutableArray<ISymbol> writtenInside;
        private ImmutableArray<ISymbol> readOutside;
        private ImmutableArray<ISymbol> writtenOutside;
        private ImmutableArray<ISymbol> captured;
        private ImmutableArray<ISymbol> unsafeAddressTaken;
        private HashSet<PrefixUnaryExpressionSyntax> unassignedVariableAddressOfSyntaxes;
        private bool? succeeded = null;

        internal CSharpDataFlowAnalysis(RegionAnalysisContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// A collection of the local variables that are declared within the region. Note that the region must be
        /// bounded by a method's body or a field's initializer, so method parameter symbols are never included
        /// in the result, but lambda parameters might appear in the result.
        /// </summary>
        public override ImmutableArray<ISymbol> VariablesDeclared
        {
            // Variables declared in the region is computed by a simple scan.
            // ISSUE: are these only variables declared at the top level in the region,
            // or are we to include variables declared in deeper scopes within the region?
            get
            {
                if (variablesDeclared.IsDefault)
                {
                    var result = Succeeded
                        ? ((IEnumerable<ISymbol>)VariablesDeclaredWalker.Analyze(context.Compilation, context.Member, context.BoundNode, context.FirstInRegion, context.LastInRegion)).ToImmutableArray()
                        : ImmutableArray<ISymbol>.Empty;
                    ImmutableInterlocked.InterlockedInitialize(ref variablesDeclared, result);
                }

                return variablesDeclared;
            }
        }

        private HashSet<Symbol> UnassignedVariables
        {
            get
            {
                if (unassignedVariables == null)
                {
                    var result = Succeeded
                        ? UnassignedVariablesWalker.Analyze(context.Compilation, context.Member, context.BoundNode)
                        : new HashSet<Symbol>();
                    Interlocked.CompareExchange(ref unassignedVariables, result, null);
                }

                return unassignedVariables;
            }
        }

        /// <summary>
        /// A collection of the local variables for which a value assigned outside the region may be used inside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> DataFlowsIn
        {
            get
            {
                if (dataFlowsIn == null)
                {
                    succeeded = !context.Failed;
                    var result = context.Failed ? ImmutableArray<ISymbol>.Empty :
                        ((IEnumerable<ISymbol>)DataFlowsInWalker.Analyze(context.Compilation, context.Member, context.BoundNode, context.FirstInRegion, context.LastInRegion, UnassignedVariables, UnassignedVariableAddressOfSyntaxes, out succeeded)).ToImmutableArray();
                    ImmutableInterlocked.InterlockedInitialize(ref dataFlowsIn, result);
                }

                return dataFlowsIn;
            }
        }

        /// <summary>
        /// A collection of the local variables for which a value assigned inside the region may be used outside the region.
        /// Note that every reachable assignment to a ref or out variable will be included in the results.
        /// </summary>
        public override ImmutableArray<ISymbol> DataFlowsOut
        {
            get
            {
                var discarded = DataFlowsIn; // force DataFlowsIn to be computed
                if (dataFlowsOut == null)
                {
                    var result = Succeeded
                        ? ((IEnumerable<ISymbol>)DataFlowsOutWalker.Analyze(context.Compilation, context.Member, context.BoundNode, context.FirstInRegion, context.LastInRegion, UnassignedVariables, dataFlowsIn)).ToImmutableArray()
                        : ImmutableArray<ISymbol>.Empty;
                    ImmutableInterlocked.InterlockedInitialize(ref dataFlowsOut, result);
                }

                return dataFlowsOut;
            }
        }

        /// <summary>
        /// A collection of the local variables for which a value is always assigned inside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> AlwaysAssigned
        {
            get
            {
                if (alwaysAssigned == null)
                {
                    var result = Succeeded
                        ? ((IEnumerable<ISymbol>)AlwaysAssignedWalker.Analyze(context.Compilation, context.Member, context.BoundNode, context.FirstInRegion, context.LastInRegion)).ToImmutableArray()
                        : ImmutableArray<ISymbol>.Empty;
                    ImmutableInterlocked.InterlockedInitialize(ref alwaysAssigned, result);
                }

                return alwaysAssigned;
            }
        }

        /// <summary>
        /// A collection of the local variables that are read inside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> ReadInside
        {
            get
            {
                if (readInside == null)
                {
                    AnalyzeReadWrite();
                }

                return readInside;
            }
        }

        /// <summary>
        /// A collection of local variables that are written inside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> WrittenInside
        {
            get
            {
                if (writtenInside == null)
                {
                    AnalyzeReadWrite();
                }

                return writtenInside;
            }
        }

        /// <summary>
        /// A collection of the local variables that are read outside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> ReadOutside
        {
            get
            {
                if (readOutside == null)
                {
                    AnalyzeReadWrite();
                }

                return readOutside;
            }
        }

        /// <summary>
        /// A collection of local variables that are written outside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> WrittenOutside
        {
            get
            {
                if (writtenOutside == null)
                {
                    AnalyzeReadWrite();
                }

                return writtenOutside;
            }
        }

        private void AnalyzeReadWrite()
        {
            ImmutableArray<ISymbol> readInside, writtenInside, readOutside, writtenOutside, captured, unsafeAddressTaken;
            if (Succeeded)
            {
                ReadWriteWalker.Analyze(context.Compilation, context.Member, context.BoundNode, context.FirstInRegion, context.LastInRegion, UnassignedVariableAddressOfSyntaxes,
                    readInside: out readInside, writtenInside: out writtenInside,
                    readOutside: out readOutside, writtenOutside: out writtenOutside,
                    captured: out captured, unsafeAddressTaken: out unsafeAddressTaken);
            }
            else
            {
                readInside = writtenInside = readOutside = writtenOutside = captured = unsafeAddressTaken = ImmutableArray<ISymbol>.Empty;
            }

            ImmutableInterlocked.InterlockedInitialize(ref this.readInside, readInside);
            ImmutableInterlocked.InterlockedInitialize(ref this.writtenInside, writtenInside);
            ImmutableInterlocked.InterlockedInitialize(ref this.readOutside, readOutside);
            ImmutableInterlocked.InterlockedInitialize(ref this.writtenOutside, writtenOutside);
            ImmutableInterlocked.InterlockedInitialize(ref this.captured, captured);
            ImmutableInterlocked.InterlockedInitialize(ref this.unsafeAddressTaken, unsafeAddressTaken);
        }

        /// <summary>
        /// A collection of the non-constant local variables and parameters that have been referenced in anonymous functions
        /// and therefore must be moved to a field of a frame class.
        /// </summary>
        public override ImmutableArray<ISymbol> Captured
        {
            get
            {
                if (this.captured == null)
                {
                    AnalyzeReadWrite();
                }

                return this.captured;
            }
        }

        /// <summary>
        /// A collection of the non-constant local variables and parameters that have had their address (or the address of one
        /// of their fields) taken using the '&amp;' operator.
        /// </summary>
        /// <remarks>
        /// If there are any of these in the region, then a method should not be extracted.
        /// </remarks>
        public override ImmutableArray<ISymbol> UnsafeAddressTaken
        {
            get
            {
                if (this.unsafeAddressTaken == null)
                {
                    AnalyzeReadWrite();
                }

                return this.unsafeAddressTaken;
            }
        }

        private HashSet<PrefixUnaryExpressionSyntax> UnassignedVariableAddressOfSyntaxes
        {
            get
            {
                if (unassignedVariableAddressOfSyntaxes == null)
                {
                    var result = Succeeded
                        ? UnassignedAddressTakenVariablesWalker.Analyze(context.Compilation, context.Member, context.BoundNode)
                        : new HashSet<PrefixUnaryExpressionSyntax>();
                    Interlocked.CompareExchange(ref unassignedVariableAddressOfSyntaxes, result, null);
                }

                return unassignedVariableAddressOfSyntaxes;
            }
        }

        /// <summary>
        /// Returns true iff analysis was successful.  Analysis can fail if the region does not properly span a single expression,
        /// a single statement, or a contiguous series of statements within the enclosing block.
        /// </summary>
        public sealed override bool Succeeded
        {
            get
            {
                if (succeeded == null)
                {
                    var discarded = DataFlowsIn;
                }

                return succeeded.Value;
            }
        }
    }
}