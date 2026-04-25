// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal abstract class GeneratedVirtualDocument<T>(Uri uri, ITextBuffer textBuffer, ITelemetryReporter telemetryReporter) : VirtualDocumentBase<T>(uri, textBuffer) where T : VirtualDocumentSnapshot
{
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;

    public override VirtualDocumentSnapshot Update(IReadOnlyList<ITextChange> changes, int hostDocumentVersion, object? state)
    {
        var currentSnapshotLength = CurrentSnapshot.Snapshot.Length;
        if (state is bool previousWasEmpty &&
            previousWasEmpty != (currentSnapshotLength == 0))
        {
            Debug.Fail($"The language server is sending us changes for what it/we thought was an empty file, but their/our copy is not empty. Generated C# file may have corrupted file contents after this update.");

            var recoverable = false;
            if (previousWasEmpty && changes is [{ OldPosition: 0, OldEnd: 0 } change])
            {
                recoverable = true;
                // The LSP server thought the file was empty, but we have some contents. That's not good, but we can recover
                // by adjusting the range for the change (which would be (0,0)-(0,0) from the LSP server point of view) to
                // cover the whole buffer, essentially just taking the LSP server as the source of truth.
                changes = new[] { new VisualStudioTextChange(0, currentSnapshotLength, change.NewText) };
            }

            _telemetryReporter.ReportEvent(
                "sync", Severity.High,
                new("version", hostDocumentVersion),
                new("type", typeof(T).Name),
                new("recoverable", recoverable));
        }

        return base.Update(changes, hostDocumentVersion, state);
    }
}
