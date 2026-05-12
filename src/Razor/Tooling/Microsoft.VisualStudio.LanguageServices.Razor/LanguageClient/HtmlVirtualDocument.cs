// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal class HtmlVirtualDocument(Uri uri, ITextBuffer textBuffer, ITelemetryReporter telemetryReporter)
    : GeneratedVirtualDocument<HtmlVirtualDocumentSnapshot>(uri, textBuffer, telemetryReporter)
{
    protected override HtmlVirtualDocumentSnapshot GetUpdatedSnapshot(object? state) => new(Uri, TextBuffer.CurrentSnapshot, HostDocumentVersion, state);
}
