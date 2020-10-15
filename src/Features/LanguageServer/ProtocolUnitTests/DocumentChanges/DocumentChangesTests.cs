// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.DocumentChanges
{
    public partial class DocumentChangesTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task DocumentChanges_EndToEnd()
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var expected =
@"class A
{
    void M()
    {
        // hi there
    }
}";
            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                Assert.Empty(queue.GetTestAccessor().GetTrackedTexts());

                await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(locationTyped, documentText));

                Assert.Single(queue.GetTestAccessor().GetTrackedTexts());

                var document = queue.GetTestAccessor().GetTrackedTexts().Single();
                Assert.Equal(documentText, document.ToString());

                await DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(locationTyped.Uri, (4, 8, "// hi there")));

                document = queue.GetTestAccessor().GetTrackedTexts().Single();
                Assert.Equal(expected, document.ToString());

                await DidClose(queue, workspace.CurrentSolution, CreateDidCloseTextDocumentParams(locationTyped));

                Assert.Empty(queue.GetTestAccessor().GetTrackedTexts());
            }
        }

        [Fact]
        public async Task DidOpen_DocumentIsTracked()
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(locationTyped, documentText));

                var document = queue.GetTestAccessor().GetTrackedTexts().FirstOrDefault();

                Assert.NotNull(document);
                Assert.Equal(documentText, document.ToString());
            }
        }

        [Fact]
        public async Task MultipleDidOpen_Errors()
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(locationTyped, documentText));

                await Assert.ThrowsAsync<InvalidOperationException>(() => DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(locationTyped, documentText)));
            }
        }

        [Fact]
        public async Task DidCloseWithoutDidOpen_Errors()
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                await Assert.ThrowsAsync<InvalidOperationException>(() => DidClose(queue, workspace.CurrentSolution, CreateDidCloseTextDocumentParams(locationTyped)));
            }
        }

        [Fact]
        public async Task DidChangeWithoutDidOpen_Errors()
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                await Assert.ThrowsAsync<InvalidOperationException>(() => DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(locationTyped.Uri, (0, 0, "goo"))));
            }
        }

        [Fact]
        public async Task DidClose_StopsTrackingDocument()
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";

            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(locationTyped, documentText));

                await DidClose(queue, workspace.CurrentSolution, CreateDidCloseTextDocumentParams(locationTyped));

                Assert.Empty(queue.GetTestAccessor().GetTrackedTexts());
            }
        }

        [Fact]
        public async Task DidChange_AppliesChanges()
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var expected =
  @"class A
{
    void M()
    {
        // hi there
    }
}";

            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(locationTyped, documentText));

                await DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(locationTyped.Uri, (4, 8, "// hi there")));

                var document = queue.GetTestAccessor().GetTrackedTexts().FirstOrDefault();

                Assert.NotNull(document);
                Assert.Equal(expected, document.ToString());
            }
        }

        [Fact]
        public async Task DidChange_DoesntUpdateWorkspace()
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var expected =
  @"class A
{
    void M()
    {
        // hi there
    }
}";

            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(locationTyped, documentText));

                await DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(locationTyped.Uri, (4, 8, "// hi there")));

                var documentTextFromWorkspace = (await workspace.CurrentSolution.GetDocuments(locationTyped.Uri).Single().GetTextAsync()).ToString();

                Assert.NotNull(documentTextFromWorkspace);
                Assert.Equal(documentText, documentTextFromWorkspace);

                // Just to ensure this test breaks if didChange stops working for some reason
                Assert.NotEqual(expected, documentTextFromWorkspace);
            }
        }

        [Fact]
        public async Task DidChange_MultipleChanges()
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var expected =
  @"class A
{
    void M()
    {
        // hi there
        // this builds on that
    }
}";

            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(locationTyped, documentText));

                await DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(locationTyped.Uri, (4, 8, "// hi there"), (5, 0, "        // this builds on that\r\n")));

                var document = queue.GetTestAccessor().GetTrackedTexts().FirstOrDefault();

                Assert.NotNull(document);
                Assert.Equal(expected, document.ToString());
            }
        }

        [Fact]
        public async Task DidChange_MultipleRequests()
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var expected =
  @"class A
{
    void M()
    {
        // hi there
        // this builds on that
    }
}";

            var (workspace, locationTyped, documentText) = await GetWorkspaceAndLocationAsync(source);

            using (workspace)
            {
                var queue = CreateRequestQueue(workspace.CurrentSolution);

                await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(locationTyped, documentText));

                await DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(locationTyped.Uri, (4, 8, "// hi there")));

                await DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(locationTyped.Uri, (5, 0, "        // this builds on that\r\n")));

                var document = queue.GetTestAccessor().GetTrackedTexts().FirstOrDefault();

                Assert.NotNull(document);
                Assert.Equal(expected, document.ToString());
            }
        }

        private async Task<(TestWorkspace, LSP.Location, string)> GetWorkspaceAndLocationAsync(string source)
        {
            var workspace = CreateTestWorkspace(source, out var locations);
            var locationTyped = locations["type"].Single();
            var documentText = await workspace.CurrentSolution.GetDocuments(locationTyped.Uri).Single().GetTextAsync();

            return (workspace, locationTyped, documentText.ToString());
        }

        private static async Task DidOpen(RequestExecutionQueue queue, Solution solution, LSP.DidOpenTextDocumentParams didOpenParams)
        {
            await GetLanguageServer(solution).ExecuteRequestAsync<LSP.DidOpenTextDocumentParams, object>(queue, Methods.TextDocumentDidOpenName,
                           didOpenParams, new LSP.ClientCapabilities(), null, CancellationToken.None);
        }

        private static async Task DidChange(RequestExecutionQueue queue, Solution solution, LSP.DidChangeTextDocumentParams didChangeParams)
        {
            await GetLanguageServer(solution).ExecuteRequestAsync<LSP.DidChangeTextDocumentParams, object>(queue, Methods.TextDocumentDidChangeName,
                           didChangeParams, new LSP.ClientCapabilities(), null, CancellationToken.None);
        }

        private static async Task DidClose(RequestExecutionQueue queue, Solution solution, LSP.DidCloseTextDocumentParams didCloseParams)
        {
            await GetLanguageServer(solution).ExecuteRequestAsync<LSP.DidCloseTextDocumentParams, object>(queue, Methods.TextDocumentDidCloseName,
                           didCloseParams, new LSP.ClientCapabilities(), null, CancellationToken.None);
        }

        private static LSP.DidOpenTextDocumentParams CreateDidOpenTextDocumentParams(LSP.Location location, string source)
            => new LSP.DidOpenTextDocumentParams()
            {
                TextDocument = new TextDocumentItem
                {
                    Text = source,
                    Uri = location.Uri
                }
            };

        private static LSP.DidChangeTextDocumentParams CreateDidChangeTextDocumentParams(Uri documentUri, params (int line, int column, string text)[] changes)
        {
            var changeEvents = new List<TextDocumentContentChangeEvent>();
            foreach (var change in changes)
            {
                changeEvents.Add(new TextDocumentContentChangeEvent
                {
                    Text = change.text,
                    Range = new LSP.Range
                    {
                        Start = new Position(change.line, change.column),
                        End = new Position(change.line, change.column)
                    }
                });
            }

            return new LSP.DidChangeTextDocumentParams()
            {
                TextDocument = new VersionedTextDocumentIdentifier
                {
                    Uri = documentUri
                },
                ContentChanges = changeEvents.ToArray()
            };
        }

        private static LSP.DidCloseTextDocumentParams CreateDidCloseTextDocumentParams(LSP.Location location)
           => new LSP.DidCloseTextDocumentParams()
           {
               TextDocument = new TextDocumentIdentifier
               {
                   Uri = location.Uri
               }
           };
    }
}
