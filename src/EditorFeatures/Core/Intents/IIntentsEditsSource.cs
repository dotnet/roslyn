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
    internal interface IIntentsEditsSource
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
    internal class IntentRequestContext
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
        /// The text in the document before changes were made triggering an intent.
        /// <remarks>
        /// For example, if the text typed was 'public Class(' which led to a generate ctor intent,
        /// we expect a snapshot before any of the ctor was typed.
        /// </remarks>
        /// </summary>
        public ITextSnapshot SnapshotBeforeIntent { get; }

        /// <summary>
        /// The original caret position / selection in the <see cref="SnapshotBeforeIntent"/>
        /// </summary>
        public Span Selection { get; }

        /// <summary>
        /// The text snapshot when <see cref="IIntentsEditsSource.ComputeEditsAsync"/>
        /// was called to compute the text edits and against which the resulting text edits will be calculated.
        /// </summary>
        public ITextSnapshot CurrentSnapshot { get; }

        public IntentRequestContext(string intent, ITextSnapshot snapshotBeforeIntent, Span selection, ITextSnapshot currentSnapshot, string? intentData)
        {
            IntentName = intent ?? throw new ArgumentNullException(nameof(intent));
            IntentData = intentData;
            SnapshotBeforeIntent = snapshotBeforeIntent ?? throw new ArgumentNullException(nameof(snapshotBeforeIntent));
            Selection = selection;
            CurrentSnapshot = currentSnapshot ?? throw new ArgumentNullException(nameof(currentSnapshot));
        }
    }

    /// <summary>
    /// Defines the text changes needed to apply an intent.
    /// </summary>
    internal struct IntentResult
    {
        /// <summary>
        /// The text changes that should be applied to the <see cref="IntentRequestContext.CurrentSnapshot"/>
        /// </summary>
        public readonly ImmutableArray<TextChange> TextChanges;

        public IntentResult(ImmutableArray<TextChange> textChanges)
        {
            TextChanges = textChanges;
        }
    }
}
