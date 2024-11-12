// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class DebuggingSessionTelemetry(Guid solutionSessionId)
{
    internal readonly struct Data(DebuggingSessionTelemetry telemetry)
    {
        public readonly Guid SolutionSessionId = telemetry._solutionSessionId;
        public readonly ImmutableArray<EditSessionTelemetry.Data> EditSessionData = [.. telemetry._editSessionData];
        public readonly int EmptyEditSessionCount = telemetry._emptyEditSessionCount;
        public readonly int EmptyHotReloadEditSessionCount = telemetry._emptyHotReloadEditSessionCount;
    }

    private readonly object _guard = new();

    private readonly Guid _solutionSessionId = solutionSessionId;
    private readonly List<EditSessionTelemetry.Data> _editSessionData = [];
    private int _emptyEditSessionCount;
    private int _emptyHotReloadEditSessionCount;

    public Data GetDataAndClear()
    {
        lock (_guard)
        {
            var data = new Data(this);
            _editSessionData.Clear();
            _emptyEditSessionCount = 0;
            _emptyHotReloadEditSessionCount = 0;
            return data;
        }
    }

    public void LogEditSession(EditSessionTelemetry.Data editSessionTelemetryData)
    {
        lock (_guard)
        {
            if (editSessionTelemetryData.IsEmpty)
            {
                if (editSessionTelemetryData.InBreakState)
                    _emptyEditSessionCount++;
                else
                    _emptyHotReloadEditSessionCount++;
            }
            else
            {
                _editSessionData.Add(editSessionTelemetryData);
            }
        }
    }

    // Example query:
    //
    // RawEventsVS
    // | where EventName == "vs/ide/vbcs/debugging/encsession/editsession"
    // | project EventId, EventName, Properties, Measures, MacAddressHash
    // | where Measures["vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerroridcount"] == 0
    // | extend HasValidChanges = Properties["vs.ide.vbcs.debugging.encsession.editsession.hadvalidchanges"] == "True"
    // | where HasValidChanges
    // | extend IsHotReload = Properties["vs.ide.vbcs.debugging.encsession.editsession.inbreakstate"] == "False"
    // | extend IsEnC = not(IsHotReload)
    // | summarize HotReloadUsers = dcountif(MacAddressHash, IsHotReload),
    //             EncUsers = dcountif(MacAddressHash, IsEnC)
    public static void Log(Data data, Action<FunctionId, LogMessage> log, Func<int> getNextId)
    {
        const string SessionId = nameof(SessionId);
        const string EditSessionId = nameof(EditSessionId);

        var debugSessionId = getNextId();

        log(FunctionId.Debugging_EncSession, KeyValueLogMessage.Create(map =>
        {
            map["SolutionSessionId"] = data.SolutionSessionId.ToString("B").ToUpperInvariant();
            map[SessionId] = debugSessionId;
            map["SessionCount"] = data.EditSessionData.Count(session => session.InBreakState);
            map["EmptySessionCount"] = data.EmptyEditSessionCount;
            map["HotReloadSessionCount"] = data.EditSessionData.Count(session => !session.InBreakState);
            map["EmptyHotReloadSessionCount"] = data.EmptyHotReloadEditSessionCount;
        }));

        foreach (var editSessionData in data.EditSessionData)
        {
            var editSessionId = getNextId();

            log(FunctionId.Debugging_EncSession_EditSession, KeyValueLogMessage.Create(map =>
            {
                map[SessionId] = debugSessionId;
                map[EditSessionId] = editSessionId;

                map["HadCompilationErrors"] = editSessionData.HadCompilationErrors;
                map["HadRudeEdits"] = editSessionData.HadRudeEdits;

                // Changes made to source code during the edit session were valid - they were significant and no rude edits were reported.
                // The changes still might fail to emit (see EmitDeltaErrorIdCount).
                map["HadValidChanges"] = editSessionData.HadValidChanges;
                map["HadValidInsignificantChanges"] = editSessionData.HadValidInsignificantChanges;

                map["RudeEditsCount"] = editSessionData.RudeEdits.Length;

                // Number of emit errors.
                map["EmitDeltaErrorIdCount"] = editSessionData.EmitErrorIds.Length;

                // False for Hot Reload session, true or missing for EnC session (missing in older data that did not have this property).
                map["InBreakState"] = editSessionData.InBreakState;

                map["Capabilities"] = (int)editSessionData.Capabilities;

                // Ids of all projects whose binaries were successfully updated during the session.
                map["ProjectIdsWithAppliedChanges"] = editSessionData.Committed ? editSessionData.ProjectsWithValidDelta.Select(ProjectIdToPii) : "";

                // Total milliseconds it took to emit the delta in this edit session.
                map["EmitDifferenceMilliseconds"] = (long)editSessionData.EmitDifferenceTime.TotalMilliseconds;

                // Total milliseconds it took to analyze all documents that contributed to the changes that were
                // attempted to be applied (whether or not the applications was successful) in this edit session.
                // Includes analysis that had been performed asynchronously before "apply changes" was triggered
                // (if we reused analysis results that were calculated by EnC analyzer for rude edit reporting).
                map["TotalAnalysisMilliseconds"] = (long)editSessionData.AnalysisTime.TotalMilliseconds;
            }));

            foreach (var errorId in editSessionData.EmitErrorIds)
            {
                log(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, KeyValueLogMessage.Create(map =>
                {
                    map[SessionId] = debugSessionId;
                    map[EditSessionId] = editSessionId;
                    map["ErrorId"] = errorId;
                }));
            }

            foreach (var (editKind, syntaxKind, projectId) in editSessionData.RudeEdits)
            {
                log(FunctionId.Debugging_EncSession_EditSession_RudeEdit, KeyValueLogMessage.Create(map =>
                {
                    map[SessionId] = debugSessionId;
                    map[EditSessionId] = editSessionId;

                    map["RudeEditKind"] = editKind;
                    map["RudeEditSyntaxKind"] = syntaxKind;
                    map["RudeEditBlocking"] = editSessionData.HadRudeEdits;
                    map["RudeEditProjectId"] = ProjectIdToPii(projectId);
                }));
            }

            static PiiValue ProjectIdToPii(Guid projectId)
                => new(projectId.ToString("B").ToUpperInvariant());
        }
    }
}
