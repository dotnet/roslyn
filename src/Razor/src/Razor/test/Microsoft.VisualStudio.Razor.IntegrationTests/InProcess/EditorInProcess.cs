// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Razor.IntegrationTests.Extensions;
using Microsoft.VisualStudio.Razor.IntegrationTests.InProcess;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal partial class EditorInProcess
{
    public async Task<ITextSnapshot> GetActiveSnapshotAsync(CancellationToken cancellationToken)
        => (await GetActiveTextViewAsync(cancellationToken)).TextSnapshot;

    public async Task ActivateAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        dte.ActiveDocument.Activate();
    }

    public async Task<bool> IsUseSuggestionModeOnAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var textView = await GetActiveTextViewAsync(cancellationToken);

        var subjectBuffer = textView.GetBufferContainingCaret();
        Assumes.Present(subjectBuffer);

        var options = textView.Options.GlobalOptions;
        EditorOptionKey<bool> optionKey;
        bool defaultOption;
        if (IsDebuggerTextView(textView))
        {
            optionKey = new EditorOptionKey<bool>(PredefinedCompletionNames.SuggestionModeInDebuggerCompletionOptionName);
            defaultOption = true;
        }
        else
        {
            optionKey = new EditorOptionKey<bool>(PredefinedCompletionNames.SuggestionModeInCompletionOptionName);
            defaultOption = false;
        }

        if (!options.IsOptionDefined(optionKey, localScopeOnly: false))
        {
            return defaultOption;
        }

        return options.GetOptionValue(optionKey);

        static bool IsDebuggerTextView(IWpfTextView textView)
        {
            return textView.Roles.Contains("DEBUGVIEW");
        }
    }

    public async Task SetUseSuggestionModeAsync(bool value, CancellationToken cancellationToken)
    {
        if (await IsUseSuggestionModeOnAsync(cancellationToken) != value)
        {
            var dispatcher = await GetRequiredGlobalServiceAsync<SUIHostCommandDispatcher, IOleCommandTarget>(cancellationToken);
            ErrorHandler.ThrowOnFailure(dispatcher.Exec(typeof(VSConstants.VSStd2KCmdID).GUID, (uint)VSConstants.VSStd2KCmdID.ToggleConsumeFirstCompletionMode, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero));

            if (await IsUseSuggestionModeOnAsync(cancellationToken) != value)
            {
                throw new InvalidOperationException($"Edit.ToggleCompletionMode did not leave the editor in the expected state.");
            }
        }

        if (!value)
        {
            // For blocking completion mode, make sure we don't have responsive completion interfering when
            // integration tests run slowly.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);
            var options = view.Options.GlobalOptions;
            options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, false);

            var latencyGuardOptionKey = new EditorOptionKey<bool>("EnableTypingLatencyGuard");
            options.SetOptionValue(latencyGuardOptionKey, false);
        }
    }

    public async Task SetAdvancedSettingsAsync(ClientAdvancedSettings clientAdvancedSettings, CancellationToken cancellationToken)
    {
        var clientSettingsManager = await GetComponentModelServiceAsync<IClientSettingsManager>(cancellationToken);

        clientSettingsManager.Update(clientAdvancedSettings);
    }

    public async Task<ImmutableArray<TagSpan<TTag>>> WaitForTagsAsync<TTag>(CancellationToken cancellationToken)
        where TTag : ITag
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        return await Helper.RetryAsync(async ct =>
        {
            var tags = await GetTagsAsync<TTag>(cancellationToken);
            if (tags.Length == 0)
            {
                return default;
            }

            return tags;
        }, TimeSpan.FromMilliseconds(50), cancellationToken);
    }

    public async Task<ImmutableArray<TagSpan<TTag>>> GetTagsAsync<TTag>(CancellationToken cancellationToken)
        where TTag : ITag
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var viewTagAggregatorFactory = await GetComponentModelServiceAsync<IViewTagAggregatorFactoryService>(cancellationToken);

        var aggregator = viewTagAggregatorFactory.CreateTagAggregator<TTag>(view);
        var tags = aggregator
            .GetTags(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length))
            .Cast<IMappingTagSpan<ITag>>();

        return tags.SelectAsArray(tag => (new TagSpan<TTag>(tag.Span.GetSpans(view.TextBuffer).Single(), (TTag)tag.Tag)));
    }
}
