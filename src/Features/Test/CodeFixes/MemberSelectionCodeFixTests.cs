// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.SelectedMembers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.SelectedMembers;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeFixes;

[UseExportProvider]
public sealed class MemberSelectionCodeFixTests
{
    public static TheoryData<string, string, bool> Selections => new()
    {
        { LanguageNames.CSharp, "class C { [|int bad;|] string good; }", true },
        { LanguageNames.CSharp, "class C { int [|bad|]; string good; }", true },
        { LanguageNames.CSharp, "class C { int [||]bad, good; }", true },
        { LanguageNames.CSharp, "class C { [||]int bad, good; }", true },
        { LanguageNames.CSharp, "class C { [|int bad; string good;|] }", false },
        { LanguageNames.CSharp, "class C { int [|bad; string good|]; }", false },
        { LanguageNames.CSharp, "class C { [|int bad1; int bad2;|] }", false },
        { LanguageNames.CSharp, "class C { [|int bad, good;|] }", false },
        { LanguageNames.CSharp, "class C { [|int bad { get; set; }|] string good { get; set; } }", true },
        { LanguageNames.CSharp, "class C { [|int bad { get; set; } string good { get; set; }|] }", false },
        { LanguageNames.CSharp, "class C { [|void bad() { }|] void good() { } }", true },
        { LanguageNames.CSharp, "class C { [|void bad() { } void good() { }|] }", false },
        { LanguageNames.CSharp, "class C { [|void bad() { } int good;|] }", false },
        { LanguageNames.CSharp, "class C { void M() { [|int bad = 0; int good = 0;|] } }", true },
        { LanguageNames.CSharp, "[|class C { int bad; string good; }|]", true },
        { LanguageNames.CSharp, "class [|C { int bad;|] string good; }", true },
        { LanguageNames.CSharp, "class C { int bad;[|\nstring good;|] }", true },
        { LanguageNames.VisualBasic, "Class C\n[|Dim bad As Integer|]\nDim good As String\nEnd Class", true },
        { LanguageNames.VisualBasic, "Class C\nDim [|bad|] As Integer\nDim good As String\nEnd Class", true },
        { LanguageNames.VisualBasic, "Class C\nDim [||]bad, good As Integer\nEnd Class", true },
        { LanguageNames.VisualBasic, "Class C\n[||]Dim bad, good As Integer\nEnd Class", true },
        { LanguageNames.VisualBasic, "Class C\n[|Dim bad As Integer\nDim good As String|]\nEnd Class", false },
        { LanguageNames.VisualBasic, "Class C\nDim [|bad As Integer\nDim good|] As String\nEnd Class", false },
        { LanguageNames.VisualBasic, "Class C\n[|Dim bad1 As Integer\nDim bad2 As Integer|]\nEnd Class", false },
        { LanguageNames.VisualBasic, "Class C\n[|Dim bad, good As Integer|]\nEnd Class", false },
        { LanguageNames.VisualBasic, "Class C\n[|Property bad As Integer|]\nProperty good As String\nEnd Class", true },
        { LanguageNames.VisualBasic, "Class C\n[|Property bad As Integer\nProperty good As String|]\nEnd Class", false },
        { LanguageNames.VisualBasic, "Class C\n[|Sub bad()\nEnd Sub|]\nSub good()\nEnd Sub\nEnd Class", true },
        { LanguageNames.VisualBasic, "Class C\n[|Sub bad()\nEnd Sub\nSub good()\nEnd Sub|]\nEnd Class", false },
        { LanguageNames.VisualBasic, "Class C\n[|Sub bad()\nEnd Sub\nDim good As Integer|]\nEnd Class", false },
        { LanguageNames.VisualBasic, "Class C\nSub M()\n[|Dim bad = 0\nDim good = 0|]\nEnd Sub\nEnd Class", true },
        { LanguageNames.VisualBasic, "[|Class C\nDim bad As Integer\nDim good As String\nEnd Class|]", true },
        { LanguageNames.VisualBasic, "Class [|C\nDim bad As Integer|]\nDim good As String\nEnd Class", true },
        { LanguageNames.VisualBasic, "Class C\nDim bad As Integer[|\nDim good As String|]\nEnd Class", true },
    };

    [Theory]
    [MemberData(nameof(Selections))]
    public async Task FixesAndAvailabilityRespectSelection(string language, string markup, bool expected)
    {
        using var workspace = CreateWorkspace(language, markup);
        var hostDocument = workspace.Documents.Single();
        var document = workspace.CurrentSolution.GetRequiredDocument(hostDocument.Id);
        var selection = hostDocument.CursorPosition is int position
            ? new TextSpan(position, 0)
            : hostDocument.SelectedSpans.Single();
        var text = await document.GetTextAsync();
        // Visual Studio can query just one diagnostic while the actual selection spans multiple members.
        var range = new TextSpan(text.ToString().IndexOf("bad", StringComparison.Ordinal), 3);
        var service = CreateCodeFixService(workspace, language);

        var fixes = await service.GetFixesAsync(document, range, selection, priority: null, CancellationToken.None);
        var firstFix = await service.GetMostSevereFixAsync(document, range, selection, priority: null, CancellationToken.None);

        Assert.Equal(expected, fixes.Any(collection => collection.Provider is TestFixer));
        Assert.Equal(expected, firstFix is not null);
        if (!expected)
            Assert.Empty(fixes);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp, "class C { [|int bad;|] int good; }", true)]
    [InlineData(LanguageNames.CSharp, "class C { [|int bad; int good;|] }", false)]
    [InlineData(LanguageNames.VisualBasic, "Class C\n[|Dim bad As Integer|]\nDim good As Integer\nEnd Class", true)]
    [InlineData(LanguageNames.VisualBasic, "Class C\n[|Dim bad As Integer\nDim good As Integer|]\nEnd Class", false)]
    public async Task SuppressionAndConfigurationFixesRespectSelection(string language, string markup, bool expected)
    {
        using var workspace = CreateWorkspace(language, markup);
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var selection = workspace.Documents.Single().SelectedSpans.Single();
        var service = CreateCodeFixService(workspace, language);

        var fixes = await service.GetFixesAsync(
            document, selection, selection, CodeActionRequestPriority.Lowest, CancellationToken.None);

        Assert.Equal(expected, !fixes.IsEmpty);
        Assert.All(fixes, collection => Assert.IsAssignableFrom<IConfigurationFixProvider>(collection.Provider));
    }

    [Theory]
    [InlineData(LanguageNames.CSharp, "class C { [|int bad; int good;|] }")]
    [InlineData(LanguageNames.VisualBasic, "Class C\n[|Dim bad As Integer\nDim good As Integer|]\nEnd Class")]
    public async Task MissingMemberSelectionServiceDoesNotFilter(string language, string markup)
    {
        var composition = FeaturesTestCompositions.Features.AddExcludedPartTypes(
            typeof(CSharpSelectedMembersService), typeof(VisualBasicSelectedMembersService));
        using var workspace = CreateWorkspace(language, markup, composition);
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var selection = workspace.Documents.Single().SelectedSpans.Single();
        var service = CreateCodeFixService(workspace, language);

        Assert.NotEmpty(await service.GetFixesAsync(document, selection, selection, priority: null, CancellationToken.None));
        Assert.NotNull(await service.GetMostSevereFixAsync(document, selection, selection, priority: null, CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownMemberSelectionDoesNotFilter(bool returnDefault)
    {
        var composition = FeaturesTestCompositions.Features
            .AddExcludedPartTypes(typeof(CSharpSelectedMembersService))
            .AddParts(typeof(UnknownSelectedMembersService));
        using var workspace = CreateWorkspace(LanguageNames.CSharp, "class C { [|int bad; int good;|] }", composition);
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var selection = workspace.Documents.Single().SelectedSpans.Single();
        var memberService = (UnknownSelectedMembersService)document.GetRequiredLanguageService<ISelectedMembersService>();
        memberService.ReturnDefault = returnDefault;
        var service = CreateCodeFixService(workspace, LanguageNames.CSharp);

        Assert.NotEmpty(await service.GetFixesAsync(document, selection, selection, priority: null, CancellationToken.None));
        Assert.NotNull(await service.GetMostSevereFixAsync(document, selection, selection, priority: null, CancellationToken.None));
    }

    [Theory]
    [InlineData(LanguageNames.CSharp, "class C { [|int bad1; int bad2;|] }")]
    [InlineData(LanguageNames.VisualBasic, "Class C\n[|Dim bad1 As Integer\nDim bad2 As Integer|]\nEnd Class")]
    public async Task DiagnosticRangesWithoutUserSelectionAndFixAllAreNotFiltered(string language, string markup)
    {
        using var workspace = CreateWorkspace(language, markup);
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var range = workspace.Documents.Single().SelectedSpans.Single();
        var service = CreateCodeFixService(workspace, language);

        Assert.NotEmpty(await service.GetFixesAsync(document, range, CancellationToken.None));
        Assert.NotNull(await service.GetMostSevereFixAsync(document, range, priority: null, CancellationToken.None));
        Assert.NotNull(await service.GetDocumentFixAllForIdInSpanAsync(
            document, range, TestAnalyzer.Descriptor.Id, DiagnosticSeverity.Warning, CancellationToken.None));
    }

    [Fact]
    public async Task AdditionalDocumentsAreNotFiltered()
    {
        using var workspace = CreateWorkspace(LanguageNames.CSharp, "class C { }");
        var project = workspace.CurrentSolution.Projects.Single();
        var document = project.AddAdditionalDocument("test.txt", SourceText.From("int bad; int good;"), filePath: "test.txt");
        Assert.True(workspace.TryApplyChanges(document.Project.Solution));
        var selection = new TextSpan(0, (await document.GetTextAsync()).Length);
        var service = CreateCodeFixService(workspace, LanguageNames.CSharp);

        Assert.NotEmpty(await service.GetFixesAsync(document, selection, selection, priority: null, CancellationToken.None));
        Assert.NotNull(await service.GetMostSevereFixAsync(document, selection, selection, priority: null, CancellationToken.None));
    }

    private static TestWorkspace CreateWorkspace(string language, string markup, TestComposition? composition = null)
    {
        var workspace = language == LanguageNames.CSharp
            ? TestWorkspace.CreateCSharp(markup, composition: composition)
            : TestWorkspace.CreateVisualBasic(markup, composition: composition);
        var reference = new TestAnalyzerReferenceByLanguage(
            new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>> { [language] = [new TestAnalyzer()] });
        Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences([reference])));
        return workspace;
    }

    private static CodeFixService CreateCodeFixService(TestWorkspace workspace, string language)
    {
        var metadata = new CodeChangeProviderMetadata(nameof(TestFixer), languages: language);
        return new CodeFixService(
            loggers: [],
            [new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(() => new TestFixer(), metadata)],
            workspace.ExportProvider.GetExports<IConfigurationFixProvider, CodeChangeProviderMetadata>());
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        DocumentKinds = [nameof(TextDocumentKind.Document), nameof(TextDocumentKind.AdditionalDocument)])]
    [Shared, PartNotDiscoverable]
    [method: SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Constructed directly by the tests.")]
    private sealed class TestFixer() : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => [TestAnalyzer.Descriptor.Id];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(CodeAction.Create("Fix test diagnostic", _ => Task.FromResult(context.TextDocument.Project.Solution), "TestFix"), context.Diagnostics);
            return Task.CompletedTask;
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    private sealed class TestAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Descriptor = new("TestDiagnostic", "Test", "Test", "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptor];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxTreeAction(c => Report(c.Tree.GetText(c.CancellationToken), (text, span) => Location.Create(c.Tree, span), c.ReportDiagnostic));
            context.RegisterAdditionalFileAction(c => Report(c.AdditionalFile.GetText(c.CancellationToken)!, (text, span) => Location.Create(c.AdditionalFile.Path, span, text.Lines.GetLinePositionSpan(span)), c.ReportDiagnostic));
        }

        private static void Report(SourceText text, Func<SourceText, TextSpan, Location> getLocation, Action<Diagnostic> reportDiagnostic)
        {
            var source = text.ToString();
            for (var start = 0; (start = source.IndexOf("bad", start, StringComparison.Ordinal)) >= 0; start += 3)
                reportDiagnostic(Diagnostic.Create(Descriptor, getLocation(text, new TextSpan(start, 3))));
        }
    }

    [ExportLanguageService(typeof(ISelectedMembersService), LanguageNames.CSharp), Shared, PartNotDiscoverable]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class UnknownSelectedMembersService() : ISelectedMembersService
    {
        public bool ReturnDefault { get; set; }

        public Task<ImmutableArray<SyntaxNode>> GetSelectedMembersAsync(SyntaxTree tree, TextSpan span, bool allowPartialSelection, CancellationToken cancellationToken)
            => Task.FromResult(ReturnDefault ? default : ImmutableArray<SyntaxNode>.Empty);
    }
}
