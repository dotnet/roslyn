// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;

internal interface ISemanticSearchCopilotServiceImpl
{
    ValueTask<SemanticSearchCopilotGeneratedQueryImpl> TryGetQueryAsync(string text, SemanticSearchCopilotContextImpl context, CancellationToken cancellationToken);
}

internal sealed class SemanticSearchCopilotContextImpl
{
    public required string ModelName { get; init; }

    /// <summary>
    /// List of package names and versions that to include in the prompt.
    /// </summary>
    public required IEnumerable<(string name, Version version)> AvailablePackages { get; init; }
}

internal readonly struct SemanticSearchCopilotGeneratedQueryImpl
{
    /// <summary>
    /// True if <see cref="Text"/> is an error message.
    /// </summary>
    public required bool IsError { get; init; }

    /// <summary>
    /// The generated code or an error message.
    /// </summary>
    public required string Text { get; init; }
}
