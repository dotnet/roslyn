// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.UnitTests.Outlining;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining.MetadataAsSource
{
    /// <summary>
    /// Identifiers coming from IL can be just about any valid string and since C# doesn't have a way to escape all possible
    /// IL identifiers, we have to account for the possibility that an item's metadata name could lead to unparseable code.
    /// </summary>
    public class InvalidIdentifierTests : AbstractOutlinerTests
    {
        private async Task TestAsync(string fileContents, params OutliningSpan[] expectedSpans)
        {
            var workspace = await TestWorkspaceFactory.CreateWorkspaceFromFilesAsync(WorkspaceKind.MetadataAsSource, LanguageNames.CSharp, null, null, fileContents);
            var outliningService = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<IOutliningService>();
            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var actualOutliningSpans = (await outliningService.GetOutliningSpansAsync(document, CancellationToken.None)).Where(s => s != null).ToArray();

            Assert.Equal(expectedSpans.Length, actualOutliningSpans.Length);
            for (int i = 0; i < expectedSpans.Length; i++)
            {
                AssertRegion(expectedSpans[i], actualOutliningSpans[i]);
            }
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task PrependedDollarSign()
        {
            var source = @"
class C
{
    public void $Invoke();
}";
            await TestAsync(source);
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task SymbolsAndPunctuation()
        {
            var source = @"
class C
{
    public void !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`();
}";
            await TestAsync(source);
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task IdentifierThatLooksLikeCode()
        {
            var source = @"
class C
{
    public void } } public class CodeInjection{ } /* now everything is commented ();
}";
            await TestAsync(source);
        }
    }
}
