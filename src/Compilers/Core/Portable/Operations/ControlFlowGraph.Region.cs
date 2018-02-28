// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    public partial class ControlFlowGraph
    {
        /// <summary>
        /// Defines kinds of regions that can be present in a <see cref="ControlFlowGraph"/>
        /// </summary>
        public enum RegionKind
        {
            /// <summary>
            /// A root region encapsulating all <see cref="BasicBlock"/>s in a <see cref="ControlFlowGraph"/>
            /// </summary>
            Root,

            /// <summary>
            /// Region with the only purpose to represent the life-time of locals.
            /// PROTOTYPE(dataflow): We should clearly explain what "life-time" refers to here, or use a different term.
            /// </summary>
            Locals,

            /// <summary>
            /// Region representing a try region. For example, <see cref="ITryOperation.Body"/>
            /// </summary>
            Try,

            /// <summary>
            /// Region representing <see cref="ICatchClauseOperation.Filter"/>
            /// </summary>
            Filter,

            /// <summary>
            /// Region representing <see cref="ICatchClauseOperation.Handler"/>
            /// </summary>
            Catch,

            /// <summary>
            /// Region representing a union of a <see cref="Filter"/> and the corresponding catch <see cref="Catch"/> regions. 
            /// Doesn't contain any <see cref="BasicBlock"/>s directly.
            /// </summary>
            FilterAndHandler,

            /// <summary>
            /// Region representing a union of a <see cref="Try"/> and all corresponding catch <see cref="Catch"/>
            /// and <see cref="FilterAndHandler"/> regions. Doesn't contain any <see cref="BasicBlock"/>s directly.
            /// </summary>
            TryAndCatch,

            /// <summary>
            /// Region representing <see cref="ITryOperation.Finally"/>
            /// </summary>
            Finally,

            /// <summary>
            /// Region representing a union of a <see cref="Try"/> and corresponding finally <see cref="Finally"/>
            /// region. Doesn't contain any <see cref="BasicBlock"/>s directly.
            /// 
            /// An <see cref="ITryOperation"/> that has a set of <see cref="ITryOperation.Catches"/> and a <see cref="ITryOperation.Finally"/> 
            /// at the same time is mapped to a <see cref="TryAndFinally"/> region with <see cref="TryAndCatch"/> region inside its <see cref="Try"/> region.
            /// </summary>
            TryAndFinally,
        }

        /// <summary>
        /// Encapsulates information about regions of <see cref="BasicBlock"/>s in a <see cref="ControlFlowGraph"/>.
        /// Regions can overlap, but never cross each other boundaries.
        /// </summary>
        public sealed class Region
        {
            /// <summary>
            /// Region's kind
            /// </summary>
            public RegionKind Kind { get; }

            /// <summary>
            /// Enclosing region. Null for <see cref="RegionKind.Root"/>
            /// </summary>
            public Region Enclosing { get; private set; }

            /// <summary>
            /// Target exception type for <see cref="RegionKind.Filter"/>, <see cref="RegionKind.Catch"/>, 
            /// <see cref="RegionKind.FilterAndHandler "/>
            /// </summary>
            public ITypeSymbol ExceptionType { get; }

            /// <summary>
            /// Ordinal (<see cref="BasicBlock.Ordinal"/>) of the first <see cref="BasicBlock"/> within the region. 
            /// </summary>
            public int FirstBlockOrdinal { get; }

            /// <summary>
            /// Ordinal (<see cref="BasicBlock.Ordinal"/>) of the last <see cref="BasicBlock"/> within the region. 
            /// </summary>
            public int LastBlockOrdinal { get; }

            /// <summary>
            /// Regions within this region
            /// </summary>
            public ImmutableArray<Region> Regions { get; }

            /// <summary>
            /// Locals for which this region represent the life-time.
            /// </summary>
            public ImmutableArray<ILocalSymbol> Locals { get; }

            internal Region(RegionKind kind, int firstBlockOrdinal, int lastBlockOrdinal, 
                            ImmutableArray<Region> regions = default, ImmutableArray<ILocalSymbol> locals = default, 
                            ITypeSymbol exceptionType = null)
            {
                Debug.Assert(firstBlockOrdinal >= 0);
                Debug.Assert(lastBlockOrdinal >= firstBlockOrdinal);

                Kind = kind;
                FirstBlockOrdinal = firstBlockOrdinal;
                LastBlockOrdinal = lastBlockOrdinal;
                ExceptionType = exceptionType;
                Locals = locals.NullToEmpty();
                Regions = regions.NullToEmpty();

                foreach(Region r in Regions)
                {
                    Debug.Assert(r.Enclosing == null && r.Kind != RegionKind.Root);
                    r.Enclosing = this;
                }
#if DEBUG
                int previousLast;

                switch (kind)
                {
                    case RegionKind.TryAndFinally:
                    case RegionKind.FilterAndHandler:
                        Debug.Assert(Regions.Length == 2);
                        Debug.Assert(Regions[0].Kind == (kind == RegionKind.TryAndFinally ? RegionKind.Try : RegionKind.Filter));
                        Debug.Assert(Regions[1].Kind == (kind == RegionKind.TryAndFinally ? RegionKind.Finally : RegionKind.Catch));
                        Debug.Assert(Regions[0].FirstBlockOrdinal == firstBlockOrdinal);
                        Debug.Assert(Regions[1].LastBlockOrdinal == lastBlockOrdinal);
                        Debug.Assert(Regions[0].LastBlockOrdinal + 1 == Regions[1].FirstBlockOrdinal);
                        break;

                    case RegionKind.TryAndCatch:
                        Debug.Assert(Regions.Length >= 2);
                        Debug.Assert(Regions[0].Kind == RegionKind.Try);
                        Debug.Assert(Regions[0].FirstBlockOrdinal == firstBlockOrdinal);
                        previousLast = Regions[0].LastBlockOrdinal;

                        for (int i = 1; i < Regions.Length; i++)
                        {
                            Region r = Regions[i];
                            Debug.Assert(previousLast + 1 == r.FirstBlockOrdinal);
                            previousLast = r.LastBlockOrdinal;

                            Debug.Assert(r.Kind == RegionKind.FilterAndHandler || r.Kind == RegionKind.Catch);
                        }

                        Debug.Assert(previousLast == lastBlockOrdinal);
                        break;

                    case RegionKind.Root:
                    case RegionKind.Locals:
                    case RegionKind.Try:
                    case RegionKind.Filter:
                    case RegionKind.Catch:
                    case RegionKind.Finally:
                        previousLast = firstBlockOrdinal - 1;

                        foreach (Region r in Regions)
                        {
                            Debug.Assert(previousLast < r.FirstBlockOrdinal);
                            previousLast = r.LastBlockOrdinal;
                        }

                        Debug.Assert(previousLast <= lastBlockOrdinal);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(kind); 
                }
#endif
            }
        }
    }
}
