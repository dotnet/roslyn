// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.References;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.DocumentChanges
{
    public partial class DocumentChangesTests
    {
        [Fact]
        public async Task FindReferencesInChangingDocument()
        {
            var source =
@"class A
{
    public int {|type:|}someInt = 1;
    void M()
    {
    }
}
class B
{
    void M2()
    {
    }
}";

            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                Assert.Empty(queue.GetTestAccessor().GetTrackedTexts());

                await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(locationTyped, documentText));

                var findResults = await FindAllReferencesHandlerTests.RunFindAllReferencesAsync(queue, workspace.CurrentSolution, locationTyped);
                Assert.Single(findResults);

                Assert.Equal("A", findResults[0].ContainingType);

                // Declare a local inside A.M()
                await DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(locationTyped.Uri, (5, 0, "var i = someInt + 1;\r\n")));

                findResults = await FindAllReferencesHandlerTests.RunFindAllReferencesAsync(queue, workspace.CurrentSolution, locationTyped);
                Assert.Equal(2, findResults.Length);

                Assert.Equal("A", findResults[0].ContainingType);
                Assert.Equal("M", findResults[1].ContainingMember);

                // Declare a field in B
                await DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(locationTyped.Uri, (10, 0, "int someInt = A.someInt + 1;\r\n")));

                findResults = await FindAllReferencesHandlerTests.RunFindAllReferencesAsync(queue, workspace.CurrentSolution, locationTyped);
                Assert.Equal(3, findResults.Length);

                Assert.Equal("A", findResults[0].ContainingType);
                Assert.Equal("B", findResults[2].ContainingType);
                Assert.Equal("M", findResults[1].ContainingMember);

                // Declare a local inside B.M2()
                await DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(locationTyped.Uri, (13, 0, "var j = someInt + A.someInt;\r\n")));

                findResults = await FindAllReferencesHandlerTests.RunFindAllReferencesAsync(queue, workspace.CurrentSolution, locationTyped);
                Assert.Equal(4, findResults.Length);

                Assert.Equal("A", findResults[0].ContainingType);
                Assert.Equal("B", findResults[2].ContainingType);
                Assert.Equal("M", findResults[1].ContainingMember);
                Assert.Equal("M2", findResults[3].ContainingMember);

                // NOTE: This is not a real world scenario
                // By closing the document we revert back to the original state, but in the real world
                // the original state will have been updated by back channels (text buffer sync, file changed on disk, etc.)
                // This is validating that the above didn't succeed by any means except the FAR handler being passed
                // the updated document, so if we regress and get lucky, we still know about it.
                await DidClose(queue, workspace.CurrentSolution, CreateDidCloseTextDocumentParams(locationTyped));

                findResults = await FindAllReferencesHandlerTests.RunFindAllReferencesAsync(queue, workspace.CurrentSolution, locationTyped);
                Assert.Single(findResults);

                Assert.Equal("A", findResults[0].ContainingType);

            }
        }
    }
}
