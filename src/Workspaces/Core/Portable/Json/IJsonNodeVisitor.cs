// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Json
{
    internal interface IJsonNodeVisitor
    {
        void Visit(JsonCompilationUnit node);
        void Visit(JsonSequenceNode node);
        void Visit(JsonArrayNode node);
        void Visit(JsonObjectNode node);
        void Visit(JsonPropertyNode node);
        void Visit(JsonConstructorNode node);
        void Visit(JsonLiteralNode node);
        void Visit(JsonNegativeLiteralNode node);
        void Visit(JsonTextNode node);
        void Visit(JsonEmptyValueNode node);
    }
}
