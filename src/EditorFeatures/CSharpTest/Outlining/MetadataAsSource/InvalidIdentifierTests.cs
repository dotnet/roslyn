// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
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
        private void Test(string fileContents, params OutliningSpan[] expectedSpans)
        {
            var workspace = TestWorkspaceFactory.CreateWorkspaceFromFiles(WorkspaceKind.MetadataAsSource, LanguageNames.CSharp, null, null, fileContents);
            var outliningService = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<IOutliningService>();
            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var actualOutliningSpans = outliningService.GetOutliningSpansAsync(document, CancellationToken.None).Result.Where(s => s != null).ToArray();

            Assert.Equal(expectedSpans.Length, actualOutliningSpans.Length);
            for (int i = 0; i < expectedSpans.Length; i++)
            {
                AssertRegion(expectedSpans[i], actualOutliningSpans[i]);
            }
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void PrependedDollarSign()
        {
            var source = @"
class C
{
    public void $Invoke();
}";
            Test(source);
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void SymbolsAndPunctuation()
        {
            var source = @"
class C
{
    public void !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`();
}";
            Test(source);
        }

        [WorkItem(1174405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void IdentifierThatLooksLikeCode()
        {
            var source = @"
class C
{
    public void } } public class CodeInjection{ } /* now everything is commented ();
}";
            Test(source);
        }
    }
}
