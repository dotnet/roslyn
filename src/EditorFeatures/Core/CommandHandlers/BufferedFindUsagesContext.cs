// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers;

/// <summary>
/// An impl of <see cref="IFindUsagesContext"/> that will buffer results internally to either be shown to the 
/// user immediately if the find command completes quickly, or which will be pushed into the streaming presenter 
/// if the search is taking too long.
/// </summary>
internal sealed class BufferedFindUsagesContext : IFindUsagesContext, IStreamingProgressTracker
{
    /// <summary>
    /// Lock which controls access to all members below.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(initialCount: 1);

    /// <summary>
    /// The underlying presenter context to forward messages to once the presenter is opened.  Prior to having 
    /// this, we will buffer the results within ourselves.
    /// </summary>
    private IFindUsagesContext? _streamingPresenterContext;

    // Values we buffer inside ourselves until _streamingPresenterContext is non-null.  Once non-null, we'll push
    // the values into it and forward all future calls from that point to it.

    private int _totalItemCount;
    private int _itemsCompleted;

    private string? _message;
    private string? _informationalMessage;
    private string? _searchTitle;

    private ImmutableArray<DefinitionItem>.Builder? _definitions = ImmutableArray.CreateBuilder<DefinitionItem>();

    public BufferedFindUsagesContext()
    {
    }

    public async Task<string?> GetMessageAsync(CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            Contract.ThrowIfTrue(_streamingPresenterContext != null, "Should not be called if we've switched over to the streaming presenter");
            return _message;
        }
    }

    public async Task<string?> GetInformationalMessageAsync(CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            Contract.ThrowIfTrue(_streamingPresenterContext != null, "Should not be called if we've switched over to the streaming presenter");
            return _informationalMessage;
        }
    }

    public async Task<string?> GetSearchTitleAsync(CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            Contract.ThrowIfTrue(_streamingPresenterContext != null, "Should not be called if we've switched over to the streaming presenter");
            return _searchTitle;
        }
    }

    public async Task<ImmutableArray<DefinitionItem>> GetDefinitionsAsync(CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            Contract.ThrowIfNull(_definitions, "This should not be called if we switched over to the presenter to show results");
            return _definitions.ToImmutable();
        }
    }

    public async Task AttachToStreamingPresenterAsync(IFindUsagesContext presenterContext, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            Contract.ThrowIfTrue(_streamingPresenterContext != null, "Trying to set the presenter multiple times.");

            // Push all values we've buffered into the new presenter context.

            await presenterContext.ProgressTracker.AddItemsAsync(_totalItemCount, cancellationToken).ConfigureAwait(false);
            await presenterContext.ProgressTracker.ItemsCompletedAsync(_itemsCompleted, cancellationToken).ConfigureAwait(false);

            if (_searchTitle != null)
                await presenterContext.SetSearchTitleAsync(_searchTitle, cancellationToken).ConfigureAwait(false);

            if (_message != null)
                await presenterContext.ReportMessageAsync(_message, cancellationToken).ConfigureAwait(false);

            if (_informationalMessage != null)
                await presenterContext.ReportInformationalMessageAsync(_informationalMessage, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfNull(_definitions);
            foreach (var definition in _definitions)
                await presenterContext.OnDefinitionFoundAsync(definition, cancellationToken).ConfigureAwait(false);

            // Now swap over to the presenter being the sink for all future callbacks, and clear any buffered data.
            _streamingPresenterContext = presenterContext;

            _totalItemCount = -1;
            _itemsCompleted = -1;
            _searchTitle = null;
            _message = null;
            _informationalMessage = null;
            _definitions = null;
        }
    }

    #region IStreamingProgressTracker

    IStreamingProgressTracker IFindUsagesContext.ProgressTracker => this;

    async ValueTask IStreamingProgressTracker.AddItemsAsync(int count, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_streamingPresenterContext != null)
            {
                await _streamingPresenterContext.ProgressTracker.AddItemsAsync(count, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _totalItemCount += count;
            }
        }
    }

    async ValueTask IStreamingProgressTracker.ItemsCompletedAsync(int count, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_streamingPresenterContext != null)
            {
                await _streamingPresenterContext.ProgressTracker.ItemsCompletedAsync(count, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _itemsCompleted += count;
            }
        }
    }

    #endregion

    #region IFindUsagesContext

    async ValueTask IFindUsagesContext.ReportMessageAsync(string message, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_streamingPresenterContext != null)
            {
                await _streamingPresenterContext.ReportMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _message = message;
            }
        }
    }

    async ValueTask IFindUsagesContext.ReportInformationalMessageAsync(string message, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_streamingPresenterContext != null)
            {
                await _streamingPresenterContext.ReportInformationalMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _informationalMessage = message;
            }
        }
    }

    async ValueTask IFindUsagesContext.SetSearchTitleAsync(string title, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_streamingPresenterContext != null)
            {
                await _streamingPresenterContext.SetSearchTitleAsync(title, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _searchTitle = title;
            }
        }
    }

    async ValueTask IFindUsagesContext.OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_streamingPresenterContext != null)
            {
                await _streamingPresenterContext.OnDefinitionFoundAsync(definition, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Contract.ThrowIfNull(_definitions);
                _definitions.Add(definition);
            }
        }
    }

    ValueTask IFindUsagesContext.OnReferenceFoundAsync(SourceReferenceItem reference, CancellationToken cancellationToken)
    {
        // Entirely ignored.  These features do not show references.
        return ValueTaskFactory.CompletedTask;
    }

    #endregion
}
