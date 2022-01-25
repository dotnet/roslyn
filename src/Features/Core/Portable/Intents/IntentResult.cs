﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Features.Intents
{
    /// <summary>
    /// Defines the text changes needed to apply an intent.
    /// </summary>
    internal struct IntentProcessorResult
    {
        /// <summary>
        /// The changed solution for this intent result.
        /// </summary>
        public readonly Solution Solution;

        /// <summary>
        /// The title associated with this intent result.
        /// </summary>
        public readonly string Title;

        /// <summary>
        /// Contains metadata that can be used to identify the kind of sub-action these edits
        /// apply to for the requested intent.
        /// </summary>
        public readonly string ActionName;

        public IntentProcessorResult(Solution solution, string title, string actionName)
        {
            Solution = solution;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            ActionName = actionName ?? throw new ArgumentNullException(nameof(actionName));
        }
    }
}
