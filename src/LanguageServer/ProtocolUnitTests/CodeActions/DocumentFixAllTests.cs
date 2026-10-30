// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
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
    public async Task DiscoveryOffersFixAllOnlyForMatchingSourceFilters(
        bool mutatingLspWorkspace,
        [CombinatorialValues(null, "", "quickfix", "source.organizeImports", "source.fixAll.child", "source", "source.fixAll")] string? only)
    {
        var source = "class C { {|caret:|} }";

        // A clean document ensures discovery depends on the requested kind, not the presence of fixable diagnostics.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);

        // Compare omitted/empty filters, unrelated kinds, a child kind, and the matching source kinds.
        var actions = await GetActionsAsync(server, only is null ? null : only.Length == 0 ? [] : [only]);
        var sourceActions = actions.Where(action => action.Kind == CodeActionKind.SourceFixAll);
        if (only is "source" or "source.fixAll")
        {
            // An explicit request for this action or its ancestor returns a deferred action with no command or edit.
            var action = Assert.Single(sourceActions);
            Assert.Null(action.Command);
            Assert.Null(action.Edit);

            // Resolve data must identify the aggregate document action and the document it will operate on.
            var data = Assert.IsType<JsonElement>(action.Data);
            Assert.True(data.GetProperty("FixAllInDocument").GetBoolean());
            Assert.Equal(server.GetLocations("caret").Single().DocumentUri,
                JsonSerializer.Deserialize<TextDocumentIdentifier>(data.GetProperty("TextDocument"), ProtocolConversions.LspJsonSerializerOptions)!.DocumentUri);
        }
        else
        {
            // Unrestricted, unrelated, and child-only filters must not expose the source action.
            Assert.Empty(sourceActions);
        }
    }

    [Theory, CombinatorialData]
    public async Task RequestingFixAllPreservesRequestedRefactorings(bool mutatingLspWorkspace)
    {
        var source = "class C { void M() { {|caret:|}int value = 1; } }";

        // Use a caret position offering ordinary refactorings, then add source.fixAll to the same request filter.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var ordinary = await GetActionsAsync(server, ["refactor"]);
        var mixed = await GetActionsAsync(server, ["refactor", "source.fixAll"]);

        // Adding the source action must leave the ordinary action list intact and in the same order.
        Assert.Single(mixed, action => action.Kind == CodeActionKind.SourceFixAll);
        Assert.NotEmpty(ordinary);
        Assert.Equal(ordinary.Select(action => action.Title),
            mixed.Where(action => action.Kind != CodeActionKind.SourceFixAll).Select(action => action.Title));
    }

    private static async Task<LSP.CodeAction[]> GetActionsAsync(TestLspServer server, string[]? only)
    {
        var location = server.GetLocations("caret").Single();
        var actions = await server.ExecuteRequestAsync<CodeActionParams, LSP.CodeAction[]>(
            Methods.TextDocumentCodeActionName,
            new CodeActionParams
            {
                TextDocument = CreateTextDocumentIdentifier(location.DocumentUri),
                Range = location.Range,
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
    public async Task FixAllIncludesOnlyDiagnosticsAtOrAboveMinimumSeverity(
        bool mutatingLspWorkspace,
        [CombinatorialValues(DiagnosticSeverity.Error, DiagnosticSeverity.Warning, DiagnosticSeverity.Info, DiagnosticSeverity.Hidden)] DiagnosticSeverity threshold)
    {
        var source = "// error warning info hidden\n{|caret:|}class C { }";

        // Share an ID between warning and info occurrences to check filtering both between IDs and within one ID.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002", "FA0003"]);
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "error", DiagnosticSeverity.Error), new("FA0002", "warning"),
                new("FA0002", "info", DiagnosticSeverity.Info), new("FA0003", "hidden", DiagnosticSeverity.Hidden))], [fixer]));

        // Apply each threshold, including diagnostics exactly at the selected severity.
        var changed = await ChangeAsync(document, server, threshold);

        // Lower-severity markers must remain untouched; hidden diagnostics participate only at the Hidden threshold.
        var expected = threshold switch
        {
            DiagnosticSeverity.Error => "// fixed warning info hidden\nclass C { }",
            DiagnosticSeverity.Warning => "// fixed fixed info hidden\nclass C { }",
            DiagnosticSeverity.Info => "// fixed fixed fixed hidden\nclass C { }",
            DiagnosticSeverity.Hidden => "// fixed fixed fixed fixed\nclass C { }",
            _ => throw new InvalidOperationException(),
        };
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
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
    public async Task FixAllRespectsEditorConfigSeverityOverrides(bool mutatingLspWorkspace)
    {
        var source = "// promoted demoted disabled\n{|caret:|}class C { }";
        var expected = "// fixed demoted disabled\nclass C { }";

        // Start with descriptor severities that will be overridden by editorconfig settings.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002", "FA0003"]);
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "promoted", DiagnosticSeverity.Info), new("FA0002", "demoted"), new("FA0003", "disabled"))], [fixer]));

        // Promote info to warning, demote warning to suggestion, and disable the remaining warning.
        var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(document.FilePath!)!, ".editorconfig");
        var editorConfig = """
            root = true

            [*]
            dotnet_diagnostic.FA0001.severity = warning
            dotnet_diagnostic.FA0002.severity = suggestion
            dotnet_diagnostic.FA0003.severity = none
            """;
        var project = document.Project.AddAnalyzerConfigDocument(".editorconfig", SourceText.From(editorConfig), filePath: path).Project;

        // Use effective severities: only the promoted diagnostic should meet the default Warning threshold.
        var changed = await ChangeAsync(project.GetDocument(document.Id)!, server);
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Equal(["FA0001"], fixer.Invocations);
    }

    [Theory, CombinatorialData]
    public async Task FixAllSkipsDiagnosticsWithoutCodeFixProviders(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// warning\nclass C { }";

        // Report an eligible warning without registering a code-fix provider for it.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new Rule("FA0001", "warning"))], []));

        // Attempt document Fix All even though no provider can handle the diagnostic.
        var changed = await ChangeAsync(document, server);

        // Leave the document unchanged when no code fix is available.
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
    }

    [Theory, CombinatorialData]
    public async Task FixAllSkipsFixesWithoutFixAllSupport(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// warning\nclass C { }";

        // Register a code-fix provider that cannot supply a Fix All provider.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001"], hasFixAll: false);
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));

        // Attempt document Fix All rather than applying the available single-diagnostic fix.
        var changed = await ChangeAsync(document, server);

        // Skip the fix without changing text or invoking a Fix All provider.
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Empty(fixer.Invocations);
    }

    [Theory, CombinatorialData]
    public async Task FixAllSkipsFixesWithoutDocumentScope(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// warning\nclass C { }";

        // Register a Fix All provider that supports project scope but not document scope.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001"], documentScope: false);
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));

        // Request document Fix All without falling back to the provider's broader scope.
        var changed = await ChangeAsync(document, server);

        // Keep the document unchanged and never invoke the project-only provider.
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Empty(fixer.Invocations);
    }

    [Theory, CombinatorialData]
    public async Task FixAllSkipsFixesRequiringOptions(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// warning\nclass C { }";

        // Register an action that requires user-supplied options before it can run.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001"], options: true);
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));

        // Attempt unattended document Fix All without requesting user input.
        var changed = await ChangeAsync(document, server);

        // Skip the interactive action rather than invoking its Fix All provider.
        Assert.Equal(expected, (await changed.GetTextAsync()).ToString());
        Assert.Empty(fixer.Invocations);
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
    public async Task ResolveCreatesDocumentsOnlyWhenSupported(bool mutatingLspWorkspace, bool supportsCreate)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// fixed\nclass C { }";
        var expectedAdded = "class Added { }";
        var addedPath = TestHelpers.GetRootedPath("Added.cs");

        // Advertise file creation independently of ordinary text-edit support.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace,
            initializationOptions: ResourceOperationOptions(ResourceOperationKind.Create, supportsCreate));
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            return solution.AddDocument(DocumentId.CreateNewId(document.Project.Id), "Added.cs",
                SourceText.From(expectedAdded), filePath: addedPath);
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

        // Create the new file before supplying its contents, and include the original file's fix.
        Assert.Equal(3, operations.Length);
        var create = Assert.IsType<CreateFile>(operations[0]);
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(addedPath), create.DocumentUri);
        Assert.Equal(expectedAdded, ApplyDocumentEdit(operations, SourceText.From(""), create.DocumentUri));
        Assert.Equal(expected, ApplyDocumentEdit(operations, await document.GetTextAsync(), document.GetURI()));
    }

    [Theory, CombinatorialData]
    public async Task ResolveRenamesDocumentsOnlyWhenSupported(bool mutatingLspWorkspace, bool supportsRename)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// fixed\nclass C { }";
        var renamedPath = TestHelpers.GetRootedPath("Renamed.cs");

        // Combine a text edit with a physical rename, not just a change to the document's display name.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace,
            initializationOptions: ResourceOperationOptions(ResourceOperationKind.Rename, supportsRename));
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            return solution.WithDocumentName(document.Id, "Renamed.cs").WithDocumentFilePath(document.Id, renamedPath);
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

        // Apply text at the old URI before renaming the file.
        Assert.Equal(2, operations.Length);
        Assert.IsType<TextDocumentEdit>(operations[0]);
        Assert.Equal(expected, ApplyDocumentEdit(operations, await document.GetTextAsync(), document.GetURI()));
        var rename = Assert.IsType<RenameFile>(operations[1]);
        Assert.Equal(document.GetURI(), rename.OldDocumentUri);
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(renamedPath), rename.NewDocumentUri);
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
    public Task FixAllRejectsReferenceChangesWithoutLosingEarlierEdits(bool mutatingLspWorkspace)
        => VerifyRejectedProjectChangeAsync(mutatingLspWorkspace, project => project.WithMetadataReferences([]));

    [Theory, CombinatorialData]
    public Task FixAllRejectsProjectOptionChangesWithoutLosingEarlierEdits(bool mutatingLspWorkspace)
        => VerifyRejectedProjectChangeAsync(mutatingLspWorkspace, project =>
            project.WithCompilationOptions(project.CompilationOptions!.WithConcurrentBuild(!project.CompilationOptions.ConcurrentBuild)));

    private async Task VerifyRejectedProjectChangeAsync(bool mutatingLspWorkspace, Func<Project, Project> changeProject)
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
                return changeProject(solution.GetRequiredProject(document.Project.Id)).Solution;

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
        Assert.Equal(document.Project.CompilationOptions, changed.Project.CompilationOptions);
        Assert.Equal(document.Project.MetadataReferences, changed.Project.MetadataReferences);
        Assert.Single(logs, message => message.Contains("FA0002", StringComparison.Ordinal));
    }

    [Theory, CombinatorialData]
    public async Task ResolveUsesUpdatedMinimumSeveritySetting(bool mutatingLspWorkspace)
    {
        var source = "// info warning\n{|caret:|}class C { }";
        var expected = "// fixed fixed\nclass C { }";

        // Discover the source action while the default Warning threshold still excludes the info diagnostic.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var fixer = new TestFixer(["FA0001", "FA0002"]);
        await AddReferenceAsync(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "info", DiagnosticSeverity.Info), new("FA0002", "warning"))], [fixer]));
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));

        // Lower the threshold after discovery to confirm that settings are read when edits are computed.
        var globalOptions = server.TestWorkspace.GetService<IGlobalOptionService>();
        Assert.Equal(DiagnosticSeverity.Warning, globalOptions.GetOption(LspOptionsStorage.FixAllMinimumSeverity));
        globalOptions.SetGlobalOption(LspOptionsStorage.FixAllMinimumSeverity, DiagnosticSeverity.Info);

        // The already-discovered action must now fix both info and warning occurrences.
        var resolved = await ResolveAsync(server, action);
        var uri = server.GetLocations("caret").Single().DocumentUri;
        Assert.Equal(expected, ApplySingleDocumentEdit(resolved, await server.GetDocumentTextAsync(uri), uri));
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
    public async Task ResolveGeneratesMissingInterfaceMethod(bool mutatingLspWorkspace)
    {
        // Leave an interface member unimplemented to exercise the built-in implementation fix.
        var source = """
            interface I
            {
                void M();
            }

            class C : I
            {
                {|caret:|}
            }
            """;
        var expected = """
            interface I
            {
                void M();
            }

            class C : I
            {
                public void M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """;
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var uri = server.GetLocations("caret").Single().DocumentUri;

        // Resolve the real source action to check that document-local member generation is eligible.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var resolved = await ResolveAsync(server, action);

        // Generating a throwing stub is supported by an unattended workspace edit.
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

        // Resolving through LSP must emit only one edit for the shared physical file.
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        var resolved = await ResolveAsync(server, action);
        Assert.Equal(expected,
            ApplySingleDocumentEdit(resolved, await server.GetDocumentTextAsync(server.GetLocations("caret").Single().DocumentUri),
                server.GetLocations("caret").Single().DocumentUri).ReplaceLineEndings("\n"));
    }

    [Theory, CombinatorialData]
    public async Task FixAllPropagatesLinkedChangesBeforeLaterFixes(bool mutatingLspWorkspace)
    {
        var source = "// alpha beta\n{|caret:|}class C { }";
        var otherSource = "class Other { }";
        var expected = "// fixed fixed\nclass C { }";
        var expectedOther = "class Changed { }";
        var otherPath = TestHelpers.GetRootedPath("Other.cs");

        // Share both the requested file and a sibling between project contexts.
        await using var server = await CreateLinkedDocumentTestLspServerAsync(source, mutatingLspWorkspace);
        var solution = server.TestWorkspace.CurrentSolution;
        foreach (var projectId in solution.ProjectIds)
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId), "Other.cs", SourceText.From(otherSource), filePath: otherPath);
        await server.TestWorkspace.ChangeSolutionAsync(solution);
        var observedOtherTexts = new List<string>();
        var fixer = new TestFixer(["FA0001", "FA0002"], async (document, diagnostics, cancellationToken) =>
        {
            var changed = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            var otherIds = document.Project.Solution.GetDocumentIdsWithFilePath(otherPath);
            if (diagnostics[0].Id == "FA0001")
                return changed.WithDocumentText(otherIds[0], SourceText.From(expectedOther));

            // The later fix must observe the earlier edit in every context, not only in the final LSP merge.
            foreach (var id in otherIds)
                observedOtherTexts.Add((await document.Project.Solution.GetRequiredDocument(id).GetTextAsync(cancellationToken)).ToString());
            return changed;
        });
        var document = GetDocumentWithReference(server, new TestReference(
            [new MarkerAnalyzer(new("FA0001", "alpha"), new("FA0002", "beta"))], [fixer]));

        var result = await ChangeAsync(document, server);

        Assert.Equal(expected, (await result.GetTextAsync()).ToString().ReplaceLineEndings("\n"));
        Assert.NotEmpty(observedOtherTexts);
        Assert.All(observedOtherTexts, text => Assert.Equal(expectedOther, text));
        foreach (var id in result.Project.Solution.GetDocumentIdsWithFilePath(otherPath))
            Assert.Equal(expectedOther, (await result.Project.Solution.GetRequiredDocument(id).GetTextAsync()).ToString());
    }

    [Theory, CombinatorialData]
    public async Task FixAllRejectsConflictingLinkedDocumentChanges(bool mutatingLspWorkspace)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var conflictingSource = "// different\nclass C { }";
        var expected = "// warning\nclass C { }";

        await using var server = await CreateLinkedDocumentTestLspServerAsync(source, mutatingLspWorkspace);

        // Mix a valid local fix with different text for the linked context of the same file.
        var fixer = new TestFixer(["FA0001"], async (document, diagnostics, cancellationToken) =>
        {
            var solution = await ReplaceDiagnosticsAsync(document, diagnostics, "fixed", cancellationToken);
            foreach (var linkedId in document.GetLinkedDocumentIds())
                solution = solution.WithDocumentText(linkedId, SourceText.From(conflictingSource));
            return solution;
        });
        await AddReferenceAsync(server, new TestReference([new MarkerAnalyzer(new Rule("FA0001", "warning"))], [fixer]));
        var document = server.TestWorkspace.CurrentSolution.Projects.First().Documents.Single();

        // Reject the whole candidate, preserving both original contexts and recording one rejection.
        var logs = new List<string>();
        var changed = await ChangeAsync(document, server, log: logs.Add);

        Assert.Equal(expected, (await changed.GetTextAsync()).ToString().ReplaceLineEndings("\n"));
        var linked = changed.Project.Solution.GetDocument(Assert.Single(changed.GetLinkedDocumentIds()))!;
        Assert.Equal(expected, (await linked.GetTextAsync()).ToString().ReplaceLineEndings("\n"));
        Assert.Single(logs);
    }

    private Task<TestLspServer> CreateLinkedDocumentTestLspServerAsync(string source, bool mutatingLspWorkspace)
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
        return CreateXmlTestLspServerAsync(workspace, mutatingLspWorkspace);
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
        Assert.Single(logs, message => message.Contains("unsupported", StringComparison.Ordinal));
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
        bool hasFixAll = true,
        bool documentScope = true,
        bool options = false,
        bool unsupportedOperations = false) : CodeFixProvider
    {
        private readonly bool _unsupportedOperations = unsupportedOperations;
        public List<string> Invocations { get; } = [];
        public List<FixAllScope> RequestedScopes { get; } = [];
        public override ImmutableArray<string> FixableDiagnosticIds => ids;
        public override FixAllProvider? GetFixAllProvider() => hasFixAll ? new TestFixAllProvider(this, documentScope) : null;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            CodeAction action = options ? new OptionsAction() : CodeAction.Create(
                "First", cancellationToken => ReplaceDiagnosticsAsync(context.Document, context.Diagnostics, "fixed", cancellationToken), "first");
            context.RegisterCodeFix(action, context.Diagnostics);
            return Task.CompletedTask;
        }

        private sealed class TestFixAllProvider(TestFixer fixer, bool documentScope) : FixAllProvider
        {
            public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
                => documentScope ? [FixAllScope.Document] : [FixAllScope.Project];

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
