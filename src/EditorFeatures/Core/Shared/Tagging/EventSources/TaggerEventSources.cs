// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal static partial class TaggerEventSources
{
    public static ITaggerEventSource Compose(
        params ITaggerEventSource[] eventSources)
    {
        return new CompositionEventSource(eventSources);
    }

    public static ITaggerEventSource Compose(IEnumerable<ITaggerEventSource> eventSources)
        => new CompositionEventSource([.. eventSources]);

    public static ITaggerEventSource OnCaretPositionChanged(ITextView textView, ITextBuffer subjectBuffer)
        => new CaretPositionChangedEventSource(textView, subjectBuffer);

    public static ITaggerEventSource OnTextChanged(ITextBuffer subjectBuffer)
        => new TextChangedEventSource(subjectBuffer);

    /// <summary>
    /// Reports an event any time the workspace changes.
    /// </summary>
    public static ITaggerEventSource OnWorkspaceChanged(ITextBuffer subjectBuffer, IAsynchronousOperationListener listener)
        => new WorkspaceChangedEventSource(subjectBuffer, listener);

    public static ITaggerEventSource OnDocumentActiveContextChanged(ITextBuffer subjectBuffer)
        => new DocumentActiveContextChangedEventSource(subjectBuffer);

    public static ITaggerEventSource OnSelectionChanged(ITextView textView)
        => new SelectionChangedEventSource(textView);

    public static ITaggerEventSource OnReadOnlyRegionsChanged(ITextBuffer subjectBuffer)
        => new ReadOnlyRegionsChangedEventSource(subjectBuffer);

    public static ITaggerEventSource OnGlobalOptionChanged(IGlobalOptionService globalOptions, Func<IOption2, bool> predicate)
        => new GlobalOptionChangedEventSource(globalOptions, predicate);

    public static ITaggerEventSource OnParseOptionChanged(ITextBuffer subjectBuffer)
        => new ParseOptionChangedEventSource(subjectBuffer);

    public static ITaggerEventSource OnWorkspaceRegistrationChanged(ITextBuffer subjectBuffer)
        => new WorkspaceRegistrationChangedEventSource(subjectBuffer);

    public static ITaggerEventSource OnViewSpanChanged(IThreadingContext threadingContext, ITextView textView)
        => new ViewSpanChangedEventSource(threadingContext, textView);
}
