// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.InlineHints
{
    /// <summary>
    /// TODO: remove. https://github.com/dotnet/roslyn/issues/57283
    /// </summary>
    internal sealed class InlineHintsGlobalStateOption
    {
        /// <summary>
        /// Non-persisted option used to switch to displaying everything while the user is holding ctrl-alt.
        /// </summary>
        public static readonly Option2<bool> DisplayAllOverride =
            new("InlineHintsOptions", "DisplayAllOverride", defaultValue: false);
    }
}
