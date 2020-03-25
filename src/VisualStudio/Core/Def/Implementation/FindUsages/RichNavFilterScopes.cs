// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.Internal.VisualStudio.Shell.ErrorList;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindUsages
{
    [Export(typeof(IScopeFilterFactory))]
    [TableManagerIdentifier("FindAllReferences*")]
    [TableManagerIdentifier("FindResults*")]
    [Replaces(PredefinedScopeFilterNames.EntireSolutionScopeFilter)]
    [Replaces(PredefinedScopeFilterNames.AllItemsScopeFilter)]
    [DefaultScope]
    [Name(PredefinedScopeFilterNames.LoadedSolutionScopeFilter)]
    [Order(Before = PredefinedScopeFilterNames.AllItemsScopeFilter)]
    internal class LoadedSolutionScopeFilterFactory : IReplacingScopeFilterFactory
    {
        [ImportingConstructor]
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
                return new LoadedSolutionFilterHandler("Loaded Items");
            }
            else if (filterIdentifier == PredefinedScopeFilterNames.EntireSolutionScopeFilter)
            {
                return new LoadedSolutionFilterHandler("Loaded Solution");
            }
            return null; // Don't replace
        }
    }

    internal class LoadedSolutionFilterHandler : FilterHandlerBase
    {
        private readonly string displayName;
        internal const int LoadedSolutionFilterHandlerFilterId = 20;

        public LoadedSolutionFilterHandler(string displayName)
        {
            this.displayName = displayName;
        }

        public override int FilterId => LoadedSolutionFilterHandlerFilterId;

        public override string FilterDisplayName => this.displayName;

        public override IEntryFilter GetFilter(out string displayText)
        {
            displayText = this.displayName;
            return new ItemOriginFilter(ItemOrigin.ExactMetadata);
        }
    }

    [Export(typeof(IScopeFilterFactory))]
    [TableManagerIdentifier("FindAllReferences*")]
    [TableManagerIdentifier("FindResults*")]
    [DeferCreation(OptionName = "ResponsiveCompletion")]
    [Name(PredefinedScopeFilterNames.EntireOrganizationScopeFilter)]
    [Order(Before = PredefinedScopeFilterNames.EntireRepositoryScopeFilter)]
    internal class EntireOragnizationFilterHandlerFactory : IScopeFilterFactory
    {
        [ImportingConstructor]
        public EntireOragnizationFilterHandlerFactory()
        {
        }

        public IErrorListFilterHandler CreateFilter(IWpfTableControl tableControl)
        {
            return new EntireOragnizationFilterHandler();
        }
    }

    internal class EntireOragnizationFilterHandler : FilterHandlerBase
    {
        private readonly string displayName;
        internal const int EntireRepositoryFilterHandlerFilterId = 22;

        public EntireOragnizationFilterHandler()
        {
            this.displayName = "Entire Organization";
        }

        public override int FilterId => EntireRepositoryFilterHandlerFilterId;

        public override string FilterDisplayName => this.displayName;

        public override IEntryFilter GetFilter(out string displayText)
        {
            displayText = this.displayName;
            return new ItemOriginFilter(ItemOrigin.IndexedInOrganization);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            // User just selected the broadest scope. See if we need to re-do the search
        }
    }

    [Export(typeof(IScopeFilterFactory))]
    [TableManagerIdentifier("FindAllReferences*")]
    [TableManagerIdentifier("FindResults*")]
    [Name(PredefinedScopeFilterNames.EntireRepositoryScopeFilter)]
    [Order(Before = PredefinedScopeFilterNames.LoadedSolutionScopeFilter)]
    internal class EntireRepositoryFilterHandlerFactory : IScopeFilterFactory
    {
        [ImportingConstructor]
        public EntireRepositoryFilterHandlerFactory()
        {
        }

        public IErrorListFilterHandler CreateFilter(IWpfTableControl tableControl)
        {
            return new EntireRepositoryFilterHandler();
        }
    }

    internal class EntireRepositoryFilterHandler : FilterHandlerBase
    {
        private readonly string displayName;
        internal const int EntireRepositoryFilterHandlerFilterId = 21;

        public EntireRepositoryFilterHandler()
        {
            this.displayName = "Entire Repository";
        }

        public override int FilterId => EntireRepositoryFilterHandlerFilterId;

        public override string FilterDisplayName => this.displayName;

        public override IEntryFilter GetFilter(out string displayText)
        {
            displayText = this.displayName;
            return new ItemOriginFilter(ItemOrigin.IndexedInRepo);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            // User just selected a broader scope. See if we need to re-do the search
        }
    }

    internal class ItemOriginFilter : IEntryFilter
    {
        private readonly ItemOrigin targetOrigin;

        internal ItemOriginFilter(ItemOrigin targetOrigin)
        {
            this.targetOrigin = targetOrigin;
        }

        public bool Match(ITableEntryHandle entry)
        {
            Requires.NotNull(entry, nameof(entry));

            if (entry.TryGetValue(StandardTableKeyNames.ItemOrigin, out ItemOrigin entryOrigin))
            {
                return entryOrigin < targetOrigin;
            }

            // For backwards compatibility, consider items without ItemOrigin to be ItemOrigin.Exact (always matched)
            return true;
        }
    }
}
