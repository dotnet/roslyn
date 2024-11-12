// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Diagnostics)]
public class DiagnosticDataTests
{
    [Fact]
    public async Task DiagnosticData_GetText()
    {
        var code = "";
        await VerifyTextSpanAsync(code, 10, 10, 20, 20, new TextSpan(0, 0));
    }

    [Fact]
    public async Task DiagnosticData_GetText1()
    {
        var code = @"
";

        await VerifyTextSpanAsync(code, 30, 30, 40, 40, new TextSpan(code.Length, 0));
    }

    [Fact]
    public async Task DiagnosticData_GetText2()
    {
        var code = @"
";

        await VerifyTextSpanAsync(code, 0, 30, 40, 40, new TextSpan(code.Length, 0));
    }

    [Fact]
    public async Task DiagnosticData_GetText3()
    {
        var code = @"
";

        await VerifyTextSpanAsync(code, 0, 30, 0, 40, new TextSpan(code.Length, 0));
    }

    [Fact]
    public async Task DiagnosticData_GetText4()
    {
        var code = @"
";

        await VerifyTextSpanAsync(code, 1, 30, 1, 40, new TextSpan(code.Length, 0));
    }

    [Fact]
    public async Task DiagnosticData_GetText5()
    {
        var code = @"
";

        await VerifyTextSpanAsync(code, 1, 30, 1, 40, new TextSpan(code.Length, 0));
    }

    [Fact]
    public async Task DiagnosticData_GetText6()
    {
        var code = @"
";

        await VerifyTextSpanAsync(code, 1, 30, 2, 40, new TextSpan(code.Length, 0));
    }

    [Fact]
    public async Task DiagnosticData_GetText7()
    {
        var code = @"
";

        await VerifyTextSpanAsync(code, 1, 0, 1, 2, new TextSpan(code.Length, 0));
    }

    [Fact]
    public async Task DiagnosticData_GetText8()
    {
        var code = @"
namespace B
{
    class A
    {
    }
}
";

        await VerifyTextSpanAsync(code, 3, 10, 3, 11, new TextSpan(28, 1));
    }

    private static async Task VerifyTextSpanAsync(string code, int startLine, int startColumn, int endLine, int endColumn, TextSpan span)
    {
        using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures);
        var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", code);

        var data = new DiagnosticData(
            id: "test1",
            category: "Test",
            message: "test1 message",
            severity: DiagnosticSeverity.Info,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: false,
            warningLevel: 1,
            projectId: document.Project.Id,
            customTags: ImmutableArray<string>.Empty,
            properties: ImmutableDictionary<string, string>.Empty,
            location: new DiagnosticDataLocation(new("originalFile1", new(startLine, startColumn), new(endLine, endColumn)), document.Id),
            language: document.Project.Language);

        var text = await document.GetTextAsync();
        var actual = data.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text);

        Assert.Equal(span, actual);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46377")]
    public async Task DiagnosticData_ExternalAdditionalLocationIsPreserved()
    {
        using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures);

        var additionalDocument = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp)
            .AddDocument("test.cs", "", filePath: "test.cs")
            .Project.AddAdditionalDocument("AdditionalDocument.txt", "First line in file", filePath: "AdditionalDocument.txt");
        var document = additionalDocument.Project.Documents.Single();

        var externalAdditionalLocation = new DiagnosticDataLocation(
            new(additionalDocument.Name, new(0, 0), new(0, 1)), additionalDocument.Id);

        var diagnosticData = new DiagnosticData(
            id: "test1",
            category: "Test",
            message: "test1 message",
            severity: DiagnosticSeverity.Info,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            warningLevel: 1,
            projectId: document.Project.Id,
            customTags: ImmutableArray<string>.Empty,
            properties: ImmutableDictionary<string, string>.Empty,
            location: new DiagnosticDataLocation(new FileLinePositionSpan(document.FilePath, span: default), document.Id),
            additionalLocations: ImmutableArray.Create(externalAdditionalLocation),
            language: document.Project.Language);

        var diagnostic = await diagnosticData.ToDiagnosticAsync(document.Project, CancellationToken.None);
        var roundTripDiagnosticData = DiagnosticData.Create(diagnostic, document);

        var roundTripAdditionalLocation = Assert.Single(roundTripDiagnosticData.AdditionalLocations);
        Assert.Equal(externalAdditionalLocation.DocumentId, roundTripAdditionalLocation.DocumentId);
        Assert.Equal(externalAdditionalLocation.UnmappedFileSpan, roundTripAdditionalLocation.UnmappedFileSpan);
    }

    [Fact]
    public async Task DiagnosticData_SourceGeneratedDocumentLocationIsPreserved()
    {
        var content = @"
namespace B
{
    class A
    {
    }
}
";
        using var workspace = TestWorkspace.CreateCSharp(files: [], sourceGeneratedFiles: [content], composition: EditorTestCompositions.EditorFeatures);
        var hostDocument = workspace.Documents.Single();
        Assert.True(hostDocument.IsSourceGenerated);

        var documentId = hostDocument.Id;
        var project = workspace.CurrentSolution.GetRequiredProject(documentId.ProjectId);
        var document = await project.GetSourceGeneratedDocumentAsync(documentId, CancellationToken.None);

        await VerifyTextSpanAsync(content, 3, 10, 3, 11, new TextSpan(28, 1));
        var location = new DiagnosticDataLocation(
            new(document.FilePath, new(3, 10), new(3, 11)), documentId);

        var diagnosticData = new DiagnosticData(
            id: "test1",
            category: "Test",
            message: "test1 message",
            severity: DiagnosticSeverity.Info,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            warningLevel: 1,
            projectId: documentId.ProjectId,
            customTags: ImmutableArray<string>.Empty,
            properties: ImmutableDictionary<string, string>.Empty,
            location: location,
            additionalLocations: ImmutableArray<DiagnosticDataLocation>.Empty,
            language: project.Language);

        var diagnostic = await diagnosticData.ToDiagnosticAsync(project, CancellationToken.None);
        var roundTripDiagnosticData = DiagnosticData.Create(diagnostic, document);

        var roundTripLocation = roundTripDiagnosticData.DataLocation;
        Assert.NotNull(roundTripDiagnosticData.DataLocation);
        Assert.Equal(location.DocumentId, roundTripLocation.DocumentId);
        Assert.Equal(location.UnmappedFileSpan, roundTripLocation.UnmappedFileSpan);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/1676229")]
    public async Task DiagnosticData_SourceFileAdditionalLocationIsPreserved(bool testDifferentProject, bool testRemovedDocument)
    {
        using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures);

        var firstDocument = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp)
            .AddDocument("test.cs", "class C1 { }", filePath: "test.cs");
        Document secondDocument;
        if (testDifferentProject)
        {
            secondDocument = firstDocument.Project.Solution.AddProject("TestProject2", "TestProject2", LanguageNames.CSharp)
                .AddDocument("test2.cs", "class C2 { }", filePath: "test2.cs");
        }
        else
        {
            secondDocument = firstDocument.Project.AddDocument("test2.cs", "class C2 { }", filePath: "test2.cs");
        }

        firstDocument = secondDocument.Project.Solution.GetRequiredDocument(firstDocument.Id);

        var additionalLocation = new DiagnosticDataLocation(
            new(secondDocument.Name, new(0, 0), new(0, 1)), secondDocument.Id);

        var diagnosticData = new DiagnosticData(
            id: "test1",
            category: "Test",
            message: "test1 message",
            severity: DiagnosticSeverity.Info,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            warningLevel: 1,
            projectId: firstDocument.Project.Id,
            customTags: ImmutableArray<string>.Empty,
            properties: ImmutableDictionary<string, string>.Empty,
            location: new DiagnosticDataLocation(new FileLinePositionSpan(firstDocument.FilePath, span: default), firstDocument.Id),
            additionalLocations: ImmutableArray.Create(additionalLocation),
            language: firstDocument.Project.Language);

        var diagnostic = await diagnosticData.ToDiagnosticAsync(firstDocument.Project, CancellationToken.None);

        if (testRemovedDocument)
        {
            firstDocument = firstDocument.Project.Solution
                .RemoveDocument(secondDocument.Id)
                .GetRequiredDocument(firstDocument.Id);
        }

        var roundTripDiagnosticData = DiagnosticData.Create(diagnostic, firstDocument);

        var roundTripAdditionalLocation = Assert.Single(roundTripDiagnosticData.AdditionalLocations);
        var expectedAdditionalDocumentId = !testRemovedDocument ? additionalLocation.DocumentId : null;
        Assert.Equal(expectedAdditionalDocumentId, roundTripAdditionalLocation.DocumentId);
        Assert.Equal(additionalLocation.UnmappedFileSpan, roundTripAdditionalLocation.UnmappedFileSpan);
    }
}
