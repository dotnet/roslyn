// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.SemanticSearch;

/// <summary>
/// Context necessary to generate Copilot prompt for semantic search query.
/// </summary>
internal sealed class SemanticSearchCopilotContext
{
    public required string ModelName { get; init; }

    /// <summary>
    /// List of package names and versions that to include in the prompt.
    /// </summary>
    public required IEnumerable<(string name, Version version)> AvailablePackages { get; init; }
}

internal readonly struct SemanticSearchCopilotGeneratedQuery
{
    /// <summary>
    /// The generated code or an error message.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// True if <see cref="Text"/> is an error message.
    /// </summary>
    public required bool IsError { get; init; }
}

