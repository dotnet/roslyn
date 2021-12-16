// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents a control flow branch from a <see cref="Source"/> basic block to a <see cref="Destination"/>
    /// basic block in a <see cref="ControlFlowGraph"/>.
    /// </summary>
    public sealed class ControlFlowBranch
    {
        private ImmutableArray<ControlFlowRegion> _lazyLeavingRegions;
        private ImmutableArray<ControlFlowRegion> _lazyFinallyRegions;
        private ImmutableArray<ControlFlowRegion> _lazyEnteringRegions;

        internal ControlFlowBranch(
            BasicBlock source,
            BasicBlock? destination,
            ControlFlowBranchSemantics semantics,
            bool isConditionalSuccessor)
        {
            Source = source;
            Destination = destination;
            Semantics = semantics;
            IsConditionalSuccessor = isConditionalSuccessor;
        }

        /// <summary>
        /// Source basic block of this branch.
        /// </summary>
        public BasicBlock Source { get; }

        /// <summary>
        /// Destination basic block of this branch.
        /// </summary>
        public BasicBlock? Destination { get; }

        /// <summary>
        /// Semantics associated with this branch (such as "regular", "return", "throw", etc).
        /// </summary>
        public ControlFlowBranchSemantics Semantics { get; }

        /// <summary>
        /// Indicates if this branch represents <see cref="BasicBlock.ConditionalSuccessor"/> of the <see cref="Source"/> basic block.
        /// </summary>
        public bool IsConditionalSuccessor { get; }

        /// <summary>
        /// Regions exited if this branch is taken.
        /// Ordered from the innermost region to the outermost region.
        /// </summary>
        public ImmutableArray<ControlFlowRegion> LeavingRegions
        {
            get
            {
                if (_lazyLeavingRegions.IsDefault)
                {
                    ImmutableArray<ControlFlowRegion> result;

                    if (Destination == null)
                    {
                        result = ImmutableArray<ControlFlowRegion>.Empty;
                    }
                    else
                    {
                        result = CollectRegions(Destination.Ordinal, Source.EnclosingRegion).ToImmutableAndFree();
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyLeavingRegions, result);
                }

                return _lazyLeavingRegions;
            }
        }

        private static ArrayBuilder<ControlFlowRegion> CollectRegions(int destinationOrdinal, ControlFlowRegion source)
        {
            var builder = ArrayBuilder<ControlFlowRegion>.GetInstance();

            while (!source.ContainsBlock(destinationOrdinal))
            {
                Debug.Assert(source.Kind != ControlFlowRegionKind.Root);
                Debug.Assert(source.EnclosingRegion != null);
                builder.Add(source);
                source = source.EnclosingRegion;
            }

            return builder;
        }

        /// <summary>
        /// Regions entered if this branch is taken.
        /// Ordered from the outermost region to the innermost region.
        /// </summary>
        public ImmutableArray<ControlFlowRegion> EnteringRegions
        {
            get
            {
                if (_lazyEnteringRegions.IsDefault)
                {
                    ImmutableArray<ControlFlowRegion> result;

                    if (Destination == null)
                    {
                        result = ImmutableArray<ControlFlowRegion>.Empty;
                    }
                    else
                    {
                        ArrayBuilder<ControlFlowRegion> builder = CollectRegions(Source.Ordinal, Destination.EnclosingRegion);
                        builder.ReverseContents();
                        result = builder.ToImmutableAndFree();
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyEnteringRegions, result);
                }

                return _lazyEnteringRegions;
            }
        }

        /// <summary>
        /// The finally regions the control goes through if this branch is taken.
        /// Ordered in the sequence by which the finally regions are executed.
        /// </summary>
        public ImmutableArray<ControlFlowRegion> FinallyRegions
        {
            get
            {
                if (_lazyFinallyRegions.IsDefault)
                {
                    ArrayBuilder<ControlFlowRegion>? builder = null;
                    ImmutableArray<ControlFlowRegion> leavingRegions = LeavingRegions;
                    int stopAt = leavingRegions.Length - 1;
                    for (int i = 0; i < stopAt; i++)
                    {
                        if (leavingRegions[i].Kind == ControlFlowRegionKind.Try && leavingRegions[i + 1].Kind == ControlFlowRegionKind.TryAndFinally)
                        {
                            if (builder == null)
                            {
                                builder = ArrayBuilder<ControlFlowRegion>.GetInstance();
                            }

                            builder.Add(leavingRegions[i + 1].NestedRegions.Last());
                            Debug.Assert(builder.Last().Kind == ControlFlowRegionKind.Finally);
                        }
                    }

                    var result = builder == null ? ImmutableArray<ControlFlowRegion>.Empty : builder.ToImmutableAndFree();

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyFinallyRegions, result);
                }

                return _lazyFinallyRegions;
            }
        }
    }
}
