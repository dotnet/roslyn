// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// PROTOTYPE(dataflow): Add documentation
    /// </summary>
    public enum BasicBlockKind
    {
        Entry,
        Exit,
        Block
    }

    /// <summary>
    /// PROTOTYPE(dataflow): Add documentation
    /// </summary>
    public enum ConditionalBranchKind
    {
        /// <summary>
        /// Jump if value is true
        /// </summary>
        IfTrue,

        /// <summary>
        /// Jump if value is false
        /// </summary>
        IfFalse,

        /// <summary>
        /// Jump if value is null/Nothing 
        /// </summary>
        IfNull
    }

    /// <summary>
    /// PROTOTYPE(dataflow): Add documentation
    /// PROTOTYPE(dataflow): We need to figure out how to split it into a builder and 
    ///                      a public immutable type.
    /// </summary>
    public sealed class BasicBlock
    {
        private readonly ImmutableArray<IOperation>.Builder _statements;
        private readonly ImmutableHashSet<BasicBlock>.Builder _predecessors;

        public BasicBlock(BasicBlockKind kind)
        {
            Kind = kind;
            _statements = ImmutableArray.CreateBuilder<IOperation>();
            _predecessors = ImmutableHashSet.CreateBuilder<BasicBlock>();
        }

        public BasicBlockKind Kind { get; private set; }
        public ImmutableArray<IOperation> Statements => _statements.ToImmutable();

        /// <summary>
        /// PROTOTYPE(dataflow): Tuple is temporary return type, we probably should use special structure instead.
        /// </summary>
        public (IOperation Value, ConditionalBranchKind Kind, BasicBlock Destination) Conditional { get; internal set; }

        /// <summary>
        /// PROTOTYPE(dataflow): During CR there was a suggestion to use different name - "Successor".
        /// </summary>
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
