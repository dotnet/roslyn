// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.References;
public class LspOptionsTests : AbstractLanguageServerProtocolTests
{
    public LspOptionsTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override TestComposition Composition => EditorTestCompositions.LanguageServerProtocol
        .AddParts(typeof(TestDocumentTrackingService))
        .AddParts(typeof(TestWorkspaceRegistrationService));

    [Theory, CombinatorialData]
    public async Task TestCanRetrieveCSharpOptionsWithOnlyLspLayer(bool mutatingLspWorkspace)
    {
        var markup = "";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var globalOptions = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IGlobalOptionService>();
        var project = testLspServer.GetCurrentSolution().Projects.Single().Services;
        Assert.NotNull(globalOptions.GetAddImportPlacementOptions(project));
        Assert.NotNull(globalOptions.GetCodeGenerationOptions(project));
        Assert.NotNull(globalOptions.GetCodeStyleOptions(project));
        Assert.NotNull(globalOptions.GetSyntaxFormattingOptions(project));
        Assert.NotNull(globalOptions.GetSimplifierOptions(project));
    }

    [Theory, CombinatorialData]
    public async Task TestCanRetrieveVisualBasicOptionsWithOnlyLspLayer(bool mutatingLspWorkspace)
    {
        var markup = "";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        var globalOptions = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IGlobalOptionService>();
        var project = testLspServer.GetCurrentSolution().Projects.Single().Services;
        Assert.NotNull(globalOptions.GetAddImportPlacementOptions(project));
        Assert.NotNull(globalOptions.GetCodeGenerationOptions(project));
        Assert.NotNull(globalOptions.GetCodeStyleOptions(project));
        Assert.NotNull(globalOptions.GetSyntaxFormattingOptions(project));
        Assert.NotNull(globalOptions.GetSimplifierOptions(project));
    }
}
