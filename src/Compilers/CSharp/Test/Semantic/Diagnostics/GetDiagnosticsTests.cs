// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
        public void DiagnosticsFilteredForInsersectingIntervals()
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

        [Fact, WorkItem(1066483)]
        public void TestDiagnosticWithSeverity()
        {
            var source = @"
class C
{
    public void Foo()
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
            Assert.Equal(4, hidden.WarningLevel);

            var info = diag.WithSeverity(DiagnosticSeverity.Info);
            Assert.Equal(DiagnosticSeverity.Info, info.Severity);
            Assert.Equal(DiagnosticSeverity.Warning, info.DefaultSeverity);
            Assert.Equal(4, info.WarningLevel);
        }

        [Fact(Skip ="7446"), WorkItem(7446, "https://github.com/dotnet/roslyn/issues/7446")]
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
            var tree = compilation.SyntaxTrees.Single(t => t == tree1);
            var root = tree.GetRoot();
            var model = compilation.GetSemanticModel(tree);
            model.GetDiagnostics(root.FullSpan);

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
            Assert.True(completedCompilationUnits.Contains(tree.FilePath));
        }

        [Fact(Skip = "7446"), WorkItem(7446, "https://github.com/dotnet/roslyn/issues/7446")]
        public void TestCompilationEventsForPartialMethod()
        {
            var source1 = @"
namespace N1
{
    partial class Class
    {
        private void NonPartialMethod1() { }
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
            var tree = compilation.SyntaxTrees.Single(t => t == tree1);
            var root = tree.GetRoot();
            var model = compilation.GetSemanticModel(tree);
            model.GetDiagnostics(root.FullSpan);

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
            Assert.True(declaredSymbolNames.Contains("PartialMethod"));
            Assert.True(completedCompilationUnits.Contains(tree.FilePath));
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
                        Assert.True(declaredSymbolNames.Add(symbolDeclaredEvent.Symbol.Name), "Unexpected multiple symbol declared events for same symbol");
                    }
                    else
                    {
                        var compilationCompeletedEvent = compEvent as CompilationUnitCompletedEvent;
                        if (compilationCompeletedEvent != null)
                        {
                            Assert.True(completedCompilationUnits.Add(compilationCompeletedEvent.CompilationUnit.FilePath));
                        }
                    }
                }
            }

            return true;
        }
    }
}
