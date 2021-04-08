// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class PipelineStateTable
    {
        // PROTOTYPE(source-generators): should we make a non generic node interface that we can use as the key
        //                               instead of just object?
        private readonly ImmutableDictionary<object, IStateTable> _tables;

        internal static PipelineStateTable Empty { get; } = new PipelineStateTable(ImmutableDictionary<object, IStateTable>.Empty);

        private PipelineStateTable(ImmutableDictionary<object, IStateTable> tables)
        {
            _tables = tables;
        }

        public class Builder
        {
            private readonly ImmutableDictionary<object, IStateTable>.Builder _tableBuilder = ImmutableDictionary<object, IStateTable>.Empty.ToBuilder();

            private readonly PipelineStateTable _previousTable;

            public Builder(PipelineStateTable previousTable)
            {
                _previousTable = previousTable;
            }

            public NodeStateTable<T> GetLatestStateTableForNode<T>(IIncrementalGeneratorNode<T> source)
            {
                // if we've already evaluated node during this build, we can just return the existing result
                if (_tableBuilder.ContainsKey(source))
                {
                    return (NodeStateTable<T>)_tableBuilder[source];
                }

                // get the previous table, if there was one for this node
                NodeStateTable<T> previousTable = _previousTable._tables.ContainsKey(source)
                                                  ? (NodeStateTable<T>)_previousTable._tables[source]
                                                  : NodeStateTable<T>.Empty;

                // request the node update its state based and store the new result
                var newTable = source.UpdateStateTable(this, previousTable);
                _tableBuilder[source] = newTable;
                return newTable;
            }

            public PipelineStateTable ToImmutable()
            {
                // we can compact the tables at this point, as we'll no longer be using them to determine current state
                var keys = _tableBuilder.Keys.ToArray();
                for (int i = 0; i < _tableBuilder.Count; i++)
                {
                    _tableBuilder[keys[i]] = _tableBuilder[keys[i]].Compact();
                }

                return new PipelineStateTable(_tableBuilder.ToImmutable());
            }
        }
    }
}
