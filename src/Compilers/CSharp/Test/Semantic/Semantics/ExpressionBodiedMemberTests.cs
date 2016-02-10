// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    /// <summary>
    /// Contains tests for expression-bodied members in the semantic model.
    /// </summary>
    public class ExpressionBodiedMemberTests : SemanticModelTestBase
    {
        [Fact]
        public void PartialMethods()
        {
            var comp = CreateCompilationWithMscorlib45(@"
public partial class C
{
    static partial void foo() => System.Console.WriteLine(""test"");
}

public partial class C
{
    static partial void foo();
}
");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .ElementAt(1);

            var fooDef = model.GetDeclaredSymbol(node) as SourceMemberMethodSymbol;
            Assert.NotNull(fooDef);
            Assert.True(fooDef.IsPartial);
            Assert.True(fooDef.IsPartialDefinition);
            Assert.False(fooDef.IsPartialImplementation);
            Assert.Null(fooDef.PartialDefinitionPart);

            var fooImpl = fooDef.PartialImplementationPart
                as SourceMemberMethodSymbol;
            Assert.NotNull(fooImpl);
            Assert.True(fooImpl.IsPartial);
            Assert.True(fooImpl.IsPartialImplementation);
            Assert.False(fooImpl.IsPartialDefinition);
            Assert.True(fooImpl.IsExpressionBodied);
        }

        [Fact]
        public void ExprBodiedProp01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class Program
{
    public int F = 1;
    public int P2 => /*<bind>*/this.F/*</bind>*/;
}
");
            comp.VerifyDiagnostics();
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(comp);

            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            var semanticSymbol = semanticInfo.Symbol;
            var global = comp.GlobalNamespace;
            var program = global.GetTypeMember("Program");
            var field = program.GetMember<SourceFieldSymbol>("F");

            Assert.Equal(field, semanticSymbol);

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ExprBodiedProp03()
        {
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(@"
using System;

class C
{
    int P => /*<bind>*/10/*</bind>*/;
}");
            Assert.NotNull(semanticInfo);
            Assert.Null(semanticInfo.Symbol);
            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(10, semanticInfo.ConstantValue);
        }

        [Fact]
        public void ExprBodiedProp04()
        {
            var tree = Parse(@"
using System;

class C
{
    int P => /*<bind>*/P/*</bind>*/;
}");
            var comp = CreateCompilationWithMscorlib45(new[] { tree });

            var info = GetSemanticInfoForTest<IdentifierNameSyntax>(comp);
            Assert.NotNull(info);
            var sym = Assert.IsType<SourcePropertySymbol>(info.Symbol);
            var c = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(c.GetMember<SourcePropertySymbol>("P"), sym);
        }

        [Fact]
        public void ExprBodiedIndexer01()
        {
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(@"
using System;
class Program
{
    public int this[int i] => /*<bind>*/i/*</bind>*/;
}");
            Assert.NotNull(semanticInfo);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
            Assert.Equal("i", semanticInfo.Symbol.Name);
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ExprBodiedIndexer02()
        {
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(@"
using System;
class C {}
class Program
{
    public C this[C c] => /*<bind>*/c/*</bind>*/;
}");
            Assert.NotNull(semanticInfo);
            Assert.Equal("C", semanticInfo.Type.Name);
            Assert.Equal("c", semanticInfo.Symbol.Name);
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ExprBodiedIndexer03()
        {
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(@"
class C
{
    public object this[string s] => /*<bind>*/s/*</bind>*/;
}");
            Assert.NotNull(semanticInfo);
            Assert.Equal("s", semanticInfo.Symbol.Name);
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);

            Assert.Equal(SpecialType.System_String, semanticInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Object, semanticInfo.ConvertedType.SpecialType);
        }

        [Fact]
        public void ExprBodiedIndexer04()
        {
            var semanticInfo = GetSemanticInfoForTest<InvocationExpressionSyntax>(@"
class C
{
    int M(int i) => i;
    long M(long l) => l;
    string this[string s] => /*<bind>*/M(s)/*</bind>*/;
}");
            Assert.NotNull(semanticInfo);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            Assert.Equal("System.Int32 C.M(System.Int32 i)", semanticInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("System.Int64 C.M(System.Int64 l)", semanticInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
        }

        [Fact]
        public void ExprBodiedIndexer05()
        {
            var semanticInfo = GetSemanticInfoForTest<SimpleNameSyntax>(@"
class C
{
    int this[int i] => /*<bind>*/i/*</bind>*/;
}");
            Assert.NotNull(semanticInfo);
            var sym = semanticInfo.Symbol;
            var accessor = Assert.IsType<SourcePropertyAccessorSymbol>(sym.ContainingSymbol);
            var prop = accessor.AssociatedSymbol;
            Assert.IsType<SourcePropertySymbol>(prop);
        }

        [Fact]
        public void ExprBodiedFunc01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class Program
{
    public int M(int i) => /*<bind>*/i/*</bind>*/;
}");
            comp.VerifyDiagnostics();

            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(comp);

            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            var semanticSymbol = semanticInfo.Symbol;
            var global = comp.GlobalNamespace;
            var program = global.GetTypeMember("Program");
            var method = program.GetMember<SourceMemberMethodSymbol>("M");
            var i = method.Parameters[0];

            Assert.Equal(i, semanticSymbol);

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        [WorkItem(1009638, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1009638")]
        public void ExprBodiedFunc02()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public T M<T>(T t) where T : class => /*<bind>*/t/*</bind>*/;
}");
            comp.VerifyDiagnostics();

            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(comp);

            Assert.Equal(TypeKind.TypeParameter, semanticInfo.Type.TypeKind);
            Assert.Equal("T", semanticInfo.Type.Name);
            Assert.Equal("t", semanticInfo.Symbol.Name);
            var m = semanticInfo.Symbol.ContainingSymbol as SourceMemberMethodSymbol;
            Assert.Equal(1, m.TypeParameters.Length);
            Assert.Equal(m.TypeParameters[0], semanticInfo.Type);
            Assert.Equal(m.TypeParameters[0], m.ReturnType);
            Assert.Equal(m, semanticInfo.Type.ContainingSymbol);
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
        }

        [Fact]
        public void ExprBodiedOperator01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class Program
{
    public static Program operator ++(Program p) => /*<bind>*/p/*</bind>*/;
}");
            comp.VerifyDiagnostics();

            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(comp);

            Assert.Equal("Program", semanticInfo.Type.Name);
            Assert.Equal("Program", semanticInfo.ConvertedType.Name);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            var semanticSymbol = semanticInfo.Symbol;
            var global = comp.GlobalNamespace;
            var program = global.GetTypeMember("Program");
            var method = program.GetMember<SourceUserDefinedOperatorSymbol>("op_Increment");
            var p = method.Parameters[0];

            Assert.Equal(p, semanticSymbol);

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ExprBodiedConversion01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public static C M(int i) => new C();
    public static explicit operator C(int i) => M(/*<bind>*/i/*</bind>*/);
}");
            comp.VerifyDiagnostics();

            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(comp);

            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            var semanticSymbol = semanticInfo.Symbol;
            var global = comp.GlobalNamespace;
            var program = global.GetTypeMember("C");
            var method = program.GetMember<SourceUserDefinedConversionSymbol>("op_Explicit");
            var p = method.Parameters[0];

            Assert.Equal(p, semanticSymbol);

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(1065375, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065375"), WorkItem(313, "CodePlex")]
        [Fact]
        public void Bug1065375_1()
        {
            string source = @"
using System;
public static class TestExtension 
{
    static void Main()
    {
        GetAction(1)();
    } 
   
    public static Action GetAction(int x) => () => Console.WriteLine(""GetAction {0}"", x);
}
";

            CompileAndVerify(source, expectedOutput: "GetAction 1");
        }

        [Fact, WorkItem(1069421, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1069421")]
        public void Bug1069421()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class Program
{
    private int x => () => { 
}
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

            var typeInfo1 = model.GetTypeInfo(node);
            var typeInfo2 = model.GetTypeInfo(node);

            Assert.Equal(typeInfo1.Type, typeInfo2.Type);
        }

        [WorkItem(1112875, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112875")]
        [Fact]
        public void Bug1112875()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Program
{
    private void M() => (new object());
}
");
            comp.VerifyDiagnostics(
                // (4,25): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //     private void M() => (new object());
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(new object())").WithLocation(4, 25));
        }

        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    static void M1() 
    { }
    => P1;
}
");

            comp.VerifyDiagnostics(
    // (6,5): error CS8057: Methods cannot combine block bodies with expression bodies.
    //     static void M1() 
    Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"static void M1() 
    { }
    => P1;").WithLocation(6, 5)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single().Expression;

            Assert.Equal("P1", node.ToString());
            Assert.Equal("System.Int32 C.P1 { get; set; }", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    static public int operator + (C x, C y)
    { return 1; }
    => P1;
}
");

            comp.VerifyDiagnostics(
    // (6,5): error CS8057: Methods cannot combine block bodies with expression bodies.
    //     static public int operator + (C x, C y)
    Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"static public int operator + (C x, C y)
    { return 1; }
    => P1;").WithLocation(6, 5)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single().Expression;

            Assert.Equal("P1", node.ToString());
            Assert.Equal("System.Int32 C.P1 { get; set; }", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    C()
    { }
    => P1;
}
");

            comp.VerifyDiagnostics(
    // (8,5): error CS1519: Invalid token '=>' in class, struct, or interface member declaration
    //     => P1;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=>").WithArguments("=>").WithLocation(8, 5),
    // (8,10): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     => P1;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(8, 10),
    // (8,10): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     => P1;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(8, 10)
                );

            var tree = comp.SyntaxTrees[0];
            Assert.False(tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Any());
        }

        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    ~C()
    { }
    => P1;
}
");

            comp.VerifyDiagnostics(
    // (8,5): error CS1519: Invalid token '=>' in class, struct, or interface member declaration
    //     => P1;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=>").WithArguments("=>").WithLocation(8, 5),
    // (8,10): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     => P1;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(8, 10),
    // (8,10): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     => P1;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(8, 10)
                );

            var tree = comp.SyntaxTrees[0];
            Assert.False(tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Any());
        }

        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_05()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    static public int P2
    {
        get
        { return 1; }
        => P1;
    }
}
");

            comp.VerifyDiagnostics(
    // (10,9): error CS1014: A get or set accessor expected
    //         => P1;
    Diagnostic(ErrorCode.ERR_GetOrSetExpected, "=>").WithLocation(10, 9),
    // (10,12): error CS1014: A get or set accessor expected
    //         => P1;
    Diagnostic(ErrorCode.ERR_GetOrSetExpected, "P1").WithLocation(10, 12)
                );

            var tree = comp.SyntaxTrees[0];
            Assert.False(tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Any());
        }


        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_06()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    static public int P2
    {
    }
    => P1;
}
");

            comp.VerifyDiagnostics(
    // (6,5): error CS8056: Properties cannot combine accessor lists with expression bodies.
    //     static public int P2
    Diagnostic(ErrorCode.ERR_AccessorListAndExpressionBody, @"static public int P2
    {
    }
    => P1;").WithLocation(6, 5),
    // (6,23): error CS0548: 'C.P2': property or indexer must have at least one accessor
    //     static public int P2
    Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P2").WithArguments("C.P2").WithLocation(6, 23)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single().Expression;

            Assert.Equal("P1", node.ToString());
            Assert.Null(model.GetSymbolInfo(node).Symbol);
        }

        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_07()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    static public explicit operator int (C x)
    { return 1; }
    => P1;
}
");

            comp.VerifyDiagnostics(
    // (6,5): error CS8057: Methods cannot combine block bodies with expression bodies.
    //     static public explicit operator int (C x)
    Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"static public explicit operator int (C x)
    { return 1; }
    => P1;").WithLocation(6, 5)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single().Expression;

            Assert.Equal("P1", node.ToString());
            Assert.Equal("System.Int32 C.P1 { get; set; }", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_08()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    static int M1() 
    { return P1; }
    => 1;
}
");

            comp.VerifyDiagnostics(
    // (6,5): error CS8057: Methods cannot combine block bodies with expression bodies.
    //     static int M1() 
    Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"static int M1() 
    { return P1; }
    => 1;").WithLocation(6, 5)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            Assert.Equal("P1", node.ToString());
            Assert.Equal("System.Int32 C.P1 { get; set; }", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_09()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    static public int operator + (C x, C y)
    { return P1; }
    => 1;
}
");

            comp.VerifyDiagnostics(
    // (6,5): error CS8057: Methods cannot combine block bodies with expression bodies.
    //     static public int operator + (C x, C y)
    Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"static public int operator + (C x, C y)
    { return P1; }
    => 1;").WithLocation(6, 5)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            Assert.Equal("P1", node.ToString());
            Assert.Equal("System.Int32 C.P1 { get; set; }", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_10()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    static public int P2
    {
        get { return P1; }
    }
    => 1;
}
");

            comp.VerifyDiagnostics(
    // (6,5): error CS8056: Properties cannot combine accessor lists with expression bodies.
    //     static public int P2
    Diagnostic(ErrorCode.ERR_AccessorListAndExpressionBody, @"static public int P2
    {
        get { return P1; }
    }
    => 1;").WithLocation(6, 5)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            Assert.Equal("P1", node.ToString());
            Assert.Equal("System.Int32 C.P1 { get; set; }", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(1702, "https://github.com/dotnet/roslyn/issues/1702")]
        public void BlockBodyAndExpressionBody_11()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    static int P1 {get; set;}

    static public explicit operator int (C x)
    { return P1; }
    => 1;
}
");

            comp.VerifyDiagnostics(
    // (6,5): error CS8057: Methods cannot combine block bodies with expression bodies.
    //     static public explicit operator int (C x)
    Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"static public explicit operator int (C x)
    { return P1; }
    => 1;").WithLocation(6, 5)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;

            Assert.Equal("P1", node.ToString());
            Assert.Equal("System.Int32 C.P1 { get; set; }", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(971, "https://github.com/dotnet/roslyn/issues/971")]
        public void LookupSymbols()
        {
            var comp = CreateCompilationWithMscorlib(@"
public class C
{
    Func<int, int> U() => delegate (int y0) { return 0; } 
    int V(int y1) => 1;  
}

Func<int, int> W() => delegate (int y2) { return 2; } 
");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();

            Assert.Equal(3, nodes.Length);

            for (int i = 0; i < nodes.Length; i++)
            {
                Assert.Equal($"{i}", nodes[i].ToString());
                Assert.Equal($"System.Int32 y{i}", model.LookupSymbols(nodes[i].SpanStart, name: $"y{i}").Single().ToTestDisplayString());
            }
        }
    }
}
