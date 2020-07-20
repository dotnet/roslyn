// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal readonly struct ComplexifiedSpan
    {
        public readonly TextSpan OriginalSpan;
        public readonly TextSpan NewSpan;
        public readonly ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)> ModifiedSubSpans;

        public ComplexifiedSpan(TextSpan originalSpan, TextSpan newSpan, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)> modifiedSubSpans)
        {
            OriginalSpan = originalSpan;
            NewSpan = newSpan;
            ModifiedSubSpans = modifiedSubSpans;
        }
    }
}
