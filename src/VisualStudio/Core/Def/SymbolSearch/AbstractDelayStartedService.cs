// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch;

/// <summary>
/// Base type for services that we want to delay running until certain criteria is met. For example, we don't want
/// to run the <see cref="VisualStudioSymbolSearchService"/> core codepath if the user has not enabled the features
/// that need it.  That helps us avoid loading dlls unnecessarily and bloating the VS memory space.
/// </summary>
internal abstract class AbstractDelayStartedService : ForegroundThreadAffinitizedObject
{
    private readonly IGlobalOptionService _globalOptions;
    protected readonly VisualStudioWorkspaceImpl Workspace;

    /// <summary>
    /// The set of languages that have loaded that care about this service.  If one language loads and has an
    /// appropriate <see cref="_perLanguageOptions"/> also enabled, then this service will start working.
    /// </summary>
    private readonly ConcurrentSet<string> _registeredLanguages = [];

    /// <summary>
    /// Option that controls if this service is enabled or not (regardless of language).
    /// </summary>
    private readonly Option2<bool> _featureEnabledOption;

    /// <summary>
    /// Options that control if this service is enabled or not for a particular language.
    /// </summary>
    private readonly ImmutableArray<PerLanguageOption2<bool>> _perLanguageOptions;

    protected CancellationToken DisposalToken => ThreadingContext.DisposalToken;

    private readonly AsyncBatchingWorkQueue _optionChangedWorkQueue;

    private bool _enabled = false;

    protected AbstractDelayStartedService(
        IThreadingContext threadingContext,
        IGlobalOptionService globalOptions,
        VisualStudioWorkspaceImpl workspace,
        IAsynchronousOperationListenerProvider listenerProvider,
        Option2<bool> featureEnabledOption,
        ImmutableArray<PerLanguageOption2<bool>> perLanguageOptions)
        : base(threadingContext)
    {
        _globalOptions = globalOptions;
        Workspace = workspace;
        _featureEnabledOption = featureEnabledOption;
        _perLanguageOptions = perLanguageOptions;

        _optionChangedWorkQueue = new AsyncBatchingWorkQueue(
            DelayTimeSpan.Medium,
            ProcessOptionChangesAsync,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            this.DisposalToken);
        _globalOptions.AddOptionChangedHandler(this, OnOptionChanged);
    }

    protected abstract Task EnableServiceAsync(CancellationToken cancellationToken);

    public void RegisterLanguage(string language)
    {
        _registeredLanguages.Add(language);
        _optionChangedWorkQueue.AddWork();
    }

    private void OnOptionChanged(object sender, OptionChangedEventArgs e)
        => _optionChangedWorkQueue.AddWork();

    private async ValueTask ProcessOptionChangesAsync(CancellationToken arg)
    {
        // If we're already enabled, nothing to do.
        if (_enabled)
            return;

        // If feature is totally disabled.  Do nothing.
        if (!_globalOptions.GetOption(_featureEnabledOption))
            return;

        // If feature isn't enabled for any registered language, do nothing.
        var languageEnabled = _registeredLanguages.Any(lang => _perLanguageOptions.Any(static (option, arg) => arg.self._globalOptions.GetOption(option, arg.lang), (self: this, lang)));
        if (!languageEnabled)
            return;

        // We were enabled for some language.  Kick off the work for this service now. Since we're now enabled, we
        // no longer need to listen for option changes.
        _enabled = true;
        _globalOptions.RemoveOptionChangedHandler(this, OnOptionChanged);

        // Don't both kicking off delay-started services prior to the actual workspace being fully loaded.  We don't
        // want them using CPU/memory in the BG while we're loading things for the user.
        var statusService = this.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();
        await statusService.WaitUntilFullyLoadedAsync(this.DisposalToken).ConfigureAwait(false);

        await this.EnableServiceAsync(this.DisposalToken).ConfigureAwait(false);
    }
}
