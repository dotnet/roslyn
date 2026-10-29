// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeFixes;

[UseExportProvider]
public sealed class DocumentFixAllProviderSelectionTests
{
    [Fact]
    public Task SkipsProviderWithoutFixAllSupport()
        => VerifyProviderFallbackAsync(hasFixAll: false, documentScope: true);

    [Fact]
    public Task SkipsProviderWithoutDocumentScope()
        => VerifyProviderFallbackAsync(hasFixAll: true, documentScope: false);

    private static async Task VerifyProviderFallbackAsync(bool hasFixAll, bool documentScope)
    {
        using var workspace = TestWorkspace.CreateCSharp("class C { }", composition: FeaturesTestCompositions.Features);
        var reference = new TestAnalyzerReferenceByLanguage(
            ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty.Add(LanguageNames.CSharp, [new TestAnalyzer()]));
        var document = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference).Documents.Single();
        var first = new TestFixer(hasFixAll, documentScope);
        var second = new TestFixer(hasFixAll: true, documentScope: true);
        var service = new CodeFixService([], [
            new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(() => first,
                new CodeChangeProviderMetadata("First", before: ["Second"], languages: LanguageNames.CSharp)),
            new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(() => second,
                new CodeChangeProviderMetadata("Second", languages: LanguageNames.CSharp)),
        ], configurationProviders: []);

        // Ordinary actions still prefer the first provider and deduplicate the identical equivalence keys.
        var fixes = await service.GetFixesAsync(document, new TextSpan(0, 0), CancellationToken.None);
        Assert.Same(first, Assert.Single(fixes).Provider);

        var collection = await service.GetDocumentFixAllForIdInSpanAsync(
            document, textSpan: null, "FA0001", DiagnosticSeverity.Warning, CancellationToken.None);

        Assert.NotNull(collection);
        Assert.Same(second, collection.Provider);
        Assert.Contains(FixAllScope.Document, collection.SupportedScopes);
        Assert.Equal("same", Assert.Single(collection.Fixes).Action.EquivalenceKey);
    }

    private sealed class TestAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            "FA0001", "Test", "Test", "Testing", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxTreeAction(context => context.ReportDiagnostic(
                Diagnostic.Create(s_rule, Location.Create(context.Tree, new TextSpan(0, 0)))));
        }
    }

    private sealed class TestFixer(bool hasFixAll, bool documentScope) : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ["FA0001"];

        public override FixAllProvider? GetFixAllProvider()
            => hasFixAll ? new TestFixAllProvider(documentScope) : null;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create("Fix", _ => Task.FromResult(context.Document), "same"), context.Diagnostics);
            return Task.CompletedTask;
        }

        private sealed class TestFixAllProvider(bool documentScope) : FixAllProvider
        {
            public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
                => documentScope ? [FixAllScope.Document] : [FixAllScope.Project];

            public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
                => throw new InvalidOperationException("Provider selection must not compute the fix.");
        }
    }
}
