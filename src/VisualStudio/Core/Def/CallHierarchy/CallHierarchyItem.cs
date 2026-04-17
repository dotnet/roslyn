// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.CodeAnalysis.CallHierarchy;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

internal readonly record struct CallHierarchySearchCategoryEntry(
    CallHierarchySearchDescriptor SearchDescriptor,
    string SearchCategory,
    string DisplayName);

internal sealed class CallHierarchyItem : ICallHierarchyMemberItem
{
    private readonly Workspace _workspace;
    private readonly INavigableLocation _navigableLocation;
    private readonly ImmutableArray<CallHierarchyDetail> _callsites;
    private readonly ImmutableArray<CallHierarchySearchCategoryEntry> _searchCategories;
    private readonly Dictionary<string, CancellationTokenSource> _searches = [];
    private readonly Func<ImageSource> _glyphCreator;
    private readonly CallHierarchyProvider _provider;

    private readonly object _gate = new();

    public CallHierarchyItem(
        CallHierarchyProvider provider,
        CallHierarchyItemDescriptor descriptor,
        INavigableLocation navigableLocation,
        ImmutableArray<CallHierarchySearchCategoryEntry> searchCategories,
        Func<ImageSource> glyphCreator,
        string sortText,
        string projectName,
        ImmutableArray<Location> callsites,
        Project project)
    {
        _workspace = project.Solution.Workspace;
        _provider = provider;
        _navigableLocation = navigableLocation;
        _searchCategories = searchCategories;
        ContainingTypeName = descriptor.ContainingTypeName;
        ContainingNamespaceName = descriptor.ContainingNamespaceName;
        _glyphCreator = glyphCreator;
        MemberName = descriptor.MemberName;
        _callsites = callsites.SelectAsArray(loc => new CallHierarchyDetail(provider, loc, _workspace));
        SortText = sortText;
        ProjectName = projectName;
    }

    public string ProjectName { get; }

    public string ContainingNamespaceName { get; }

    public string ContainingTypeName { get; }

    public IEnumerable<ICallHierarchyItemDetails> Details => _callsites;

    public ImageSource DisplayGlyph
    {
        get
        {
            return _glyphCreator();
        }
    }

    public string MemberName { get; }

    public string NameSeparator => ".";

    public string SortText { get; }

    public IEnumerable<CallHierarchySearchCategory> SupportedSearchCategories
    {
        get
        {
            return _searchCategories.Select(s => new CallHierarchySearchCategory(s.SearchCategory, s.DisplayName));
        }
    }

    public bool SupportsFindReferences
            // TODO: Use Dustin's find-references-from-symbol service.
            => false;

    public bool SupportsNavigateTo => true;

    public bool Valid => true;

    public void CancelSearch(string categoryName)
    {
        lock (_gate)
        {
            CancelSearch_NoLock(categoryName);
        }
    }

    private void CancelSearch_NoLock(string categoryName)
    {
        if (_searches.TryGetValue(categoryName, out var cancellationSource))
            cancellationSource.Cancel();
    }

    public void FindReferences()
    {
    }

    public void ItemSelected()
    {
    }

    public void NavigateTo()
    {
        var token = _provider.AsyncListener.BeginAsyncOperation(nameof(NavigateTo));
        NavigateToAsync().ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
    }

    private async Task NavigateToAsync()
    {
        using var context = _provider.ThreadOperationExecutor.BeginExecute(
            EditorFeaturesResources.Call_Hierarchy, ServicesVSResources.Navigating, allowCancellation: true, showProgress: false);
        await _navigableLocation.NavigateToAsync(
            NavigationOptions.Default with { PreferProvisionalTab = true }, context.UserCancellationToken).ConfigureAwait(false);
    }

    public void StartSearch(string categoryName, CallHierarchySearchScope searchScope, ICallHierarchySearchCallback callback)
        => StartSearchWorker(categoryName, searchScope, callback, documents: null);

    public void ResumeSearch(string categoryName)
    {
        // Do nothing.
    }

    public void SuspendSearch(string categoryName)
    {
        // Just cancel.
        CancelSearch(categoryName);
    }

    // For Testing only
    internal void StartSearchWithDocuments(string categoryName, CallHierarchySearchScope searchScope, ICallHierarchySearchCallback callback, IImmutableSet<Document> documents)
        => StartSearchWorker(categoryName, searchScope, callback, documents);

    /// <summary>
    /// Starts a search operation for the given category.
    /// 
    /// Threading guarantees:
    /// - This method is called on the UI thread by VS.
    /// - Concurrent calls with different categoryNames are safe; each category has its own cancellation source.
    /// - Concurrent calls with the same categoryName will cancel the previous search.
    /// - The actual search work is offloaded to a background thread via Task.Run to avoid blocking the UI thread.
    /// - Callbacks are invoked on the background thread; the callback implementation must handle thread safety.
    /// </summary>
    private void StartSearchWorker(string categoryName, CallHierarchySearchScope searchScope, ICallHierarchySearchCallback callback, IImmutableSet<Document> documents)
    {
        var searchCategory = _searchCategories.FirstOrDefault(s => s.SearchCategory == categoryName);
        if (searchCategory == default)
            return;

        CancellationTokenSource cancellationSource;
        lock (_gate)
        {
            CancelSearch_NoLock(categoryName);

            cancellationSource = new();
            _searches[categoryName] = cancellationSource;
        }

        var asyncToken = _provider.AsyncListener.BeginAsyncOperation(this.GetType().Name + ".Search");

        // NOTE: This task has CancellationToken.None specified, since it must complete no matter what
        // so the callback is appropriately notified that the search has terminated.
        Task.Run(async () =>
        {
            string completionErrorMessage = null;
            try
            {
                var results = await _provider.SearchAsync(
                    _workspace, searchCategory.SearchDescriptor, searchScope, documents, cancellationSource.Token).ConfigureAwait(false);
                foreach (var result in results)
                {
                    if (result.Item != null)
                    {
                        var item = await _provider.CreateItemAsync(result.Item, _workspace, result.ReferenceLocations, cancellationSource.Token).ConfigureAwait(false);
                        callback.AddResult(item);
                    }
                    else
                    {
                        var details = result.ReferenceLocations.SelectAsArray(loc => new CallHierarchyDetail(_provider, loc, _workspace));
                        callback.AddResult(_provider.CreateInitializerItem(details));
                    }

                    cancellationSource.Token.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                completionErrorMessage = EditorFeaturesResources.Canceled;
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                completionErrorMessage = e.Message;
            }
            finally
            {
                lock (_gate)
                {
                    _searches.Remove(categoryName);
                }

                if (completionErrorMessage != null)
                {
                    callback.SearchFailed(completionErrorMessage);
                }
                else
                {
                    callback.SearchSucceeded();
                }
            }
        }, CancellationToken.None).CompletesAsyncOperation(asyncToken);
    }
}
