// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal sealed partial class ControlFlowGraphBuilder
    {
        private class RegionBuilder
        {
            public ControlFlowRegionKind Kind;
            public RegionBuilder Enclosing { get; private set; } = null;
            public readonly ITypeSymbol ExceptionType;
            public BasicBlock FirstBlock = null;
            public BasicBlock LastBlock = null;
            public ArrayBuilder<RegionBuilder> Regions = null;
            public ImmutableArray<ILocalSymbol> Locals;
            public ArrayBuilder<(IMethodSymbol, IOperation)> Methods = null;
            public readonly bool AllowMethods;
#if DEBUG
            private bool _aboutToFree = false;
#endif 

            public RegionBuilder(ControlFlowRegionKind kind, ITypeSymbol exceptionType = null, ImmutableArray<ILocalSymbol> locals = default, bool allowMethods = true)
            {
                Kind = kind;
                ExceptionType = exceptionType;
                Locals = locals.NullToEmpty();
                AllowMethods = allowMethods;
            }

            public bool IsEmpty => FirstBlock == null;
            public bool HasRegions => Regions?.Count > 0;
            public bool HasMethods => Methods?.Count > 0;

#if DEBUG
            public void AboutToFree() => _aboutToFree = true;
#endif 

            public void Add(IMethodSymbol symbol, IOperation operation)
            {
                Debug.Assert(Kind != ControlFlowRegionKind.Root);

                if (Methods == null)
                {
                    if (!AllowMethods)
                    {
                        throw ExceptionUtilities.Unreachable;
                    }

                    Methods = ArrayBuilder<(IMethodSymbol, IOperation)>.GetInstance();
                }

                // Some expressions (like "As New" initializers, for example) can be visited multiple times.
                // That can lead to multiple attempts to add the same lambda into the list.
                // Let's detect that.
                if (Methods.Count > 0 && symbol.MethodKind == MethodKind.AnonymousFunction)
                {
                    foreach ((IMethodSymbol m, IOperation o) in Methods)
                    {
                        if (m.Equals(symbol))
                        {
                            Debug.Assert(o == operation);
                            return;
                        }
                    }
                }

                Methods.Add((symbol, operation));
            }

            public void AddRange(ArrayBuilder<(IMethodSymbol, IOperation)> others)
            {
                Debug.Assert(Kind != ControlFlowRegionKind.Root);

                if (others == null)
                {
                    return;
                }

                if (Methods == null)
                {
                    if (!AllowMethods)
                    {
                        throw ExceptionUtilities.Unreachable;
                    }

                    Methods = ArrayBuilder<(IMethodSymbol, IOperation)>.GetInstance();
                }

                Methods.AddRange(others);
            }

            public void Add(RegionBuilder region)
            {
                if (Regions == null)
                {
                    Regions = ArrayBuilder<RegionBuilder>.GetInstance();
                }

#if DEBUG
                Debug.Assert(region.Enclosing == null || (region.Enclosing._aboutToFree && region.Enclosing.Enclosing == this));
#endif 
                region.Enclosing = this;
                Regions.Add(region);

#if DEBUG
                ControlFlowRegionKind lastKind = Regions.Last().Kind;
                switch (Kind)
                {
                    case ControlFlowRegionKind.FilterAndHandler:
                        Debug.Assert(Regions.Count <= 2);
                        Debug.Assert(lastKind == (Regions.Count < 2 ? ControlFlowRegionKind.Filter : ControlFlowRegionKind.Catch));
                        break;

                    case ControlFlowRegionKind.TryAndCatch:
                        if (Regions.Count == 1)
                        {
                            Debug.Assert(lastKind == ControlFlowRegionKind.Try);
                        }
                        else
                        {
                            Debug.Assert(lastKind == ControlFlowRegionKind.Catch || lastKind == ControlFlowRegionKind.FilterAndHandler);
                        }
                        break;

                    case ControlFlowRegionKind.TryAndFinally:
                        Debug.Assert(Regions.Count <= 2);
                        if (Regions.Count == 1)
                        {
                            Debug.Assert(lastKind == ControlFlowRegionKind.Try);
                        }
                        else
                        {
                            Debug.Assert(lastKind == ControlFlowRegionKind.Finally);
                        }
                        break;

                    default:
                        Debug.Assert(lastKind != ControlFlowRegionKind.Filter);
                        Debug.Assert(lastKind != ControlFlowRegionKind.Catch);
                        Debug.Assert(lastKind != ControlFlowRegionKind.Finally);
                        Debug.Assert(lastKind != ControlFlowRegionKind.Try);
                        break;
                }
#endif
            }

            public void Remove(RegionBuilder region)
            {
                Debug.Assert(region.Enclosing == this);

                if (Regions.Count == 1)
                {
                    Debug.Assert(Regions[0] == region);
                    Regions.Clear();
                }
                else
                {
                    Regions.RemoveAt(Regions.IndexOf(region));
                }

                region.Enclosing = null;
            }

            public void ReplaceRegion(RegionBuilder toReplace, ArrayBuilder<RegionBuilder> replaceWith)
            {
                Debug.Assert(toReplace.Enclosing == this);
                Debug.Assert(toReplace.FirstBlock.Ordinal <= replaceWith.First().FirstBlock.Ordinal);
                Debug.Assert(toReplace.LastBlock.Ordinal >= replaceWith.Last().LastBlock.Ordinal);

                int insertAt;

                if (Regions.Count == 1)
                {
                    Debug.Assert(Regions[0] == toReplace);
                    insertAt = 0;
                }
                else
                {
                    insertAt = Regions.IndexOf(toReplace);
                }

                int replaceWithCount = replaceWith.Count;
                if (replaceWithCount == 1)
                {
                    RegionBuilder single = replaceWith[0];
                    single.Enclosing = this;
                    Regions[insertAt] = single;
                }
                else
                {
                    int originalCount = Regions.Count;
                    Regions.Count = replaceWithCount - 1 + originalCount;

                    for (int i = originalCount - 1, j = Regions.Count - 1; i > insertAt; i--, j--)
                    {
                        Regions[j] = Regions[i];
                    }

                    foreach (RegionBuilder region in replaceWith)
                    {
                        region.Enclosing = this;
                        Regions[insertAt++] = region;
                    }
                }

                toReplace.Enclosing = null;
            }

            public void ExtendToInclude(BasicBlock block)
            {
                Debug.Assert((Kind != ControlFlowRegionKind.FilterAndHandler &&
                              Kind != ControlFlowRegionKind.TryAndCatch &&
                              Kind != ControlFlowRegionKind.TryAndFinally) ||
                              Regions.Last().LastBlock == block);

                if (FirstBlock == null)
                {
                    Debug.Assert(LastBlock == null);

                    if (!HasRegions)
                    {
                        FirstBlock = block;
                        LastBlock = block;
                        return;
                    }

                    FirstBlock = Regions.First().FirstBlock;
                    Debug.Assert(Regions.Count == 1 && Regions.First().LastBlock == block);
                }
                else
                {
                    Debug.Assert(LastBlock.Ordinal < block.Ordinal);
                    Debug.Assert(!HasRegions || Regions.Last().LastBlock.Ordinal <= block.Ordinal);
                }

                LastBlock = block;
            }

            public void Free()
            {
#if DEBUG
                Debug.Assert(_aboutToFree);
#endif 
                Enclosing = null;
                FirstBlock = null;
                LastBlock = null;
                Regions?.Free();
                Regions = null;
                Methods?.Free();
                Methods = null;
            }

            public ControlFlowRegion ToImmutableRegionAndFree(ArrayBuilder<BasicBlock> blocks,
                                                                    ArrayBuilder<IMethodSymbol> methods,
                                                                    ImmutableDictionary<IMethodSymbol, (ControlFlowRegion region, IOperation operation, int ordinal)>.Builder methodsMap,
                                                                    ControlFlowRegion enclosing)
            {
#if DEBUG
                Debug.Assert(!_aboutToFree);
#endif 
                Debug.Assert(!IsEmpty);

                int methodsBefore = methods.Count;

                if (HasMethods)
                {
                    foreach ((IMethodSymbol method, IOperation _) in Methods)
                    {
                        methods.Add(method);
                    }
                }

                ImmutableArray<ControlFlowRegion> subRegions;

                if (HasRegions)
                {
                    var builder = ArrayBuilder<ControlFlowRegion>.GetInstance(Regions.Count);

                    foreach (RegionBuilder region in Regions)
                    {
                        builder.Add(region.ToImmutableRegionAndFree(blocks, methods, methodsMap, enclosing: null));
                    }

                    subRegions = builder.ToImmutableAndFree();
                }
                else
                {
                    subRegions = ImmutableArray<ControlFlowRegion>.Empty;
                }

                var result = new ControlFlowRegion(Kind, FirstBlock.Ordinal, LastBlock.Ordinal, subRegions,
                                                         Locals, Methods?.SelectAsArray(((IMethodSymbol, IOperation) tuple) => tuple.Item1) ?? default,
                                                         ExceptionType,
                                                         enclosing);

                if (HasMethods)
                {
                    foreach ((IMethodSymbol method, IOperation operation) in Methods)
                    {
                        methodsMap.Add(method, (result, operation, methodsBefore++));
                    }
                }

                int firstBlockWithoutRegion = FirstBlock.Ordinal;

                foreach (ControlFlowRegion region in subRegions)
                {
                    for (int i = firstBlockWithoutRegion; i < region.FirstBlockOrdinal; i++)
                    {
                        Debug.Assert(blocks[i].Region == null);
                        blocks[i].Region = result;
                    }

                    firstBlockWithoutRegion = region.LastBlockOrdinal + 1;
                }

                for (int i = firstBlockWithoutRegion; i <= LastBlock.Ordinal; i++)
                {
                    Debug.Assert(blocks[i].Region == null);
                    blocks[i].Region = result;
                }

#if DEBUG
                AboutToFree();
#endif 
                Free();
                return result;
            }
        }
    }
}
