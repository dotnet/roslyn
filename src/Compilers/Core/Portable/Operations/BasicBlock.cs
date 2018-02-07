﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Operations
{
    public enum BasicBlockKind
    {
        Entry,
        Exit,
        Block
    }

    public sealed class BasicBlock
    {
        private ImmutableArray<IOperation>.Builder _statements;
        private ImmutableHashSet<BasicBlock>.Builder _predecessors;

        public BasicBlock(BasicBlockKind kind)
        {
            Kind = kind;
            _statements = ImmutableArray.CreateBuilder<IOperation>();
            _predecessors = ImmutableHashSet.CreateBuilder<BasicBlock>();
        }

        public BasicBlockKind Kind { get; private set; }
        public ImmutableArray<IOperation> Statements => _statements.ToImmutable();
        public (IOperation Condition, bool JumpIfTrue, BasicBlock Destination) Conditional { get; internal set; }
        public BasicBlock Next { get; internal set; }
        public ImmutableHashSet<BasicBlock> Predecessors => _predecessors.ToImmutable();

        internal void AddStatement(IOperation statement)
        {
            _statements.Add(statement);
        }

        internal void AddPredecessor(BasicBlock block)
        {
            _predecessors.Add(block);
        }

        internal void RemovePredecessor(BasicBlock block)
        {
            _predecessors.Remove(block);
        }
    }
}
