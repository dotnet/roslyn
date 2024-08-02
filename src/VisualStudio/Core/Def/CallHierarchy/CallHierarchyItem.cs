// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy.Finders;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

internal class CallHierarchyItem : ICallHierarchyMemberItem
{
    private readonly Workspace _workspace;
    private readonly string _containingTypeName;
    private readonly INavigableLocation _navigableLocation;
    private readonly IEnumerable<CallHierarchyDetail> _callsites;
    private readonly IEnumerable<AbstractCallFinder> _finders;
    private readonly Func<ImageSource> _glyphCreator;
    private readonly CallHierarchyProvider _provider;

    public CallHierarchyItem(
        CallHierarchyProvider provider,
        ISymbol symbol,
        INavigableLocation navigableLocation,
        IEnumerable<AbstractCallFinder> finders,
        Func<ImageSource> glyphCreator,
        ImmutableArray<Location> callsites,
        Project project)
    {
        _workspace = project.Solution.Workspace;
        _provider = provider;
        _navigableLocation = navigableLocation;
        _finders = finders;
        _containingTypeName = symbol.ContainingType.ToDisplayString(ContainingTypeFormat);
        ContainingNamespaceName = symbol.ContainingNamespace.ToDisplayString(ContainingNamespaceFormat);
        _glyphCreator = glyphCreator;
        MemberName = symbol.ToDisplayString(MemberNameFormat);
        _callsites = callsites.SelectAsArray(loc => new CallHierarchyDetail(provider, loc, _workspace));
        SortText = symbol.ToDisplayString();
        ProjectName = project.Name;
    }

    public static readonly SymbolDisplayFormat MemberNameFormat =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
            parameterOptions:
                SymbolDisplayParameterOptions.IncludeParamsRefOut |
                SymbolDisplayParameterOptions.IncludeExtensionThis |
                SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static readonly SymbolDisplayFormat ContainingTypeFormat =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static readonly SymbolDisplayFormat ContainingNamespaceFormat =
       new(
           globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
           typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public string ProjectName { get; }

    public string ContainingNamespaceName { get; }

    public string ContainingTypeName => _containingTypeName;

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
            return _finders.Select(s => new CallHierarchySearchCategory(s.SearchCategory, s.DisplayName));
        }
    }

    public bool SupportsFindReferences
            // TODO: Use Dustin's find-references-from-symbol service.
            => false;

    public bool SupportsNavigateTo => true;

    public bool Valid => true;

    public void CancelSearch(string categoryName)
    {
        var finder = _finders.FirstOrDefault(s => s.SearchCategory == categoryName);
        finder.CancelSearch();
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
            ServicesVSResources.Call_Hierarchy, ServicesVSResources.Navigating, allowCancellation: true, showProgress: false);
        await _navigableLocation.NavigateToAsync(
            NavigationOptions.Default with { PreferProvisionalTab = true }, context.UserCancellationToken).ConfigureAwait(false);
    }

    public void StartSearch(string categoryName, CallHierarchySearchScope searchScope, ICallHierarchySearchCallback callback)
    {
        var finder = _finders.FirstOrDefault(s => s.SearchCategory == categoryName);
        finder.StartSearch(_workspace, searchScope, callback);
    }

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
    {
        var finder = _finders.FirstOrDefault(s => s.SearchCategory == categoryName);
        finder.SetDocuments(documents);
        finder.StartSearch(_workspace, searchScope, callback);
    }
}
