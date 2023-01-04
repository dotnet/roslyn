// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.DocumentHighlighting;

namespace Microsoft.CodeAnalysis.BraceMatching
{
    [DataContract]
    internal readonly record struct BraceMatchingOptions(
        [property: DataMember(Order = 0)] HighlightingOptions HighlightingOptions)
    {
        public BraceMatchingOptions()
            : this(HighlightingOptions.Default)
        {
        }

        public static readonly BraceMatchingOptions Default = new();
    }
}
