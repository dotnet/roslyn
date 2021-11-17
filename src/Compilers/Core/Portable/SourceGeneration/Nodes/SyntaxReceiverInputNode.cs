// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SyntaxReceiverInputNode : ISyntaxInputNode, IIncrementalGeneratorNode<ISyntaxContextReceiver?>
    {
        private readonly SyntaxContextReceiverCreator _receiverCreator;
        private readonly Action<IIncrementalGeneratorOutputNode> _registerOutput;

        public SyntaxReceiverInputNode(SyntaxContextReceiverCreator receiverCreator, Action<IIncrementalGeneratorOutputNode> registerOutput)
        {
            _receiverCreator = receiverCreator;
            _registerOutput = registerOutput;
        }

        public NodeStateTable<ISyntaxContextReceiver?> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<ISyntaxContextReceiver?> previousTable, CancellationToken cancellationToken)
        {
            return (NodeStateTable<ISyntaxContextReceiver?>)graphState.GetSyntaxInputTable(this);
        }

        public IIncrementalGeneratorNode<ISyntaxContextReceiver?> WithComparer(IEqualityComparer<ISyntaxContextReceiver?> comparer)
        {
            // we don't expose this node to end users
            throw ExceptionUtilities.Unreachable;
        }

        public ISyntaxInputBuilder GetBuilder(DriverStateTable table) => new Builder(this, table);

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _registerOutput(output);

        private sealed class Builder : ISyntaxInputBuilder
        {
            private readonly SyntaxReceiverInputNode _owner;
            private readonly NodeStateTable<ISyntaxContextReceiver?>.Builder _nodeStateTable;
            private readonly ISyntaxContextReceiver? _receiver;
            private readonly GeneratorSyntaxWalker? _walker;

            public Builder(SyntaxReceiverInputNode owner, DriverStateTable driverStateTable)
            {
                _owner = owner;
                _nodeStateTable = driverStateTable.GetStateTableOrEmpty<ISyntaxContextReceiver?>(_owner).ToBuilder();
                try
                {
                    _receiver = owner._receiverCreator();
                }
                catch (Exception e)
                {
                    throw new UserFunctionException(e);
                }

                if (_receiver is object)
                {
                    _walker = new GeneratorSyntaxWalker(_receiver);
                }
            }

            public ISyntaxInputNode SyntaxInputNode { get => _owner; }

            public void SaveStateAndFree(ImmutableSegmentedDictionary<object, IStateTable>.Builder tables)
            {
                _nodeStateTable.AddEntry(_receiver, EntryState.Modified);
                tables[_owner] = _nodeStateTable.ToImmutableAndFree();
            }

            public void VisitTree(Lazy<SyntaxNode> root, EntryState state, SemanticModel? model, CancellationToken cancellationToken)
            {
                if (_walker is object && state != EntryState.Removed)
                {
                    Debug.Assert(model is object);
                    try
                    {
                        _walker.VisitWithModel(model, root.Value);
                    }
                    catch (Exception e)
                    {
                        throw new UserFunctionException(e);
                    }
                }
            }
        }
    }
}
