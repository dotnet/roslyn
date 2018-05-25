// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Defines kinds of regions that can be present in a <see cref="ControlFlowGraph"/>
    /// PROTOTYPE(dataflow): Split <see cref="ControlFlowRegion"/> and <see cref="ControlFlowRegionKind"/> into separate files.
    /// </summary>
    public enum ControlFlowRegionKind
    {
        /// <summary>
        /// A root region encapsulating all <see cref="BasicBlock"/>s in a <see cref="ControlFlowGraph"/>
        /// </summary>
        Root,

        /// <summary>
        /// Region with the only purpose to represent the life-time of locals and methods (local functions, lambdas).
        /// PROTOTYPE(dataflow): We should clearly explain what "life-time" refers to here, or use a different term.
        /// PROTOTYPE(dataflow): Should consider renaming to "Lifetime" or something else so that the name
        ///                      wouldn't imply that it is only about locals
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

        /// <summary>
        /// Region representing the initialization for a VB <code>Static</code> local variable. This region will only be executed
        /// the first time a function is called.
        /// </summary>
        StaticLocalInitializer,

        /// <summary>
        /// Region representing erroneous block of code that is unreachable from the entry block.
        /// </summary>
        ErroneousBody,
    }

    /// <summary>
    /// Encapsulates information about regions of <see cref="BasicBlock"/>s in a <see cref="ControlFlowGraph"/>.
    /// Regions can overlap, but never cross each other boundaries.
    /// </summary>
    public sealed class ControlFlowRegion
    {
        /// <summary>
        /// Region's kind
        /// </summary>
        public ControlFlowRegionKind Kind { get; }

        /// <summary>
        /// Enclosing region. Null for <see cref="ControlFlowRegionKind.Root"/>
        /// </summary>
        public ControlFlowRegion Enclosing { get; private set; }

        /// <summary>
        /// Target exception type for <see cref="ControlFlowRegionKind.Filter"/>, <see cref="ControlFlowRegionKind.Catch"/>, 
        /// <see cref="ControlFlowRegionKind.FilterAndHandler "/>
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
        public ImmutableArray<ControlFlowRegion> Regions { get; }

        /// <summary>
        /// Locals for which this region represent the life-time.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Locals { get; }

        /// <summary>
        /// Methods (local functions or lambdas) declared within the region.
        /// </summary>
        public ImmutableArray<IMethodSymbol> Methods { get; }

        internal ControlFlowRegion(ControlFlowRegionKind kind, int firstBlockOrdinal, int lastBlockOrdinal,
                        ImmutableArray<ControlFlowRegion> regions,
                        ImmutableArray<ILocalSymbol> locals,
                        ImmutableArray<IMethodSymbol> methods,
                        ITypeSymbol exceptionType,
                        ControlFlowRegion enclosing)
        {
            Debug.Assert(firstBlockOrdinal >= 0);
            Debug.Assert(lastBlockOrdinal >= firstBlockOrdinal);

            Kind = kind;
            FirstBlockOrdinal = firstBlockOrdinal;
            LastBlockOrdinal = lastBlockOrdinal;
            ExceptionType = exceptionType;
            Locals = locals.NullToEmpty();
            Methods = methods.NullToEmpty();
            Regions = regions.NullToEmpty();
            Enclosing = enclosing;

            foreach (ControlFlowRegion r in Regions)
            {
                Debug.Assert(r.Enclosing == null && r.Kind != ControlFlowRegionKind.Root);
                r.Enclosing = this;
            }
#if DEBUG
            int previousLast;

            switch (kind)
            {
                case ControlFlowRegionKind.TryAndFinally:
                case ControlFlowRegionKind.FilterAndHandler:
                    Debug.Assert(Regions.Length == 2);
                    Debug.Assert(Regions[0].Kind == (kind == ControlFlowRegionKind.TryAndFinally ? ControlFlowRegionKind.Try : ControlFlowRegionKind.Filter));
                    Debug.Assert(Regions[1].Kind == (kind == ControlFlowRegionKind.TryAndFinally ? ControlFlowRegionKind.Finally : ControlFlowRegionKind.Catch));
                    Debug.Assert(Regions[0].FirstBlockOrdinal == firstBlockOrdinal);
                    Debug.Assert(Regions[1].LastBlockOrdinal == lastBlockOrdinal);
                    Debug.Assert(Regions[0].LastBlockOrdinal + 1 == Regions[1].FirstBlockOrdinal);
                    break;

                case ControlFlowRegionKind.TryAndCatch:
                    Debug.Assert(Regions.Length >= 2);
                    Debug.Assert(Regions[0].Kind == ControlFlowRegionKind.Try);
                    Debug.Assert(Regions[0].FirstBlockOrdinal == firstBlockOrdinal);
                    previousLast = Regions[0].LastBlockOrdinal;

                    for (int i = 1; i < Regions.Length; i++)
                    {
                        ControlFlowRegion r = Regions[i];
                        Debug.Assert(previousLast + 1 == r.FirstBlockOrdinal);
                        previousLast = r.LastBlockOrdinal;

                        Debug.Assert(r.Kind == ControlFlowRegionKind.FilterAndHandler || r.Kind == ControlFlowRegionKind.Catch);
                    }

                    Debug.Assert(previousLast == lastBlockOrdinal);
                    break;

                case ControlFlowRegionKind.Root:
                case ControlFlowRegionKind.Locals:
                case ControlFlowRegionKind.Try:
                case ControlFlowRegionKind.Filter:
                case ControlFlowRegionKind.Catch:
                case ControlFlowRegionKind.Finally:
                case ControlFlowRegionKind.StaticLocalInitializer:
                case ControlFlowRegionKind.ErroneousBody:
                    previousLast = firstBlockOrdinal - 1;

                    foreach (ControlFlowRegion r in Regions)
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

        internal bool ContainsBlock(int destinationOrdinal)
        {
            return FirstBlockOrdinal <= destinationOrdinal && LastBlockOrdinal >= destinationOrdinal;
        }
    }
}
