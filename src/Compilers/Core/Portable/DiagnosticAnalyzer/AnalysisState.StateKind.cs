﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        /// 4. Fully processed: <see cref="FullyProcessed"/>.
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
            InProcess,

            /// <summary>
            /// Fully processed.
            /// </summary>
            FullyProcessed,
        }
    }
}
