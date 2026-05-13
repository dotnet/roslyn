// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostRoslynGoToDefTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task Component()
        => VerifyGoToDefinitionAsync(
            csharpFile: """
                using SomeProject;
                using Microsoft.AspNetCore.Components;

                public class C
                {
                    private string componentName = nameof(Comp$$onent);
                }
                """,
            razorFile: """
                This is a Razor document.

                <Component></Component>

                @code
                {
                    private string componentnName = nameof(Component);
                }

                The end.
                """);

    private protected override TestComposition ConfigureLocalComposition(TestComposition composition)
    {
        return composition
            .AddParts(typeof(RazorSourceGeneratedDocumentSpanMappingService))
            .AddParts(typeof(ExportableRemoteServiceInvoker));
    }

    private async Task VerifyGoToDefinitionAsync(
        TestCode csharpFile,
        TestCode razorFile)
    {
        var razorDocument = CreateProjectAndRazorDocument(razorFile.Text, documentFilePath: FilePath("Component.razor"), additionalFiles: [(FilePath("File.cs"), csharpFile.Text)]);
        var project = razorDocument.Project;
        var csharpDocument = project.Documents.First();

        var sourceText = await csharpDocument.GetTextAsync(DisposalToken);
        var csharpPosition = sourceText.GetLinePosition(csharpFile.Position);

        // Normally in cohosting tests we directly construct and invoke the endpoints, but in this scenario Roslyn is going to do it
        // using a service in their MEF composition, so we have to jump through an extra hook to hook up our test invoker.
        var invoker = LocalExportProvider.AssumeNotNull().GetExportedValue<ExportableRemoteServiceInvoker>();
        invoker.SetInvoker(RemoteServiceInvoker);

        var definition = await GoToDefinition.GetDefinitionsAsync(LocalWorkspace, csharpDocument, typeOnly: false, csharpPosition, DisposalToken);
        Assert.NotNull(definition);

        var def = Assert.Single(definition);
        Assert.Equal(razorDocument.CreateDocumentUri(), def.DocumentUri);
    }
}
