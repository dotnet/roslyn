// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
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

        public static ITaggerEventSource OnCaretPositionChanged(ITextView textView, ITextBuffer subjectBuffer)
            => new CaretPositionChangedEventSource(textView, subjectBuffer);

        public static ITaggerEventSource OnTextChanged(ITextBuffer subjectBuffer)
            => new TextChangedEventSource(subjectBuffer);

        [Obsolete("Legacy api for TypeScript.  Can be removed once they call to the overload without TaggerDelay.")]
        public static ITaggerEventSource OnTextChanged(ITextBuffer subjectBuffer, TaggerDelay delay)
            => OnTextChanged(subjectBuffer);

        /// <summary>
        /// Reports an event any time the workspace changes.
        /// </summary>
        public static ITaggerEventSource OnWorkspaceChanged(ITextBuffer subjectBuffer, IAsynchronousOperationListener listener)
            => new WorkspaceChangedEventSource(subjectBuffer, listener);

        [Obsolete("Legacy api for TypeScript.  Can be removed once they call to the overload without TaggerDelay.")]
        public static ITaggerEventSource OnWorkspaceChanged(ITextBuffer subjectBuffer, TaggerDelay delay, IAsynchronousOperationListener listener)
            => OnWorkspaceChanged(subjectBuffer, listener);

        public static ITaggerEventSource OnDocumentActiveContextChanged(ITextBuffer subjectBuffer)
            => new DocumentActiveContextChangedEventSource(subjectBuffer);

        public static ITaggerEventSource OnSelectionChanged(ITextView textView)
            => new SelectionChangedEventSource(textView);

        public static ITaggerEventSource OnReadOnlyRegionsChanged(ITextBuffer subjectBuffer)
            => new ReadOnlyRegionsChangedEventSource(subjectBuffer);

        public static ITaggerEventSource OnOptionChanged(ITextBuffer subjectBuffer, IOption option)
            => new OptionChangedEventSource(subjectBuffer, option);

        public static ITaggerEventSource OnDiagnosticsChanged(ITextBuffer subjectBuffer, IDiagnosticService service)
            => new DiagnosticsChangedEventSource(subjectBuffer, service);

        public static ITaggerEventSource OnParseOptionChanged(ITextBuffer subjectBuffer)
            => new ParseOptionChangedEventSource(subjectBuffer);

        public static ITaggerEventSource OnWorkspaceRegistrationChanged(ITextBuffer subjectBuffer)
            => new WorkspaceRegistrationChangedEventSource(subjectBuffer);

        [Obsolete("Legacy api for TypeScript.  Can be removed once they call to the overload without TaggerDelay.")]
        public static ITaggerEventSource OnWorkspaceRegistrationChanged(ITextBuffer subjectBuffer, TaggerDelay delay)
            => OnWorkspaceRegistrationChanged(subjectBuffer);

        public static ITaggerEventSource OnViewSpanChanged(IThreadingContext threadingContext, ITextView textView)
            => new ViewSpanChangedEventSource(threadingContext, textView);
    }
}
