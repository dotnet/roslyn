// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    public class RenameHandlerTests : AbstractLiveShareRequestHandlerTests
    {
        [WpfFact]
        public async Task TestRenameAsync()
        {
            var markup =
@"class A
{
    void {|caret:|}{|renamed:M|}()
    {
    }
    void M2()
    {
        {|renamed:M|}()
    }
}";
            var (solution, ranges) = CreateTestSolution(markup);
            var renameLocation = ranges["caret"].First();
            var renameValue = "RENAME";
            var expectedEdits = ranges["renamed"].Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

            var results = await TestHandleAsync<LSP.RenameParams, LSP.WorkspaceEdit>(solution, CreateRenameParams(renameLocation, renameValue));
            AssertJsonEquals(expectedEdits, results.DocumentChanges.First().Edits);
        }

        private static void AssertDocumentEditsEqual(IList<LSP.Location> expectedRenameLocations, string expectedRenameValue, LSP.TextEdit[] actualEdits)
        {
            for (var i = 0; i < expectedRenameLocations.Count; i++)
            {
                var expectedLocation = expectedRenameLocations[i];
                var actualEdit = actualEdits[i];
                Assert.Equal(expectedLocation.Range, actualEdit.Range);
                Assert.Equal(expectedRenameValue, actualEdit.NewText);
            }
        }

        private static LSP.RenameParams CreateRenameParams(LSP.Location location, string newName)
            => new LSP.RenameParams()
            {
                NewName = newName,
                Position = location.Range.Start,
                TextDocument = CreateTextDocumentIdentifier(location.Uri)
            };
    }
}
