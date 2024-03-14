// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.ErrorList;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences.Filters;

[Export(typeof(IScopeFilterFactory))]
[TableManagerIdentifier("FindAllReferences*")]
[Replaces(PredefinedScopeFilterNames.EntireSolutionScopeFilter)]
[Name(nameof(EntireSolutionWithoutMetadataFilterFactory))]
[Order(After = PredefinedScopeFilterNames.EntireSolutionScopeFilter)]
internal class EntireSolutionWithoutMetadataFilterFactory : IReplacingScopeFilterFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EntireSolutionWithoutMetadataFilterFactory()
    {
    }

    public IErrorListFilterHandler? CreateFilter(IWpfTableControl tableControl)
    {
        // We're only replacing the "Entire Solution" filter, and not creating a new one.
        return null;
    }

    public IErrorListFilterHandler ReplaceFilter(IWpfTableControl tableControl, string filterIdentifier)
    {
        return EntireSolutionWithoutMetadataFilterHandler.Instance;
    }

    private class EntireSolutionWithoutMetadataFilterHandler : ExternalSourcesFilterHandlerBase
    {
        public static EntireSolutionWithoutMetadataFilterHandler Instance = new();

        public override int FilterId => PredefinedScopeFilterIds.EntireSolutionScopeFilter;
        public override string FilterDisplayName => ServicesVSResources.Entire_solution;

        public override bool IncludeExact => true;
        public override bool IncludeExactMetadata => false;

        // We include Other because LSP currently returns that, so otherwise we'd break Razor
        // TODO: https://github.com/dotnet/roslyn/issues/42847
        public override bool IncludeOther => true;
    }
}
