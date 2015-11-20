// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.UnitTests.Outlining;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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
            using (var workspace = TestWorkspaceFactory.CreateWorkspaceFromFiles(WorkspaceKind.MetadataAsSource, LanguageNames.CSharp, null, null, fileContents))
            {
                var hostDocument = workspace.Documents.Single();
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var outliningService = document.Project.LanguageServices.GetService<IOutliningService>();
                var actualOutliningSpans = (await outliningService.GetOutliningSpansAsync(document, CancellationToken.None))
                	.WhereNotNull().ToArray();

                Assert.Equal(expectedSpans.Length, actualOutliningSpans.Length);
                for (int i = 0; i < expectedSpans.Length; i++)
                {
                    AssertRegion(expectedSpans[i], actualOutliningSpans[i]);
                }
            }
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task PrependedDollarSign()
        {
            const string code = @"
class C
{
    public void $Invoke();
}";

            await TestAsync(code);
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task SymbolsAndPunctuation()
        {
            const string code = @"
class C
{
    public void !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`();
}";

            await TestAsync(code);
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task IdentifierThatLooksLikeCode()
        {
            const string code = @"
class C
{
    public void } } public class CodeInjection{ } /* now everything is commented ();
}";

            await TestAsync(code);
        }
    }
}
