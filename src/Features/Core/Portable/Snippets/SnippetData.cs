// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    /// <summary>
    /// Stores only the dates needed for the creation of a CompletionItem.
    /// Avoids using the Snippet and creating a TextChange/finding cursor
    /// position before we know it was the selected CompletionItem.
    /// </summary>
    internal struct SnippetData
    {
        public readonly string DisplayName;

        public SnippetData(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
