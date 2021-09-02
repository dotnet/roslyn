// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.DocumentChanges
{
    public partial class DocumentChangesTests
    {
        protected override TestComposition Composition => base.Composition
            .AddParts(typeof(GetLspSolutionHandlerProvider));

        [Fact]
        public async Task LinkedDocuments_AllTracked()
        {
            var documentText = "class C { }";
            var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\C.cs"">{documentText}{{|caret:|}}</Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document IsLinkFile=""true"" LinkFilePath=""C:\C.cs"" LinkAssemblyName=""CSProj1""></Document>
    </Project>
</Workspace>";

            using var testLspServer = CreateXmlTestLspServer(workspaceXml, out var locations);
            var caretLocation = locations["caret"].Single();

            await DidOpen(testLspServer, caretLocation.Uri);

            var trackedDocuments = testLspServer.GetQueueAccessor().GetTrackedTexts();
            Assert.Equal(1, trackedDocuments.Count);

            var solution = await GetLSPSolution(testLspServer, caretLocation.Uri);

            foreach (var document in solution.Projects.First().Documents)
            {
                Assert.Equal(documentText, document.GetTextSynchronously(CancellationToken.None).ToString());
            }

            await DidClose(testLspServer, caretLocation.Uri);

            Assert.Empty(testLspServer.GetQueueAccessor().GetTrackedTexts());
        }

        [Fact]
        public async Task LinkedDocuments_AllTextChanged()
        {
            var initialText =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";
            var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\C.cs"">{initialText}</Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document IsLinkFile=""true"" LinkFilePath=""C:\C.cs"" LinkAssemblyName=""CSProj1""></Document>
    </Project>
</Workspace>";

            using var testLspServer = CreateXmlTestLspServer(workspaceXml, out var locations);
            var caretLocation = locations["caret"].Single();

            var updatedText =
@"class A
{
    void M()
    {
        // hi there
    }
}";

            await DidOpen(testLspServer, caretLocation.Uri);

            Assert.Equal(1, testLspServer.GetQueueAccessor().GetTrackedTexts().Count);

            await DidChange(testLspServer, caretLocation.Uri, (4, 8, "// hi there"));

            var solution = await GetLSPSolution(testLspServer, caretLocation.Uri);

            foreach (var document in solution.Projects.First().Documents)
            {
                Assert.Equal(updatedText, document.GetTextSynchronously(CancellationToken.None).ToString());
            }

            await DidClose(testLspServer, caretLocation.Uri);

            Assert.Empty(testLspServer.GetQueueAccessor().GetTrackedTexts());
        }

        private static Task<Solution> GetLSPSolution(TestLspServer testLspServer, Uri uri)
        {
            return testLspServer.ExecuteRequestAsync<Uri, Solution>(nameof(GetLSPSolutionHandler), uri, new ClientCapabilities(), null, CancellationToken.None);
        }

        [Shared, ExportRoslynLanguagesLspRequestHandlerProvider, PartNotDiscoverable]
        [ProvidesMethod(GetLSPSolutionHandler.MethodName)]
        private class GetLspSolutionHandlerProvider : AbstractRequestHandlerProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public GetLspSolutionHandlerProvider()
            {
            }

            public override ImmutableArray<IRequestHandler> CreateRequestHandlers() => ImmutableArray.Create<IRequestHandler>(new GetLSPSolutionHandler());
        }

        private class GetLSPSolutionHandler : IRequestHandler<Uri, Solution>
        {
            public const string MethodName = nameof(GetLSPSolutionHandler);

            public string Method => MethodName;

            public bool MutatesSolutionState => false;
            public bool RequiresLSPSolution => true;

            public TextDocumentIdentifier? GetTextDocumentIdentifier(Uri request)
                => new TextDocumentIdentifier { Uri = request };

            public Task<Solution> HandleRequestAsync(Uri request, RequestContext context, CancellationToken cancellationToken)
                => Task.FromResult(context.Solution!);
        }
    }
}
