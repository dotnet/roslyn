// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using static Microsoft.CodeAnalysis.Operations.ControlFlowGraph;

namespace Microsoft.CodeAnalysis.Operations.DataFlow
{
    /// <summary>
    /// Contains aggregated information about a control flow branch.
    /// </summary>
    internal sealed class BranchWithInfo
    {
        public BranchWithInfo(BasicBlock.Branch branch, IOperation conditionOpt, IOperation valueOpt, bool? jumpIfTrue)
            : this(branch.Destination, branch.EnteringRegions, branch.LeavingRegions, branch.FinallyRegions,
                  branch.Kind, conditionOpt, valueOpt, jumpIfTrue)
        {
        }

        public BranchWithInfo(BasicBlock destination)
            : this(destination, enteringRegions: ImmutableArray<Region>.Empty, leavingRegions: ImmutableArray<Region>.Empty,
                  finallyRegions: ImmutableArray<Region>.Empty, kind: BasicBlock.BranchKind.Regular,
                  conditionOpt: null, valueOpt: null, jumpIfTrue: null)
        {
        }

        private BranchWithInfo(
            BasicBlock destination,
            ImmutableArray<Region> enteringRegions,
            ImmutableArray<Region> leavingRegions,
            ImmutableArray<Region> finallyRegions,
            BasicBlock.BranchKind kind,
            IOperation conditionOpt,
            IOperation valueOpt,
            bool? jumpIfTrue)
        {
            Destination = destination;
            Kind = kind;
            EnteringRegions = enteringRegions;
            LeavingRegions = leavingRegions;
            FinallyRegions = finallyRegions;
            ConditionOpt = conditionOpt;
            ValueOpt = valueOpt;
            JumpIfTrue = jumpIfTrue;
        }

        public BasicBlock Destination { get; }
        public BasicBlock.BranchKind Kind { get; }
        public ImmutableArray<Region> EnteringRegions { get; }
        public ImmutableArray<Region> FinallyRegions { get; }
        public ImmutableArray<Region> LeavingRegions { get; }
        public IOperation ConditionOpt { get; }
        public IOperation ValueOpt { get; }
        public bool? JumpIfTrue { get; }

        public BranchWithInfo With(
            BasicBlock destination,
            ImmutableArray<Region> enteringRegions,
            ImmutableArray<Region> leavingRegions,
            ImmutableArray<Region> finallyRegions)
        {
            return new BranchWithInfo(destination, enteringRegions, leavingRegions,
                finallyRegions, Kind, ConditionOpt, ValueOpt, JumpIfTrue);
        }

        public BranchWithInfo With(
            IOperation conditionOpt,
            IOperation valueOpt,
            bool? jumpIfTrue)
        {
            return new BranchWithInfo(Destination, EnteringRegions, LeavingRegions,
                FinallyRegions, Kind, conditionOpt, valueOpt, jumpIfTrue);
        }
    }
}
