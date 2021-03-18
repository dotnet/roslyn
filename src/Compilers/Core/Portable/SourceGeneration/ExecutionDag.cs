// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
//    [DebuggerDisplay("{source.GetType().Name} -> ({typeof(TProperty).Name}){previousValue} -> {sink.GetType().Name}")]
//    internal struct Connection<TSource, TSink, TProperty> : IConnection
//           where TSource : IAction
//           where TSink : IAction
//    {
//        private readonly TSource source;
//        private readonly Func<TSource, TProperty> sourceValue;
//        private readonly TSink sink;
//        private readonly Action<TSink, TProperty> sinkValue;
//        private readonly Func<TProperty?, TProperty?, bool> areSame;

//#if DEBUG
//        internal readonly StackTrace? trace;
//#endif

//        TProperty? previousValue;

//        public Connection(TSource source, Func<TSource, TProperty> sourceValue, TSink sinkNode, Action<TSink, TProperty> sinkValue, Func<TProperty?, TProperty?, bool> areSame)
//        {
//            this.source = source;
//            this.sourceValue = sourceValue;
//            this.sink = sinkNode;
//            this.sinkValue = sinkValue;
//            this.areSame = areSame;
//            this.previousValue = default;

//#if DEBUG
//            this.trace = new StackTrace(fNeedFileInfo: true);
//#endif

//        }

//        public IAction Source => source;

//        public bool UpdateIfNeeded()
//        {
//            var value = sourceValue(this.source);
//            if (areSame(value, this.previousValue))
//            {
//                return false;
//            }

//            sinkValue(sink, value);
//            this.previousValue = value;
//            return true;
//        }
//    }

//    internal class ExecutionDag
//    {
//        internal HashSet<ISource> _inputs = new HashSet<ISource>();

//        internal HashSet<IAction> _nodes = new();

//        internal Dictionary<IAction, HashSet<IAction>> _forwardEdges = new();

//        internal Dictionary<IAction, HashSet<IConnection>> _backEdges = new();

//        //internal List<IAction> _connections = new();

//        public void BringUpToDate()
//        {
//            List<ISource> dirtySources = _inputs.Where(i => i.IsDirty).ToList();
//            if (dirtySources.Count == 0)
//            {
//                // no work to do.
//                return;
//            }


//            var evaluatedNodes = new Dictionary<IAction, bool>();

//            // we work at the bottom of the graph and tunnel up, working out what (if anything) changed
//            //TODO: we can cache this once, and reuse it
//            List<IAction> sinks = _nodes.Where(n => !_forwardEdges.ContainsKey(n)).ToList();
//            Debug.Assert(sinks.Count != 0); // must have sinks if there are no cycles

//            foreach (var sink in sinks)
//            {
//                evaluateNode(sink, evaluatedNodes);
//            }


//            bool evaluateNode(IAction node, Dictionary<IAction, bool> evaluatedNodes)
//            {
//                if (evaluatedNodes.ContainsKey(node))
//                    return evaluatedNodes[node];

//                if (node is ISource source)
//                    return source.IsDirty;

//                // look at this nodes inputs
//                var connections = _backEdges[node];

//                bool isDirty = false;
//                List<IAction> connectionsToEval = new List<IAction>();
//                foreach (var connection in connections)
//                {
//                    // we need to decide if the parent node is up to date or not
//                    // if not, we execute, and report back that we updated

//                    // ultimately it chains up to a source node, which is dirty if not up-to-date
//                    // when that happens we want to pull on the value to get it updated
//                    // if (evaluateNode(input.))

//                    // but the connection itself determines if it is up to date, by pulling on the parent
//                    // so is this going to work??

//                    // we need to walk up the connections, until we get to the root node. Then if that's dirty,
//                    // we push the values all the way down?

//                    // no, we can query the node, if its dirty, we'll re-eval the inputs going to *this* node, and then we'll make a decision based on that.

//                    // so we call each sourcenode, and put the ones that are dirty in a list. Then we'll call execute on each in turn, and see if any update.
//                    // if any of them report as updating, then we can in turn report that we're dirty. 

//                    var inputSource = connection.Source;
//                    if (evaluateNode(inputSource, evaluatedNodes))
//                    {
//                        isDirty |= connection.UpdateIfNeeded();
//                    }
//                }

//                if (isDirty)
//                {
//                    // update this node
//                    node.Execute();
//                }

//                evaluatedNodes.Add(node, isDirty);
//                return isDirty;
//            }

//        }

//        public void RegisterConnection<TSource, TSink, TProperty>(TSource source, Func<TSource, TProperty> sourceValue, TSink sink, Action<TSink, TProperty> sinkValue, Func<TProperty?, TProperty?, bool> areSame)
//            where TSource : IAction
//            where TSink : IAction
//        {
//            //TODO: do we need this?
//            if (source is ISource isource)
//            {
//                _inputs.Add(isource);
//            }

//            //TODO: should we just maintain a node list?
//            _nodes.Add(sink);
//            _nodes.Add(source);

//            // check for cycles
//            if (isReachable(sink, source))
//            {
//                throw new InvalidOperationException("Cycle Detected");
//            }

//            _forwardEdges.AddConnection(source, sink);
//            _backEdges.AddConnection(sink, new Connection<TSource, TSink, TProperty>(source, sourceValue, sink, sinkValue, areSame));

//            bool isReachable(IAction from, IAction to)
//            {
//                // look up the from and see if we have an edge to to
//                // if not, then look at sources forward edges and see if any of those do

//                if (!_forwardEdges.ContainsKey(from))
//                    return false;

//                var edges = _forwardEdges[from];
//                foreach (var edge in edges)
//                {
//                    if (object.ReferenceEquals(edge, to))
//                    {
//                        return true;
//                    }
//                }

//                foreach (var edge in edges)
//                {
//                    if (isReachable(edge, to))
//                    {
//                        return true;
//                    }
//                }

//                return false;
//            }
//        }

//        private string DumpGraph()
//        {
//            return string.Empty;
//            //return new Vizualizer.ObjectSource().GetData(this);
//        }
//    }

//    static class Extensions
//    {
//        public static void AddConnection<TEdge>(this Dictionary<IAction, HashSet<TEdge>> edges, IAction from, TEdge to)
//        {
//            if (!edges.ContainsKey(from))
//            {
//                edges.Add(from, new());
//            }

//            edges[from].Add(to);
//        }
//    }
}
