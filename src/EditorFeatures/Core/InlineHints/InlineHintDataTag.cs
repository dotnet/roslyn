// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// The simple tag that only holds information regarding the associated parameter name
    /// for the argument
    /// </summary>
    internal class InlineHintDataTag : ITag
    {
        public readonly InlineHint Hint;

        public InlineHintDataTag(InlineHint hint)
        {
            Hint = hint;
        }
    }
}
