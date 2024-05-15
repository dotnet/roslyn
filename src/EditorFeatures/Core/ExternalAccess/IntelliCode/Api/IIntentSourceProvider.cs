// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.IntelliCode.Api;

internal interface IIntentSourceProvider
{
    /// <summary>
    /// For an input intent, computes the edits required to apply that intent and returns them.
    /// </summary>
    /// <param name="context">the intents with the context in which the intent was found.</param>
    /// <returns>the edits that should be applied to the current snapshot.</returns>
    Task<ImmutableArray<IntentSource>> ComputeIntentsAsync(IntentRequestContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the data needed to compute the code action edits from an intent.
/// </summary>
internal readonly struct IntentRequestContext(string intentName, SnapshotSpan currentSnapshotSpan, ImmutableArray<TextChange> textEditsToPrior, TextSpan priorSelection, string? intentData)
{
    /// <summary>
    /// The intent name.  <see cref="WellKnownIntents"/> contains all intents roslyn knows how to handle.
    /// </summary>
    public string IntentName { get; } = intentName ?? throw new ArgumentNullException(nameof(intentName));

    /// <summary>
    /// JSON formatted data specific to the intent that must be deserialized into the appropriate object.
    /// </summary>
    public string? IntentData { get; } = intentData;

    /// <summary>
    /// The text snapshot and selection when <see cref="IIntentSourceProvider.ComputeIntentsAsync"/>
    /// was called to compute the text edits and against which the resulting text edits will be calculated.
    /// </summary>
    public SnapshotSpan CurrentSnapshotSpan { get; } = currentSnapshotSpan;

    /// <summary>
    /// The text edits that should be applied to the <see cref="CurrentSnapshotSpan"/> to calculate
    /// a prior text snapshot before the intent happened.  The snapshot is used to calculate the actions.
    /// </summary>
    public ImmutableArray<TextChange> PriorTextEdits { get; } = textEditsToPrior;

    /// <summary>
    /// The caret position / selection in the snapshot calculated by applying
    /// <see cref="PriorTextEdits"/> to the <see cref="CurrentSnapshotSpan"/>
    /// </summary>
    public TextSpan PriorSelection { get; } = priorSelection;
}

/// <summary>
/// Defines the text changes needed to apply an intent.
/// </summary>
internal readonly struct IntentSource(string title, string actionName, ImmutableDictionary<DocumentId, ImmutableArray<TextChange>> documentChanges)
{
    /// <summary>
    /// The title associated with this intent result.
    /// </summary>
    public readonly string Title { get; } = title ?? throw new ArgumentNullException(nameof(title));

    /// <summary>
    /// The text changes that should be applied to each document.
    /// </summary>
    public readonly ImmutableDictionary<DocumentId, ImmutableArray<TextChange>> DocumentChanges = documentChanges;

    /// <summary>
    /// Contains metadata that can be used to identify the kind of sub-action these edits
    /// apply to for the requested intent.  Used for telemetry purposes only.
    /// For example, the code action type name like FieldDelegatingCodeAction.
    /// </summary>
    public readonly string ActionName { get; } = actionName ?? throw new ArgumentNullException(nameof(actionName));
}
