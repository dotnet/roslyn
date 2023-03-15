// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.ErrorList;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences.Filters
{
    [Export(typeof(IScopeFilterFactory))]
    [TableManagerIdentifier("FindAllReferences*")]
    [DefaultScope]
    [Name(nameof(EntireSolutionAndExternalFilterFactory))]
    [Order(Before = PredefinedScopeFilterNames.EntireSolutionScopeFilter)]
    internal class EntireSolutionAndExternalFilterFactory : IScopeFilterFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EntireSolutionAndExternalFilterFactory()
        {
        }

        public IErrorListFilterHandler CreateFilter(IWpfTableControl tableControl)
        {
            return EntireSolutionAndExternalFilterHandler.Instance;
        }

        private class EntireSolutionAndExternalFilterHandler : ExternalSourcesFilterHandlerBase
        {
            public static EntireSolutionAndExternalFilterHandler Instance = new();

            /// <summary>
            /// FilterId is persisted to user settings to remember the selection. Starting in the 40s means
            /// its unqiue compared to the rest of <see cref="PredefinedScopeFilterIds"/>
            /// </summary>
            public override int FilterId => 40;
            public override string FilterDisplayName => ServicesVSResources.Entire_solution_and_external_sources;

            public override bool IncludeExact => true;
            public override bool IncludeExactMetadata => true;

            // We include Other because LSP currently returns that, so otherwise we'd break Razor
            // TODO: https://github.com/dotnet/roslyn/issues/42847
            public override bool IncludeOther => true;
        }
    }
}
