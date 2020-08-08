// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal static partial class TaggerEventSources
    {
        public static ITaggerEventSource Compose(
            params ITaggerEventSource[] eventSources)
        {
            return new CompositionEventSource(eventSources);
        }

        public static ITaggerEventSource Compose(IEnumerable<ITaggerEventSource> eventSources)
            => new CompositionEventSource(eventSources.ToArray());

        public static ITaggerEventSource OnCaretPositionChanged(ITextView textView, ITextBuffer subjectBuffer, TaggerDelay delay)
            => new CaretPositionChangedEventSource(textView, subjectBuffer, delay);

        public static ITaggerEventSource OnCompletionClosed(
            IIntellisenseSessionStack sessionStack,
            TaggerDelay delay)
        {
            return new CompletionClosedEventSource(sessionStack, delay);
        }

        public static ITaggerEventSource OnTextChanged(ITextBuffer subjectBuffer, TaggerDelay delay)
        {
            Contract.ThrowIfNull(subjectBuffer);

            return new TextChangedEventSource(subjectBuffer, delay);
        }

        /// <summary>
        /// Reports an event any time the workspace changes.
        /// </summary>
        public static ITaggerEventSource OnWorkspaceChanged(
            ITextBuffer subjectBuffer, TaggerDelay delay, IAsynchronousOperationListener listener)
        {
            return new WorkspaceChangedEventSource(subjectBuffer, delay, listener);
        }

        public static ITaggerEventSource OnDocumentActiveContextChanged(ITextBuffer subjectBuffer, TaggerDelay delay)
            => new DocumentActiveContextChangedEventSource(subjectBuffer, delay);

        public static ITaggerEventSource OnSelectionChanged(
            ITextView textView,
            TaggerDelay delay)
        {
            return new SelectionChangedEventSource(textView, delay);
        }

        public static ITaggerEventSource OnReadOnlyRegionsChanged(ITextBuffer subjectBuffer, TaggerDelay delay)
        {
            Contract.ThrowIfNull(subjectBuffer);

            return new ReadOnlyRegionsChangedEventSource(subjectBuffer, delay);
        }

        public static ITaggerEventSource OnOptionChanged(
            ITextBuffer subjectBuffer,
            IOption option,
            TaggerDelay delay)
        {
            return new OptionChangedEventSource(subjectBuffer, option, delay);
        }

        public static ITaggerEventSource OnDiagnosticsChanged(
            ITextBuffer subjectBuffer,
            IDiagnosticService service,
            TaggerDelay delay)
        {
            return new DiagnosticsChangedEventSource(subjectBuffer, service, delay);
        }

        public static ITaggerEventSource OnParseOptionChanged(
            ITextBuffer subjectBuffer,
            TaggerDelay delay)
        {
            return new ParseOptionChangedEventSource(subjectBuffer, delay);
        }

        public static ITaggerEventSource OnWorkspaceRegistrationChanged(ITextBuffer subjectBuffer, TaggerDelay delay)
            => new WorkspaceRegistrationChangedEventSource(subjectBuffer, delay);

        public static ITaggerEventSource OnViewSpanChanged(IThreadingContext threadingContext, ITextView textView, TaggerDelay textChangeDelay, TaggerDelay scrollChangeDelay)
            => new ViewSpanChangedEventSource(threadingContext, textView, textChangeDelay, scrollChangeDelay);
    }
}
