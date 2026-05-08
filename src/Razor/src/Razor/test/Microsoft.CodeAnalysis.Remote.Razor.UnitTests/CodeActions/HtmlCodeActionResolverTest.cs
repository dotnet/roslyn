// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.NET.Sdk.Razor.SourceGenerators;
using Moq;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

public class HtmlCodeActionResolverTest
{
    [Fact]
    public async Task ResolveAsync_RemapsAndFixesEdits()
    {
        // Arrange
        var contents = "[|<$$h1>Goo @(DateTime.Now) Bar</h1>|]";
        TestFileMarkupParser.GetPositionAndSpan(contents, out contents, out _, out var span);

        var documentPath = @"C:\Test.razor";
        var documentUri = new Uri(documentPath);
        var (context, sourceText, workspace) = CreateDocumentContext(documentUri, documentPath, contents);
        using var workspaceLifetime = workspace;

        var razorEditServiceMock = new StrictMock<IRazorEditService>();
        razorEditServiceMock
            .Setup(x => x.MapWorkspaceEditAsync(It.IsAny<IDocumentSnapshot>(), It.IsAny<WorkspaceEdit>(), It.IsAny<CancellationToken>()))
            .Callback<IDocumentSnapshot, WorkspaceEdit, CancellationToken>((snapshot, edit, _) =>
            {
                Assert.IsType<RemoteDocumentSnapshot>(snapshot);
                var textDocumentEdit = edit.EnumerateTextDocumentEdits().First();
                textDocumentEdit.TextDocument.DocumentUri = new(documentPath);
                textDocumentEdit.Edits = [LspFactory.CreateTextEdit(sourceText.GetRange(span), "Goo /*~~~~~~~~~~~*/ Bar")];
            })
            .Returns(Task.CompletedTask);

        var resolver = new HtmlCodeActionResolver(razorEditServiceMock.Object);

        var codeAction = new RazorVSInternalCodeAction()
        {
            Name = "Test",
            Edit = new WorkspaceEdit
            {
                DocumentChanges = new TextDocumentEdit[]
                {
                    new()
                    {
                        TextDocument = new OptionalVersionedTextDocumentIdentifier
                        {
                            DocumentUri = new(new Uri(@"C:\Test.razor.html")),
                        },
                        Edits = [LspFactory.CreateTextEdit(position: (0, 0), "Goo")]
                    }
                }
            }
        };

        // Act
        var action = await resolver.ResolveAsync(context, codeAction, CancellationToken.None);

        // Assert
        Assert.NotNull(action.Edit);
        var documentEdits = action.Edit.EnumerateTextDocumentEdits().ToArray();
        Assert.NotEmpty(documentEdits);
        Assert.Equal(documentUri.AbsolutePath, documentEdits[0].TextDocument.DocumentUri.GetRequiredParsedUri().AbsolutePath);

        var text = SourceText.From(contents);
        var changed = text.WithChanges(documentEdits[0].Edits.Select(e => text.GetTextChange((TextEdit)e)));
        Assert.Equal("Goo @(DateTime.Now) Bar", changed.ToString());
    }

    private static (RemoteDocumentContext Context, SourceText SourceText, AdhocWorkspace Workspace) CreateDocumentContext(Uri documentUri, string filePath, string text)
    {
        var sourceText = SourceText.From(text);

        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        var projectFilePath = @"C:\TestProject.csproj";
        var projectBasePath = Path.GetDirectoryName(projectFilePath)!;
        var targetPath = Path.GetFileName(filePath);

        var projectInfo = ProjectInfo
            .Create(
                projectId,
                VersionStamp.Create(),
                name: "TestProject",
                assemblyName: "TestProject",
                language: LanguageNames.CSharp,
                filePath: projectFilePath,
                parseOptions: CSharpParseOptions.Default.WithFeatures([new("use-roslyn-tokenizer", "true")]),
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(AspNet80.ReferenceInfos.All.Select(static r => r.Reference))
            .WithDefaultNamespace("ASP")
            .WithAnalyzerReferences([new AnalyzerFileReference(typeof(RazorSourceGenerator).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile)]);

        var globalConfig = $$"""
            is_global = true

            build_property.RazorLangVersion = {{RazorLanguageVersion.Preview}}
            build_property.RazorConfiguration = {{FallbackRazorConfiguration.Latest.ConfigurationName}}
            build_property.RootNamespace = ASP

            # This mirrors the Razor SDK and is required for the host output path used by GeneratorRunResult.
            build_property.SuppressRazorSourceGenerator = true
            build_property.MSBuildProjectDirectory = {{projectBasePath}}

            [{{filePath.Replace('\\', '/')}}]
            build_metadata.AdditionalFiles.TargetPath = {{Convert.ToBase64String(Encoding.UTF8.GetBytes(targetPath))}}
            """;

        var solution = workspace.CurrentSolution
            .AddProject(projectInfo)
            .AddAdditionalDocument(documentId, Path.GetFileName(filePath), sourceText, filePath: filePath)
            .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), ".globalconfig", SourceText.From(globalConfig), filePath: Path.Combine(projectBasePath, ".globalconfig"));

        workspace.TryApplyChanges(solution);
        var document = workspace.CurrentSolution.GetAdditionalDocument(documentId)!;
        var snapshotManager = new RemoteSnapshotManager(new RemoteFilePathService(), NoOpTelemetryReporter.Instance);
        var snapshot = snapshotManager.GetSnapshot(document);

        return (new RemoteDocumentContext(documentUri, snapshot), sourceText, workspace);
    }
}
