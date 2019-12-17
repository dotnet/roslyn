// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph;

namespace Microsoft.CodeAnalysis.Lsif.Generator.Writing
{
    internal interface ILsifJsonWriter
    {
        void Write(Vertex vertex);
        void Write(Edge edge);
    }
}
