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

namespace Microsoft.CodeAnalysis.Editor.Intents
{
    internal interface IIntentProcessor
    {
        /// <summary>
        /// For an input intent, computes the edits required to apply that intent and returns them.
        /// </summary>
        /// <param name="intentRequestContext">the intents with the context in which the intent was found.</param>
        /// <returns>the edits that should be applied to the current snapshot.</returns>
        Task<IntentResult?> ComputeEditsAsync(IntentRequestContext intentRequestContext, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Defines the data needed to compute the code action edits from an intent.
    /// </summary>
    internal struct IntentRequestContext
    {
        /// <summary>
        /// The intent name.  <see cref="WellKnownIntents"/> contains all intents roslyn knows how to handle.
        /// </summary>
        public string IntentName { get; }

        /// <summary>
        /// JSON formatted data specific to the intent that must be deserialized into the appropriate object.
        /// </summary>
        public string? IntentData { get; }

        /// <summary>
        /// The text and selection in the document before changes were made triggering an intent.
        /// <remarks>
        /// For example, if the text typed was 'public Class(' which led to a generate ctor intent,
        /// we expect a snapshot before any of the ctor was typed.
        /// </remarks>
        /// </summary>
        public SnapshotSpan PriorSnapshotSpan { get; }

        /// <summary>
        /// The text snapshot and selection when <see cref="IIntentProcessor.ComputeEditsAsync"/>
        /// was called to compute the text edits and against which the resulting text edits will be calculated.
        /// </summary>
        public SnapshotSpan CurrentSnapshotSpan { get; }

        public IntentRequestContext(string intentName, SnapshotSpan priorSnapshotSpan, SnapshotSpan currentSnapshotSpan, string? intentData)
        {
            IntentName = intentName ?? throw new ArgumentNullException(nameof(intentName));
            IntentData = intentData;
            PriorSnapshotSpan = priorSnapshotSpan;
            CurrentSnapshotSpan = currentSnapshotSpan;
        }
    }

    /// <summary>
    /// Defines the text changes needed to apply an intent.
    /// </summary>
    internal struct IntentResult
    {
        /// <summary>
        /// The text changes that should be applied to the <see cref="IntentRequestContext.CurrentSnapshotSpan"/>
        /// </summary>
        public readonly ImmutableArray<TextChange> TextChanges;

        /// <summary>
        /// The title associated with this intent result.
        /// </summary>
        public readonly string Title;

        public IntentResult(ImmutableArray<TextChange> textChanges, string title)
        {
            TextChanges = textChanges;
            Title = title;
        }
    }
}
