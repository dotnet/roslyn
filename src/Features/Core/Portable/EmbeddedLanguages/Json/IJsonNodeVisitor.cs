// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;

internal interface IJsonNodeVisitor
{
    void Visit(JsonCompilationUnit node);
    void Visit(JsonArrayNode node);
    void Visit(JsonObjectNode node);
    void Visit(JsonPropertyNode node);
    void Visit(JsonConstructorNode node);
    void Visit(JsonLiteralNode node);
    void Visit(JsonNegativeLiteralNode node);
    void Visit(JsonTextNode node);
    void Visit(JsonCommaValueNode node);
}
