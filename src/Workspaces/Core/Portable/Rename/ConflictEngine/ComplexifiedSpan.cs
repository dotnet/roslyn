// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    [DataContract]
    internal readonly struct ComplexifiedSpan
    {
        [DataMember(Order = 0)]
        public readonly TextSpan OriginalSpan;

        [DataMember(Order = 1)]
        public readonly TextSpan NewSpan;

        [DataMember(Order = 2)]
        public readonly ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)> ModifiedSubSpans;

        public ComplexifiedSpan(TextSpan originalSpan, TextSpan newSpan, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)> modifiedSubSpans)
        {
            OriginalSpan = originalSpan;
            NewSpan = newSpan;
            ModifiedSubSpans = modifiedSubSpans;
        }
    }
}
