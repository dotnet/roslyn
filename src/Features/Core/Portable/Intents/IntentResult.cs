// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Features.Intents;

/// <summary>
/// Defines the text changes needed to apply an intent.
/// </summary>
internal readonly struct IntentProcessorResult(Solution solution, ImmutableArray<DocumentId> changedDocuments, string title, string actionName)
{
    /// <summary>
    /// The changed solution for this intent result.
    /// </summary>
    public readonly Solution Solution = solution;

    /// <summary>
    /// The set of documents that have changed for this intent result.
    /// </summary>
    public readonly ImmutableArray<DocumentId> ChangedDocuments = changedDocuments;

    /// <summary>
    /// The title associated with this intent result.
    /// </summary>
    public readonly string Title = title ?? throw new ArgumentNullException(nameof(title));

    /// <summary>
    /// Contains metadata that can be used to identify the kind of sub-action these edits
    /// apply to for the requested intent.
    /// </summary>
    public readonly string ActionName = actionName ?? throw new ArgumentNullException(nameof(actionName));
}
