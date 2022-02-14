// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers.Snippets
{
    internal struct SnippetData
    {
        public readonly string DisplayName;

        public SnippetData(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
