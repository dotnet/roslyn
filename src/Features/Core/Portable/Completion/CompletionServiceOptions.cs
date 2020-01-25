﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CompletionServiceOptions
    {
        /// <summary>
        /// Indicates if the completion is trigger by toggle the expander.
        /// </summary>
        public static readonly Option<bool> IsExpandedCompletion
            = new Option<bool>(nameof(CompletionServiceOptions), nameof(IsExpandedCompletion), defaultValue: false);
    }
}
