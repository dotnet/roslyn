// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Mef;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ExternalHandlers = Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostRoslynCodeActionTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task GenerateMethod_NoCodeBlock()
        => VerifyCodeActionAsync(
            csharpFile: """
                using SomeProject;
                using Microsoft.AspNetCore.Components;

                public class C
                {
                    private void M()
                    {
                        new Component().$$NewMethod();
                    }
                }
                """,
            razorFile: """
                This is a Razor document.

                <Component></Component>

                The end.
                """,
            expectedRazorFile: """
                @using System
                This is a Razor document.
                
                <Component></Component>
                
                The end.
                @code {
                    internal void NewMethod()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateMethod);

    [Fact]
    public async Task GenerateMethod_NoCodeBlock_CodeBlockBraceOnNextLine()
    {
        ClientSettingsManager.Update(ClientSettingsManager.GetClientSettings().AdvancedSettings with { CodeBlockBraceOnNextLine = true });

        await VerifyCodeActionAsync(
                csharpFile: """
                using SomeProject;
                using Microsoft.AspNetCore.Components;

                public class C
                {
                    private void M()
                    {
                        new Component().$$NewMethod();
                    }
                }
                """,
                razorFile: """
                This is a Razor document.

                <Component></Component>

                The end.
                """,
                expectedRazorFile: """
                @using System
                This is a Razor document.
                
                <Component></Component>
                
                The end.
                @code
                {
                    internal void NewMethod()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
                codeActionName: RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    [Fact]
    public Task GenerateMethod_ExistingCodeBlock()
        => VerifyCodeActionAsync(
            csharpFile: """
                using SomeProject;
                using Microsoft.AspNetCore.Components;

                public class C
                {
                    private void M()
                    {
                        new Component().$$NewMethod();
                    }
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
                """,
            expectedRazorFile: """
                @using System
                This is a Razor document.
                
                <Component></Component>
                
                @code
                {
                    private string componentnName = nameof(Component);

                    internal void NewMethod()
                    {
                        throw new NotImplementedException();
                    }
                }

                The end.
                """,
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateMethod);

    [Fact]
    public Task GenerateMethod_ExistingCodeBlock_UsesTabsWhenConfigured()
    {
        ClientSettingsManager.Update(new ClientSpaceSettings(IndentWithTabs: true, IndentSize: 4));

        return VerifyCodeActionAsync(
            csharpFile: """
                using SomeProject;
                using Microsoft.AspNetCore.Components;

                public class C
                {
                    private void M()
                    {
                        new Component().$$NewMethod();
                    }
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
                """,
            expectedRazorFile: """
                @using System
                This is a Razor document.
                
                <Component></Component>
                
                @code
                {
                    private string componentnName = nameof(Component);

                	internal void NewMethod()
                	{
                		throw new NotImplementedException();
                	}
                }

                The end.
                """,
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateMethod);
    }

    private protected override TestComposition ConfigureLocalComposition(TestComposition composition)
    {
        return composition
            .AddParts(typeof(RazorSourceGeneratedDocumentSpanMappingService))
            .AddParts(typeof(ExportableRemoteServiceInvoker));
    }

    private async Task VerifyCodeActionAsync(
        TestCode csharpFile,
        TestCode razorFile,
        TestCode expectedRazorFile,
        string codeActionName)
    {
        var razorDocument = CreateProjectAndRazorDocument(razorFile.Text, documentFilePath: FilePath("Component.razor"), additionalFiles: [(FilePath("File.cs"), csharpFile.Text)]);
        var project = razorDocument.Project;
        var csharpDocument = project.Documents.First();
        var solution = csharpDocument.Project.Solution;

        var sourceText = await csharpDocument.GetTextAsync(DisposalToken);
        var csharpPosition = sourceText.GetLinePosition(csharpFile.Position);

        // Normally in cohosting tests we directly construct and invoke the endpoints, but in this scenario Roslyn is going to do it
        // using a service in their MEF composition, so we have to jump through an extra hook to hook up our test invoker.
        var invoker = LocalExportProvider.AssumeNotNull().GetExportedValue<ExportableRemoteServiceInvoker>();
        invoker.SetInvoker(RemoteServiceInvoker);

        var request = new CodeActionParams
        {
            TextDocument = new() { DocumentUri = csharpDocument.CreateDocumentUri() },
            Range = new LspRange
            {
                Start = new Position(csharpPosition.Line, csharpPosition.Character),
                End = new Position(csharpPosition.Line, csharpPosition.Character)
            },
            Context = new CodeActionContext()
        };

        // We're really just pretending to do the same thing as Roslyn code action logic would do from a C# file in the IDE, by only getting
        // Roslyn code actions, resolving them, and applying edits through our span mapping service. The main test coverage this gives us is
        // use of the edit service without going through our formatter, which otherwise hides a lot of sins :)

        var codeActions = await ExternalHandlers.CodeActions.GetCodeActionsAsync(csharpDocument, request, supportsVSExtensions: true, DisposalToken);

        Assert.NotEmpty(codeActions);

        // Resolve expects data to round-trip through an LSP client, and be a JsonElement, so serialize. Conveniently, this makes it easier for
        // us to pull out the code action name without access to Roslyn's data type.
        foreach (var action in codeActions)
        {
            action.Data = JsonSerializer.SerializeToElement(action.Data);
        }

        var codeAction = codeActions.First(a => ((JsonElement)a.Data.AssumeNotNull()).TryGetProperty("CustomTags", out var value) &&
                value.Deserialize<string[]>() is [..] tags &&
                tags.Any(t => t == codeActionName));

        var resolvedCodeAction = await ExternalHandlers.CodeActions.ResolveCodeActionAsync(csharpDocument, codeAction, [], DisposalToken);

        var workspaceEdit = resolvedCodeAction.Edit.AssumeNotNull();

        var generatedDoc = await project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(razorDocument, DisposalToken);
        Assert.NotNull(generatedDoc);
        var generatedSourceText = await generatedDoc.GetTextAsync(DisposalToken);

        var modifiedGeneratedSourceText = generatedSourceText
            .WithChanges(
                workspaceEdit.EnumerateTextDocumentEdits()
                    .Where(e => e.TextDocument.DocumentUri.GetRequiredParsedUri() == generatedDoc.CreateUri())
                    .SelectMany(e => e.Edits)
                    .Select(e => generatedSourceText.GetTextChange((TextEdit)e)));

        // Normally in VS, TryApplyChanges would be called, and that calls into our edit mapping service.
        var modifiedGeneratedDoc = (SourceGeneratedDocument)generatedDoc.WithText(modifiedGeneratedSourceText);
        var mappingService = new RazorSourceGeneratedDocumentSpanMappingService(RemoteServiceInvoker);
        var changes = await mappingService.GetMappedTextChangesAsync(generatedDoc, modifiedGeneratedDoc, DisposalToken);

        var razorText = await razorDocument.GetTextAsync(DisposalToken);
        foreach (var change in changes)
        {
            Assert.Equal(razorDocument.FilePath, change.FilePath);
            razorText = razorText.WithChanges(change.TextChanges);
        }

        AssertEx.EqualOrDiff(expectedRazorFile.Text, razorText.ToString());
    }
}
