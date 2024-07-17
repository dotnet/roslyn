// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.ErrorList;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindUsages;

internal static class RichNavOptions
{
    internal const string RichNavAvailableOptionName = "RichNavAvailable";
}

[Export(typeof(IScopeFilterFactory))]
[TableManagerIdentifier("FindAllReferences*")]
[TableManagerIdentifier("FindResults*")]
[Replaces(PredefinedScopeFilterNames.EntireSolutionScopeFilter)]
[Replaces(PredefinedScopeFilterNames.AllItemsScopeFilter)]
[DeferCreation(OptionName = RichNavOptions.RichNavAvailableOptionName)] // This factory will not be loaded unless this option is set to Boolean true
[DefaultScope]
[Name(PredefinedScopeFilterNames.LoadedSolutionScopeFilter)]
[Order(Before = PredefinedScopeFilterNames.AllItemsScopeFilter)]
internal class LoadedSolutionScopeFilterFactory : IReplacingScopeFilterFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LoadedSolutionScopeFilterFactory()
    {
    }

    public IErrorListFilterHandler CreateFilter(IWpfTableControl tableControl)
    {
        // We're only replacing an existing filter, and not creating a new one.
        return null;
    }

    public IErrorListFilterHandler ReplaceFilter(IWpfTableControl tableControl, string filterIdentifier)
    {
        if (filterIdentifier == PredefinedScopeFilterNames.AllItemsScopeFilter)
        {
            return new LoadedSolutionFilterHandler(ServicesVSResources.Loaded_items);
        }
        else if (filterIdentifier == PredefinedScopeFilterNames.EntireSolutionScopeFilter)
        {
            return new LoadedSolutionFilterHandler(ServicesVSResources.Loaded_solution);
        }

        return null; // Don't replace
    }
}

internal abstract class RoslynFilterHandler : FilterHandlerBase
{
    private readonly ItemOrigin _origin;

    protected RoslynFilterHandler(int id, string displayName, ItemOrigin origin)
    {
        FilterId = id;
        FilterDisplayName = displayName;
        _origin = origin;
    }

    public sealed override int FilterId { get; }

    public sealed override string FilterDisplayName { get; }

    public sealed override IEntryFilter GetFilter(out string displayText)
    {
        displayText = FilterDisplayName;
        return new ItemOriginFilter(_origin);
    }
}

internal class LoadedSolutionFilterHandler : RoslynFilterHandler
{
    private const int LoadedSolutionFilterHandlerFilterId = 20;
    public LoadedSolutionFilterHandler(string displayName)
        : base(LoadedSolutionFilterHandlerFilterId, displayName, ItemOrigin.ExactMetadata)
    {
    }
}

[Export(typeof(IScopeFilterFactory))]
[TableManagerIdentifier("FindAllReferences*")]
[TableManagerIdentifier("FindResults*")]
[DeferCreation(OptionName = RichNavOptions.RichNavAvailableOptionName)] // This factory will not be loaded unless this option is set to Boolean true
[Name(AllSourcesFilterHandlerFactory.AllSourcesScopeFilter)]
[Order(Before = PredefinedScopeFilterNames.EntireRepositoryScopeFilter)]
internal class AllSourcesFilterHandlerFactory : IScopeFilterFactory
{
    private const string AllSourcesScopeFilter = "All Sources";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public AllSourcesFilterHandlerFactory()
    {
    }

    public IErrorListFilterHandler CreateFilter(IWpfTableControl tableControl)
        => new AllSourcesFilterHandler();
}

internal class AllSourcesFilterHandler : RoslynFilterHandler
{
    private const int AllSourcesFilterHandlerFilterId = 22;

    public AllSourcesFilterHandler()
        : base(AllSourcesFilterHandlerFilterId, ServicesVSResources.All_sources, ItemOrigin.IndexedInThirdParty)
    {
    }
}

[Export(typeof(IScopeFilterFactory))]
[TableManagerIdentifier("FindAllReferences*")]
[TableManagerIdentifier("FindResults*")]
[DeferCreation(OptionName = RichNavOptions.RichNavAvailableOptionName)] // This factory will not be loaded unless this option is set to Boolean true
[Name(PredefinedScopeFilterNames.EntireRepositoryScopeFilter)]
[Order(Before = PredefinedScopeFilterNames.LoadedSolutionScopeFilter)]
internal class EntireRepositoryFilterHandlerFactory : IScopeFilterFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EntireRepositoryFilterHandlerFactory()
    {
    }

    public IErrorListFilterHandler CreateFilter(IWpfTableControl tableControl)
        => new EntireRepositoryFilterHandler();
}

internal class EntireRepositoryFilterHandler : RoslynFilterHandler
{
    private const int EntireRepositoryFilterHandlerFilterId = 21;

    public EntireRepositoryFilterHandler()
        : base(EntireRepositoryFilterHandlerFilterId, ServicesVSResources.Entire_repository, ItemOrigin.IndexedInRepo)
    {
    }
}

internal class ItemOriginFilter : IEntryFilter
{
    private readonly ItemOrigin _targetOrigin;

    internal ItemOriginFilter(ItemOrigin targetOrigin)
        => _targetOrigin = targetOrigin;

    public bool Match(ITableEntryHandle entry)
    {
        Requires.NotNull(entry, nameof(entry));

        if (entry.TryGetValue(StandardTableKeyNames.ItemOrigin, out ItemOrigin entryOrigin))
            return entryOrigin <= _targetOrigin;

        // For backwards compatibility, consider items without ItemOrigin to be ItemOrigin.Exact (always matched)
        return true;
    }
}
