// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalysisState
    {
        /// <summary>
        /// Stores the partial analysis state for a specific event/symbol/tree for a specific analyzer.
        /// </summary>
        internal class AnalyzerStateData
        {
            /// <summary>
            /// Current state of analysis.
            /// </summary>
            public StateKind StateKind { get; private set; }

            /// <summary>
            /// Set of completed actions.
            /// </summary>
            public HashSet<AnalyzerAction> ProcessedActions { get; }

            /// <summary>
            /// Deferred callback for freeing this state object and returning it back to pool of state objects.
            /// This callback is initialized to a non-null value when
            /// <see cref="FreeAndReturnToPool{TAnalyzerStateData}(ObjectPool{TAnalyzerStateData})"/>
            /// is invoked while the state object is still being used by another thread.
            /// Once the state object is reset back to <see cref="StateKind.ReadyToProcess"/>, we perform
            /// this deferred free operation and return the object back to the pool.
            /// </summary>
            private Action _callbackForDeferredFreeOperation;

            public static readonly AnalyzerStateData FullyProcessedInstance = CreateFullyProcessedInstance();

            public AnalyzerStateData()
            {
                StateKind = StateKind.InProcess;
                ProcessedActions = new HashSet<AnalyzerAction>();
            }

            private static AnalyzerStateData CreateFullyProcessedInstance()
            {
                var instance = new AnalyzerStateData();
                instance.SetStateKind(StateKind.FullyProcessed);
                return instance;
            }

            public virtual void SetStateKind(StateKind stateKind)
            {
                StateKind = stateKind;
            }

            /// <summary>
            /// Resets the <see cref="StateKind"/> from <see cref="StateKind.InProcess"/> to <see cref="StateKind.ReadyToProcess"/>.
            /// This method must be invoked after successful analysis completion AND on analysis cancellation.
            /// </summary>
            public void ResetToReadyState()
            {
                SetStateKind(StateKind.ReadyToProcess);

                if (_callbackForDeferredFreeOperation != null)
                {
                    _callbackForDeferredFreeOperation();
                    _callbackForDeferredFreeOperation = null;
                }
            }

            public virtual void Free()
            {
                this.StateKind = StateKind.ReadyToProcess;
                this.ProcessedActions.Clear();
            }

            public void FreeAndReturnToPool<TAnalyzerStateData>(ObjectPool<TAnalyzerStateData> pool)
                where TAnalyzerStateData : AnalyzerStateData
            {
                var state = (TAnalyzerStateData)this;
                if (StateKind == StateKind.InProcess)
                {
                    // Do not free the state here if it is still being processed for analysis,
                    // i.e. StateKind is 'InProcess'. Instead, marked it for a deferred free operation to
                    // execute after the StateKind is reset to 'ReadyToProcess'.
                    // This can happen in rare cases when multiple threads are trying to analyze the same
                    // symbol/syntax/operation, such that one thread has finished all the analyzer callbacks,
                    // but before this thread marks the state as complete and invokes this Free call,
                    // second thread starts processing the same symbol/syntax/operation and sets the StateKind to
                    // 'InProcess' for the same state object. If we free the state here in the first thread,
                    // then this can lead to data corruption/exceptions in the second thread.
                    // For example, see https://github.com/dotnet/roslyn/issues/59988.
                    _callbackForDeferredFreeOperation = () => FreeAndReturnToPoolCore(state, pool);
                }
                else
                {
                    FreeAndReturnToPoolCore(state, pool);
                }

                static void FreeAndReturnToPoolCore(TAnalyzerStateData state, ObjectPool<TAnalyzerStateData> pool)
                {
                    state.Free();
                    pool.Free(state);
                }
            }
        }
    }
}
