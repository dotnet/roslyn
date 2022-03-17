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
            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source);

            using (testLspServer)
            {
                Assert.Empty(testLspServer.GetQueueAccessor().GetTrackedTexts());

                await DidOpen(testLspServer, locationTyped.Uri);

                Assert.Single(testLspServer.GetQueueAccessor().GetTrackedTexts());

                var document = testLspServer.GetQueueAccessor().GetTrackedTexts().Single();
                Assert.Equal(documentText, document.ToString());

                await DidChange(testLspServer, locationTyped.Uri, (4, 8, "// hi there"));

                document = testLspServer.GetQueueAccessor().GetTrackedTexts().Single();
                Assert.Equal(expected, document.ToString());

                await DidClose(testLspServer, locationTyped.Uri);

                Assert.Empty(testLspServer.GetQueueAccessor().GetTrackedTexts());
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
            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source);

            using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                var document = testLspServer.GetQueueAccessor().GetTrackedTexts().FirstOrDefault();

                AssertEx.NotNull(document);
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
            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source);

            using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await Assert.ThrowsAsync<StreamJsonRpc.RemoteInvocationException>(() => DidOpen(testLspServer, locationTyped.Uri));
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
            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source);

            using (testLspServer)
            {
                await Assert.ThrowsAsync<StreamJsonRpc.RemoteInvocationException>(() => DidClose(testLspServer, locationTyped.Uri));
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
            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source);

            using (testLspServer)
            {
                await Assert.ThrowsAsync<StreamJsonRpc.RemoteInvocationException>(() => DidChange(testLspServer, locationTyped.Uri, (0, 0, "goo")));
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

            var (testLspServer, locationTyped, _) = await GetTestLspServerAndLocationAsync(source);

            using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await DidClose(testLspServer, locationTyped.Uri);

                Assert.Empty(testLspServer.GetQueueAccessor().GetTrackedTexts());
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

            var (testLspServer, locationTyped, _) = await GetTestLspServerAndLocationAsync(source);

            using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await DidChange(testLspServer, locationTyped.Uri, (4, 8, "// hi there"));

                var document = testLspServer.GetQueueAccessor().GetTrackedTexts().FirstOrDefault();

                AssertEx.NotNull(document);
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

            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source);

            using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await DidChange(testLspServer, locationTyped.Uri, (4, 8, "// hi there"));

                var documentTextFromWorkspace = (await testLspServer.GetCurrentSolution().GetDocuments(locationTyped.Uri).Single().GetTextAsync()).ToString();

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

            var (testLspServer, locationTyped, _) = await GetTestLspServerAndLocationAsync(source);

            using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await DidChange(testLspServer, locationTyped.Uri, (4, 8, "// hi there"), (5, 0, "        // this builds on that\r\n"));

                var document = testLspServer.GetQueueAccessor().GetTrackedTexts().FirstOrDefault();

                AssertEx.NotNull(document);
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

            var (testLspServer, locationTyped, _) = await GetTestLspServerAndLocationAsync(source);

            using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await DidChange(testLspServer, locationTyped.Uri, (4, 8, "// hi there"));

                await DidChange(testLspServer, locationTyped.Uri, (5, 0, "        // this builds on that\r\n"));

                var document = testLspServer.GetQueueAccessor().GetTrackedTexts().FirstOrDefault();

                AssertEx.NotNull(document);
                Assert.Equal(expected, document.ToString());
            }
        }

        private async Task<(TestLspServer, LSP.Location, string)> GetTestLspServerAndLocationAsync(string source)
        {
            var testLspServer = await CreateTestLspServerAsync(source, CapabilitiesWithVSExtensions);
            var locationTyped = testLspServer.GetLocations("type").Single();
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(locationTyped.Uri).Single().GetTextAsync();

            return (testLspServer, locationTyped, documentText.ToString());
        }

        private static Task DidOpen(TestLspServer testLspServer, Uri uri) => testLspServer.OpenDocumentAsync(uri);

        private static async Task DidChange(TestLspServer testLspServer, Uri uri, params (int line, int column, string text)[] changes)
            => await testLspServer.InsertTextAsync(uri, changes);

        private static async Task DidClose(TestLspServer testLspServer, Uri uri) => await testLspServer.CloseDocumentAsync(uri);
    }
}
