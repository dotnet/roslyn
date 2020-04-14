// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    internal sealed class DefinitionResult : Vertex
    {
        public DefinitionResult()
            : base(label: "definitionResult")
        {
        }
    }
}
