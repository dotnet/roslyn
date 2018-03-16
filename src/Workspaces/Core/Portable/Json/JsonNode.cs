// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.Json
{
    internal abstract class JsonNode : EmbeddedSyntaxNode<JsonKind, JsonNode>
    {
        protected JsonNode(JsonKind kind) : base(kind)
        {
        }

        public abstract void Accept(IJsonNodeVisitor visitor);
    }
}
