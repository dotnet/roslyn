// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeRefactoringService;

[UseExportProvider]
public class CodeRefactoringServiceTest
{
    [Fact]
    public async Task TestExceptionInComputeRefactorings()
        => await VerifyRefactoringDisabledAsync<ErrorCases.ExceptionInCodeActions>();

    [Fact]
    public async Task TestExceptionInComputeRefactoringsAsync()
        => await VerifyRefactoringDisabledAsync<ErrorCases.ExceptionInComputeRefactoringsAsync>();

    [Fact]
    public async Task TestProjectRefactoringAsync()
    {
        var code = @"
    a
";

        using var workspace = TestWorkspace.CreateCSharp(code, composition: FeaturesTestCompositions.Features);
        var refactoringService = workspace.GetService<ICodeRefactoringService>();

        var reference = new StubAnalyzerReference();
        var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
        var document = project.Documents.Single();
        var refactorings = await refactoringService.GetRefactoringsAsync(document, TextSpan.FromBounds(0, 0), CodeActionOptions.DefaultProvider, CancellationToken.None);

        var stubRefactoringAction = refactorings.Single(refactoring => refactoring.CodeActions.FirstOrDefault().action?.Title == nameof(StubRefactoring));
        Assert.True(stubRefactoringAction is object);
    }

    [ExportCodeRefactoringProvider(InternalLanguageNames.TypeScript, Name = "TypeScript CodeRefactoring Provider"), Shared, PartNotDiscoverable]
    internal sealed class TypeScriptCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptCodeRefactoringProvider()
        {
        }

        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            context.RegisterRefactoring(CodeAction.Create($"Blocking=false", _ => Task.FromResult<Document>(null)));

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task TestTypeScriptRefactorings()
    {
        var composition = FeaturesTestCompositions.Features.AddParts(typeof(TypeScriptCodeRefactoringProvider));

        using var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""TypeScript"">
        <Document FilePath=""Test"">abc</Document>
    </Project>
</Workspace>", composition: composition);

        var refactoringService = workspace.GetService<ICodeRefactoringService>();

        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var optionsProvider = workspace.GlobalOptions.GetCodeActionOptionsProvider();
        var refactorings = await refactoringService.GetRefactoringsAsync(document, TextSpan.FromBounds(0, 0), optionsProvider, CancellationToken.None);
        Assert.Equal($"Blocking=false", refactorings.Single().CodeActions.Single().action.Title);
    }

    private static async Task VerifyRefactoringDisabledAsync<T>()
        where T : CodeRefactoringProvider
    {
        using var workspace = TestWorkspace.CreateCSharp(@"class Program {}",
            composition: EditorTestCompositions.EditorFeatures.AddParts(typeof(T)));

        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();

        var errorReported = false;
        errorReportingService.OnError = message => errorReported = true;

        var refactoringService = workspace.GetService<ICodeRefactoringService>();
        var codeRefactoring = workspace.ExportProvider.GetExportedValues<CodeRefactoringProvider>().OfType<T>().Single();

        var project = workspace.CurrentSolution.Projects.Single();
        var document = project.Documents.Single();
        var extensionManager = (EditorLayerExtensionManager.ExtensionManager)document.Project.Solution.Services.GetRequiredService<IExtensionManager>();
        var result = await refactoringService.GetRefactoringsAsync(document, TextSpan.FromBounds(0, 0), CodeActionOptions.DefaultProvider, CancellationToken.None);
        Assert.True(extensionManager.IsDisabled(codeRefactoring));
        Assert.False(extensionManager.IsIgnored(codeRefactoring));

        Assert.True(errorReported);
    }

    internal class StubRefactoring : CodeRefactoringProvider
    {
        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            context.RegisterRefactoring(CodeAction.Create(
                nameof(StubRefactoring),
                cancellationToken => Task.FromResult(context.Document),
                equivalenceKey: nameof(StubRefactoring)));

            return Task.CompletedTask;
        }
    }

    private class StubAnalyzerReference : AnalyzerReference, ICodeRefactoringProviderFactory
    {
        private readonly ImmutableArray<CodeRefactoringProvider> _refactorings;

        public StubAnalyzerReference() : this(new StubRefactoring()) { }

        public StubAnalyzerReference(params CodeRefactoringProvider[] codeRefactorings)
            => _refactorings = codeRefactorings.ToImmutableArray();

        public override string Display => nameof(StubAnalyzerReference);

        public override string FullPath => string.Empty;

        public override object Id => nameof(StubAnalyzerReference);

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            => ImmutableArray<DiagnosticAnalyzer>.Empty;

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
            => ImmutableArray<DiagnosticAnalyzer>.Empty;

        public ImmutableArray<CodeRefactoringProvider> GetRefactorings()
            => _refactorings;
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62877")]
    public async Task TestAdditionalDocumentRefactoringAsync()
    {
        using var workspace = TestWorkspace.CreateCSharp("", composition: FeaturesTestCompositions.Features);
        var refactoringService = workspace.GetService<ICodeRefactoringService>();

        var refactoring1 = new NonSourceFileRefactoringWithDocumentKindsAndExtensions();
        var refactoring2 = new NonSourceFileRefactoringWithDocumentKinds();
        var refactoring3 = new NonSourceFileRefactoringWithDocumentExtensions();
        var refactoring4 = new NonSourceFileRefactoringWithoutDocumentKindsAndExtensions();
        var reference = new StubAnalyzerReference(refactoring1, refactoring2, refactoring3, refactoring4);
        var project = workspace.CurrentSolution.Projects.Single()
            .AddAnalyzerReference(reference)
            .AddAdditionalDocument("test.txt", "", filePath: "test.txt").Project
            .AddAdditionalDocument("test.log", "", filePath: "test.log").Project;

        // Verify available refactorings for .txt additional document
        var txtAdditionalDocument = project.AdditionalDocuments.Single(t => t.Name == "test.txt");
        var txtRefactorings = await refactoringService.GetRefactoringsAsync(txtAdditionalDocument, TextSpan.FromBounds(0, 0), CodeActionOptions.DefaultProvider, CancellationToken.None);
        Assert.Equal(2, txtRefactorings.Length);
        var txtRefactoringTitles = txtRefactorings.Select(s => s.CodeActions.Single().action.Title).ToImmutableArray();
        Assert.Contains(refactoring1.Title, txtRefactoringTitles);
        Assert.Contains(refactoring2.Title, txtRefactoringTitles);

        // Verify code refactoring application
        var codeAction = txtRefactorings.Single(s => s.CodeActions.Single().action.Title == refactoring1.Title).CodeActions.Single().action;
        var solution = await codeAction.GetChangedSolutionInternalAsync(project.Solution, CodeAnalysisProgress.None);
        var changedtxtDocument = solution.Projects.Single().AdditionalDocuments.Single(t => t.Id == txtAdditionalDocument.Id);
        Assert.Empty(txtAdditionalDocument.GetTextSynchronously(CancellationToken.None).ToString());
        Assert.Equal(refactoring1.Title, changedtxtDocument.GetTextSynchronously(CancellationToken.None).ToString());

        // Verify available refactorings for .log additional document
        var logAdditionalDocument = project.AdditionalDocuments.Single(t => t.Name == "test.log");
        var logRefactorings = await refactoringService.GetRefactoringsAsync(logAdditionalDocument, TextSpan.FromBounds(0, 0), CodeActionOptions.DefaultProvider, CancellationToken.None);
        var logRefactoring = Assert.Single(logRefactorings);
        var logRefactoringTitle = logRefactoring.CodeActions.Single().action.Title;
        Assert.Equal(refactoring2.Title, logRefactoringTitle);
    }

    [Fact]
    public async Task TestAnalyzerConfigDocumentRefactoringAsync()
    {
        using var workspace = TestWorkspace.CreateCSharp("", composition: FeaturesTestCompositions.Features);
        var refactoringService = workspace.GetService<ICodeRefactoringService>();

        var refactoring1 = new NonSourceFileRefactoringWithDocumentKindsAndExtensions();
        var refactoring2 = new NonSourceFileRefactoringWithDocumentKinds();
        var refactoring3 = new NonSourceFileRefactoringWithDocumentExtensions();
        var refactoring4 = new NonSourceFileRefactoringWithoutDocumentKindsAndExtensions();
        var reference = new StubAnalyzerReference(refactoring1, refactoring2, refactoring3, refactoring4);
        var project = workspace.CurrentSolution.Projects.Single()
            .AddAnalyzerReference(reference)
            .AddAnalyzerConfigDocument(".editorconfig", SourceText.From(""), filePath: "c:\\.editorconfig").Project
            .AddAnalyzerConfigDocument(".globalconfig", SourceText.From("is_global = true"), filePath: "c:\\.globalconfig").Project;

        // Verify available refactorings for .editorconfig document
        var editorConfig = project.AnalyzerConfigDocuments.Single(t => t.Name == ".editorconfig");
        var editorConfigRefactorings = await refactoringService.GetRefactoringsAsync(editorConfig, TextSpan.FromBounds(0, 0), CodeActionOptions.DefaultProvider, CancellationToken.None);
        Assert.Equal(2, editorConfigRefactorings.Length);
        var editorConfigRefactoringTitles = editorConfigRefactorings.Select(s => s.CodeActions.Single().action.Title).ToImmutableArray();
        Assert.Contains(refactoring1.Title, editorConfigRefactoringTitles);
        Assert.Contains(refactoring2.Title, editorConfigRefactoringTitles);

        // Verify code refactoring application
        var codeAction = editorConfigRefactorings.Single(s => s.CodeActions.Single().action.Title == refactoring1.Title).CodeActions.Single().action;
        var solution = await codeAction.GetChangedSolutionInternalAsync(project.Solution, CodeAnalysisProgress.None);
        var changedEditorConfig = solution.Projects.Single().AnalyzerConfigDocuments.Single(t => t.Id == editorConfig.Id);
        Assert.Empty(editorConfig.GetTextSynchronously(CancellationToken.None).ToString());
        Assert.Equal(refactoring1.Title, changedEditorConfig.GetTextSynchronously(CancellationToken.None).ToString());

        // Verify available refactorings for .globalconfig document
        var globalConfig = project.AnalyzerConfigDocuments.Single(t => t.Name == ".globalconfig");
        var globalConfigRefactorings = await refactoringService.GetRefactoringsAsync(globalConfig, TextSpan.FromBounds(0, 0), CodeActionOptions.DefaultProvider, CancellationToken.None);
        var globalConfigRefactoring = Assert.Single(globalConfigRefactorings);
        var globalConfigRefactoringTitle = globalConfigRefactoring.CodeActions.Single().action.Title;
        Assert.Equal(refactoring2.Title, globalConfigRefactoringTitle);
    }

    internal abstract class AbstractNonSourceFileRefactoring : CodeRefactoringProvider
    {
        public string Title { get; }

        protected AbstractNonSourceFileRefactoring(string title)
            => Title = title;

        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            context.RegisterRefactoring(CodeAction.Create(Title,
                createChangedSolution: async ct =>
                {
                    var document = context.TextDocument;
                    var text = await document.GetTextAsync(ct).ConfigureAwait(false);
                    var newText = SourceText.From(text.ToString() + Title);
                    if (document.Kind == TextDocumentKind.AdditionalDocument)
                        return document.Project.Solution.WithAdditionalDocumentText(document.Id, newText);
                    return document.Project.Solution.WithAnalyzerConfigDocumentText(document.Id, newText);
                }));

            return Task.CompletedTask;
        }
    }

#pragma warning disable RS0034 // Exported parts should be marked with 'ImportingConstructorAttribute'
    [ExportCodeRefactoringProvider(
        LanguageNames.CSharp,
        DocumentKinds = new[] { nameof(TextDocumentKind.AdditionalDocument), nameof(TextDocumentKind.AnalyzerConfigDocument) },
        DocumentExtensions = new[] { ".txt", ".editorconfig" })]
    [Shared]
    internal sealed class NonSourceFileRefactoringWithDocumentKindsAndExtensions : AbstractNonSourceFileRefactoring
    {
        public NonSourceFileRefactoringWithDocumentKindsAndExtensions() : base(nameof(NonSourceFileRefactoringWithDocumentKindsAndExtensions)) { }
    }

    [ExportCodeRefactoringProvider(
        LanguageNames.CSharp,
        DocumentKinds = new[] { nameof(TextDocumentKind.AdditionalDocument), nameof(TextDocumentKind.AnalyzerConfigDocument) })]
    [Shared]
    internal sealed class NonSourceFileRefactoringWithDocumentKinds : AbstractNonSourceFileRefactoring
    {
        public NonSourceFileRefactoringWithDocumentKinds() : base(nameof(NonSourceFileRefactoringWithDocumentKinds)) { }
    }

    [ExportCodeRefactoringProvider(
        LanguageNames.CSharp,
        DocumentExtensions = new[] { ".txt", ".editorconfig" })]
    [Shared]
    internal sealed class NonSourceFileRefactoringWithDocumentExtensions : AbstractNonSourceFileRefactoring
    {
        public NonSourceFileRefactoringWithDocumentExtensions() : base(nameof(NonSourceFileRefactoringWithDocumentExtensions)) { }
    }

    [ExportCodeRefactoringProvider(LanguageNames.CSharp)]
    [Shared]
    internal sealed class NonSourceFileRefactoringWithoutDocumentKindsAndExtensions : AbstractNonSourceFileRefactoring
    {
        public NonSourceFileRefactoringWithoutDocumentKindsAndExtensions() : base(nameof(NonSourceFileRefactoringWithoutDocumentKindsAndExtensions)) { }
    }
#pragma warning restore RS0034 // Exported parts should be marked with 'ImportingConstructorAttribute'
}
