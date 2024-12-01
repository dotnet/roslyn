// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal sealed class SemanticSearchProjectConfiguration
{
    public required string Language { get; init; }
    public required string Query { get; init; }
    public required string GlobalUsings { get; init; }
    public required string EditorConfig { get; init; }
    public required ParseOptions ParseOptions { get; init; }
    public required CompilationOptions CompilationOptions { get; init; }
}
