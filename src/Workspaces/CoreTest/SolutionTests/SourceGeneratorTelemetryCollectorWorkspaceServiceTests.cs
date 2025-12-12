// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SourceGeneratorTelemetry;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;
using static Microsoft.CodeAnalysis.UnitTests.SolutionTestHelpers;
using static Microsoft.CodeAnalysis.UnitTests.SolutionUtilities;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed class SourceGeneratorTelemetryCollectorWorkspaceServiceTests
{
    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1675665")]
    public async Task WithReferencesMethodCorrectlyUpdatesWithEqualReferences()
    {
        using var workspace = CreateWorkspace(additionalParts: [typeof(TestSourceGeneratorTelemetryCollectorWorkspaceServiceFactory)]);

        var nonExistentFilePath = "Z:\\" + Guid.NewGuid().ToString();
        var analyzerReference = new TestGeneratorReference(new SingleFileTestGenerator("// Hello World"), analyzerFilePath: nonExistentFilePath);

        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference);

        _ = await project.GetCompilationAsync();
    }

    [ExportWorkspaceServiceFactory(typeof(ISourceGeneratorTelemetryCollectorWorkspaceService)), Shared]
    [PartNotDiscoverable]
    public sealed class TestSourceGeneratorTelemetryCollectorWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestSourceGeneratorTelemetryCollectorWorkspaceServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new SourceGeneratorTelemetryCollectorWorkspaceService();
        }
    }
}
