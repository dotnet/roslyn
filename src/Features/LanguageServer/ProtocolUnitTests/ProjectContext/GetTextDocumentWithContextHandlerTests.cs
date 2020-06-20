﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.ProjectContext
{
    public class GetTextDocumentWithContextHandlerTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task SingleDocumentReturnsSingleContext()
        {
            var workspaceXml =
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj"">
        <Document FilePath = ""C:\C.cs"">{|caret:|}</Document>
    </Project>
</Workspace>";

            using var workspace = CreateXmlTestWorkspace(workspaceXml, out var locations);
            var documentUri = locations["caret"].Single().Uri;
            var result = await RunGetProjectContext(workspace.CurrentSolution, documentUri);

            Assert.NotNull(result);
            Assert.Equal(0, result!.DefaultIndex);
            var context = Assert.Single(result.ProjectContexts);

            Assert.Equal(ProtocolConversions.ProjectIdToProjectContextId(workspace.CurrentSolution.ProjectIds.Single()), context.Id);
            Assert.Equal(LSP.ProjectContextKind.CSharp, context.Kind);
            Assert.Equal("CSProj", context.Label);
        }

        [Fact]
        public async Task MultipleDocumentsReturnsMultipleContexts()
        {
            var workspaceXml =
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\C.cs"">{|caret:|}</Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document IsLinkFile=""true"" LinkFilePath=""C:\C.cs"" LinkAssemblyName=""CSProj1"">{|caret:|}</Document>
    </Project>
</Workspace>";

            using var workspace = CreateXmlTestWorkspace(workspaceXml, out var locations);
            var documentUri = locations["caret"].Single().Uri;
            var result = await RunGetProjectContext(workspace.CurrentSolution, documentUri);

            Assert.NotNull(result);

            Assert.Collection(result!.ProjectContexts.OrderBy(c => c.Label),
                c => Assert.Equal("CSProj1", c.Label),
                c => Assert.Equal("CSProj2", c.Label));
        }

        [Fact]
        public async Task SwitchingContextsChangesDefaultContext()
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

            // Ensure the documents are open so we can change contexts
            foreach (var document in workspace.Documents)
            {
                _ = document.GetOpenTextContainer();
            }

            var documentUri = locations["caret"].Single().Uri;

            foreach (var project in workspace.CurrentSolution.Projects)
            {
                workspace.SetDocumentContext(project.DocumentIds.Single());
                var result = await RunGetProjectContext(workspace.CurrentSolution, documentUri);

                Assert.Equal(ProtocolConversions.ProjectIdToProjectContextId(project.Id), result!.ProjectContexts[result.DefaultIndex].Id);
                Assert.Equal(project.Name, result!.ProjectContexts[result.DefaultIndex].Label);
            }
        }

        private static async Task<LSP.ActiveProjectContexts?> RunGetProjectContext(Solution solution, Uri uri)
            => await GetLanguageServer(solution).ExecuteRequestAsync<LSP.GetTextDocumentWithContextParams, LSP.ActiveProjectContexts?>(LSP.MSLSPMethods.ProjectContextsName,
                CreateGetProjectContextParams(uri), new LSP.ClientCapabilities(), clientName: null, cancellationToken: CancellationToken.None);

        private static LSP.GetTextDocumentWithContextParams CreateGetProjectContextParams(Uri uri)
            => new LSP.GetTextDocumentWithContextParams()
            {
                TextDocument = new LSP.TextDocumentItem { Uri = uri }
            };
    }
}
