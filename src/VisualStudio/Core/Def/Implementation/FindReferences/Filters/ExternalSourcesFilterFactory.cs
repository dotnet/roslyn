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
    [Name(nameof(ExternalSourcesFilterFactory))]
    [Order(After = nameof(EntireSolutionWithoutMetadataFilterFactory))]
    internal class ExternalSourcesFilterFactory : IScopeFilterFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExternalSourcesFilterFactory()
        {
        }

        public IErrorListFilterHandler CreateFilter(IWpfTableControl tableControl)
        {
            return new ExternalSourcesFilterHandler();
        }

        private class ExternalSourcesFilterHandler : ExternalSourcesFilterHandlerBase
        {
            public override int FilterId => 42;
            public override string FilterDisplayName => ServicesVSResources.External_sources;

            public override bool IncludeExact => false;
            public override bool IncludeExactMetadata => true;

            // TODO: Remove when https://github.com/dotnet/roslyn/issues/42847 is fixed
            public override bool IncludeOther => false;
        }
    }
}
