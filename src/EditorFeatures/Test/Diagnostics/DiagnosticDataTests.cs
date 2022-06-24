// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [UseExportProvider]
    public class DiagnosticDataTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText()
        {
            var code = "";
            await VerifyTextSpanAsync(code, 10, 10, 20, 20, new TextSpan(0, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText1()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, 30, 30, 40, 40, new TextSpan(code.Length, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText2()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, -1, 30, 40, 40, new TextSpan(0, code.Length));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText3()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, -1, 30, -1, 40, new TextSpan(0, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText4()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, 1, 30, -1, 40, new TextSpan(code.Length, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText5()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, 1, 30, 1, 40, new TextSpan(code.Length, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText6()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, 1, 30, 2, 40, new TextSpan(code.Length, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText7()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, 1, 0, 1, 2, new TextSpan(code.Length, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
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
                location: new DiagnosticDataLocation(document.Id, null, "originalFile1", startLine, startColumn, endLine, endColumn),
                language: document.Project.Language);

            var text = await document.GetTextAsync();
            var actual = DiagnosticData.GetExistingOrCalculatedTextSpan(data.DataLocation, text);

            Assert.Equal(span, actual);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        [WorkItem(46377, "https://github.com/dotnet/roslyn/issues/46377")]
        public async Task DiagnosticData_ExternalAdditionalLocationIsPreserved()
        {
            using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures);

            var additionalDocument = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument("test.cs", "")
                .Project.AddAdditionalDocument("AdditionalDocument.txt", "First line in file", filePath: "AdditionalDocument.txt");
            var document = additionalDocument.Project.Documents.Single();

            var externalAdditionalLocation = new DiagnosticDataLocation(
                additionalDocument.Id, sourceSpan: new TextSpan(0, 1), originalFilePath: additionalDocument.Name,
                originalStartLine: 0, originalStartColumn: 0, originalEndLine: 0, originalEndColumn: 1);

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
                location: new DiagnosticDataLocation(document.Id),
                additionalLocations: ImmutableArray.Create(externalAdditionalLocation),
                language: document.Project.Language);

            var diagnostic = await diagnosticData.ToDiagnosticAsync(document.Project, CancellationToken.None);
            var roundTripDiagnosticData = DiagnosticData.Create(diagnostic, document);

            var roundTripAdditionalLocation = Assert.Single(roundTripDiagnosticData.AdditionalLocations);
            Assert.Equal(externalAdditionalLocation.DocumentId, roundTripAdditionalLocation.DocumentId);
            Assert.Equal(externalAdditionalLocation.SourceSpan, roundTripAdditionalLocation.SourceSpan);
            Assert.Equal(externalAdditionalLocation.OriginalFilePath, roundTripAdditionalLocation.OriginalFilePath);
            Assert.Equal(externalAdditionalLocation.OriginalStartLine, roundTripAdditionalLocation.OriginalStartLine);
            Assert.Equal(externalAdditionalLocation.OriginalStartColumn, roundTripAdditionalLocation.OriginalStartColumn);
            Assert.Equal(externalAdditionalLocation.OriginalEndLine, roundTripAdditionalLocation.OriginalEndLine);
            Assert.Equal(externalAdditionalLocation.OriginalEndLine, roundTripAdditionalLocation.OriginalEndLine);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
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
            using var workspace = TestWorkspace.CreateCSharp(files: Array.Empty<string>(), sourceGeneratedFiles: new[] { content }, composition: EditorTestCompositions.EditorFeatures);
            var hostDocument = workspace.Documents.Single();
            Assert.True(hostDocument.IsSourceGenerated);

            var documentId = hostDocument.Id;
            var project = workspace.CurrentSolution.GetRequiredProject(documentId.ProjectId);
            var document = await project.GetSourceGeneratedDocumentAsync(documentId, CancellationToken.None);

            await VerifyTextSpanAsync(content, 3, 10, 3, 11, new TextSpan(28, 1));
            var location = new DiagnosticDataLocation(
                documentId, sourceSpan: new TextSpan(28, 1), originalFilePath: document.FilePath,
                originalStartLine: 3, originalStartColumn: 10, originalEndLine: 3, originalEndColumn: 11);

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
            Assert.Equal(location.SourceSpan, roundTripLocation.SourceSpan);
            Assert.Equal(location.OriginalFilePath, roundTripLocation.OriginalFilePath);
            Assert.Equal(location.OriginalStartLine, roundTripLocation.OriginalStartLine);
            Assert.Equal(location.OriginalStartColumn, roundTripLocation.OriginalStartColumn);
            Assert.Equal(location.OriginalEndLine, roundTripLocation.OriginalEndLine);
            Assert.Equal(location.OriginalEndLine, roundTripLocation.OriginalEndLine);
        }
    }
}
