// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class IteratorMethodToStateMachineRewriter
    {
        // storage of various information about a given try/finally frame
        private sealed class IteratorFinallyFrame
        {
            // finalize state of this frame. This is the state we should be in when we are "between real states"
            public readonly StateMachineState finalizeState;

            // Enclosing frame. Root frame does not have parent.
            public readonly IteratorFinallyFrame parent;

            // Finally handler function. We must run this when logically leaving the frame. Root does not have a handler.
            public readonly IteratorFinallyMethodSymbol handler;

            // All states encountered in nested frames mapped to corresponding finally frames.
            // This is enough information to restore Try/Finally tree structure in Dispose and dispatch any valid state
            // into a corresponding try.
            // NOTE: union of all values in this map gives all nested frames.
            public Dictionary<StateMachineState, IteratorFinallyFrame> knownStates;

            // labels within this frame (branching to these labels does not go through finally).
            public readonly HashSet<LabelSymbol> labels;

            // proxy labels for branches leaving the frame. 
            // we build this on demand once we encounter leaving branches.
            // subsequent leaves to an already proxied label redirected to the proxy.
            // At the proxy label we will execute finally and forward the control flow 
            // to the actual destination. (which could be proxied again in the parent)
            public Dictionary<LabelSymbol, LabelSymbol> proxyLabels;

            public IteratorFinallyFrame(
                IteratorFinallyFrame parent,
                StateMachineState finalizeState,
                IteratorFinallyMethodSymbol handler,
                HashSet<LabelSymbol> labels)
            {
                Debug.Assert(parent != null, "non root frame must have a parent");
                Debug.Assert((object)handler != null, "non root frame must have a handler");

                this.parent = parent;
                this.finalizeState = finalizeState;
                this.handler = handler;
                this.labels = labels;
            }

            public IteratorFinallyFrame()
            {
                this.finalizeState = StateMachineState.NotStartedOrRunningState;
            }

            public bool IsRoot()
            {
                return this.parent == null;
            }

            public void AddState(StateMachineState state)
            {
                if (parent != null)
                {
                    parent.AddState(state, this);
                }
            }

            // Notifies all parents about the state recursively. 
            // All parents need to know states they recursively contain and what 
            // immediate child can handle every particular state.
            private void AddState(StateMachineState state, IteratorFinallyFrame innerHandler)
            {
                var knownStates = this.knownStates;
                if (knownStates == null)
                {
                    this.knownStates = knownStates = new Dictionary<StateMachineState, IteratorFinallyFrame>();
                }

                knownStates.Add(state, innerHandler);

                if (parent != null)
                {
                    // Present ourselves to the parent as responsible for a handling a state.
                    parent.AddState(state, this);
                }
            }

            // returns a proxy for a label if branch must be hijacked to run finally
            // otherwise returns same label back
            public LabelSymbol ProxyLabelIfNeeded(LabelSymbol label)
            {
                // no need to proxy a label in the current frame or when we are at the root
                if (this.IsRoot() || (labels != null && labels.Contains(label)))
                {
                    return label;
                }

                var proxyLabels = this.proxyLabels;
                if (proxyLabels == null)
                {
                    this.proxyLabels = proxyLabels = new Dictionary<LabelSymbol, LabelSymbol>();
                }

                LabelSymbol proxy;
                if (!proxyLabels.TryGetValue(label, out proxy))
                {
                    proxy = new GeneratedLabelSymbol("proxy" + label.Name);
                    proxyLabels.Add(label, proxy);
                }

                return proxy;
            }
        }
    }
}
