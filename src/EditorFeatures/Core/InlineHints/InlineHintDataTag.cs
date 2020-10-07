// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// The simple tag that only holds information regarding the associated parameter name
    /// for the argument
    /// </summary>
    internal class InlineHintDataTag : ITag
    {
        public readonly string Text;
        public readonly SymbolKey? SymbolKey;

        public InlineHintDataTag(string text, SymbolKey? symbolKey)
        {
            if (text.Length == 0)
                throw new ArgumentException("Must have a length greater than 0", nameof(text));

            Text = text;
            SymbolKey = symbolKey;
        }
    }
}
