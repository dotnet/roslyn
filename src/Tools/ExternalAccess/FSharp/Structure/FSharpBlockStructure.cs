// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Structure
{
    internal class FSharpBlockStructure
    {
        public ImmutableArray<FSharpBlockSpan> Spans { get; }

        public FSharpBlockStructure(ImmutableArray<FSharpBlockSpan> spans)
        {
            Spans = spans;
        }
    }
}
