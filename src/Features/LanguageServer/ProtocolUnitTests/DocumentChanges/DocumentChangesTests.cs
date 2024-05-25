// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.DocumentChanges
{
    public partial class DocumentChangesTests : AbstractLanguageServerProtocolTests
    {
        public DocumentChangesTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task DocumentChanges_EndToEnd(bool mutatingLspWorkspace)
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
            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
            {
                Assert.Empty(testLspServer.GetTrackedTexts());

                await DidOpen(testLspServer, locationTyped.Uri);

                Assert.Single(testLspServer.GetTrackedTexts());

                var document = testLspServer.GetTrackedTexts().Single();
                Assert.Equal(documentText, document.ToString());

                await DidChange(testLspServer, locationTyped.Uri, (4, 8, "// hi there"));

                document = testLspServer.GetTrackedTexts().Single();
                Assert.Equal(expected, document.ToString());

                await DidClose(testLspServer, locationTyped.Uri);

                Assert.Empty(testLspServer.GetTrackedTexts());
            }
        }

        [Theory, CombinatorialData]
        public async Task DidOpen_DocumentIsTracked(bool mutatingLspWorkspace)
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                var document = testLspServer.GetTrackedTexts().FirstOrDefault();

                AssertEx.NotNull(document);
                Assert.Equal(documentText, document.ToString());
            }
        }

        [Theory, CombinatorialData]
        public async Task MultipleDidOpen_Errors(bool mutatingLspWorkspace)
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await Assert.ThrowsAnyAsync<StreamJsonRpc.RemoteRpcException>(() => DidOpen(testLspServer, locationTyped.Uri));
                await testLspServer.AssertServerShuttingDownAsync();
            }
        }

        [Theory, CombinatorialData]
        public async Task DidCloseWithoutDidOpen_Errors(bool mutatingLspWorkspace)
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
            {
                await Assert.ThrowsAnyAsync<StreamJsonRpc.RemoteRpcException>(() => DidClose(testLspServer, locationTyped.Uri));
                await testLspServer.AssertServerShuttingDownAsync();
            }
        }

        [Theory, CombinatorialData]
        public async Task DidChangeWithoutDidOpen_Errors(bool mutatingLspWorkspace)
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";
            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
            {
                await Assert.ThrowsAnyAsync<StreamJsonRpc.RemoteRpcException>(() => DidChange(testLspServer, locationTyped.Uri, (0, 0, "goo")));
                await testLspServer.AssertServerShuttingDownAsync();
            }
        }

        [Theory, CombinatorialData]
        public async Task DidClose_StopsTrackingDocument(bool mutatingLspWorkspace)
        {
            var source =
@"class A
{
    void M()
    {
        {|type:|}
    }
}";

            var (testLspServer, locationTyped, _) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await DidClose(testLspServer, locationTyped.Uri);

                Assert.Empty(testLspServer.GetTrackedTexts());
            }
        }

        [Theory, CombinatorialData]
        public async Task DidChange_AppliesChanges(bool mutatingLspWorkspace)
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

            var (testLspServer, locationTyped, _) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await DidChange(testLspServer, locationTyped.Uri, (4, 8, "// hi there"));

                var document = testLspServer.GetTrackedTexts().FirstOrDefault();

                AssertEx.NotNull(document);
                Assert.Equal(expected, document.ToString());
            }
        }

        [Theory, CombinatorialData]
        public async Task DidChange_DoesntUpdateWorkspace(bool mutatingLspWorkspace)
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

            var (testLspServer, locationTyped, documentText) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
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

        [Theory, CombinatorialData]
        public async Task DidChange_MultipleChanges1(bool mutatingLspWorkspace)
        {
            var source =
                """
                class A
                {
                    void M()
                    {
                        {|type:|}
                    }
                }
                """;
            var expected =
                """
                class A
                {
                    void M()
                    {
                        // hi there
                        // this builds on that
                    }
                }
                """;

            var (testLspServer, locationTyped, _) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await DidChange(testLspServer, locationTyped.Uri, (4, 8, "// hi there"), (5, 0, "        // this builds on that\r\n"));

                var document = testLspServer.GetTrackedTexts().FirstOrDefault();

                AssertEx.NotNull(document);
                Assert.Equal(expected, document.ToString());
            }
        }

        [Theory, CombinatorialData]
        public async Task DidChange_MultipleChanges2(bool mutatingLspWorkspace)
        {
            var source =
                """
                class A
                {
                    void M()
                    {
                        {|type:|}
                    }
                }
                """;
            var expected =
                """
                class A
                {
                    void M()
                    {
                        // hi there
                    }
                }
                """;

            var (testLspServer, locationTyped, _) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await DidChange(testLspServer, locationTyped.Uri, (4, 8, "// there"), (4, 11, "hi "));

                var document = testLspServer.GetTrackedTexts().FirstOrDefault();

                AssertEx.NotNull(document);
                Assert.Equal(expected, document.ToString());
            }
        }

        [Theory, CombinatorialData]
        public async Task DidChange_MultipleRequests(bool mutatingLspWorkspace)
        {
            var source =
                """
                class A
                {
                    void M()
                    {
                        {|type:|}
                    }
                }
                """;
            var expected =
                """
                class A
                {
                    void M()
                    {
                        // hi there
                        // this builds on that
                    }
                }
                """;

            var (testLspServer, locationTyped, _) = await GetTestLspServerAndLocationAsync(source, mutatingLspWorkspace);

            await using (testLspServer)
            {
                await DidOpen(testLspServer, locationTyped.Uri);

                await DidChange(testLspServer, locationTyped.Uri, (4, 8, "// hi there"));
                await DidChange(testLspServer, locationTyped.Uri, (5, 0, "        // this builds on that\r\n"));

                var document = testLspServer.GetTrackedTexts().FirstOrDefault();

                AssertEx.NotNull(document);
                Assert.Equal(expected, document.ToString());
            }
        }

        private async Task<(TestLspServer, LSP.Location, string)> GetTestLspServerAndLocationAsync(string source, bool mutatingLspWorkspace)
        {
            var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
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
