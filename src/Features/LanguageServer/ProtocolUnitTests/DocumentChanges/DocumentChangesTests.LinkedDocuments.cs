// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.DocumentChanges
{
    public partial class DocumentChangesTests
    {
        [Fact]
        public async Task LinkedDocuments_AllTracked()
        {
            var workspaceXml =
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\C.cs"">{|caret:|}</Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document IsLinkFile=""true"" LinkFilePath=""C:\C.cs"" LinkAssemblyName=""CSProj1""></Document>
    </Project>
</Workspace>";

            using var workspace = CreateXmlTestWorkspace(workspaceXml, out var locations);
            var caretLocation = locations["caret"].Single();

            var documentText = "class C { }";

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(caretLocation, documentText));

            var trackedDocuments = queue.GetTestAccessor().GetTrackedTexts();
            Assert.Equal(1, trackedDocuments.Count);

            var solution = await GetLSPSolution(queue, caretLocation.Uri);

            foreach (var document in solution.Projects.First().Documents)
            {
                Assert.Equal(documentText, document.GetTextSynchronously(CancellationToken.None).ToString());
            }

            await DidClose(queue, workspace.CurrentSolution, CreateDidCloseTextDocumentParams(caretLocation));

            Assert.Empty(queue.GetTestAccessor().GetTrackedTexts());
        }

        [Fact]
        public async Task LinkedDocuments_AllTextChanged()
        {
            var workspaceXml =
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\C.cs"">{|caret:|}</Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document IsLinkFile=""true"" LinkFilePath=""C:\C.cs"" LinkAssemblyName=""CSProj1""></Document>
    </Project>
</Workspace>";

            using var workspace = CreateXmlTestWorkspace(workspaceXml, out var locations);
            var caretLocation = locations["caret"].Single();

            var initialText =
@"class A
{
    void M()
    {
        
    }
}";
            var updatedText =
@"class A
{
    void M()
    {
        // hi there
    }
}";

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            await DidOpen(queue, workspace.CurrentSolution, CreateDidOpenTextDocumentParams(caretLocation, initialText));

            Assert.Equal(1, queue.GetTestAccessor().GetTrackedTexts().Count);

            await DidChange(queue, workspace.CurrentSolution, CreateDidChangeTextDocumentParams(caretLocation.Uri, (4, 8, "// hi there")));

            var solution = await GetLSPSolution(queue, caretLocation.Uri);

            foreach (var document in solution.Projects.First().Documents)
            {
                Assert.Equal(updatedText, document.GetTextSynchronously(CancellationToken.None).ToString());
            }

            await DidClose(queue, workspace.CurrentSolution, CreateDidCloseTextDocumentParams(caretLocation));

            Assert.Empty(queue.GetTestAccessor().GetTrackedTexts());
        }

        private static Task<Solution> GetLSPSolution(Handler.RequestExecutionQueue queue, Uri uri)
        {
            return queue.ExecuteAsync(false, new GetLSPSolutionHandler(), uri, new ClientCapabilities(), null, "test/getLSPSolution", CancellationToken.None);
        }

        private class GetLSPSolutionHandler : IRequestHandler<Uri, Solution>
        {
            public TextDocumentIdentifier? GetTextDocumentIdentifier(Uri request)
                => new TextDocumentIdentifier { Uri = request };

            public Task<Solution> HandleRequestAsync(Uri request, RequestContext context, CancellationToken cancellationToken)
                => Task.FromResult(context.Solution);
        }
    }
}
