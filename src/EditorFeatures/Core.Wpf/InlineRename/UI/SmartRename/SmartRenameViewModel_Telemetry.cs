// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename;

internal partial class SmartRenameViewModel
{
    private SuggestionsPanelTelemetry? _suggestionsPanelTelemetry;
    private SuggestionsDropdownTelemetry? _suggestionsDropdownTelemetry;

    private sealed class SuggestionsPanelTelemetry
    {
        public bool CollapseSuggestionsPanelWhenRenameStarts { get; set; }
    }

    private sealed class SuggestionsDropdownTelemetry
    {
        public int DropdownButtonClickTimes { get; set; }
    }

    private void SetupTelemetry()
    {
        var getSuggestionsAutomatically = _globalOptionService.GetOption(InlineRenameUIOptionsStorage.GetSuggestionsAutomatically);
        if (getSuggestionsAutomatically)
        {
            _suggestionsPanelTelemetry = new SuggestionsPanelTelemetry
            {
                CollapseSuggestionsPanelWhenRenameStarts = _globalOptionService.GetOption(InlineRenameUIOptionsStorage.CollapseSuggestionsPanel)
            };
        }
        else
        {
            _suggestionsDropdownTelemetry = new SuggestionsDropdownTelemetry();
        }
    }

    private void PostTelemetry(bool isCommit)
    {
        if (_suggestionsPanelTelemetry is not null)
        {
            RoslynDebug.Assert(_suggestionsDropdownTelemetry is null);
            TelemetryLogging.Log(FunctionId.Copilot_Rename, KeyValueLogMessage.Create(m =>
            {
                m[nameof(isCommit)] = isCommit;
                m["UseSuggestionsPanel"] = true;
                m[nameof(SuggestionsPanelTelemetry.CollapseSuggestionsPanelWhenRenameStarts)] = _suggestionsPanelTelemetry.CollapseSuggestionsPanelWhenRenameStarts;
                m["CollapseSuggestionsPanelWhenRenameEnds"] = _globalOptionService.GetOption(InlineRenameUIOptionsStorage.CollapseSuggestionsPanel);
                m["smartRenameSessionInProgress"] = _smartRenameSession.IsInProgress;
                m["smartRenameCorrelationId"] = _smartRenameSession.CorrelationId;
                m["smartRenameSemanticContextUsed"] = _semanticContextUsed;
                m["smartRenameSemanticContextDelay"] = _semanticContextDelay.TotalMilliseconds;
                m["smartRenameSemanticContextError"] = _semanticContextError;
            }));
        }
        else
        {
            RoslynDebug.Assert(_suggestionsDropdownTelemetry is not null);
            TelemetryLogging.Log(FunctionId.Copilot_Rename, KeyValueLogMessage.Create(m =>
            {
                m[nameof(isCommit)] = isCommit;
                m["UseDropDown"] = true;
                m[nameof(SuggestionsDropdownTelemetry.DropdownButtonClickTimes)] = _suggestionsDropdownTelemetry.DropdownButtonClickTimes;
                m["smartRenameSessionInProgress"] = _smartRenameSession.IsInProgress;
                m["smartRenameCorrelationId"] = _smartRenameSession.CorrelationId;
                m["smartRenameSemanticContextUsed"] = _semanticContextUsed;
                m["smartRenameSemanticContextDelay"] = _semanticContextDelay.TotalMilliseconds;
                m["smartRenameSemanticContextError"] = _semanticContextError;
            }));
        }
    }
}
