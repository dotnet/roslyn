// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace BuildValidator
{
    /// <summary>An entry in the source-link.json dictionary.</summary>
    internal readonly struct SourceLink
    {
        public string Prefix { get; }
        public string Replace { get; }

        public SourceLink(string prefix, string replace)
        {
            Prefix = prefix;
            Replace = replace;
        }
    }
}
