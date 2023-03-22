// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json
{
    internal abstract class JsonNode : EmbeddedSyntaxNode<JsonKind, JsonNode>
    {
        protected JsonNode(JsonKind kind) : base(kind)
        {
        }

        public abstract void Accept(IJsonNodeVisitor visitor);
    }
}
