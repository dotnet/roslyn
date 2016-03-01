// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

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
            }

            public virtual void Free()
            {
                this.StateKind = StateKind.ReadyToProcess;
                this.ProcessedActions.Clear();
            }
        }
    }
}
