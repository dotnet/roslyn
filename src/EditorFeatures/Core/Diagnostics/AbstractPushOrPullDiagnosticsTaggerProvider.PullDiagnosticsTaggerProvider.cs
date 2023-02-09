// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal abstract partial class AbstractPushOrPullDiagnosticsTaggerProvider<TTag> where TTag : ITag
{
    /// <summary>
    /// Base type for all taggers that interact with the <see cref="IDiagnosticAnalyzerService"/> and produce tags for the
    /// diagnostics with different UI presentations.  It does no computation work itself, but instead defers that to it's
    /// underlying <see cref="SingleDiagnosticKindPullTaggerProvider"/>s.
    /// </summary>
    private sealed partial class PullDiagnosticsTaggerProvider : ITaggerProvider
    {
        /// <summary>
        /// Underlying diagnostic tagger responsible for the syntax/semantic and compiler/analyzer split.  The ordering of
        /// these taggers is not relevant.  They are not executed serially.  Rather, they all run concurrently, notifying us
        /// (potentially concurrently as well) when change occur.
        /// </summary>
        private readonly ImmutableArray<SingleDiagnosticKindPullTaggerProvider> _diagnosticsTaggerProviders;

        public PullDiagnosticsTaggerProvider(
            AbstractPushOrPullDiagnosticsTaggerProvider<TTag> callback,
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            IDiagnosticAnalyzerService analyzerService,
            IGlobalOptionService globalOptions,
            ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListener listener)
        {
            _diagnosticsTaggerProviders = ImmutableArray.Create(
                CreateDiagnosticsTaggerProvider(DiagnosticKind.CompilerSyntax),
                CreateDiagnosticsTaggerProvider(DiagnosticKind.CompilerSemantic),
                CreateDiagnosticsTaggerProvider(DiagnosticKind.AnalyzerSyntax),
                CreateDiagnosticsTaggerProvider(DiagnosticKind.AnalyzerSemantic));

            return;

            SingleDiagnosticKindPullTaggerProvider CreateDiagnosticsTaggerProvider(DiagnosticKind diagnosticKind)
                => new(callback, diagnosticKind, threadingContext, diagnosticService, analyzerService, globalOptions, visibilityTracker, listener);
        }

        public ITagger<T>? CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            using var _ = ArrayBuilder<ITagger<TTag>>.GetInstance(out var taggers);
            foreach (var taggerProvider in _diagnosticsTaggerProviders)
                taggers.AddIfNotNull(taggerProvider.CreateTagger<TTag>(buffer));

            var tagger = new AggregateTagger(taggers.ToImmutable());
            if (tagger is not ITagger<T> genericTagger)
            {
                tagger.Dispose();
                return null;
            }

            return genericTagger;
        }
    }
}
