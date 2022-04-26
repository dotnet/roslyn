// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal readonly struct RoslynLSPSnippetStringItem
    {
        /// <summary>
        /// The identifier in the snippet that needs to be renamed.
        /// Will be null in the case of the final tab stop location,
        /// the '$0' case.
        /// </summary>
        public readonly string? Identifier;

        /// <summary>
        /// The value associated with the identifier.
        /// EX: if (${1:true})
        ///     {$0
        ///     }
        /// The '1' and '0' are represented by this value.
        /// </summary>
        public readonly int Priority;

        public RoslynLSPSnippetStringItem(string? identifier, int priority)
        {
            Identifier = identifier;
            Priority = priority;
        }
    }
}
