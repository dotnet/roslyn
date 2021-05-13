// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SyntaxReceiverInputNode : ISyntaxInputNode, IIncrementalGeneratorNode<ISyntaxContextReceiver>
    {
        private readonly SyntaxContextReceiverCreator _receiverCreator;

        public SyntaxReceiverInputNode(SyntaxContextReceiverCreator receiverCreator)
        {
            _receiverCreator = receiverCreator;
        }

        public NodeStateTable<ISyntaxContextReceiver> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<ISyntaxContextReceiver> previousTable, CancellationToken cancellationToken)
        {
            return (NodeStateTable<ISyntaxContextReceiver>)graphState.GetSyntaxInputTable(this);
        }

        public IIncrementalGeneratorNode<ISyntaxContextReceiver> WithComparer(IEqualityComparer<ISyntaxContextReceiver> comparer)
        {
            // we don't publically expose this node to end users
            throw ExceptionUtilities.Unreachable;
        }

        public ISyntaxInputBuilder GetBuilder(DriverStateTable table) => new Builder(this, table);

        public class Builder : ISyntaxInputBuilder
        {
            private readonly SyntaxReceiverInputNode _owner;
            private readonly NodeStateTable<ISyntaxContextReceiver>.Builder _nodeStateTable;
            private readonly ISyntaxContextReceiver? _receiver;
            private readonly GeneratorSyntaxWalker? _walker;
            private Exception? _exception;

            public Builder(SyntaxReceiverInputNode owner, DriverStateTable driverStateTable)
            {
                _owner = owner;
                _nodeStateTable = driverStateTable.GetStateTableOrEmpty<ISyntaxContextReceiver>(_owner).ToBuilder();
                try
                {
                    _receiver = owner._receiverCreator();
                }
                catch (Exception e)
                {
                    _exception = e;
                }

                if (_receiver is object)
                {
                    _walker = new GeneratorSyntaxWalker(_receiver);
                }
            }

            public void SaveStateAndFree(ImmutableDictionary<object, IStateTable>.Builder tables)
            {
                if (_exception is object)
                {
                    _nodeStateTable.SetFaulted(_exception);
                }
                else if (_receiver is object)
                {
                    _nodeStateTable.AddEntries(ImmutableArray.Create(_receiver), EntryState.Modified);
                }
                tables[_owner] = _nodeStateTable.ToImmutableAndFree();
            }

            public void VisitTree(SyntaxNode root, EntryState state, SemanticModel? model)
            {
                if (_walker is object && _exception is null && state != EntryState.Removed)
                {
                    Debug.Assert(model is object);
                    try
                    {
                        _walker.VisitWithModel(model, root);
                    }
                    catch (Exception e)
                    {
                        _exception = e;
                    }
                }
            }
        }
    }
}
