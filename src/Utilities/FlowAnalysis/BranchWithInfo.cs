// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Contains aggregated information about a control flow branch.
    /// </summary>
    public sealed class BranchWithInfo
    {
        internal BranchWithInfo(ControlFlowBranch branch)
            : this(branch.Destination, branch.EnteringRegions, branch.LeavingRegions, branch.FinallyRegions,
                  branch.Semantics, branch.Source.BranchValue,
                  GetControlFlowConditionKind(branch),
                  leavingRegionLocals: ComputeLeavingRegionLocals(branch.LeavingRegions),
                  leavingRegionFlowCaptures: ComputeLeavingRegionFlowCaptures(branch.LeavingRegions))
        {
        }

        internal BranchWithInfo(BasicBlock destination)
            : this(destination,
                  enteringRegions: ImmutableArray<ControlFlowRegion>.Empty,
                  leavingRegions: ImmutableArray<ControlFlowRegion>.Empty,
                  finallyRegions: ImmutableArray<ControlFlowRegion>.Empty,
                  kind: ControlFlowBranchSemantics.Regular,
                  branchValueOpt: null,
                  controlFlowConditionKind: ControlFlowConditionKind.None,
                  leavingRegionLocals: ImmutableHashSet<ILocalSymbol>.Empty,
                  leavingRegionFlowCaptures: ImmutableHashSet<CaptureId>.Empty)
        {
        }

        private BranchWithInfo(
            BasicBlock destination,
            ImmutableArray<ControlFlowRegion> enteringRegions,
            ImmutableArray<ControlFlowRegion> leavingRegions,
            ImmutableArray<ControlFlowRegion> finallyRegions,
            ControlFlowBranchSemantics kind,
            IOperation? branchValueOpt,
            ControlFlowConditionKind controlFlowConditionKind,
            IEnumerable<ILocalSymbol> leavingRegionLocals,
            IEnumerable<CaptureId> leavingRegionFlowCaptures)
        {
            Destination = destination;
            Kind = kind;
            EnteringRegions = enteringRegions;
            LeavingRegions = leavingRegions;
            FinallyRegions = finallyRegions;
            BranchValueOpt = branchValueOpt;
            ControlFlowConditionKind = controlFlowConditionKind;
            LeavingRegionLocals = leavingRegionLocals;
            LeavingRegionFlowCaptures = leavingRegionFlowCaptures;
        }

        public BasicBlock Destination { get; }
        public ControlFlowBranchSemantics Kind { get; }
        public ImmutableArray<ControlFlowRegion> EnteringRegions { get; }
        public ImmutableArray<ControlFlowRegion> FinallyRegions { get; }
        public ImmutableArray<ControlFlowRegion> LeavingRegions { get; }
        public IOperation? BranchValueOpt { get; }

#pragma warning disable CA1721 // Property names should not match get methods - https://github.com/dotnet/roslyn-analyzers/issues/2085
        public ControlFlowConditionKind ControlFlowConditionKind { get; }
#pragma warning restore CA1721 // Property names should not match get methods

        public IEnumerable<ILocalSymbol> LeavingRegionLocals { get; }
        public IEnumerable<CaptureId> LeavingRegionFlowCaptures { get; }

        internal BranchWithInfo WithEmptyRegions(BasicBlock destination)
        {
            return new BranchWithInfo(
                destination,
                enteringRegions: ImmutableArray<ControlFlowRegion>.Empty,
                leavingRegions: ImmutableArray<ControlFlowRegion>.Empty,
                finallyRegions: ImmutableArray<ControlFlowRegion>.Empty,
                kind: Kind,
                branchValueOpt: BranchValueOpt,
                controlFlowConditionKind: ControlFlowConditionKind,
                leavingRegionLocals: ImmutableHashSet<ILocalSymbol>.Empty,
                leavingRegionFlowCaptures: ImmutableHashSet<CaptureId>.Empty);
        }

        internal BranchWithInfo With(
            IOperation? branchValueOpt,
            ControlFlowConditionKind controlFlowConditionKind)
        {
            return new BranchWithInfo(Destination, EnteringRegions, LeavingRegions,
                FinallyRegions, Kind, branchValueOpt, controlFlowConditionKind,
                LeavingRegionLocals, LeavingRegionFlowCaptures);
        }

        private static IEnumerable<ControlFlowRegion> GetTransitiveNestedRegions(ControlFlowRegion region)
        {
            yield return region;

            foreach (var nestedRegion in region.NestedRegions)
            {
                foreach (var transitiveNestedRegion in GetTransitiveNestedRegions(nestedRegion))
                {
                    yield return transitiveNestedRegion;
                }
            }
        }

        private static IEnumerable<ILocalSymbol> ComputeLeavingRegionLocals(ImmutableArray<ControlFlowRegion> leavingRegions)
        {
            return leavingRegions.SelectMany(GetTransitiveNestedRegions).Distinct().SelectMany(r => r.Locals);
        }

        private static IEnumerable<CaptureId> ComputeLeavingRegionFlowCaptures(ImmutableArray<ControlFlowRegion> leavingRegions)
        {
            return leavingRegions.SelectMany(GetTransitiveNestedRegions).Distinct().SelectMany(r => r.CaptureIds);
        }

        private static ControlFlowConditionKind GetControlFlowConditionKind(ControlFlowBranch branch)
        {
            if (branch.IsConditionalSuccessor ||
                branch.Source.ConditionKind == ControlFlowConditionKind.None)
            {
                return branch.Source.ConditionKind;
            }

            return branch.Source.ConditionKind.Negate();
        }
    }
}
