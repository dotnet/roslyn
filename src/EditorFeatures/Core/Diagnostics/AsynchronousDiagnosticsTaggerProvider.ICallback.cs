// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class AsynchronousDiagnosticsTaggerProvider<TTag>
{
    /// <summary>
    /// Callback a particular <see cref="AsynchronousDiagnosticsTaggerProvider{TTag}"/> needs to perform some work.
    /// This callback behaves the same regardless of the particular <see cref="DiagnosticKinds"/> the tagger is
    /// computing tags for.
    /// </summary>
    public interface ICallback
    {
        ImmutableArray<IOption> Options { get; }
        ImmutableArray<IOption> FeatureOptions { get; }

        bool IsEnabled { get; }
        bool SupportsDiagnosticMode(DiagnosticMode mode);
        bool IncludeDiagnostic(DiagnosticData data);

        bool TagEquals(TTag tag1, TTag tag2);

        /// <summary>
        /// Get the <see cref="DiagnosticDataLocation"/> that should have the tag applied to it.
        /// In most cases, this is the <see cref="DiagnosticData.DataLocation"/> but overrides can change it (e.g. unnecessary classifications).
        /// </summary>
        /// <param name="diagnosticData">the diagnostic containing the location(s).</param>
        /// <returns>an array of locations that should have the tag applied.</returns>
        ImmutableArray<DiagnosticDataLocation> GetLocationsToTag(DiagnosticData diagnosticData);

        ITagSpan<TTag>? CreateTagSpan(Workspace workspace, SnapshotSpan span, DiagnosticData data);
    }
}
