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
using Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy.Finders;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy
{
    internal class CallHierarchyItem : ICallHierarchyMemberItem
    {
        private readonly string _containingNamespaceName;
        private readonly string _containingTypeName;
        private readonly SymbolKey _symbolId;
        private readonly IEnumerable<CallHierarchyDetail> _callsites;
        private readonly IEnumerable<AbstractCallFinder> _finders;
        private readonly Func<ImageSource> _glyphCreator;
        private readonly string _name;
        private readonly CallHierarchyProvider _provider;
        private readonly ProjectId _projectId;
        private readonly string _sortText;

        public CallHierarchyItem(
            CallHierarchyProvider provider,
            ISymbol symbol,
            ProjectId projectId,
            IEnumerable<AbstractCallFinder> finders,
            Func<ImageSource> glyphCreator,
            ImmutableArray<Location> callsites,
            Workspace workspace)
        {
            _provider = provider;
            _symbolId = symbol.GetSymbolKey();
            _projectId = projectId;
            _finders = finders;
            _containingTypeName = symbol.ContainingType.ToDisplayString(ContainingTypeFormat);
            _containingNamespaceName = symbol.ContainingNamespace.ToDisplayString(ContainingNamespaceFormat);
            _glyphCreator = glyphCreator;
            _name = symbol.ToDisplayString(MemberNameFormat);
            _callsites = callsites.SelectAsArray(loc => new CallHierarchyDetail(provider, loc, workspace));
            _sortText = symbol.ToDisplayString();
            _workspace = workspace;
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
        private readonly Workspace _workspace;

        internal Project Project
        {
            get
            {
                return _workspace.CurrentSolution.GetProject(_projectId);
            }
        }

        public string ContainingNamespaceName => _containingNamespaceName;

        public string ContainingTypeName => _containingTypeName;

        public IEnumerable<ICallHierarchyItemDetails> Details => _callsites;

        public ImageSource DisplayGlyph
        {
            get
            {
                return _glyphCreator();
            }
        }

        public string MemberName => _name;

        public string NameSeparator => ".";

        public string SortText => _sortText;

        public IEnumerable<CallHierarchySearchCategory> SupportedSearchCategories
        {
            get
            {
                return _finders.Select(s => new CallHierarchySearchCategory(s.SearchCategory, s.DisplayName));
            }
        }

        public bool SupportsFindReferences =>
                // TODO: Use Dustin's find-references-from-symbol service.
                false;

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
            await _provider.NavigateToAsync(
                _symbolId, _workspace.CurrentSolution.GetProject(_projectId), context.UserCancellationToken).ConfigureAwait(false);
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
}
