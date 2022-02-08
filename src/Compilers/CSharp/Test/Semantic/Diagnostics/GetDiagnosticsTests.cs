// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class GetDiagnosticsTests : CSharpTestBase
    {
        [Fact]
        public void DiagnosticsFilteredInMethodBody()
        {
            var source = @"
class C
{
    void M()
    {
        @
        #
        !
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            DiagnosticsHelper.VerifyDiagnostics(model, source, @"(?s)^.*$", "CS1646", "CS1024", "CS1525", "CS1002");
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"\s*(?=@)", "CS1646");
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"#", "CS1024");
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"(?<=\!)", "CS1525", "CS1002");
        }

        [Fact]
        public void DiagnosticsFilteredInMethodBodyInsideNamespace()
        {
            var source = @"
namespace N
{
    class C
    {
        void S()
        {
            var x = X;
        }
    }
}

class D
{
    int P
    {
        get
        {
            return Y;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            DiagnosticsHelper.VerifyDiagnostics(model, source, @"var x = X;", "CS0103");
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"return Y;", "CS0103");
        }

        [Fact]
        public void DiagnosticsFilteredForIntersectingIntervals()
        {
            var source = @"
class C : Abracadabra
{
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            const string ErrorId = "CS0246";
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"(?s)^.*$", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"Abracadabra", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"C : Abracadabra", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"C : Abracadabr", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"Abracadabra[\r\n]+", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"bracadabra[\r\n]+", ErrorId);
        }

        [Fact, WorkItem(1066483, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1066483")]
        public void TestDiagnosticWithSeverity()
        {
            var source = @"
class C
{
    public void Goo()
    {
        int x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var diag = compilation.GetDiagnostics().Single();

            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal(3, diag.WarningLevel);

            var error = diag.WithSeverity(DiagnosticSeverity.Error);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Equal(DiagnosticSeverity.Warning, error.DefaultSeverity);
            Assert.Equal(0, error.WarningLevel);

            var warning = error.WithSeverity(DiagnosticSeverity.Warning);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
            Assert.Equal(DiagnosticSeverity.Warning, warning.DefaultSeverity);
            Assert.Equal(3, warning.WarningLevel);

            var hidden = diag.WithSeverity(DiagnosticSeverity.Hidden);
            Assert.Equal(DiagnosticSeverity.Hidden, hidden.Severity);
            Assert.Equal(DiagnosticSeverity.Warning, hidden.DefaultSeverity);
            Assert.Equal(1, hidden.WarningLevel);

            var info = diag.WithSeverity(DiagnosticSeverity.Info);
            Assert.Equal(DiagnosticSeverity.Info, info.Severity);
            Assert.Equal(DiagnosticSeverity.Warning, info.DefaultSeverity);
            Assert.Equal(1, info.WarningLevel);
        }

        [Fact, WorkItem(7446, "https://github.com/dotnet/roslyn/issues/7446")]
        public void TestCompilationEventQueueWithSemanticModelGetDiagnostics()
        {
            var source1 = @"
namespace N1
{
    partial class Class
    {
        private void NonPartialMethod1() { }
    }
} 
";
            var source2 = @"
namespace N1
{
    partial class Class
    {
        private void NonPartialMethod2() { }
    }
} 
";

            var tree1 = CSharpSyntaxTree.ParseText(source1, path: "file1");
            var tree2 = CSharpSyntaxTree.ParseText(source2, path: "file2");
            var eventQueue = new AsyncQueue<CompilationEvent>();
            var compilation = CreateCompilationWithMscorlib45(new[] { tree1, tree2 }).WithEventQueue(eventQueue);

            // Invoke SemanticModel.GetDiagnostics to force populate the event queue for symbols in the first source file.
            var model = compilation.GetSemanticModel(tree1);
            model.GetDiagnostics(tree1.GetRoot().FullSpan);

            Assert.True(eventQueue.Count > 0);
            bool compilationStartedFired;
            HashSet<string> declaredSymbolNames, completedCompilationUnits;
            Assert.True(DequeueCompilationEvents(eventQueue, out compilationStartedFired, out declaredSymbolNames, out completedCompilationUnits));

            // Verify symbol declared events fired for all symbols declared in the first source file.
            Assert.True(compilationStartedFired);
            Assert.True(declaredSymbolNames.Contains(compilation.GlobalNamespace.Name));
            Assert.True(declaredSymbolNames.Contains("N1"));
            Assert.True(declaredSymbolNames.Contains("Class"));
            Assert.True(declaredSymbolNames.Contains("NonPartialMethod1"));
            Assert.True(completedCompilationUnits.Contains(tree1.FilePath));
        }

        [Fact, WorkItem(7477, "https://github.com/dotnet/roslyn/issues/7477")]
        public void TestCompilationEventsForPartialMethod()
        {
            var source1 = @"
namespace N1
{
    partial class Class
    {
        private void NonPartialMethod1() { }
        partial void ImpartialMethod1();
        partial void ImpartialMethod2() { }
        partial void PartialMethod();
    }
} 
";
            var source2 = @"
namespace N1
{
    partial class Class
    {
        private void NonPartialMethod2() { }
        partial void PartialMethod() { }
    }
} 
";

            var tree1 = CSharpSyntaxTree.ParseText(source1, path: "file1");
            var tree2 = CSharpSyntaxTree.ParseText(source2, path: "file2");
            var eventQueue = new AsyncQueue<CompilationEvent>();
            var compilation = CreateCompilationWithMscorlib45(new[] { tree1, tree2 }).WithEventQueue(eventQueue);

            // Invoke SemanticModel.GetDiagnostics to force populate the event queue for symbols in the first source file.
            var model = compilation.GetSemanticModel(tree1);
            model.GetDiagnostics(tree1.GetRoot().FullSpan);

            Assert.True(eventQueue.Count > 0);
            bool compilationStartedFired;
            HashSet<string> declaredSymbolNames, completedCompilationUnits;
            Assert.True(DequeueCompilationEvents(eventQueue, out compilationStartedFired, out declaredSymbolNames, out completedCompilationUnits));

            // Verify symbol declared events fired for all symbols declared in the first source file.
            Assert.True(compilationStartedFired);
            Assert.True(declaredSymbolNames.Contains(compilation.GlobalNamespace.Name));
            Assert.True(declaredSymbolNames.Contains("N1"));
            Assert.True(declaredSymbolNames.Contains("Class"));
            Assert.True(declaredSymbolNames.Contains("NonPartialMethod1"));
            Assert.True(declaredSymbolNames.Contains("ImpartialMethod1"));
            Assert.True(declaredSymbolNames.Contains("ImpartialMethod2"));
            Assert.True(declaredSymbolNames.Contains("PartialMethod"));
            Assert.True(completedCompilationUnits.Contains(tree1.FilePath));
        }

        [Fact, WorkItem(8178, "https://github.com/dotnet/roslyn/issues/8178")]
        public void TestEarlyCancellation()
        {
            var source = @"
namespace N1
{
    partial class Class
    {
        private void NonPartialMethod1() { }
        partial void PartialMethod();
    }
} 
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "file1");
            var eventQueue = new AsyncQueue<CompilationEvent>();
            var compilation = CreateCompilationWithMscorlib45(new[] { tree }).WithEventQueue(eventQueue);
            eventQueue.TryComplete(); // complete the queue before the compiler is finished with it
            var model = compilation.GetSemanticModel(tree);
            model.GetDiagnostics(tree.GetRoot().FullSpan);
        }

        private static bool DequeueCompilationEvents(AsyncQueue<CompilationEvent> eventQueue, out bool compilationStartedFired, out HashSet<string> declaredSymbolNames, out HashSet<string> completedCompilationUnits)
        {
            compilationStartedFired = false;
            declaredSymbolNames = new HashSet<string>();
            completedCompilationUnits = new HashSet<string>();
            if (eventQueue.Count == 0)
            {
                return false;
            }

            CompilationEvent compEvent;
            while (eventQueue.TryDequeue(out compEvent))
            {
                if (compEvent is CompilationStartedEvent)
                {
                    Assert.False(compilationStartedFired, "Unexpected multiple compilation stated events");
                    compilationStartedFired = true;
                }
                else
                {
                    var symbolDeclaredEvent = compEvent as SymbolDeclaredCompilationEvent;
                    if (symbolDeclaredEvent != null)
                    {
                        var symbol = symbolDeclaredEvent.Symbol;
                        var added = declaredSymbolNames.Add(symbol.Name);
                        if (!added)
                        {
                            var method = symbol.GetSymbol() as Symbols.MethodSymbol;
                            Assert.NotNull(method);

                            var isPartialMethod = method.PartialDefinitionPart != null ||
                                                  method.PartialImplementationPart != null;
                            Assert.True(isPartialMethod, "Unexpected multiple symbol declared events for symbol " + symbol);
                        }
                    }
                    else
                    {
                        var compilationCompletedEvent = compEvent as CompilationUnitCompletedEvent;
                        if (compilationCompletedEvent != null)
                        {
                            Assert.True(completedCompilationUnits.Add(compilationCompletedEvent.CompilationUnit.FilePath));
                        }
                    }
                }
            }

            return true;
        }

        [Fact]
        public void TestEventQueueCompletionForEmptyCompilation()
        {
            var compilation = CreateCompilationWithMscorlib45(CSharpTestSource.None).WithEventQueue(new AsyncQueue<CompilationEvent>());

            // Force complete compilation event queue
            var unused = compilation.GetDiagnostics();

            Assert.True(compilation.EventQueue.IsCompleted);
        }

        [Fact]
        public void CompilingCodeWithInvalidPreProcessorSymbolsShouldProvideDiagnostics()
        {
            var compilation = CreateEmptyCompilation(string.Empty, parseOptions: new CSharpParseOptions().WithPreprocessorSymbols(new[] { "1" }));

            compilation.VerifyDiagnostics(
                // (1,1): error CS8301: Invalid name for a preprocessing symbol; '1' is not a valid identifier
                // 
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol, "").WithArguments("1").WithLocation(1, 1));
        }

        [Fact]
        public void CompilingCodeWithInvalidSourceCodeKindShouldProvideDiagnostics()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var compilation = CreateCompilationWithMscorlib45(string.Empty, parseOptions: new CSharpParseOptions().WithKind(SourceCodeKind.Interactive));
#pragma warning restore CS0618 // Type or member is obsolete

            compilation.VerifyDiagnostics(
                // (1,1): error CS8190: Provided source code kind is unsupported or invalid: 'Interactive'
                // 
                Diagnostic(ErrorCode.ERR_BadSourceCodeKind, "").WithArguments("Interactive").WithLocation(1, 1));
        }

        [Fact]
        public void CompilingCodeWithInvalidLanguageVersionShouldProvideDiagnostics()
        {
            var compilation = CreateEmptyCompilation(string.Empty, parseOptions: new CSharpParseOptions().WithLanguageVersion((LanguageVersion)10000));
            compilation.VerifyDiagnostics(
                // (1,1): error CS8192: Provided language version is unsupported or invalid: '10000'.
                // 
                Diagnostic(ErrorCode.ERR_BadLanguageVersion, "").WithArguments("10000").WithLocation(1, 1));
        }

        [Fact]
        public void CompilingCodeWithInvalidDocumentationModeShouldProvideDiagnostics()
        {
            var compilation = CreateEmptyCompilation(string.Empty, parseOptions: new CSharpParseOptions().WithDocumentationMode(unchecked((DocumentationMode)100)));
            compilation.VerifyDiagnostics(
                // (1,1): error CS8191: Provided documentation mode is unsupported or invalid: '100'.
                // 
                Diagnostic(ErrorCode.ERR_BadDocumentationMode, "").WithArguments("100").WithLocation(1, 1));
        }

        [Fact]
        public void CompilingCodeWithInvalidParseOptionsInMultipleSyntaxTreesShouldReportThemAll()
        {
            var syntaxTree1 = Parse(string.Empty, options: new CSharpParseOptions().WithPreprocessorSymbols(new[] { "1" }));
            var syntaxTree2 = Parse(string.Empty, options: new CSharpParseOptions().WithPreprocessorSymbols(new[] { "2" }));
            var syntaxTree3 = Parse(string.Empty, options: new CSharpParseOptions().WithPreprocessorSymbols(new[] { "3" }));

            var compilation = CreateEmptyCompilation(new[] { syntaxTree1, syntaxTree2, syntaxTree3 });
            var diagnostics = compilation.GetDiagnostics();

            diagnostics.Verify(
                // (1,1): error CS8301: Invalid name for a preprocessing symbol; '1' is not a valid identifier
                // 
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol, "").WithArguments("1").WithLocation(1, 1),
                // (1,1): error CS8301: Invalid name for a preprocessing symbol; '2' is not a valid identifier
                // 
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol, "").WithArguments("2").WithLocation(1, 1),
                // (1,1): error CS8301: Invalid name for a preprocessing symbol; '3' is not a valid identifier
                // 
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol, "").WithArguments("3").WithLocation(1, 1));

            Assert.True(diagnostics[0].Location.SourceTree.Equals(syntaxTree1));
            Assert.True(diagnostics[1].Location.SourceTree.Equals(syntaxTree2));
            Assert.True(diagnostics[2].Location.SourceTree.Equals(syntaxTree3));
        }

        [Fact]
        public void CompilingCodeWithSameParseOptionsInMultipleSyntaxTreesShouldReportOnlyNonDuplicates()
        {
            var parseOptions1 = new CSharpParseOptions().WithPreprocessorSymbols(new[] { "1" });
            var parseOptions2 = new CSharpParseOptions().WithPreprocessorSymbols(new[] { "2" });

            var syntaxTree1 = Parse(string.Empty, options: parseOptions1);
            var syntaxTree2 = Parse(string.Empty, options: parseOptions2);
            var syntaxTree3 = Parse(string.Empty, options: parseOptions2);

            var compilation = CreateCompilation(new[] { syntaxTree1, syntaxTree2, syntaxTree3 });
            var diagnostics = compilation.GetDiagnostics();

            diagnostics.Verify(
                // (1,1): error CS8301: Invalid name for a preprocessing symbol; '1' is not a valid identifier
                // 
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol, "").WithArguments("1").WithLocation(1, 1),
                // (1,1): error CS8301: Invalid name for a preprocessing symbol; '2' is not a valid identifier
                // 
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol, "").WithArguments("2").WithLocation(1, 1));

            Assert.True(diagnostics[0].Location.SourceTree.Equals(syntaxTree1));
            Assert.True(diagnostics[1].Location.SourceTree.Equals(syntaxTree2));
        }

        [Fact]
        [WorkItem(24351, "https://github.com/dotnet/roslyn/issues/24351")]
        public void GettingDeclarationDiagnosticsForATreeShouldNotFreezeCompilation()
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
            var tree1 = Parse(string.Empty, options: parseOptions);
            var tree2 = Parse("ref struct X {}", options: parseOptions);

            var compilation = CreateCompilation(new[] { tree1, tree2 });

            // Verify diagnostics for the first tree. This should have sealed the attributes
            compilation.GetSemanticModel(tree1).GetDeclarationDiagnostics().Verify();

            // Verify diagnostics for the second tree. This should have triggered the assert
            compilation.GetSemanticModel(tree2).GetDeclarationDiagnostics().Verify();
        }

        [Fact]
        [WorkItem(39094, "https://github.com/dotnet/roslyn/issues/39094")]
        public void TestSuppressMessageAttributeDoesNotSuppressCompilerDiagnostics()
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("""", ""CS0168"", Justification = """", Scope = ""type"", Target = ""~T:C"")]

class C
{
    void M()
    {
        // warning CS0168:  The variable 'x' is declared but never used.
        int x;
    }
}
";
            // Verify unsuppressed CS0168 in 'Compilation.GetDiagnostics'
            var compilation = CreateCompilation(source);
            var diagnostics = compilation.GetDiagnostics();
            var expected = Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(11, 13);
            diagnostics.Verify(expected);
            Assert.False(diagnostics.Single().IsSuppressed);

            // Verify 'GetEffectiveDiagnostics' does not apply SuppressMessage suppression to compiler diagnostics.
            var effectiveDiagnostics = CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation);
            effectiveDiagnostics.Verify(expected);
            Assert.False(effectiveDiagnostics.Single().IsSuppressed);

            // Verify CS0168 is not suppressed for compiler diagnostics computed
            // using CompilerDiagnosticAnalyzer
            var analyzers = new DiagnosticAnalyzer[] { new CSharpCompilerDiagnosticAnalyzer() };
            var analyzerDiagnostics = compilation.GetAnalyzerDiagnostics(analyzers);
            analyzerDiagnostics.Verify(expected);
            Assert.False(analyzerDiagnostics.Single().IsSuppressed);
        }

        [Fact]
        [WorkItem(42116, "https://github.com/dotnet/roslyn/issues/42116")]
        public async Task TestAnalyzerConfigurationDoesNotAffectCompilerDiagnostics()
        {
            var source = @"
class C
{
    void M()
    {
        // warning CS0168:  The variable 'x' is declared but never used.
        int x;
    }
}
";
            // Verify CS0168 reported from 'Compilation.GetDiagnostics'
            var compilation = CreateCompilation(source);
            var compilerDiagnostics = compilation.GetDiagnostics();
            verifyDiagnostics(compilerDiagnostics);

            // Verify CS0168 reported from 'CSharpCompilerDiagnosticAnalyzer', i.e. the diagnostic analyzer used in the IDE layer to report live compiler diagnostics.
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer());
            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty));
            var analyzerDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            verifyDiagnostics(analyzerDiagnostics);

            // Verify CS0168 reported by CSharpCompilerDiagnosticAnalyzer is not affected by "dotnet_analyzer_diagnostic = none"
            var analyzerConfigOptions = new DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty.Add("dotnet_analyzer_diagnostic.severity", "none"));
            var analyzerConfigOptionsProvider = new CompilerAnalyzerConfigOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigOptions>.Empty.Add(compilation.SyntaxTrees.Single(), analyzerConfigOptions),
                DictionaryAnalyzerConfigOptions.Empty);
            var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, analyzerConfigOptionsProvider);
            compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, analyzerOptions);
            analyzerDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            verifyDiagnostics(analyzerDiagnostics);

            static void verifyDiagnostics(ImmutableArray<Diagnostic> diagnostics)
            {
                var expected = Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13);
                diagnostics.Verify(expected);

                var diagnostic = diagnostics.Single();
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                Assert.False(diagnostic.IsSuppressed);
            }
        }

        [Fact]
        [WorkItem(43305, "https://github.com/dotnet/roslyn/issues/43305")]
        public async Task TestAnalyzerConfigurationDoesNotAffectNonConfigurableDiagnostics()
        {
            var source = @"class C { }";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            // Verify 'NonConfigurable' analyzer diagnostic without any analyzer config options.
            var analyzer = new NamedTypeAnalyzer(NamedTypeAnalyzer.AnalysisKind.Symbol, configurable: false);
            await verifyDiagnosticsAsync(compilation, analyzer, options: null);

            // Verify 'NonConfigurable' analyzer diagnostic is not affected by category based configuration.
            await verifyDiagnosticsAsync(compilation, analyzer, options: ($"dotnet_analyzer_diagnostic.category-{NamedTypeAnalyzer.RuleCategory}.severity", "none"));

            // Verify 'NonConfigurable' analyzer diagnostic is not affected by all analyzers bulk configuration.
            await verifyDiagnosticsAsync(compilation, analyzer, options: ("dotnet_analyzer_diagnostic.severity", "none"));

            return;

            static async Task verifyDiagnosticsAsync(Compilation compilation, DiagnosticAnalyzer analyzer, (string key, string value)? options)
            {
                AnalyzerOptions analyzerOptions;
                if (options.HasValue)
                {
                    var analyzerConfigOptions = new DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty.Add(options.Value.key, options.Value.value));
                    var analyzerConfigOptionsProvider = new CompilerAnalyzerConfigOptionsProvider(
                        ImmutableDictionary<object, AnalyzerConfigOptions>.Empty.Add(compilation.SyntaxTrees.Single(), analyzerConfigOptions),
                        DictionaryAnalyzerConfigOptions.Empty);
                    analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, analyzerConfigOptionsProvider);
                }
                else
                {
                    analyzerOptions = null;
                }

                var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer), analyzerOptions);
                var analyzerDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
                var expected = Diagnostic(NamedTypeAnalyzer.RuleId, "C").WithArguments("C").WithLocation(1, 7);
                analyzerDiagnostics.Verify(expected);

                var diagnostic = analyzerDiagnostics.Single();
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                Assert.False(diagnostic.IsSuppressed);
            }
        }

        [Fact]
        public async Task TestConcurrentGetAnalyzerDiagnostics()
        {
            var source1 = @"
partial class C
{
    void M1()
    {
        // warning CS0168:  The variable 'x' is declared but never used.
        int x;
    }
}
";
            var source2 = @"
partial class C
{
    void M2()
    {
        // warning CS0168:  The variable 'x' is declared but never used.
        int x;
    }
}
";
            var source3 = @"
class C3
{
    void M2()
    {
        // warning CS0168:  The variable 'x' is declared but never used.
        int x;
    }
}
";
            var compilation = CreateCompilation(new[] { source1, source2, source3 });
            compilation = compilation.WithOptions(compilation.Options.WithConcurrentBuild(true));

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer());
            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers,
                new CompilationWithAnalyzersOptions(
                    new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                    onAnalyzerException: null,
                    concurrentAnalysis: true,
                    logAnalyzerExecutionTime: false));

            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree, true);
            var tasks = new Task[10];
            for (var i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(model, null, CancellationToken.None));
            }

            await Task.WhenAll(tasks);
        }

        [Theory, WorkItem(46874, "https://github.com/dotnet/roslyn/pull/46874")]
        [InlineData(2)]
        [InlineData(50)]
        public async Task TestConcurrentGetAnalyzerDiagnostics_SymbolStartAnalyzer(int partialDeclarationCount)
        {
            var sources = new string[partialDeclarationCount + 1];

            for (var i = 0; i < partialDeclarationCount; i++)
            {
                sources[i] = $@"
partial class C
{{
    void M{i}()
    {{
        // warning CS0168:  The variable 'x' is declared but never used.
        int x;
    }}
}}
";
            }

            sources[partialDeclarationCount] = @"
class C3
{
    void M2()
    {
        // warning CS0168:  The variable 'x' is declared but never used.
        int x;
    }
}
";
            var compilation = CreateCompilation(sources);
            compilation = compilation.WithOptions(compilation.Options.WithConcurrentBuild(true));

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new SymbolStartAnalyzer(topLevelAction: false, SymbolKind.NamedType, OperationKind.VariableDeclaration));
            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers,
                new CompilationWithAnalyzersOptions(
                    new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                    onAnalyzerException: null,
                    concurrentAnalysis: true,
                    logAnalyzerExecutionTime: false));

            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree, true);
            var tasks = new Task[10];
            for (var i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(model, null, CancellationToken.None));
            }

            await Task.WhenAll(tasks);
        }

        [Theory, CombinatorialData]
        [WorkItem(46950, "https://github.com/dotnet/roslyn/issues/46950")]
        public async Task TestGetAnalyzerSyntaxDiagnosticsWithCancellation(bool concurrent)
        {
            var source = @"class C { }";
            var compilation = CreateCompilation(source);
            compilation = compilation.WithOptions(compilation.Options.WithConcurrentBuild(concurrent));
            var tree = compilation.SyntaxTrees.First();

            var analyzer = new RegisterSyntaxTreeCancellationAnalyzer();
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers,
                new CompilationWithAnalyzersOptions(
                    new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                    onAnalyzerException: null,
                    concurrentAnalysis: concurrent,
                    logAnalyzerExecutionTime: false));

            // First call into analyzer mimics cancellation.
            await Assert.ThrowsAsync<OperationCanceledException>(() => compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(tree, analyzer.CancellationToken));

            // Second call into analyzer reports diagnostic.
            var diagnostics = await compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(tree, CancellationToken.None);
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(RegisterSyntaxTreeCancellationAnalyzer.DiagnosticId, diagnostic.Id);
        }

        [Fact]
        public async Task TestEventQueuePartialCompletionForSpanBasedQuery()
        {
            var source = @"
class C
{
    void M1()
    {
        int x1 = 0;
    }

    void M2()
    {
        int x2 = 0;
    }
}";
            var compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Get analyzer diagnostics for a span within "M1".
            var localDecl = syntaxTree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().First();
            var span = localDecl.Span;
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer());
            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, AnalyzerOptions.Empty);
            _ = await compilationWithAnalyzers.GetAnalysisResultAsync(semanticModel, span, CancellationToken.None);

            // Verify only required compilation events are generated in the event queue.
            // Event queue should not be completed as we are requesting diagnostics for a span within "M1"
            // and no compilation event should be generated for "M2".
            var eventQueue = compilationWithAnalyzers.Compilation.EventQueue;
            Assert.False(eventQueue.IsCompleted);

            // Now fetch diagnostics for entire tree and verify event queue is completed.
            _ = await compilationWithAnalyzers.GetAnalysisResultAsync(semanticModel, filterSpan: null, CancellationToken.None);
            Assert.True(eventQueue.IsCompleted);
        }

        [Fact, WorkItem(56843, "https://github.com/dotnet/roslyn/issues/56843")]
        public async Task TestCompilerAnalyzerForSpanBasedSemanticDiagnostics()
        {
            var source = @"
class C
{
    void M1()
    {
        int x1 = 0; // CS0219 (unused variable)
    }
}";
            var compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Get compiler analyzer diagnostics for a span within "M1".
            var localDecl = syntaxTree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().First();
            var span = localDecl.Span;
            var compilerAnalyzer = new CSharpCompilerDiagnosticAnalyzer();
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(compilerAnalyzer), AnalyzerOptions.Empty);
            var result = await compilationWithAnalyzers.GetAnalysisResultAsync(semanticModel, span, CancellationToken.None);
            var diagnostics = result.SemanticDiagnostics[syntaxTree][compilerAnalyzer];
            diagnostics.Verify(
                // (6,13): warning CS0219: The variable 'x1' is assigned but its value is never used
                //         int x1 = 0; // CS0219 (unused variable)
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x1").WithArguments("x1").WithLocation(6, 13));

            // Verify compiler analyzer diagnostics for entire tree
            result = await compilationWithAnalyzers.GetAnalysisResultAsync(semanticModel, filterSpan: null, CancellationToken.None);
            diagnostics = result.SemanticDiagnostics[syntaxTree][compilerAnalyzer];
            diagnostics.Verify(
                // (6,13): warning CS0219: The variable 'x1' is assigned but its value is never used
                //         int x1 = 0; // CS0219 (unused variable)
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x1").WithArguments("x1").WithLocation(6, 13));

            // Verify no diagnostics with a span outside the local decl
            span = localDecl.GetLastToken().GetNextToken().Span;
            result = await compilationWithAnalyzers.GetAnalysisResultAsync(semanticModel, span, CancellationToken.None);
            var diagnosticsByAnalyzerMap = result.SemanticDiagnostics[syntaxTree];
            Assert.Empty(diagnosticsByAnalyzerMap);
        }
    }
}
