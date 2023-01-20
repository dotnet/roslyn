// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.UnitTests.SolutionTestHelpers;

namespace Microsoft.CodeAnalysis.UnitTests.Options
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    public class DiagnosticSeverityOptionsTests : TestBase
    {
        [Theory, CombinatorialData]
        public async Task TestGetEffectiveSeverityFromProjectOptions(bool testGlobalConfig, bool testBulkConfiguration)
        {
            using var workspace = CreateWorkspace();

            var projectId = ProjectId.CreateNewId();
            var project = workspace.CurrentSolution
                .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp)
                .AddDocument(DocumentId.CreateNewId(projectId), "goo.cs", SourceText.From("public class Goo { }", Encoding.UTF8, SourceHashAlgorithms.Default), filePath: @"z:\\goo.cs")
                .Projects.Single();

            var descriptor = new DiagnosticDescriptor("ID1000", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            var editorConfigHeader = testGlobalConfig ? "is_global = true" : "[*.cs]";
            var editorConfigEntry = testBulkConfiguration
                ? "dotnet_analyzer_diagnostic.severity = error"
                : "dotnet_diagnostic.ID1000.severity = error";
            var analyzerConfigText = $@"
{editorConfigHeader}
{editorConfigEntry}
";
            project = project.AddAnalyzerConfigDocument(".editorconfig", SourceText.From(analyzerConfigText), filePath: @"z:\\.editorconfig").Project;

            workspace.TryApplyChanges(project.Solution);

            var document = project.Documents.Single();
            var tree = await document.GetSyntaxTreeAsync();
            var optionsProvider = project.AnalyzerOptions.AnalyzerConfigOptionsProvider;
            var analyzerConfigOptions = testGlobalConfig ? optionsProvider.GlobalOptions : optionsProvider.GetOptions(tree);
            Assert.Equal(ReportDiagnostic.Error, descriptor.GetEffectiveSeverity(analyzerConfigOptions));

            var compilation = await project.GetCompilationAsync();
            Assert.Equal(ReportDiagnostic.Error, descriptor.GetEffectiveSeverity(compilation.Options, tree, project.AnalyzerOptions));
        }
    }
}
