// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestBase;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UserDiagnosticProviderEngine;

[UseExportProvider]
public class DiagnosticAnalyzerDriverTests
{
    private static readonly TestComposition s_compositionWithMockDiagnosticUpdateSourceRegistrationService = EditorTestCompositions.EditorFeatures;

    [Fact]
    public async Task DiagnosticAnalyzerDriverAllInOne()
    {
        var source = TestResource.AllInOneCSharpCode;

        // AllInOneCSharpCode has no properties with initializers or named types with primary constructors.
        var symbolKindsWithNoCodeBlocks = new HashSet<SymbolKind>
        {
            SymbolKind.Property,
            SymbolKind.NamedType
        };

        var missingSyntaxNodes = new HashSet<SyntaxKind>
        {
            // https://github.com/dotnet/roslyn/issues/44682 - Add to all in one
            SyntaxKind.WithExpression,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.CollectionExpression,
            SyntaxKind.ExpressionElement,
            SyntaxKind.SpreadElement
        };

        var analyzer = new CSharpTrackingDiagnosticAnalyzer();
        using var workspace = EditorTestWorkspace.CreateCSharp(source, TestOptions.Regular, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService);

        var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        var newSolution = workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference })
            .Projects.Single().AddAdditionalDocument(name: "dummy.txt", text: "", filePath: "dummy.txt").Project.Solution;
        workspace.TryApplyChanges(newSolution);

        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        AccessSupportedDiagnostics(analyzer);
        await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, document, new TextSpan(0, document.GetTextAsync().Result.Length));
        analyzer.VerifyAllAnalyzerMembersWereCalled();
        analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds();
        analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds(missingSyntaxNodes);
        analyzer.VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds(symbolKindsWithNoCodeBlocks, true);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908658")]
    public async Task DiagnosticAnalyzerDriverVsAnalyzerDriverOnCodeBlock()
    {
        var methodNames = new string[] { "Initialize", "AnalyzeCodeBlock" };
        var source = """
            [System.Obsolete]
            class C
            {
                int P { get; set; }
                delegate void A();
                delegate string F();
            }
            """;

        var ideEngineAnalyzer = new CSharpTrackingDiagnosticAnalyzer();
        using (var ideEngineWorkspace = EditorTestWorkspace.CreateCSharp(source, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService))
        {
            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(ideEngineAnalyzer));
            ideEngineWorkspace.TryApplyChanges(ideEngineWorkspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var ideEngineDocument = ideEngineWorkspace.CurrentSolution.Projects.Single().Documents.Single();
            await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(ideEngineWorkspace, ideEngineDocument, new TextSpan(0, ideEngineDocument.GetTextAsync().Result.Length));
            foreach (var method in methodNames)
            {
                Assert.False(ideEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.MethodKind == MethodKind.DelegateInvoke && e.ReturnsVoid));
                Assert.False(ideEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.MethodKind == MethodKind.DelegateInvoke && !e.ReturnsVoid));
                Assert.True(ideEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.SymbolKind == SymbolKind.NamedType));
                Assert.False(ideEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.SymbolKind == SymbolKind.Property));
            }
        }

        var compilerEngineAnalyzer = new CSharpTrackingDiagnosticAnalyzer();
        using var compilerEngineWorkspace = EditorTestWorkspace.CreateCSharp(source, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService);
        var compilerEngineCompilation = (CSharpCompilation)compilerEngineWorkspace.CurrentSolution.Projects.Single().GetRequiredCompilationAsync(CancellationToken.None).Result;
        compilerEngineCompilation.GetAnalyzerDiagnostics(new[] { compilerEngineAnalyzer });
        foreach (var method in methodNames)
        {
            Assert.False(compilerEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.MethodKind == MethodKind.DelegateInvoke && e.ReturnsVoid));
            Assert.False(compilerEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.MethodKind == MethodKind.DelegateInvoke && !e.ReturnsVoid));
            Assert.True(compilerEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.SymbolKind == SymbolKind.NamedType));
            Assert.False(compilerEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.SymbolKind == SymbolKind.Property));
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/759")]
    public async Task DiagnosticAnalyzerDriverIsSafeAgainstAnalyzerExceptions()
    {
        var source = TestResource.AllInOneCSharpCode;

        await ThrowingDiagnosticAnalyzer<SyntaxKind>.VerifyAnalyzerEngineIsSafeAgainstExceptionsAsync(async analyzer =>
        {
            using var workspace = EditorTestWorkspace.CreateCSharp(source, TestOptions.Regular, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService);

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create(analyzer));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            return await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, document, new TextSpan(0, document.GetTextAsync().Result.Length));
        });
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908621")]
    public void DiagnosticServiceIsSafeAgainstAnalyzerExceptions_1()
    {
        var analyzer = new ThrowingDiagnosticAnalyzer<SyntaxKind>();
        analyzer.ThrowOn(typeof(DiagnosticAnalyzer).GetProperties().Single().Name);
        AccessSupportedDiagnostics(analyzer);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908621")]
    public void DiagnosticServiceIsSafeAgainstAnalyzerExceptions_2()
    {
        var analyzer = new ThrowingDoNotCatchDiagnosticAnalyzer<SyntaxKind>();
        analyzer.ThrowOn(typeof(DiagnosticAnalyzer).GetProperties().Single().Name);
        var exceptions = new List<Exception>();
        try
        {
            AccessSupportedDiagnostics(analyzer);
        }
        catch (Exception e)
        {
            exceptions.Add(e);
        }

        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task AnalyzerOptionsArePassedToAllAnalyzers()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(TestResource.AllInOneCSharpCode, TestOptions.Regular, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService);

        var additionalDocId = DocumentId.CreateNewId(workspace.CurrentSolution.Projects.Single().Id);
        var additionalText = new TestAdditionalText("add.config", SourceText.From("random text"));
        var options = new AnalyzerOptions(ImmutableArray.Create<AdditionalText>(additionalText));
        var analyzer = new OptionsDiagnosticAnalyzer<SyntaxKind>(expectedOptions: options);
        var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        workspace.TryApplyChanges(workspace.CurrentSolution
            .WithAnalyzerReferences(new[] { analyzerReference })
            .AddAdditionalDocument(additionalDocId, "add.config", additionalText.GetText()!));

        var sourceDocument = workspace.CurrentSolution.Projects.Single().Documents.Single();
        await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, sourceDocument, new TextSpan(0, sourceDocument.GetTextAsync().Result.Length));
        analyzer.VerifyAnalyzerOptions();
    }

    private static void AccessSupportedDiagnostics(DiagnosticAnalyzer analyzer)
    {
        var diagnosticService = new HostDiagnosticAnalyzers(new[] { new AnalyzerImageReference(ImmutableArray.Create(analyzer)) });
        diagnosticService.GetDiagnosticDescriptorsPerReference(new DiagnosticAnalyzerInfoCache());
    }

    private class ThrowingDoNotCatchDiagnosticAnalyzer<TLanguageKindEnum> : ThrowingDiagnosticAnalyzer<TLanguageKindEnum>, IBuiltInAnalyzer where TLanguageKindEnum : struct
    {
        public bool IsHighPriority => false;

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis | DiagnosticAnalyzerCategory.ProjectAnalysis;
    }

    [Fact]
    public async Task AnalyzerCreatedAtCompilationLevelNeedNotBeCompilationAnalyzer()
    {
        var source = @"x";

        using var workspace = EditorTestWorkspace.CreateCSharp(source, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService);

        var analyzer = new CompilationAnalyzerWithSyntaxTreeAnalyzer();
        var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

        var ideEngineDocument = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var diagnostics = await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, ideEngineDocument, new TextSpan(0, ideEngineDocument.GetTextAsync().Result.Length));

        var diagnosticsFromAnalyzer = diagnostics.Where(d => d.Id == "SyntaxDiagnostic");

        Assert.Equal(1, diagnosticsFromAnalyzer.Count());
    }

    private class CompilationAnalyzerWithSyntaxTreeAnalyzer : DiagnosticAnalyzer
    {
        private const string ID = "SyntaxDiagnostic";

        private static readonly DiagnosticDescriptor s_syntaxDiagnosticDescriptor =
            new DiagnosticDescriptor(ID, title: "Syntax", messageFormat: "Syntax", category: "Test", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(s_syntaxDiagnosticDescriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
            => context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);

        public void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
            => context.RegisterSyntaxTreeAction(SyntaxTreeAnalyzer.AnalyzeSyntaxTree);

        private class SyntaxTreeAnalyzer
        {
            public static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
                => context.ReportDiagnostic(Diagnostic.Create(s_syntaxDiagnosticDescriptor, context.Tree.GetRoot().GetFirstToken().GetLocation()));
        }
    }

    [Fact]
    public async Task CodeBlockAnalyzersOnlyAnalyzeExecutableCode()
    {
        var source = """
            using System;
            class C
            {
                void F(int x = 0)
                {
                    Console.WriteLine(0);
                }
            }
            """;

        var analyzer = new CodeBlockAnalyzerFactory();
        using (var ideEngineWorkspace = EditorTestWorkspace.CreateCSharp(source, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService))
        {
            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            ideEngineWorkspace.TryApplyChanges(ideEngineWorkspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var ideEngineDocument = ideEngineWorkspace.CurrentSolution.Projects.Single().Documents.Single();
            var diagnostics = await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(ideEngineWorkspace, ideEngineDocument, new TextSpan(0, ideEngineDocument.GetTextAsync().Result.Length));
            var diagnosticsFromAnalyzer = diagnostics.Where(d => d.Id == CodeBlockAnalyzerFactory.Descriptor.Id);
            Assert.Equal(2, diagnosticsFromAnalyzer.Count());
        }

        source = """
            using System;
            class C
            {
                void F(int x = 0, int y = 1, int z = 2)
                {
                    Console.WriteLine(0);
                }
            }
            """;

        using (var compilerEngineWorkspace = EditorTestWorkspace.CreateCSharp(source, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService))
        {
            var compilerEngineCompilation = (CSharpCompilation)compilerEngineWorkspace.CurrentSolution.Projects.Single().GetRequiredCompilationAsync(CancellationToken.None).Result;
            var diagnostics = compilerEngineCompilation.GetAnalyzerDiagnostics(new[] { analyzer });
            var diagnosticsFromAnalyzer = diagnostics.Where(d => d.Id == CodeBlockAnalyzerFactory.Descriptor.Id);
            Assert.Equal(4, diagnosticsFromAnalyzer.Count());
        }
    }

    private class CodeBlockAnalyzerFactory : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor Descriptor = DescriptorFactory.CreateSimpleDescriptor("DummyDiagnostic");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Descriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
            => context.RegisterCodeBlockStartAction<SyntaxKind>(CreateAnalyzerWithinCodeBlock);

        public void CreateAnalyzerWithinCodeBlock(CodeBlockStartAnalysisContext<SyntaxKind> context)
        {
            var blockAnalyzer = new CodeBlockAnalyzer();
            context.RegisterCodeBlockEndAction(CodeBlockAnalyzer.AnalyzeCodeBlock);
            context.RegisterSyntaxNodeAction(CodeBlockAnalyzer.AnalyzeNode, CodeBlockAnalyzer.SyntaxKindsOfInterest.ToArray());
        }

        private class CodeBlockAnalyzer
        {
            public static ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
            {
                get
                {
                    return ImmutableArray.Create(SyntaxKind.MethodDeclaration, SyntaxKind.ExpressionStatement, SyntaxKind.EqualsValueClause);
                }
            }

            public static void AnalyzeCodeBlock(CodeBlockAnalysisContext _)
            {
            }

            public static void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                // Ensure only executable nodes are analyzed.
                Assert.NotEqual(SyntaxKind.MethodDeclaration, context.Node.Kind());
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }
    }

    [Fact]
    public async Task TestDiagnosticSpan()
    {
        var source = @"// empty code";

        var analyzer = new InvalidSpanAnalyzer();
        using var compilerEngineWorkspace = EditorTestWorkspace.CreateCSharp(source);
        var compilerEngineCompilation = (CSharpCompilation)(await compilerEngineWorkspace.CurrentSolution.Projects.Single().GetRequiredCompilationAsync(CancellationToken.None));

        var diagnostics = compilerEngineCompilation.GetAnalyzerDiagnostics(new[] { analyzer });
        AssertEx.Any(diagnostics, d => d.Id == DocumentAnalysisExecutor.AnalyzerExceptionDiagnosticId);
    }

    private class InvalidSpanAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor Descriptor = DescriptorFactory.CreateSimpleDescriptor("DummyDiagnostic");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(Analyze);

        private void Analyze(SyntaxTreeAnalysisContext context)
            => context.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.Create(context.Tree, TextSpan.FromBounds(1000, 2000))));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18818")]
    public async Task TestNuGetAndVsixAnalyzer_ReportsSameId()
    {
        // NuGet and VSIX analyzer reporting same diagnostic IDs.
        var reportedDiagnosticIds = new[] { "A", "B", "C" };
        var nugetAnalyzer = new NuGetAnalyzer(reportedDiagnosticIds);
        var vsixAnalyzer = new VsixAnalyzer(reportedDiagnosticIds);
        Assert.Equal(reportedDiagnosticIds, nugetAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());
        Assert.Equal(reportedDiagnosticIds, vsixAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());

        // No NuGet or VSIX analyzer - no diagnostics.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer: null,
            expectedNugetAnalyzerExecuted: false,
            vsixAnalyzer: null,
            expectedVsixAnalyzerExecuted: false);

        // Only NuGet analyzer - verify diagnostics.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer,
            expectedNugetAnalyzerExecuted: true,
            vsixAnalyzer: null,
            expectedVsixAnalyzerExecuted: false,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
            });

        // Only VSIX analyzer - verify diagnostics.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer: null,
            expectedNugetAnalyzerExecuted: false,
            vsixAnalyzer,
            expectedVsixAnalyzerExecuted: true,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(VsixAnalyzer)),
            });

        // Both NuGet and VSIX analyzer, verify the following:
        //   1) No duplicate diagnostics
        //   2) Only NuGet analyzer executes
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer,
            expectedNugetAnalyzerExecuted: true,
            vsixAnalyzer,
            expectedVsixAnalyzerExecuted: false,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
            });
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18818")]
    public async Task TestNuGetAndVsixAnalyzer_NuGetAnalyzerReportsSubsetOfVsixAnalyzerIds()
    {
        // NuGet analyzer reports subset of diagnostic IDs reported by VSIX analyzer.
        var nugetAnalyzerDiagnosticIds = new[] { "B" };
        var vsixAnalyzerDiagnosticIds = new[] { "A", "B", "C" };
        var nugetAnalyzer = new NuGetAnalyzer(nugetAnalyzerDiagnosticIds);
        var vsixAnalyzer = new VsixAnalyzer(vsixAnalyzerDiagnosticIds);
        Assert.Equal(nugetAnalyzerDiagnosticIds, nugetAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());
        Assert.Equal(vsixAnalyzerDiagnosticIds, vsixAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());

        // Only NuGet analyzer - verify diagnostics.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer,
            expectedNugetAnalyzerExecuted: true,
            vsixAnalyzer: null,
            expectedVsixAnalyzerExecuted: false,
            new[]
            {
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer))
            });

        // Only VSIX analyzer - verify diagnostics.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer: null,
            expectedNugetAnalyzerExecuted: false,
            vsixAnalyzer,
            expectedVsixAnalyzerExecuted: true,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(VsixAnalyzer)),
            });

        // Both NuGet and VSIX analyzer, verify the following:
        //   1) No duplicate diagnostics
        //   2) Both NuGet and Vsix analyzers execute
        //   3) Appropriate diagnostic filtering is done - NuGet analyzer reported diagnostic IDs are filtered from Vsix analyzer execution.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer,
            expectedNugetAnalyzerExecuted: true,
            vsixAnalyzer,
            expectedVsixAnalyzerExecuted: true,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(VsixAnalyzer)),
            });
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18818")]
    public async Task TestNuGetAndVsixAnalyzer_VsixAnalyzerReportsSubsetOfNuGetAnalyzerIds()
    {
        // VSIX analyzer reports subset of diagnostic IDs reported by NuGet analyzer.
        var nugetAnalyzerDiagnosticIds = new[] { "A", "B", "C" };
        var vsixAnalyzerDiagnosticIds = new[] { "B" };
        var nugetAnalyzer = new NuGetAnalyzer(nugetAnalyzerDiagnosticIds);
        var vsixAnalyzer = new VsixAnalyzer(vsixAnalyzerDiagnosticIds);
        Assert.Equal(nugetAnalyzerDiagnosticIds, nugetAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());
        Assert.Equal(vsixAnalyzerDiagnosticIds, vsixAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());

        // Only NuGet analyzer - verify diagnostics.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer,
            expectedNugetAnalyzerExecuted: true,
            vsixAnalyzer: null,
            expectedVsixAnalyzerExecuted: false,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer))
            });

        // Only VSIX analyzer - verify diagnostics.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer: null,
            expectedNugetAnalyzerExecuted: false,
            vsixAnalyzer,
            expectedVsixAnalyzerExecuted: true,
            new[]
            {
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(VsixAnalyzer))
            });

        // Both NuGet and VSIX analyzer, verify the following:
        //   1) No duplicate diagnostics
        //   2) Only NuGet analyzer executes
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer,
            expectedNugetAnalyzerExecuted: true,
            vsixAnalyzer,
            expectedVsixAnalyzerExecuted: false,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
            });
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18818")]
    public async Task TestNuGetAndVsixAnalyzer_MultipleNuGetAnalyzersCollectivelyReportSameIds()
    {
        // Multiple NuGet analyzers collectively report same diagnostic IDs reported by Vsix analyzer.
        var firstNugetAnalyzerDiagnosticIds = new[] { "B" };
        var secondNugetAnalyzerDiagnosticIds = new[] { "A", "C" };
        var vsixAnalyzerDiagnosticIds = new[] { "A", "B", "C" };
        var firstNugetAnalyzer = new NuGetAnalyzer(firstNugetAnalyzerDiagnosticIds);
        var secondNugetAnalyzer = new NuGetAnalyzer(secondNugetAnalyzerDiagnosticIds);
        var vsixAnalyzer = new VsixAnalyzer(vsixAnalyzerDiagnosticIds);
        Assert.Equal(firstNugetAnalyzerDiagnosticIds, firstNugetAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());
        Assert.Equal(secondNugetAnalyzerDiagnosticIds, secondNugetAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());
        Assert.Equal(vsixAnalyzerDiagnosticIds, vsixAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());

        // All NuGet analyzers and no Vsix analyzer, verify the following:
        //   1) No duplicate diagnostics
        //   2) Only NuGet analyzers execute
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzers: ImmutableArray.Create(firstNugetAnalyzer, secondNugetAnalyzer),
            expectedNugetAnalyzersExecuted: true,
            vsixAnalyzers: ImmutableArray<VsixAnalyzer>.Empty,
            expectedVsixAnalyzersExecuted: false,
            nugetSuppressors: ImmutableArray<NuGetSuppressor>.Empty,
            expectedNugetSuppressorsExecuted: false,
            vsixSuppressors: ImmutableArray<VsixSuppressor>.Empty,
            expectedVsixSuppressorsExecuted: false,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer))
            });

        // All NuGet analyzers and Vsix analyzer, verify the following:
        //   1) No duplicate diagnostics
        //   2) Only NuGet analyzers execute
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzers: ImmutableArray.Create(firstNugetAnalyzer, secondNugetAnalyzer),
            expectedNugetAnalyzersExecuted: true,
            vsixAnalyzers: ImmutableArray.Create(vsixAnalyzer),
            expectedVsixAnalyzersExecuted: false,
            nugetSuppressors: ImmutableArray<NuGetSuppressor>.Empty,
            expectedNugetSuppressorsExecuted: false,
            vsixSuppressors: ImmutableArray<VsixSuppressor>.Empty,
            expectedVsixSuppressorsExecuted: false,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer))
            });

        // Subset of NuGet analyzers and Vsix analyzer, verify the following:
        //   1) No duplicate diagnostics
        //   2) Both NuGet and Vsix analyzers execute
        //   3) Appropriate diagnostic filtering is done - NuGet analyzer reported diagnostic IDs are filtered from Vsix analyzer execution.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzers: ImmutableArray.Create(firstNugetAnalyzer),
            expectedNugetAnalyzersExecuted: true,
            vsixAnalyzers: ImmutableArray.Create(vsixAnalyzer),
            expectedVsixAnalyzersExecuted: true,
            nugetSuppressors: ImmutableArray<NuGetSuppressor>.Empty,
            expectedNugetSuppressorsExecuted: false,
            vsixSuppressors: ImmutableArray<VsixSuppressor>.Empty,
            expectedVsixSuppressorsExecuted: false,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(VsixAnalyzer))
            });
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46942")]
    public async Task TestNuGetAndVsixAnalyzer_SuppressorSuppressesVsixAnalyzer()
    {
        // Multiple NuGet analyzers do not overlap with the VSIX analyzer or suppressor
        var firstNugetAnalyzerDiagnosticIds = new[] { "A" };
        var secondNugetAnalyzerDiagnosticIds = new[] { "B", "C" };
        var vsixAnalyzerDiagnosticIds = new[] { "X", "Y", "Z" };
        var firstNugetAnalyzer = new NuGetAnalyzer(firstNugetAnalyzerDiagnosticIds);
        var secondNugetAnalyzer = new NuGetAnalyzer(secondNugetAnalyzerDiagnosticIds);
        var vsixAnalyzer = new VsixAnalyzer(vsixAnalyzerDiagnosticIds);
        var vsixSuppressor = new VsixSuppressor(vsixAnalyzerDiagnosticIds);
        var nugetSuppressor = new NuGetSuppressor(vsixAnalyzerDiagnosticIds);
        var partialNugetSuppressor = new NuGetSuppressor(["Y", "Z"]);

        Assert.Equal(firstNugetAnalyzerDiagnosticIds, firstNugetAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());
        Assert.Equal(secondNugetAnalyzerDiagnosticIds, secondNugetAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());
        Assert.Equal(vsixAnalyzerDiagnosticIds, vsixAnalyzer.SupportedDiagnostics.Select(d => d.Id).Order());
        Assert.Equal(vsixAnalyzerDiagnosticIds, vsixSuppressor.SupportedSuppressions.Select(s => s.SuppressedDiagnosticId).Order());
        Assert.Equal(vsixAnalyzerDiagnosticIds, nugetSuppressor.SupportedSuppressions.Select(s => s.SuppressedDiagnosticId).Order());

        // Verify the following:
        //   1) No duplicate diagnostics
        //   2) The VSIX diagnostics are suppressed by the VSIX suppressor
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzers: ImmutableArray<NuGetAnalyzer>.Empty,
            expectedNugetAnalyzersExecuted: false,
            vsixAnalyzers: ImmutableArray.Create(vsixAnalyzer),
            expectedVsixAnalyzersExecuted: true,
            nugetSuppressors: ImmutableArray<NuGetSuppressor>.Empty,
            expectedNugetSuppressorsExecuted: false,
            vsixSuppressors: ImmutableArray.Create(vsixSuppressor),
            expectedVsixSuppressorsExecuted: true,
            new[]
            {
                (Diagnostic("X", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("Y", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("Z", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer))
            });

        // All without overlap, the VSIX analyzer and suppressor still work when nuget analyzers are present:
        //   1) No duplicate diagnostics
        //   2) All analyzers execute
        //   3) VSIX diagnostics are suppressed.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzers: ImmutableArray.Create(firstNugetAnalyzer, secondNugetAnalyzer),
            expectedNugetAnalyzersExecuted: true,
            vsixAnalyzers: ImmutableArray.Create(vsixAnalyzer),
            expectedVsixAnalyzersExecuted: true,
            nugetSuppressors: ImmutableArray<NuGetSuppressor>.Empty,
            expectedNugetSuppressorsExecuted: false,
            vsixSuppressors: ImmutableArray.Create(vsixSuppressor),
            expectedVsixSuppressorsExecuted: true,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("B", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("C", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("X", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("Y", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("Z", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer))
            });

        // All without overlap, verify the following:
        //   1) No duplicate diagnostics
        //   2) Both NuGet and Vsix analyzers execute
        //   3) Appropriate diagnostic filtering is done - Nuget suppressor suppresses VSIX analyzer.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzers: ImmutableArray.Create(firstNugetAnalyzer),
            expectedNugetAnalyzersExecuted: true,
            vsixAnalyzers: ImmutableArray.Create(vsixAnalyzer),
            expectedVsixAnalyzersExecuted: true,
            nugetSuppressors: ImmutableArray.Create(nugetSuppressor),
            expectedNugetSuppressorsExecuted: true,
            vsixSuppressors: ImmutableArray<VsixSuppressor>.Empty,
            expectedVsixSuppressorsExecuted: false,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("X", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("Y", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("Z", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer))
            });

        // Suppressors with duplicate support for VsixAnalyzer, but not 100% overlap. Verify the following:
        //   1) No duplicate diagnostics
        //   2) Both NuGet and Vsix analyzers execute
        //   3) Only Nuget suppressor executes
        //   4) Appropriate diagnostic filtering is done - Nuget suppressor suppresses VSIX analyzer.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzers: ImmutableArray.Create(firstNugetAnalyzer),
            expectedNugetAnalyzersExecuted: true,
            vsixAnalyzers: ImmutableArray.Create(vsixAnalyzer),
            expectedVsixAnalyzersExecuted: true,
            nugetSuppressors: ImmutableArray.Create(partialNugetSuppressor),
            expectedNugetSuppressorsExecuted: true,
            vsixSuppressors: ImmutableArray.Create(vsixSuppressor),
            expectedVsixSuppressorsExecuted: false,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("X", "Class", isSuppressed: false).WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("Y", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("Z", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer))
            });

        // Suppressors with duplicate support for VsixAnalyzer, with 100% overlap. Verify the following:
        //   1) No duplicate diagnostics
        //   2) Both NuGet and Vsix analyzers execute
        //   3) Only Nuget suppressor executes
        //   4) Appropriate diagnostic filtering is done - Nuget suppressor suppresses VSIX analyzer.
        await TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzers: ImmutableArray.Create(firstNugetAnalyzer),
            expectedNugetAnalyzersExecuted: true,
            vsixAnalyzers: ImmutableArray.Create(vsixAnalyzer),
            expectedVsixAnalyzersExecuted: true,
            nugetSuppressors: ImmutableArray.Create(nugetSuppressor),
            expectedNugetSuppressorsExecuted: true,
            vsixSuppressors: ImmutableArray.Create(vsixSuppressor),
            expectedVsixSuppressorsExecuted: false,
            new[]
            {
                (Diagnostic("A", "Class").WithLocation(1, 7), nameof(NuGetAnalyzer)),
                (Diagnostic("X", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("Y", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer)),
                (Diagnostic("Z", "Class", isSuppressed: true).WithLocation(1, 7), nameof(VsixAnalyzer))
            });
    }

    private static Task TestNuGetAndVsixAnalyzerCoreAsync(
        NuGetAnalyzer? nugetAnalyzer,
        bool expectedNugetAnalyzerExecuted,
        VsixAnalyzer? vsixAnalyzer,
        bool expectedVsixAnalyzerExecuted,
        params (DiagnosticDescription diagnostic, string message)[] expectedDiagnostics)
        => TestNuGetAndVsixAnalyzerCoreAsync(
            nugetAnalyzer != null ? ImmutableArray.Create(nugetAnalyzer) : ImmutableArray<NuGetAnalyzer>.Empty,
            expectedNugetAnalyzerExecuted,
            vsixAnalyzer != null ? ImmutableArray.Create(vsixAnalyzer) : ImmutableArray<VsixAnalyzer>.Empty,
            expectedVsixAnalyzerExecuted,
            ImmutableArray<NuGetSuppressor>.Empty,
            false,
            ImmutableArray<VsixSuppressor>.Empty,
            false,
            expectedDiagnostics);

    private static async Task TestNuGetAndVsixAnalyzerCoreAsync(
        ImmutableArray<NuGetAnalyzer> nugetAnalyzers,
        bool expectedNugetAnalyzersExecuted,
        ImmutableArray<VsixAnalyzer> vsixAnalyzers,
        bool expectedVsixAnalyzersExecuted,
        ImmutableArray<NuGetSuppressor> nugetSuppressors,
        bool expectedNugetSuppressorsExecuted,
        ImmutableArray<VsixSuppressor> vsixSuppressors,
        bool expectedVsixSuppressorsExecuted,
        params (DiagnosticDescription diagnostic, string message)[] expectedDiagnostics)
    {
        // First clear out the analyzer state for all analyzers.
        foreach (var nugetAnalyzer in nugetAnalyzers)
        {
            nugetAnalyzer.SymbolActionInvoked = false;
        }

        foreach (var vsixAnalyzer in vsixAnalyzers)
        {
            vsixAnalyzer.SymbolActionInvoked = false;
        }

        foreach (var nugetSuppressor in nugetSuppressors)
        {
            nugetSuppressor.SuppressorInvoked = false;
        }

        foreach (var vsixSuppressor in vsixSuppressors)
        {
            vsixSuppressor.SuppressorInvoked = false;
        }

        using var workspace = EditorTestWorkspace.CreateCSharp("class Class { }", TestOptions.Regular, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService);
        var vsixAnalyzerReferences = new List<DiagnosticAnalyzer>(vsixAnalyzers.CastArray<DiagnosticAnalyzer>());
        vsixAnalyzerReferences.AddRange(vsixSuppressors.CastArray<DiagnosticAnalyzer>());

        Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[]
        {
            new AnalyzerImageReference(vsixAnalyzerReferences.ToImmutableArray())
        })));

        var project = workspace.CurrentSolution.Projects.Single();

        var nugetAnalyzerReferences = new List<DiagnosticAnalyzer>();
        if (!nugetAnalyzers.IsEmpty)
        {
            nugetAnalyzerReferences.AddRange(nugetAnalyzers.As<DiagnosticAnalyzer>());
        }

        if (!nugetSuppressors.IsEmpty)
        {
            nugetAnalyzerReferences.AddRange(nugetSuppressors.As<DiagnosticAnalyzer>());
        }

        if (nugetAnalyzerReferences.Count > 0)
        {
            project = project.WithAnalyzerReferences(new[] { new AnalyzerImageReference(nugetAnalyzerReferences.ToImmutableArray()) });
        }

        var document = project.Documents.Single();
        var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);

        var diagnostics = (await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, document, root.FullSpan, includeSuppressedDiagnostics: true))
            .OrderBy(d => d.Id).ToImmutableArray();

        diagnostics.Verify(expectedDiagnostics.Select(d => d.diagnostic).ToArray());

        var index = 0;
        foreach (var (d, expectedMessage) in expectedDiagnostics)
        {
            Assert.Equal(expectedMessage, diagnostics[index].GetMessage());
            Assert.Equal(d.IsSuppressed, diagnostics[index].IsSuppressed);
            index++;
        }

        foreach (var nugetAnalyzer in nugetAnalyzers)
        {
            Assert.Equal(expectedNugetAnalyzersExecuted, nugetAnalyzer.SymbolActionInvoked);
        }

        foreach (var vsixAnalyzer in vsixAnalyzers)
        {
            Assert.Equal(expectedVsixAnalyzersExecuted, vsixAnalyzer.SymbolActionInvoked);
        }

        foreach (var nugetSuppressor in nugetSuppressors)
        {
            Assert.Equal(expectedNugetSuppressorsExecuted, nugetSuppressor.SuppressorInvoked);
        }

        foreach (var vsixSuppressor in vsixSuppressors)
        {
            Assert.Equal(expectedVsixSuppressorsExecuted, vsixSuppressor.SuppressorInvoked);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    private sealed class NuGetAnalyzer : AbstractNuGetOrVsixAnalyzer
    {
        public NuGetAnalyzer(string[] reportedIds)
            : base(nameof(NuGetAnalyzer), reportedIds)
        {
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    private sealed class VsixAnalyzer : AbstractNuGetOrVsixAnalyzer
    {
        public VsixAnalyzer(string[] reportedIds)
            : base(nameof(VsixAnalyzer), reportedIds)
        {
        }
    }

    private abstract class AbstractNuGetOrVsixAnalyzer : DiagnosticAnalyzer
    {
        protected AbstractNuGetOrVsixAnalyzer(string analyzerName, params string[] reportedIds)
            => SupportedDiagnostics = CreateSupportedDiagnostics(analyzerName, reportedIds);

        private static ImmutableArray<DiagnosticDescriptor> CreateSupportedDiagnostics(string analyzerName, string[] reportedIds)
        {
            var builder = ArrayBuilder<DiagnosticDescriptor>.GetInstance(reportedIds.Length);
            foreach (var id in reportedIds)
            {
                var descriptor = new DiagnosticDescriptor(id, "Title", messageFormat: analyzerName, "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);
                builder.Add(descriptor);
            }

            return builder.ToImmutableAndFree();
        }

        public bool SymbolActionInvoked { get; set; }
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        public sealed override void Initialize(AnalysisContext context)
            => context.RegisterSymbolAction(OnSymbol, SymbolKind.NamedType);

        private void OnSymbol(SymbolAnalysisContext context)
        {
            SymbolActionInvoked = true;
            foreach (var descriptor in SupportedDiagnostics)
            {
                var diagnostic = Diagnostic.Create(descriptor, context.Symbol.Locations[0]);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private sealed class NuGetSuppressor : AbstractNugetOrVsixSuppressor
    {
        public NuGetSuppressor(string[] reportIds)
            : base(nameof(NuGetSuppressor), reportIds)
        {
        }
    }

    private sealed class VsixSuppressor : AbstractNugetOrVsixSuppressor
    {
        public VsixSuppressor(string[] reportIds)
            : base(nameof(VsixSuppressor), reportIds)
        {
        }
    }

    private abstract class AbstractNugetOrVsixSuppressor : DiagnosticSuppressor
    {
        private readonly Dictionary<string, SuppressionDescriptor> mapping = [];

        protected AbstractNugetOrVsixSuppressor(string analyzerName, params string[] reportedIds)
            => SupportedSuppressions = CreateSupportedSuppressions(analyzerName, this.mapping, reportedIds);

        private static ImmutableArray<SuppressionDescriptor> CreateSupportedSuppressions(
            string analyzerName,
            Dictionary<string, SuppressionDescriptor> mapping,
            string[] reportedIds)
        {
            var builder = ArrayBuilder<SuppressionDescriptor>.GetInstance(reportedIds.Length);
            foreach (var id in reportedIds)
            {
                var descriptor = new SuppressionDescriptor("SPR" + id, id, justification: analyzerName);
                mapping.Add(descriptor.SuppressedDiagnosticId, descriptor);
                builder.Add(descriptor);
            }

            return builder.ToImmutableAndFree();
        }

        public bool SuppressorInvoked { get; set; }

        public sealed override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; }

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            SuppressorInvoked = true;

            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                if (this.mapping.TryGetValue(diagnostic.Id, out var descriptor))
                {
                    context.ReportSuppression(
                        Microsoft.CodeAnalysis.Diagnostics.Suppression.Create(descriptor, diagnostic));
                }
            }
        }
    }
}
