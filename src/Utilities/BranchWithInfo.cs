// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Contains aggregated information about a control flow branch.
    /// </summary>
    internal sealed class BranchWithInfo
    {
        public BranchWithInfo(ControlFlowBranch branch)
            : this(branch.Destination, branch.EnteringRegions, branch.LeavingRegions, branch.FinallyRegions,
                  branch.Semantics, branch.Source.BranchValue,
                  GetControlFlowConditionKind(branch))
        {
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

        public BranchWithInfo(BasicBlock destination)
            : this(destination, enteringRegions: ImmutableArray<ControlFlowRegion>.Empty, leavingRegions: ImmutableArray<ControlFlowRegion>.Empty,
                  finallyRegions: ImmutableArray<ControlFlowRegion>.Empty, kind: ControlFlowBranchSemantics.Regular,
                  branchValueOpt: null, controlFlowConditionKind: ControlFlowConditionKind.None)
        {
        }

        private BranchWithInfo(
            BasicBlock destination,
            ImmutableArray<ControlFlowRegion> enteringRegions,
            ImmutableArray<ControlFlowRegion> leavingRegions,
            ImmutableArray<ControlFlowRegion> finallyRegions,
            ControlFlowBranchSemantics kind,
            IOperation branchValueOpt,
            ControlFlowConditionKind controlFlowConditionKind)
        {
            Destination = destination;
            Kind = kind;
            EnteringRegions = enteringRegions;
            LeavingRegions = leavingRegions;
            FinallyRegions = finallyRegions;
            BranchValueOpt = branchValueOpt;
            ControlFlowConditionKind = controlFlowConditionKind;
        }

        public BasicBlock Destination { get; }
        public ControlFlowBranchSemantics Kind { get; }
        public ImmutableArray<ControlFlowRegion> EnteringRegions { get; }
        public ImmutableArray<ControlFlowRegion> FinallyRegions { get; }
        public ImmutableArray<ControlFlowRegion> LeavingRegions { get; }
        public IOperation BranchValueOpt { get; }
        public ControlFlowConditionKind ControlFlowConditionKind { get; }

        public BranchWithInfo With(
            BasicBlock destination,
            ImmutableArray<ControlFlowRegion> enteringRegions,
            ImmutableArray<ControlFlowRegion> leavingRegions,
            ImmutableArray<ControlFlowRegion> finallyRegions)
        {
            return new BranchWithInfo(destination, enteringRegions, leavingRegions,
                finallyRegions, Kind, BranchValueOpt, ControlFlowConditionKind);
        }

        public BranchWithInfo With(
            IOperation branchValueOpt,
            ControlFlowConditionKind controlFlowConditionKind)
        {
            return new BranchWithInfo(Destination, EnteringRegions, LeavingRegions,
                FinallyRegions, Kind, branchValueOpt, controlFlowConditionKind);
        }
    }
}
