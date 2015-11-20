// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.UnitTests.Outlining;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining.MetadataAsSource
{
    /// <summary>
    /// Identifiers coming from IL can be just about any valid string and since C# doesn't have a way to escape all possible
    /// IL identifiers, we have to account for the possibility that an item's metadata name could lead to unparseable code.
    /// </summary>
    public class InvalidIdentifierTests : AbstractSyntaxOutlinerTests
    {
        protected override string LanguageName => LanguageNames.CSharp;
        protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;

        internal override OutliningSpan[] GetRegions(Document document, int position)
        {
            var outliningService = document.Project.LanguageServices.GetService<IOutliningService>();

            return outliningService
                .GetOutliningSpansAsync(document, CancellationToken.None).Result.WhereNotNull().ToArray();
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task PrependedDollarSign()
        {
            const string code = @"
$$class C
{
    public void $Invoke();
}";

            NoRegions(code);
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task SymbolsAndPunctuation()
        {
            const string code = @"
$$class C
{
    public void !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`();
}";

            NoRegions(code);
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task IdentifierThatLooksLikeCode()
        {
            const string code = @"
$$class C
{
    public void } } public class CodeInjection{ } /* now everything is commented ();
}";

            NoRegions(code);
        }
    }
}
