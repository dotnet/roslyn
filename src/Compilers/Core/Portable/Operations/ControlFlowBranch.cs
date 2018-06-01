// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// PROTOTYPE(dataflow): Add documentation
    /// </summary>
    public sealed class ControlFlowBranch
    {
        private ImmutableArray<ControlFlowRegion> _lazyLeavingRegions;
        private ImmutableArray<ControlFlowRegion> _lazyFinallyRegions;
        private ImmutableArray<ControlFlowRegion> _lazyEnteringRegions;

        internal ControlFlowBranch(
            BasicBlock source,
            BasicBlock destination,
            ControlFlowBranchSemantics semantics,
            bool isConditionalSuccessor)
        {
            Source = source;
            Destination = destination;
            Semantics = semantics;
            IsConditionalSuccessor = isConditionalSuccessor;
        }

        public BasicBlock Source { get; }

        public BasicBlock Destination { get; }

        public ControlFlowBranchSemantics Semantics { get; }

        public bool IsConditionalSuccessor { get; }

        /// <summary>
        /// What regions are exited (from inner most to outer most) if this branch is taken.
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
                builder.Add(source);
                source = source.EnclosingRegion;
            }

            return builder;
        }

        /// <summary>
        /// What regions are entered (from outer most to inner most) if this branch is taken.
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
        /// The finally regions the control goes through if the branch is taken
        /// </summary>
        public ImmutableArray<ControlFlowRegion> FinallyRegions
        {
            get
            {
                if (_lazyFinallyRegions.IsDefault)
                {
                    ArrayBuilder<ControlFlowRegion> builder = null;
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
