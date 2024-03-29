using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename
{
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
                }));
            }
        }
    }
}
