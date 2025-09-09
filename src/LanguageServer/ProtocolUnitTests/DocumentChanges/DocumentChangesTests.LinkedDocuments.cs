// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.DocumentChanges;

public sealed partial class DocumentChangesTests
{
    [Theory, CombinatorialData]
    public async Task LinkedDocuments_AllTracked(bool mutatingLspWorkspace)
    {
        var documentText = "class C { }";
        var workspaceXml =
            $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1">
                    <Document FilePath="C:\C.cs">{{documentText}}{|caret:|}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2">
                    <Document IsLinkFile="true" LinkFilePath="C:\C.cs" LinkAssemblyName="CSProj1"></Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateXmlTestLspServerAsync(workspaceXml, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();

        await DidOpen(testLspServer, caretLocation.DocumentUri);

        var trackedDocuments = testLspServer.GetTrackedTexts();
        Assert.Equal(1, trackedDocuments.Length);

        var solution = await GetLSPSolutionAsync(testLspServer, caretLocation.DocumentUri).ConfigureAwait(false);

        foreach (var document in solution.Projects.First().Documents)
        {
            Assert.Equal(documentText, document.GetTextSynchronously(CancellationToken.None).ToString());
        }

        await DidClose(testLspServer, caretLocation.DocumentUri);

        Assert.Empty(testLspServer.GetTrackedTexts());
    }

    [Theory, CombinatorialData]
    public async Task LinkedDocuments_AllTextChanged(bool mutatingLspWorkspace)
    {
        var initialText =
            """
            class A
            {
                void M()
                {
                    {|caret:|}
                }
            }
            """;
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1">
                    <Document FilePath="C:\C.cs">{initialText}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2">
                    <Document IsLinkFile="true" LinkFilePath="C:\C.cs" LinkAssemblyName="CSProj1"></Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateXmlTestLspServerAsync(workspaceXml, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        await DidOpen(testLspServer, caretLocation.DocumentUri);

        Assert.Equal(1, testLspServer.GetTrackedTexts().Length);

        await DidChange(testLspServer, caretLocation.DocumentUri, (4, 8, "// hi there"));

        var solution = await GetLSPSolutionAsync(testLspServer, caretLocation.DocumentUri).ConfigureAwait(false);

        foreach (var document in solution.Projects.First().Documents)
        {
            Assert.Equal("""
                class A
                {
                    void M()
                    {
                        // hi there
                    }
                }
                """, document.GetTextSynchronously(CancellationToken.None).ToString());
        }

        await DidClose(testLspServer, caretLocation.DocumentUri);

        Assert.Empty(testLspServer.GetTrackedTexts());
    }

    private static async Task<Solution> GetLSPSolutionAsync(TestLspServer testLspServer, DocumentUri uri)
    {
        var (_, _, lspDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new TextDocumentIdentifier { DocumentUri = uri }, CancellationToken.None).ConfigureAwait(false);
        Contract.ThrowIfNull(lspDocument);
        return lspDocument.Project.Solution;
    }
}
