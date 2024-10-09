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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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
            var compilation = CreateCompilationWithMscorlib461(source);
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
            var compilation = CreateCompilationWithMscorlib461(source);
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
            var compilation = CreateCompilationWithMscorlib461(source);
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
            var compilation = CreateCompilationWithMscorlib461(source);
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
            var compilation = CreateCompilationWithMscorlib461(new[] { tree1, tree2 }).WithEventQueue(eventQueue);

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
            var compilation = CreateCompilationWithMscorlib461(new[] { tree1, tree2 }).WithEventQueue(eventQueue);

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

        [Fact]
        public void TestCompilationEventsForPartialProperty()
        {
            var source1 = @"
namespace N1
{
    partial class Class
    {
        int NonPartialProp1 { get; set; }
        partial int DefOnlyPartialProp { get; set; }
        partial int ImplOnlyPartialProp { get => 1; set { } }
        partial int PartialProp { get; set; }
    }
} 
";
            var source2 = @"
namespace N1
{
    partial class Class
    {
        int NonPartialProp2 { get; set; }
        partial int PartialProp { get => 1; set { } }
    }
} 
";

            var tree1 = CSharpSyntaxTree.ParseText(source1, path: "file1");
            var tree2 = CSharpSyntaxTree.ParseText(source2, path: "file2");
            var eventQueue = new AsyncQueue<CompilationEvent>();
            var compilation = CreateCompilationWithMscorlib461(new[] { tree1, tree2 }).WithEventQueue(eventQueue);

            // Invoke SemanticModel.GetDiagnostics to force populate the event queue for symbols in the first source file.
            var model = compilation.GetSemanticModel(tree1);
            model.GetDiagnostics(tree1.GetRoot().FullSpan);

            Assert.True(eventQueue.Count > 0);
            bool compilationStartedFired;
            HashSet<string> declaredSymbolNames, completedCompilationUnits;
            Assert.True(DequeueCompilationEvents(eventQueue, out compilationStartedFired, out declaredSymbolNames, out completedCompilationUnits));

            // Verify symbol declared events fired for all symbols declared in the first source file.
            Assert.True(compilationStartedFired);

            // NB: NonPartialProp2 is missing here because we only asked for diagnostics in tree1
            AssertEx.Equal([
                "",
                "Class",
                "DefOnlyPartialProp",
                "get_ImplOnlyPartialProp",
                "get_NonPartialProp1",
                "get_PartialProp",
                "ImplOnlyPartialProp",
                "N1",
                "NonPartialProp1",
                "PartialProp",
                "set_ImplOnlyPartialProp",
                "set_NonPartialProp1",
                "set_PartialProp"
            ], declaredSymbolNames.OrderBy(name => name));

            AssertEx.Equal(["file1"], completedCompilationUnits.OrderBy(name => name));
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
            var compilation = CreateCompilationWithMscorlib461(new[] { tree }).WithEventQueue(eventQueue);
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
                            Assert.True(symbol.GetSymbol().IsPartialMember(), "Unexpected multiple symbol declared events for symbol " + symbol);
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
            var compilation = CreateCompilationWithMscorlib461(CSharpTestSource.None).WithEventQueue(new AsyncQueue<CompilationEvent>());

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
            var compilation = CreateCompilationWithMscorlib461(string.Empty, parseOptions: new CSharpParseOptions().WithKind(SourceCodeKind.Interactive));
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

        [Theory, CombinatorialData, WorkItem(67310, "https://github.com/dotnet/roslyn/issues/67310")]
        public async Task TestBlockStartAnalyzer(bool testCodeBlockStart)
        {
            var source = @"
using System;

class C
{
    private int _field;

    // Expression bodied members
    int P1 => 0;
    int P2 { get => 0; set => value = 0; }
    int this[int i] => 0;
    int this[char i] { get => 0; set => value = 0; }
    event EventHandler E1 { add => _ = 0; remove => _ = 0; }
    int M1() => 0;
    C() => _field = 0;
    ~C() => _field = 0;
    public static int operator +(C p) => 0;
}

class D
{
    private int _field;

    // Block bodied members
    int P3 { get { return 0; } set { value = 0; } }
    int this[char i] { get { return 0; } set { value = 0; } }
    event EventHandler E2 { add { _ = 0; } remove => _ = 0; }
    int M2() { return 0; }
    D() { _field = 0; }
    ~D() { _field = 0; }
    public static int operator -(D p) { return 0; }
}";
            var compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees[0];

            var analyzer = new BlockStartAnalyzer(testCodeBlockStart);
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer), AnalyzerOptions.Empty);
            var result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            var semanticDiagnostics = result.SemanticDiagnostics[syntaxTree][analyzer];
            var group1 = semanticDiagnostics.Where(d => d.Id == "ID0001");
            var group2 = semanticDiagnostics.Except(group1).ToImmutableArray();

            group1.Verify(
                Diagnostic("ID0001", "M1").WithArguments("M1").WithLocation(14, 9),
                Diagnostic("ID0001", "C").WithArguments(".ctor").WithLocation(15, 5),
                Diagnostic("ID0001", "C").WithArguments("Finalize").WithLocation(16, 6),
                Diagnostic("ID0001", "+").WithArguments("op_UnaryPlus").WithLocation(17, 32),
                Diagnostic("ID0001", "M2").WithArguments("M2").WithLocation(28, 9),
                Diagnostic("ID0001", "D").WithArguments(".ctor").WithLocation(29, 5),
                Diagnostic("ID0001", "D").WithArguments("Finalize").WithLocation(30, 6),
                Diagnostic("ID0001", "-").WithArguments("op_UnaryNegation").WithLocation(31, 32));

            Assert.Equal(22, group2.Length);
            if (testCodeBlockStart)
            {
                group2.Verify(
                    Diagnostic("ID0002", "=> 0").WithLocation(9, 12),
                    Diagnostic("ID0002", "get => 0;").WithLocation(10, 14),
                    Diagnostic("ID0002", "set => value = 0;").WithLocation(10, 24),
                    Diagnostic("ID0002", "=> 0").WithLocation(11, 21),
                    Diagnostic("ID0002", "get => 0;").WithLocation(12, 24),
                    Diagnostic("ID0002", "set => value = 0;").WithLocation(12, 34),
                    Diagnostic("ID0002", "add => _ = 0;").WithLocation(13, 29),
                    Diagnostic("ID0002", "remove => _ = 0;").WithLocation(13, 43),
                    Diagnostic("ID0002", "int M1() => 0;").WithLocation(14, 5),
                    Diagnostic("ID0002", "C() => _field = 0;").WithLocation(15, 5),
                    Diagnostic("ID0002", "~C() => _field = 0;").WithLocation(16, 5),
                    Diagnostic("ID0002", "public static int operator +(C p) => 0;").WithLocation(17, 5),
                    Diagnostic("ID0002", "get { return 0; }").WithLocation(25, 14),
                    Diagnostic("ID0002", "set { value = 0; }").WithLocation(25, 32),
                    Diagnostic("ID0002", "get { return 0; }").WithLocation(26, 24),
                    Diagnostic("ID0002", "set { value = 0; }").WithLocation(26, 42),
                    Diagnostic("ID0002", "add { _ = 0; }").WithLocation(27, 29),
                    Diagnostic("ID0002", "remove => _ = 0;").WithLocation(27, 44),
                    Diagnostic("ID0002", "int M2() { return 0; }").WithLocation(28, 5),
                    Diagnostic("ID0002", "D() { _field = 0; }").WithLocation(29, 5),
                    Diagnostic("ID0002", "~D() { _field = 0; }").WithLocation(30, 5),
                    Diagnostic("ID0002", "public static int operator -(D p) { return 0; }").WithLocation(31, 5));
            }
            else
            {
                group2.Verify(
                    Diagnostic("ID0002", "=> 0").WithLocation(9, 12),
                    Diagnostic("ID0002", "=> 0").WithLocation(10, 18),
                    Diagnostic("ID0002", "=> value = 0").WithLocation(10, 28),
                    Diagnostic("ID0002", "=> 0").WithLocation(11, 21),
                    Diagnostic("ID0002", "=> 0").WithLocation(12, 28),
                    Diagnostic("ID0002", "=> value = 0").WithLocation(12, 38),
                    Diagnostic("ID0002", "=> _ = 0").WithLocation(13, 33),
                    Diagnostic("ID0002", "=> _ = 0").WithLocation(13, 50),
                    Diagnostic("ID0002", "=> 0").WithLocation(14, 14),
                    Diagnostic("ID0002", "=> _field = 0").WithLocation(15, 9),
                    Diagnostic("ID0002", "=> _field = 0").WithLocation(16, 10),
                    Diagnostic("ID0002", "=> 0").WithLocation(17, 39),
                    Diagnostic("ID0002", "{ return 0; }").WithLocation(25, 18),
                    Diagnostic("ID0002", "{ value = 0; }").WithLocation(25, 36),
                    Diagnostic("ID0002", "{ return 0; }").WithLocation(26, 28),
                    Diagnostic("ID0002", "{ value = 0; }").WithLocation(26, 46),
                    Diagnostic("ID0002", "{ _ = 0; }").WithLocation(27, 33),
                    Diagnostic("ID0002", "=> _ = 0").WithLocation(27, 51),
                    Diagnostic("ID0002", "{ return 0; }").WithLocation(28, 14),
                    Diagnostic("ID0002", "{ _field = 0; }").WithLocation(29, 9),
                    Diagnostic("ID0002", "{ _field = 0; }").WithLocation(30, 10),
                    Diagnostic("ID0002", "{ return 0; }").WithLocation(31, 39));
            }

            result.CompilationDiagnostics[analyzer].Verify(
                Diagnostic("ID0001", "P1").WithArguments("get_P1").WithLocation(9, 9),
                Diagnostic("ID0001", "P2").WithArguments("get_P2").WithLocation(10, 9),
                Diagnostic("ID0001", "P2").WithArguments("set_P2").WithLocation(10, 9),
                Diagnostic("ID0001", "this").WithArguments("get_Item").WithLocation(11, 9),
                Diagnostic("ID0001", "this").WithArguments("get_Item").WithLocation(12, 9),
                Diagnostic("ID0001", "this").WithArguments("set_Item").WithLocation(12, 9),
                Diagnostic("ID0001", "E1").WithArguments("add_E1").WithLocation(13, 24),
                Diagnostic("ID0001", "E1").WithArguments("remove_E1").WithLocation(13, 24),
                Diagnostic("ID0001", "P3").WithArguments("get_P3").WithLocation(25, 9),
                Diagnostic("ID0001", "P3").WithArguments("set_P3").WithLocation(25, 9),
                Diagnostic("ID0001", "this").WithArguments("get_Item").WithLocation(26, 9),
                Diagnostic("ID0001", "this").WithArguments("set_Item").WithLocation(26, 9),
                Diagnostic("ID0001", "E2").WithArguments("remove_E2").WithLocation(27, 24),
                Diagnostic("ID0001", "E2").WithArguments("add_E2").WithLocation(27, 24));

            Assert.Empty(result.SyntaxDiagnostics);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private sealed class BlockStartAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "ID0001",
                "Title",
                "{0}",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor DescriptorForBlockEnd = new DiagnosticDescriptor(
                "ID0002",
                "Title",
                "Message",
                "Category",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            private readonly bool _testCodeBlockStart;

            public BlockStartAnalyzer(bool testCodeBlockStart)
                => _testCodeBlockStart = testCodeBlockStart;

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor, DescriptorForBlockEnd);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(context =>
                {
                    // Analyzers should not be allowed to report local diagnostics on the containing
                    // PropertyDeclarationSyntax/IndexerDeclarationSyntax/BaseMethodDeclarationSyntax nodes
                    // when analyzing code block and operation block.
                    if (_testCodeBlockStart)
                    {
                        context.RegisterCodeBlockStartAction<SyntaxKind>(context =>
                        {
                            context.RegisterSyntaxNodeAction(
                                context => analyzeNode(context.Node, context.ContainingSymbol, context.ReportDiagnostic),
                                SyntaxKind.NumericLiteralExpression);

                            context.RegisterCodeBlockEndAction(blockEndContext =>
                            {
                                blockEndContext.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(DescriptorForBlockEnd, blockEndContext.CodeBlock.GetLocation()));

                                if (blockEndContext.CodeBlock is BasePropertyDeclarationSyntax)
                                    throw new Exception($"Unexpected topmost node for code block '{context.CodeBlock.Kind()}'");
                            });
                        });
                    }
                    else
                    {
                        context.RegisterOperationBlockStartAction(context =>
                        {
                            context.RegisterOperationAction(
                                context => analyzeNode(context.Operation.Syntax, context.ContainingSymbol, context.ReportDiagnostic),
                                OperationKind.Literal);

                            context.RegisterOperationBlockEndAction(blockEndContext =>
                            {
                                foreach (var operationBlock in blockEndContext.OperationBlocks)
                                {
                                    blockEndContext.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(DescriptorForBlockEnd, operationBlock.Syntax.GetLocation()));

                                    if (operationBlock.Syntax is PropertyDeclarationSyntax or IndexerDeclarationSyntax)
                                        throw new Exception($"Unexpected topmost node for operation block '{operationBlock.Syntax.Kind()}'");
                                }
                            });
                        });
                    }

                    var uniqueCallbacks = new HashSet<SyntaxNode>();

                    context.RegisterSyntaxNodeAction(context =>
                    {
                        // Ensure that we do not get duplicate callbacks for
                        // PropertyDeclarationSyntax/IndexerDeclarationSyntax/EventDeclarationSyntax/MethodDeclarationSyntax nodes.
                        // Below exception will translate into an unexpected AD0001 diagnostic.
                        if (!uniqueCallbacks.Add(context.Node))
                            throw new Exception($"Multiple callbacks for {context.Node}");
                    }, SyntaxKind.PropertyDeclaration, SyntaxKind.IndexerDeclaration, SyntaxKind.EventDeclaration, SyntaxKind.MethodDeclaration);
                });

                static void analyzeNode(SyntaxNode node, ISymbol containingSymbol, Action<Diagnostic> reportDiagnostic)
                {
                    Location location;
                    if (node.FirstAncestorOrSelf<BasePropertyDeclarationSyntax>() is { } basePropertyDecl)
                    {
                        location = basePropertyDecl switch
                        {
                            PropertyDeclarationSyntax propertyDecl => propertyDecl.Identifier.GetLocation(),
                            IndexerDeclarationSyntax indexerDecl => indexerDecl.ThisKeyword.GetLocation(),
                            EventDeclarationSyntax eventDecl => eventDecl.Identifier.GetLocation(),
                            _ => throw ExceptionUtilities.UnexpectedValue(basePropertyDecl.Kind()),
                        };
                    }
                    else if (node.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>() is { } baseMethodDecl)
                    {
                        location = baseMethodDecl switch
                        {
                            MethodDeclarationSyntax methodDecl => methodDecl.Identifier.GetLocation(),
                            OperatorDeclarationSyntax operatorDecl => operatorDecl.OperatorToken.GetLocation(),
                            ConstructorDeclarationSyntax constructorDecl => constructorDecl.Identifier.GetLocation(),
                            DestructorDeclarationSyntax destructorDecl => destructorDecl.Identifier.GetLocation(),
                            _ => throw ExceptionUtilities.UnexpectedValue(baseMethodDecl.Kind()),
                        };
                    }
                    else
                    {
                        return;
                    }

                    reportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor, location, containingSymbol.Name));
                }
            }
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

        [Theory, CombinatorialData, WorkItem(66968, "https://github.com/dotnet/roslyn/issues/66968")]
        public async Task TestAnalyzerLocalAndNonLocalDiagnostics(LocalNonLocalDiagnosticsAnalyzer.ActionKind actionKind)
        {
            var source1 = @"
class C
{
    void M1()
    {
        int x1a = 0;
        int x1b = 0;
    }

    void M2()
    {
        int x2 = 0;
    }
}

class D
{
    void M3()
    {
        int x3 = 0;
    }
}";
            var source2 = @"
class E
{
    void M4()
    {
        int x4 = 0;
    }
}";
            var compilation = CreateCompilation(new[] { source1, source2 });
            var tree1 = compilation.SyntaxTrees[0];
            var tree2 = compilation.SyntaxTrees[1];
            var analyzer = new LocalNonLocalDiagnosticsAnalyzer(actionKind);
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer), AnalyzerOptions.Empty);

            var result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            // Verify syntax diagnostics.
            if (actionKind == LocalNonLocalDiagnosticsAnalyzer.ActionKind.SyntaxTreeAction)
            {
                result.SyntaxDiagnostics[tree1][analyzer].Verify(
                    Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxTreeAction(File1)").WithLocation(6, 9),
                    Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxTreeAction(File1)").WithLocation(7, 9),
                    Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxTreeAction(File1)").WithLocation(12, 9),
                    Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxTreeAction(File1)").WithLocation(20, 9));
                result.SyntaxDiagnostics[tree2][analyzer].Verify(
                    Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterSyntaxTreeAction(File2)").WithLocation(6, 9));

                result.CompilationDiagnostics[analyzer].Verify(
                    Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterSyntaxTreeAction(File1)").WithLocation(6, 9),
                    Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxTreeAction(File2)").WithLocation(6, 9),
                    Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxTreeAction(File2)").WithLocation(7, 9),
                    Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxTreeAction(File2)").WithLocation(12, 9),
                    Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxTreeAction(File2)").WithLocation(20, 9));

                Assert.Empty(result.SemanticDiagnostics);

                return;
            }

            // Verify semantic and non-local diagnostics.
            Assert.Empty(result.SyntaxDiagnostics);

            var localSemanticDiagnostics_1 = result.SemanticDiagnostics[tree1][analyzer];
            var localSemanticDiagnostics_2 = result.SemanticDiagnostics[tree2][analyzer];
            var nonLocalSemanticDiagnostics = result.CompilationDiagnostics[analyzer];

            switch (actionKind)
            {
                case LocalNonLocalDiagnosticsAnalyzer.ActionKind.SemanticModelAction:
                    localSemanticDiagnostics_1.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSemanticModelAction(File1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSemanticModelAction(File1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSemanticModelAction(File1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSemanticModelAction(File1)").WithLocation(20, 9));
                    localSemanticDiagnostics_2.Verify(
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterSemanticModelAction(File2)").WithLocation(6, 9));
                    nonLocalSemanticDiagnostics.Verify(
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterSemanticModelAction(File1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSemanticModelAction(File2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSemanticModelAction(File2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSemanticModelAction(File2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSemanticModelAction(File2)").WithLocation(20, 9));
                    break;

                case LocalNonLocalDiagnosticsAnalyzer.ActionKind.SymbolAction:
                    localSemanticDiagnostics_1.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSymbolAction(C)(File1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSymbolAction(M1)(File1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSymbolAction(C)(File1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSymbolAction(M1)(File1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSymbolAction(C)(File1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSymbolAction(M2)(File1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSymbolAction(D)(File1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSymbolAction(M3)(File1)").WithLocation(20, 9));
                    localSemanticDiagnostics_2.Verify(
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterSymbolAction(E)(File2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterSymbolAction(M4)(File2)").WithLocation(6, 9));
                    nonLocalSemanticDiagnostics.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSymbolAction(D)(File1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSymbolAction(D)(File1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSymbolAction(D)(File1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSymbolAction(C)(File1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSymbolAction(M1)(File1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSymbolAction(M1)(File1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSymbolAction(M3)(File1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSymbolAction(M3)(File1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSymbolAction(M3)(File1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSymbolAction(M2)(File1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSymbolAction(M2)(File1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSymbolAction(M2)(File1)").WithLocation(20, 9));
                    break;

                case LocalNonLocalDiagnosticsAnalyzer.ActionKind.OperationAction:
                    localSemanticDiagnostics_1.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x1a = 0;)(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x1b = 0;)(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x2 = 0;)(M2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x3 = 0;)(M3)").WithLocation(20, 9));
                    localSemanticDiagnostics_2.Verify(
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterOperationAction(int x4 = 0;)(M4)").WithLocation(6, 9));
                    nonLocalSemanticDiagnostics.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x2 = 0;)(M2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x1b = 0;)(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x3 = 0;)(M3)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x1a = 0;)(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x2 = 0;)(M2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x3 = 0;)(M3)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x1a = 0;)(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x1b = 0;)(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x3 = 0;)(M3)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x1a = 0;)(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x2 = 0;)(M2)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x1b = 0;)(M1)").WithLocation(20, 9));
                    break;

                case LocalNonLocalDiagnosticsAnalyzer.ActionKind.OperationBlockAction:
                    localSemanticDiagnostics_1.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationBlockAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationBlockAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationBlockAction(M2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationBlockAction(M3)").WithLocation(20, 9));
                    localSemanticDiagnostics_2.Verify(
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterOperationBlockAction(M4)").WithLocation(6, 9));
                    nonLocalSemanticDiagnostics.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationBlockAction(M2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationBlockAction(M3)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationBlockAction(M2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationBlockAction(M3)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationBlockAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationBlockAction(M3)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationBlockAction(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationBlockAction(M2)").WithLocation(20, 9));
                    break;

                case LocalNonLocalDiagnosticsAnalyzer.ActionKind.OperationBlockStartEndAction:
                    localSemanticDiagnostics_1.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x1a = 0;) in RegisterOperationBlockStartAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x1b = 0;) in RegisterOperationBlockStartAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationBlockEndAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x1a = 0;) in RegisterOperationBlockStartAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x1b = 0;) in RegisterOperationBlockStartAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationBlockEndAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x2 = 0;) in RegisterOperationBlockStartAction(M2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationBlockEndAction(M2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x3 = 0;) in RegisterOperationBlockStartAction(M3)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationBlockEndAction(M3)").WithLocation(20, 9));
                    localSemanticDiagnostics_2.Verify(
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterOperationAction(int x4 = 0;) in RegisterOperationBlockStartAction(M4)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterOperationBlockEndAction(M4)").WithLocation(6, 9));
                    nonLocalSemanticDiagnostics.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x2 = 0;) in RegisterOperationBlockStartAction(M2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationBlockEndAction(M2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x3 = 0;) in RegisterOperationBlockStartAction(M3)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationBlockEndAction(M3)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x2 = 0;) in RegisterOperationBlockStartAction(M2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationBlockEndAction(M2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x3 = 0;) in RegisterOperationBlockStartAction(M3)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationBlockEndAction(M3)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x3 = 0;) in RegisterOperationBlockStartAction(M3)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x1a = 0;) in RegisterOperationBlockStartAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationBlockEndAction(M3)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x1b = 0;) in RegisterOperationBlockStartAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationBlockEndAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x2 = 0;) in RegisterOperationBlockStartAction(M2)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationBlockEndAction(M2)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x1a = 0;) in RegisterOperationBlockStartAction(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x1b = 0;) in RegisterOperationBlockStartAction(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationBlockEndAction(M1)").WithLocation(20, 9));
                    break;

                case LocalNonLocalDiagnosticsAnalyzer.ActionKind.SyntaxNodeAction:
                    localSemanticDiagnostics_1.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;)(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;)(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;)(M2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;)(M3)").WithLocation(20, 9));
                    localSemanticDiagnostics_2.Verify(
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterSyntaxNodeAction(int x4 = 0;)(M4)").WithLocation(6, 9));
                    nonLocalSemanticDiagnostics.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;)(M2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;)(M3)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;)(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;)(M2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;)(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;)(M3)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;)(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;)(M3)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;)(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;)(M2)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;)(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;)(M1)").WithLocation(20, 9));
                    break;

                case LocalNonLocalDiagnosticsAnalyzer.ActionKind.CodeBlockAction:
                    localSemanticDiagnostics_1.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterCodeBlockAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterCodeBlockAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterCodeBlockAction(M2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterCodeBlockAction(M3)").WithLocation(20, 9));
                    localSemanticDiagnostics_2.Verify(
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterCodeBlockAction(M4)").WithLocation(6, 9));
                    nonLocalSemanticDiagnostics.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterCodeBlockAction(M2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterCodeBlockAction(M3)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterCodeBlockAction(M2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterCodeBlockAction(M3)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterCodeBlockAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterCodeBlockAction(M3)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterCodeBlockAction(M2)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterCodeBlockAction(M1)").WithLocation(20, 9));
                    break;

                case LocalNonLocalDiagnosticsAnalyzer.ActionKind.CodeBlockStartEndAction:
                    localSemanticDiagnostics_1.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;) in RegisterCodeBlockStartAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;) in RegisterCodeBlockStartAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterCodeBlockEndAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;) in RegisterCodeBlockStartAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;) in RegisterCodeBlockStartAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterCodeBlockEndAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;) in RegisterCodeBlockStartAction(M2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterCodeBlockEndAction(M2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;) in RegisterCodeBlockStartAction(M3)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterCodeBlockEndAction(M3)").WithLocation(20, 9));
                    localSemanticDiagnostics_2.Verify(
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterSyntaxNodeAction(int x4 = 0;) in RegisterCodeBlockStartAction(M4)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterCodeBlockEndAction(M4)").WithLocation(6, 9));
                    nonLocalSemanticDiagnostics.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;) in RegisterCodeBlockStartAction(M2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;) in RegisterCodeBlockStartAction(M2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;) in RegisterCodeBlockStartAction(M2)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;) in RegisterCodeBlockStartAction(M3)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;) in RegisterCodeBlockStartAction(M3)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;) in RegisterCodeBlockStartAction(M3)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;) in RegisterCodeBlockStartAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;) in RegisterCodeBlockStartAction(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;) in RegisterCodeBlockStartAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;) in RegisterCodeBlockStartAction(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterCodeBlockEndAction(M2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterCodeBlockEndAction(M2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterCodeBlockEndAction(M2)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterCodeBlockEndAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterCodeBlockEndAction(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterCodeBlockEndAction(M3)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterCodeBlockEndAction(M3)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterCodeBlockEndAction(M3)").WithLocation(12, 9));
                    break;

                case LocalNonLocalDiagnosticsAnalyzer.ActionKind.SymbolStartEndAction:
                    localSemanticDiagnostics_1.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;) in RegisterSymbolStartAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;) in RegisterSymbolStartAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x1a = 0;) in RegisterSymbolStartAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x1b = 0;) in RegisterSymbolStartAction(M1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSymbolEndAction(C)(File1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;) in RegisterSymbolStartAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;) in RegisterSymbolStartAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x1a = 0;) in RegisterSymbolStartAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x1b = 0;) in RegisterSymbolStartAction(M1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSymbolEndAction(C)(File1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;) in RegisterSymbolStartAction(M2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x2 = 0;) in RegisterSymbolStartAction(M2)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSymbolEndAction(C)(File1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;) in RegisterSymbolStartAction(M3)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x3 = 0;) in RegisterSymbolStartAction(M3)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSymbolEndAction(D)(File1)").WithLocation(20, 9));
                    localSemanticDiagnostics_2.Verify(
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterSyntaxNodeAction(int x4 = 0;) in RegisterSymbolStartAction(M4)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterOperationAction(int x4 = 0;) in RegisterSymbolStartAction(M4)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x4 = 0;").WithArguments("RegisterSymbolEndAction(E)(File2)").WithLocation(6, 9));
                    nonLocalSemanticDiagnostics.Verify(
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;) in RegisterSymbolStartAction(M3)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;) in RegisterSymbolStartAction(M2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x2 = 0;) in RegisterSymbolStartAction(M2)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterOperationAction(int x3 = 0;) in RegisterSymbolStartAction(M3)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1a = 0;").WithArguments("RegisterSymbolEndAction(D)(File1)").WithLocation(6, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;) in RegisterSymbolStartAction(M3)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;) in RegisterSymbolStartAction(M2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x2 = 0;) in RegisterSymbolStartAction(M2)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterOperationAction(int x3 = 0;) in RegisterSymbolStartAction(M3)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x1b = 0;").WithArguments("RegisterSymbolEndAction(D)(File1)").WithLocation(7, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x3 = 0;) in RegisterSymbolStartAction(M3)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;) in RegisterSymbolStartAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;) in RegisterSymbolStartAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x1a = 0;) in RegisterSymbolStartAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x1b = 0;) in RegisterSymbolStartAction(M1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterOperationAction(int x3 = 0;) in RegisterSymbolStartAction(M3)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x2 = 0;").WithArguments("RegisterSymbolEndAction(D)(File1)").WithLocation(12, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x2 = 0;) in RegisterSymbolStartAction(M2)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1a = 0;) in RegisterSymbolStartAction(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSyntaxNodeAction(int x1b = 0;) in RegisterSymbolStartAction(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x2 = 0;) in RegisterSymbolStartAction(M2)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x1a = 0;) in RegisterSymbolStartAction(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterOperationAction(int x1b = 0;) in RegisterSymbolStartAction(M1)").WithLocation(20, 9),
                        Diagnostic("ID0001", "int x3 = 0;").WithArguments("RegisterSymbolEndAction(C)(File1)").WithLocation(20, 9));
                    break;

                default:
                    throw ExceptionUtilities.Unreachable();
            }
        }

        [Fact]
        [WorkItem(63923, "https://github.com/dotnet/roslyn/issues/63923")]
        public async Task TestEqualityForCompilerAnalyzerDiagnosticWithPropertyBag()
        {
            var source = @"using System;

public class SomeClass
{
    [property: Test]
    public string Name;
}

internal class TestAttribute : Attribute
{
}
";
            var compilation = CreateCompilation(source);
            var compilationDiagnostics = compilation.GetDiagnostics();
            compilationDiagnostics.Verify(
                // (5,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                //     [property: Test]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "field").WithLocation(5, 6));
            var compilationDiagnostic = compilationDiagnostics.Single();

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer());
            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
            var analyzerDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var analyzerDiagnostic = analyzerDiagnostics.Single();

            // Verify equality for the compiler diagnostic reported from 'CSharpCompilerDiagnosticAnalyzer' with itself.
            Assert.Equal(analyzerDiagnostic, analyzerDiagnostic);

            // Verify the diagnostic from both sources is the same
            Assert.Equal(analyzerDiagnostic.ToString(), compilationDiagnostic.ToString());

            // Verify that diagnostic equality check fails when compared with the same compiler diagnostic
            // fetched from 'compilation.GetDiagnostics()'. Hosts that want to compare compiler diagnostics from
            // different sources should use custom equality comparer.
            Assert.NotEqual(analyzerDiagnostic, compilationDiagnostic);
            Assert.NotEqual(compilationDiagnostic, analyzerDiagnostic);

            // Verify that CS0657 can be suppressed with a DiagnosticSuppressor
            var suppressor = new DiagnosticSuppressorForCS0657();
            analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer(), suppressor);
            var options = new CompilationWithAnalyzersOptions(AnalyzerOptions.Empty, onAnalyzerException: null,
                concurrentAnalysis: false, logAnalyzerExecutionTime: false, reportSuppressedDiagnostics: true);
            compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, options);
            analyzerDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            analyzerDiagnostic = analyzerDiagnostics.Single();
            Assert.True(analyzerDiagnostic.IsSuppressed);
            var suppression = analyzerDiagnostic.ProgrammaticSuppressionInfo.Suppressions.Single();
            Assert.Equal(DiagnosticSuppressorForCS0657.SuppressionId, suppression.Descriptor.Id);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private sealed class DiagnosticSuppressorForCS0657 : DiagnosticSuppressor
        {
            internal const string SuppressionId = "SPR0001";
            private readonly SuppressionDescriptor _descriptor = new(SuppressionId, "CS0657", "Justification");
            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(_descriptor);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                foreach (var diagnostic in context.ReportedDiagnostics)
                {
                    context.ReportSuppression(Suppression.Create(_descriptor, diagnostic));
                }
            }
        }

        [Theory, CombinatorialData, WorkItem(66968, "https://github.com/dotnet/roslyn/issues/66968")]
        public async Task TestAnalyzerCallbacksForSpanBasedDiagnostics(bool testSyntaxTreeAction, bool testSemanticModelAction, bool testSymbolStartAction, bool testBlockActions)
        {
            var source1 = @"
partial class C
{
    void M1()
    {
        int x11 = 0; // Test span
        int x12 = 0;
    }

    void M2()
    {
        int x21 = 0;
        int x22 = 0;
    }
}";

            var source2 = @"
partial class C
{
    void M3()
    {
        int x31 = 0;
        int x32 = 0;
    }
}
";
            var compilation = CreateCompilation(new[] { source1, source2 });
            var tree1 = compilation.SyntaxTrees[0];
            var tree2 = compilation.SyntaxTrees[1];
            var model1 = compilation.GetSemanticModel(tree1);

            var analyzer = new AllActionsAnalyzer(testSyntaxTreeAction, testSemanticModelAction, testSymbolStartAction, testBlockActions);
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer), AnalyzerOptions.Empty);

            if (testSyntaxTreeAction)
            {
                // Verify only SyntaxTree action callback with GetAnalysisResultAsync(tree).
                var syntaxResult = await compilationWithAnalyzers.GetAnalysisResultAsync(tree1, CancellationToken.None);
                Assert.Empty(syntaxResult.GetAllDiagnostics());

                var analyzedTree = Assert.Single(analyzer.AnalyzedTrees);
                Assert.Same(tree1, analyzedTree);

                Assert.Empty(analyzer.AnalyzedSymbols);
                Assert.Empty(analyzer.AnalyzedSymbolStartSymbols);
                Assert.Empty(analyzer.AnalyzedSymbolEndSymbols);
                Assert.Empty(analyzer.AnalyzedCodeBlockSymbols);
                Assert.Empty(analyzer.AnalyzedCodeBlockStartSymbols);
                Assert.Empty(analyzer.AnalyzedCodeBlockEndSymbols);
                Assert.Empty(analyzer.AnalyzedOperationBlockSymbols);
                Assert.Empty(analyzer.AnalyzedOperationBlockStartSymbols);
                Assert.Empty(analyzer.AnalyzedOperationBlockEndSymbols);
                Assert.Empty(analyzer.AnalyzedOperations);
                Assert.Empty(analyzer.AnalyzedOperationsInsideOperationBlock);
                Assert.Empty(analyzer.AnalyzedSyntaxNodes);
                Assert.Empty(analyzer.AnalyzedSyntaxNodesInsideCodeBlock);
                Assert.Empty(analyzer.AnalyzedSemanticModels);

                analyzer.AnalyzedTrees.Clear();
            }

            // Get analyzer semantic diagnostics for first local declaration's span within "M1".
            var localDecl = tree1.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().First();
            Assert.Equal("int x11 = 0;", localDecl.ToString());

            // Verify analyzer callbacks for computing semantic diagnostics for the span.
            var result = await compilationWithAnalyzers.GetAnalysisResultAsync(model1, filterSpan: localDecl.Span, CancellationToken.None);
            Assert.Empty(result.GetAllDiagnostics());

            // Verify no syntax tree action callbacks
            Assert.Empty(analyzer.AnalyzedTrees);

            // Compute expected callbacks based on analyzer registrations.
            var expectedSymbolCallbacks = new HashSet<string>() { "C", "M1" };
            var expectedSyntaxNodeCallbacks = new HashSet<string>() { "int x11 = 0;" };
            var expectedSyntaxNodeInsideBlockCallbacks = new HashSet<string>();
            var expectedOperationCallbacks = new HashSet<string>() { "int x11 = 0" };
            var expectedOperationInsideBlockCallbacks = new HashSet<string>();
            var expectedBlockSymbolCallbacks = new HashSet<string>();
            var expectedSymbolStartSymbolCallbacks = new HashSet<string>();
            var expectedSemanticModelTreeCallbacks = new HashSet<SyntaxTree>();

            if (testBlockActions)
            {
                expectedBlockSymbolCallbacks.Add("M1");

                // As we have registered block actions, we expect callbacks for all nodes/operations in the block.
                expectedSyntaxNodeCallbacks.Add("int x12 = 0;");
                expectedSyntaxNodeInsideBlockCallbacks.Add("int x11 = 0;");
                expectedSyntaxNodeInsideBlockCallbacks.Add("int x12 = 0;");
                expectedOperationCallbacks.Add("int x12 = 0");
                expectedOperationInsideBlockCallbacks.Add("int x11 = 0");
                expectedOperationInsideBlockCallbacks.Add("int x12 = 0");
            }

            if (testSemanticModelAction)
            {
                expectedSemanticModelTreeCallbacks.Add(tree1);
                if (testSymbolStartAction)
                {
                    // As we have registered symbol start actions, we expect callbacks for all files with partial declarations.
                    expectedSemanticModelTreeCallbacks.Add(tree2);
                }
            }

            if (testSymbolStartAction)
            {
                expectedSymbolStartSymbolCallbacks.Add("C");

                expectedSymbolCallbacks.Add("M2");
                expectedSymbolCallbacks.Add("M3");

                expectedSyntaxNodeCallbacks.Add("int x12 = 0;");
                expectedSyntaxNodeCallbacks.Add("int x21 = 0;");
                expectedSyntaxNodeCallbacks.Add("int x22 = 0;");
                expectedSyntaxNodeCallbacks.Add("int x31 = 0;");
                expectedSyntaxNodeCallbacks.Add("int x32 = 0;");

                expectedOperationCallbacks.Add("int x12 = 0");
                expectedOperationCallbacks.Add("int x21 = 0");
                expectedOperationCallbacks.Add("int x22 = 0");
                expectedOperationCallbacks.Add("int x31 = 0");
                expectedOperationCallbacks.Add("int x32 = 0");

                if (testBlockActions)
                {
                    expectedSyntaxNodeInsideBlockCallbacks.Add("int x12 = 0;");
                    expectedSyntaxNodeInsideBlockCallbacks.Add("int x21 = 0;");
                    expectedSyntaxNodeInsideBlockCallbacks.Add("int x22 = 0;");
                    expectedSyntaxNodeInsideBlockCallbacks.Add("int x31 = 0;");
                    expectedSyntaxNodeInsideBlockCallbacks.Add("int x32 = 0;");

                    expectedOperationInsideBlockCallbacks.Add("int x12 = 0");
                    expectedOperationInsideBlockCallbacks.Add("int x21 = 0");
                    expectedOperationInsideBlockCallbacks.Add("int x22 = 0");
                    expectedOperationInsideBlockCallbacks.Add("int x31 = 0");
                    expectedOperationInsideBlockCallbacks.Add("int x32 = 0");

                    expectedBlockSymbolCallbacks.Add("M2");
                    expectedBlockSymbolCallbacks.Add("M3");
                }
            }

            // Verify symbol callbacks
            Assert.Equal(expectedSymbolCallbacks.Count, analyzer.AnalyzedSymbols.Count);
            AssertEx.SetEqual(expectedSymbolCallbacks, analyzer.AnalyzedSymbols.Select(s => s.Name).ToHashSet());

            // Verify syntax node callbacks
            Assert.Equal(expectedSyntaxNodeCallbacks.Count, analyzer.AnalyzedSyntaxNodes.Count);
            AssertEx.All(analyzer.AnalyzedSyntaxNodes, node => node.IsKind(SyntaxKind.LocalDeclarationStatement));
            AssertEx.SetEqual(expectedSyntaxNodeCallbacks, analyzer.AnalyzedSyntaxNodes.Select(s => s.ToString()).ToHashSet());

            Assert.Equal(expectedSyntaxNodeInsideBlockCallbacks.Count, analyzer.AnalyzedSyntaxNodesInsideCodeBlock.Count);
            AssertEx.All(analyzer.AnalyzedSyntaxNodesInsideCodeBlock, node => node.IsKind(SyntaxKind.LocalDeclarationStatement));
            AssertEx.SetEqual(expectedSyntaxNodeInsideBlockCallbacks, analyzer.AnalyzedSyntaxNodesInsideCodeBlock.Select(s => s.ToString()).ToHashSet());

            // Verify operation callbacks
            Assert.Equal(expectedOperationCallbacks.Count, analyzer.AnalyzedOperations.Count);
            AssertEx.All(analyzer.AnalyzedOperations, operation => operation.Kind == OperationKind.VariableDeclaration);
            AssertEx.SetEqual(expectedOperationCallbacks, analyzer.AnalyzedOperations.Select(op => op.Syntax.ToString()).ToHashSet());

            Assert.Equal(expectedOperationInsideBlockCallbacks.Count, analyzer.AnalyzedOperationsInsideOperationBlock.Count);
            AssertEx.All(analyzer.AnalyzedOperationsInsideOperationBlock, operation => operation.Kind == OperationKind.VariableDeclaration);
            AssertEx.SetEqual(expectedOperationInsideBlockCallbacks, analyzer.AnalyzedOperationsInsideOperationBlock.Select(op => op.Syntax.ToString()).ToHashSet());

            // Verify operation and code block callbacks
            var actualBlockSymbolCallbacksArray = new[]
            {
                analyzer.AnalyzedCodeBlockSymbols, analyzer.AnalyzedCodeBlockStartSymbols, analyzer.AnalyzedCodeBlockEndSymbols,
                analyzer.AnalyzedOperationBlockSymbols, analyzer.AnalyzedOperationBlockStartSymbols, analyzer.AnalyzedOperationBlockEndSymbols
            };

            foreach (var actualBlockSymbolCallbacks in actualBlockSymbolCallbacksArray)
            {
                Assert.Equal(expectedBlockSymbolCallbacks.Count, actualBlockSymbolCallbacks.Count);
                AssertEx.SetEqual(expectedBlockSymbolCallbacks, actualBlockSymbolCallbacks.Select(s => s.Name).ToHashSet());
            }

            // Verify SymbolStart/End callbacks
            Assert.Equal(expectedSymbolStartSymbolCallbacks.Count, analyzer.AnalyzedSymbolStartSymbols.Count);
            AssertEx.SetEqual(expectedSymbolStartSymbolCallbacks, analyzer.AnalyzedSymbolStartSymbols.Select(s => s.Name).ToHashSet());
            Assert.Equal(expectedSymbolStartSymbolCallbacks.Count, analyzer.AnalyzedSymbolEndSymbols.Count);
            AssertEx.SetEqual(expectedSymbolStartSymbolCallbacks, analyzer.AnalyzedSymbolEndSymbols.Select(s => s.Name).ToHashSet());

            // Verify SemanticModel callbacks
            Assert.Equal(expectedSemanticModelTreeCallbacks.Count, analyzer.AnalyzedSemanticModels.Count);
            AssertEx.SetEqual(expectedSemanticModelTreeCallbacks, analyzer.AnalyzedSemanticModels.Select(s => s.SyntaxTree).ToHashSet());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68654")]
        public async Task TestAnalyzerLocalDiagnosticsWhenReportedOnEnumFieldSymbol()
        {
            var source = @"
public class Outer
{
    public enum E1
    {
        A1 = 0
    }
}

public enum E2
{
    A2 = 0
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees[0];
            var analyzer = new EnumTypeFieldSymbolAnalyzer();
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer), AnalyzerOptions.Empty);
            var result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            var localSemanticDiagnostics = result.SemanticDiagnostics[tree][analyzer];
            localSemanticDiagnostics.Verify(
                Diagnostic("ID0001", "A1 = 0").WithLocation(6, 9),
                Diagnostic("ID0001", "A2 = 0").WithLocation(12, 5));

            Assert.Empty(result.CompilationDiagnostics);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class EnumTypeFieldSymbolAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor("ID0001", "Title", "Message", "Category", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(symbolContext =>
                {
                    var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                    foreach (var field in namedType.GetMembers().OfType<IFieldSymbol>())
                    {
                        if (!field.IsImplicitlyDeclared)
                        {
                            var diag = CodeAnalysis.Diagnostic.Create(Descriptor, field.DeclaringSyntaxReferences[0].GetLocation());
                            symbolContext.ReportDiagnostic(diag);
                        }
                    }
                }, SymbolKind.NamedType);
            }
        }
    }
}

