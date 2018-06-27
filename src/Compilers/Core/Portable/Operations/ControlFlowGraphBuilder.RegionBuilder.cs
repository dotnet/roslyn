// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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
            public BasicBlockBuilder FirstBlock = null;
            public BasicBlockBuilder LastBlock = null;
            public ArrayBuilder<RegionBuilder> Regions = null;
            public ImmutableArray<ILocalSymbol> Locals;
            public ArrayBuilder<(IMethodSymbol, ILocalFunctionOperation)> LocalFunctions = null;
#if DEBUG
            private bool _aboutToFree = false;
#endif 

            public RegionBuilder(ControlFlowRegionKind kind, ITypeSymbol exceptionType = null, ImmutableArray<ILocalSymbol> locals = default)
            {
                Kind = kind;
                ExceptionType = exceptionType;
                Locals = locals.NullToEmpty();
            }

            public bool IsEmpty => FirstBlock == null;
            public bool HasRegions => Regions?.Count > 0;
            public bool HasLocalFunctions => LocalFunctions?.Count > 0;

#if DEBUG
            public void AboutToFree() => _aboutToFree = true;
#endif 

            public void Add(IMethodSymbol symbol, ILocalFunctionOperation operation)
            {
                Debug.Assert(Kind != ControlFlowRegionKind.Root);
                Debug.Assert(symbol.MethodKind == MethodKind.LocalFunction);

                if (LocalFunctions == null)
                {
                    LocalFunctions = ArrayBuilder<(IMethodSymbol, ILocalFunctionOperation)>.GetInstance();
                }

                LocalFunctions.Add((symbol, operation));
            }

            public void AddRange(ArrayBuilder<(IMethodSymbol, ILocalFunctionOperation)> others)
            {
                Debug.Assert(Kind != ControlFlowRegionKind.Root);

                if (others == null)
                {
                    return;
                }

                Debug.Assert(others.All(((IMethodSymbol m, ILocalFunctionOperation _) tuple) => tuple.m.MethodKind == MethodKind.LocalFunction));

                if (LocalFunctions == null)
                {
                    LocalFunctions = ArrayBuilder<(IMethodSymbol, ILocalFunctionOperation)>.GetInstance();
                }

                LocalFunctions.AddRange(others);
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

            public void ExtendToInclude(BasicBlockBuilder block)
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
                LocalFunctions?.Free();
                LocalFunctions = null;
            }

            public ControlFlowRegion ToImmutableRegionAndFree(ArrayBuilder<BasicBlockBuilder> blocks,
                                                              ArrayBuilder<IMethodSymbol> localFunctions,
                                                              ImmutableDictionary<IMethodSymbol, (ControlFlowRegion region, ILocalFunctionOperation operation, int ordinal)>.Builder localFunctionsMap,
                                                              ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion region, int ordinal)>.Builder anonymousFunctionsMapOpt,
                                                              ControlFlowRegion enclosing)
            {
#if DEBUG
                Debug.Assert(!_aboutToFree);
#endif 
                Debug.Assert(!IsEmpty);

                int localFunctionsBefore = localFunctions.Count;

                if (HasLocalFunctions)
                {
                    foreach ((IMethodSymbol method, IOperation _) in LocalFunctions)
                    {
                        localFunctions.Add(method);
                    }
                }

                ImmutableArray<ControlFlowRegion> subRegions;

                if (HasRegions)
                {
                    var builder = ArrayBuilder<ControlFlowRegion>.GetInstance(Regions.Count);

                    foreach (RegionBuilder region in Regions)
                    {
                        builder.Add(region.ToImmutableRegionAndFree(blocks, localFunctions, localFunctionsMap, anonymousFunctionsMapOpt, enclosing: null));
                    }

                    subRegions = builder.ToImmutableAndFree();
                }
                else
                {
                    subRegions = ImmutableArray<ControlFlowRegion>.Empty;
                }

                var result = new ControlFlowRegion(Kind, FirstBlock.Ordinal, LastBlock.Ordinal, subRegions,
                                                   Locals, LocalFunctions?.SelectAsArray(((IMethodSymbol, ILocalFunctionOperation) tuple) => tuple.Item1) ?? default,
                                                   ExceptionType,
                                                   enclosing);

                if (HasLocalFunctions)
                {
                    foreach ((IMethodSymbol method, ILocalFunctionOperation operation) in LocalFunctions)
                    {
                        localFunctionsMap.Add(method, (result, operation, localFunctionsBefore++));
                    }
                }

                int firstBlockWithoutRegion = FirstBlock.Ordinal;

                foreach (ControlFlowRegion region in subRegions)
                {
                    for (int i = firstBlockWithoutRegion; i < region.FirstBlockOrdinal; i++)
                    {
                        setRegion(blocks[i]);
                    }

                    firstBlockWithoutRegion = region.LastBlockOrdinal + 1;
                }

                for (int i = firstBlockWithoutRegion; i <= LastBlock.Ordinal; i++)
                {
                    setRegion(blocks[i]);
                }

#if DEBUG
                AboutToFree();
#endif 
                Free();
                return result;

                void setRegion(BasicBlockBuilder block)
                {
                    Debug.Assert(block.Region == null);
                    block.Region = result;

                    // Populate the map of IFlowAnonymousFunctionOperation nodes, if we have any
                    if (anonymousFunctionsMapOpt != null)
                    {
                        (ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion region, int ordinal)>.Builder map, ControlFlowRegion region) argument = (anonymousFunctionsMapOpt, result);

                        if (block.HasStatements)
                        {
                            foreach (IOperation o in block.StatementsOpt)
                            {
                                AnonymousFunctionsMapBuilder.Instance.Visit(o, argument);
                            }
                        }

                        AnonymousFunctionsMapBuilder.Instance.Visit(block.BranchValue, argument);
                    }
                }
            }

            private sealed class AnonymousFunctionsMapBuilder : 
                OperationVisitor<(ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion region, int ordinal)>.Builder map, ControlFlowRegion region), IOperation>
            {
                public static readonly AnonymousFunctionsMapBuilder Instance = new AnonymousFunctionsMapBuilder();

                public override IOperation VisitFlowAnonymousFunction(
                    IFlowAnonymousFunctionOperation operation, 
                    (ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion region, int ordinal)>.Builder map, ControlFlowRegion region) argument)
                {
                    argument.map.Add(operation, (argument.region, argument.map.Count));
                    return base.VisitFlowAnonymousFunction(operation, argument);
                }

                internal override IOperation VisitNoneOperation(IOperation operation, (ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion region, int ordinal)>.Builder map, ControlFlowRegion region) argument)
                {
                    return DefaultVisit(operation, argument);
                }

                public override IOperation DefaultVisit(
                    IOperation operation, 
                    (ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion region, int ordinal)>.Builder map, ControlFlowRegion region) argument)
                {
                    foreach (IOperation child in operation.Children)
                    {
                        Visit(child, argument);
                    }

                    return null;
                }
            }
        }
    }
}
