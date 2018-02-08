// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class ControlFlowGraphBuilder
    {
        private class RegionBuilder
        {
            public readonly ControlFlowGraph.RegionKind Kind;
            public RegionBuilder Enclosing;
            public readonly ITypeSymbol ExceptionType;
            public ArrayBuilder<BasicBlock> Blocks = null;
            public ArrayBuilder<RegionBuilder> Regions = null;
            public ImmutableArray<ILocalSymbol> Locals;

            public RegionBuilder(ControlFlowGraph.RegionKind kind, RegionBuilder enclosing, ITypeSymbol exceptionType = null, ImmutableArray<ILocalSymbol> locals = default)
            {
                Debug.Assert((enclosing == null) == (kind == ControlFlowGraph.RegionKind.Root));
                Kind = kind;
                Enclosing = enclosing;
                ExceptionType = exceptionType;
                Locals = locals.NullToEmpty();

                enclosing?.Add(this);
            }

            public bool IsEmpty => !HasBlocks && !HasRegions;
            public bool HasBlocks => Blocks?.Count > 0;
            public bool HasRegions => Regions?.Count > 0;

            public void Add(RegionBuilder region)
            {
                if (Regions == null)
                {
                    Regions = ArrayBuilder<RegionBuilder>.GetInstance();
                }

                Regions.Add(region);

#if DEBUG
                ControlFlowGraph.RegionKind lastKind = Regions.Last().Kind;
                switch (Kind)
                {
                    case ControlFlowGraph.RegionKind.FilterAndHandler:
                        Debug.Assert(Regions.Count <= 2);
                        Debug.Assert(lastKind == (Regions.Count < 2 ? ControlFlowGraph.RegionKind.Filter : ControlFlowGraph.RegionKind.Handler));
                        break;

                    case ControlFlowGraph.RegionKind.TryAndCatch:
                        if (Regions.Count == 1)
                        {
                            Debug.Assert(lastKind == ControlFlowGraph.RegionKind.Try);
                        }
                        else
                        {
                            Debug.Assert(lastKind == ControlFlowGraph.RegionKind.Handler || lastKind == ControlFlowGraph.RegionKind.FilterAndHandler);
                        }
                        break;

                    case ControlFlowGraph.RegionKind.TryAndFinally:
                        Debug.Assert(Regions.Count <= 2);
                        if (Regions.Count == 1)
                        {
                            Debug.Assert(lastKind == ControlFlowGraph.RegionKind.Try);
                        }
                        else
                        {
                            Debug.Assert(lastKind == ControlFlowGraph.RegionKind.Handler);
                        }
                        break;

                    default:
                        Debug.Assert(lastKind != ControlFlowGraph.RegionKind.Filter);
                        Debug.Assert(lastKind != ControlFlowGraph.RegionKind.Handler);
                        Debug.Assert(lastKind != ControlFlowGraph.RegionKind.Try);
                        break;
                }
#endif
            }

            public void Add(BasicBlock block)
            {
                Debug.Assert(Kind != ControlFlowGraph.RegionKind.FilterAndHandler);
                Debug.Assert(Kind != ControlFlowGraph.RegionKind.TryAndCatch);
                Debug.Assert(Kind != ControlFlowGraph.RegionKind.TryAndFinally);

                if (Blocks == null)
                {
                    Blocks = ArrayBuilder<BasicBlock>.GetInstance();
                }

                Blocks.Add(block);
            }

            public void AddRange(ArrayBuilder<BasicBlock> blocks)
            {
                Debug.Assert(Kind != ControlFlowGraph.RegionKind.FilterAndHandler);
                Debug.Assert(Kind != ControlFlowGraph.RegionKind.TryAndCatch);
                Debug.Assert(Kind != ControlFlowGraph.RegionKind.TryAndFinally);

                if (Blocks == null)
                {
                    Blocks = ArrayBuilder<BasicBlock>.GetInstance(blocks.Count);
                }

                Blocks.AddRange(blocks);
            }

            public void Free()
            {
                Enclosing = null;
                Blocks?.Free();
                Regions?.Free();
                Blocks = null;
                Regions = null;
            }

            public ControlFlowGraph.Region ToImmutableRegionAndFree()
            {
                Debug.Assert(!IsEmpty);
                ControlFlowGraph.Region result;

                int first = int.MaxValue;
                int last = int.MinValue;
                ImmutableArray<ControlFlowGraph.Region> subRegions = default;

                if (HasRegions)
                {
                    var builder = ArrayBuilder<ControlFlowGraph.Region>.GetInstance(Regions.Count);

                    foreach (RegionBuilder region in Regions)
                    {
                        builder.Add(region.ToImmutableRegionAndFree());
                    }

                    first = builder.First().FirstBlockOrdinal;
                    last = builder.Last().LastBlockOrdinal;

                    subRegions = builder.ToImmutableAndFree();
                }

                if (HasBlocks)
                {
                    first = Math.Min(first, Blocks.First().Ordinal);
                    last = Math.Max(last, Blocks.Last().Ordinal);
                }

                result = new ControlFlowGraph.Region(Kind, first, last, subRegions, Locals, ExceptionType);

                if (HasBlocks)
                {
                    foreach (BasicBlock block in Blocks)
                    {
                        block.Region = result;
                    }
                }

                Free();
                return result;
            }
        }
    }
}
