// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages;

/// <summary>
/// An impl of <see cref="IFindUsagesContext"/> that will buffer results internally to either be shown to the 
/// user immediately if the find command completes quickly, or which will be pushed into the streaming presenter 
/// if the search is taking too long.
/// </summary>
internal sealed class BufferedFindUsagesContext : IFindUsagesContext, IStreamingProgressTracker
{
    private class State
    {
        public int TotalItemCount;
        public int ItemsCompleted;
        public string? Message;
        public string? InformationalMessage;
        public string? SearchTitle;
        public ImmutableArray<DefinitionItem>.Builder Definitions = ImmutableArray.CreateBuilder<DefinitionItem>();
    }

    /// <summary>
    /// Lock which controls access to all members below.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(initialCount: 1);

    /// <summary>
    /// The underlying presenter context to forward messages to once the presenter is opened.  Prior to having 
    /// this, we will buffer the results within ourselves.
    /// </summary>
    private IFindUsagesContext? _streamingPresenterContext;

    /// <summary>
    /// Values we buffer inside ourselves until <see cref="_streamingPresenterContext"/> is non-null.  Once non-null,
    /// we'll push the values into it and forward all future calls from that point to it.
    /// </summary> 
    private State? _state = new();

    [MemberNotNullWhen(true, nameof(_streamingPresenterContext))]
    [MemberNotNullWhen(false, nameof(_state))]
    private bool IsSwapped
    {
        get
        {
            Contract.ThrowIfFalse(_gate.CurrentCount == 0);
            return _streamingPresenterContext != null;
        }
    }

    public async Task<string?> GetMessageAsync(CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfTrue(IsSwapped, "Should not be called if we've switched over to the streaming presenter");
        return _state.Message;
    }

    public async Task<string?> GetInformationalMessageAsync(CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfTrue(IsSwapped, "Should not be called if we've switched over to the streaming presenter");
        return _state.InformationalMessage;
    }

    public async Task<string?> GetSearchTitleAsync(CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfTrue(IsSwapped, "Should not be called if we've switched over to the streaming presenter");
        return _state.SearchTitle;
    }

    public async Task<ImmutableArray<DefinitionItem>> GetDefinitionsAsync(CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfTrue(IsSwapped, "Should not be called if we've switched over to the streaming presenter");
        return _state.Definitions.ToImmutable();
    }

    public async Task AttachToStreamingPresenterAsync(IFindUsagesContext presenterContext, CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfTrue(IsSwapped, "Trying to set the presenter multiple times.");

        // Push all values we've buffered into the new presenter context.

        await presenterContext.ProgressTracker.AddItemsAsync(_state.TotalItemCount, cancellationToken).ConfigureAwait(false);
        await presenterContext.ProgressTracker.ItemsCompletedAsync(_state.ItemsCompleted, cancellationToken).ConfigureAwait(false);

        if (_state.SearchTitle != null)
            await presenterContext.SetSearchTitleAsync(_state.SearchTitle, cancellationToken).ConfigureAwait(false);

        if (_state.Message != null)
            await presenterContext.ReportNoResultsAsync(_state.Message, cancellationToken).ConfigureAwait(false);

        if (_state.InformationalMessage != null)
            await presenterContext.ReportMessageAsync(_state.InformationalMessage, NotificationSeverity.Information, cancellationToken).ConfigureAwait(false);

        foreach (var definition in _state.Definitions)
            await presenterContext.OnDefinitionFoundAsync(definition, cancellationToken).ConfigureAwait(false);

        // Now swap over to the presenter being the sink for all future callbacks, and clear any buffered data.
        _streamingPresenterContext = presenterContext;
        _state = null;
    }

    #region IStreamingProgressTracker

    IStreamingProgressTracker IFindUsagesContext.ProgressTracker => this;

    async ValueTask IStreamingProgressTracker.AddItemsAsync(int count, CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        if (IsSwapped)
        {
            await _streamingPresenterContext.ProgressTracker.AddItemsAsync(count, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _state.TotalItemCount += count;
        }
    }

    async ValueTask IStreamingProgressTracker.ItemsCompletedAsync(int count, CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        if (IsSwapped)
        {
            await _streamingPresenterContext.ProgressTracker.ItemsCompletedAsync(count, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _state.ItemsCompleted += count;
        }
    }

    #endregion

    #region IFindUsagesContext

    async ValueTask IFindUsagesContext.ReportNoResultsAsync(string message, CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        if (IsSwapped)
        {
            await _streamingPresenterContext.ReportNoResultsAsync(message, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _state.Message = message;
        }
    }

    async ValueTask IFindUsagesContext.ReportMessageAsync(string message, NotificationSeverity severity, CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        if (IsSwapped)
        {
            await _streamingPresenterContext.ReportMessageAsync(message, severity, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _state.InformationalMessage = message;
        }
    }

    async ValueTask IFindUsagesContext.SetSearchTitleAsync(string title, CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        if (IsSwapped)
        {
            await _streamingPresenterContext.SetSearchTitleAsync(title, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _state.SearchTitle = title;
        }
    }

    async ValueTask IFindUsagesContext.OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
    {
        using var _ = await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        if (IsSwapped)
        {
            await _streamingPresenterContext.OnDefinitionFoundAsync(definition, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _state.Definitions.Add(definition);
        }
    }

    ValueTask IFindUsagesContext.OnReferenceFoundAsync(SourceReferenceItem reference, CancellationToken cancellationToken)
    {
        // Entirely ignored.  These features do not show references.
        Contract.Fail("GoToImpl/Base should never report a reference.");
        return ValueTaskFactory.CompletedTask;
    }

    #endregion
}
