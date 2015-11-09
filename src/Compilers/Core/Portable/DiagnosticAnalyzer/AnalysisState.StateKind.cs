// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalysisState
    {
        /// <summary>
        /// State kind of per-analyzer <see cref="AnalyzerStateData"/> tracking an analyzer's partial analysis state.
        /// An analysis state object can be in one of the following states:
        /// 1. Completely unprocessed: <see cref="ReadyToProcess"/>
        /// 2. Currently being processed: <see cref="InProcess"/>
        /// 3. Partially processed by one or more older requests that was either completed or cancelled: <see cref="ReadyToProcess"/>
        /// 4. Fully processed: We don't need a state kind to represent fully processed state as the analysis state object is discarded once fully processed.
        /// </summary>
        internal enum StateKind
        {
            /// <summary>
            /// Ready for processing.
            /// Indicates it is either completely unprocessed or partially processed by one or more older requests that was either completed or cancelled.
            /// </summary>
            ReadyToProcess,

            /// <summary>
            /// Currently being processed.
            /// </summary>
            InProcess
        }
    }
}
