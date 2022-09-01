// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class CompileTimeSolutionProviderTests
    {
        [Theory]
        [InlineData("razor")]
        [InlineData("cshtml")]
        public async Task TryGetCompileTimeDocumentAsync(string kind)
        {
            var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);
            var projectId = ProjectId.CreateNewId();

            var projectFilePath = Path.Combine(TempRoot.Root, "a.csproj");
            var additionalFilePath = Path.Combine(TempRoot.Root, "a", $"X.{kind}");
            var designTimeFilePath = Path.Combine(TempRoot.Root, "a", $"X.{kind}.g.cs");

            var generator = new TestSourceGenerator() { ExecuteImpl = context => context.AddSource($"a_X_{kind}.g.cs", "") };
            var sourceGeneratedPathPrefix = Path.Combine(typeof(TestSourceGenerator).Assembly.GetName().Name, typeof(TestSourceGenerator).FullName);
            var analyzerConfigId = DocumentId.CreateNewId(projectId);
            var documentId = DocumentId.CreateNewId(projectId);
            var additionalDocumentId = DocumentId.CreateNewId(projectId);
            var designTimeDocumentId = DocumentId.CreateNewId(projectId);

            var designTimeSolution = workspace.CurrentSolution.
                AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "proj", "proj", LanguageNames.CSharp, filePath: projectFilePath)).
                WithProjectMetadataReferences(projectId, TargetFrameworkUtil.GetReferences(TargetFramework.NetStandard20)).
                AddAnalyzerReference(projectId, new TestGeneratorReference(generator)).
                AddAdditionalDocument(additionalDocumentId, "additional", SourceText.From(""), filePath: additionalFilePath).
                AddAnalyzerConfigDocument(analyzerConfigId, "config", SourceText.From(""), filePath: "RazorSourceGenerator.razorencconfig").
                AddDocument(documentId, "a.cs", "").
                AddDocument(DocumentInfo.Create(
                    designTimeDocumentId,
                    name: "a",
                    folders: Array.Empty<string>(),
                    sourceCodeKind: SourceCodeKind.Regular,
                    loader: null,
                    filePath: designTimeFilePath,
                    isGenerated: true,
                    designTimeOnly: true,
                    documentServiceProvider: null));

            var designTimeDocument = designTimeSolution.GetRequiredDocument(designTimeDocumentId);

            var provider = workspace.Services.GetRequiredService<ICompileTimeSolutionProvider>();
            var compileTimeSolution = provider.GetCompileTimeSolution(designTimeSolution);

            Assert.False(compileTimeSolution.ContainsAnalyzerConfigDocument(analyzerConfigId));
            Assert.False(compileTimeSolution.ContainsDocument(designTimeDocumentId));
            Assert.True(compileTimeSolution.ContainsDocument(documentId));

            var sourceGeneratedDoc = (await compileTimeSolution.Projects.Single().GetSourceGeneratedDocumentsAsync()).Single();

            var compileTimeDocument = await CompileTimeSolutionProvider.TryGetCompileTimeDocumentAsync(designTimeDocument, compileTimeSolution, CancellationToken.None, sourceGeneratedPathPrefix);
            Assert.Same(sourceGeneratedDoc, compileTimeDocument);

            var actualDesignTimeDocumentIds = await CompileTimeSolutionProvider.GetDesignTimeDocumentsAsync(
                compileTimeSolution, ImmutableArray.Create(documentId, sourceGeneratedDoc.Id), designTimeSolution, CancellationToken.None, sourceGeneratedPathPrefix);

            AssertEx.Equal(new[] { documentId, designTimeDocumentId }, actualDesignTimeDocumentIds);
        }
    }
}
