// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class SemanticModelTests : CSharpTestBase
    {
        [Fact]
        public void TestGetDeclaredSymbolFromNamespace()
        {
            var compilation = CreateCompilationWithMscorlib(@"
namespace A.B
{
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var decl = (NamespaceDeclarationSyntax)root.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.NotNull(symbol);
            Assert.Equal("B", symbol.Name);
        }

        [Fact]
        public void NamespaceAndClassWithNoNames()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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

        [Fact]
        public void TestGetDeclaredSymbolFromNestedNamespace()
        {
            var compilation = CreateCompilationWithMscorlib(@"
namespace A.B
{
   namespace C.D
   {
   }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var abns = (NamespaceDeclarationSyntax)root.Members[0];
            var cdns = (NamespaceDeclarationSyntax)abns.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(cdns);
            Assert.NotNull(symbol);
            Assert.Equal("D", symbol.Name);
        }

        [Fact]
        public void IncompleteNamespaceDeclaration()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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

        [Fact]
        public void GenericNameInNamespaceName()
        {
            var compilation = CreateCompilationWithMscorlib(@"
namespace C<int>.B 
{ 
    class Y { }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var classY = ((root.
                Members[0] as NamespaceDeclarationSyntax).
                Members[0] as TypeDeclarationSyntax);

            var model = compilation.GetSemanticModel(tree);

            var symbol = model.GetDeclaredSymbol(classY);
            Assert.NotNull(symbol);
            Assert.Equal("C.B.Y", symbol.ToTestDisplayString());
        }

        [Fact]
        public void AliasedNameInNamespaceName()
        {
            var compilation = CreateCompilationWithMscorlib(@"
namespace alias::C<int>.B 
{ 
    class Y { }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var classY = ((root.
                Members[0] as NamespaceDeclarationSyntax).
                Members[0] as TypeDeclarationSyntax);

            var model = compilation.GetSemanticModel(tree);

            var symbol = model.GetDeclaredSymbol(classY);
            Assert.NotNull(symbol);
            Assert.Equal("C.B.Y", symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestGetDeclaredSymbolFromType()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(
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
            var compilation = CreateCompilationWithMscorlib(
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
            var compilation = CreateCompilationWithMscorlib(
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
            Assert.IsType<SourceCustomEventSymbol>(eventSymbol);

            var accessorList = eventDecl.AccessorList.Accessors;
            Assert.Equal(2, accessorList.Count);
            Assert.Same(eventSymbol.AddMethod, model.GetDeclaredSymbol(accessorList[0]));
            Assert.Same(eventSymbol.RemoveMethod, model.GetDeclaredSymbol(accessorList[1]));
        }

        [WorkItem(543494, "DevDiv")]
        [Fact()]
        public void TestGetDeclaredSymbolFromFieldLikeEvent()
        {
            var compilation = CreateCompilationWithMscorlib(
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
            Assert.IsType<SourceFieldLikeEventSymbol>(eventSymbol);
        }

        [WorkItem(543574, "DevDiv")]
        [Fact()]
        public void GetDeclaredSymbolOfEventDeclarationSyntaxAsBasePropertyDeclarationSyntax()
        {
            var compilation = CreateCompilationWithMscorlib(
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

        [WorkItem(543574, "DevDiv")]
        [Fact()]
        public void GetDeclaredSymbolOfPropertyDeclarationSyntaxAsBasePropertyDeclarationSyntax()
        {
            var compilation = CreateCompilationWithMscorlib(
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

        [WorkItem(543574, "DevDiv")]
        [Fact()]
        public void GetDeclaredSymbolOfIndexerDeclarationSyntaxAsBasePropertyDeclarationSyntax()
        {
            var compilation = CreateCompilationWithMscorlib(
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

        [WorkItem(543574, "DevDiv")]
        [Fact()]
        public void GetDeclaredSymbolOfEventDeclarationSyntax()
        {
            var compilation = CreateCompilationWithMscorlib(
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

        [WorkItem(543574, "DevDiv")]
        [Fact()]
        public void GetDeclaredSymbolOfPropertyDeclarationSyntax()
        {
            var compilation = CreateCompilationWithMscorlib(
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

        [WorkItem(543574, "DevDiv")]
        [Fact()]
        public void GetDeclaredSymbolOfIndexerDeclarationSyntax()
        {
            var compilation = CreateCompilationWithMscorlib(
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var symbol1 = model.GetDeclaredSymbol(labeled1);
            Assert.NotNull(symbol1);
            Assert.Equal("label1", symbol1.Name);

            var labeled2 = (LabeledStatementSyntax)labeled1.Statement;
            var symbol2 = model.GetDeclaredSymbol(labeled2);
            Assert.NotNull(symbol2);
            Assert.Equal("label2", symbol2.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromAnonymousTypePropertyInitializer()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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

            var symbol = model.GetDeclaredSymbol(switchLabel);
            Assert.NotNull(symbol);
            Assert.IsType<SourceLabelSymbol>(symbol);

            var labelSymbol = (SourceLabelSymbol)symbol;
            Assert.Equal(ConstantValue.Default(SpecialType.System_Int32), labelSymbol.SwitchCaseLabelConstant);
            Assert.Equal(switchLabel, labelSymbol.IdentifierNodeOrToken.AsNode());
            Assert.Equal("case 0:", labelSymbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromSwitchDefaultLabel()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var symbol1 = model.GetDeclaredSymbol(switchLabel);
            Assert.NotNull(symbol1);
            Assert.IsAssignableFrom<SourceLabelSymbol>(symbol1);
            var labelSymbol = (SourceLabelSymbol)symbol1;
            Assert.Null(labelSymbol.SwitchCaseLabelConstant);
            Assert.Equal(switchLabel, labelSymbol.IdentifierNodeOrToken.AsNode());
            Assert.Equal("default:", labelSymbol.Name);
        }

        [Fact]
        public void TestGetDeclaredSymbolFromFieldDeclarator()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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

        [WorkItem(540108, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolFromDelegateParameter()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
    public partial interface IFoo<T, V>
    {
        St<object>.Em ReturnEnum();
    }

    public partial interface IFoo<T, V>
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
            var fSymbol = model.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0]) as FieldSymbol;
            Assert.Equal("field", fSymbol.Name);
            Assert.Equal("T", fSymbol.Type.TypeSymbol.Name);
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
            Assert.Equal("N1.N2.IFoo<T, V>", itfcSymbol.ToTestDisplayString());
            // CC
            var pt = typeDecl.TypeParameterList.Parameters[1];
            var ptsym = model.GetDeclaredSymbol(pt);
            Assert.Equal(SymbolKind.TypeParameter, ptsym.Kind);
            Assert.Equal("V", ptsym.Name);

            var memDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var mSymbol = model.GetDeclaredSymbol(memDecl) as MethodSymbol;
            Assert.Equal("ReturnEnum", mSymbol.Name);
            Assert.Equal("N1.N2.St<System.Object>.Em", mSymbol.ReturnType.ToTestDisplayString());
            Assert.Equal<ISymbol>(enumSymbol, mSymbol.ReturnType.TypeSymbol.OriginalDefinition);

            typeDecl = (TypeDeclarationSyntax)nsDecl.Members[1];
            memDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            mSymbol = model.GetDeclaredSymbol(memDecl) as MethodSymbol;
            Assert.Equal(2, mSymbol.Parameters.Length);
            Assert.Equal("p1", mSymbol.Parameters[0].Name);
            Assert.Equal("St", mSymbol.Parameters[0].Type.Name);
            Assert.Equal<ISymbol>(structSymbol, mSymbol.Parameters[1].Type.TypeSymbol.OriginalDefinition);
            // CC
            var psym = model.GetDeclaredSymbol(memDecl.ParameterList.Parameters[0]);
            Assert.Equal(SymbolKind.Parameter, psym.Kind);
            Assert.Equal("p1", psym.Name);
        }

        [WorkItem(537230, "DevDiv")]
        [Fact]
        public void TestLookupUnresolvableNamespaceUsing()
        {
            var compilation = CreateCompilationWithMscorlib(@"
                namespace A
                {
                    using B.C;

                    public class B : C
                    {    
                    }
                }
            ");
            var tree = compilation.SyntaxTrees.Single();
            var usingDirective = (UsingDirectiveSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.UsingDirective).AsNode();
            var model = compilation.GetSemanticModel(tree);
            var type = model.GetTypeInfo(usingDirective.Name);
            Assert.NotEmpty(compilation.GetDeclarationDiagnostics());
            // should validate type here
        }

        [Fact]
        public void TestLookupSourceSymbolHidesMetadataSymbol()
        {
            var compilation = CreateCompilationWithMscorlib(@"
namespace System
{
    public class DateTime
    {
        string TheDateAndTime;
    }
}");

            var tree = compilation.SyntaxTrees.Single();

            var namespaceDecl = (NamespaceDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0];
            var classDecl = (ClassDeclarationSyntax)namespaceDecl.Members[0];
            var memberDecl = (FieldDeclarationSyntax)classDecl.Members[0];

            var model = compilation.GetSemanticModel(tree);
            var symbols = model.LookupSymbols(memberDecl.SpanStart, null, "DateTime");
            Assert.Equal(1, symbols.Length);
        }

        [Fact]
        public void TestLookupAllArities()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var container = (NamespaceOrTypeSymbol)symbols.Single();

            symbols = model.LookupSymbols(positionInC, name: "AliasZ", container: container);
            Assert.Equal(0, symbols.Length);
        }

        [Fact]
        public void TestLookupTypesAllArities()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            ");

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
            var compilation = CreateCompilationWithMscorlib(@"
                class A : B
                {
                   public int X;
                }

                class B : A
                {
                   public int Y;
                }
            ");

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
            var compilation = CreateCompilationWithMscorlib(@"
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
            ");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclC = (TypeDeclarationSyntax)cu.Members[2];
            var someMemberInC = (MemberDeclarationSyntax)typeDeclC.Members[0];

            var namesInC = model.LookupNames(someMemberInC.SpanStart);
            Assert.Equal(15, namesInC.Count); // everything in System.Object is included, with an uncertain count
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            ");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var cu = tree.GetCompilationUnitRoot();
            var typeDeclC = (TypeDeclarationSyntax)cu.Members[2];
            var someMemberInC = (MemberDeclarationSyntax)typeDeclC.Members[0];
            int positionInC = someMemberInC.SpanStart;

            var symbolsInC = model.LookupSymbols(positionInC);
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(
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
            var compilation = CreateCompilationWithMscorlib(
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

        private static TypeParameterSymbol LookupTypeParameterFromConstraintClause(SemanticModel model, TypeParameterConstraintClauseSyntax constraintSyntax, string name)
        {
            var constraintStart = constraintSyntax.WhereKeyword.SpanStart;
            var symbols = model.LookupSymbols(constraintStart, null, name: name);
            Assert.Equal(1, symbols.Length);
            var symbol = symbols[0] as TypeParameterSymbol;
            Assert.NotNull(symbol);
            return symbol;
        }

        [Fact]
        public void TestLookupSymbolsAllNames()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            ");

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
            var compilation = CreateCompilationWithMscorlib(@"
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

            var symbols = model.LookupSymbols(position).Where(s => !s.IsStatic && !((s is TypeSymbol)));
            Assert.Equal(9, symbols.Count());  // A.X, B.Y, C.Z, Object.ToString, Object.Equals, Object.Finalize, Object.GetHashCode, Object.GetType, Object.MemberwiseClone

            var symbols2 = model.LookupSymbols(position, container: symbolC).Where(s => !s.IsStatic && !((s is TypeSymbol)));
            Assert.Equal(9, symbols2.Count());  // A.X, B.Y, C.Z, Object.ToString, Object.Equals, Object.Finalize, Object.GetHashCode, Object.GetType, Object.MemberwiseClone
        }

        [Fact]
        public void TestLookupSymbolsAllNamesMustNotBeInstance()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(
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
                symbol.Kind == SymbolKind.Method && (((MethodSymbol)symbol).IsExtensionMethod || ((MethodSymbol)symbol).MethodKind == MethodKind.ReducedExtension);

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
            var compilation = CreateCompilationWithMscorlib(
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

        [WorkItem(541125, "DevDiv")]
        [Fact]
        public void TestLookupSymbolsMoreGenericExtensionMethods()
        {
            var compilation = CreateCompilationWithMscorlib(
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
            var typeA = ((ParameterSymbol)symbols[0]).Type.TypeSymbol;
            Assert.NotNull(typeA);
            symbols = model.LookupSymbols(methodStart, null, name: "b");
            CheckSymbolsUnordered(symbols, "B<string> b");
            var typeB = ((ParameterSymbol)symbols[0]).Type.TypeSymbol;
            Assert.NotNull(typeB);

            Func<ISymbol, bool> isExtensionMethod = symbol =>
                symbol.Kind == SymbolKind.Method && (((MethodSymbol)symbol).IsExtensionMethod || ((MethodSymbol)symbol).MethodKind == MethodKind.ReducedExtension);

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

        [WorkItem(544933, "DevDiv")]
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
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (8,9): error CS0311: The type 'B' cannot be used as type parameter 'T' in the generic type or method 'E.F<T>(T)'. There is no implicit reference conversion from 'B' to 'A'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "b.F").WithArguments("E.F<T>(T)", "A", "T", "B").WithLocation(8, 9));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var position = source.IndexOf("a.F()", StringComparison.Ordinal);
            var method = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetMember<MethodSymbol>("M");

            // No type.
            var symbols = model.LookupSymbols(position, container: null, name: "F", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols, "void E.F<T>(T t)");

            // Type satisfying constraint.
            symbols = model.LookupSymbols(position, container: method.Parameters[0].Type.TypeSymbol, name: "F", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols, "void A.F<A>()");

            // Type not satisfying constraint.
            symbols = model.LookupSymbols(position, container: method.Parameters[1].Type.TypeSymbol, name: "F", includeReducedExtensionMethods: true);
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
            compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (8,9): error CS0311: The type 'B' cannot be used as type parameter 'T' in the generic type or method 'E.F<T>(T)'. There is no implicit reference conversion from 'B' to 'A'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "b.F").WithArguments("E.F<T>(T)", "A", "T", "B").WithLocation(8, 9));

            tree = compilation.SyntaxTrees.Single();
            model = compilation.GetSemanticModel(tree);
            position = source.IndexOf("a.F()", StringComparison.Ordinal);
            method = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");

            // No type.
            symbols = model.LookupSymbols(position, container: null, name: "F", includeReducedExtensionMethods: true);
            CheckSymbols(symbols);

            // Type satisfying constraint.
            symbols = model.LookupSymbols(position, container: method.Parameters[0].Type.TypeSymbol, name: "F", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols, "void A.F<A>()");

            // Type not satisfying constraint.
            symbols = model.LookupSymbols(position, container: method.Parameters[1].Type.TypeSymbol, name: "F", includeReducedExtensionMethods: true);
            CheckSymbols(symbols);
        }

        [Fact]
        public void TestLookupSymbolsArrayExtensionMethods()
        {
            var reference = SystemCoreRef;
            var compilation = CreateCompilationWithMscorlib(
                references: new MetadataReference[] { reference },
                text:
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
            var type = ((ParameterSymbol)symbols[0]).Type.TypeSymbol;
            Assert.NotNull(type);

            // Extension method overloads for o.First.
            symbols = model.LookupSymbols(methodStart, type, name: "First", includeReducedExtensionMethods: true);
            CheckSymbolsUnordered(symbols,
                "object IEnumerable<object>.First<object>()",
                "object IEnumerable<object>.First<object>(Func<object, bool> predicate)");
        }

        private static void CheckSymbols(ImmutableArray<ISymbol> symbols, params string[] descriptions)
        {
            CompilationUtils.CheckSymbols(symbols, descriptions);
        }

        private static void CheckSymbolsUnordered(ImmutableArray<ISymbol> symbols, params string[] descriptions)
        {
            CompilationUtils.CheckSymbolsUnordered(symbols, descriptions);
        }

        [Fact]
        public void TestLookupSymbolsWithEmptyStringForNameDoesNotAssert()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(text);
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
using FooAttribute = System.ObsoleteAttribute;

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

[Foo()]
class Bar { }
";
            var compilation = CreateCompilationWithMscorlib(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            Func<int, NamespaceOrTypeSymbol, string, bool, ImmutableArray<ISymbol>> lookupAttributeTypeWithQualifier = (pos, qualifierOpt, name, isVerbatim) =>
            {
                var options = isVerbatim ? LookupOptions.VerbatimNameAttributeTypeOnly : LookupOptions.AttributeTypeOnly;

                var binder = ((CSharpSemanticModel)model).GetEnclosingBinder(pos);
                Assert.NotNull(binder);

                var lookupResult = LookupResult.GetInstance();
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                binder.LookupSymbolsSimpleName(
                    lookupResult, qualifierOpt, plainName: name, arity: 0, basesBeingResolved: null, options: options, diagnose: false, useSiteDiagnostics: ref useSiteDiagnostics);
                Assert.Null(useSiteDiagnostics);
                var result = lookupResult.IsMultiViable ? lookupResult.Symbols.ToImmutable() : ImmutableArray.Create<Symbol>();
                lookupResult.Free();
                return StaticCast<ISymbol>.From(result);
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
            var qnInvalidWithoutSuffix = (NamespaceOrTypeSymbol)qnSymbols[0];

            symbols = model.LookupNamespacesAndTypes(position, name: "Y", container: qnInvalidWithoutSuffix);
            Assert.Equal(1, symbols.Length);

            symbols = lookupAttributeTypeWithQualifier(position, qnInvalidWithoutSuffix, "Y", false);
            Assert.Equal(0, symbols.Length);

            symbols = lookupAttributeTypeWithQualifier(position, qnInvalidWithoutSuffix, "Y", true);
            Assert.Equal(0, symbols.Length);

            position = text.IndexOf("Foo()", 0, StringComparison.Ordinal);
            symbols = lookupAttributeType(position, "Foo", false);
            Assert.Equal(1, symbols.Length);

            symbols = lookupAttributeType(position, "Foo", true);
            Assert.Equal(0, symbols.Length);
        }

        [Fact]
        public void TestGetSemanticInfoOfInvocation()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
    public static void foo(int a)
    {
    }
}

public static class S2
{
    public static void foo(string a)
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
                foo(""sss"");
                foo(1);
            }
        }
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6));

            var compilation = CSharpCompilation.Create(
                assemblyName: "Test",
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithScriptClassName("Script"),
                syntaxTrees: new[] { tree },
                references: new[] { MscorlibRef });

            var expr = tree.FindNodeOrTokenByKind(SyntaxKind.StringLiteralToken).Parent.FirstAncestorOrSelf<ExpressionStatementSyntax>().Expression;

            var global = compilation.GlobalNamespace;
            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(expr);

            Assert.NotNull(info.Symbol);
        }

        [WorkItem(537932, "DevDiv")]
        [Fact]
        public void GetDeclaredSymbolDupAliasNameErr()
        {
            var compilation = CreateCompilationWithMscorlib(@"
namespace NS {  class A {}  }

namespace NS {
    using System;
    using B = NS.A;

    class B {}
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var n1 = globalNS.GetMembers("NS").First() as NamespaceSymbol;
            var typeB = n1.GetTypeMembers("B").First() as NamedTypeSymbol;
            Assert.Equal(2, root.Members.Count);
            var nsSyntax = (root.Members[1] as NamespaceDeclarationSyntax);
            Assert.Equal(1, nsSyntax.Members.Count);
            var classB = model.GetDeclaredSymbol(nsSyntax.Members[0] as TypeDeclarationSyntax);
            // Reference equal
            Assert.Equal(typeB, classB);
        }

        [WorkItem(537624, "DevDiv")]
        [Fact]
        public void GetDeclaredSymbolForUsingDirective()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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

            var explicitMethodSymbol = (MethodSymbol)model.GetDeclaredSymbol(explicitMethodNode);
            Assert.NotNull(explicitMethodSymbol);
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, explicitMethodSymbol.MethodKind);
            Assert.Equal("I.M", explicitMethodSymbol.Name);
            Assert.Equal(1, explicitMethodSymbol.ExplicitInterfaceImplementations.Length);

            var explicitPropertyNode = (PropertyDeclarationSyntax)classMemberNodes[1];
            Assert.Equal("P", explicitPropertyNode.Identifier.ValueText);

            var explicitPropertySymbol = (PropertySymbol)model.GetDeclaredSymbol(explicitPropertyNode);
            Assert.NotNull(explicitPropertySymbol);
            Assert.Equal("I.P", explicitPropertySymbol.Name);
            Assert.Equal(1, explicitPropertySymbol.ExplicitInterfaceImplementations.Length);

            var explicitPropertyAccessors = explicitPropertyNode.AccessorList.Accessors;
            Assert.Equal(2, explicitPropertyAccessors.Count);

            var explicitPropertyGetterNode = explicitPropertyAccessors[0];
            Assert.Equal("get", explicitPropertyGetterNode.Keyword.ValueText);

            var explicitPropertyGetterSymbol = (MethodSymbol)model.GetDeclaredSymbol(explicitPropertyGetterNode);
            Assert.NotNull(explicitPropertyGetterSymbol);
            Assert.Equal(MethodKind.PropertyGet, explicitPropertyGetterSymbol.MethodKind);
            Assert.Equal("I.get_P", explicitPropertyGetterSymbol.Name);
            Assert.Equal(1, explicitPropertyGetterSymbol.ExplicitInterfaceImplementations.Length);
            Assert.Same(explicitPropertySymbol.GetMethod, explicitPropertyGetterSymbol);

            var explicitPropertySetterNode = explicitPropertyAccessors[1];
            Assert.Equal("set", explicitPropertySetterNode.Keyword.ValueText);

            var explicitPropertySetterSymbol = (MethodSymbol)model.GetDeclaredSymbol(explicitPropertySetterNode);
            Assert.NotNull(explicitPropertySetterSymbol);
            Assert.Equal(MethodKind.PropertySet, explicitPropertySetterSymbol.MethodKind);
            Assert.Equal("I.set_P", explicitPropertySetterSymbol.Name);
            Assert.Equal(1, explicitPropertySetterSymbol.ExplicitInterfaceImplementations.Length);
            Assert.Same(explicitPropertySymbol.SetMethod, explicitPropertySetterSymbol);
        }

        [WorkItem(527284, "DevDiv")]
        [Fact]
        public void GetDeclaredSymbolDottedNSAPI()
        {
            var compilation = CreateCompilationWithMscorlib(@"
namespace N1 {
  namespace N2.N3
  {
    class C {}
  }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var n1 = globalNS.GetMembers().First() as NamespaceSymbol;
            var n2 = n1.GetMembers().First() as NamespaceSymbol;
            var n3 = n2.GetMembers().First() as NamespaceSymbol;

            var nsSyntax = (root.Members[0] as NamespaceDeclarationSyntax);
            var dn1 = model.GetDeclaredSymbol(nsSyntax);

            var nsSyntax23 = (nsSyntax.Members[0] as NamespaceDeclarationSyntax);
            var dn23 = model.GetDeclaredSymbol(nsSyntax23);

            // Reference equal
            Assert.Equal(n1, dn1);
            Assert.Equal(n3, dn23);
        }

        [WorkItem(527285, "DevDiv")]
        [Fact]
        public void GetDeclaredSymbolGlobalSystemNSErr()
        {
            var compilation = CreateCompilationWithMscorlib(@"
namespace global::System {}

class Test { }
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var compSym = compilation.GlobalNamespace.GetMembers("System").First() as NamespaceSymbol;

            var nsSyntax = (root.Members[0] as NamespaceDeclarationSyntax);
            var declsym = model.GetDeclaredSymbol(nsSyntax);
            // Reference equal
            Assert.Equal(compSym, declsym);
        }

        [WorkItem(527286, "DevDiv")]
        [Fact]
        public void GetDeclaredSymbolInvalidOverloadsErr()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class CFoo
{
    void M() {}
    int M() { return 0; }

    interface IFoo {}
    struct SFoo
    {
        void M(byte p) {}
        void M(ref byte p) {}
        void M(out byte p) {}
    }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;

            var sym1 = globalNS.GetMembers("CFoo").First() as NamedTypeSymbol;
            var mems = sym1.GetMembers("M");
            var node1 = (root.Members[0] as TypeDeclarationSyntax);

            var dsyma1 = model.GetDeclaredSymbol(node1.Members[0] as MethodDeclarationSyntax);
            var dsyma2 = model.GetDeclaredSymbol(node1.Members[1]);
            // By Design - conflicting overloads bind to distinct symbols
            Assert.NotEqual(dsyma1, dsyma2);
            // for CC?
            var sym2 = sym1.GetMembers("IFoo").First() as NamedTypeSymbol;
            var node2 = (node1.Members[2] as TypeDeclarationSyntax);
            var dsym2 = model.GetDeclaredSymbol(node2);
            Assert.Equal(TypeKind.Interface, dsym2.TypeKind);

            var sym3 = sym1.GetMembers("SFoo").First() as NamedTypeSymbol;
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

        [WorkItem(537953, "DevDiv")]
        [Fact]
        public void GetDeclaredSymbolNoTypeSymbolWithErr()
        {
            var compilation = CreateCompilationWithMscorlib(@"
namespace NS
{
  protected class A { }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var ns1 = globalNS.GetMembers("NS").Single() as NamespaceSymbol;
            var srcSym = ns1.GetMembers("A").Single() as NamedTypeSymbol;

            var nsSyntax = (root.Members[0] as NamespaceDeclarationSyntax);
            var declSym = model.GetDeclaredSymbol(nsSyntax.Members[0] as TypeDeclarationSyntax);

            Assert.Equal(srcSym, declSym);
        }

        [WorkItem(537954, "DevDiv")]
        [Fact]
        public void GetDeclaredSymbolExtraForDupTypesErr()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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

        [WorkItem(537955, "DevDiv")]
        [Fact]
        public void GetDeclaredSymbolSameNameMethodsDiffNSs()
        {
            var compilation = CreateCompilationWithMscorlib(@"
namespace Foo {
    class A { }
}

namespace NS {
    using Foo;
    class A { }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var ns1 = globalNS.GetMembers("NS").Single() as NamespaceSymbol;
            var typeA = ns1.GetTypeMembers("A").First() as NamedTypeSymbol;

            var nsSyntax = (root.Members[1] as NamespaceDeclarationSyntax);
            var dsym1 = model.GetDeclaredSymbol(nsSyntax.Members[0] as TypeDeclarationSyntax);

            Assert.Equal(typeA, dsym1);
        }

        [Fact]
        public void GetDeclaredSymbolNSCrossComps()
        {
            var comp1 = CreateCompilationWithMscorlib(@"
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

            var comp = CSharpCompilation.Create(
                "Repro",
                new[] { SyntaxFactory.ParseSyntaxTree(text2) },
                new[] { MscorlibRef, ref1 });

            var tree = comp.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = comp.GetSemanticModel(tree);

            var nsg = comp.GlobalNamespace;
            var ns1 = nsg.GetMembers("NS1").Single() as NamespaceSymbol;
            var ns2 = ns1.GetMembers("NS2").Single() as NamespaceSymbol;

            var nsSyntax1 = (root.Members[0] as NamespaceDeclarationSyntax);
            var nsSyntax2 = (nsSyntax1.Members[0] as NamespaceDeclarationSyntax);
            var dsym1 = model.GetDeclaredSymbol(nsSyntax1);
            var dsym2 = model.GetDeclaredSymbol(nsSyntax2);

            Assert.Equal(ns1, dsym1);
            Assert.Equal(ns2, dsym2);
        }

        [WorkItem(538953, "DevDiv")]
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

            var compilation = CreateCompilationWithMscorlib(text1);
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree);

            var globalNS = compilation.SourceModule.GlobalNamespace;
            var typeA = globalNS.GetTypeMembers("ErrorProp").First() as NamedTypeSymbol;

            var prop = typeA.GetMembers("Prop1").FirstOrDefault() as PropertySymbol;
            var synType = root.Members[0] as TypeDeclarationSyntax;
            var accessors = (synType.Members[0] as PropertyDeclarationSyntax).AccessorList;
            var dsym = model.GetDeclaredSymbol(accessors.Accessors[0]);
            Assert.Equal(prop.GetMethod, dsym);
            dsym = model.GetDeclaredSymbol(accessors.Accessors[1]);
            Assert.Null(dsym);

            prop = typeA.GetMembers("Prop2").FirstOrDefault() as PropertySymbol;
            accessors = (synType.Members[1] as PropertyDeclarationSyntax).AccessorList;
            dsym = model.GetDeclaredSymbol(accessors.Accessors[0]);
            Assert.Equal(prop.GetMethod, dsym);
            dsym = model.GetDeclaredSymbol(accessors.Accessors[1]);
            Assert.Equal(prop.SetMethod, dsym);
            dsym = model.GetDeclaredSymbol(accessors.Accessors[2]);
            Assert.Null(dsym);
        }

        [WorkItem(538953, "DevDiv")]
        [Fact]
        public void GetDeclaredSymbolAccessorErrs2()
        {
            var text =
@"
public sealed class ErrorProp
{
    public string Prop1 { foo { return null; } } // invalid accessor name
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var accessorDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();
            Assert.Equal(SyntaxKind.UnknownAccessorDeclaration, accessorDecl.Kind());
            Assert.Null(model.GetDeclaredSymbol(accessorDecl));
        }

        [WorkItem(538148, "DevDiv")]
        [Fact]
        public void TestOverloadsInImplementedInterfaceMethods()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  void F()
  {
    strin g;
  }
}
");
            var tree = compilation.SyntaxTrees[0];
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
            var compilation = CreateCompilationWithMscorlib(@"
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
        [WorkItem(539346, "DevDiv")]
        [WorkItem(540792, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolForParamInLambdaExprPrecededByExplicitKeyword()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
        [WorkItem(539346, "DevDiv")]
        [WorkItem(540792, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolForLambdaInDefaultValue1()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;
class Program
{
    void Foo(Func<int, int> f = x => 1)
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

        [WorkItem(540792, "DevDiv")]
        [WorkItem(539346, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolForLambdaInDefaultValue2()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;
class Program
{
    void Foo(Func<int, Func<int, int>> f = w => x => 1)
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

        [WorkItem(540834, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolForIncompleteMemberNode()
        {
            var compilation = CreateCompilationWithMscorlib(@"u");
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
            var compilation = CreateCompilationWithMscorlib(@"
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

        [WorkItem(541057, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolConstDelegateDecl()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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

        [WorkItem(541084, "DevDiv")]
        [Fact]
        public void TestIncompleteUsingDirectiveSyntax()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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

        [WorkItem(541225, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolForeachStmt()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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

        [WorkItem(541225, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolForeachStmtError1()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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

        [WorkItem(541225, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolForeachStmtError2()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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

        [WorkItem(541225, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolCatchClause()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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

        [WorkItem(541214, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolTopLevelMethod()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;
class void Foo()
{
    int x;
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var methodDecl = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("Foo", StringComparison.Ordinal)).Parent;
            Assert.Equal(SyntaxKind.MethodDeclaration, methodDecl.Kind());

            var symbol = model.GetDeclaredSymbol(methodDecl);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            Assert.Equal("Foo", symbol.Name);
        }

        [WorkItem(541214, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolNamespaceLevelMethod()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;
namespace N
{
    class void Foo()
    {
        int x;
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var methodDecl = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("Foo", StringComparison.Ordinal)).Parent;
            Assert.Equal(SyntaxKind.MethodDeclaration, methodDecl.Kind());

            var symbol = model.GetDeclaredSymbol(methodDecl);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            Assert.Equal("Foo", symbol.Name);
        }

        [WorkItem(543238, "DevDiv")]
        [Fact]
        public void TestGetDeclaredSymbolEnumMemberDeclarationSyntax()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
            var compilation = CreateCompilationWithMscorlib(@"
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
            var parameter = (ParameterSymbol)info.Symbol;
            Assert.Equal("x", parameter.Name);
            Assert.Equal(SpecialType.System_Int32, parameter.Type.SpecialType);
        }

        [WorkItem(541800, "DevDiv")]
        [Fact]
        public void GetDeclaredSymbolOnGlobalStmtParseOptionInteractive()
        {
            var parseOptions = TestOptions.Interactive;
            var compilation = CreateCompilationWithMscorlib(@"/", parseOptions: parseOptions);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var globalStmt = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf('/')).Parent.AncestorsAndSelf().Single(x => x.IsKind(SyntaxKind.GlobalStatement));

            var symbol = model.GetDeclaredSymbol(globalStmt);

            Assert.Null(symbol);
        }

        [WorkItem(542102, "DevDiv")]
        [Fact]
        public void GetSymbolInGoto()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class Program
{
    static void Main()
    {
    Foo:
        int Foo;
        goto Foo;
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

        [WorkItem(542342, "DevDiv")]
        [Fact]
        public void SourceNamespaceSymbolMergeWithMetadata()
        {
            var compilation = CreateCompilationWithMscorlib(new string[] {@"namespace System {
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
}"}
 );

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var decl = (NamespaceDeclarationSyntax)root.Members[0];
            var model = compilation.GetSemanticModel(tree);
            var declSymbol = model.GetDeclaredSymbol(decl);
            Assert.NotNull(declSymbol);
            Assert.Equal("System", declSymbol.Name);
            Assert.Equal(3, declSymbol.Locations.Length);
            Assert.Equal(typeof(MergedNamespaceSymbol).FullName, declSymbol.GetType().ToString());
            Assert.Equal(NamespaceKind.Compilation, declSymbol.NamespaceKind);
            Assert.Equal(2, declSymbol.ConstituentNamespaces.Length);

            var tree2 = compilation.SyntaxTrees[1];
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

        [WorkItem(542459, "DevDiv")]
        [Fact]
        public void StructKeywordInsideSwitchWithInteractiveParseOption()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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
", parseOptions: TestOptions.Interactive
 );

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var diagnostics = model.GetDiagnostics();

            Assert.NotEmpty(diagnostics);
        }

        [WorkItem(542459, "DevDiv")]
        [Fact]
        public void Bug9728_SmallerReproCase()
        {
            var code = @"
using System;
struct break;
";

            var compilation = CreateCompilationWithMscorlib(code, parseOptions: TestOptions.Interactive);

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var diagnostics = model.GetDiagnostics();

            Assert.NotEmpty(diagnostics);
        }

        [WorkItem(542483, "DevDiv")]
        [Fact]
        public void IncompleteStructDeclWithSpace()
        {
            var compilation = CreateCompilationWithMscorlib(@"
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

        [WorkItem(542583, "DevDiv")]
        [Fact]
        public void LambdaExpressionInFieldInitReferencingAnotherFieldWithInteractiveParseOption()
        {
            string sourceCode = @"
using System.Linq;
using System.Collections;
 
class P
{
    double one = 1;
    public Func<int, int> z = (x => x + one);
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode, parseOptions: TestOptions.Interactive);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            var queryExpr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParenthesizedExpressionSyntax>().First();
            var symbolInfo = semanticModel.GetSemanticInfoSummary(queryExpr);
            Assert.Equal(SymbolKind.Method, symbolInfo.Symbol.Kind);
        }

        [WorkItem(542495, "DevDiv")]
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
            var compilation = CreateCompilationWithMscorlib(text);
            var tree = compilation.SyntaxTrees[0];
            var node = (IdentifierNameSyntax)tree.GetCompilationUnitRoot().DescendantTokens().Where(t => t.ToString() == "Alias").Last().Parent;
            var model1 = compilation.GetSemanticModel(tree);
            var alias1 = model1.GetAliasInfo(node);

            // We want the Compilation's WeakReference<BinderFactory> to be collected
            // so that the next semantic model will get a new one.
            model1 = null; // otherwise it will keep the BinderFactory alive
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();

            var model2 = compilation.GetSemanticModel(tree);
            var alias2 = model2.GetAliasInfo(node);

            Assert.Equal(alias1, alias2);
            Assert.NotSame(alias1, alias2);
        }

        [WorkItem(542475, "DevDiv")]
        [Fact]
        public void PartialMethods()
        {
            string sourceCode = @" using System;
partial class program
{
    static void Main(string[] args)
    {
        //foo(gender: 1 > 2, name: "", age: 1);
    }
    static partial void foo(string name, int age, bool gender, int index1 = 1) { }
}
partial class program
{
    static partial void foo(string name, int age, bool gender, int index1 = 1);
}";
            var tree = Parse(sourceCode);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);
            var param = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().Where(p => p.Identifier.ValueText == "name").First();
            var symbol = model.GetDeclaredSymbol(param);
            Assert.Equal(param.Identifier.Span, symbol.Locations[0].SourceSpan);
        }

        [WorkItem(542217, "DevDiv")]
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
            var comp = CreateCompilationWithMscorlib(tree);
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
            Assert.NotSame(alias2, alias2b);
            Assert.Equal(alias2, alias2b);
        }

        [WorkItem(542902, "DevDiv")]
        [Fact]
        public void InaccessibleDefaultAttributeConstructor()
        {
            var c1 = CreateCompilationWithMscorlib(@"
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

            var c2 = CreateCompilationWithMscorlib(tree2, references: new[] { new CSharpCompilationReference(c1) });

            var attr = (AttributeSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)tree2.GetCompilationUnitRoot()).Members[0]).AttributeLists[0].Attributes[0];
            var model = c2.GetSemanticModel(tree2);

            var symbolInfo = model.GetSymbolInfo(attr);
            Assert.Equal(CandidateReason.NotAnAttributeType, symbolInfo.CandidateReason);
        }

        [WorkItem(543024, "DevDiv")]
        [Fact]
        public void BindUnboundGenericType()
        {
            var source = @"
public class A<T>
{
    public class B<U>
    {
        void Foo(object o) 
        {
            Foo(typeof(T));
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();

            var typeA = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var typeB = typeA.GetMember<NamedTypeSymbol>("B");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var typeofSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Single();
            var typeofArgSyntax = typeofSyntax.Type;
            var typeofArgPosition = typeofArgSyntax.SpanStart;

            TypeSymbol boundType;
            SymbolInfo symbolInfo;

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<T>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as TypeSymbol;
            Assert.Equal(typeA, boundType);
            Assert.Equal(typeA, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<int>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as TypeSymbol;
            Assert.NotEqual(typeA, boundType);
            Assert.Equal(typeA, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as TypeSymbol;
            Assert.NotEqual(typeA, boundType);
            Assert.Equal(typeA, boundType.OriginalDefinition);
            Assert.True(boundType.IsUnboundGenericType());


            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("B<U>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as TypeSymbol;
            Assert.Equal(typeB, boundType);
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("B<int>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as TypeSymbol;
            Assert.NotEqual(typeB, boundType);
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("B<>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as TypeSymbol;
            Assert.NotEqual(typeB, boundType);
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.True(boundType.IsUnboundGenericType());


            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<>.B<>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as TypeSymbol;
            Assert.NotEqual(typeB, boundType);
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.True(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<>.B<U>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as TypeSymbol;
            Assert.NotEqual(typeB, boundType); // unbound generic type not constructed since illegal
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<T>.B<>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as TypeSymbol;
            Assert.NotEqual(typeB, boundType); // unbound generic type not constructed since illegal
            Assert.Equal(typeB, boundType.OriginalDefinition);
            Assert.False(boundType.IsUnboundGenericType());

            symbolInfo = model.GetSpeculativeSymbolInfo(typeofArgPosition, SyntaxFactory.ParseTypeName("A<T>.B<U>"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            boundType = symbolInfo.Symbol as TypeSymbol;
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
            var compilation = CreateCompilationWithMscorlib(source);

            var classM = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("M");
            var fieldB = classM.GetMember<FieldSymbol>("B");

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

        // Bind an attribute. No such API, so use a bit of a workaround.
        private AttributeSyntax BindAttributeSyntax(string source)
        {
            return SyntaxFactory.ParseCompilationUnit(source + " class X {}").Members.First().AsTypeDeclarationSyntax().AttributeLists.First().Attributes.First();
        }

        [WorkItem(653957, "DevDiv")]
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
            var comp = CreateCompilationWithMscorlib(src);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var call = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            SymbolInfo info = new SymbolInfo();
            info = model.GetSymbolInfo(call);

            Assert.IsType<SourceMemberMethodSymbol>(info.Symbol);

            src = @"
static class S
{
    public static void Write(this IWriter writer, int value)
    {
        writer.Write(value);
    }
}";
            comp = CreateCompilationWithMscorlib(src);
            tree = comp.SyntaxTrees.Single();
            model = comp.GetSemanticModel(tree);

            call = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            info = model.GetSymbolInfo(call);

            Assert.IsType<ReducedExtensionMethodSymbol>(info.Symbol);
        }

        [Fact]
        public void BindSpeculativeAttribute()
        {
            var source = @"
using System;
using O=System.ObsoleteAttribute;

class C {
    class DAttribute: Attribute {}
    C foo<O>() { return null; }
    [Serializable] int i;
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position = tree.GetText().ToString().IndexOf("class C {", StringComparison.Ordinal);
            var attr1 = BindAttributeSyntax("[Obsolete]");

            var symbolInfo = model.GetSpeculativeSymbolInfo(position, attr1);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString());

            var attr2 = BindAttributeSyntax("[ObsoleteAttribute(4)]");

            symbolInfo = model.GetSpeculativeSymbolInfo(position, attr2);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.OverloadResolutionFailure);
            Assert.Equal(3, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor()", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());

            var attr3 = BindAttributeSyntax(@"[O(""hello"")]");

            symbolInfo = model.GetSpeculativeSymbolInfo(position, attr3);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());

            var attr4 = BindAttributeSyntax("[P]");

            symbolInfo = model.GetSpeculativeSymbolInfo(position, attr4);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

            var attr5 = BindAttributeSyntax("[D]");

            symbolInfo = model.GetSpeculativeSymbolInfo(position, attr5);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("C.DAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString());

            var attr6 = BindAttributeSyntax(@"[O(""hello"")]");
            var position2 = tree.GetText().ToString().IndexOf("C foo<O>", StringComparison.Ordinal);

            symbolInfo = model.GetSpeculativeSymbolInfo(position2, attr6);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());

            var attr7 = BindAttributeSyntax(@"[O(""hello"")]");
            var position3 = tree.GetText().ToString().IndexOf("Serializable", StringComparison.Ordinal);

            symbolInfo = model.GetSpeculativeSymbolInfo(position3, attr7);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
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
    C foo<O>() { return null; }
    [Serializable] int i;
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            var tree = compilation.SyntaxTrees.Single();
            var parentModel = compilation.GetSemanticModel(tree);

            var position = tree.GetText().ToString().IndexOf("class C {", StringComparison.Ordinal);

            var attr1 = BindAttributeSyntax("[Obsolete]");

            SemanticModel speculativeModel;
            var success = parentModel.TryGetSpeculativeSemanticModel(position, attr1, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var symbolInfo = speculativeModel.GetSymbolInfo(attr1);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString());

            var attr2 = BindAttributeSyntax("[ObsoleteAttribute(4)]");
            success = parentModel.TryGetSpeculativeSemanticModel(position, attr2, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr2);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.OverloadResolutionFailure);
            Assert.Equal(3, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.ObsoleteAttribute..ctor()", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());

            var constantInfo = speculativeModel.GetConstantValue(attr2.ArgumentList.Arguments.First().Expression);
            Assert.True(constantInfo.HasValue, "must be constant");
            Assert.Equal(4, constantInfo.Value);

            var attr3 = BindAttributeSyntax(@"[O(""hello"")]");

            success = parentModel.TryGetSpeculativeSemanticModel(position, attr3, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);
            symbolInfo = speculativeModel.GetSymbolInfo(attr3);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
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

            var attr4 = BindAttributeSyntax("[P]");

            success = parentModel.TryGetSpeculativeSemanticModel(position, attr4, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr4);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

            var attr5 = BindAttributeSyntax("[D]");

            success = parentModel.TryGetSpeculativeSemanticModel(position, attr5, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr5);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("C.DAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString());

            var attr6 = BindAttributeSyntax(@"[O(""hello"")]");
            var position2 = tree.GetText().ToString().IndexOf("C foo<O>", StringComparison.Ordinal);

            success = parentModel.TryGetSpeculativeSemanticModel(position, attr6, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr6);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
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

            var attr7 = BindAttributeSyntax(@"[O(""hello"")]");
            var position3 = tree.GetText().ToString().IndexOf("Serializable", StringComparison.Ordinal);

            success = parentModel.TryGetSpeculativeSemanticModel(position3, attr7, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbolInfo = speculativeModel.GetSymbolInfo(attr7);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
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
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None);
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
            var compilation = CreateCompilationWithMscorlib(@"
using System.Collections.Generic;
using System.Linq;
class Foo
{
  public string[] P { get; set; }
  public IEnumerable<Foo> Arguments { get; set; }

    static void M(IEnumerable<Foo> c)
    {
        var q = from e in c
                    select new Foo()
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

            var compilation = CreateCompilationWithMscorlib(source);

            var conversion = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.ImplicitConversionName);
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

            var compilation = CreateCompilationWithMscorlib(source);

            var conversion = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.ExplicitConversionName);
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

            var compilation = CreateCompilationWithMscorlib(source);

            var @operator = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.AdditionOperatorName);
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

        [WorkItem(543415, "DevDiv")]
        [Fact]
        public void AliasRace1()
        {
            var text = @"
using Alias = Foo;

namespace Foo { }

[System.Obsolete]
class C { }
";

            var compilation = CreateCompilationWithMscorlib(text);
            compilation.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using Alias = Foo;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Alias = Foo;"));

            var @namespace = compilation.GlobalNamespace.GetMember<NamespaceSymbol>("Foo");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            int position = text.IndexOf("Obsolete", StringComparison.Ordinal);

            var result = Parallel.For(0, 100, i =>
            {
                var symbols = model.LookupSymbols(position, name: "Alias");
                var alias = (AliasSymbol)symbols.Single();

                Assert.Equal(@namespace, alias.Target);
            });

            Assert.True(result.IsCompleted);
        }

        [WorkItem(543415, "DevDiv")]
        [Fact]
        public void AliasRace2()
        {
            var text = @"
using Alias = Foo;

namespace Foo { }

[System.Obsolete]
class C { }
";

            var compilation = CreateCompilationWithMscorlib(text);
            compilation.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using Alias = Foo;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Alias = Foo;"));

            var @namespace = compilation.GlobalNamespace.GetMember<NamespaceSymbol>("Foo");

            var tree = compilation.SyntaxTrees.Single();

            int position = text.IndexOf("Obsolete", StringComparison.Ordinal);

            var result = Parallel.For(0, 100, i =>
            {
                var model = compilation.GetSemanticModel(tree);
                var symbols = model.LookupSymbols(position, name: "Alias");
                var alias = (AliasSymbol)symbols.Single();

                Assert.Equal(@namespace, alias.Target);
            });

            Assert.True(result.IsCompleted);
        }

        [WorkItem(544100, "DevDiv")]
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

            var compilation = CreateCompilationWithMscorlib(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var node = (DefaultExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Where(i => i is DefaultExpressionSyntax).First();
            model.GetSemanticInfoSummary(node.Type);
        }

        [WorkItem(543868, "DevDiv")]
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

            var compilation = CreateCompilationWithMscorlib(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            compilation.VerifyDiagnostics(
                // (11,36): warning CS0067: The event 'Enclosing.Declaring.E' is never used
                //         public event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("Enclosing.Declaring.E"));

            var declaringType = compilation.GlobalNamespace.GetMember<TypeSymbol>("Enclosing").GetMember<TypeSymbol>("Declaring");
            var fieldLikeEvent = declaringType.GetMember<EventSymbol>("E");
            var customEvent = declaringType.GetMember<EventSymbol>("F");

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
            var compilation = CreateCompilationWithMscorlib(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ClassDeclarationSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.ClassDeclaration));
            var model = compilation.GetSemanticModel(tree);
            var type = (NamedTypeSymbol)model.GetDeclaredSymbol(decl);
            type = type.BaseType;
            Assert.Equal(type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), "A<object>.B<?>");
        }

        [Fact]
        public void UnboundNestedType_2()
        {
            var source =
@"class A<T, U> { }
class C : A<,,>.B<object> { }";
            var compilation = CreateCompilationWithMscorlib(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ClassDeclarationSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.ClassDeclaration));
            var model = compilation.GetSemanticModel(tree);
            var type = (NamedTypeSymbol)model.GetDeclaredSymbol(decl);
            type = type.BaseType;
            Assert.Equal(type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), "A<?, ?, ?>.B<object>");
        }

        [Fact]
        public void UnboundNestedType_3()
        {
            var source =
@"class A { }
class C : A<>.B<> { }";
            var compilation = CreateCompilationWithMscorlib(source);
            var tree = compilation.SyntaxTrees[0];
            var decl = (ClassDeclarationSyntax)tree.GetCompilationUnitRoot().DescendantNodes().Last(n => n.IsKind(SyntaxKind.ClassDeclaration));
            var model = compilation.GetSemanticModel(tree);
            var type = (NamedTypeSymbol)model.GetDeclaredSymbol(decl);
            type = type.BaseType;
            Assert.Equal(type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), "A<?>.B<?>");
        }

        [WorkItem(563572, "DevDiv")]
        [Fact]
        public void InvalidEnumDeclaration()
        {
            var source = @"
public class C
{
    public event enum
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            var tree = compilation.SyntaxTrees.Single();
            var enumDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<EnumDeclarationSyntax>().Single();
            var eventDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<EventDeclarationSyntax>().Single();

            // To repro DevDiv #563572, we need to go through the interface (which, at the time, followed
            // a different code path).
            var model = (SemanticModel)compilation.GetSemanticModel(tree);

            var enumSymbol = model.GetDeclaredSymbol(enumDecl); //Used to assert.
            Assert.Equal(SymbolKind.NamedType, enumSymbol.Kind);
            Assert.Equal(TypeKind.Enum, ((TypeSymbol)enumSymbol).TypeKind);

            var eventSymbol = model.GetDeclaredSymbol(eventDecl);
            Assert.Equal(SymbolKind.Event, eventSymbol.Kind);
        }

        [WorkItem(563572, "DevDiv")]
        [Fact]
        public void TypeMembersWithoutNames()
        {
            var source = @"
public class S
{
    struct interface
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            var tree = compilation.SyntaxTrees.Single();
            var structDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<StructDeclarationSyntax>().First();
            var interfaceDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Last();

            var model = (SemanticModel)compilation.GetSemanticModel(tree);

            var structSymbol = model.GetDeclaredSymbol(structDecl);
            var interfaceSymbol = model.GetDeclaredSymbol(interfaceDecl);

            // The missing identifier of the struct declaration is contained in both declaration spans (since it has width zero).
            // We used to just pick the first matching span, but now we keep looking until we find a "good" match.
            Assert.NotEqual(structSymbol, interfaceSymbol);
            Assert.Equal(TypeKind.Struct, ((TypeSymbol)structSymbol).TypeKind);
            Assert.Equal(TypeKind.Interface, ((TypeSymbol)interfaceSymbol).TypeKind);
        }

        [Fact]
        public void TestIncompleteMemberNode_Visitor()
        {
            var compilation = CreateCompilationWithMscorlib(@"u");
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
            Assert.Equal(1, x.Width);

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
    }
}
