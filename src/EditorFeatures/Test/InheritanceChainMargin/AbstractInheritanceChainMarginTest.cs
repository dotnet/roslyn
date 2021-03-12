// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.InheritanceChainMargin
{
    [Trait(Traits.Feature, Traits.Features.InheritanceChainMargin)]
    public abstract class AbstractInheritanceChainMarginTest
    {
        protected readonly string BaseType = nameof(BaseType);
        protected readonly string SubType = nameof(SubType);
        protected readonly string Overriding = nameof(Overriding);
        protected readonly string Overriden = nameof(Overriden);
        protected readonly string Implementing = nameof(Implementing);
        protected readonly string Implemented = nameof(Implemented);

        public Task VerifyInDifferentFileAsync(
            string membersMarkup,
            string targetsMarkup,
            bool testInSingleProject)
        {

        }

        public async Task VerifyInSameFileAsync(string markup, string languageName)
        {
            TestFileMarkupParser.GetPositionsAndSpans(
                markup,
                out var cleanMarkup,
                out var carets,
                out var selectedSpans);
            var workspaceFile = $@"
<Workspace>
   <Project Language=""{languageName}"" CommonReferences=""true"">
       <Document>
            {markup}
       </Document>
   </Project>
</Workspace> ";

            using var testWorkspace = TestWorkspace.Create(workspaceFile);
            var document = testWorkspace.CurrentSolution.GetRequiredDocument(testWorkspace.Documents.Single().Id);
            var service = document.GetRequiredLanguageService<IInheritanceMarginService>();
            var lineToInheritanceInfo = await service.GetInheritanceInfoForLineAsync(document, CancellationToken.None).ConfigureAwait(false);

            var lines = (await document.GetTextAsync().ConfigureAwait(false)).Lines;

            // 1. Verify all the expected lines have inheritance info
            var expectedLines = carets.Select(c => lines.GetLineFromPosition(c).LineNumber).ToHashSet();
            var actualLines = lineToInheritanceInfo.Keys;
            AssertEx.SetEqual(expectedLines, actualLines);

            // 2. Verify the info in each line
            foreach (var line in expectedLines)
            {
                var identifierStarts = await GetMemberIdentifierStartsOnLineAsync(document, line).ConfigureAwait(false);
                foreach (var position in identifierStarts)
                {

                }
                var symbol = await SymbolFinder.FindSymbolAtPositionAsync()
            }
        }

        protected abstract Task<ImmutableArray<int>> GetMemberIdentifierStartsOnLineAsync(Document document, int lineNumber);
    }
}
