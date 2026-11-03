// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.MiscellaneousFiles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using CodeAction = Microsoft.CodeAnalysis.CodeActions.CodeAction;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions;

public sealed class DocumentFixAllTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task CapabilitiesAdvertiseFixAllAndResolveSupport(bool mutatingLspWorkspace)
    {
        var source = "";

        // Inspect initialization capabilities without needing any document diagnostics.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var options = server.GetServerCapabilities().CodeActionProvider!.Value.Second;

        // Advertise source.fixAll together with resolve support, since its edits are computed lazily.
        Assert.Contains(CodeActionKind.SourceFixAll, options.CodeActionKinds!);
        Assert.True(options.ResolveProvider);
    }

    [Theory, CombinatorialData]
    public async Task DiscoveryOffersDeferredFixAllForMatchingSourceFilters(
        bool mutatingLspWorkspace, [CombinatorialValues("source", "source.fixAll")] string only)
    {
        var source = "class C { {|caret:|} }";

        // A clean document proves that discovery does not depend on fixable diagnostics.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var actions = await GetActionsAsync(server, [only]);

        // Matching kinds return a deferred source action with no command or edit.
        var action = Assert.Single(actions, action => action.Kind == CodeActionKind.SourceFixAll);
        Assert.Null(action.Command);
        Assert.Null(action.Edit);

        // Resolve data identifies the aggregate action and its requested document.
        var data = Assert.IsType<JsonElement>(action.Data);
        Assert.True(data.GetProperty("FixAllInDocument").GetBoolean());
        Assert.False(data.TryGetProperty(nameof(CodeActionResolveData.Range), out _));
        Assert.Equal(server.GetLocations("caret").Single().DocumentUri,
            JsonSerializer.Deserialize<TextDocumentIdentifier>(data.GetProperty("TextDocument"), ProtocolConversions.LspJsonSerializerOptions)!.DocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task DiscoveryOmitsFixAllForNonMatchingSourceFilters(
        bool mutatingLspWorkspace,
        [CombinatorialValues(null, "", "quickfix", "source.organizeImports", "source.fixAll.child")] string? only)
    {
        var source = "class C { {|caret:|} }";

        // A clean document ensures discovery depends on the requested kind, not the presence of fixable diagnostics.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);

        // Omitted/empty filters, unrelated kinds, and child-only filters must not expose the source action.
        var actions = await GetActionsAsync(server, only is null ? null : only.Length == 0 ? [] : [only]);
        Assert.DoesNotContain(actions, action => action.Kind == CodeActionKind.SourceFixAll);
    }

    [Theory, CombinatorialData]
    public async Task RequestingFixAllPreservesRequestedRefactorings(bool mutatingLspWorkspace)
    {
        var source = "class C { void M() { {|caret:|}int value = 1; } }";

        // Use a caret position offering ordinary refactorings, then add source.fixAll to the same request filter.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var ordinary = await GetActionsAsync(server, ["refactor"]);
        var mixed = await GetActionsAsync(server, ["refactor", "source.fixAll"]);

        // Append the source action after ordinary actions without changing their order.
        Assert.Single(mixed, action => action.Kind == CodeActionKind.SourceFixAll);
        Assert.Equal(CodeActionKind.SourceFixAll, mixed.Last().Kind);
        Assert.NotEmpty(ordinary);
        Assert.Equal(ordinary.Select(action => action.Title),
            mixed.Take(mixed.Length - 1).Select(action => action.Title));
    }

    private static Task<LSP.CodeAction[]> GetActionsAsync(TestLspServer server, string[]? only)
    {
        var location = server.GetLocations("caret").Single();
        return GetActionsAsync(server, location.DocumentUri, location.Range, only);
    }

    private static async Task<LSP.CodeAction[]> GetActionsAsync(TestLspServer server, DocumentUri documentUri, LSP.Range range, string[]? only)
    {
        var actions = await server.ExecuteRequestAsync<CodeActionParams, LSP.CodeAction[]>(
            Methods.TextDocumentCodeActionName,
            new CodeActionParams
            {
                TextDocument = CreateTextDocumentIdentifier(documentUri),
                Range = range,
                Context = new CodeActionContext { Diagnostics = [], Only = only?.Select(kind => new CodeActionKind(kind)).ToArray() },
            },
            CancellationToken.None);
        Assert.NotNull(actions);
        return actions;
    }

    [Theory, CombinatorialData]
    public Task ResolveFixesAllOccurrencesAcrossDiagnosticIds_CSharp(bool mutatingLspWorkspace)
    {
        var source = "// beta alpha beta alpha\n{|caret:|}class C { }";
        var expected = "// fixed fixed fixed fixed\nclass C { }";

        return VerifyResolveFixesAllOccurrencesAcrossDiagnosticIdsAsync(
            source: source,
            expectedText: expected,
            LanguageNames.CSharp,
            mutatingLspWorkspace);
    }

    [Theory, CombinatorialData]
    public Task ResolveFixesAllOccurrencesAcrossDiagnosticIds_VisualBasic(bool mutatingLspWorkspace)
    {
        var source = "' beta alpha beta alpha\n{|caret:|}Class C\nEnd Class";
        var expected = "' fixed fixed fixed fixed\nClass C\nEnd Class";

        return VerifyResolveFixesAllOccurrencesAcrossDiagnosticIdsAsync(
            source: source,
            expectedText: expected,
            LanguageNames.VisualBasic,
            mutatingLspWorkspace);
    }

    private async Task VerifyResolveFixesAllOccurrencesAcrossDiagnosticIdsAsync(
        string source, string expectedText, string language, bool mutatingLspWorkspace)
    {
        // Give two diagnostic IDs repeated occurrences in the supplied document.
        await using var server = await CreateTestLspServerAsync(
            [source], language, mutatingLspWorkspace, initializationOptions: null);
        var fixer = new TestFixer(["FA0002", "FA0001"]);
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new("FA0002", "beta"), new("FA0001", "alpha"))], [fixer]));

        // Discover the deferred action, saving the workspace snapshot to check that resolution never applies changes.
        var before = server.TestWorkspace.CurrentSolution;
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        Assert.Null(action.Edit);
        Assert.Null(action.Command);

        // Resolve and apply the returned edit to a copy of the original text to check that every occurrence is fixed.
        var resolved = await ResolveAsync(server, action);
        var originalText = await server.GetDocumentTextAsync(server.GetLocations("caret").Single().DocumentUri);
        Assert.Equal(expectedText,
            ApplySingleDocumentEdit(resolved, originalText, server.GetLocations("caret").Single().DocumentUri));

        // Resolution computes edits without changing the live workspace.
        Assert.Same(before, server.TestWorkspace.CurrentSolution);
    }

    [Theory, CombinatorialData]
    public async Task ResolveReturnsEmptyEditForEmptyDocument(bool mutatingLspWorkspace)
    {
        var source = "{|caret:|}";

        // An empty document can still offer source.fixAll because discovery does not search for fixes.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));

        // With no diagnostics to fix, resolution should return a valid edit containing no document changes.
        var resolved = await ResolveAsync(server, action);
        Assert.NotNull(resolved.Edit);
        Assert.Empty(resolved.Edit.DocumentChanges!.Value.First);
    }

    [Theory, CombinatorialData]
    public async Task FixAllHandlesDiagnosticsMovedOrRemovedByEarlierFixes(bool mutatingLspWorkspace)
    {
        var source = "// expand remove later\n{|caret:|}class C { }";
        var expected = "// expanded_text gone fixed\nclass C { }";

        // Make the first fix lengthen the text and eliminate a later diagnostic, invalidating the original spans.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002", "FA0003"], async (document, diagnostics, cancellationToken) =>
        {
            var text = await document.GetTextAsync(cancellationToken);
            return diagnostics[0].Id == "FA0001"
                ? document.WithText(SourceText.From(text.ToString().Replace("expand remove", "expanded_text gone"))).Project.Solution
                : await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
        });
        // Register diagnostics out of order so execution must use ID order rather than analyzer registration order.
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new("FA0003", "later"), new("FA0002", "remove"), new("FA0001", "expand"))], [fixer]));

        // Run the sequence against progressively updated document snapshots.
        var changed = await ChangeAsync(document, server);

        // Skip the eliminated ID and fix the surviving later diagnostic at its shifted span.
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Equal(["FA0001", "FA0003"], fixer.Invocations);
    }

    [Theory, CombinatorialData]
    public async Task FixAllStopsRetryingWithoutDiagnosticProgress(bool mutatingLspWorkspace)
    {
        var source = "// alpha beta\n{|caret:|}class C { }";
        var expected = "// alpha fixed\nclass C { }\n// changed";

        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002"], async (document, diagnostics, cancellationToken) =>
        {
            var text = await document.GetTextAsync(cancellationToken);
            if (diagnostics[0].Id == "FA0002")
                return await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);

            // A retry would remove alpha, but changing text alone must not count as diagnostic progress.
            return text.ToString().Contains("// changed", StringComparison.Ordinal)
                ? await ReplaceDiagnosticsAsync(document, diagnostics, "unexpected", cancellationToken)
                : document.WithText(SourceText.From(text + "\n// changed")).Project.Solution;
        });
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "alpha"), new("FA0002", "beta"))], [fixer]));
        var logs = new List<string>();

        var changed = await ChangeAsync(document, server, log: logs.Add);

        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Equal(["FA0001", "FA0002"], fixer.Invocations);
        Assert.Equal(
            $"Stopping document Fix All for 'FA0001' from '{fixer.GetType().FullName}' (Fix All provider '{fixer.GetFixAllProvider().GetType().FullName}'): no diagnostic progress.",
            Assert.Single(logs));
    }

    [Theory, CombinatorialData]
    public async Task ResolveIncludesOnlyDiagnosticsAtOrAboveMinimumSeverity(
        bool mutatingLspWorkspace,
        [CombinatorialValues(DiagnosticSeverity.Error, DiagnosticSeverity.Warning, DiagnosticSeverity.Info, DiagnosticSeverity.Hidden)] DiagnosticSeverity threshold)
    {
        var source = "// error warning info hidden\n{|caret:|}class C { }";

        // Share an ID between warning and info occurrences to check filtering both between IDs and within one ID.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002", "FA0003"]);
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "error", DiagnosticSeverity.Error), new("FA0002", "warning"),
                new("FA0002", "info", DiagnosticSeverity.Info), new("FA0003", "hidden", DiagnosticSeverity.Hidden))], [fixer]));

        // Discover first, then configure each threshold to prove the resolve handler reads the current option.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        server.TestWorkspace.GetService<IGlobalOptionService>().SetGlobalOption(LspOptionsStorage.FixAllMinimumSeverity, threshold);
        var resolved = await ResolveAsync(server, action);

        // Lower-severity markers must remain untouched; hidden diagnostics participate only at the Hidden threshold.
        var expected = threshold switch
        {
            DiagnosticSeverity.Error => "// fixed warning info hidden\nclass C { }",
            DiagnosticSeverity.Warning => "// fixed fixed info hidden\nclass C { }",
            DiagnosticSeverity.Info => "// fixed fixed fixed hidden\nclass C { }",
            DiagnosticSeverity.Hidden => "// fixed fixed fixed fixed\nclass C { }",
            _ => throw new InvalidOperationException(),
        };
        var uri = server.GetLocations("caret").Single().DocumentUri;
        Assert.Equal(expected, ApplySingleDocumentEdit(resolved, await server.GetDocumentTextAsync(uri), uri));
    }

    [Theory, CombinatorialData]
    public async Task FixAllSkipsSuppressedAndDisabledDiagnostics(bool mutatingLspWorkspace)
    {
        var source = "#pragma warning disable FA0001\n// suppressed disabled enabled\n{|caret:|}class C { }";
        var expected = "#pragma warning disable FA0001\n// suppressed disabled fixed\nclass C { }";

        // Combine pragma-suppressed, disabled-by-default, and enabled hidden diagnostics.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002", "FA0003"]);
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "suppressed"), new("FA0002", "disabled", Enabled: false),
                new("FA0003", "enabled", DiagnosticSeverity.Hidden))], [fixer]));

        // Even the most permissive threshold must not re-enable suppressed or disabled diagnostics.
        var changed = await ChangeAsync(document, server, DiagnosticSeverity.Hidden);

        // Only the enabled hidden diagnostic should reach its fixer.
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Equal(["FA0003"], fixer.Invocations);
    }

    [Theory, CombinatorialData]
    public async Task FixAllFallsBackFromInteractiveProvider(bool mutatingLspWorkspace, bool nested)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// fixed\nclass C { }";

        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        CodeAction interactiveAction = nested
            ? CodeAction.Create("Choices", [new OptionsAction()], isInlinable: false)
            : new OptionsAction();
        var first = new TestFixer(["FA0001"], action: interactiveAction);
        var second = new TestFixer(["FA0001"]);
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new Rule("FA0001", "warning"))], [first, second]));

        var changed = await ChangeAsync(document, server);

        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Empty(first.Invocations);
        Assert.Equal(["FA0001"], second.Invocations);
    }

    [Theory, CombinatorialData]
    public async Task ResolvePreservesChangesAcrossDocumentsAndDiagnosticIds(bool mutatingLspWorkspace)
    {
        var source = "// alpha beta\n{|caret:|}class C { }";
        var otherSource = "class Other { }";
        var expected = "// fixed fixed\nclass C { }";
        var expectedOther = "class Changed { }";

        // The first diagnostic changes a sibling; the second must preserve that edit while fixing the requested file.
        await using var server = await CreateTestLspServerAsync([source, otherSource], mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            var otherId = document.Project.DocumentIds.Single(id => id != document.Id);
            return diagnostics[0].Id == "FA0001"
                ? solution.WithDocumentText(otherId, SourceText.From(expectedOther))
                : solution;
        });
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "alpha"), new("FA0002", "beta"))], [fixer]));
        var documents = server.TestWorkspace.CurrentSolution.Projects.Single().Documents.ToArray();

        // Resolve through LSP so both files must appear in one workspace edit without a command.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var operations = GetEditOperations(await ResolveAsync(server, action));

        Assert.Equal(2, operations.Length);
        Assert.Equal(expected, ApplyDocumentEdit(operations, await documents[0].GetTextAsync(), documents[0].GetURI()));
        Assert.Equal(expectedOther, ApplyDocumentEdit(operations, await documents[1].GetTextAsync(), documents[1].GetURI()));
        Assert.Equal(["FA0001", "FA0002"], fixer.Invocations);
    }

    [Theory, CombinatorialData]
    public async Task ResolveFixesUntitledDocumentAcrossDiagnosticIds(bool mutatingLspWorkspace)
    {
        var source = """
            class C
            {
                void M()
                {
                    int unused;
                    int assigned = 1;
                    return;
                }
            }
            """;
        var expected = """
            class C
            {
                void M()
                {
                    return;
                }
            }
            """;
        var uri = new DocumentUri("untitled:Untitled-1");

        // Open a real untitled document; didOpen supplies the language that its URI cannot identify.
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer },
            composition: Composition.AddParts(typeof(TestLspMiscellaneousFilesWorkspaceProviderFactory)));
        var miscellaneousFiles = Assert.IsType<TestLspMiscellaneousFilesWorkspaceProviderFactory.TestLspMiscellaneousFilesWorkspaceProvider>(
            server.GetRequiredLspService<ILspMiscellaneousFilesWorkspaceProvider>());
        miscellaneousFiles.EnableCompilerDiagnostics = true;
        await server.OpenDocumentAsync(uri, source, languageId: "csharp");

        // Request source.fixAll at a caret outside the warnings, using the opened URI rather than fixture markup.
        var actions = await GetActionsAsync(server, uri, new LSP.Range { Start = new(), End = new() }, ["source.fixAll"]);
        var action = Assert.Single(actions);

        // Built-in fixes remove both warning kinds in one text edit without changing the open document.
        var resolved = await ResolveAsync(server, action);
        Assert.Equal(expected, ApplySingleDocumentEdit(resolved, SourceText.From(source), uri).ReplaceLineEndings("\n"));
        Assert.Equal(source, Assert.Single(server.GetTrackedTexts()).ToString());
    }

    [Theory, CombinatorialData]
    public Task ResolveRejectsUntitledDocumentCreationWithoutLosingEarlierEdits(bool mutatingLspWorkspace)
        => VerifyResolveRejectsUntitledResourceOperationWithoutLosingEarlierEditsAsync(
            mutatingLspWorkspace, ResourceOperationKind.Create,
            (solution, untitled) => solution.AddDocument(DocumentId.CreateNewId(untitled.Project.Id), "Untitled-2",
                SourceText.From("class Added { }"), filePath: "untitled:Untitled-2"));

    [Theory, CombinatorialData]
    public Task ResolveRejectsUntitledDocumentRenameWithoutLosingEarlierEdits(bool mutatingLspWorkspace)
        => VerifyResolveRejectsUntitledResourceOperationWithoutLosingEarlierEditsAsync(
            mutatingLspWorkspace, ResourceOperationKind.Rename,
            (solution, untitled) => solution.WithDocumentName(untitled.Id, "Renamed.cs"));

    [Theory, CombinatorialData]
    public Task ResolveRejectsUntitledDocumentSaveWithoutLosingEarlierEdits(bool mutatingLspWorkspace)
        => VerifyResolveRejectsUntitledResourceOperationWithoutLosingEarlierEditsAsync(
            mutatingLspWorkspace, ResourceOperationKind.Rename,
            (solution, untitled) => solution.WithDocumentFilePath(untitled.Id, TestHelpers.GetRootedPath("Saved.cs")));

    [Theory, CombinatorialData]
    public Task ResolveRejectsUntitledDocumentDeletionWithoutLosingEarlierEdits(bool mutatingLspWorkspace)
        => VerifyResolveRejectsUntitledResourceOperationWithoutLosingEarlierEditsAsync(
            mutatingLspWorkspace, ResourceOperationKind.Delete,
            (solution, untitled) => solution.RemoveDocument(untitled.Id));

    private async Task VerifyResolveRejectsUntitledResourceOperationWithoutLosingEarlierEditsAsync(
        bool mutatingLspWorkspace, ResourceOperationKind resourceOperation, Func<Solution, Document, Solution> changeSolution)
    {
        var source = "// alpha beta\n{|caret:|}class C { }";
        var expected = "// fixed beta\nclass C { }";
        var uri = new DocumentUri("untitled:Untitled-1");

        // Advertise the relevant resource capability so rejection must come from the untitled-document policy.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace,
            initializationOptions: ResourceOperationOptions(resourceOperation, supported: true));
        var document = server.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        var untitled = document.Project.AddDocument("Untitled-1", SourceText.From("class Other { }"), filePath: uri.UriString);
        await server.TestWorkspace.ChangeSolutionAsync(untitled.Project.Solution);

        // Accept an ordinary fix first, then combine the second text fix with an unsupported untitled operation.
        var fixer = new TestFixer(["FA0001", "FA0002"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            if (diagnostics[0].Id == "FA0001")
                return solution;

            return changeSolution(solution, untitled);
        });
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "alpha"), new("FA0002", "beta"))], [fixer]));

        // Reject the whole second candidate, retaining only the earlier text edit and no resource operations.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var operations = GetEditOperations(await ResolveAsync(server, action));
        Assert.IsType<TextDocumentEdit>(Assert.Single(operations));
        Assert.Equal(expected, ApplyDocumentEdit(operations, await document.GetTextAsync(), document.GetURI()));
        Assert.Equal(["FA0001", "FA0002"], fixer.Invocations);
        Assert.Equal("class Other { }", (await server.GetDocumentTextAsync(uri)).ToString());
    }

    [Theory, CombinatorialData]
    public async Task ResolveIncludesAdditionalAndAnalyzerConfigDocumentChanges(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var additionalSource = "Before";
        var configSource = "root = true\n";
        var expected = "// fixed\nclass C { }";
        var expectedAdditional = "After";
        var expectedConfig = "root = true\n# Updated\n";

        // Include non-source files that the same fix needs to update.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var document = server.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        var additional = document.Project.AddAdditionalDocument(
            "Notes.txt", SourceText.From(additionalSource), filePath: TestHelpers.GetRootedPath("Notes.txt"));
        var config = additional.Project.AddAnalyzerConfigDocument(
            ".editorconfig", SourceText.From(configSource), filePath: TestHelpers.GetRootedPath(".editorconfig"));
        await server.TestWorkspace.ChangeSolutionAsync(config.Project.Solution);
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            return solution.WithAdditionalDocumentText(additional.Id, SourceText.From(expectedAdditional))
                .WithAnalyzerConfigDocumentText(config.Id, SourceText.From(expectedConfig));
        });
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));

        // All three text updates must survive conversion into the resolved workspace edit.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var operations = GetEditOperations(await ResolveAsync(server, action));

        Assert.Equal(3, operations.Length);
        Assert.Equal(expected, ApplyDocumentEdit(operations, await document.GetTextAsync(), document.GetURI()));
        Assert.Equal(expectedAdditional, ApplyDocumentEdit(operations, await additional.GetTextAsync(), additional.GetURI()));
        Assert.Equal(expectedConfig, ApplyDocumentEdit(operations, await config.GetTextAsync(), config.GetURI()));
    }

    [Theory, CombinatorialData]
    public async Task ResolveCreatesDocumentsOnlyWhenSupported(bool mutatingLspWorkspace, bool supportsCreate, bool hasFilePath)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// fixed\nclass C { }";
        var expectedAdded = "class Added { }";
        var addedPath = Path.Combine(TestHelpers.GetRootedPath("Folder"), "Added file.cs");

        // Advertise file creation independently of ordinary text-edit support.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace,
            initializationOptions: ResourceOperationOptions(ResourceOperationKind.Create, supportsCreate));

        // A missing document path uses the project directory, logical folders, and name.
        var project = server.TestWorkspace.CurrentSolution.Projects.Single();
        await server.TestWorkspace.ChangeSolutionAsync(project.Solution.WithProjectFilePath(
            project.Id, TestHelpers.GetRootedPath("Project.csproj")));
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            return solution.AddDocument(DocumentId.CreateNewId(document.Project.Id), "Added file.cs",
                SourceText.From(expectedAdded), folders: ["Folder"], filePath: hasFilePath ? addedPath : null);
        });
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));
        var document = server.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();

        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var operations = GetEditOperations(await ResolveAsync(server, action));
        if (!supportsCreate)
        {
            // Reject the whole fix, including its otherwise-supported edit to the requested document.
            Assert.Empty(operations);
            return;
        }

        // Preserve the source action's creation and text edits; ordinary resolve tests own operation ordering.
        var create = Assert.Single(operations.OfType<CreateFile>());
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(addedPath), create.DocumentUri);
        Assert.Equal(expectedAdded, ApplyDocumentEdit(operations, SourceText.From(""), create.DocumentUri));
        Assert.Equal(expected, ApplyDocumentEdit(operations, await document.GetTextAsync(), document.GetURI()));
    }

    [Theory, CombinatorialData]
    public async Task ResolveRenamesDocumentsOnlyWhenSupported(bool mutatingLspWorkspace, bool supportsRename, bool updatesFilePath)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// fixed\nclass C { }";
        var renamedPath = TestHelpers.GetRootedPath("Renamed.cs");

        // Rename the physical file whether the fixer updates its path or only its logical name.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace,
            initializationOptions: ResourceOperationOptions(ResourceOperationKind.Rename, supportsRename));
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            solution = solution.WithDocumentName(document.Id, "Renamed.cs");
            return updatesFilePath ? solution.WithDocumentFilePath(document.Id, renamedPath) : solution;
        });
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));
        var document = server.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();

        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var operations = GetEditOperations(await ResolveAsync(server, action));
        if (!supportsRename)
        {
            Assert.Empty(operations);
            return;
        }

        // Preserve the accepted text and rename destination; ordinary resolve tests own operation ordering.
        Assert.Equal(expected, ApplyDocumentEdit(operations, await document.GetTextAsync(), document.GetURI()));
        var rename = Assert.Single(operations.OfType<RenameFile>());
        Assert.Equal(document.GetURI(), rename.OldDocumentUri);
        var expectedPath = updatesFilePath ? renamedPath : Path.Combine(Path.GetDirectoryName(document.FilePath)!, "Renamed.cs");
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(expectedPath), rename.NewDocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task ResolveDeletesRequestedDocumentOnlyWhenSupported(bool mutatingLspWorkspace, bool supportsDelete)
    {
        var source = "// alpha beta\n{|caret:|}class C { }";

        // A supported deletion ends aggregation because the requested document no longer exists.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace,
            initializationOptions: ResourceOperationOptions(ResourceOperationKind.Delete, supportsDelete));
        var fixer = new TestFixer(["FA0001", "FA0002"], (document, _, _) =>
            Task.FromResult(document.Project.Solution.RemoveDocument(document.Id)));
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "alpha"), new("FA0002", "beta"))], [fixer]));
        var document = server.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();

        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var operations = GetEditOperations(await ResolveAsync(server, action));
        if (!supportsDelete)
        {
            Assert.Empty(operations);
            return;
        }

        // Emit the deletion without trying to fix the remaining diagnostic in a removed document.
        Assert.Equal(document.GetURI(), Assert.IsType<DeleteFile>(Assert.Single(operations)).DocumentUri);
        Assert.Equal(["FA0001"], fixer.Invocations);
    }

    [Theory, CombinatorialData]
    public async Task ResolvePreservesTextEditsWithIncidentalStateChanges(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// fixed\nclass C { }";

        // Include a sibling so the fix can also reorder existing documents.
        await using var server = await CreateTestLspServerAsync([source, "class Other { }"], mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            // Combine the text fix with incidental project settings, logical folders, and document ordering.
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            return solution
                .WithProjectCompilationOptions(document.Project.Id,
                    document.Project.CompilationOptions!.WithConcurrentBuild(!document.Project.CompilationOptions.ConcurrentBuild))
                .WithDocumentFolders(document.Id, ["Folder"])
                .WithProjectDocumentsOrder(document.Project.Id, document.Project.DocumentIds.Reverse().ToImmutableList());
        });

        // Expose the marker analyzer and fixer to normal LSP discovery.
        await AddReferenceAsync(server, new TestReference([new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));
        var document = server.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();

        // Discover and resolve the document-wide source action through LSP.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var resolved = await ResolveAsync(server, action);

        // Emit the text fix without rejecting it or producing edits for the incidental state.
        Assert.Equal(expected, ApplySingleDocumentEdit(resolved, await document.GetTextAsync(), document.GetURI()));
    }

    [Theory, CombinatorialData]
    public async Task ResolveIncludesFrozenGeneratedDocumentChanges(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// fixed\nclass C { }";
        var generatedSource = "class Generated { }";
        var expectedGenerated = "class ChangedGenerated { }";

        // Generate the original document so resolve can compute a text edit for its frozen replacement.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        await AddGeneratorAsync(new SingleFileTestGenerator(generatedSource), server.TestWorkspace);
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            // Fix the ordinary document and override the generated output in the same returned snapshot.
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            var generated = Assert.Single(await document.Project.GetSourceGeneratedDocumentsAsync(cancellationToken));
            return solution.WithFrozenSourceGeneratedDocument(
                generated.Identity, DateTime.MinValue, SourceText.From(expectedGenerated)).Project.Solution;
        });

        // Register the fixer and capture both original documents for checking the emitted edits.
        await AddReferenceAsync(server, new TestReference([new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));
        var document = server.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        var generated = Assert.Single(await document.Project.GetSourceGeneratedDocumentsAsync());

        // Resolve through LSP to exercise conversion of both ordinary and frozen generated-document changes.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var operations = GetEditOperations(await ResolveAsync(server, action));

        // Preserve both edits rather than rejecting the fix because its generated output was frozen.
        Assert.Equal(2, operations.Length);
        Assert.Equal(expected, ApplyDocumentEdit(operations, await document.GetTextAsync(), document.GetURI()));
        Assert.Equal(expectedGenerated, ApplyDocumentEdit(operations, await generated.GetTextAsync(), generated.GetURI()));
    }

    [Theory, CombinatorialData]
    public async Task FixAllRejectsReferenceChangesWithoutLosingEarlierEdits(bool mutatingLspWorkspace)
    {
        var source = "// alpha beta\n{|caret:|}class C { }";
        var otherSource = "class Other { }";
        var expected = "// fixed beta\nclass C { }";
        var expectedOther = "class Changed { }";

        // Accept an initial cross-file fix, then encounter a fix requiring unsupported project state.
        await using var server = await CreateTestLspServerAsync([source, otherSource], mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            if (diagnostics[0].Id == "FA0002")
                return solution.WithProjectMetadataReferences(document.Project.Id, []);

            var otherId = document.Project.DocumentIds.Single(id => id != document.Id);
            return solution.WithDocumentText(otherId, SourceText.From(expectedOther));
        });
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "alpha"), new("FA0002", "beta"))], [fixer]));
        var logs = new List<string>();
        var changed = await ChangeAsync(document, server, log: logs.Add);

        // Keep the accepted fix, but discard both the later text edit and its unsupported project change.
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Equal(expectedOther, (await changed.Project.Documents.Last().GetTextAsync()).ToString());
        Assert.Equal(document.Project.MetadataReferences, changed.Project.MetadataReferences);
        Assert.Equal(
            $"Skipping document Fix All for 'FA0002' from '{fixer.GetType().FullName}' (Fix All provider '{fixer.GetFixAllProvider().GetType().FullName}'): unsupported accumulated workspace changes.",
            Assert.Single(logs));
    }

    [Theory, CombinatorialData]
    public async Task FixAllRejectsUnusableDocumentPathsWithoutLosingEarlierEdits(
        bool mutatingLspWorkspace, [CombinatorialValues(null, "Relative.cs", "untitled:Untitled-1")] string? filePath)
    {
        var source = "// alpha beta\n{|caret:|}class C { }";
        var expected = "// fixed beta\nclass C { }";

        // Accept an initial fix before a later candidate removes the filesystem path, including a rename to untitled.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            return diagnostics[0].Id == "FA0002" ? solution.WithDocumentFilePath(document.Id, filePath) : solution;
        });
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "alpha"), new("FA0002", "beta"))], [fixer]));

        // Normalization must reach rejection without attempting to construct an invalid document URI.
        var logs = new List<string>();
        // Allow renames so rejection depends on the output path rather than missing client capabilities.
        var changedSolution = await DocumentFixAllHelper.GetChangedSolutionAsync(
            document, server.TestWorkspace.GetService<ICodeFixService>(), DiagnosticSeverity.Warning,
            [ResourceOperationKind.Rename], logs.Add, CancellationToken.None);
        var changed = changedSolution.GetRequiredDocument(document.Id);

        // Preserve the earlier text and original path, discarding the whole unsupported candidate.
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Equal(document.FilePath, changed.FilePath);
        Assert.Equal(
            $"Skipping document Fix All for 'FA0002' from '{fixer.GetType().FullName}' (Fix All provider '{fixer.GetFixAllProvider().GetType().FullName}'): unsupported accumulated workspace changes.",
            Assert.Single(logs));
    }

    [Theory, CombinatorialData]
    public async Task ResolveUsesDocumentTextUpdatedAfterDiscovery(bool mutatingLspWorkspace)
    {
        var source = "// alpha\n{|caret:|}class C { }";
        var updatedSource = "// beta beta\nclass C { }";
        var expected = "// fixed fixed\nclass C { }";

        // Start with an alpha diagnostic, then discover an action for the open document.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002"]);
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "alpha"), new("FA0002", "beta"))], [fixer]));
        var uri = server.GetLocations("caret").Single().DocumentUri;
        await server.OpenDocumentAsync(uri);
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));

        // Replace alpha after discovery so resolving against the original snapshot would fix the wrong ID.
        await server.ReplaceTextAsync(uri, (new LSP.Range { Start = new Position(0, 3), End = new Position(0, 8) }, "beta beta"));

        // Resolve must recompute diagnostics from the latest didChange snapshot.
        var resolved = await ResolveAsync(server, action);
        // The non-mutating harness keeps its Workspace text unchanged; LSP edits target the didChange snapshot.
        Assert.Equal(expected, ApplySingleDocumentEdit(resolved, SourceText.From(updatedSource), uri));

        // Only the replacement document's diagnostic ID should be sent to Fix All.
        Assert.Equal(["FA0002"], fixer.Invocations);
    }

    [Theory, CombinatorialData]
    public async Task ResolveRemovesAllUnusedLocalDeclarations(bool mutatingLspWorkspace)
    {
        // Include two occurrences of both unassigned and assigned-but-unused local warnings.
        var source = """
            class C
            {
                void M()
                {
                    int unused;
                    int alsoUnused;
                    int assigned = 1;
                    int alsoAssigned = 2;
                    {|caret:|}System.Console.WriteLine(3);
                }
            }
            """;
        var expected = """
            class C
            {
                void M()
                {
                    System.Console.WriteLine(3);
                }
            }
            """;
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var uri = server.GetLocations("caret").Single().DocumentUri;

        // Exercise the built-in compiler fixes through source.fixAll rather than the synthetic test fixer.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var resolved = await ResolveAsync(server, action);

        // Aggregate both diagnostic IDs, removing all four unused declarations while preserving the remaining statement.
        Assert.Equal(expected.ReplaceLineEndings("\n"),
            ApplySingleDocumentEdit(resolved, await server.GetDocumentTextAsync(uri), uri).ReplaceLineEndings("\n"));
    }

    [Theory, CombinatorialData]
    public async Task ResolveGeneratesMembersForDifferentInterfaces(bool mutatingLspWorkspace)
    {
        // Both classes report CS0535, but implementing different interfaces requires different equivalence keys.
        var source = """
            interface IFirst
            {
                void First();
            }

            interface ISecond
            {
                void Second();
            }

            class C : IFirst
            {
                {|caret:|}
            }

            class D : ISecond
            {
            }
            """;
        var expected = """
            interface IFirst
            {
                void First();
            }

            interface ISecond
            {
                void Second();
            }

            class C : IFirst
            {
                public void First()
                {
                    throw new System.NotImplementedException();
                }
            }

            class D : ISecond
            {
                public void Second()
                {
                    throw new System.NotImplementedException();
                }
            }
            """;
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var uri = server.GetLocations("caret").Single().DocumentUri;

        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var resolved = await ResolveAsync(server, action);

        Assert.Equal(expected.ReplaceLineEndings("\n"),
            ApplySingleDocumentEdit(resolved, await server.GetDocumentTextAsync(uri), uri).ReplaceLineEndings("\n"));
    }

    [Theory, CombinatorialData]
    public async Task FixAllAcceptsMatchingLinkedDocumentChanges(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// fixed\nclass C { }";

        await using var server = await CreateLinkedDocumentTestLspServerAsync(source, mutatingLspWorkspace);

        // Return the same updated text for every project context of the shared file.
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            var text = await solution.GetDocument(document.Id)!.GetTextAsync(cancellationToken);
            foreach (var linkedId in document.GetLinkedDocumentIds())
                solution = solution.WithDocumentText(linkedId, text);
            return solution;
        });
        await AddReferenceAsync(server, new TestReference([new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));
        var document = server.TestWorkspace.CurrentSolution.Projects.First().Documents.Single();

        // Run the helper directly to verify that both contexts receive the accepted text.
        var logs = new List<string>();
        var changed = await ChangeAsync(document, server, log: logs.Add);

        Assert.Equal(expected, (await changed.GetTextAsync()).ToString().ReplaceLineEndings("\n"));
        var linked = changed.Project.Solution.GetDocument(Assert.Single(changed.GetLinkedDocumentIds()))!;
        Assert.Equal(expected, (await linked.GetTextAsync()).ToString().ReplaceLineEndings("\n"));
        Assert.Empty(logs);
    }

    [Theory, CombinatorialData]
    public async Task FixAllPropagatesLinkedChangesBeforeLaterFixes(bool mutatingLspWorkspace, TextDocumentKind documentKind)
    {
        var source = "// alpha beta\n{|caret:|}class C { }";
        var otherSource = documentKind == TextDocumentKind.AnalyzerConfigDocument ? "root = true\n" : "class Other { }";
        var expected = "// fixed fixed\nclass C { }";
        var expectedOther = documentKind == TextDocumentKind.AnalyzerConfigDocument ? "root = true\n# Changed\n" : "class Changed { }";
        var otherName = documentKind == TextDocumentKind.AnalyzerConfigDocument ? ".editorconfig" : "Other.cs";
        var otherPath = TestHelpers.GetRootedPath(otherName);

        // Share both the requested source and a sibling of the selected document kind between projects.
        await using var server = await CreateLinkedDocumentTestLspServerAsync(source, mutatingLspWorkspace);
        var solution = server.TestWorkspace.CurrentSolution;
        foreach (var projectId in solution.ProjectIds)
        {
            var id = DocumentId.CreateNewId(projectId);
            var text = SourceText.From(otherSource);
            solution = documentKind switch
            {
                TextDocumentKind.Document => solution.AddDocument(id, otherName, text, filePath: otherPath),
                TextDocumentKind.AdditionalDocument => solution.AddAdditionalDocument(id, otherName, text, filePath: otherPath),
                TextDocumentKind.AnalyzerConfigDocument => solution.AddAnalyzerConfigDocument(id, otherName, text, filePath: otherPath),
                _ => throw ExceptionUtilities.UnexpectedValue(documentKind),
            };
        }
        await server.TestWorkspace.ChangeSolutionAsync(solution);
        var observedOtherTexts = new List<string>();
        var fixer = new TestFixer(["FA0001", "FA0002"], async (document, diagnostics, cancellationToken) =>
        {
            var changed = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            var otherIds = document.Project.Solution.GetDocumentIdsWithFilePath(otherPath);
            if (diagnostics[0].Id == "FA0001")
                return changed.WithTextDocumentText(otherIds[0], SourceText.From(expectedOther));

            // The later fix must observe the earlier edit in every context, not only in the final LSP merge.
            foreach (var id in otherIds)
                observedOtherTexts.Add((await document.Project.Solution.GetRequiredTextDocument(id).GetTextAsync(cancellationToken)).ToString());
            return changed;
        });
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "alpha"), new("FA0002", "beta"))], [fixer]));

        var result = await ChangeAsync(document, server);

        Assert.Equal(expected, (await result.GetTextAsync()).ToString().ReplaceLineEndings("\n"));
        Assert.NotEmpty(observedOtherTexts);
        Assert.All(observedOtherTexts, text => Assert.Equal(expectedOther, text));
        foreach (var id in result.Project.Solution.GetDocumentIdsWithFilePath(otherPath))
            Assert.Equal(expectedOther, (await result.Project.Solution.GetRequiredTextDocument(id).GetTextAsync()).ToString());
    }

    [Theory, CombinatorialData]
    public async Task FixAllRejectsConflictingLinkedDocumentChanges(bool mutatingLspWorkspace)
    {
        var source = "// alpha beta\n{|caret:|}class C { }";
        var conflictingSource = "// different\nclass C { }";
        var expected = "// fixed beta\nclass C { }";

        await using var server = await CreateLinkedDocumentTestLspServerAsync(source, mutatingLspWorkspace);

        // Accept one fix, then mix a later local fix with conflicting text for its linked context.
        var fixer = new TestFixer(["FA0001", "FA0002"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            if (diagnostics[0].Id == "FA0002")
            {
                foreach (var linkedId in document.GetLinkedDocumentIds())
                    solution = solution.WithDocumentText(linkedId, SourceText.From(conflictingSource));
            }

            return solution;
        });
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "alpha"), new("FA0002", "beta"))], [fixer]));
        var document = server.TestWorkspace.CurrentSolution.Projects.First().Documents.Single();

        // Reject only the conflicting candidate, preserving the earlier accepted text in both contexts.
        var logs = new List<string>();
        var changed = await ChangeAsync(document, server, log: logs.Add);

        Assert.Equal(expected, (await changed.GetTextAsync()).ToString().ReplaceLineEndings("\n"));
        var linked = changed.Project.Solution.GetDocument(Assert.Single(changed.GetLinkedDocumentIds()))!;
        Assert.Equal(expected, (await linked.GetTextAsync()).ToString().ReplaceLineEndings("\n"));
        Assert.Equal(
            $"Skipping document Fix All for 'FA0002' from '{fixer.GetType().FullName}' (Fix All provider '{fixer.GetFixAllProvider().GetType().FullName}'): conflicting linked document changes.",
            Assert.Single(logs));
    }

    [Theory, CombinatorialData]
    public async Task ResolveAcceptsLinkedRenameFromOneContext(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// fixed\nclass C { }";
        var renamedPath = TestHelpers.GetRootedPath("Renamed.cs");

        // Advertise rename support for one physical file shared by two project contexts.
        await using var server = await CreateLinkedDocumentTestLspServerAsync(source, mutatingLspWorkspace,
            ResourceOperationOptions(ResourceOperationKind.Rename, supported: true));
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            // Update only the requested context, leaving its linked peer's name and path unchanged.
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            return solution.WithDocumentName(document.Id, "Renamed.cs").WithDocumentFilePath(document.Id, renamedPath);
        });

        // Register in both projects so discovery works whichever linked context LSP selects.
        await AddReferenceAsync(server, new TestReference([new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));
        var document = server.TestWorkspace.CurrentSolution.Projects.First().Documents.Single();

        // Resolve the source action against the original shared-file URI.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var operations = GetEditOperations(await ResolveAsync(server, action));

        // Edit the old URI before one physical rename; unchanged peer metadata must not block it.
        Assert.Equal(2, operations.Length);
        Assert.Equal(expected, ApplyDocumentEdit(operations, await document.GetTextAsync(), document.GetURI()).ReplaceLineEndings("\n"));
        var rename = Assert.IsType<RenameFile>(operations[1]);
        Assert.Equal(document.GetURI(), rename.OldDocumentUri);
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(renamedPath), rename.NewDocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task FixAllRejectsDeletingOneLinkedContext(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";

        // Allow deletes so rejection exercises shared-file safety rather than missing client capabilities.
        await using var server = await CreateLinkedDocumentTestLspServerAsync(source, mutatingLspWorkspace,
            ResourceOperationOptions(ResourceOperationKind.Delete, supported: true));

        // Remove only the selected project's document, leaving the other context backed by the same file.
        var fixer = new TestFixer(["FA0001"], (document, _, _) =>
            Task.FromResult(document.Project.Solution.RemoveDocument(document.Id)));

        // Make the fixer available in either linked context selected by LSP.
        await AddReferenceAsync(server, new TestReference([new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));

        // Discover the source action, then resolve the candidate removal through the normal LSP path.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));

        // Reject the candidate: DeleteFile would remove the surviving context's physical source.
        Assert.Empty(GetEditOperations(await ResolveAsync(server, action)));
    }

    [Theory, CombinatorialData]
    public async Task FixAllRejectsConflictingLinkedRenames(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";

        // Allow renames so rejection depends on conflicting destinations, not client capabilities.
        await using var server = await CreateLinkedDocumentTestLspServerAsync(source, mutatingLspWorkspace,
            ResourceOperationOptions(ResourceOperationKind.Rename, supported: true));
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            // First fix the text and rename the requested context.
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            solution = solution.WithDocumentName(document.Id, "First.cs")
                .WithDocumentFilePath(document.Id, TestHelpers.GetRootedPath("First.cs"));

            // Ask the other context to move that same physical file to a different destination.
            foreach (var linkedId in document.GetLinkedDocumentIds())
                solution = solution.WithDocumentName(linkedId, "Second.cs")
                    .WithDocumentFilePath(linkedId, TestHelpers.GetRootedPath("Second.cs"));
            return solution;
        });

        // Register in both projects so either linked context can supply the candidate.
        await AddReferenceAsync(server, new TestReference([new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));

        // Resolve through LSP, where both requested renames would have the same source URI.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));

        // Reject the entire fix, including its text edit: the converter can emit only one of these renames.
        Assert.Empty(GetEditOperations(await ResolveAsync(server, action)));
    }

    private Task<TestLspServer> CreateLinkedDocumentTestLspServerAsync(
        string source, bool mutatingLspWorkspace, InitializationOptions? initializationOptions = null)
    {
        // Model one physical file shared by two project contexts.
        var path = TestHelpers.GetRootedPath("Linked.cs");
        var workspace = $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="First" Name="First">
                    <Document FilePath="{{path}}">{{source}}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Second" Name="Second">
                    <Document IsLinkFile="true" LinkProjectName="First" LinkFilePath="{{path}}" />
                </Project>
            </Workspace>
            """;
        return CreateXmlTestLspServerAsync(workspace, mutatingLspWorkspace, initializationOptions: initializationOptions);
    }

    [Theory, CombinatorialData]
    public async Task FixAllRejectsFixesWithAdditionalOperations(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// warning\nclass C { }";

        // Return a valid text edit together with an extra operation that must never execute.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001"], unsupportedOperations: true);
        var document = GetDocumentWithReference(server, new TestReference([new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));

        // Capture the rejection while asking the helper to process the candidate.
        var logs = new List<string>();
        var changed = await ChangeAsync(document, server, log: logs.Add);

        // Reject both operations together rather than accepting the edit or executing the side effect.
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Equal(
            $"Skipping document Fix All for 'FA0001' from '{fixer.GetType().FullName}' (Fix All provider '{fixer.GetFixAllProvider().GetType().FullName}'): unsupported code action operations.",
            Assert.Single(logs));
    }

    [Theory, CombinatorialData]
    public async Task FixAllDiscoversDiagnosticsOnlyInTheRequestedDocument(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var otherSource = "// warning\nclass Other { }";
        var otherProjectSource = "// warning\nclass Another { }";
        var expected = "// fixed\nclass C { }";
        var expectedOriginal = "// warning\nclass C { }";

        // Place the same diagnostic marker in the requested document and a sibling document.
        await using var server = await CreateTestLspServerAsync([source, otherSource], mutatingLspWorkspace);
        var document = server.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
        var fixer = new TestFixer(["FA0001"]);
        var reference = new TestReference([new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]);

        // Add another project with the same marker to catch accidental project- or solution-wide fixing.
        var otherProject = document.Project.Solution.AddProject("OtherProject", "OtherProject", LanguageNames.CSharp)
            .WithMetadataReferences(document.Project.MetadataReferences)
            .AddDocument("OtherProject.cs", SourceText.From(otherProjectSource), filePath: TestHelpers.GetRootedPath("OtherProject.cs"));
        document = WithReference(otherProject.Project.Solution, reference).GetRequiredDocument(document.Id);

        // Prove that every document is eligible; unchanged outside text must not be a consequence of missing diagnostics.
        var diagnosticService = document.Project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
        foreach (var candidate in document.Project.Solution.Projects.SelectMany(project => project.Documents))
        {
            var diagnostics = await diagnosticService.GetDiagnosticsForSpanAsync(
                candidate, range: null, DiagnosticIdFilter.All, priority: null, DiagnosticKind.All, CancellationToken.None);
            Assert.Single(diagnostics, diagnostic => diagnostic.Id == "FA0001" &&
                diagnostic.Severity == DiagnosticSeverity.Warning && !diagnostic.IsSuppressed);
        }

        // Limit the invocation to the document marked by the caret.
        var changed = await ChangeAsync(document, server);
        Assert.Equal([FixAllScope.Document], fixer.RequestedScopes);

        // Only its returned snapshot may change; other documents/projects and the input snapshot must stay intact.
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Equal(otherSource, (await changed.Project.Documents.Last().GetTextAsync()).ToString());
        Assert.Equal(otherProjectSource,
            (await changed.Project.Solution.GetDocument(otherProject.Id)!.GetTextAsync()).ToString());
        Assert.Equal(expectedOriginal, (await document.GetTextAsync()).ToString());
    }

    private static Solution WithReference(Solution solution, TestReference reference)
    {
        // Linked-file requests can select either project context, so each needs the test analyzer and fixer.
        foreach (var projectId in solution.ProjectIds)
            solution = solution.AddAnalyzerReference(projectId, reference);

        return solution;
    }

    private static Document GetDocumentWithReference(TestLspServer server, TestReference reference)
    {
        var solution = WithReference(server.TestWorkspace.CurrentSolution, reference);
        return solution.Projects.First().Documents.First();
    }

    private static Task AddReferenceAsync(TestLspServer server, TestReference reference)
        => server.TestWorkspace.ChangeSolutionAsync(WithReference(server.TestWorkspace.CurrentSolution, reference));

    private static async Task<Document> ChangeAsync(
        Document document, TestLspServer server, DiagnosticSeverity severity = DiagnosticSeverity.Warning,
        Action<string>? log = null)
    {
        var solution = await DocumentFixAllHelper.GetChangedSolutionAsync(
            document, server.TestWorkspace.GetService<ICodeFixService>(), severity, [], log ?? (_ => { }), CancellationToken.None);
        return solution.GetRequiredDocument(document.Id);
    }

    private static async Task<LSP.CodeAction> ResolveAsync(TestLspServer server, LSP.CodeAction action)
    {
        var resolved = await server.ExecuteRequestAsync<LSP.CodeAction, LSP.CodeAction>(Methods.CodeActionResolveName, action, CancellationToken.None);
        Assert.NotNull(resolved);
        return resolved;
    }

    private static InitializationOptions ResourceOperationOptions(ResourceOperationKind operation, bool supported)
        => new()
        {
            ClientCapabilities = new ClientCapabilities
            {
                Workspace = new WorkspaceClientCapabilities
                {
                    WorkspaceEdit = new WorkspaceEditSetting { ResourceOperations = supported ? [operation] : [] }
                }
            }
        };

    private static object[] GetEditOperations(LSP.CodeAction action)
    {
        Assert.Null(action.Command);
        Assert.NotNull(action.Edit);
        return action.Edit.DocumentChanges!.Value.Match(
            edits => edits.Cast<object>().ToArray(),
            edits => edits.Select(edit => edit.Value!).ToArray());
    }

    private static string ApplyDocumentEdit(object[] operations, SourceText originalText, DocumentUri uri)
    {
        var edit = Assert.Single(operations.OfType<TextDocumentEdit>(), edit => edit.TextDocument.DocumentUri == uri);
        return ApplyTextEdits(edit.Edits.Select(edit => (TextEdit)edit.Value!).ToArray(), originalText);
    }

    private static string ApplySingleDocumentEdit(LSP.CodeAction action, SourceText originalText, DocumentUri uri)
    {
        Assert.Null(action.Command);
        Assert.NotNull(action.Edit);
        var edit = Assert.Single(action.Edit.DocumentChanges!.Value.First);
        Assert.Equal(uri, edit.TextDocument.DocumentUri);
        return ApplyTextEdits(edit.Edits.Select(edit => (TextEdit)edit.Value!).ToArray(), originalText);
    }

    private static async Task<Solution> ReplaceDiagnosticsAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics, string replacement, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken);
        return document.WithText(text.WithChanges(diagnostics.Select(diagnostic => new TextChange(diagnostic.Location.SourceSpan, replacement)))).Project.Solution;
    }

    private sealed record Rule(string Id, string Marker, DiagnosticSeverity Severity = DiagnosticSeverity.Warning, bool Enabled = true);

    private sealed class MarkerAnalyzer(params Rule[] rules) : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            [.. rules.GroupBy(rule => rule.Id).Select(group => group.First()).Select(rule => new DiagnosticDescriptor(
                rule.Id, "Document Fix All test", "Document Fix All test", "Testing", rule.Severity, rule.Enabled))];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxTreeAction(context =>
            {
                var text = context.Tree.GetText(context.CancellationToken).ToString();
                foreach (var rule in rules)
                {
                    for (var start = 0; (start = text.IndexOf(rule.Marker, start, StringComparison.Ordinal)) >= 0; start += rule.Marker.Length)
                    {
                        var descriptor = SupportedDiagnostics.Single(descriptor => descriptor.Id == rule.Id);
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor, Location.Create(context.Tree, new TextSpan(start, rule.Marker.Length)),
                            rule.Severity, additionalLocations: null, properties: null));
                    }
                }
            });
        }
    }

    private sealed class TestReference(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> fixers)
        : AnalyzerReference, ICodeFixProviderFactory, SerializerService.TestAccessor.IAnalyzerReferenceWithGuid
    {
        public override string FullPath => "";
        public override object Id { get; } = Guid.NewGuid();
        public Guid Guid { get; } = Guid.NewGuid();
        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => analyzers;
        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => analyzers;
        public ImmutableArray<CodeFixProvider> GetFixers() => fixers;
    }

    private sealed class TestFixer(
        ImmutableArray<string> ids,
        Func<Document, ImmutableArray<Diagnostic>, CancellationToken, Task<Solution>>? transform = null,
        CodeAction? action = null,
        bool unsupportedOperations = false) : CodeFixProvider
    {
        private readonly bool _unsupportedOperations = unsupportedOperations;
        public List<string> Invocations { get; } = [];
        public List<FixAllScope> RequestedScopes { get; } = [];
        public override ImmutableArray<string> FixableDiagnosticIds => ids;
        public override FixAllProvider GetFixAllProvider() => new TestFixAllProvider(this);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var codeAction = action ?? CodeAction.Create(
                "First", cancellationToken => ReplaceDiagnosticsAsync(context.Document, context.Diagnostics, "fixed", cancellationToken), "first");
            context.RegisterCodeFix(codeAction, context.Diagnostics);
            return Task.CompletedTask;
        }

        private sealed class TestFixAllProvider(TestFixer fixer) : FixAllProvider
        {
            public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
                => [FixAllScope.Document];

            public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                fixer.RequestedScopes.Add(fixAllContext.Scope);
                var document = fixAllContext.Document!;
                var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document);
                Assert.NotEmpty(diagnostics);
                fixer.Invocations.Add(diagnostics[0].Id);
                if (fixer._unsupportedOperations)
                {
                    var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", fixAllContext.CancellationToken);
                    return new UnsupportedOperationsAction(solution);
                }

                return CodeAction.Create("Fix document", cancellationToken =>
                    fixer.ChangeAsync(document, diagnostics, cancellationToken), fixAllContext.CodeActionEquivalenceKey);
            }
        }

        private Task<Solution> ChangeAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
            => transform is null ? ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken)
                : transform(document, diagnostics, cancellationToken);
    }

    private sealed class OptionsAction : CodeActionWithOptions
    {
        public override string Title => "Requires options";
        public override string EquivalenceKey => "first";
        public override object GetOptions(CancellationToken cancellationToken)
            => throw new InvalidOperationException("Document Fix All must not request user options.");
    }

    private sealed class UnsupportedOperationsAction(Solution solution) : CodeAction
    {
        public override string Title => "Has additional operations";
        protected override Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
            => Task.FromResult<Solution?>(solution);

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<CodeActionOperation>>([new ApplyChangesOperation(solution), new UnsupportedOperation()]);
    }

    private sealed class UnsupportedOperation : CodeActionOperation
    {
        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Document Fix All must not execute provider operations.");
    }
}
