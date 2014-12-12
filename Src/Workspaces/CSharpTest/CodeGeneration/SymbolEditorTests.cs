using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGeneration
{
    public class SymbolEditorTests
    {
        private readonly SyntaxGenerator g = SyntaxGenerator.GetGenerator(new CustomWorkspace(), LanguageNames.CSharp);

        private Solution GetSolution(params string[] sources)
        {
            var ws = new CustomWorkspace();
            var pid = ProjectId.CreateNewId();

            var docs = sources.Select((s, i) => 
                DocumentInfo.Create(
                    DocumentId.CreateNewId(pid), 
                    name: "code" + i, 
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(s), VersionStamp.Default)))).ToList();

            var proj = ProjectInfo.Create(pid, VersionStamp.Default, "test", "test.dll", LanguageNames.CSharp, documents: docs);

            return ws.AddProject(proj).Solution;
        }

        private IEnumerable<ISymbol> GetSymbols(Solution solution, string name)
        {
            var compilation = solution.Projects.First().GetCompilationAsync().Result;
            return compilation.GlobalNamespace.GetMembers(name);
        }

        private string GetActual(Document document)
        {
            document = Simplifier.ReduceAsync(document).Result;
            document = Formatter.FormatAsync(document, Formatter.Annotation).Result;
            document = Formatter.FormatAsync(document, SyntaxAnnotation.ElasticAnnotation).Result;
            return document.GetSyntaxRootAsync().Result.ToFullString();
        }

        [Fact]
        public void TestEditOneDeclaration()
        {
            var code = 
@"class C
{
}";

            var expected =
@"class C
{
    void m()
    {
    }
}";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, (d, g) => g.AddMembers(d, g.MethodDeclaration("m"))).Result;
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestSequentialEdits()
        {
            var code =
@"class C
{
}";

            var expected =
@"class C
{
    void m()
    {
    }

    void m2()
    {
    }
}";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, (d, g) => g.AddMembers(d, g.MethodDeclaration("m"))).Result;
            Assert.Equal(1, newSymbol.GetMembers("m").Length);
            Assert.Equal(0, newSymbol.GetMembers("m2").Length);

            newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, (d, g) => g.AddMembers(d, g.MethodDeclaration("m2"))).Result;
            Assert.Equal(1, newSymbol.GetMembers("m").Length);
            Assert.Equal(1, newSymbol.GetMembers("m2").Length);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestSequentialEdit_NewSymbols()
        {
            var code =
@"class C
{
}";

            var expected =
@"class C
{
    void m()
    {
    }

    void m2()
    {
    }
}";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, (d, g) => g.AddMembers(d, g.MethodDeclaration("m"))).Result;
            Assert.Equal(1, newSymbol.GetMembers("m").Length);
            Assert.Equal(0, newSymbol.GetMembers("m2").Length);

            newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(newSymbol, (d, g) => g.AddMembers(d, g.MethodDeclaration("m2"))).Result;
            Assert.Equal(1, newSymbol.GetMembers("m").Length);
            Assert.Equal(1, newSymbol.GetMembers("m2").Length);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestSequentialEdits_SeparateSymbols()
        {
            var code =
@"class A
{
}

class B
{
}";

            var expected =
@"class A
{
    void ma()
    {
    }
}

class B
{
    void mb()
    {
    }
}";

            var solution = GetSolution(code);
            var comp = solution.Projects.First().GetCompilationAsync().Result;
            var symbolA = comp.GlobalNamespace.GetMembers("A").First();
            var symbolB = comp.GlobalNamespace.GetMembers("B").First();

            var editor = new SymbolEditor(solution);

            var newSymbolA = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbolA, (d, g) => g.AddMembers(d, g.MethodDeclaration("ma"))).Result;
            Assert.Equal(1, newSymbolA.GetMembers("ma").Length);

            var newSymbolB = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbolB, (d, g) => g.AddMembers(d, g.MethodDeclaration("mb"))).Result;
            Assert.Equal(1, newSymbolB.GetMembers("mb").Length);

            var actual = GetActual(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestSequentialEdits_SeparateSymbolsAndFiles()
        {
            var code1 =
@"class A
{
}";

            var code2 =
@"class B
{
}";

            var expected1 =
@"class A
{
    void ma()
    {
    }
}";

            var expected2 =
@"class B
{
    void mb()
    {
    }
}";

            var solution = GetSolution(code1, code2);
            var comp = solution.Projects.First().GetCompilationAsync().Result;
            var symbolA = comp.GlobalNamespace.GetMembers("A").First();
            var symbolB = comp.GlobalNamespace.GetMembers("B").First();

            var editor = new SymbolEditor(solution);

            var newSymbolA = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbolA, (d, g) => g.AddMembers(d, g.MethodDeclaration("ma"))).Result;
            Assert.Equal(1, newSymbolA.GetMembers("ma").Length);

            var newSymbolB = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbolB, (d, g) => g.AddMembers(d, g.MethodDeclaration("mb"))).Result;
            Assert.Equal(1, newSymbolB.GetMembers("mb").Length);

            var docs = editor.GetChangedDocuments().ToList();
            var actual1 = GetActual(docs[0]);
            var actual2 = GetActual(docs[1]);

            Assert.Equal(expected1, actual1);
            Assert.Equal(expected2, actual2);
        }

        [Fact]
        public void TestEditAllDeclarations_SameFile()
        {
            var code =
@"public partial class C
{
}

public partial class C
{
}";

            var expected =
@"internal partial class C
{
}

internal partial class C
{
}";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditAllDeclarationsAsync(symbol, (d, g) => g.WithAccessibility(d, Accessibility.Internal)).Result;

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEditAllDeclarations_MultipleFiles()
        {
            var code1 =
@"class C
{
}";

            var code2 =
@"class C
{
    void M() {}
}";

            var expected1 =
@"public class C
{
}";

            var expected2 =
@"public class C
{
    void M() {}
}";

            var solution = GetSolution(code1, code2);
            var comp = solution.Projects.First().GetCompilationAsync().Result;
            var symbol = comp.GlobalNamespace.GetMembers("C").First();

            var editor = new SymbolEditor(solution);
            var newSymbol = (INamedTypeSymbol)editor.EditAllDeclarationsAsync(symbol, (d, g) => g.WithAccessibility(d, Accessibility.Public)).Result;

            var docs = editor.GetChangedDocuments().ToList();
            var actual1 = GetActual(docs[0]);
            var actual2 = GetActual(docs[1]);

            Assert.Equal(expected1, actual1);
            Assert.Equal(expected2, actual2);
        }

        [Fact]
        public void TestEditDeclarationWithLocation_Last()
        {
            var code =
@"partial class C
{
}

partial class C
{
}";

            var expected =
@"partial class C
{
}

partial class C
{
    void m()
    {
    }
}";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var location = symbol.Locations.Last();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, location, (d, g) => g.AddMembers(d, g.MethodDeclaration("m"))).Result;
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEditDeclarationWithLocation_First()
        {
            var code =
@"partial class C
{
}

partial class C
{
}";

            var expected =
@"partial class C
{
    void m()
    {
    }
}

partial class C
{
}";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var location = symbol.Locations.First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, location, (d, g) => g.AddMembers(d, g.MethodDeclaration("m"))).Result;
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEditDeclarationWithLocation_SequentialEdits_SameLocation()
        {
            var code =
@"partial class C
{
}

partial class C
{
}";

            var expected =
@"partial class C
{
}

partial class C
{
    void m()
    {
    }

    void m2()
    {
    }
}";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var location = symbol.Locations.Last();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, location, (d, g) => g.AddMembers(d, g.MethodDeclaration("m"))).Result;
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            // reuse location from original symbol/solution
            var newSymbol2 = (INamedTypeSymbol)editor.EditOneDeclarationAsync(newSymbol, location, (d, g) => g.AddMembers(d, g.MethodDeclaration("m2"))).Result;
            Assert.Equal(1, newSymbol2.GetMembers("m").Length);
            Assert.Equal(1, newSymbol2.GetMembers("m2").Length);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEditDeclarationWithLocation_SequentialEdits_NewLocation()
        {
            var code =
@"partial class C
{
}

partial class C
{
}";

            var expected =
@"partial class C
{
}

partial class C
{
    void m()
    {
    }

    void m2()
    {
    }
}";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var location = symbol.Locations.Last();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, location, (d, g) => g.AddMembers(d, g.MethodDeclaration("m"))).Result;
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            // use location from new symbol
            var newLocation = newSymbol.Locations.Last();
            var newSymbol2 = (INamedTypeSymbol)editor.EditOneDeclarationAsync(newSymbol, newLocation, (d, g) => g.AddMembers(d, g.MethodDeclaration("m2"))).Result;
            Assert.Equal(1, newSymbol2.GetMembers("m").Length);
            Assert.Equal(1, newSymbol2.GetMembers("m2").Length);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEditDeclarationWithMember()
        {
            var code =
@"partial class C
{
}

partial class C
{
    void m()
    {
    }
}";

            var expected =
@"partial class C
{
}

partial class C
{
    void m()
    {
    }

    void m2()
    {
    }
}";

            var solution = GetSolution(code);
            var symbol = (INamedTypeSymbol)GetSymbols(solution, "C").First();
            var member = symbol.GetMembers("m").First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, member, (d, g) => g.AddMembers(d, g.MethodDeclaration("m2"))).Result;
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestChangeLogicalIdentityReturnsCorrectSymbol_OneDeclaration()
        {
            // proves that APIs return the correct new symbol even after a change that changes the symbol's logical identity.
            var code =
@"class C
{
}";

            var expected =
@"class X
{
}";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, (d, g) => g.WithName(d, "X")).Result;
            Assert.Equal("X", newSymbol.Name);

            // original symbols cannot be rebound after identity change.
            var reboundSymbol = editor.GetCurrentSymbolAsync(symbol).Result;
            Assert.Null(reboundSymbol);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestChangeLogicalIdentityReturnsCorrectSymbol_AllDeclarations()
        {
            // proves that APIs return the correct new symbol even after a change that changes the symbol's logical identity.
            var code =
@"partial class C
{
}

partial class C
{
}";

            var expected =
@"partial class X
{
}

partial class X
{
}";
            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditAllDeclarationsAsync(symbol, (d, g) => g.WithName(d, "X")).Result;
            Assert.Equal("X", newSymbol.Name);

            // original symbols cannot be rebound after identity change.
            var reboundSymbol = editor.GetCurrentSymbolAsync(symbol).Result;
            Assert.Null(reboundSymbol);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestRemovedDeclarationReturnsNull()
        {
            var code =
@"class C
{
}";

            var expected =
@"";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, (d, g) => null).Result;
            Assert.Null(newSymbol);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestRemovedOneOfManyDeclarationsReturnsChangedSymbol()
        {
            var code =
@"partial class C
{
}

partial class C
{
}";

            var expected =
@"
partial class C
{
}";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbol, (d, g) => null).Result;
            Assert.NotNull(newSymbol);
            Assert.Equal("C", newSymbol.Name);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestRemoveAllOfManyDeclarationsReturnsNull()
        {
            var code =
@"partial class C
{
}

partial class C
{
}";

            var expected =
@"";

            var solution = GetSolution(code);
            var symbol = GetSymbols(solution, "C").First();
            var editor = new SymbolEditor(solution);

            var newSymbol = (INamedTypeSymbol)editor.EditAllDeclarationsAsync(symbol, (d, g) => null).Result;
            Assert.Null(newSymbol);

            var actual = GetActual(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestRemoveFieldFromMultiFieldDeclaration()
        {
            var code =
@"class C
{
    public int X, Y;
}";

            var expected =
@"class C
{
    public int Y;
}";

            var expected2 =
@"class C
{
}";

            var solution = GetSolution(code);
            var symbol = (INamedTypeSymbol)GetSymbols(solution, "C").First();
            var symbolX = symbol.GetMembers("X").First();
            var symbolY = symbol.GetMembers("Y").First();

            var editor = new SymbolEditor(solution);

            // remove X -- should remove only part of the field declaration.
            var newSymbolX = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbolX, (d, g) => null).Result;
            Assert.Null(newSymbolX);

            var actual = GetActual(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);

            // now remove Y -- should remove entire remaining field declaration
            var newSymbolY = (INamedTypeSymbol)editor.EditOneDeclarationAsync(symbolY, (d, g) => null).Result;
            Assert.Null(newSymbolY);

            actual = GetActual(editor.GetChangedDocuments().First());
            Assert.Equal(expected2, actual);
        }
    }
}
