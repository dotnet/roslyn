// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Razor.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Options;

[Export(typeof(OptionsStorage))]
[Export(typeof(IAdvancedSettingsStorage))]
internal class OptionsStorage : IAdvancedSettingsStorage, IDisposable
{
    private readonly JoinableTask _initializeTask;
    private ImmutableArray<string> _taskListDescriptors = [];
    private ISettingsReader? _unifiedSettingsReader;
    private IDisposable? _unifiedSettingsSubscription;
    private bool _changedBeforeSubscription;

    [ImportingConstructor]
    public OptionsStorage(
        SVsServiceProvider synchronousServiceProvider,
        [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider serviceProvider,
        Lazy<ITelemetryReporter> telemetryReporter,
        JoinableTaskContext joinableTaskContext)
    {
        _initializeTask = joinableTaskContext.Factory.RunAsync(async () =>
        {
            var unifiedSettingsManager = await serviceProvider.GetServiceAsync<SVsUnifiedSettingsManager, ISettingsManager>();
            _unifiedSettingsReader = unifiedSettingsManager.GetReader();
            _unifiedSettingsSubscription = _unifiedSettingsReader.SubscribeToChanges(OnUnifiedSettingsChanged, SettingsNames.AllSettings);

            await GetTaskListDescriptorsAsync(joinableTaskContext.Factory, serviceProvider);
        });

        // NotifyChange waits for the initialize task to be finished, but we still want to notify once we've
        // done loading, so do it in a background continuation.
        _initializeTask.Task.ContinueWith(t =>
        {
            NotifyChange();
        }, TaskScheduler.Default).Forget();
    }

    private async Task GetTaskListDescriptorsAsync(JoinableTaskFactory jtf, IAsyncServiceProvider serviceProvider)
    {
        await jtf.SwitchToMainThreadAsync();

        var taskListService = await serviceProvider.GetServiceAsync<IVsTaskList, IVsCommentTaskInfo>();
        if (taskListService is null)
        {
            return;
        }

        // Not sure why, but the VS Threading analyzer isn't recognizing that we switched to the main thread, above.
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
        ErrorHandler.ThrowOnFailure(taskListService.TokenCount(out var count));
        var tokens = new IVsCommentTaskToken[count];
        ErrorHandler.ThrowOnFailure(taskListService.EnumTokens(out var enumerator));
        ErrorHandler.ThrowOnFailure(enumerator.Next((uint)count, tokens, out var numFetched));

        using var tokensBuilder = new PooledArrayBuilder<string>(capacity: (int)numFetched);
        for (var i = 0; i < numFetched; i++)
        {
            tokens[i].Text(out var text);
            tokensBuilder.Add(text);
        }
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread

        _taskListDescriptors = tokensBuilder.ToImmutable();
    }

    public async Task OnChangedAsync(Action<ClientAdvancedSettings> changed)
    {
        await _initializeTask.JoinAsync();

        _changed += (_, args) => changed(args.Settings);

        // Since initialize happens async, we don't want our subscribers to miss the initial update, so trigger it now, since we know
        // initialization is done.
        if (_changedBeforeSubscription)
        {
            changed(GetAdvancedSettings());
        }
    }

    private EventHandler<ClientAdvancedSettingsChangedEventArgs>? _changed;

    public ClientAdvancedSettings GetAdvancedSettings()
        => new(
            GetBool(SettingsNames.FormatOnType, defaultValue: true),
            GetBool(SettingsNames.AutoClosingTags, defaultValue: true),
            GetBool(SettingsNames.AutoInsertAttributeQuotes, defaultValue: true),
            GetBool(SettingsNames.ColorBackground, defaultValue: false),
            GetBool(SettingsNames.CodeBlockBraceOnNextLine, defaultValue: false),
            GetEnum(SettingsNames.AttributeIndentStyle, AttributeIndentStyle.AlignWithFirst),
            GetBool(SettingsNames.CommitElementsWithSpace, defaultValue: true),
            GetEnum(SettingsNames.Snippets, SnippetSetting.All),
            GetEnum(SettingsNames.LogLevel, LogLevel.Warning),
            GetBool(SettingsNames.FormatOnPaste, defaultValue: true),
            _taskListDescriptors);

    public bool GetBool(string name, bool defaultValue)
    {
        if (_unifiedSettingsReader.AssumeNotNull().GetValue<bool>(name) is { Outcome: SettingRetrievalOutcome.Success, Value: { } unifiedValue })
        {
            return unifiedValue;
        }

        return defaultValue;
    }

    public T GetEnum<T>(string name, T defaultValue) where T : struct, Enum
    {
        if (_unifiedSettingsReader.AssumeNotNull().GetValue<string>(name) is { Outcome: SettingRetrievalOutcome.Success, Value: { } unifiedValue })
        {
            if (Enum.TryParse<T>(unifiedValue, ignoreCase: true, out var parsed))
            {
                return parsed;
            }
        }

        return defaultValue;
    }

    private void NotifyChange()
    {
        _initializeTask.Join();

        if (_changed is null)
        {
            _changedBeforeSubscription = true;
        }
        else
        {
            _changed?.Invoke(this, new ClientAdvancedSettingsChangedEventArgs(GetAdvancedSettings()));
        }
    }

    private void OnUnifiedSettingsChanged(SettingsUpdate update)
    {
        NotifyChange();
    }

    public void Dispose()
    {
        _unifiedSettingsSubscription?.Dispose();
    }
}
