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
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.MiscellaneousFiles;
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
    [Fact]
    public async Task CapabilitiesAdvertiseFixAllAndResolveSupport()
    {
        var source = "";

        // Inspect initialization capabilities without needing any document diagnostics.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace: false);
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
        var source = """
            class C
            {
                void M()
                {
                    int firstAssigned = 1;
                    int firstUnused;
                    int secondAssigned = 2;
                    int secondUnused;
                    {|caret:|}return;
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

        return VerifyResolveFixesAllOccurrencesAcrossDiagnosticIdsAsync(
            source: source,
            expectedText: expected,
            LanguageNames.CSharp,
            mutatingLspWorkspace);
    }

    private async Task VerifyResolveFixesAllOccurrencesAcrossDiagnosticIdsAsync(
        string source, string expectedText, string language, bool mutatingLspWorkspace)
    {
        // Use built-in fixes for repeated occurrences of two compiler diagnostic IDs.
        await using var server = await CreateTestLspServerAsync(
            [source], language, mutatingLspWorkspace, initializationOptions: null);

        // Discover the deferred action, saving the workspace snapshot to check that resolution never applies changes.
        var before = server.TestWorkspace.CurrentSolution;
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));
        Assert.Null(action.Edit);
        Assert.Null(action.Command);

        // Resolve and apply the returned edit to a copy of the original text to check that every occurrence is fixed.
        var resolved = await ResolveAsync(server, action);
        var originalText = await server.GetDocumentTextAsync(server.GetLocations("caret").Single().DocumentUri);
        Assert.Equal(expectedText.ReplaceLineEndings("\n"),
            ApplySingleDocumentEdit(resolved, originalText, server.GetLocations("caret").Single().DocumentUri).ReplaceLineEndings("\n"));

        // Resolution computes edits without changing the live workspace.
        Assert.Same(before, server.TestWorkspace.CurrentSolution);
    }

    [Fact]
    public async Task FixAllHandlesDiagnosticsMovedOrRemovedByEarlierFixes()
    {
        var source = "// expand remove later\n{|caret:|}class C { }";
        var expected = "// expanded_text gone fixed\nclass C { }";

        // Make the first fix lengthen the text and eliminate a later diagnostic, invalidating the original spans.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace: false);
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

    [Fact]
    public async Task FixAllStopsRetryingWithoutDiagnosticProgress()
    {
        var source = "// alpha beta\n{|caret:|}class C { }";
        var expected = "// alpha fixed\nclass C { }\n// changed";

        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace: false);
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

    [Fact]
    public async Task FixAllSkipsSuppressedAndDisabledDiagnostics()
    {
        var source = "#pragma warning disable FA0001\n// suppressed disabled enabled\n{|caret:|}class C { }";
        var expected = "#pragma warning disable FA0001\n// suppressed disabled fixed\nclass C { }";

        // Combine pragma-suppressed, disabled-by-default, and enabled hidden diagnostics.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace: false);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FixAllFallsBackFromInteractiveProvider(bool nested)
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// fixed\nclass C { }";

        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace: false);
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
    public async Task ResolveUsesDocumentTextUpdatedAfterDiscovery(bool mutatingLspWorkspace)
    {
        var source = """
            class C
            {
                void M()
                {
                    int unused;
                    {|caret:|}return;
                }
            }
            """;
        var updatedSource = """
            class C
            {
                void M()
                {
                    int first = 1;
                    int second = 2;
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

        // Discover with CS0168, then replace it with two occurrences of CS0219.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        var uri = server.GetLocations("caret").Single().DocumentUri;
        await server.OpenDocumentAsync(uri);
        var action = Assert.Single(await GetActionsAsync(server, ["source.fixAll"]));

        await server.ReplaceTextAsync(uri,
            (new LSP.Range { Start = new Position(4, 8), End = new Position(4, 19) }, "int first = 1;\n        int second = 2;"));

        // Resolve must recompute diagnostics from the latest didChange snapshot.
        var resolved = await ResolveAsync(server, action);
        // The non-mutating harness keeps its Workspace text unchanged; LSP edits target the didChange snapshot.
        Assert.Equal(expected.ReplaceLineEndings("\n"),
            ApplySingleDocumentEdit(resolved, SourceText.From(updatedSource), uri).ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task FixAllRejectsFixesWithAdditionalOperations()
    {
        var source = "// warning\n{|caret:|}class C { }";
        var expected = "// warning\nclass C { }";

        // Return a valid text edit together with an extra operation that must never execute.
        await using var server = await CreateTestLspServerAsync(source, mutatingLspWorkspace: false);
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

    [Fact]
    public async Task FixAllDiscoversDiagnosticsOnlyInTheRequestedDocument()
    {
        var source = """
            {|caret:|}class C
            {
                void M()
                {
                    int unused;
                    return;
                }
            }
            """;
        var expectedOriginal = source.Replace("{|caret:|}", "");
        var otherSource = expectedOriginal.Replace("class C", "class Other");
        var otherProjectSource = expectedOriginal.Replace("class C", "class Another");
        var expected = """
            class C
            {
                void M()
                {
                    return;
                }
            }
            """;

        // Place CS0168 in the requested document and a sibling document.
        await using var server = await CreateTestLspServerAsync([source, otherSource], mutatingLspWorkspace: false);
        var document = server.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();

        // Add another project with the same warning to catch accidental project- or solution-wide fixing.
        var otherProject = document.Project.Solution.AddProject("OtherProject", "OtherProject", LanguageNames.CSharp)
            .WithMetadataReferences(document.Project.MetadataReferences)
            .AddDocument("OtherProject.cs", SourceText.From(otherProjectSource), filePath: TestHelpers.GetRootedPath("OtherProject.cs"));
        document = otherProject.Project.Solution.GetRequiredDocument(document.Id);

        // Prove that every document is eligible; unchanged outside text must not be a consequence of missing diagnostics.
        var diagnosticService = document.Project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
        foreach (var candidate in document.Project.Solution.Projects.SelectMany(project => project.Documents))
        {
            var diagnostics = await diagnosticService.GetDiagnosticsForSpanAsync(
                candidate, range: null, DiagnosticIdFilter.All, priority: null, DiagnosticKind.All, CancellationToken.None);
            Assert.Single(diagnostics, diagnostic => diagnostic.Id == "CS0168" &&
                diagnostic.Severity == DiagnosticSeverity.Warning && !diagnostic.IsSuppressed);
        }

        // Limit the invocation to the document marked by the caret.
        var changed = await ChangeAsync(document, server);

        // Only its returned snapshot may change; other documents/projects and the input snapshot must stay intact.
        Assert.Equal(expected.ReplaceLineEndings("\n"), (await changed.GetTextAsync()).ToString().ReplaceLineEndings("\n"));
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
