// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Editting
{
    public class SymbolEditorTests
    {
        private readonly SyntaxGenerator _g = SyntaxGenerator.GetGenerator(new AdhocWorkspace(), LanguageNames.CSharp);

        private Solution GetSolution(params string[] sources)
        {
            var ws = new AdhocWorkspace();
            var pid = ProjectId.CreateNewId();

            var docs = sources.Select((s, i) =>
                DocumentInfo.Create(
                    DocumentId.CreateNewId(pid),
                    name: "code" + i,
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(s), VersionStamp.Default)))).ToList();

            var proj = ProjectInfo.Create(pid, VersionStamp.Default, "test", "test.dll", LanguageNames.CSharp, documents: docs,
                metadataReferences: new[] { TestReferences.NetFx.v4_0_30319.mscorlib });

            return ws.AddProject(proj).Solution;
        }

        private async Task<IEnumerable<ISymbol>> GetSymbolsAsync(Solution solution, string name)
        {
            var compilation = await solution.Projects.First().GetCompilationAsync();
            return compilation.GlobalNamespace.GetMembers(name);
        }

        private async Task<string> GetActualAsync(Document document)
        {
            document = await Simplifier.ReduceAsync(document);
            document = await Formatter.FormatAsync(document, Formatter.Annotation);
            document = await Formatter.FormatAsync(document, SyntaxAnnotation.ElasticAnnotation);
            return (await document.GetSyntaxRootAsync()).ToFullString();
        }

        [Fact]
        public async Task TestEditOneDeclaration()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("m")));
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSequentialEdits()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, (e, d) => e.AddMember(d, _g.MethodDeclaration("m")));
            Assert.Equal(1, newSymbol.GetMembers("m").Length);
            Assert.Equal(0, newSymbol.GetMembers("m2").Length);

            newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, (e, d) => e.AddMember(d, _g.MethodDeclaration("m2")));
            Assert.Equal(1, newSymbol.GetMembers("m").Length);
            Assert.Equal(1, newSymbol.GetMembers("m2").Length);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSequentialEdit_NewSymbols()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("m")));
            Assert.Equal(1, newSymbol.GetMembers("m").Length);
            Assert.Equal(0, newSymbol.GetMembers("m2").Length);

            newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(newSymbol, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("m2")));
            Assert.Equal(1, newSymbol.GetMembers("m").Length);
            Assert.Equal(1, newSymbol.GetMembers("m2").Length);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSequentialEdits_SeparateSymbols()
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
            var comp = await solution.Projects.First().GetCompilationAsync();
            var symbolA = comp.GlobalNamespace.GetMembers("A").First();
            var symbolB = comp.GlobalNamespace.GetMembers("B").First();

            var editor = SymbolEditor.Create(solution);

            var newSymbolA = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbolA, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("ma")));
            Assert.Equal(1, newSymbolA.GetMembers("ma").Length);

            var newSymbolB = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbolB, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("mb")));
            Assert.Equal(1, newSymbolB.GetMembers("mb").Length);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSequentialEdits_SeparateSymbolsAndFiles()
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
            var comp = await solution.Projects.First().GetCompilationAsync();
            var symbolA = comp.GlobalNamespace.GetMembers("A").First();
            var symbolB = comp.GlobalNamespace.GetMembers("B").First();

            var editor = SymbolEditor.Create(solution);

            var newSymbolA = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbolA, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("ma")));
            Assert.Equal(1, newSymbolA.GetMembers("ma").Length);

            var newSymbolB = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbolB, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("mb")));
            Assert.Equal(1, newSymbolB.GetMembers("mb").Length);

            var docs = editor.GetChangedDocuments().ToList();
            var actual1 = await GetActualAsync(docs[0]);
            var actual2 = await GetActualAsync(docs[1]);

            Assert.Equal(expected1, actual1);
            Assert.Equal(expected2, actual2);
        }

        [Fact]
        public async Task TestEditAllDeclarations_SameFile()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditAllDeclarationsAsync(symbol, (e, d) => e.SetAccessibility(d, Accessibility.Internal));

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestEditAllDeclarations_MultipleFiles()
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
            var comp = await solution.Projects.First().GetCompilationAsync();
            var symbol = comp.GlobalNamespace.GetMembers("C").First();

            var editor = SymbolEditor.Create(solution);
            var newSymbol = (INamedTypeSymbol)await editor.EditAllDeclarationsAsync(symbol, (e, d) => e.SetAccessibility(d, Accessibility.Public));

            var docs = editor.GetChangedDocuments().ToList();
            var actual1 = await GetActualAsync(docs[0]);
            var actual2 = await GetActualAsync(docs[1]);

            Assert.Equal(expected1, actual1);
            Assert.Equal(expected2, actual2);
        }

        [Fact]
        public async Task TestEditDeclarationWithLocation_Last()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var location = symbol.Locations.Last();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, location, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("m")));
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestEditDeclarationWithLocation_First()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var location = symbol.Locations.First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, location, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("m")));
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestEditDeclarationWithLocation_SequentialEdits_SameLocation()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var location = symbol.Locations.Last();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, location, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("m")));
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            // reuse location from original symbol/solution
            var newSymbol2 = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(newSymbol, location, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("m2")));
            Assert.Equal(1, newSymbol2.GetMembers("m").Length);
            Assert.Equal(1, newSymbol2.GetMembers("m2").Length);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestEditDeclarationWithLocation_SequentialEdits_NewLocation()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var location = symbol.Locations.Last();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, location, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("m")));
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            // use location from new symbol
            var newLocation = newSymbol.Locations.Last();
            var newSymbol2 = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(newSymbol, newLocation, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("m2")));
            Assert.Equal(1, newSymbol2.GetMembers("m").Length);
            Assert.Equal(1, newSymbol2.GetMembers("m2").Length);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestEditDeclarationWithMember()
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
            var symbol = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();
            var member = symbol.GetMembers("m").First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, member, (e, d) => e.AddMember(d, e.Generator.MethodDeclaration("m2")));
            Assert.Equal(1, newSymbol.GetMembers("m").Length);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestChangeLogicalIdentityReturnsCorrectSymbol_OneDeclaration()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, (e, d) => e.SetName(d, "X"));
            Assert.Equal("X", newSymbol.Name);

            // original symbols cannot be rebound after identity change.
            var reboundSymbol = await editor.GetCurrentSymbolAsync(symbol);
            Assert.Null(reboundSymbol);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestChangeLogicalIdentityReturnsCorrectSymbol_AllDeclarations()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditAllDeclarationsAsync(symbol, (e, d) => e.SetName(d, "X"));
            Assert.Equal("X", newSymbol.Name);

            // original symbols cannot be rebound after identity change.
            var reboundSymbol = await editor.GetCurrentSymbolAsync(symbol);
            Assert.Null(reboundSymbol);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestRemovedDeclarationReturnsNull()
        {
            var code =
@"class C
{
}";

            var expected =
@"";

            var solution = GetSolution(code);
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, (e, d) => e.RemoveNode(d));
            Assert.Null(newSymbol);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestRemovedOneOfManyDeclarationsReturnsChangedSymbol()
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
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbol, (e, d) => e.RemoveNode(d));
            Assert.NotNull(newSymbol);
            Assert.Equal("C", newSymbol.Name);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestRemoveAllOfManyDeclarationsReturnsNull()
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
";

            var solution = GetSolution(code);
            var symbol = (await GetSymbolsAsync(solution, "C")).First();
            var editor = SymbolEditor.Create(solution);

            var newSymbol = (INamedTypeSymbol)await editor.EditAllDeclarationsAsync(symbol, (e, d) => e.RemoveNode(d));
            Assert.Null(newSymbol);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestRemoveFieldFromMultiFieldDeclaration()
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
            var symbol = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();
            var symbolX = symbol.GetMembers("X").First();
            var symbolY = symbol.GetMembers("Y").First();

            var editor = SymbolEditor.Create(solution);

            // remove X -- should remove only part of the field declaration.
            var newSymbolX = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbolX, (e, d) => e.RemoveNode(d));
            Assert.Null(newSymbolX);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);

            // now remove Y -- should remove entire remaining field declaration
            var newSymbolY = (INamedTypeSymbol)await editor.EditOneDeclarationAsync(symbolY, (e, d) => e.RemoveNode(d));
            Assert.Null(newSymbolY);

            actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected2, actual);
        }

        [Fact]
        public async Task TestSetBaseType_ExistingBase()
        {
            var code =
@"class C : B
{
}

class A
{
}

class B
{
}";

            var expected =
@"class C : A
{
}

class A
{
}

class B
{
}";

            var solution = GetSolution(code);
            var symbol = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();

            var editor = SymbolEditor.Create(solution);

            // set base to A
            var newSymbolC = await editor.SetBaseTypeAsync(symbol, g => g.IdentifierName("A"));

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSetBaseType_ExistingInterface()
        {
            var code =
@"class C : I
{
}

class A
{
}

interface I
{
}";

            var expected =
@"class C : A, I
{
}

class A
{
}

interface I
{
}";

            var solution = GetSolution(code);
            var symbol = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();

            var editor = SymbolEditor.Create(solution);

            // set base to A
            var newSymbolC = await editor.SetBaseTypeAsync(symbol, g => g.IdentifierName("A"));

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSetBaseType_NoBaseOrInterface()
        {
            var code =
@"class C
{
}

class A
{
}";

            var expected =
@"class C : A
{
}

class A
{
}";

            var solution = GetSolution(code);
            var symbol = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();

            var editor = SymbolEditor.Create(solution);

            // set base to A
            var newSymbolC = await editor.SetBaseTypeAsync(symbol, g => g.IdentifierName("A"));

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSetBaseType_UnknownBase()
        {
            var code =
@"class C : X
{
}

class A
{
}";

            var expected =
@"class C : A
{
}

class A
{
}";

            var solution = GetSolution(code);
            var symbol = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();

            var editor = SymbolEditor.Create(solution);

            // set base to A
            var newSymbolC = editor.SetBaseTypeAsync(symbol, g => g.IdentifierName("A"));

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSetBaseType_Null_ExistingBase()
        {
            var code =
@"class C : A
{
}

class A
{
}";

            var expected =
@"class C
{
}

class A
{
}";

            var solution = GetSolution(code);
            var symbol = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();

            var editor = SymbolEditor.Create(solution);

            // set base to null
            var newSymbolC = await editor.SetBaseTypeAsync(symbol, g => null);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSetBaseType_Null_ExistingBaseAndInterface()
        {
            var code =
@"class C : A, I
{
}

class A
{
}

interface I
{
}";

            var expected =
@"class C : I
{
}

class A
{
}

interface I
{
}";

            var solution = GetSolution(code);
            var symbol = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();

            var editor = SymbolEditor.Create(solution);

            // set base to null
            var newSymbolC = await editor.SetBaseTypeAsync(symbol, g => null);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSetBaseType_Null_ExistingInterface()
        {
            var code =
@"class C : I
{
}

interface I
{
}";

            var expected =
@"class C : I
{
}

interface I
{
}";

            var solution = GetSolution(code);
            var symbol = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();

            var editor = SymbolEditor.Create(solution);

            // set base to null
            var newSymbolC = await editor.SetBaseTypeAsync(symbol, g => null);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSetBaseType_Null_UnknownBase()
        {
            var code =
@"class C : X
{
}";

            var expected =
@"class C
{
}";

            var solution = GetSolution(code);
            var symbol = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();

            var editor = SymbolEditor.Create(solution);

            // set base to null
            var newSymbolC = await editor.SetBaseTypeAsync(symbol, g => null);

            var actual = await GetActualAsync(editor.GetChangedDocuments().First());
            Assert.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(2650, "https://github.com/dotnet/roslyn/issues/2650")]
        public async Task TestEditExplicitInterfaceIndexer()
        {
            var code =
@"public interface I
{
    int this[int item] { get; }
}

public class C  : I
{
    int I.this[int item]
    {
        get
        {
            return item;
        }
    }
}";

            var solution = GetSolution(code);
            var typeC = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "C")).First();
            var property = typeC.GetMembers().First(m => m.Kind == SymbolKind.Property);

            var editor = SymbolEditor.Create(solution);

            var newProperty = editor.EditOneDeclarationAsync(property, (e, d) =>
            {
                // nothing
            });

            var typeI = (INamedTypeSymbol)(await GetSymbolsAsync(solution, "I")).First();
            var iproperty = typeI.GetMembers().First(m => m.Kind == SymbolKind.Property);

            var newIProperty = editor.EditOneDeclarationAsync(iproperty, (e, d) =>
            {
                // nothing;
            });
        }
    }
}
