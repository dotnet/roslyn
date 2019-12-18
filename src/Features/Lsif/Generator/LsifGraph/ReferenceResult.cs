// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    internal sealed class ReferenceResult : Vertex
    {
        public ReferenceResult()
            : base(label: "referenceResult")
        {
        }
    }
}
