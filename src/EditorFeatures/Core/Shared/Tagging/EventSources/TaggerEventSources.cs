// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense;
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

        public static ITaggerEventSource OnCaretPositionChanged(ITextView textView, ITextBuffer subjectBuffer, TaggerDelay delay)
        {
            return new CaretPositionChangedEventSource(textView, subjectBuffer, delay);
        }

        public static ITaggerEventSource OnCompletionClosed(
            ITextView textView,
            IIntellisenseSessionStack sessionStack,
            TaggerDelay delay)
        {
            return new CompletionClosedEventSource(textView, sessionStack, delay);
        }

        public static ITaggerEventSource OnTextChanged(ITextBuffer subjectBuffer, TaggerDelay delay, bool reportChangedSpans = false)
        {
            Contract.ThrowIfNull(subjectBuffer);

            return new TextChangedEventSource(subjectBuffer, delay, reportChangedSpans);
        }

        public static ITaggerEventSource OnSemanticChanged(ITextBuffer subjectBuffer, TaggerDelay delay, ISemanticChangeNotificationService notificationService)
        {
            return new SemanticChangedEventSource(subjectBuffer, delay, notificationService);
        }

        public static ITaggerEventSource OnDocumentActiveContextChanged(ITextBuffer subjectBuffer, TaggerDelay delay)
        {
            return new DocumentActiveContextChangedEventSource(subjectBuffer, delay);
        }

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
        {
            return new WorkspaceRegistrationChangedEventSource(subjectBuffer, delay);
        }
    }
}
