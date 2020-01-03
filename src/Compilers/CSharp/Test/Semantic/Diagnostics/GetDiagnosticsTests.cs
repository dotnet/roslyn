// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

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
    }
}
