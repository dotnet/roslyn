// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private static readonly Func<ControlFlowRegion, IEnumerable<ControlFlowRegion>> s_getTransitiveNestedRegions = GetTransitiveNestedRegions;

        internal BranchWithInfo(ControlFlowBranch branch)
            : this(branch.Destination!, branch.EnteringRegions, branch.LeavingRegions, branch.FinallyRegions,
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
                  branchValue: null,
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
            IOperation? branchValue,
            ControlFlowConditionKind controlFlowConditionKind,
            IEnumerable<ILocalSymbol> leavingRegionLocals,
            IEnumerable<CaptureId> leavingRegionFlowCaptures)
        {
            Destination = destination;
            Kind = kind;
            EnteringRegions = enteringRegions;
            LeavingRegions = leavingRegions;
            FinallyRegions = finallyRegions;
            BranchValue = branchValue;
            ControlFlowConditionKind = controlFlowConditionKind;
            LeavingRegionLocals = leavingRegionLocals;
            LeavingRegionFlowCaptures = leavingRegionFlowCaptures;
        }

        public BasicBlock Destination { get; }
        public ControlFlowBranchSemantics Kind { get; }
        public ImmutableArray<ControlFlowRegion> EnteringRegions { get; }
        public ImmutableArray<ControlFlowRegion> FinallyRegions { get; }
        public ImmutableArray<ControlFlowRegion> LeavingRegions { get; }
        public IOperation? BranchValue { get; }

        public ControlFlowConditionKind ControlFlowConditionKind { get; }

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
                branchValue: BranchValue,
                controlFlowConditionKind: ControlFlowConditionKind,
                leavingRegionLocals: ImmutableHashSet<ILocalSymbol>.Empty,
                leavingRegionFlowCaptures: ImmutableHashSet<CaptureId>.Empty);
        }

        internal BranchWithInfo With(
            IOperation? branchValue,
            ControlFlowConditionKind controlFlowConditionKind)
        {
            return new BranchWithInfo(Destination, EnteringRegions, LeavingRegions,
                FinallyRegions, Kind, branchValue, controlFlowConditionKind,
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
            return leavingRegions.SelectMany(s_getTransitiveNestedRegions).Distinct().SelectMany(r => r.Locals);
        }

        private static IEnumerable<CaptureId> ComputeLeavingRegionFlowCaptures(ImmutableArray<ControlFlowRegion> leavingRegions)
        {
            return leavingRegions.SelectMany(s_getTransitiveNestedRegions).Distinct().SelectMany(r => r.CaptureIds);
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
