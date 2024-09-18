// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class SemanticModelTests : CSharpTestBase
    {
        [Fact]
        public void RefForEachVar()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M(Span<int> span)
    {
        foreach (ref readonly int rx in span) { }
    }
}");
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var root = tree.GetCompilationUnitRoot();
            var rxDecl = root.DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var model = comp.GetSemanticModel(tree);
            ILocalSymbol rx = model.GetDeclaredSymbol(rxDecl);
            Assert.NotNull(rx);
            Assert.True(rx.IsRef);
            Assert.Equal(RefKind.RefReadOnly, rx.RefKind);
        }

        [Fact]
        public void RefForVar()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(int x)
    {
        for (ref readonly int rx = ref x;;) { }
    }
}");
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var root = tree.GetCompilationUnitRoot();
            var rxDecl = root.DescendantNodes().OfType<ForStatementSyntax>().Single().Declaration;
            var model = comp.GetSemanticModel(tree);
            ISymbol rx = model.GetDeclaredSymbol(rxDecl.Variables.Single());
            Assert.NotNull(rx);
            var rxLocal = Assert.IsAssignableFrom<ILocalSymbol>(rx);
            Assert.True(rxLocal.IsRef);
            Assert.Equal(RefKind.RefReadOnly, rxLocal.RefKind);
        }

        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void TestGetDeclaredSymbolFromNamespace(string ob, string cb)
        {
            var compilation = CreateCompilation($@"
namespace A.B
{ob}
{cb}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var decl = (BaseNamespaceDeclarationSyntax)root.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.NotNull(symbol);
            Assert.Equal("B", symbol.Name);
        }

        [Fact]
        public void NamespaceAndClassWithNoNames()
        {
            var compilation = CreateCompilation(@"
class
{
}

namespace
{
}

class
{
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var decl = (NamespaceDeclarationSyntax)root.Members[1];
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.NotNull(symbol);
            Assert.Equal("", symbol.Name);
        }

        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void TestGetDeclaredSymbolFromNestedNamespace(string ob, string cb)
        {
            var compilation = CreateCompilation($@"
namespace A.B
{ob}
   namespace C.D
   {ob}
   {cb}
{cb}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var abns = (BaseNamespaceDeclarationSyntax)root.Members[0];
            var cdns = (BaseNamespaceDeclarationSyntax)abns.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(cdns);
            Assert.NotNull(symbol);
            Assert.Equal("D", symbol.Name);
        }

        [Fact]
        public void IncompleteNamespaceDeclaration()
        {
            var compilation = CreateCompilation(@"
namespace

class C
{
    void M() { }
}
"
            );

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var classC = (root.Members[0] as NamespaceDeclarationSyntax).Members[0] as TypeDeclarationSyntax;
            var model = compilation.GetSemanticModel(tree);

            var symbol = model.GetDeclaredSymbol(classC);
            Assert.NotNull(symbol);
            Assert.Equal("C", symbol.Name);
        }

        [Fact]
        public void AmbiguousSymbolInNamespaceName()
        {
            var compilation = CreateCompilation(@"
class C { }

namespace C.B 
{ 
    class Y { }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var classY = ((root.
                Members[1] as NamespaceDeclarationSyntax).
                Members[0] as TypeDeclarationSyntax);

            var model = compilation.GetSemanticModel(tree);

            var symbol = model.GetDeclaredSymbol(classY);
            Assert.NotNull(symbol);
            Assert.Equal("C.B.Y", symbol.ToTestDisplayString());
        }

        [Fact]
        public void GetEnumDeclaration()
        {
            var compilation = CreateCompilation(@"
enum E { }
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var enumE = root.Members[0] as EnumDeclarationSyntax;

            var model = compilation.GetSemanticModel(tree);

            var symbol = model.GetDeclaredSymbol(enumE);
            Assert.NotNull(symbol);
            Assert.Equal("E", symbol.ToTestDisplayString());
        }

        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void GenericNameInNamespaceName(string ob, string cb)
        {
            var compilation = CreateCompilation(@"
namespace C<int>.B
" + ob + @"
    class Y { }
" + cb + @"
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var classY = ((root.
                Members[0] as BaseNamespaceDeclarationSyntax).
                Members[0] as TypeDeclarationSyntax);

            var model = compilation.GetSemanticModel(tree);

            var symbol = model.GetDeclaredSymbol(classY);
            Assert.NotNull(symbol);
            Assert.Equal("C.B.Y", symbol.ToTestDisplayString());
        }

        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void AliasedNameInNamespaceName(string ob, string cb)
        {
            var compilation = CreateCompilation(@"
namespace alias::C<int>.B
" + ob + @"
    class Y { }
" + cb + @"
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var classY = ((root.
                Members[0] as BaseNamespaceDeclarationSyntax).
                Members[0] as TypeDeclarationSyntax);

            var model = compilation.GetSemanticModel(tree);

            var symbol = model.GetDeclaredSymbol(classY);
            Assert.NotNull(symbol);
            Assert.Equal("C.B.Y", symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestGetDeclaredSymbolFromType()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var typeSymbol = model.GetDeclaredSymbol(typeDecl);
            Assert.NotNull(typeSymbol);
            Assert.Equal("C", typeSymbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromMethod()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var methodSymbol = model.GetDeclaredSymbol(methodDecl);
            Assert.NotNull(methodSymbol);
            Assert.Equal("M", methodSymbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromProperty()
        {
            var compilation = CreateCompilation(
@"class C
{
    object P
    {
        get { return null; }
        set { }
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var propertyDecl = (PropertyDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var propertySymbol = model.GetDeclaredSymbol(propertyDecl);
            Assert.NotNull(propertySymbol);
            Assert.Equal("P", propertySymbol.Name);

            var accessorList = propertyDecl.AccessorList.Accessors;
            Assert.Equal(2, accessorList.Count);
            foreach (var accessorDecl in accessorList)
            {
                var accessorSymbol = model.GetDeclaredSymbol(accessorDecl);
                Assert.NotNull(accessorSymbol);
            }
        }

        [Fact]
        public void TestGetDeclaredSymbolFromIndexer()
        {
            var compilation = CreateCompilation(
@"class C
{
    object this[int x, int y]
    {
        get { return null; }
        set { }
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = (CSharpSemanticModel)compilation.GetSemanticModel(tree);

            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var indexerDecl = (IndexerDeclarationSyntax)typeDecl.Members[0];
            var paramDecl = indexerDecl.ParameterList.Parameters[0];
            var getterDecl = indexerDecl.AccessorList.Accessors[0];
            var setterDecl = indexerDecl.AccessorList.Accessors[1];

            var propertySymbol = model.GetDeclaredSymbol(indexerDecl);
            Assert.NotNull(propertySymbol);
            Assert.Equal(WellKnownMemberNames.Indexer, propertySymbol.Name);
            Assert.Equal("Item", propertySymbol.MetadataName);
            Assert.Equal("System.Object C.this[System.Int32 x, System.Int32 y] { get; set; }", propertySymbol.ToTestDisplayString());

            var paramSymbol = model.GetDeclaredSymbol(paramDecl);
            Assert.NotNull(paramSymbol);
            Assert.Equal(SpecialType.System_Int32, paramSymbol.Type.SpecialType);
            Assert.Equal("x", paramSymbol.Name);
            Assert.Equal(propertySymbol, paramSymbol.ContainingSymbol);
            Assert.Equal("System.Int32 x", paramSymbol.ToTestDisplayString());

            var getterSymbol = model.GetDeclaredSymbol(getterDecl);
            Assert.NotNull(getterSymbol);
            Assert.Equal(MethodKind.PropertyGet, getterSymbol.MethodKind);
            Assert.Equal(propertySymbol.GetMethod, getterSymbol);
            Assert.Equal("System.Object C.this[System.Int32 x, System.Int32 y].get", getterSymbol.ToTestDisplayString());

            var setterSymbol = model.GetDeclaredSymbol(setterDecl);
            Assert.NotNull(setterSymbol);
            Assert.Equal(MethodKind.PropertySet, setterSymbol.MethodKind);
            Assert.Equal(propertySymbol.SetMethod, setterSymbol);
            Assert.Equal("void C.this[System.Int32 x, System.Int32 y].set", setterSymbol.ToTestDisplayString());
        }

        [Fact]
        public void TestGetDeclaredSymbolFromCustomEvent()
        {
            var compilation = CreateCompilation(
@"class C
{
    event System.Action E
    {
        add { }
        remove { }
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var eventDecl = (EventDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var eventSymbol = model.GetDeclaredSymbol(eventDecl);
            Assert.NotNull(eventSymbol);
            Assert.Equal("E", eventSymbol.Name);
            Assert.IsType<SourceCustomEventSymbol>(eventSymbol.GetSymbol());

            var accessorList = eventDecl.AccessorList.Accessors;
            Assert.Equal(2, accessorList.Count);
            Assert.Same(eventSymbol.AddMethod, model.GetDeclaredSymbol(accessorList[0]));
            Assert.Same(eventSymbol.RemoveMethod, model.GetDeclaredSymbol(accessorList[1]));
        }

        [WorkItem(543494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543494")]
        [Fact()]
        public void TestGetDeclaredSymbolFromFieldLikeEvent()
        {
            var compilation = CreateCompilation(
@"class C
{
    event System.Action E;
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var eventDecl = (EventFieldDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var eventSymbol = model.GetDeclaredSymbol(eventDecl.Declaration.Variables[0]);
            Assert.NotNull(eventSymbol);
            Assert.Null(model.GetDeclaredSymbol(eventDecl)); // the event decl may have multiple variable declarators, the result for GetDeclaredSymbol will always be null
            Assert.Equal("E", eventSymbol.Name);
            Assert.IsType<SourceFieldLikeEventSymbol>(eventSymbol.GetSymbol());
        }

        [WorkItem(543574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543574")]
        [Fact()]
        public void GetDeclaredSymbolOfEventDeclarationSyntaxAsBasePropertyDeclarationSyntax()
        {
            var compilation = CreateCompilation(
@"
public class Test
{
    public event D Iter3 { add {return 5; } remove { return 5; } }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var node = root.FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("public event D Iter3", StringComparison.Ordinal)).Parent as BasePropertyDeclarationSyntax;
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(node);

            Assert.Equal(SymbolKind.Event, symbol.Kind);
            Assert.Equal("Iter3", symbol.Name);
        }

        [WorkItem(543574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543574")]
        [Fact()]
        public void GetDeclaredSymbolOfPropertyDeclarationSyntaxAsBasePropertyDeclarationSyntax()
        {
            var compilation = CreateCompilation(
@"
public class Test
{
    public int Prop { get { return 5; } }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var node = (BasePropertyDeclarationSyntax)typeDecl.Members[0];
            var symbol = model.GetDeclaredSymbol(node);

            Assert.Equal(SymbolKind.Property, symbol.Kind);
            Assert.Equal("Prop", symbol.Name);
        }

        [WorkItem(543574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543574")]
        [Fact()]
        public void GetDeclaredSymbolOfIndexerDeclarationSyntaxAsBasePropertyDeclarationSyntax()
        {
            var compilation = CreateCompilation(
@"
public class Test
{
    object this[int x, int y] { get { return null; } }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var node = (BasePropertyDeclarationSyntax)typeDecl.Members[0];
            var symbol = model.GetDeclaredSymbol(node);

            Assert.Equal(SymbolKind.Property, symbol.Kind);
            Assert.NotNull(symbol);
            Assert.Equal(WellKnownMemberNames.Indexer, symbol.Name);
            Assert.Equal("Item", symbol.MetadataName);
            Assert.Equal("System.Object Test.this[System.Int32 x, System.Int32 y] { get; }", symbol.ToTestDisplayString());
        }

        [WorkItem(543574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543574")]
        [Fact()]
        public void GetDeclaredSymbolOfEventDeclarationSyntax()
        {
            var compilation = CreateCompilation(
@"
public class Test
{
    public event D Iter3 { add {return 5; } remove { return 5; } }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var node = (EventDeclarationSyntax)typeDecl.Members[0];
            var symbol = model.GetDeclaredSymbol(node);

            Assert.Equal(SymbolKind.Event, symbol.Kind);
            Assert.Equal("Iter3", symbol.Name);
        }

        [WorkItem(543574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543574")]
        [Fact()]
        public void GetDeclaredSymbolOfPropertyDeclarationSyntax()
        {
            var compilation = CreateCompilation(
@"
public class Test
{
    public int Prop { get { return 5; } }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var node = (PropertyDeclarationSyntax)typeDecl.Members[0];
            var symbol = model.GetDeclaredSymbol(node);

            Assert.Equal(SymbolKind.Property, symbol.Kind);
            Assert.Equal("Prop", symbol.Name);
        }

        [WorkItem(543574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543574")]
        [Fact()]
        public void GetDeclaredSymbolOfIndexerDeclarationSyntax()
        {
            var compilation = CreateCompilation(
@"
public class Test
{
    object this[int x, int y] { get { return null; } }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var node = (IndexerDeclarationSyntax)typeDecl.Members[0];
            var symbol = model.GetDeclaredSymbol(node);

            Assert.Equal(SymbolKind.Property, symbol.Kind);
            Assert.NotNull(symbol);
            Assert.Equal(WellKnownMemberNames.Indexer, symbol.Name);
            Assert.Equal("Item", symbol.MetadataName);
            Assert.Equal("System.Object Test.this[System.Int32 x, System.Int32 y] { get; }", symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestGetDeclaredSymbolFromLocalDeclarator()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    int x = 10;
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(localDecl.Declaration.Variables[0]);
            Assert.NotNull(symbol);
            Assert.Equal("x", symbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromMultipleLocalDeclarators()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    int x = 10, y = 20;
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(localDecl.Declaration.Variables[0]);
            Assert.NotNull(symbol);
            Assert.Equal("x", symbol.Name);

            symbol = model.GetDeclaredSymbol(localDecl.Declaration.Variables[1]);
            Assert.NotNull(symbol);
            Assert.Equal("y", symbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromMultipleLabelDeclarators()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    label1:
    label2:
       object x = this;
  }
}
");
            dynamic tree = compilation.SyntaxTrees[0];
            dynamic methodDecl = tree.GetCompilationUnitRoot().Members[0].Members[0];
            var model = compilation.GetSemanticModel(tree);

            LabeledStatementSyntax labeled1 = methodDecl.Body.Statements[0];
            var symbol1 = (ISymbol)model.GetDeclaredSymbol(labeled1);
            Assert.NotNull(symbol1);
            Assert.Equal("label1", symbol1.Name);

            var labeled2 = (LabeledStatementSyntax)labeled1.Statement;
            var symbol2 = (ISymbol)model.GetDeclaredSymbol(labeled2);
            Assert.NotNull(symbol2);
            Assert.Equal("label2", symbol2.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromAnonymousTypePropertyInitializer()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    var o = new { a, b = """" };
  }
  public static int a = 123;
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            TestAnonymousTypePropertySymbol(model,
                                tree.FindNodeOrTokenByKind(SyntaxKind.AnonymousObjectMemberDeclarator, 1).AsNode(),
                                "System.Int32 <anonymous type: System.Int32 a, System.String b>.a { get; }");

            TestAnonymousTypePropertySymbol(model,
                                tree.FindNodeOrTokenByKind(SyntaxKind.AnonymousObjectMemberDeclarator, 2).AsNode(),
                                "System.String <anonymous type: System.Int32 a, System.String b>.b { get; }");
        }

        [Fact]
        public void TestGetDeclaredSymbolFromAnonymousTypePropertyInitializer_WithErrors()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    var o = new { a, a(), b = 123, c = null };
  }
  public static int a() { return 1; }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            TestAnonymousTypePropertySymbol(model,
                                            tree.FindNodeOrTokenByKind(SyntaxKind.AnonymousObjectMemberDeclarator, 1).AsNode(),
                                            "error <anonymous type: error a, System.Int32 $1, System.Int32 b, error c>.a { get; }");

            TestAnonymousTypePropertySymbol(model,
                                            tree.FindNodeOrTokenByKind(SyntaxKind.AnonymousObjectMemberDeclarator, 2).AsNode(),
                                            "System.Int32 <anonymous type: error a, System.Int32 $1, System.Int32 b, error c>.$1 { get; }");

            TestAnonymousTypePropertySymbol(model,
                                            tree.FindNodeOrTokenByKind(SyntaxKind.AnonymousObjectMemberDeclarator, 3).AsNode(),
                                            "System.Int32 <anonymous type: error a, System.Int32 $1, System.Int32 b, error c>.b { get; }");

            TestAnonymousTypePropertySymbol(model,
                                            tree.FindNodeOrTokenByKind(SyntaxKind.AnonymousObjectMemberDeclarator, 4).AsNode(),
                                            "error <anonymous type: error a, System.Int32 $1, System.Int32 b, error c>.c { get; }");
        }

        private void TestAnonymousTypePropertySymbol(SemanticModel model, SyntaxNode node, string name)
        {
            Assert.NotNull(node);
            var symbol = model.GetDeclaredSymbol(node);
            Assert.NotNull(symbol);
            Assert.Equal(name, symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestGetDeclaredSymbolFromSwitchCaseLabel()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    int val = 0;
    switch(val)
    {
        case 0:
            break;
    }
  }
}
");
            dynamic tree = compilation.SyntaxTrees[0];
            dynamic methodDecl = tree.GetCompilationUnitRoot().Members[0].Members[0];
            var model = compilation.GetSemanticModel(tree);

            SwitchStatementSyntax switchStmt = methodDecl.Body.Statements[1];
            SwitchLabelSyntax switchLabel = switchStmt.Sections[0].Labels[0];

            var symbol = (ISymbol)model.GetDeclaredSymbol(switchLabel);
            Assert.NotNull(symbol);

            var labelSymbol = (SourceLabelSymbol)symbol.GetSymbol();
            Assert.Equal(ConstantValue.Default(SpecialType.System_Int32), labelSymbol.SwitchCaseLabelConstant);
            Assert.Equal(switchLabel, labelSymbol.IdentifierNodeOrToken.AsNode());
            Assert.Equal("case 0:", labelSymbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromSwitchDefaultLabel()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    int val = 0;
    switch(val)
    {
        default:
            break;
    }
  }
}
");
            dynamic tree = compilation.SyntaxTrees[0];
            dynamic methodDecl = tree.GetCompilationUnitRoot().Members[0].Members[0];
            var model = compilation.GetSemanticModel(tree);

            SwitchStatementSyntax switchStmt = methodDecl.Body.Statements[1];
            SwitchLabelSyntax switchLabel = switchStmt.Sections[0].Labels[0];
            var symbol1 = (ISymbol)model.GetDeclaredSymbol(switchLabel);
            Assert.NotNull(symbol1);

            var labelSymbol = (SourceLabelSymbol)symbol1.GetSymbol();
            Assert.Null(labelSymbol.SwitchCaseLabelConstant);
            Assert.Equal(switchLabel, labelSymbol.IdentifierNodeOrToken.AsNode());
            Assert.Equal("default", labelSymbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromFieldDeclarator()
        {
            var compilation = CreateCompilation(@"
class C 
{
  int x;

  void M()
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var fieldDecl = (FieldDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0]);
            Assert.NotNull(symbol);
            Assert.Equal("x", symbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromMultipleFieldDeclarators()
        {
            var compilation = CreateCompilation(@"
class C 
{
  int x, y;

  void M()
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var fieldDecl = (FieldDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0]);
            Assert.NotNull(symbol);
            Assert.Equal("x", symbol.Name);

            symbol = model.GetDeclaredSymbol(fieldDecl.Declaration.Variables[1]);
            Assert.NotNull(symbol);
            Assert.Equal("y", symbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromParameter()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M(int x)
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(methodDecl.ParameterList.Parameters[0]);
            Assert.NotNull(symbol);
            Assert.Equal("x", symbol.Name);
        }

        [WorkItem(540108, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540108")]
        [Fact]
        public void TestGetDeclaredSymbolFromDelegateParameter()
        {
            var compilation = CreateCompilation(@"
delegate D(int x);
");
            var tree = compilation.SyntaxTrees[0];
            var delegateDecl = (DelegateDeclarationSyntax)(tree.GetCompilationUnitRoot().Members[0]);

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(delegateDecl.ParameterList.Parameters[0]);
            Assert.NotNull(symbol);
            Assert.Equal("x", symbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromMultipleParameter()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M(int x, int y)
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(methodDecl.ParameterList.Parameters[0]);
            Assert.NotNull(symbol);
            Assert.Equal("x", symbol.Name);

            symbol = model.GetDeclaredSymbol(methodDecl.ParameterList.Parameters[1]);
            Assert.NotNull(symbol);
            Assert.Equal("y", symbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromClassTypeParameter()
        {
            var compilation = CreateCompilation(@"
class C<T>
{
  void M()
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var typeDecl = (TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0];

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(typeDecl.TypeParameterList.Parameters[0]);
            Assert.NotNull(symbol);
            Assert.Equal("T", symbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromMethodTypeParameter()
        {
            var compilation = CreateCompilation(@"
class C
{
  void M<T>()
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(methodDecl.TypeParameterList.Parameters[0]);
            Assert.NotNull(symbol);
            Assert.Equal("T", symbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromGenericPartialType()
        {
            var compilation = CreateCompilation(@"
namespace N1.N2
{
    public struct St<T>
    {
        private T field;
        public enum Em { Zero, One, Two, Three}
    }
}

namespace N1.N2
{
    public partial interface IGoo<T, V>
    {
        St<object>.Em ReturnEnum();
    }

    public partial interface IGoo<T, V>
    {
        void M(St<T> p1, St<V> p2);
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var root = tree.GetCompilationUnitRoot();
            var nsDecl = (NamespaceDeclarationSyntax)root.Members[0];
            var nsSymbol = model.GetDeclaredSymbol(nsDecl);
            var typeDecl = (TypeDeclarationSyntax)nsDecl.Members[0];
            var structSymbol = model.GetDeclaredSymbol(typeDecl);
            Assert.NotNull(structSymbol);
            Assert.Equal(1, structSymbol.Arity);
            Assert.Equal("St", structSymbol.Name);
            Assert.Equal("N1.N2.St<T>", structSymbol.ToTestDisplayString());

            var fieldDecl = (FieldDeclarationSyntax)typeDecl.Members[0];
            var fSymbol = model.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0]) as IFieldSymbol;
            Assert.Equal("field", fSymbol.Name);
            Assert.Equal("T", fSymbol.Type.Name);
            Assert.Equal<ISymbol>(structSymbol, fSymbol.ContainingSymbol);
            Assert.Null(model.GetDeclaredSymbol(fieldDecl));

            var enumDecl = (EnumDeclarationSyntax)typeDecl.Members[1];
            var enumSymbol = model.GetDeclaredSymbol(enumDecl);
            Assert.Equal("Em", enumSymbol.Name);

            nsDecl = (NamespaceDeclarationSyntax)root.Members[1];
            var nsSymbol01 = model.GetDeclaredSymbol(nsDecl);
            Assert.Equal(nsSymbol, nsSymbol01);

            typeDecl = (TypeDeclarationSyntax)nsDecl.Members[0];
            var itfcSymbol = model.GetDeclaredSymbol(typeDecl);
            Assert.Equal(2, itfcSymbol.Arity);
            Assert.Equal("N1.N2.IGoo<T, V>", itfcSymbol.ToTestDisplayString());
            // CC
            var pt = typeDecl.TypeParameterList.Parameters[1];
            var ptsym = model.GetDeclaredSymbol(pt);
            Assert.Equal(SymbolKind.TypeParameter, ptsym.Kind);
            Assert.Equal("V", ptsym.Name);

            var memDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var mSymbol = model.GetDeclaredSymbol(memDecl) as IMethodSymbol;
            Assert.Equal("ReturnEnum", mSymbol.Name);
            Assert.Equal("N1.N2.St<System.Object>.Em", mSymbol.ReturnType.ToTestDisplayString());
            Assert.Equal<ISymbol>(enumSymbol, mSymbol.ReturnType.OriginalDefinition);

            typeDecl = (TypeDeclarationSyntax)nsDecl.Members[1];
            memDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            mSymbol = model.GetDeclaredSymbol(memDecl) as IMethodSymbol;
            Assert.Equal(2, mSymbol.Parameters.Length);
            Assert.Equal("p1", mSymbol.Parameters[0].Name);
            Assert.Equal("St", mSymbol.Parameters[0].Type.Name);
            Assert.Equal<ISymbol>(structSymbol, mSymbol.Parameters[1].Type.OriginalDefinition);
            // CC
            var psym = model.GetDeclaredSymbol(memDecl.ParameterList.Parameters[0]);
            Assert.Equal(SymbolKind.Parameter, psym.Kind);
            Assert.Equal("p1", psym.Name);
        }

        [Fact]
        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        public void TestGetDeclaredSymbolWithIncompleteDeclaration()
        {
            var compilation = CreateCompilation(@"
class C0 { }

class 

class C1 { }
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (ClassDeclarationSyntax)root.Members[1];
            var model = compilation.GetSemanticModel(tree);

            var symbol = model.GetDeclaredSymbol(typeDecl);

            Assert.NotNull(symbol);
            Assert.Equal(string.Empty, symbol.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, symbol.TypeKind);
        }

        [WorkItem(537230, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537230")]
        [Theory]
        [MemberData(nameof(FileScopedOrBracedNamespace))]
        public void TestLookupUnresolvableNamespaceUsing(string ob, string cb)
        {
            var compilation = CreateCompilation(@"
                namespace A
                " + ob + @"
                    using B.C;

                    public class B : C
                    {    
                    }
                " + cb + @"
            ");
            var tree = compilation.SyntaxTrees.Single();
            var usingDirective = (UsingDirectiveSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.UsingDirective).AsNode();
            var model = compilation.GetSemanticModel(tree);
            var type = model.GetTypeInfo(usingDirective.Name);
            Assert.NotEmpty(compilation.GetDeclarationDiagnostics());
            // should validate type here
        }

        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void TestLookupSourceSymbolHidesMetadataSymbol(string ob, string cb)
        {
            var compilation = CreateCompilation(@"
namespace System
" + ob + @"
    public class DateTime
    {
        string TheDateAndTime;
    }
" + cb + @"
");

            var tree = compilation.SyntaxTrees.Single();

            var namespaceDecl = (BaseNamespaceDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0];
            var classDecl = (ClassDeclarationSyntax)namespaceDecl.Members[0];
            var memberDecl = (FieldDeclarationSyntax)classDecl.Members[0];

            var model = compilation.GetSemanticModel(tree);
            var symbols = model.LookupSymbols(memberDecl.SpanStart, null, "DateTime");
            Assert.Equal(1, symbols.Length);
        }

        [Fact]
        public void TestLookupAllArities()
        {
            var compilation = CreateCompilation(@"
                class B {}

                class A
                {
                   class B<X> {}
                   class B<X,Y> {}
 
                   void M()
                   {
                      int B;
                   }
                }
            ");

            var tree = compilation.SyntaxTrees.Single();
            var cu = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)cu.Members[1];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[2];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];

            var model = compilation.GetSemanticModel(tree);

            var symbols = model.LookupSymbols(methodDecl.SpanStart, null, "B");
            Assert.Equal(3, symbols.Length);
            Assert.Equal(SymbolKind.NamedType, symbols[0].Kind);

            symbols = model.LookupSymbols(localDecl.SpanStart, null, "B");
            Assert.Equal(3, symbols.Length);
            Assert.Equal(SymbolKind.Local, symbols[0].Kind);
        }

        [Fact]
        public void TestLookupWithinClassAllArities()
        {
            var compilation = CreateCompilation(@"
                using AliasZ = B.Z;

                class A
                {
                   public class Z<X> {}
                   public class Z<X,Y> {}
                }

                class B : A
                {
                   public class Z {}
                   public class Z<X> {}
                }

                class C : B
                {
                   public int Z;
                }
            ");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclB = (TypeDeclarationSyntax)cu.Members[1];
            var someMemberInB = (MemberDeclarationSyntax)typeDeclB.Members[0];
            int positionInB = someMemberInB.SpanStart;

            var symbols = model.LookupSymbols(positionInB, name: "Z");
            Assert.Equal(3, symbols.Length);
            Assert.Equal(SymbolKind.NamedType, symbols[0].Kind);

            var typeDeclC = (TypeDeclarationSyntax)cu.Members[2];
            var someMemberInC = (MemberDeclarationSyntax)typeDeclC.Members[0];
            int positionInC = someMemberInC.SpanStart;

            symbols = model.LookupSymbols(positionInC, name: "Z");
            Assert.Equal(3, symbols.Length);
            Assert.Equal(SymbolKind.Field, symbols[0].Kind);

            symbols = model.LookupSymbols(positionInC, name: "AliasZ");
            Assert.Equal(1, symbols.Length);
            Assert.Equal(SymbolKind.Alias, symbols[0].Kind);

            symbols = model.LookupSymbols(positionInC, name: "C");
            var container = (INamespaceOrTypeSymbol)symbols.Single();

            symbols = model.LookupSymbols(positionInC, name: "AliasZ", container: container);
            Assert.Equal(0, symbols.Length);
        }

        [Fact]
        public void TestLookupTypesAllArities()
        {
            var compilation = CreateCompilation(@"
                class B {}

                class A
                {
                   class B<X> {}
                   class B<X,Y> {}
 
                   void M()
                   {
                      int B;
                   }
                }
            ");

            var tree = compilation.SyntaxTrees.Single();
            var cu = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)cu.Members[1];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[2];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];

            var model = compilation.GetSemanticModel(tree);

            var symbols = model.LookupNamespacesAndTypes(localDecl.SpanStart, name: "B");
            Assert.Equal(3, symbols.Length);
            Assert.Equal(SymbolKind.NamedType, symbols[0].Kind);
        }

        [Fact]
        public void TestLookupSymbolNames()
        {
            var compilation = CreateCompilation(@"
                class A
                {
                   public int X;
                }

                class B : A
                {
                   public int Y;
                }

                class C : B
                {
                   public int Z;

                   public void M()
                   {
                   }
                }
            ", targetFramework: TargetFramework.NetStandard20);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclC = (TypeDeclarationSyntax)cu.Members[2];
            var someMemberInC = (MemberDeclarationSyntax)typeDeclC.Members[0];
            int positionInC = someMemberInC.SpanStart;

            var namesInC = model.LookupNames(positionInC, namespacesAndTypesOnly: true);
            Assert.Equal(5, namesInC.Count);  // A, B, C, System, Microsoft
            Assert.Contains("A", namesInC);
            Assert.Contains("B", namesInC);
            Assert.Contains("C", namesInC);
            Assert.Contains("System", namesInC);
            Assert.Contains("Microsoft", namesInC);

            var methodM = (MethodDeclarationSyntax)typeDeclC.Members[1];
            var namesInM = model.LookupNames(methodM.Body.SpanStart);
            Assert.Equal(16, namesInM.Count);
        }

        [Fact]
        public void TestLookupSymbolNamesInCyclicClass()
        {
            var compilation = CreateCompilation(@"
                class A : B
                {
                   public int X;
                }

                class B : A
                {
                   public int Y;
                }
            ", targetFramework: TargetFramework.NetStandard20);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclA = (TypeDeclarationSyntax)cu.Members[0];
            var someMemberInA = (MemberDeclarationSyntax)typeDeclA.Members[0];

            var namesInA = model.LookupNames(someMemberInA.SpanStart);
            Assert.Equal(13, namesInA.Count);
            Assert.Contains("X", namesInA);
            Assert.Contains("Y", namesInA);
            Assert.Contains("ToString", namesInA);
        }

        [Fact]
        public void TestLookupSymbolNamesInInterface()
        {
            var compilation = CreateCompilation(@"
                interface A
                {
                   void AM();
                }

                interface B : A
                {
                   void BX();
                }

                interface C : B
                {
                   void CM();
                }
            ", targetFramework: TargetFramework.NetStandard20);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclC = (TypeDeclarationSyntax)cu.Members[2];
            var someMemberInC = (MemberDeclarationSyntax)typeDeclC.Members[0];

            var namesInC = model.LookupNames(someMemberInC.SpanStart);
            Assert.Equal(13, namesInC.Count); // everything with exception of protected members in System.Object is included, with an uncertain count
            Assert.Contains("A", namesInC);
            Assert.Contains("B", namesInC);
            Assert.Contains("C", namesInC);
            Assert.Contains("AM", namesInC);
            Assert.Contains("BX", namesInC);
            Assert.Contains("CM", namesInC);
            Assert.Contains("System", namesInC);
            Assert.Contains("Microsoft", namesInC);
        }

        [Fact]
        public void TestLookupSymbolNamesInTypeParameter()
        {
            var compilation = CreateCompilation(@"
interface IA
{
    void MA();
}
interface IB
{
    void MB();
}
interface IC
{
    void M<T, U>();
}
class C : IA
{
    void IA.MA() { }
    public void M<T>() { }
}
class D<T>
    where T : IB
{
    void MD<U, V>(U u, V v)
        where U : C, T, IC
        where V : struct
    {
    }
}");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)cu.Members[4];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var parameterDecl = (ParameterSyntax)methodDecl.ParameterList.Parameters[0];
            var paramSymbol = model.GetDeclaredSymbol(parameterDecl);
            var names = model.LookupNames(methodDecl.SpanStart, paramSymbol.Type);
            Assert.Equal(7, names.Count);
            Assert.Contains("M", names);
            Assert.Contains("ToString", names);
            Assert.Contains("ReferenceEquals", names);
            Assert.Contains("GetHashCode", names);
            Assert.Contains("GetType", names);
            Assert.Contains("Equals", names);
            Assert.Contains("MB", names);

            parameterDecl = (ParameterSyntax)methodDecl.ParameterList.Parameters[1];
            paramSymbol = model.GetDeclaredSymbol(parameterDecl);
            names = model.LookupNames(methodDecl.SpanStart, paramSymbol.Type);
            Assert.Equal(5, names.Count);
            Assert.Contains("ToString", names);
            Assert.Contains("ReferenceEquals", names);
            Assert.Contains("GetHashCode", names);
            Assert.Contains("GetType", names);
            Assert.Contains("Equals", names);
        }

        [Fact]
        public void TestLookupSymbolsInInterface()
        {
            var compilation = CreateCompilation(@"
                interface A
                {
                   void M<T>(T t);
                }

                interface B : A
                {
                   void M<T, U>(T t, U u);
                }

                interface C : B
                {
                   void F();
                   void M<T, U, V>(T t, U u, V v);
                }
            ", targetFramework: TargetFramework.NetStandard20);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclC = (TypeDeclarationSyntax)cu.Members[2];
            var someMemberInC = (MemberDeclarationSyntax)typeDeclC.Members[0];
            int positionInC = someMemberInC.SpanStart;

            var symbolsInC = model.LookupSymbols(positionInC);
            var test = symbolsInC.Where(s => s.ContainingAssembly == null).ToList();
            Assert.Equal(9, symbolsInC.Where(s => s.ContainingType == null || s.ContainingType.SpecialType != SpecialType.System_Object).Count());
            Assert.True(symbolsInC.Any(s => s.Name == "A" && s.Kind == SymbolKind.NamedType));
            Assert.True(symbolsInC.Any(s => s.Name == "B" && s.Kind == SymbolKind.NamedType));
            Assert.True(symbolsInC.Any(s => s.Name == "C" && s.Kind == SymbolKind.NamedType));
            Assert.True(symbolsInC.Any(s => s.Name == "M" && s.Kind == SymbolKind.Method && s.ContainingType.Name == "A"));
            Assert.True(symbolsInC.Any(s => s.Name == "M" && s.Kind == SymbolKind.Method && s.ContainingType.Name == "B"));
            Assert.True(symbolsInC.Any(s => s.Name == "M" && s.Kind == SymbolKind.Method && s.ContainingType.Name == "C"));
            Assert.True(symbolsInC.Any(s => s.Name == "F" && s.Kind == SymbolKind.Method && s.ContainingType.Name == "C"));
            Assert.True(symbolsInC.Any(s => s.Name == "System" && s.Kind == SymbolKind.Namespace));
            Assert.True(symbolsInC.Any(s => s.Name == "Microsoft" && s.Kind == SymbolKind.Namespace));
        }

        [Fact]
        public void TestLookupSymbolsInTypeParameter()
        {
            var compilation = CreateCompilation(@"
interface IA
{
    void MA();
}
interface IB
{
    void MB();
}
interface IC
{
    void M<T, U>();
}
class C : IA
{
    void IA.MA() { }
    public void M<T>() { }
}
class D<T>
    where T : IB
{
    void MD<U, V>(U u, V v)
        where U : C, T, IC
        where V : struct
    {
    }
}");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)cu.Members[4];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var parameterDecl = (ParameterSyntax)methodDecl.ParameterList.Parameters[0];
            var paramSymbol = model.GetDeclaredSymbol(parameterDecl);
            var symbols = model.LookupSymbols(methodDecl.SpanStart, paramSymbol.Type);
            CheckSymbolsUnordered(symbols,
                "void C.M<T>()",
                "void IC.M<T, U>()",
                "string object.ToString()",
                "bool object.Equals(object obj)",
                "bool object.Equals(object objA, object objB)",
                "bool object.ReferenceEquals(object objA, object objB)",
                "int object.GetHashCode()",
                "Type object.GetType()",
                "void IB.MB()");

            parameterDecl = (ParameterSyntax)methodDecl.ParameterList.Parameters[1];
            paramSymbol = model.GetDeclaredSymbol(parameterDecl);
            symbols = model.LookupSymbols(methodDecl.SpanStart, paramSymbol.Type);
            CheckSymbolsUnordered(symbols,
                "bool ValueType.Equals(object obj)",
                "bool object.Equals(object obj)",
                "bool object.Equals(object objA, object objB)",
                "int ValueType.GetHashCode()",
                "int object.GetHashCode()",
                "string ValueType.ToString()",
                "string object.ToString()",
                "bool object.ReferenceEquals(object objA, object objB)",
                "Type object.GetType()");
        }

        [Fact]
        public void TestLookupSymbolsTypeParameterConstraints()
        {
            var compilation = CreateCompilation(
@"interface I<T> where T : new() { }
class A
{
    void M<T>() where T : struct, I<int> { }
}
struct B<T> where T : A
{
    void M<U>() where U : T { }
}");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var cu = tree.GetCompilationUnitRoot();

            var interfaceDecl = (InterfaceDeclarationSyntax)cu.Members[0];
            var symbol = LookupTypeParameterFromConstraintClause(model, interfaceDecl.ConstraintClauses[0], "T");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.Constructor);

            var classDecl = (ClassDeclarationSyntax)cu.Members[1];
            var methodDecl = (MethodDeclarationSyntax)classDecl.Members[0];
            symbol = LookupTypeParameterFromConstraintClause(model, methodDecl.ConstraintClauses[0], "T");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.ValueType, "I<int>");

            var structDecl = (StructDeclarationSyntax)cu.Members[2];
            symbol = LookupTypeParameterFromConstraintClause(model, structDecl.ConstraintClauses[0], "T");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.None, "A");

            methodDecl = (MethodDeclarationSyntax)structDecl.Members[0];
            symbol = LookupTypeParameterFromConstraintClause(model, methodDecl.ConstraintClauses[0], "U");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.None, "T");
        }

        /// <summary>
        /// Cycles should be broken at the first cycle encountered by
        /// traversing the constraints in declaration order. It should not depend
        /// on the order the symbols are queried from the binding API.
        /// </summary>
        [Fact]
        public void TestLookupSymbolsTypeParameterConstraintCycles()
        {
            var compilation = CreateCompilation(
@"interface IA<T1, T2>
    where T1 : T2
    where T2 : T1
{
    void M<U1, U2>()
        where U1 : U2
        where U2 : U1;
}
interface IB<T3, T4>
    where T3 : T4
    where T4 : T3
{
    void M<U3, U4>()
        where U3 : U4
        where U4 : U3;
}");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var cu = tree.GetCompilationUnitRoot();

            // Query for type parameters in declaration order.
            var interfaceDecl = (InterfaceDeclarationSyntax)cu.Members[0];
            var symbol = LookupTypeParameterFromConstraintClause(model, interfaceDecl.ConstraintClauses[0], "T1");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.None, "T2");
            symbol = LookupTypeParameterFromConstraintClause(model, interfaceDecl.ConstraintClauses[0], "T2");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.None);
            var methodDecl = (MethodDeclarationSyntax)interfaceDecl.Members[0];
            symbol = LookupTypeParameterFromConstraintClause(model, methodDecl.ConstraintClauses[0], "U1");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.None, "U2");
            symbol = LookupTypeParameterFromConstraintClause(model, methodDecl.ConstraintClauses[0], "U2");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.None);

            // Query for type parameters in reverse order.
            interfaceDecl = (InterfaceDeclarationSyntax)cu.Members[1];
            symbol = LookupTypeParameterFromConstraintClause(model, interfaceDecl.ConstraintClauses[0], "T4");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.None);
            symbol = LookupTypeParameterFromConstraintClause(model, interfaceDecl.ConstraintClauses[0], "T3");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.None, "T4");
            methodDecl = (MethodDeclarationSyntax)interfaceDecl.Members[0];
            symbol = LookupTypeParameterFromConstraintClause(model, methodDecl.ConstraintClauses[0], "U4");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.None);
            symbol = LookupTypeParameterFromConstraintClause(model, methodDecl.ConstraintClauses[0], "U3");
            CompilationUtils.CheckConstraints(symbol, TypeParameterConstraintKind.None, "U4");
        }

        private static ITypeParameterSymbol LookupTypeParameterFromConstraintClause(SemanticModel model, TypeParameterConstraintClauseSyntax constraintSyntax, string name)
        {
            var constraintStart = constraintSyntax.WhereKeyword.SpanStart;
            var symbols = model.LookupSymbols(constraintStart, null, name: name);
            Assert.Equal(1, symbols.Length);
            var symbol = symbols[0] as ITypeParameterSymbol;
            Assert.NotNull(symbol);
            return symbol;
        }

        [Fact]
        public void TestLookupSymbolsAllNames()
        {
            var compilation = CreateCompilation(@"
                class A
                {
                   public int X;
                }

                class B : A
                {
                   public int Y;
                }

                class C : B
                {
                   public int Z;

                   public void M()
                   {
                   }
                }
            ", targetFramework: TargetFramework.NetStandard20);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclC = (TypeDeclarationSyntax)cu.Members[2];
            var someMemberInC = (MemberDeclarationSyntax)typeDeclC.Members[0];

            // specify (name = null) returns symbols for all names in scope
            var symbols = model.LookupNamespacesAndTypes(someMemberInC.SpanStart);
            Assert.Equal(5, symbols.Length);  // A, B, C, System, Microsoft
        }

        [Fact]
        public void TestLookupSymbolsAllNamesMustBeInstance()
        {
            var compilation = CreateCompilation(@"
                class A
                {
                   public int X;
                   public static int SX;
                }

                class B : A
                {
                   public int Y() { };
                   public static int SY() { };
                }

                class C : B
                {
                   public int Z;
                }
            ");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclC = (TypeDeclarationSyntax)cu.Members[2];
            var symbolC = model.GetDeclaredSymbol(typeDeclC);
            var someMemberInC = (MemberDeclarationSyntax)typeDeclC.Members[0];
            int position = someMemberInC.SpanStart;

            var symbols = model.LookupSymbols(position).Where(s => !s.IsStatic && !((s is ITypeSymbol)));
            Assert.Equal(9, symbols.Count());  // A.X, B.Y, C.Z, Object.ToString, Object.Equals, Object.Finalize, Object.GetHashCode, Object.GetType, Object.MemberwiseClone

            var symbols2 = model.LookupSymbols(position, container: symbolC).Where(s => !s.IsStatic && !((s is ITypeSymbol)));
            Assert.Equal(9, symbols2.Count());  // A.X, B.Y, C.Z, Object.ToString, Object.Equals, Object.Finalize, Object.GetHashCode, Object.GetType, Object.MemberwiseClone
        }

        [Fact]
        public void TestLookupSymbolsAllNamesMustNotBeInstance()
        {
            var compilation = CreateCompilation(@"
                class A
                {
                   public int X;
                   public static int SX;
                }

                class B : A
                {
                   public int Y() { };
                   public static int SY() { };
                }

                class C : B
                {
                   public int Z;
                }
            ");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclC = (TypeDeclarationSyntax)cu.Members[2];
            var symbolC = model.GetDeclaredSymbol(typeDeclC);
            var someMemberInC = (MemberDeclarationSyntax)typeDeclC.Members[0];

            var symbols = model.LookupStaticMembers(someMemberInC.SpanStart, container: symbolC);
            Assert.Equal(4, symbols.Length);  // A.SX, B.SY, Object.Equals, Object.ReferenceEquals
        }

        [Fact]
        public void TestLookupSymbolsExtensionMethods()
        {
            var compilation = (Compilation)CreateCompilation(
@"namespace N
{
    class C
    {
        void M()
        {
            this.E(1);
            this.F();
            this.G<int>();
        }
        void F() { }
    }
    static class S1
    {
        internal static void E(this object o, int x) { }
        internal static void F(this C c) { }
        internal static void G<T, U>(this object o) { }
        internal static void H(this int i) { }
    }
}
static class S2
{
    internal static void E(this N.C c) { }
    internal static void F<T>(this T t) { }
    internal static void G<T>(this object o) { }
    internal static void H(this double d) { }
}");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var namespaceDecl = (NamespaceDeclarationSyntax)cu.Members[0];
            var typeDecl = (TypeDeclarationSyntax)namespaceDecl.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var statement = (ExpressionStatementSyntax)methodDecl.Body.Statements[0];
            var namespaceStart = namespaceDecl.OpenBraceToken.SpanStart;
            int typeDeclStart = typeDecl.OpenBraceToken.SpanStart;
            int statementStart = statement.SpanStart;

            var type = model.GetDeclaredSymbol(typeDecl);
            Assert.NotNull(type);

            Func<ISymbol, bool> isExtensionMethod = symbol =>
                symbol.Kind == SymbolKind.Method && (((IMethodSymbol)symbol).IsExtensionMethod || ((IMethodSymbol)symbol).MethodKind == MethodKind.ReducedExtension);

            // All extension methods available for specific type.
            var symbols = model.LookupSymbols(typeDeclStart, type, name: null, includeReducedExtensionMethods: true).WhereAsArray(isExtensionMethod);
            CheckSymbolsUnordered(symbols,
                "void object.E(int x)",
                "void C.F()",
                "void object.G<T, U>()",
                "void C.E()",
                "void C.F<C>()",
                "void object.G<T>()");

#if false
            // All extension methods of specific arity.
            symbols = model.LookupSymbols(typeDeclStart, type, name: null, arity: 0, includeReducedExtensionMethods: true);
            CheckSymbols(symbols,
                "bool object.Equals(object objA, object objB)",
                "bool object.ReferenceEquals(object objA, object objB)",
                "void object.E(int x)",
                "void C.F()",
                "void C.E()");
            symbols = model.LookupSymbols(typeDeclStart, type, name: null, arity: 1, includeReducedExtensionMethods: true);
            CheckSymbols(symbols,
                "void C.F<C>()",
                "void object.G<T>()");
            symbols = model.LookupSymbols(namespaceStart, type, name: null, arity: 2, includeReducedExtensionMethods: true);
            CheckSymbols(symbols,
                "void object.G<T, U>()");
#endif

            // All instance and extension methods of specific name.
            symbols = model.LookupSymbols(statementStart, type, "E", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols,
                "void object.E(int x)",
                "void C.E()");
            symbols = model.LookupSymbols(statementStart, type, "F", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols,
                "void C.F()",
                "void C.F()",
                "void C.F<C>()");
            symbols = model.LookupSymbols(statementStart, null, "F", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols,
                "void C.F()");

            // All extension methods for base type.
            var baseType = compilation.GetSpecialType(SpecialType.System_Object);
            symbols = model.LookupSymbols(namespaceStart, baseType, name: null, includeReducedExtensionMethods: true).WhereAsArray(isExtensionMethod);
            CheckSymbolsUnordered(symbols,
                "void object.E(int x)",
                "void object.G<T, U>()",
                "void object.F<object>()",
                "void object.G<T>()");

            // All extension methods of specific name for value type.
            var valueType = compilation.GetSpecialType(SpecialType.System_Int32);
            symbols = model.LookupSymbols(typeDeclStart, valueType, name: "E", includeReducedExtensionMethods: true).WhereAsArray(isExtensionMethod);
            CheckSymbolsUnordered(symbols,
                "void object.E(int x)");

            // Skip extension methods for which there are no "this" arg conversions.
            symbols = model.LookupSymbols(namespaceStart, valueType, name: "H", includeReducedExtensionMethods: true).WhereAsArray(isExtensionMethod);
            CheckSymbolsUnordered(symbols,
                "void int.H()");

            // All extension methods of unrecognized name.
            symbols = model.LookupSymbols(typeDeclStart, type, name: "C", includeReducedExtensionMethods: true).WhereAsArray(isExtensionMethod);
            CheckSymbolsUnordered(symbols);
        }

        /// <summary>
        /// LookupSymbols should return partially constructed
        /// methods for generic extension methods.
        /// </summary>
        [Fact]
        public void TestLookupSymbolsGenericExtensionMethods()
        {
            var compilation = (Compilation)CreateCompilation(
@"class C
{
    static void M() { }
}
static class S
{
    internal static void E<T>(this System.Collections.Generic.IEnumerable<T> t) { }
    internal static void E<T, U>(this U u, T t) { }
    internal static void E<T, U>(this object o, T t, U u) { }
}");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)cu.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var methodStart = methodDecl.Body.OpenBraceToken.SpanStart;

            // All extension method overloads regardless of type.
            var symbols = model.LookupSymbols(methodStart, null, name: "E", includeReducedExtensionMethods: true);
            CheckSymbols(symbols);

            // All extension method overloads for IList<string>.
            var type = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T);
            type = type.Construct(compilation.GetSpecialType(SpecialType.System_String));
            Assert.NotNull(type);
            symbols = model.LookupSymbols(methodStart, type, name: "E", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols,
                "void IEnumerable<string>.E<string>()",
                "void IList<string>.E<T, IList<string>>(T t)",
                "void object.E<T, U>(T t, U u)");

            // All extension method overloads for double.
            type = compilation.GetSpecialType(SpecialType.System_Double);
            Assert.NotNull(type);
            symbols = model.LookupSymbols(methodStart, type, name: "E", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols,
                "void double.E<T, double>(T t)",
                "void object.E<T, U>(T t, U u)");
        }

        [WorkItem(541125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541125")]
        [Fact]
        public void TestLookupSymbolsMoreGenericExtensionMethods()
        {
            var compilation = CreateCompilation(
@"using System;
class A<T> { }
class B<T> : A<T> { }
class C
{
    static void M(A<int> a, B<string> b) { }
}
static class S
{
    internal static void E0<T>() { }
    internal static void E1<T>(this B<T> b) { }
    internal static void E2<T>(this A<T> a, T t) { }
    internal static void E3<T>(this A<T> a, B<T> b, T t) { }
    internal static void E4<T>(this T t, A<T> a) { }
    internal static void E5<U, T>(this A<T> a, Func<T, U> f) { }
    internal static void E6<T, U>(this B<T> b, Func<T, U> f) { }
    internal static void E7<T>(this A<T> a, Action<A<T>> f) { }
    internal static void E8<T, U>(this B<string> b, Func<T, U> f) { }
}");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)cu.Members[2];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var methodStart = methodDecl.Body.OpenBraceToken.SpanStart;

            // Get types for A<int> and B<string>.
            var symbols = model.LookupSymbols(methodStart, null, name: "a");
            CheckSymbolsUnordered(symbols, "A<int> a");
            var typeA = ((IParameterSymbol)symbols[0]).Type;
            Assert.NotNull(typeA);
            symbols = model.LookupSymbols(methodStart, null, name: "b");
            CheckSymbolsUnordered(symbols, "B<string> b");
            var typeB = ((IParameterSymbol)symbols[0]).Type;
            Assert.NotNull(typeB);

            Func<ISymbol, bool> isExtensionMethod = symbol =>
                symbol.Kind == SymbolKind.Method && (((IMethodSymbol)symbol).IsExtensionMethod || ((IMethodSymbol)symbol).MethodKind == MethodKind.ReducedExtension);

            // Extension methods for B<string>
            symbols = model.LookupSymbols(methodStart, typeB, name: null, includeReducedExtensionMethods: true).WhereAsArray(isExtensionMethod);
            CheckSymbolsUnordered(symbols,
                "void B<string>.E1<string>()",
                "void A<string>.E2<string>(string t)",
                "void A<string>.E3<string>(B<string> b, string t)",
                "void B<string>.E4<B<string>>(A<B<string>> a)",
                "void A<string>.E5<U, string>(Func<string, U> f)",
                "void B<string>.E6<string, U>(Func<string, U> f)",
                "void A<string>.E7<string>(Action<A<string>> f)",
                "void B<string>.E8<T, U>(Func<T, U> f)");

            // Extension methods for A<int>
            symbols = model.LookupSymbols(methodStart, typeA, name: null, includeReducedExtensionMethods: true).WhereAsArray(isExtensionMethod);
            CheckSymbolsUnordered(symbols,
                "void A<int>.E2<int>(int t)",
                "void A<int>.E3<int>(B<int> b, int t)",
                "void A<int>.E4<A<int>>(A<A<int>> a)",
                "void A<int>.E5<U, int>(Func<int, U> f)",
                "void A<int>.E7<int>(Action<A<int>> f)");
        }

        [WorkItem(544933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544933")]
        [Fact]
        public void TestLookupSymbolsGenericExtensionMethodWithConstraints()
        {
            var source =
@"class A { }
class B { }
static class E
{
    static void M(A a, B b)
    {
        a.F();
        b.F();
    }
    internal static void F<T>(this T t) where T : A { }
}";
            var compilation = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (8,11): error CS0311: The type 'B' cannot be used as type parameter 'T' in the generic type or method 'E.F<T>(T)'. There is no implicit reference conversion from 'B' to 'A'.
                //         b.F();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "F").WithArguments("E.F<T>(T)", "A", "T", "B").WithLocation(8, 11)
);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var position = source.IndexOf("a.F()", StringComparison.Ordinal);
            var method = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("E").GetMember<IMethodSymbol>("M");

            // No type.
            var symbols = model.LookupSymbols(position, container: null, name: "F", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols, "void E.F<T>(T t)");

            // Type satisfying constraint.
            symbols = model.LookupSymbols(position, container: method.Parameters[0].Type, name: "F", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols, "void A.F<A>()");

            // Type not satisfying constraint.
            symbols = model.LookupSymbols(position, container: method.Parameters[1].Type, name: "F", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols);

            // Same tests as above but with position outside of
            // static class defining extension methods.
            source =
@"class A { }
class B { }
class C
{
    static void M(A a, B b)
    {
        a.F();
        b.F();
    }
}
static class E
{
    internal static void F<T>(this T t) where T : A { }
}";
            compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (8,11): error CS0311: The type 'B' cannot be used as type parameter 'T' in the generic type or method 'E.F<T>(T)'. There is no implicit reference conversion from 'B' to 'A'.
                //         b.F();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "F").WithArguments("E.F<T>(T)", "A", "T", "B").WithLocation(8, 11));

            tree = compilation.SyntaxTrees.Single();
            model = compilation.GetSemanticModel(tree);
            position = source.IndexOf("a.F()", StringComparison.Ordinal);
            method = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("M");

            // No type.
            symbols = model.LookupSymbols(position, container: null, name: "F", includeReducedExtensionMethods: true);
            CheckSymbols(symbols);

            // Type satisfying constraint.
            symbols = model.LookupSymbols(position, container: method.Parameters[0].Type, name: "F", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols, "void A.F<A>()");

            // Type not satisfying constraint.
            symbols = model.LookupSymbols(position, container: method.Parameters[1].Type, name: "F", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols);
        }

        [Fact]
        public void TestLookupSymbolsArrayExtensionMethods()
        {
            var compilation = CreateCompilation(
                source:
@"using System.Linq;
class C
{
    static void M(object[] o)
    {
    }
}");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)cu.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var methodStart = methodDecl.Body.OpenBraceToken.SpanStart;

            // Get type for object[].
            var symbols = model.LookupSymbols(methodStart, null, name: "o");
            CheckSymbolsUnordered(symbols, "object[] o");
            var type = ((IParameterSymbol)symbols[0]).Type;
            Assert.NotNull(type);

            // Extension method overloads for o.First.
            symbols = model.LookupSymbols(methodStart, type, name: "First", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols,
                "object IEnumerable<object>.First<object>()",
                "object IEnumerable<object>.First<object>(Func<object, bool> predicate)");
        }

        private static void CheckSymbols(ImmutableArray<ISymbol> symbols, params string[] descriptions)
        {
            CompilationUtils.CheckISymbols(symbols, descriptions);
        }

        private static void CheckSymbolsUnordered(ImmutableArray<ISymbol> symbols, params string[] descriptions)
        {
            CompilationUtils.CheckSymbolsUnordered(symbols, descriptions);
        }

        [Fact]
        public void TestLookupSymbolsWithEmptyStringForNameDoesNotAssert()
        {
            var compilation = CreateCompilation(@"
                class A
                {
                   public void M() 
                   { 
                      const
                   }
                }
            ");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclA = (TypeDeclarationSyntax)cu.Members[0];
            var methodM = (MethodDeclarationSyntax)typeDeclA.Members[0];
            var someStatementInM = methodM.Body.Statements[0];

            // check doesn't assert!
            var symbols = model.LookupNamespacesAndTypes(someStatementInM.SpanStart, name: "");
        }

        [Fact]
        public void TestLookupSymbolInCatch()
        {
            var text =
@"class C
{
    static void M()
    {
        try { }
        catch (System.Exception a) { }
    }
    static System.Action A = () =>
    {
        try { }
        catch (System.Exception b) { }
    };
}";
            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var position = text.IndexOf('{', text.IndexOf("a)", StringComparison.Ordinal));
            var symbols = model.LookupSymbols(position, name: "a");
            Assert.Equal(1, symbols.Length);
            position = text.IndexOf('{', text.IndexOf("b)", StringComparison.Ordinal));
            symbols = model.LookupSymbols(position, name: "b");
            Assert.Equal(1, symbols.Length);
        }

        [Fact]
        public void TestLookupSymbolAttributeType()
        {
            var text =
@"
using System;
using GooAttribute = System.ObsoleteAttribute;

namespace Blue
{
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace Red
{
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}


namespace Green
{
    using Blue;
    using Red;

    public class Test
    {
        [Description(null)]
        static void Main()
        {
        }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class X : Attribute {}

[AttributeUsage(AttributeTargets.All)]
public class XAttribute : Attribute {}

[X()]
class Class1 {}

namespace InvalidWithoutSuffix
{
    public class Y
    {
        public Y(string name) { }
    }
}

namespace ValidWithSuffix
{
    public class YAttribute : System.Attribute
    {
        public YAttribute(string name) { }
    }
}

namespace TestNamespace_01
{
    using ValidWithSuffix;
    using InvalidWithoutSuffix;

    [Y(null)]
    public class Test { }
}

[Goo()]
class Bar { }
";
            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            Func<int, INamespaceOrTypeSymbol, string, bool, ImmutableArray<ISymbol>> lookupAttributeTypeWithQualifier = (pos, qualifierOpt, name, isVerbatim) =>
            {
                var options = isVerbatim ? LookupOptions.VerbatimNameAttributeTypeOnly : LookupOptions.AttributeTypeOnly;

                var binder = ((CSharpSemanticModel)model).GetEnclosingBinder(pos);
                Assert.NotNull(binder);

                var lookupResult = LookupResult.GetInstance();
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                binder.LookupSymbolsSimpleName(
                    lookupResult,
                    ((CSharp.Symbols.PublicModel.NamespaceOrTypeSymbol)qualifierOpt)?.UnderlyingNamespaceOrTypeSymbol,
                    plainName: name, arity: 0, basesBeingResolved: null, options: options, diagnose: false, useSiteDiagnostics: ref useSiteDiagnostics);
                Assert.Null(useSiteDiagnostics);
                var result = lookupResult.IsMultiViable ? lookupResult.Symbols.ToImmutable() : ImmutableArray.Create<Symbol>();
                lookupResult.Free();
                return result.SelectAsArray(s => s.GetPublicSymbol());
            };

            Func<int, string, bool, ImmutableArray<ISymbol>> lookupAttributeType = (pos, name, isVerbatim) =>
                lookupAttributeTypeWithQualifier(pos, null, name, isVerbatim);

            var position = text.IndexOf("Description(null)", 0, StringComparison.Ordinal);
            var symbols = lookupAttributeType(position, "Description", false);
            Assert.Equal(2, symbols.Length);

            symbols = lookupAttributeType(position, "Description", true);
            Assert.Equal(0, symbols.Length);

            position = text.IndexOf("X()", 0, StringComparison.Ordinal);
            symbols = lookupAttributeType(position, "X", false);
            Assert.Equal(2, symbols.Length);

            symbols = lookupAttributeType(position, "X", true);
            Assert.Equal(1, symbols.Length);

            position = text.IndexOf("Y(null)", 0, StringComparison.Ordinal);
            symbols = lookupAttributeType(position, "Y", false);
            Assert.Equal(1, symbols.Length);

            symbols = lookupAttributeType(position, "Y", true);
            Assert.Equal(0, symbols.Length);

            var position2 = text.IndexOf("namespace InvalidWithoutSuffix", 0, StringComparison.Ordinal);
            var qnSymbols = model.LookupNamespacesAndTypes(position2, name: "InvalidWithoutSuffix");
            Assert.Equal(1, qnSymbols.Length);
            var qnInvalidWithoutSuffix = (INamespaceOrTypeSymbol)qnSymbols[0];

            symbols = model.LookupNamespacesAndTypes(position, name: "Y", container: qnInvalidWithoutSuffix);
            Assert.Equal(1, symbols.Length);

            symbols = lookupAttributeTypeWithQualifier(position, qnInvalidWithoutSuffix, "Y", false);
            Assert.Equal(0, symbols.Length);

            symbols = lookupAttributeTypeWithQualifier(position, qnInvalidWithoutSuffix, "Y", true);
            Assert.Equal(0, symbols.Length);

            position = text.IndexOf("Goo()", 0, StringComparison.Ordinal);
            symbols = lookupAttributeType(position, "Goo", false);
            Assert.Equal(1, symbols.Length);

            symbols = lookupAttributeType(position, "Goo", true);
            Assert.Equal(0, symbols.Length);
        }

        [Fact]
        public void TestGetSemanticInfoOfInvocation()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    string x = F();     
  }

  string F()
  {
    return ""Hello"";
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var invocation = (InvocationExpressionSyntax)localDecl.Declaration.Variables[0].Initializer.Value;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(invocation);
            Assert.NotNull(info);
            Assert.NotNull(info.Type);
            Assert.Equal("String", info.Type.Name);
            Assert.NotNull(info.Symbol);
            Assert.Equal("F", info.Symbol.Name);
        }

        [Fact]
        public void TestGetSemanticInfoOfInvocationWithNoMatchingOverloads()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    string x = F();     
  }

  string F(int x)
  {
    return ""Hello"";
  }

  string F(string x)
  {
    return x;
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var invocation = (InvocationExpressionSyntax)localDecl.Declaration.Variables[0].Initializer.Value;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(invocation);
            Assert.NotNull(info);
            Assert.NotNull(info.Type);
            Assert.Equal("String", info.Type.Name);
            Assert.Null(info.Symbol);
            Assert.Equal(2, info.CandidateSymbols.Length);
            Assert.Equal("F", info.CandidateSymbols[0].Name);
        }

        [Fact]
        public void TestGetSemanticInfoOfInvocationWithNoMatchingOverloadsAndNonMatchingReturnTypes()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    string x = F();     
  }

  string F(int x)
  {
    return ""Hello"";
  }

  int F(string x)
  {
    return 0;
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var invocation = (InvocationExpressionSyntax)localDecl.Declaration.Variables[0].Initializer.Value;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(invocation);
            Assert.NotNull(info);
            Assert.Equal(TypeKind.Error, info.Type.TypeKind);
            Assert.Null(info.Symbol);
            Assert.Equal(2, info.CandidateSymbols.Length);
            Assert.Equal("F", info.CandidateSymbols[0].Name);
        }

        [Fact]
        public void TestGetSemanticInfoOfIncompleteInvocation()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    string x = F(;
  }

  string F(int x)
  {
    return ""Hello"";
  }

  string F(string x)
  {
    return x;
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var invocation = (InvocationExpressionSyntax)localDecl.Declaration.Variables[0].Initializer.Value;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(invocation);
            Assert.NotNull(info);
            Assert.NotNull(info.Type);
            Assert.Null(info.Symbol);
            Assert.Equal(2, info.CandidateSymbols.Length);
            Assert.Equal("F", info.CandidateSymbols[0].Name);
        }

        [Fact]
        public void TestGetSemanticInfoOfMethodGroupAccess()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    string x = F();     
  }

  string F()
  { 
    return ""Hello"";
  }

  string F(int x)
  {
    return ""World"";
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var invocation = (InvocationExpressionSyntax)localDecl.Declaration.Variables[0].Initializer.Value;
            var methodGroup = invocation.Expression;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(methodGroup);
            Assert.NotNull(info);
            Assert.Null(info.Type);
            Assert.NotNull(info.Symbol);
        }

        [Fact]
        public void TestGetSemanticInfoOfTypeName()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    C x = F();     
  }

  C F()
  { 
    return this;
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var type = localDecl.Declaration.Type;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(type);
            Assert.NotNull(info);
            Assert.NotNull(info.Type);
            Assert.Equal("C", info.Type.Name);
            Assert.NotNull(info.Symbol);
        }

        [Fact]
        public void TestGetSemanticInfoOfTypeNameWithConflictingLocalName()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    int C = 10;
    C x = F();     
    int y = C;
  }

  C F()
  { 
    return this;
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[1];
            var type = localDecl.Declaration.Type;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(type);
            Assert.NotNull(info);
            Assert.NotNull(info.Type);
            Assert.Equal("C", info.Type.Name);
            Assert.NotNull(info.Symbol);

            // check that other references to 'C' in a non-type context bind to Int32
            localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[2];
            var init = localDecl.Declaration.Variables[0].Initializer.Value;
            info = model.GetSemanticInfoSummary(init);
            Assert.NotNull(info.Type);
            Assert.Equal("Int32", info.Type.Name);
        }

        [Fact]
        public void TestGetSemanticInfoOfNamespaceName()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    System.String x = F();
  }

  string F()
  { 
    return ""Hello"";
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var type = (QualifiedNameSyntax)localDecl.Declaration.Type;
            var ns = type.Left;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(ns);
            Assert.NotNull(info);
            Assert.Null(info.Type);
            Assert.NotNull(info.Symbol);
            Assert.Equal("System", info.Symbol.Name);
        }

        [Fact]
        public void TestGetSemanticInfoOfRightSideOfQualifiedName()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    N.D x = null;
  }
}

class D 
{
}

namespace N
{
  class D
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var type = (QualifiedNameSyntax)localDecl.Declaration.Type;
            var rightName = type.Right;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(rightName);
            Assert.NotNull(info.Type);
            // make sure that the model info for the name 'D' in this context tells us about the type 'N.D' not the type 'D'
            Assert.Equal("D", info.Type.Name);
            Assert.Equal("N", info.Type.ContainingNamespace.Name);
        }

        [Fact]
        public void TestGetSemanticInfoOfTypeInDeclaration()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    string x = F();   
  }

  string F()
  { 
    return ""Hello"";
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[1];
            var type = methodDecl.ReturnType;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(type);
            Assert.NotNull(info);
            Assert.NotNull(info.Type);
            Assert.Equal("String", info.Type.Name);
            Assert.NotNull(info.Symbol);
        }

        [Fact]
        public void TestGetSemanticInfoOfNamespaceInDeclaration()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    string x = F();
  }

  System.String F()
  { 
    return ""Hello"";
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[1];
            var type = (QualifiedNameSyntax)methodDecl.ReturnType;
            var ns = type.Left;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(ns);
            Assert.NotNull(info);
            Assert.Null(info.Type);
            Assert.NotNull(info.Symbol);
            Assert.Equal("System", info.Symbol.Name);
        }

        [Fact]
        public void TestGetSemanticInfoInParentInLocalInitializer()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    double x = 10;
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var initializer = localDecl.Declaration.Variables[0].Initializer.Value;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(initializer);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Double", info.ConvertedType.Name);
            Assert.Null(info.Symbol);
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [Fact]
        public void TestGetSemanticInfoInParentInMultipleLocalInitializers()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    double x = 10, y = 20;
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var model = compilation.GetSemanticModel(tree);

            var info = model.GetSemanticInfoSummary(localDecl.Declaration.Variables[0].Initializer.Value);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Double", info.ConvertedType.Name);

            info = model.GetSemanticInfoSummary(localDecl.Declaration.Variables[1].Initializer.Value);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Double", info.ConvertedType.Name);
        }

        [Fact]
        public void TestGetSemanticInfoInParentInArgument()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    F(10);
  }

  void F(double p)
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var exprStmt = (ExpressionStatementSyntax)methodDecl.Body.Statements[0];
            var invocation = (InvocationExpressionSyntax)exprStmt.Expression;
            var arg = invocation.ArgumentList.Arguments[0].Expression;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(arg);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Double", info.ConvertedType.Name);
        }

        [Fact]
        public void TestGetSemanticInfoInParentInMultipleArguments()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    F(10, 20);
  }

  void F(double p, long p2)
  {
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var exprStmt = (ExpressionStatementSyntax)methodDecl.Body.Statements[0];
            var invocation = (InvocationExpressionSyntax)exprStmt.Expression;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(invocation.ArgumentList.Arguments[0].Expression);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Double", info.ConvertedType.Name);

            info = model.GetSemanticInfoSummary(invocation.ArgumentList.Arguments[1].Expression);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Int64", info.ConvertedType.Name);
        }

        [Fact]
        public void UsingStaticClass()
        {
            string test = @"
public static class S1
{
    public static void goo(int a)
    {
    }
}

public static class S2
{
    public static void goo(string a)
    {
    }
}

namespace A
{
    using static S1;

    namespace B
    {
        using static S2;

        public static class Z
        {
            public static void M()
            {
                goo(""sss"");
                goo(1);
            }
        }
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6));

            var compilation = CSharpCompilation.Create(
                assemblyName: "Test",
                options: TestOptions.DebugExe.WithScriptClassName("Script"),
                syntaxTrees: new[] { tree },
                references: new[] { MscorlibRef });

            var expr = tree.FindNodeOrTokenByKind(SyntaxKind.StringLiteralToken).Parent.FirstAncestorOrSelf<ExpressionStatementSyntax>().Expression;

            var global = compilation.GlobalNamespace;
            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(expr);

            Assert.NotNull(info.Symbol);
        }

        [WorkItem(537932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537932")]
        [Fact]
        public void GetDeclaredSymbolDupAliasNameErr()
        {
            var compilation = (Compilation)CreateCompilation(@"
namespace NS {  class A {}  }

namespace NS {
    using System;
    using B = NS.A;

    class B {}
}
");

            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var n1 = globalNS.GetMembers("NS").First() as INamespaceSymbol;
            var typeB = n1.GetTypeMembers("B").First() as INamedTypeSymbol;
            Assert.Equal(2, root.Members.Count);
            var nsSyntax = (root.Members[1] as NamespaceDeclarationSyntax);
            Assert.Equal(1, nsSyntax.Members.Count);
            var classB = model.GetDeclaredSymbol(nsSyntax.Members[0] as TypeDeclarationSyntax);
            // Reference equal
            Assert.Equal(typeB, classB);
        }

        [WorkItem(537624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537624")]
        [Fact]
        public void GetDeclaredSymbolForUsingDirective()
        {
            var compilation = CreateCompilation(@"
namespace N1 {
};

namespace N2 {
  using N1;
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var n2 = (NamespaceDeclarationSyntax)root.Members[1];
            var u1 = (UsingDirectiveSyntax)n2.Usings[0];

            var info = model.GetSemanticInfoSummary(u1.Name);
            var n1 = info.Symbol;

            Assert.Equal("N1", n1.Name);
            Assert.Equal(SymbolKind.Namespace, n1.Kind);
        }

        [Fact]
        public void GetDeclaredSymbolForExplicitImplementations()
        {
            var compilation = CreateCompilation(@"
interface I
{
    void M();
    int P { get; set; }
}
class C : I
{
    void I.M() {}
    int I.P { get; set; }
}
");

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var root = tree.GetCompilationUnitRoot();

            var classNode = (TypeDeclarationSyntax)root.Members[1];
            Assert.Equal("C", classNode.Identifier.ValueText);

            var classMemberNodes = classNode.Members;

            var explicitMethodNode = (MethodDeclarationSyntax)classMemberNodes[0];
            Assert.Equal("M", explicitMethodNode.Identifier.ValueText);

            var explicitMethodSymbol = (IMethodSymbol)model.GetDeclaredSymbol(explicitMethodNode);
            Assert.NotNull(explicitMethodSymbol);
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, explicitMethodSymbol.MethodKind);
            Assert.Equal("I.M", explicitMethodSymbol.Name);
            Assert.Equal(1, explicitMethodSymbol.ExplicitInterfaceImplementations.Length);

            var explicitPropertyNode = (PropertyDeclarationSyntax)classMemberNodes[1];
            Assert.Equal("P", explicitPropertyNode.Identifier.ValueText);

            var explicitPropertySymbol = (IPropertySymbol)model.GetDeclaredSymbol(explicitPropertyNode);
            Assert.NotNull(explicitPropertySymbol);
            Assert.Equal("I.P", explicitPropertySymbol.Name);
            Assert.Equal(1, explicitPropertySymbol.ExplicitInterfaceImplementations.Length);

            var explicitPropertyAccessors = explicitPropertyNode.AccessorList.Accessors;
            Assert.Equal(2, explicitPropertyAccessors.Count);

            var explicitPropertyGetterNode = explicitPropertyAccessors[0];
            Assert.Equal("get", explicitPropertyGetterNode.Keyword.ValueText);

            var explicitPropertyGetterSymbol = (IMethodSymbol)model.GetDeclaredSymbol(explicitPropertyGetterNode);
            Assert.NotNull(explicitPropertyGetterSymbol);
            Assert.Equal(MethodKind.PropertyGet, explicitPropertyGetterSymbol.MethodKind);
            Assert.Equal("I.get_P", explicitPropertyGetterSymbol.Name);
            Assert.Equal(1, explicitPropertyGetterSymbol.ExplicitInterfaceImplementations.Length);
            Assert.Same(explicitPropertySymbol.GetMethod, explicitPropertyGetterSymbol);

            var explicitPropertySetterNode = explicitPropertyAccessors[1];
            Assert.Equal("set", explicitPropertySetterNode.Keyword.ValueText);

            var explicitPropertySetterSymbol = (IMethodSymbol)model.GetDeclaredSymbol(explicitPropertySetterNode);
            Assert.NotNull(explicitPropertySetterSymbol);
            Assert.Equal(MethodKind.PropertySet, explicitPropertySetterSymbol.MethodKind);
            Assert.Equal("I.set_P", explicitPropertySetterSymbol.Name);
            Assert.Equal(1, explicitPropertySetterSymbol.ExplicitInterfaceImplementations.Length);
            Assert.Same(explicitPropertySymbol.SetMethod, explicitPropertySetterSymbol);
        }

        [WorkItem(527284, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527284")]
        [Fact]
        public void GetDeclaredSymbolDottedNSAPI()
        {
            var compilation = (Compilation)CreateCompilation(@"
namespace N1 {
  namespace N2.N3
  {
    class C {}
  }
}
");

            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var n1 = globalNS.GetMembers().First() as INamespaceSymbol;
            var n2 = n1.GetMembers().First() as INamespaceSymbol;
            var n3 = n2.GetMembers().First() as INamespaceSymbol;

            var nsSyntax = (root.Members[0] as NamespaceDeclarationSyntax);
            var dn1 = model.GetDeclaredSymbol(nsSyntax);

            var nsSyntax23 = (nsSyntax.Members[0] as NamespaceDeclarationSyntax);
            var dn23 = model.GetDeclaredSymbol(nsSyntax23);

            // Reference equal
            Assert.Equal(n1, dn1);
            Assert.Equal(n3, dn23);
        }

        [WorkItem(527285, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527285")]
        [Fact]
        public void GetDeclaredSymbolGlobalSystemNSErr()
        {
            var compilation = (Compilation)CreateCompilation(@"
namespace global::System {}

class Test { }
");

            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var compSym = compilation.GlobalNamespace.GetMembers("System").First() as INamespaceSymbol;

            var nsSyntax = (root.Members[0] as NamespaceDeclarationSyntax);
            var declsym = model.GetDeclaredSymbol(nsSyntax);
            // Reference equal
            Assert.Equal(compSym, declsym);
        }

        [WorkItem(527286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527286")]
        [Fact]
        public void GetDeclaredSymbolInvalidOverloadsErr()
        {
            var compilation = (Compilation)CreateCompilation(@"
class CGoo
{
    void M() {}
    int M() { return 0; }

    interface IGoo {}
    struct SGoo
    {
        void M(byte p) {}
        void M(ref byte p) {}
        void M(out byte p) {}
    }
}
");

            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;

            var sym1 = globalNS.GetMembers("CGoo").First() as INamedTypeSymbol;
            var mems = sym1.GetMembers("M");
            var node1 = (root.Members[0] as TypeDeclarationSyntax);

            var dsyma1 = model.GetDeclaredSymbol(node1.Members[0] as MethodDeclarationSyntax);
            var dsyma2 = model.GetDeclaredSymbol(node1.Members[1]);
            // By Design - conflicting overloads bind to distinct symbols
            Assert.NotEqual(dsyma1, dsyma2);
            // for CC?
            var sym2 = sym1.GetMembers("IGoo").First() as INamedTypeSymbol;
            var node2 = (node1.Members[2] as TypeDeclarationSyntax);
            var dsym2 = model.GetDeclaredSymbol(node2);
            Assert.Equal(TypeKind.Interface, dsym2.TypeKind);

            var sym3 = sym1.GetMembers("SGoo").First() as INamedTypeSymbol;
            var node3 = (node1.Members[3] as TypeDeclarationSyntax);
            // CC?
            var dsym3 = model.GetDeclaredSymbol(node3);
            Assert.Equal(TypeKind.Struct, dsym3.TypeKind);

            var mems2 = sym3.GetMembers("M");

            Assert.Equal(3, mems2.Length);
            var dsymc1 = model.GetDeclaredSymbol(node3.Members[0] as MethodDeclarationSyntax);
            var dsymc2 = model.GetDeclaredSymbol(node3.Members[1] as MethodDeclarationSyntax);
            var dsymc3 = model.GetDeclaredSymbol(node3.Members[2] as MethodDeclarationSyntax);

            Assert.Same(mems2[0], dsymc1);
            Assert.Same(mems2[1], dsymc2);
            Assert.Same(mems2[2], dsymc3);
        }

        [WorkItem(537953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537953")]
        [Theory]
        [MemberData(nameof(FileScopedOrBracedNamespace))]
        public void GetDeclaredSymbolNoTypeSymbolWithErr(string ob, string cb)
        {
            var compilation = (Compilation)CreateCompilation(@"
namespace NS
" + ob + @"
  protected class A { }
" + cb + @"
");
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var ns1 = globalNS.GetMembers("NS").Single() as INamespaceSymbol;
            var srcSym = ns1.GetMembers("A").Single() as INamedTypeSymbol;

            var nsSyntax = (root.Members[0] as BaseNamespaceDeclarationSyntax);
            var declSym = model.GetDeclaredSymbol(nsSyntax.Members[0] as TypeDeclarationSyntax);

            Assert.Equal(srcSym, declSym);
        }

        [WorkItem(537954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537954")]
        [Fact]
        public void GetDeclaredSymbolExtraForDupTypesErr()
        {
            var compilation = CreateCompilation(@"
namespace NS
{
    static class Test { }

    // CS0101
    class Test { }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var ns1 = globalNS.GetMembers("NS").Single() as NamespaceSymbol;
            var mems = ns1.GetMembers("Test");
            Assert.Equal(1, mems.Length);

            var nsSyntax = (root.Members[0] as NamespaceDeclarationSyntax);
            var dsym1 = model.GetDeclaredSymbol(nsSyntax.Members[0] as TypeDeclarationSyntax);
            var dsym2 = model.GetDeclaredSymbol(nsSyntax.Members[1] as TypeDeclarationSyntax);

            Assert.Equal(dsym1, dsym2);
        }

        [WorkItem(537955, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537955")]
        [Fact]
        public void GetDeclaredSymbolSameNameMethodsDiffNSs()
        {
            var compilation = (Compilation)CreateCompilation(@"
namespace Goo {
    class A { }
}

namespace NS {
    using Goo;
    class A { }
}
");
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var ns1 = globalNS.GetMembers("NS").Single() as INamespaceSymbol;
            var typeA = ns1.GetTypeMembers("A").First() as INamedTypeSymbol;

            var nsSyntax = (root.Members[1] as NamespaceDeclarationSyntax);
            var dsym1 = model.GetDeclaredSymbol(nsSyntax.Members[0] as TypeDeclarationSyntax);

            Assert.Equal(typeA, dsym1);
        }

        [Fact]
        public void GetDeclaredSymbolNSCrossComps()
        {
            var comp1 = CreateCompilation(@"
namespace NS1 {
    namespace NS2 {    class A { }    }
    namespace NS2 {    class B { }    }
}
");
            var text2 = @"
namespace NS1 {
    namespace NS2 {    class C { }    }
}
";

            var ref1 = new CSharpCompilationReference(comp1);

            var comp = (Compilation)CSharpCompilation.Create(
                "Repro",
                new[] { SyntaxFactory.ParseSyntaxTree(text2) },
                new[] { MscorlibRef, ref1 });

            var tree = comp.SyntaxTrees.First();
            var root = tree.GetCompilationUnitRoot();
            var model = comp.GetSemanticModel(tree);

            var nsg = comp.GlobalNamespace;
            var ns1 = nsg.GetMembers("NS1").Single() as INamespaceSymbol;
            var ns2 = ns1.GetMembers("NS2").Single() as INamespaceSymbol;

            var nsSyntax1 = (root.Members[0] as NamespaceDeclarationSyntax);
            var nsSyntax2 = (nsSyntax1.Members[0] as NamespaceDeclarationSyntax);
            var dsym1 = model.GetDeclaredSymbol(nsSyntax1);
            var dsym2 = model.GetDeclaredSymbol(nsSyntax2);

            Assert.Equal(ns1, dsym1);
            Assert.Equal(ns2, dsym2);
        }

        [WorkItem(538953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538953")]
        [Fact]
        public void GetDeclaredSymbolAccessorErrs1()
        {
            var text1 =
@"public sealed class ErrorProp
{
    public string Prop1 { get { return null; } protected } // CS1007
    public int Prop2 { protected get { return 1; } set { } protected get { return 1; } } // CS1014
}
";

            var compilation = (Compilation)CreateCompilation(text1);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var typeA = globalNS.GetTypeMembers("ErrorProp").First() as INamedTypeSymbol;

            var prop = typeA.GetMembers("Prop1").FirstOrDefault() as IPropertySymbol;
            var synType = root.Members[0] as TypeDeclarationSyntax;
            var accessors = (synType.Members[0] as PropertyDeclarationSyntax).AccessorList;
            var dsym = model.GetDeclaredSymbol(accessors.Accessors[0]);
            Assert.Equal(prop.GetMethod, dsym);
            dsym = model.GetDeclaredSymbol(accessors.Accessors[1]);
            Assert.Null(dsym);

            prop = typeA.GetMembers("Prop2").FirstOrDefault() as IPropertySymbol;
            accessors = (synType.Members[1] as PropertyDeclarationSyntax).AccessorList;
            dsym = model.GetDeclaredSymbol(accessors.Accessors[0]);
            Assert.Equal(prop.GetMethod, dsym);
            dsym = model.GetDeclaredSymbol(accessors.Accessors[1]);
            Assert.Equal(prop.SetMethod, dsym);
            dsym = model.GetDeclaredSymbol(accessors.Accessors[2]);
            Assert.Null(dsym);
        }

        [WorkItem(538953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538953")]
        [Fact]
        public void GetDeclaredSymbolAccessorErrs2()
        {
            var text =
@"
public sealed class ErrorProp
{
    public string Prop1 { goo { return null; } } // invalid accessor name
}
";
            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var accessorDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();
            Assert.Equal(SyntaxKind.UnknownAccessorDeclaration, accessorDecl.Kind());
            Assert.Null(model.GetDeclaredSymbol(accessorDecl));
        }

        [WorkItem(538148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538148")]
        [Fact]
        public void TestOverloadsInImplementedInterfaceMethods()
        {
            var compilation = CreateCompilation(@"
class WorksheetClass : IWorksheet
{
    public int M1()
    {
        return 0;
    }
}
interface IWorksheet
{
    int M1();
}

public class MainClass
{
    public static void Main ()
    {
        WorksheetClass w = new WorksheetClass();
        IWorksheet iw = w;
        
        int i = w.M1() + iw.M1();
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[2]).Members[0];
            var declStmt = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[2];
            var expr = (ExpressionSyntax)declStmt.Declaration.Variables[0].Initializer.Value;

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(expr);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Int32", info.ConvertedType.Name);
        }

        [Fact]
        public void TestGetSemanticInfoBrokenDecl()
        {
            var compilation = (Compilation)CreateCompilation(@"
class C 
{
  void F()
  {
    strin g;
  }
}
");
            var tree = compilation.SyntaxTrees.First();
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];

            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(localDecl.Declaration.Type);
            Assert.NotNull(info);
            Assert.NotNull(info.Type);
            Assert.Equal("strin", info.Type.Name);
            Assert.Equal(compilation.Assembly.GlobalNamespace, info.Type.ContainingSymbol); //error type resides in global namespace
        }

        [Fact]
        public void TestGetSemanticInfoMethodGroupConversion()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M()
  {
    D1 x = N;
    P(N);
  }
  delegate void D1(int q);
  delegate void D2();
  static void N() {}
  static void N(int x) {}
  static void P(D2 d){}
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];

            // The info for "N" in the initializer should be the specific method, N(int). 

            var localDecl = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var initializer = localDecl.Declaration.Variables[0].Initializer.Value;

            var initInfo = model.GetSemanticInfoSummary(initializer);
            Assert.Null(initInfo.Type);
            Assert.NotNull(initInfo.ConvertedType);
            Assert.Equal("D1", initInfo.ConvertedType.Name);
            Assert.NotNull(initInfo.Symbol);
            Assert.Equal("C.N(int)", initInfo.Symbol.ToDisplayString());

            // Similarly for P(N) -- N is a method, not a method group.

            var expressionStmt = (ExpressionStatementSyntax)methodDecl.Body.Statements[1];
            var invocation = (InvocationExpressionSyntax)expressionStmt.Expression;
            var argument = invocation.ArgumentList.Arguments[0].Expression;

            var argInfo = model.GetSemanticInfoSummary(argument);
            Assert.NotNull(argInfo.ConvertedType);
            Assert.Equal("D2", argInfo.ConvertedType.Name);
            Assert.Null(argInfo.Type);
            Assert.NotNull(argInfo.Symbol);
            Assert.Equal("C.N()", argInfo.Symbol.ToDisplayString());
        }

        // named and optional parameters
        [WorkItem(539346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539346")]
        [WorkItem(540792, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540792")]
        [Fact]
        public void TestGetDeclaredSymbolForParamInLambdaExprPrecededByExplicitKeyword()
        {
            var compilation = CreateCompilation(@"
using System;
class Program
{
    static void Main(string[] args)
    {
        explicit
        Func<int, int> f1 = (param1) => 10;
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();

            // Get the parameter node from the SyntaxTree for the lambda parameter "param1"
            var paramNode = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("param1", StringComparison.Ordinal)).Parent;

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(paramNode);

            Assert.NotNull(symbol);
        }

        //named and optional parameters
        [WorkItem(539346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539346")]
        [WorkItem(540792, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540792")]
        [Fact]
        public void TestGetDeclaredSymbolForLambdaInDefaultValue1()
        {
            var compilation = CreateCompilation(@"
using System;
class Program
{
    void Goo(Func<int, int> f = x => 1)
    {
    }
}
");
            var tree = compilation.SyntaxTrees[0];

            // Get the parameter node from the SyntaxTree for the lambda parameter "param1"
            var root = tree.GetCompilationUnitRoot();
            var paramNode = root.FindToken(root.ToFullString().IndexOf('x')).Parent;

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(paramNode);

            Assert.NotNull(symbol);
        }

        [WorkItem(540792, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540792")]
        [WorkItem(539346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539346")]
        [Fact]
        public void TestGetDeclaredSymbolForLambdaInDefaultValue2()
        {
            var compilation = CreateCompilation(@"
using System;
class Program
{
    void Goo(Func<int, Func<int, int>> f = w => x => 1)
    {
    }
}
");
            var tree = compilation.SyntaxTrees[0];

            // Get the parameter node from the SyntaxTree for the lambda parameter "param1"
            var root = tree.GetCompilationUnitRoot();
            var paramNode = root.FindToken(root.ToFullString().IndexOf('x')).Parent;

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(paramNode);

            Assert.NotNull(symbol);
        }

        [WorkItem(540834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540834")]
        [Fact]
        public void TestGetDeclaredSymbolForIncompleteMemberNode()
        {
            var compilation = CreateCompilation(@"private");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();

            // Get the IncompleteMemberNode which is the first child node of the root of the tree.
            var node = root.ChildNodes().First();

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(node);

            Assert.Equal(SyntaxKind.IncompleteMember, node.Kind());
            Assert.Null(symbol);
        }

        [WorkItem(7358, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestGetDeclaredSymbolForeachStmtWithPointerType()
        {
            var compilation = CreateCompilation(@"
class Test
{
    static void Main(string[] args)
    {
        foreach (var x in new int*
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();

            // Get the foreach syntax node from the SyntaxTree
            var foreachNode = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("foreach", StringComparison.Ordinal)).Parent;

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(foreachNode);

            Assert.NotNull(symbol);
        }

        [WorkItem(541057, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541057")]
        [Fact]
        public void TestGetDeclaredSymbolConstDelegateDecl()
        {
            var compilation = CreateCompilation(@"
public class Test
{
    const delegate
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();

            // Get the delegate declaration syntax node from the SyntaxTree
            var delegateNode = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("delegate", StringComparison.Ordinal)).Parent;

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(delegateNode);

            Assert.NotNull(symbol);
        }

        [WorkItem(541084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541084")]
        [Fact]
        public void TestIncompleteUsingDirectiveSyntax()
        {
            var compilation = CreateCompilation(@"
using myType1 =
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();

            // Get the using directive syntax node from the SyntaxTree
            var usingNode = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("using", StringComparison.Ordinal)).Parent;

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(usingNode);

            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.Alias, symbol.Kind);
        }

        [WorkItem(541225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541225")]
        [Fact]
        public void TestGetDeclaredSymbolForeachStmt()
        {
            var compilation = CreateCompilation(@"
class C
{
    static void Main(string[] args)
    {
        int x;
        foreach (var aaa in args)
        {
            int z;
        }
    }
}
");
            var tree = compilation.SyntaxTrees[0];

            // Get the foreach syntax node from the SyntaxTree
            var foreachNode = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("aaa", StringComparison.Ordinal)).Parent;
            Assert.Equal(SyntaxKind.ForEachStatement, foreachNode.Kind());

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(foreachNode);
            Assert.Equal("aaa", symbol.Name);
        }

        [WorkItem(541225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541225")]
        [Fact]
        public void TestGetDeclaredSymbolForeachStmtError1()
        {
            var compilation = CreateCompilation(@"
class C
{
    static void Main(string[] args)
    {
        int x;
        foreach (var aaa in args)
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            // Get the foreach syntax node from the SyntaxTree
            var foreachNode = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("aaa", StringComparison.Ordinal)).Parent;
            Assert.Equal(SyntaxKind.ForEachStatement, foreachNode.Kind());

            var symbol = model.GetDeclaredSymbol(foreachNode);
            Assert.Equal("aaa", symbol.Name);
        }

        [WorkItem(541225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541225")]
        [Fact]
        public void TestGetDeclaredSymbolForeachStmtError2()
        {
            var compilation = CreateCompilation(@"
class C
{
    static void Main(string[] args)
    {
        int x;
        foreach (var aaa in args)
            foreach (var bbb in args)

namespace N
{
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var foreachNode1 = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("aaa", StringComparison.Ordinal)).Parent;
            Assert.Equal(SyntaxKind.ForEachStatement, foreachNode1.Kind());

            var symbol1 = model.GetDeclaredSymbol(foreachNode1);
            Assert.Equal("aaa", symbol1.Name);

            var foreachNode2 = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("bbb", StringComparison.Ordinal)).Parent;
            Assert.Equal(SyntaxKind.ForEachStatement, foreachNode2.Kind());

            var symbol2 = model.GetDeclaredSymbol(foreachNode2);
            Assert.Equal("bbb", symbol2.Name);
        }

        [WorkItem(541225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541225")]
        [Fact]
        public void TestGetDeclaredSymbolCatchClause()
        {
            var compilation = CreateCompilation(@"
class C
{
    static void Main(string[] args)
    {
        int x;
        try
        {
            int y;
        }
        catch (System.Exception aaa)
        {
            int z;
        }
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var catchDeclaration = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("aaa", StringComparison.Ordinal)).Parent;
            Assert.Equal(SyntaxKind.CatchDeclaration, catchDeclaration.Kind());

            var symbol = model.GetDeclaredSymbol(catchDeclaration);
            Assert.Equal("aaa", symbol.Name);
        }

        [WorkItem(541214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541214")]
        [Fact]
        public void TestGetDeclaredSymbolTopLevelMethod()
        {
            var compilation = CreateCompilation(@"
using System;
class void Goo()
{
    int x;
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var methodDecl = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("Goo", StringComparison.Ordinal)).Parent;
            Assert.Equal(SyntaxKind.LocalFunctionStatement, methodDecl.Kind());

            var symbol = model.GetDeclaredSymbol(methodDecl);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            Assert.Equal("Goo", symbol.Name);
        }

        [WorkItem(541214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541214")]
        [Fact]
        public void TestGetDeclaredSymbolNamespaceLevelMethod()
        {
            var compilation = CreateCompilation(@"
using System;
namespace N
{
    class void Goo()
    {
        int x;
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var methodDecl = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("Goo", StringComparison.Ordinal)).Parent;
            Assert.Equal(SyntaxKind.MethodDeclaration, methodDecl.Kind());

            var symbol = model.GetDeclaredSymbol(methodDecl);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            Assert.Equal("Goo", symbol.Name);
        }

        [WorkItem(543238, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543238")]
        [Fact]
        public void TestGetDeclaredSymbolEnumMemberDeclarationSyntax()
        {
            var compilation = CreateCompilation(@"
using System;
enum EnumX
{
    FieldM = 0
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var enumDecl = (EnumDeclarationSyntax)cu.Members[0];
            MemberDeclarationSyntax enumMemberDecl = enumDecl.Members[0];
            Assert.Equal(SyntaxKind.EnumMemberDeclaration, enumMemberDecl.Kind());

            var enumTypeSymbol = model.GetDeclaredSymbol(enumDecl);
            Assert.Equal(SymbolKind.NamedType, enumTypeSymbol.Kind);
            Assert.Equal("EnumX", enumTypeSymbol.Name);

            var symbol = model.GetDeclaredSymbol(enumMemberDecl);
            Assert.Equal(SymbolKind.Field, symbol.Kind);
            Assert.Equal("FieldM", symbol.Name);
            Assert.Equal(enumTypeSymbol, symbol.ContainingType);

            var fSymbol = model.GetDeclaredSymbol((EnumMemberDeclarationSyntax)enumMemberDecl);
            Assert.Equal("FieldM", fSymbol.Name);
            Assert.Equal(enumTypeSymbol, fSymbol.ContainingType);
        }

        [Fact]
        public void TestLambdaParameterInLambda()
        {
            var compilation = CreateCompilation(@"
using System;
delegate int D(int x);
class Program
{
    public static void Main(string[] args)
    {
        D d = (int x) => x;
        Console.WriteLine(d(3));
    }
}");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            dynamic methodDecl = (MethodDeclarationSyntax)tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("Main", StringComparison.Ordinal)).Parent;
            IdentifierNameSyntax x = methodDecl.Body.Statements[0].Declaration.Variables[0].Initializer.Value.Body;
            var info = model.GetSemanticInfoSummary(x);
            Assert.Equal(SymbolKind.Parameter, info.Symbol.Kind);
            var parameter = (IParameterSymbol)info.Symbol;
            Assert.Equal("x", parameter.Name);
            Assert.Equal(SpecialType.System_Int32, parameter.Type.SpecialType);
        }

        [WorkItem(541800, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541800")]
        [Fact]
        public void GetDeclaredSymbolOnGlobalStmtParseOptionScript()
        {
            var parseOptions = TestOptions.Script;
            var compilation = CreateCompilation(@"/", parseOptions: parseOptions);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var globalStmt = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf('/')).Parent.AncestorsAndSelf().Single(x => x.IsKind(SyntaxKind.GlobalStatement));

            var symbol = model.GetDeclaredSymbol(globalStmt);

            Assert.Null(symbol);
        }

        [WorkItem(542102, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542102")]
        [Fact]
        public void GetSymbolInGoto()
        {
            var compilation = CreateCompilation(@"
class Program
{
    static void Main()
    {
    Goo:
        int Goo;
        goto Goo;
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var gotoStatement = (GotoStatementSyntax)methodDecl.Body.Statements[1];
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetSemanticInfoSummary(gotoStatement.Expression).Symbol;
            Assert.Equal(SymbolKind.Label, symbol.Kind);
        }

        [WorkItem(542342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542342")]
        [Fact]
        public void SourceNamespaceSymbolMergeWithMetadata()
        {
            var compilation = (Compilation)CreateEmptyCompilation(new string[] {
@"namespace System {
    public partial class PartialClass 
    {
        public int Prop { get; set; }
    }
}",
@"namespace System
{
    public partial class PartialClass 
    {
        public int this[int i] { get { return i; } set {} }
    }
}"},
new[] { MscorlibRef });

            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetCompilationUnitRoot();
            var decl = (NamespaceDeclarationSyntax)root.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var declSymbol = model.GetDeclaredSymbol(decl);
            Assert.NotNull(declSymbol);
            Assert.Equal("System", declSymbol.Name);
            Assert.Equal(3, declSymbol.Locations.Length);
            Assert.IsType<MergedNamespaceSymbol>(declSymbol.GetSymbol());
            Assert.Equal(NamespaceKind.Compilation, declSymbol.NamespaceKind);
            Assert.Equal(2, declSymbol.ConstituentNamespaces.Length);

            var tree2 = compilation.SyntaxTrees.ElementAt(1);
            var root2 = tree.GetCompilationUnitRoot();
            var decl2 = (NamespaceDeclarationSyntax)root2.Members[0];
            var model2 = compilation.GetSemanticModel(tree2);
            var declSymbol2 = model.GetDeclaredSymbol(decl2);
            Assert.NotNull(declSymbol2);
            Assert.Equal(declSymbol, declSymbol2);
            // 
            var memSymbol = compilation.GlobalNamespace.GetMembers("System").Single();
            Assert.Same(declSymbol, memSymbol);
        }

        [WorkItem(542459, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542459")]
        [Fact]
        public void StructKeywordInsideSwitchWithScriptParseOption()
        {
            var compilation = CreateCompilation(@"
using System;

class Test
{
    public static void Main()
    {
        int i = 10;
        switch (i)
        {
            class case 0:
                break;
        }
        switch (i)
        {
            struct default:
                break;
        }
    }
}
", parseOptions: TestOptions.Script
 );

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var diagnostics = model.GetDiagnostics();

            Assert.NotEmpty(diagnostics);
        }

        [WorkItem(542459, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542459")]
        [Fact]
        public void Bug9728_SmallerReproCase()
        {
            var code = @"
using System;
struct break;
";

            var compilation = CreateCompilation(code, parseOptions: TestOptions.Script);

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var diagnostics = model.GetDiagnostics();

            Assert.NotEmpty(diagnostics);
        }

        [WorkItem(542483, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542483")]
        [Fact]
        public void IncompleteStructDeclWithSpace()
        {
            var compilation = CreateCompilation(@"
using System;

namespace N1
{
    struct Test
 ");

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var diagnostics = model.GetDiagnostics();

            Assert.NotEmpty(diagnostics);
        }

        [WorkItem(542583, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542583")]
        [Fact]
        public void LambdaExpressionInFieldInitReferencingAnotherFieldWithScriptParseOption()
        {
            string sourceCode = @"
using System.Linq;
using System.Collections;
 
class P
{
    double one = 1;
    public Func<int, int> z = (x => x + one);
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(sourceCode, parseOptions: TestOptions.Script);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            var queryExpr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParenthesizedExpressionSyntax>().First();
            var symbolInfo = semanticModel.GetSemanticInfoSummary(queryExpr);
            Assert.Equal(SymbolKind.Method, symbolInfo.Symbol.Kind);
        }

        [WorkItem(542495, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542495")]
        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void AliasSymbolEquality()
        {
            string text = @"
using Alias = System;
                               
class C
{
    private Alias.String s;
}
";
            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees[0];
            var node = (IdentifierNameSyntax)tree.GetCompilationUnitRoot().DescendantTokens().Where(t => t.ToString() == "Alias").Last().Parent;
            var modelWeakReference = ObjectReference.CreateFromFactory(() => compilation.GetSemanticModel(tree));
            var alias1 = modelWeakReference.UseReference(sm => sm.GetAliasInfo(node));

            // We want the Compilation's WeakReference<BinderFactory> to be collected
            // so that the next semantic model will get a new one.
            modelWeakReference.AssertReleased();

            var model2 = compilation.GetSemanticModel(tree);
            var alias2 = model2.GetAliasInfo(node);

            Assert.Equal(alias1, alias2);
            Assert.Same(alias1, alias2);
        }

        [WorkItem(542475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542475")]
        [Fact]
        public void PartialMethods()
        {
            string sourceCode = @" using System;
partial class program
{
    static void Main(string[] args)
    {
        //goo(gender: 1 > 2, name: "", age: 1);
    }
    static partial void goo(string name, int age, bool gender, int index1 = 1) { }
}
partial class program
{
    static partial void goo(string name, int age, bool gender, int index1 = 1);
}";
            var tree = Parse(sourceCode);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var param = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().Where(p => p.Identifier.ValueText == "name").First();
            var symbol = model.GetDeclaredSymbol(param);
            Assert.Equal(param.Identifier.Span, symbol.Locations[0].SourceSpan);
        }

        [WorkItem(542217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542217")]
        [Fact]
        public void ConflictingAliases()
        {
            string sourceCode = @"
using S = System;
using S = System.IO;

class C
{
    static void Main() { }
}
";
            var tree = Parse(sourceCode);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var usingDirectives = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();

            Assert.Equal(2, usingDirectives.Length);

            var alias1 = model.GetDeclaredSymbol(usingDirectives[0]);
            Assert.NotNull(alias1);
            Assert.Equal("System", alias1.Target.ToDisplayString());

            var alias2 = model.GetDeclaredSymbol(usingDirectives[1]);
            Assert.NotNull(alias2);
            Assert.Equal("System.IO", alias2.Target.ToDisplayString());

            Assert.NotEqual(alias1.Locations.Single(), alias2.Locations.Single());

            // This symbol we re-use.
            var alias1b = model.GetDeclaredSymbol(usingDirectives[0]);
            Assert.Same(alias1, alias1b);

            // This symbol we generate on-demand.
            var alias2b = model.GetDeclaredSymbol(usingDirectives[1]);
            Assert.Same(alias2, alias2b);
            Assert.Equal(alias2, alias2b);
        }

        [WorkItem(542902, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542902")]
        [Fact]
        public void InaccessibleDefaultAttributeConstructor()
        {
            var c1 = CreateCompilation(@"
using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal class MyAttribute : Attribute { 
    internal MyAttribute() { }
}
");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
[My]
public class X { }
");

            var c2 = CreateCompilation(tree2, references: new[] { new CSharpCompilationReference(c1) });

            var attr = (AttributeSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)tree2.GetCompilationUnitRoot()).Members[0]).AttributeLists[0].Attributes[0];
            var model = c2.GetSemanticModel(tree2);

            var symbolInfo = model.GetSymbolInfo(attr);
            Assert.Equal(CandidateReason.NotAnAttributeType, symbolInfo.CandidateReason);
        }

        [WorkItem(543024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543024")]
        [Fact]
        public void BindUnboundGenericType()
        {
            var source = @"
public class A<T>
{
    public class B<U>
    {
        void Goo(object o) 
        {
            Goo(typeof(T));
        }
    }
}
";

            var compilation = (Compilation)CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var typeA = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("A");
            var typeB = typeA.GetMember<INamedTypeSymbol>("B");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var typeofSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Single();
            var typeofArgSyntax = typeofSyntax.Type;
            var typeofArgPosition = typeofArgSyntax.SpanStart;

            ITypeSymbol boundType;
            SymbolInfo symbolInfo;

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<T>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as ITypeSymbol;
            Assert.Equal(typeA, boundType);
            Assert.Equal(typeA, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<int>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotEqual(typeA, boundType);
            Assert.Equal(typeA, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotEqual(typeA, boundType);
            Assert.Equal(typeA, boundType.OriginalDefinition);
            Assert.True(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("B<U>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as ITypeSymbol;
            Assert.Equal(typeB, boundType);
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("B<int>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotEqual(typeB, boundType);
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("B<>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotEqual(typeB, boundType);
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.True(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<>.B<>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotEqual(typeB, boundType);
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.True(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<>.B<U>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotEqual(typeB, boundType); // unbound generic type not constructed since illegal
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<T>.B<>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotEqual(typeB, boundType); // unbound generic type not constructed since illegal
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<T>.B<U>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as ITypeSymbol;
            Assert.Equal(typeB, boundType);
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());
        }

        [Fact]
        public void BindAsExpressionVsBindAsType()
        {
            var source = @"
using System;
using B = System.Console;

class M {
    public int B;
    void M() {
        Console.WriteLine(""hi"");
    }
}
";
            var compilation = (Compilation)CreateCompilation(source);

            var classM = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("M");
            var fieldB = classM.GetMember<IFieldSymbol>("B");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position1 = tree.GetText().ToString().IndexOf("WriteLine", StringComparison.Ordinal);
            var expression = SyntaxFactory.ParseExpression("B");

            var semanticInfoExpression = model.GetSpeculativeSemanticInfoSummary(position1, expression, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(fieldB, semanticInfoExpression.Symbol);
            Assert.Equal("System.Int32", semanticInfoExpression.Type.ToTestDisplayString());
            Assert.Null(semanticInfoExpression.Alias);

            semanticInfoExpression = model.GetSpeculativeSemanticInfoSummary(position1, expression, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal("System.Console", semanticInfoExpression.Symbol.ToTestDisplayString());
            Assert.Equal("System.Console", semanticInfoExpression.Type.ToTestDisplayString());
            Assert.NotNull(semanticInfoExpression.Alias);
            Assert.Equal("B=System.Console", semanticInfoExpression.Alias.ToTestDisplayString());
        }

        // Parse an attribute. No such API, so use a bit of a workaround.
        private AttributeSyntax ParseAttributeSyntax(string source)
        {
            return SyntaxFactory.ParseCompilationUnit(source + " class X {}").Members.First().AsTypeDeclarationSyntax().AttributeLists.First().Attributes.First();
        }

        [WorkItem(653957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/653957")]
        [Fact]
        public void MissingExtensionMethodNullDereference()
        {
            // This test fails during type argument inference
            var src = @"
static class S
{
    public static void Write<T>(this IWriter<T> writer, T value)
    {
        writer.Write(value);
    }
}";
            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var call = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            SymbolInfo info = new SymbolInfo();
            info = model.GetSymbolInfo(call);

            Assert.IsAssignableFrom<SourceOrdinaryMethodSymbol>(info.Symbol.GetSymbol());

            src = @"
static class S
{
    public static void Write(this IWriter writer, int value)
    {
        writer.Write(value);
    }
}";
            comp = CreateCompilation(src);
            tree = comp.SyntaxTrees.Single();
            model = comp.GetSemanticModel(tree);

            call = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            info = model.GetSymbolInfo(call);

            Assert.IsType<ReducedExtensionMethodSymbol>(info.Symbol.GetSymbol());
        }

        [Fact]
        public void BindSpeculativeAttribute()
        {
            var source = @"
using System;
using O=System.ObsoleteAttribute;

class C {
    class DAttribute: Attribute {}
    C goo<O>() { return null; }
    [Serializable] int i;
}
";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position = tree.GetText().ToString().IndexOf("class C {", StringComparison.Ordinal);
            var attr1 = ParseAttributeSyntax("[Obsolete]");

            var symbolInfo = model.GetSpeculativeSymbolInfo(position, attr1);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString());

            var attr2 = ParseAttributeSyntax("[ObsoleteAttribute(4)]");

            symbolInfo = model.GetSpeculativeSymbolInfo(position, attr2);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(3, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor()", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());

            var attr3 = ParseAttributeSyntax(@"[O(""hello"")]");

            symbolInfo = model.GetSpeculativeSymbolInfo(position, attr3);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());

            var attr4 = ParseAttributeSyntax("[P]");

            symbolInfo = model.GetSpeculativeSymbolInfo(position, attr4);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

            var attr5 = ParseAttributeSyntax("[D]");

            symbolInfo = model.GetSpeculativeSymbolInfo(position, attr5);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("C.DAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString());

            var attr6 = ParseAttributeSyntax(@"[O(""hello"")]");
            var position2 = tree.GetText().ToString().IndexOf("C goo<O>", StringComparison.Ordinal);

            symbolInfo = model.GetSpeculativeSymbolInfo(position2, attr6);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());

            var attr7 = ParseAttributeSyntax(@"[O(""hello"")]");
            var position3 = tree.GetText().ToString().IndexOf("Serializable", StringComparison.Ordinal);

            symbolInfo = model.GetSpeculativeSymbolInfo(position3, attr7);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForAttribute()
        {
            var source = @"
using System;
using O=System.ObsoleteAttribute;

class C {
    class DAttribute: Attribute {}
    C goo<O>() { return null; }
    [Serializable] int i;
}
";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees.Single();
            var parentModel = compilation.GetSemanticModel(tree);

            var position = tree.GetText().ToString().IndexOf("class C {", StringComparison.Ordinal);

            var attr1 = ParseAttributeSyntax("[Obsolete]");

            SemanticModel speculativeModel;
            var success = parentModel.TryGetSpeculativeSemanticModel(position, attr1, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var symbolInfo = speculativeModel.GetSymbolInfo(attr1);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString());

            var attr2 = ParseAttributeSyntax("[ObsoleteAttribute(4)]");
            success = parentModel.TryGetSpeculativeSemanticModel(position, attr2, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr2);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(3, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor()", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());

            var constantInfo = speculativeModel.GetConstantValue(attr2.ArgumentList.Arguments.First().Expression);
            Assert.True(constantInfo.HasValue, "must be constant");
            Assert.Equal(4, constantInfo.Value);

            var attr3 = ParseAttributeSyntax(@"[O(""hello"")]");

            success = parentModel.TryGetSpeculativeSemanticModel(position, attr3, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);
            symbolInfo = speculativeModel.GetSymbolInfo(attr3);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());

            constantInfo = speculativeModel.GetConstantValue(attr3.ArgumentList.Arguments.First().Expression);
            Assert.True(constantInfo.HasValue, "must be constant");
            Assert.Equal("hello", constantInfo.Value);

            var aliasSymbol = speculativeModel.GetAliasInfo(attr3.Name as IdentifierNameSyntax);
            Assert.NotNull(aliasSymbol);
            Assert.Equal("O", aliasSymbol.Name);
            Assert.NotNull(aliasSymbol.Target);
            Assert.Equal("ObsoleteAttribute", aliasSymbol.Target.Name);

            var attr4 = ParseAttributeSyntax("[P]");

            success = parentModel.TryGetSpeculativeSemanticModel(position, attr4, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr4);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

            var attr5 = ParseAttributeSyntax("[D]");

            success = parentModel.TryGetSpeculativeSemanticModel(position, attr5, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr5);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("C.DAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString());

            var attr6 = ParseAttributeSyntax(@"[O(""hello"")]");
            var position2 = tree.GetText().ToString().IndexOf("C goo<O>", StringComparison.Ordinal);

            success = parentModel.TryGetSpeculativeSemanticModel(position, attr6, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr6);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());

            constantInfo = speculativeModel.GetConstantValue(attr6.ArgumentList.Arguments.First().Expression);
            Assert.True(constantInfo.HasValue, "must be constant");
            Assert.Equal("hello", constantInfo.Value);

            aliasSymbol = speculativeModel.GetAliasInfo(attr6.Name as IdentifierNameSyntax);
            Assert.NotNull(aliasSymbol);
            Assert.Equal("O", aliasSymbol.Name);
            Assert.NotNull(aliasSymbol.Target);
            Assert.Equal("ObsoleteAttribute", aliasSymbol.Target.Name);

            var attr7 = ParseAttributeSyntax(@"[O(""hello"")]");
            var position3 = tree.GetText().ToString().IndexOf("Serializable", StringComparison.Ordinal);

            success = parentModel.TryGetSpeculativeSemanticModel(position3, attr7, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr7);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());

            constantInfo = speculativeModel.GetConstantValue(attr7.ArgumentList.Arguments.First().Expression);
            Assert.True(constantInfo.HasValue, "must be constant");
            Assert.Equal("hello", constantInfo.Value);

            aliasSymbol = speculativeModel.GetAliasInfo(attr7.Name as IdentifierNameSyntax);
            Assert.NotNull(aliasSymbol);
            Assert.Equal("O", aliasSymbol.Name);
            Assert.NotNull(aliasSymbol.Target);
            Assert.Equal("ObsoleteAttribute", aliasSymbol.Target.Name);

            var attr8 = SyntaxFactory.ParseCompilationUnit(@"[assembly: O(""hello"")]").AttributeLists.First().Attributes.First();

            success = parentModel.TryGetSpeculativeSemanticModel(position3, attr8, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr8);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());

            constantInfo = speculativeModel.GetConstantValue(attr8.ArgumentList.Arguments.First().Expression);
            Assert.True(constantInfo.HasValue, "must be constant");
            Assert.Equal("hello", constantInfo.Value);

            aliasSymbol = speculativeModel.GetAliasInfo(attr8.Name as IdentifierNameSyntax);
            Assert.NotNull(aliasSymbol);
            Assert.Equal("O", aliasSymbol.Name);
            Assert.NotNull(aliasSymbol.Target);
            Assert.Equal("ObsoleteAttribute", aliasSymbol.Target.Name);
        }

        [Fact]
        public void GetSymbolInfoSimpleLambda()
        {
            var compilation = CreateCompilation(@"
using System.Collections.Generic;
using System.Linq;
class Goo
{
  public string[] P { get; set; }
  public IEnumerable<Goo> Arguments { get; set; }

    static void M(IEnumerable<Goo> c)
    {
        var q = from e in c
                    select new Goo()
                    {
                        P = e.Arguments.Select(x => x)
                    };
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var lambda = (SimpleLambdaExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().First(x => x is SimpleLambdaExpressionSyntax);
            model.GetSymbolInfo(lambda);
        }

        [Fact]
        public void ImplicitConversionOperatorDeclaration()
        {
            var source = @"
class C
{
    public static implicit operator C(string s)
    {
        return null;
    }
}
";

            var compilation = (Compilation)CreateCompilation(source);

            var conversion = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>(WellKnownMemberNames.ImplicitConversionName);
            Assert.Equal(MethodKind.Conversion, conversion.MethodKind);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var conversionDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConversionOperatorDeclarationSyntax>().Single();

            var declaredSymbol = model.GetDeclaredSymbol(conversionDecl);
            Assert.NotNull(declaredSymbol);
            Assert.Equal(conversion, declaredSymbol);

            var lookupSymbols = model.LookupSymbols(conversionDecl.DescendantNodes().OfType<ReturnStatementSyntax>().Single().SpanStart, name: WellKnownMemberNames.ImplicitConversionName);
            Assert.Equal(declaredSymbol, lookupSymbols.Single()); //conversions can't be referenced by name, but the user asked for it specifically
        }

        [Fact]
        public void ExplicitConversionOperatorDeclaration()
        {
            var source = @"
class C
{
    public static explicit operator C(string s)
    {
        return null;
    }
}
";

            var compilation = (Compilation)CreateCompilation(source);

            var conversion = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>(WellKnownMemberNames.ExplicitConversionName);
            Assert.Equal(MethodKind.Conversion, conversion.MethodKind);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var conversionDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConversionOperatorDeclarationSyntax>().Single();

            var declaredSymbol = model.GetDeclaredSymbol(conversionDecl);
            Assert.NotNull(declaredSymbol);
            Assert.Equal(conversion, declaredSymbol);

            var lookupSymbols = model.LookupSymbols(conversionDecl.DescendantNodes().OfType<ReturnStatementSyntax>().Single().SpanStart, name: WellKnownMemberNames.ExplicitConversionName);
            Assert.Equal(declaredSymbol, lookupSymbols.Single()); //conversions can't be referenced by name, but the user asked for it specifically
        }

        [Fact]
        public void OperatorDeclaration()
        {
            var source = @"
class C
{
    public static C operator+(C c1, C c2)
    {
        return c1 ?? c2;
    }
}
";

            var compilation = (Compilation)CreateCompilation(source);

            var @operator = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>(WellKnownMemberNames.AdditionOperatorName);
            Assert.Equal(MethodKind.UserDefinedOperator, @operator.MethodKind);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var operatorDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<OperatorDeclarationSyntax>().Single();

            var declaredSymbol = model.GetDeclaredSymbol(operatorDecl);
            Assert.NotNull(declaredSymbol);
            Assert.Equal(@operator, declaredSymbol);

            var lookupSymbols = model.LookupSymbols(operatorDecl.DescendantNodes().OfType<ReturnStatementSyntax>().Single().SpanStart, name: WellKnownMemberNames.AdditionOperatorName);
            Assert.Equal(declaredSymbol, lookupSymbols.Single()); //operators can't be referenced by name, but the user asked for it specifically
        }

        [WorkItem(543415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543415")]
        [Fact]
        public void AliasRace1()
        {
            var text = @"
using Alias = Goo;

namespace Goo { }

[System.Obsolete]
class C { }
";

            var compilation = (Compilation)CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using Alias = Goo;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Alias = Goo;"));

            var @namespace = compilation.GlobalNamespace.GetMember<INamespaceSymbol>("Goo");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            int position = text.IndexOf("Obsolete", StringComparison.Ordinal);

            var result = Parallel.For(0, 100, i =>
            {
                var symbols = model.LookupSymbols(position, name: "Alias");
                var alias = (IAliasSymbol)symbols.Single();

                Assert.Equal(@namespace, alias.Target);
            });

            Assert.True(result.IsCompleted);
        }

        [WorkItem(543415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543415")]
        [Fact]
        public void AliasRace2()
        {
            var text = @"
using Alias = Goo;

namespace Goo { }

[System.Obsolete]
class C { }
";

            var compilation = (Compilation)CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using Alias = Goo;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Alias = Goo;"));

            var @namespace = compilation.GlobalNamespace.GetMember<INamespaceSymbol>("Goo");

            var tree = compilation.SyntaxTrees.Single();

            int position = text.IndexOf("Obsolete", StringComparison.Ordinal);

            var result = Parallel.For(0, 100, i =>
            {
                var model = compilation.GetSemanticModel(tree);
                var symbols = model.LookupSymbols(position, name: "Alias");
                var alias = (IAliasSymbol)symbols.Single();

                Assert.Equal(@namespace, alias.Target);
            });

            Assert.True(result.IsCompleted);
        }

        [WorkItem(544100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544100")]
        [Fact]
        public void NoLocalScopeBinder()
        {
            var text = @"
using S=System;
class C 
{ 
        void M(S.F x = default(S.F))
        {
        }
}

";

            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var node = (DefaultExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Where(i => i is DefaultExpressionSyntax).First();
            model.GetSemanticInfoSummary(node.Type);
        }

        [WorkItem(543868, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543868")]
        [Fact]
        public void IsEventUsableAsField()
        {
            var text = @"
class Enclosing
{
    void M()
    {
        //AAA
    }

    class Declaring
    {
        public event System.Action E;
        public event System.Action F { add { } remove { } }
        
        void M()
        {
            //BBB
        }

        class Nested
        {
            void M()
            {
                //CCC
            }
        }
    }
}

class Other
{
    void M()
    {
        //DDD
    }
}

";

            var compilation = (Compilation)CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            compilation.VerifyDiagnostics(
                // (11,36): warning CS0067: The event 'Enclosing.Declaring.E' is never used
                //         public event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("Enclosing.Declaring.E"));

            var declaringType = compilation.GlobalNamespace.GetMember<ITypeSymbol>("Enclosing").GetMember<ITypeSymbol>("Declaring");
            var fieldLikeEvent = declaringType.GetMember<IEventSymbol>("E");
            var customEvent = declaringType.GetMember<IEventSymbol>("F");

            int enclosingTypePosition = text.IndexOf("AAA", StringComparison.Ordinal);
            Assert.InRange(enclosingTypePosition, 0, text.Length);
            int declaringTypePosition = text.IndexOf("BBB", StringComparison.Ordinal);
            Assert.InRange(declaringTypePosition, 0, text.Length);
            int nestedTypePosition = text.IndexOf("CCC", StringComparison.Ordinal);
            Assert.InRange(nestedTypePosition, 0, text.Length);
            int otherTypePosition = text.IndexOf("DDD", StringComparison.Ordinal);
            Assert.InRange(otherTypePosition, 0, text.Length);

            Assert.False(model.IsEventUsableAsField(enclosingTypePosition, fieldLikeEvent));
            Assert.True(model.IsEventUsableAsField(declaringTypePosition, fieldLikeEvent));
            Assert.True(model.IsEventUsableAsField(nestedTypePosition, fieldLikeEvent));
            Assert.False(model.IsEventUsableAsField(otherTypePosition, fieldLikeEvent));

            Assert.False(model.IsEventUsableAsField(enclosingTypePosition, customEvent));
            Assert.False(model.IsEventUsableAsField(declaringTypePosition, customEvent));
            Assert.False(model.IsEventUsableAsField(nestedTypePosition, customEvent));
            Assert.False(model.IsEventUsableAsField(otherTypePosition, customEvent));
        }

        [Fact]
        public void UnboundNestedType()
        {
            var source =
@"class A<T> { }
class C : A<object>.B<> { }";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ClassDeclarationSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.ClassDeclaration));
            var model = compilation.GetSemanticModel(tree);
            var type = (INamedTypeSymbol)model.GetDeclaredSymbol(decl);
            type = type.BaseType;
            Assert.Equal("A<object>.B<?>", type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }

        [Fact]
        public void UnboundNestedType_2()
        {
            var source =
@"class A<T, U> { }
class C : A<,,>.B<object> { }";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ClassDeclarationSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.ClassDeclaration));
            var model = compilation.GetSemanticModel(tree);
            var type = (INamedTypeSymbol)model.GetDeclaredSymbol(decl);
            type = type.BaseType;
            Assert.Equal("A<?, ?, ?>.B<object>", type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }

        [Fact]
        public void UnboundNestedType_3()
        {
            var source =
@"class A { }
class C : A<>.B<> { }";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ClassDeclarationSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.ClassDeclaration));
            var model = compilation.GetSemanticModel(tree);
            var type = (INamedTypeSymbol)model.GetDeclaredSymbol(decl);
            type = type.BaseType;
            Assert.Equal("A<?>.B<?>", type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }

        [WorkItem(563572, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/563572")]
        [Fact]
        public void InvalidEnumDeclaration()
        {
            var source = @"
public class C
{
    public event enum
}
";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var enumDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<EnumDeclarationSyntax>().Single();
            var eventDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<EventDeclarationSyntax>().Single();

            var model = compilation.GetSemanticModel(tree);

            var enumSymbol = model.GetDeclaredSymbol(enumDecl); //Used to assert.
            Assert.Equal(SymbolKind.NamedType, enumSymbol.Kind);
            Assert.Equal(TypeKind.Enum, enumSymbol.TypeKind);

            var eventSymbol = model.GetDeclaredSymbol(eventDecl);
            Assert.Equal(SymbolKind.Event, eventSymbol.Kind);
        }

        [WorkItem(563572, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/563572")]
        [Fact]
        public void TypeMembersWithoutNames()
        {
            var source = @"
public class S
{
    struct interface
}
";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var structDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<StructDeclarationSyntax>().First();
            var interfaceDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Last();

            var model = compilation.GetSemanticModel(tree);

            var structSymbol = model.GetDeclaredSymbol(structDecl);
            var interfaceSymbol = model.GetDeclaredSymbol(interfaceDecl);

            // The missing identifier of the struct declaration is contained in both declaration spans (since it has width zero).
            // We used to just pick the first matching span, but now we keep looking until we find a "good" match.
            Assert.NotEqual(structSymbol, interfaceSymbol);
            Assert.Equal(TypeKind.Struct, structSymbol.TypeKind);
            Assert.Equal(TypeKind.Interface, interfaceSymbol.TypeKind);
        }

        [Fact]
        public void TupleLiteral001()
        {
            var source = @"
class C
{
    static void Main() 
    { 
        var t = (1, 2);
    }
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (TupleExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.TupleExpression));
            var model = compilation.GetSemanticModel(tree);
            var type = (INamedTypeSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(int, int)", type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("(1, 2)", type.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(type.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteral002()
        {
            var source = @"
class C
{
    static void Main() 
    { 
        var t = (Alice: 1, Bob: 2);
    }
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (TupleExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.TupleExpression));
            var model = compilation.GetSemanticModel(tree);
            var type = (INamedTypeSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(int Alice, int Bob)", type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("(Alice: 1, Bob: 2)", type.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(type.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteral003()
        {
            var source = @"
class C
{
    static void Main() 
    { 
        (short Alice, int Bob) t = (1, 1);
    }
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (TupleExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.TupleExpression));
            var model = compilation.GetSemanticModel(tree);
            var type = (INamedTypeSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(short, int)", type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("(1, 1)", type.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(type.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteral004()
        {
            var source = @"
class C
{
    static void Main() 
    { 
        (short Alice, string Bob) t = (1, null);
    }
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (TupleExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.TupleExpression));
            var model = compilation.GetSemanticModel(tree);
            var type = (INamedTypeSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(short, string)", type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("(1, null)", type.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(type.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteral005()
        {
            var source = @"
class C
{
    static void Main() 
    { 
        (short, string) t = (Alice:1, Bob:null);
    }
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (TupleExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.TupleExpression));
            var model = compilation.GetSemanticModel(tree);
            var type = (INamedTypeSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(short Alice, string Bob)", type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("(Alice:1, Bob:null)", type.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(type.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteralElement001()
        {
            var source = @"
class C
{
    static void Main() 
    { 
        var t = (Alice: 1, Bob: 2);
    }
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ArgumentSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.Argument));
            var model = compilation.GetSemanticModel(tree);
            var element = (IFieldSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(int Alice, int Bob).Bob", element.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("Bob", element.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(element.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteralElement002()
        {
            var source = @"
class C
{
    static void Main() 
    { 
        (int X, short Y) t = (Alice: 1, Bob: 2);
    }
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ArgumentSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.Argument));
            var model = compilation.GetSemanticModel(tree);
            var element = (IFieldSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(int Alice, short Bob).Bob", element.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("Bob", element.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(element.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteralElement003()
        {
            var source = @"
class C
{
    static void Main() 
    { 
        (short X, string Y) t = (Alice: 1, Bob: null);
    }
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ArgumentSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.Argument));
            var model = compilation.GetSemanticModel(tree);
            var element = (IFieldSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(short Alice, string Bob).Bob", element.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("Bob", element.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(element.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteralElement004_WithoutValueTuple()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (short X, string Y) = (Alice: 1, Bob: null);
    }
}
";

            var compilation = CreateCompilationWithMscorlib46(source);
            compilation.VerifyDiagnostics(
                // (6,31): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (short X, string Y) = (Alice: 1, Bob: null);
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(Alice: 1, Bob: null)").WithArguments("System.ValueTuple`2").WithLocation(6, 31),
                // (6,32): warning CS8123: The tuple element name 'Alice' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         (short X, string Y) = (Alice: 1, Bob: null);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "Alice: 1").WithArguments("Alice", "(short, string)").WithLocation(6, 32),
                // (6,42): warning CS8123: The tuple element name 'Bob' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         (short X, string Y) = (Alice: 1, Bob: null);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "Bob: null").WithArguments("Bob", "(short, string)").WithLocation(6, 42)
                );
            var tree = compilation.SyntaxTrees[0];
            var decl = (ArgumentSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.Argument));
            var model = compilation.GetSemanticModel(tree);
            var element = (IFieldSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(short Alice, string Bob).Bob", element.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("Bob", element.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(element.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteralElement004()
        {
            var source =
@"
class C
{
    static void Main()
    {
        (short X, string Y) = (Alice: 1, Bob: null);
    }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,32): warning CS8123: The tuple element name 'Alice' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         (short X, string Y) = (Alice: 1, Bob: null);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "Alice: 1").WithArguments("Alice", "(short, string)").WithLocation(6, 32),
                // (6,42): warning CS8123: The tuple element name 'Bob' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         (short X, string Y) = (Alice: 1, Bob: null);
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "Bob: null").WithArguments("Bob", "(short, string)").WithLocation(6, 42)
                );
            var tree = compilation.SyntaxTrees[0];
            var decl = (ArgumentSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.Argument));
            var model = compilation.GetSemanticModel(tree);
            var element = (IFieldSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(short Alice, string Bob).Bob", element.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("Bob", element.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(element.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteralElement005()
        {
            var source =
@"

using System;

class C
{
    static void Main() 
    { 
        ValueTuple<short, string> vt = (Alice: 1, Bob: null);
    }
}

namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }
    }
}

";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ArgumentSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.Argument));
            var model = compilation.GetSemanticModel(tree);
            var element = (IFieldSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(short Alice, string Bob).Bob", element.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("Bob", element.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(element.Locations.Single().IsInSource);
        }

        [Fact]
        public void TupleLiteralElement006()
        {
            var source = @"
class C
{
    static void Main() 
    { 
        (short X, string) t = (1, Bob: null);
    }
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ArgumentSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.Argument));
            var model = compilation.GetSemanticModel(tree);
            var element = (IFieldSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal("(short, string Bob).Bob", element.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            Assert.Equal("Bob", element.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.True(element.Locations.Single().IsInSource);
        }

        [Fact]
        public void TestIncompleteMemberNode_Visitor()
        {
            var compilation = CreateCompilation(@"private");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();

            // Get the IncompleteMemberNode which is the first child node of the root of the tree.
            var node = root.ChildNodes().First();

            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(node);
            Assert.Equal(SyntaxKind.IncompleteMember, node.Kind());

            var x = tree.FindNodeOrTokenByKind(SyntaxKind.IncompleteMember);
            Assert.Equal(SyntaxKind.IncompleteMember, x.Kind());
            Assert.Equal("C#", x.Language);
            Assert.Equal(7, x.Width);

            // This will call the Visitor Pattern Methods via the syntaxwalker
            var collector = new IncompleteSyntaxWalker();
            collector.Visit(root);
            int counter = collector.Incompletes.Count;
            Assert.Equal(1, counter);
        }

        private class IncompleteSyntaxWalker : CSharpSyntaxWalker
        {
            public readonly List<IncompleteMemberSyntax> Incompletes = new List<IncompleteMemberSyntax>();

            public override void VisitIncompleteMember(IncompleteMemberSyntax node)
            {
                this.Incompletes.Add(node);
                base.VisitIncompleteMember(node);
            }
        }

        [WorkItem(38074, "https://github.com/dotnet/roslyn/issues/38074")]
        [Fact]
        public void TestLookupStaticMembersLocalFunction()
        {
            var compilation = CreateCompilation(@"
class C
{
    static void M()
    {
        void Local() {}
    }
}
");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclC = (TypeDeclarationSyntax)cu.Members.Single();
            var methodDeclM = (MethodDeclarationSyntax)typeDeclC.Members.Single();

            var symbols = model.LookupStaticMembers(methodDeclM.Body.SpanStart);

            Assert.Contains(symbols, s => s.Name == "Local");
        }

        [Fact]
        public void TestLookupStaticMembers_PositionNeedsAdjustment()
        {
            var source = @"
#nullable enable

class Program
{
    static void Main(string[] args)
    {
        void local1() { }

        b

        local1();

    }
}
";
            var comp = CreateCompilation(source);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().Single(node => node is IdentifierNameSyntax { Identifier: { ValueText: "b" } });
            var symbols = model.LookupStaticMembers(node.SpanStart);
            Assert.Contains(symbols, s => s.Name == "local1");
        }

        [Fact]
        public void InvalidParameterWithDefaultValue_Method()
        {
            var source =
@"class Program
{
    static void F(int x = 2, = 3) { }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().ToArray();
            var symbol1 = VerifyParameter(model, decls[0], 0, "[System.Int32 x = 2]", "System.Int32", 2);
            var symbol2 = VerifyParameter(model, decls[1], 1, "[? = null]", "System.Int32", 3);
            Assert.Same(symbol1.ContainingSymbol, symbol2.ContainingSymbol);
        }

        [Fact]
        [WorkItem(784401, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/784401")]
        public void InvalidParameterWithDefaultValue_LocalFunction_01()
        {
            var source =
@"class Program
{
    static void Main()
    {
        void F(int x, = 3) { }
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().ToArray();
            var symbol1 = VerifyParameter(model, decls[0], 0, "System.Int32 x", null, null);
            var symbol2 = VerifyParameter(model, decls[1], 1, "[? = null]", "System.Int32", 3);
            Assert.Same(symbol1.ContainingSymbol, symbol2.ContainingSymbol);
        }

        [Fact]
        [WorkItem(784401, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/784401")]
        public void InvalidParameterWithDefaultValue_LocalFunction_02()
        {
            var source =
@"class Program
{
    static void Main()
    {
        void F(int x = 2, = 3) { }
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().ToArray();
            var symbol1 = VerifyParameter(model, decls[0], 0, "[System.Int32 x = 2]", "System.Int32", 2);
            var symbol2 = VerifyParameter(model, decls[1], 1, "[? = null]", "System.Int32", 3);
            Assert.Same(symbol1.ContainingSymbol, symbol2.ContainingSymbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74348")]
        public void ObjectInitializerIncompleteMemberValueAssignment01()
        {
            var source = """
                public class Thing
                {
                    public int Key { get; set; }
                    public string? Value { get; set; }
                }

                public class Using
                {
                    public static Thing CreateThing(int key, string? value)
                    {
                        return new()
                        {
                            Key = key,
                            Value,
                        };
                    }
                }
                """;
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
                // (4,18): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public string? Value { get; set; }
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(4, 18),
                // (9,52): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public static Thing CreateThing(int key, string? value)
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(9, 52),
                // (14,13): error CS0747: Invalid initializer member declarator
                //             Value,
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Value").WithLocation(14, 13)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var propertyDecls = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First()
                .ChildNodes()
                .OfType<PropertyDeclarationSyntax>()
                .ToArray();
            var valuePropertyDecl = propertyDecls[1];
            var valueProperty = model.GetDeclaredSymbol(valuePropertyDecl);
            Assert.NotNull(valueProperty);

            var initializers = root.DescendantNodes()
                .OfType<InitializerExpressionSyntax>()
                .First()
                .ChildNodes()
                .ToArray();
            var initializedSymbol = model.GetSymbolInfo(initializers[1]).Symbol;

            Assert.NotNull(initializedSymbol);
            Assert.True(initializedSymbol.Equals(valueProperty, SymbolEqualityComparer.Default));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74348")]
        public void ObjectInitializerIncompleteMemberValueAssignment02()
        {
            var source = """
                public class Outer
                {
                    public Thing Thing { get; } = new();
                }

                public class Thing
                {
                    public int Key { get; set; }
                    public string? Value { get; set; }
                }

                public class Using
                {
                    public static Outer CreateOuterThing(int key, string? value)
                    {
                        return new()
                        {
                            Thing =
                            {
                                Key = key,
                                Value,
                            }
                        };
                    }
                }
                """;
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
                // (9,18): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public string? Value { get; set; }
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(9, 18),
                // (14,57): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public static Outer CreateOuterThing(int key, string? value)
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(14, 57),
                // (21,17): error CS0747: Invalid initializer member declarator
                //                 Value,
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Value").WithLocation(21, 17)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var propertyDecls = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(s => s.Identifier.Text is "Thing")
                .ChildNodes()
                .OfType<PropertyDeclarationSyntax>()
                .ToArray();
            var valuePropertyDecl = propertyDecls[1];
            var valueProperty = model.GetDeclaredSymbol(valuePropertyDecl);
            Assert.NotNull(valueProperty);

            var thingInitializer = root.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .First(s => s.Left is IdentifierNameSyntax { Identifier.Text: "Thing" })
                .Right
                as InitializerExpressionSyntax;
            var valueInitializer = thingInitializer.Expressions[1];
            var initializedSymbol = model.GetSymbolInfo(valueInitializer).Symbol;

            Assert.NotNull(initializedSymbol);
            Assert.True(initializedSymbol.Equals(valueProperty, SymbolEqualityComparer.Default));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74348")]
        public void ObjectInitializerIncompleteMemberValueAssignment03()
        {
            var source = """
                public class Thing
                {
                    public int Key;
                    public string? Value;
                }

                public class Using
                {
                    public static Thing CreateThing(int key, string? value)
                    {
                        return new()
                        {
                            Key = key,
                            Value,
                        };
                    }
                }
                """;
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
                // (4,18): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public string? Value;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(4, 18),
                // (9,52): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public static Thing CreateThing(int key, string? value)
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(9, 52),
                // (14,13): error CS0747: Invalid initializer member declarator
                //             Value,
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Value").WithLocation(14, 13)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var valueFieldDecl = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First()
                .ChildNodes()
                .OfType<FieldDeclarationSyntax>()
                .ElementAt(1)
                .Declaration.Variables.First();
            var valueField = model.GetDeclaredSymbol(valueFieldDecl);
            Assert.NotNull(valueField);

            var initializers = root.DescendantNodes()
                .OfType<InitializerExpressionSyntax>()
                .First()
                .ChildNodes()
                .ToArray();
            var initializedSymbol = model.GetSymbolInfo(initializers[1]).Symbol;

            Assert.NotNull(initializedSymbol);
            Assert.True(initializedSymbol.Equals(valueField, SymbolEqualityComparer.Default));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74348")]
        public void ObjectInitializerIncompleteMemberValueAssignment04()
        {
            var source = """
                public class Thing
                {
                    public int Key;
                    public event Action? Handler;
                }

                public class Using
                {
                    public static Thing CreateThing(int key, Action? handler)
                    {
                        return new()
                        {
                            Key = key,
                            Handler,
                        };
                    }
                }
                """;
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
                // (4,18): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                //     public event Action? Handler;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Action").WithArguments("Action").WithLocation(4, 18),
                // (4,26): error CS0066: 'Thing.Handler': event must be of a delegate type
                //     public event Action? Handler;
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "Handler").WithArguments("Thing.Handler").WithLocation(4, 26),
                // (4,26): warning CS0067: The event 'Thing.Handler' is never used
                //     public event Action? Handler;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Handler").WithArguments("Thing.Handler").WithLocation(4, 26),
                // (9,46): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                //     public static Thing CreateThing(int key, Action? handler)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Action").WithArguments("Action").WithLocation(9, 46),
                // (14,13): error CS0747: Invalid initializer member declarator
                //             Handler,
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Handler").WithLocation(14, 13),
                // (14,13): error CS0070: The event 'Thing.Handler' can only appear on the left hand side of += or -= (except when used from within the type 'Thing')
                //             Handler,
                Diagnostic(ErrorCode.ERR_BadEventUsage, "Handler").WithArguments("Thing.Handler", "Thing").WithLocation(14, 13)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();

            var initializers = root.DescendantNodes()
                .OfType<InitializerExpressionSyntax>()
                .First()
                .ChildNodes()
                .ToArray();
            var initializedSymbol = model.GetSymbolInfo(initializers[1]).Symbol;

            Assert.Null(initializedSymbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74348")]
        public void ObjectInitializerIncompleteMemberValueAssignment05()
        {
            var source = """
                public class Thing
                {
                    public int Key { get; set; }
                    public string? Value { get; set; }
                }

                public class Using
                {
                    public static Thing CreateThing(int key, string? value)
                    {
                        return new()
                        {
                            Value,
                            Key = key,
                        };
                    }
                }
                """;
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
                // (4,18): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public string? Value { get; set; }
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(4, 18),
                // (9,52): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public static Thing CreateThing(int key, string? value)
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(9, 52),
                // (13,13): error CS0747: Invalid initializer member declarator
                //             Value,
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Value").WithLocation(13, 13)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var propertyDecls = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First()
                .ChildNodes()
                .OfType<PropertyDeclarationSyntax>()
                .ToArray();
            var valuePropertyDecl = propertyDecls[1];
            var valueProperty = model.GetDeclaredSymbol(valuePropertyDecl);
            Assert.NotNull(valueProperty);

            var initializers = root.DescendantNodes()
                .OfType<InitializerExpressionSyntax>()
                .First()
                .ChildNodes()
                .ToArray();
            var initializedSymbol = model.GetSymbolInfo(initializers[0]).Symbol;

            Assert.NotNull(initializedSymbol);
            Assert.True(initializedSymbol.Equals(valueProperty, SymbolEqualityComparer.Default));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74348")]
        public void ObjectInitializerIncompleteMemberValueAssignment06()
        {
            var source = """
                public class Outer
                {
                    public Thing Thing { get; } = new();
                }

                public class Thing
                {
                    public int Key { get; set; }
                    public string? Value { get; set; }
                }

                public class Using
                {
                    public static Outer CreateOuterThing(int key, string? value)
                    {
                        return new()
                        {
                            Thing =
                            {
                                Value,
                                Key = key,
                            }
                        };
                    }
                }
                """;
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
                // (9,18): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public string? Value { get; set; }
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(9, 18),
                // (14,57): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public static Outer CreateOuterThing(int key, string? value)
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(14, 57),
                // (20,17): error CS0747: Invalid initializer member declarator
                //                 Value,
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Value").WithLocation(20, 17)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var propertyDecls = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(s => s.Identifier.Text is "Thing")
                .ChildNodes()
                .OfType<PropertyDeclarationSyntax>()
                .ToArray();
            var valuePropertyDecl = propertyDecls[1];
            var valueProperty = model.GetDeclaredSymbol(valuePropertyDecl);
            Assert.NotNull(valueProperty);

            var thingInitializer = root.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .First(s => s.Left is IdentifierNameSyntax { Identifier.Text: "Thing" })
                .Right
                as InitializerExpressionSyntax;
            var valueInitializer = thingInitializer.Expressions[0];
            var initializedSymbol = model.GetSymbolInfo(valueInitializer).Symbol;

            Assert.NotNull(initializedSymbol);
            Assert.True(initializedSymbol.Equals(valueProperty, SymbolEqualityComparer.Default));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74348")]
        public void ObjectInitializerIncompleteMemberValueAssignment07()
        {
            var source = """
                public class Thing
                {
                    public int Key;
                    public string? Value;
                }

                public class Using
                {
                    public static Thing CreateThing(int key, string? value)
                    {
                        return new()
                        {
                            Value,
                            Key = key,
                        };
                    }
                }
                """;
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
                // (4,18): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public string? Value;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(4, 18),
                // (9,52): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public static Thing CreateThing(int key, string? value)
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(9, 52),
                // (13,13): error CS0747: Invalid initializer member declarator
                //             Value,
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Value").WithLocation(13, 13)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var valueFieldDecl = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First()
                .ChildNodes()
                .OfType<FieldDeclarationSyntax>()
                .ElementAt(1)
                .Declaration.Variables.First();
            var valueField = model.GetDeclaredSymbol(valueFieldDecl);
            Assert.NotNull(valueField);

            var initializers = root.DescendantNodes()
                .OfType<InitializerExpressionSyntax>()
                .First()
                .ChildNodes()
                .ToArray();
            var initializedSymbol = model.GetSymbolInfo(initializers[0]).Symbol;

            Assert.NotNull(initializedSymbol);
            Assert.True(initializedSymbol.Equals(valueField, SymbolEqualityComparer.Default));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74348")]
        public void ObjectInitializerIncompleteMemberValueAssignment08()
        {
            var source = """
                public class Thing
                {
                    public int Key;
                    public event Action? Handler;
                }

                public class Using
                {
                    public static Thing CreateThing(int key, Action? handler)
                    {
                        return new()
                        {
                            Handler,
                            Key = key,
                        };
                    }
                }
                """;
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
                // (4,18): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                //     public event Action? Handler;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Action").WithArguments("Action").WithLocation(4, 18),
                // (4,26): error CS0066: 'Thing.Handler': event must be of a delegate type
                //     public event Action? Handler;
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "Handler").WithArguments("Thing.Handler").WithLocation(4, 26),
                // (4,26): warning CS0067: The event 'Thing.Handler' is never used
                //     public event Action? Handler;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Handler").WithArguments("Thing.Handler").WithLocation(4, 26),
                // (9,46): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                //     public static Thing CreateThing(int key, Action? handler)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Action").WithArguments("Action").WithLocation(9, 46),
                // (13,13): error CS0747: Invalid initializer member declarator
                //             Handler,
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "Handler").WithLocation(13, 13),
                // (13,13): error CS0070: The event 'Thing.Handler' can only appear on the left hand side of += or -= (except when used from within the type 'Thing')
                //             Handler,
                Diagnostic(ErrorCode.ERR_BadEventUsage, "Handler").WithArguments("Thing.Handler", "Thing").WithLocation(13, 13)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();

            var initializers = root.DescendantNodes()
                .OfType<InitializerExpressionSyntax>()
                .First()
                .ChildNodes()
                .ToArray();
            var initializedSymbol = model.GetSymbolInfo(initializers[0]).Symbol;

            Assert.Null(initializedSymbol);
        }

        private static IParameterSymbol VerifyParameter(
            SemanticModel model,
            ParameterSyntax decl,
            int expectedOrdinal,
            string expectedSymbol,
            string expectedType,
            object expectedConstant)
        {
            var symbol = (IParameterSymbol)model.GetDeclaredSymbol(decl);
            Assert.Equal(expectedOrdinal, symbol.Ordinal);
            Assert.Equal(expectedSymbol, symbol.ToTestDisplayString());

            var valueSyntax = decl.Default?.Value;
            if (valueSyntax == null)
            {
                Assert.Null(expectedType);
                Assert.Null(expectedConstant);
            }
            else
            {
                var type = model.GetTypeInfo(valueSyntax);
                Assert.Equal(expectedType, type.Type.ToTestDisplayString());
                Optional<object> actualConstant = model.GetConstantValue(valueSyntax);
                Assert.Equal(expectedConstant, actualConstant.Value);
            }

            return symbol;
        }
    }
}
