// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.SimplePrograms)]
    public class SimpleProgramsTests : CompilingTestBase
    {
        private static CSharpParseOptions DefaultParseOptions => TestOptions.RegularPreview;

        [Fact]
        public void Simple_01()
        {
            var text = @"System.Console.WriteLine(""Hi!"");";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            Assert.Equal("void $Program.$Main()", SimpleProgramNamedTypeSymbol.GetSimpleProgramEntryPoint(comp).ToTestDisplayString());
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void Simple_02()
        {
            var text = @"
using System;
using System.Threading.Tasks;

Console.Write(""hello "");
await Task.Factory.StartNew(() => 5);
Console.Write(""async main"");
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            Assert.Equal("System.Threading.Tasks.Task $Program.$Main()", SimpleProgramNamedTypeSymbol.GetSimpleProgramEntryPoint(comp).ToTestDisplayString());
            CompileAndVerify(comp, expectedOutput: "hello async main");
        }

        [Fact]
        public void Simple_03()
        {
            var text1 = @"
System.Console.Write(""1"");
";
            var text2 = @"
//
System.Console.Write(""2"");
System.Console.WriteLine();
System.Console.WriteLine();
";
            var text3 = @"
//
//
System.Console.Write(""3"");
System.Console.WriteLine();
System.Console.WriteLine();
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (3,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // System.Console.Write("2");
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "System").WithLocation(3, 1)
                );

            comp = CreateCompilation(new[] { text1, text2, text3 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (3,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // System.Console.Write("2");
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "System").WithLocation(3, 1),
                // (4,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // System.Console.Write("3");
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "System").WithLocation(4, 1)
                );
        }

        [Fact]
        public void Simple_04()
        {
            var text = @"
Type.M();

static class Type
{
    public static void M()
    {
        System.Console.WriteLine(""Hi!"");
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void Simple_05()
        {
            var text1 = @"
Type.M();
";
            var text2 = @"
static class Type
{
    public static void M()
    {
        System.Console.WriteLine(""Hi!"");
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "Hi!");

            comp = CreateCompilation(new[] { text2, text1 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void Simple_06()
        {
            var text1 = @"local();";
            var text2 = @"void local() => System.Console.WriteLine(2);";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");

            verifyModel(comp, comp.SyntaxTrees[0], comp.SyntaxTrees[1]);

            comp = CreateCompilation(new[] { text2, text1 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");

            verifyModel(comp, comp.SyntaxTrees[1], comp.SyntaxTrees[0]);

            static void verifyModel(CSharpCompilation comp, SyntaxTree tree1, SyntaxTree tree2)
            {
                Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel
                var model1 = comp.GetSemanticModel(tree1);

                verifyModelForGlobalStatements(tree1, model1);

                var unit1 = (CompilationUnitSyntax)tree1.GetRoot();
                var localRef = unit1.DescendantNodes().OfType<IdentifierNameSyntax>().Single();
                var refSymbol = model1.GetSymbolInfo(localRef).Symbol;
                Assert.Equal("void local()", refSymbol.ToTestDisplayString());
                Assert.Contains(refSymbol.Name, model1.LookupNames(localRef.SpanStart));
                Assert.Contains(refSymbol, model1.LookupSymbols(localRef.SpanStart));
                Assert.Same(refSymbol, model1.LookupSymbols(localRef.SpanStart, name: refSymbol.Name).Single());
                var operation1 = model1.GetOperation(localRef.Parent);
                Assert.NotNull(operation1);
                Assert.IsAssignableFrom<IInvocationOperation>(operation1);

                Assert.NotNull(ControlFlowGraph.Create((IBlockOperation)operation1.Parent.Parent));

                // PROTOTYPE(SimplePrograms): Asking IOperation for one compilation unit returns a node
                //                            for complete method body, with statements from other compilation units.
                //                            Is this going to be confusing?
                model1.VerifyOperationTree(unit1,
@"
IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: 'local();')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'local();')
    Expression: 
      IInvocationOperation (void local()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'local()')
        Instance Receiver: 
          null
        Arguments(0)
  ILocalFunctionOperation (Symbol: void local()) (OperationKind.LocalFunction, Type: null) (Syntax: 'void local( ... iteLine(2);')
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '=> System.C ... riteLine(2)')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'System.Cons ... riteLine(2)')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(2)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '=> System.C ... riteLine(2)')
        ReturnedValue: 
          null
");

                SyntaxTreeSemanticModel syntaxTreeModel = ((SyntaxTreeSemanticModel)model1);
                MemberSemanticModel mm = syntaxTreeModel.TestOnlyMemberModels[unit1];

                var root1 = (BoundBlock)mm.TestOnlyTryGetBoundNodesFromMap(unit1).Single();
                var stmt1 = mm.TestOnlyTryGetBoundNodesFromMap(unit1.Members.OfType<GlobalStatementSyntax>().Single().Statement).Single();

                var model2 = comp.GetSemanticModel(tree2);

                verifyModelForGlobalStatements(tree2, model2);

                var unit2 = (CompilationUnitSyntax)tree2.GetRoot();
                var localDecl = unit2.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
                var declSymbol = model2.GetDeclaredSymbol(localDecl);
                Assert.Same(refSymbol, declSymbol);
                Assert.Contains(declSymbol.Name, model2.LookupNames(localDecl.SpanStart));
                Assert.Contains(declSymbol, model2.LookupSymbols(localDecl.SpanStart));
                Assert.Same(declSymbol, model2.LookupSymbols(localDecl.SpanStart, name: declSymbol.Name).Single());
                var operation2 = model2.GetOperation(localDecl);
                Assert.NotNull(operation2);
                Assert.IsAssignableFrom<ILocalFunctionOperation>(operation2);

                Assert.NotNull(ControlFlowGraph.Create((IBlockOperation)operation2.Parent));

                // PROTOTYPE(SimplePrograms): Asking IOperation for one compilation unit returns a node
                //                            for complete method body, with statements from other compilation units.
                //                            Is this going to be confusing?
                //                            Note that IBlockOperation for the following tree uses different syntax
                //                            by comparison to the tree above. This is the only difference between them.
                model2.VerifyOperationTree(unit2,
@"
IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: 'void local( ... iteLine(2);')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'local();')
    Expression: 
      IInvocationOperation (void local()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'local()')
        Instance Receiver: 
          null
        Arguments(0)
  ILocalFunctionOperation (Symbol: void local()) (OperationKind.LocalFunction, Type: null) (Syntax: 'void local( ... iteLine(2);')
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '=> System.C ... riteLine(2)')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'System.Cons ... riteLine(2)')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(2)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '=> System.C ... riteLine(2)')
        ReturnedValue: 
          null
");

                Assert.True(mm.TestOnlyTryGetBoundNodesFromMap(unit2.Members.OfType<GlobalStatementSyntax>().Single().Statement).IsDefault);

                syntaxTreeModel = ((SyntaxTreeSemanticModel)model2);
                mm = syntaxTreeModel.TestOnlyMemberModels[unit2];

                var root2 = (BoundBlock)mm.TestOnlyTryGetBoundNodesFromMap(unit2).Single();
                var stmt2 = mm.TestOnlyTryGetBoundNodesFromMap(unit2.Members.OfType<GlobalStatementSyntax>().Single().Statement).Single();

                Assert.NotEqual(root1, root2);
                Assert.True(root1.Locals.Equals(root2.Locals));
                Assert.True(root1.LocalFunctions.Equals(root2.LocalFunctions));
                Assert.True(root1.Statements.Equals(root2.Statements));
                Assert.Same(stmt1, root1.Statements[0]);
                Assert.Same(stmt2, root1.Statements[1]);

                Assert.True(mm.TestOnlyTryGetBoundNodesFromMap(unit1.Members.OfType<GlobalStatementSyntax>().Single().Statement).IsDefault);

                var model3 = comp.GetSemanticModel(tree1);
                model3.GetOperation(unit1);

                syntaxTreeModel = ((SyntaxTreeSemanticModel)model3);
                mm = syntaxTreeModel.TestOnlyMemberModels[unit1];
                var root3 = (BoundBlock)mm.TestOnlyTryGetBoundNodesFromMap(unit1).Single();
                Assert.Same(root1, root3);

                var model4 = comp.GetSemanticModel(tree2);
                model4.GetOperation(unit2);

                syntaxTreeModel = ((SyntaxTreeSemanticModel)model4);
                mm = syntaxTreeModel.TestOnlyMemberModels[unit2];
                var root4 = (BoundBlock)mm.TestOnlyTryGetBoundNodesFromMap(unit2).Single();
                Assert.Same(root2, root4);

                static void verifyModelForGlobalStatements(SyntaxTree tree1, SemanticModel model1)
                {
                    var symbolInfo = model1.GetSymbolInfo(tree1.GetRoot());
                    Assert.Null(symbolInfo.Symbol);
                    Assert.Empty(symbolInfo.CandidateSymbols);
                    Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                    var typeInfo = model1.GetTypeInfo(tree1.GetRoot());
                    Assert.Null(typeInfo.Type);
                    Assert.Null(typeInfo.ConvertedType);

                    foreach (var globalStatement in tree1.GetRoot().DescendantNodes().OfType<GlobalStatementSyntax>())
                    {
                        symbolInfo = model1.GetSymbolInfo(globalStatement);
                        Assert.Null(model1.GetOperation(globalStatement));
                        Assert.Null(symbolInfo.Symbol);
                        Assert.Empty(symbolInfo.CandidateSymbols);
                        Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                        typeInfo = model1.GetTypeInfo(globalStatement);
                        Assert.Null(typeInfo.Type);
                        Assert.Null(typeInfo.ConvertedType);
                    }
                }
            }
        }

        [Fact]
        public void Simple_07()
        {
            var text1 = @"
var i = 1;
local();
";
            var text2 = @"
void local() => System.Console.WriteLine(i);
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1");

            verifyModel(comp, comp.SyntaxTrees[0], comp.SyntaxTrees[1]);

            comp = CreateCompilation(new[] { text2, text1 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1");

            verifyModel(comp, comp.SyntaxTrees[1], comp.SyntaxTrees[0]);

            static void verifyModel(CSharpCompilation comp, SyntaxTree tree1, SyntaxTree tree2)
            {
                Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

                var model1 = comp.GetSemanticModel(tree1);
                var localDecl = tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
                var declSymbol = model1.GetDeclaredSymbol(localDecl);
                Assert.Equal("System.Int32 i", declSymbol.ToTestDisplayString());
                Assert.Contains(declSymbol.Name, model1.LookupNames(localDecl.SpanStart));
                Assert.Contains(declSymbol, model1.LookupSymbols(localDecl.SpanStart));
                Assert.Same(declSymbol, model1.LookupSymbols(localDecl.SpanStart, name: declSymbol.Name).Single());
                Assert.NotNull(model1.GetOperation(tree1.GetRoot()));
                var operation1 = model1.GetOperation(localDecl);
                Assert.NotNull(operation1);
                Assert.IsAssignableFrom<IVariableDeclaratorOperation>(operation1);

                var localFuncRef = tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "local").Single();
                Assert.Contains(declSymbol.Name, model1.LookupNames(localFuncRef.SpanStart));
                Assert.Contains(declSymbol, model1.LookupSymbols(localFuncRef.SpanStart));
                Assert.Same(declSymbol, model1.LookupSymbols(localFuncRef.SpanStart, name: declSymbol.Name).Single());

                Assert.Contains(declSymbol, model1.AnalyzeDataFlow(localDecl.Ancestors().OfType<StatementSyntax>().First()).DataFlowsOut);

                var model2 = comp.GetSemanticModel(tree2);
                var localRef = tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "i").Single();
                var refSymbol = model2.GetSymbolInfo(localRef).Symbol;
                Assert.Same(declSymbol, refSymbol);
                Assert.Contains(refSymbol.Name, model2.LookupNames(localRef.SpanStart));
                Assert.Contains(refSymbol, model2.LookupSymbols(localRef.SpanStart));
                Assert.Same(refSymbol, model2.LookupSymbols(localRef.SpanStart, name: refSymbol.Name).Single());
                Assert.NotNull(model2.GetOperation(tree2.GetRoot()));
                var operation2 = model2.GetOperation(localRef);
                Assert.NotNull(operation2);
                Assert.IsAssignableFrom<ILocalReferenceOperation>(operation2);

                // PROTOTYPE(SimplePrograms): The following assert fails due to https://github.com/dotnet/roslyn/issues/41853, enable once the issue is fixed.
                //Assert.Contains(declSymbol, model2.AnalyzeDataFlow(localRef).DataFlowsIn);
            }
        }

        [Fact]
        public void Simple_08()
        {
            var text1 = @"
var i = 1;
System.Console.Write(i++);
System.Console.Write(i);
";
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "12");

            var tree1 = comp.SyntaxTrees[0];

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var model1 = comp.GetSemanticModel(tree1);
            var localDecl = tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var declSymbol = model1.GetDeclaredSymbol(localDecl);
            Assert.Equal("System.Int32 i", declSymbol.ToTestDisplayString());
            Assert.Contains(declSymbol.Name, model1.LookupNames(localDecl.SpanStart));
            Assert.Contains(declSymbol, model1.LookupSymbols(localDecl.SpanStart));
            Assert.Same(declSymbol, model1.LookupSymbols(localDecl.SpanStart, name: declSymbol.Name).Single());

            var localRefs = tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "i").ToArray();
            Assert.Equal(2, localRefs.Length);

            foreach (var localRef in localRefs)
            {
                var refSymbol = model1.GetSymbolInfo(localRef).Symbol;
                Assert.Same(declSymbol, refSymbol);
                Assert.Contains(declSymbol.Name, model1.LookupNames(localRef.SpanStart));
                Assert.Contains(declSymbol, model1.LookupSymbols(localRef.SpanStart));
                Assert.Same(declSymbol, model1.LookupSymbols(localRef.SpanStart, name: declSymbol.Name).Single());
            }
        }

        [Fact]
        public void LanguageVersion_01()
        {
            var text = @"System.Console.WriteLine(""Hi!"");";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular8);

            comp.VerifyDiagnostics(
                // (1,1): error CS8652: The feature 'simple programs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"System.Console.WriteLine(""Hi!"");").WithArguments("simple programs").WithLocation(1, 1)
                );
        }

        [Fact]
        public void WithinType_01()
        {
            var text = @"
class Test
{
    System.Console.WriteLine(""Hi!"");
}
";

            var comp = CreateCompilation(text, parseOptions: DefaultParseOptions);

            var expected = new[] {
                // (4,29): error CS1519: Invalid token '(' in class, struct, or interface member declaration
                //     System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(4, 29),
                // (4,30): error CS1031: Type expected
                //     System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_TypeExpected, @"""Hi!""").WithLocation(4, 30),
                // (4,30): error CS8124: Tuple must contain at least two elements.
                //     System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, @"""Hi!""").WithLocation(4, 30),
                // (4,30): error CS1026: ) expected
                //     System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, @"""Hi!""").WithLocation(4, 30),
                // (4,30): error CS1519: Invalid token '"Hi!"' in class, struct, or interface member declaration
                //     System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, @"""Hi!""").WithArguments(@"""Hi!""").WithLocation(4, 30)
                };

            comp.GetDiagnostics(CompilationStage.Parse, includeEarlierStages: false, cancellationToken: default).Verify(expected);
            comp.VerifyDiagnostics(expected);
        }

        [Fact]
        public void WithinNamespace_01()
        {
            var text = @"
namespace Test
{
    System.Console.WriteLine(""Hi!"");
}
";

            var comp = CreateCompilation(text, parseOptions: DefaultParseOptions);

            var expected = new[] {
                // (4,20): error CS0116: A namespace cannot directly contain members such as fields or methods
                //     System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "WriteLine").WithLocation(4, 20),
                // (4,30): error CS1026: ) expected
                //     System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, @"""Hi!""").WithLocation(4, 30),
                // (4,30): error CS1022: Type or namespace definition, or end-of-file expected
                //     System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_EOFExpected, @"""Hi!""").WithLocation(4, 30)
                };

            comp.GetDiagnostics(CompilationStage.Parse, includeEarlierStages: false, cancellationToken: default).Verify(expected);
            comp.VerifyDiagnostics(expected);
        }

        [Fact]
        public void LocalDeclarationStatement_01()
        {
            var text = @"
string s = ""Hi!"";
System.Console.WriteLine(s);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var reference = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "s").Single();

            var local = model.GetDeclaredSymbol(declarator);
            Assert.Same(local, model.GetSymbolInfo(reference).Symbol);
            Assert.Equal("System.String s", local.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, local.Kind);

            Assert.Equal(SymbolKind.Method, local.ContainingSymbol.Kind);
            Assert.True(local.ContainingSymbol.IsImplicitlyDeclared);
            Assert.Equal(SymbolKind.NamedType, local.ContainingSymbol.ContainingSymbol.Kind);
            Assert.True(local.ContainingSymbol.ContainingSymbol.IsImplicitlyDeclared);
            Assert.True(((INamespaceSymbol)local.ContainingSymbol.ContainingSymbol.ContainingSymbol).IsGlobalNamespace);
        }

        [Fact]
        public void LocalDeclarationStatement_02()
        {
            var text = @"
new string a = ""Hi!"";
System.Console.WriteLine(a);
public string b = ""Hi!"";
System.Console.WriteLine(b);
static string c = ""Hi!"";
System.Console.WriteLine(c);
readonly string d = ""Hi!"";
System.Console.WriteLine(d);
volatile string e = ""Hi!"";
System.Console.WriteLine(e);
[System.Obsolete()]
string f = ""Hi!"";
System.Console.WriteLine(f);
[System.Obsolete()]
const string g = ""Hi!"";
System.Console.WriteLine(g);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,12): error CS0116: A namespace cannot directly contain members such as fields or methods
                // new string a = "Hi!";
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "a").WithLocation(2, 12),
                // (2,12): warning CS0109: The member '<invalid-global-code>.a' does not hide an accessible member. The new keyword is not required.
                // new string a = "Hi!";
                Diagnostic(ErrorCode.WRN_NewNotRequired, "a").WithArguments("<invalid-global-code>.a").WithLocation(2, 12),
                // (3,26): error CS0103: The name 'a' does not exist in the current context
                // System.Console.WriteLine(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(3, 26),
                // (4,15): error CS0116: A namespace cannot directly contain members such as fields or methods
                // public string b = "Hi!";
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "b").WithLocation(4, 15),
                // (5,26): error CS0103: The name 'b' does not exist in the current context
                // System.Console.WriteLine(b);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(5, 26),
                // (6,1): error CS0106: The modifier 'static' is not valid for this item
                // static string c = "Hi!";
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(6, 1),
                // (8,1): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly string d = "Hi!";
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(8, 1),
                // (10,1): error CS0106: The modifier 'volatile' is not valid for this item
                // volatile string e = "Hi!";
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "volatile").WithArguments("volatile").WithLocation(10, 1),
                // (13,8): error CS0116: A namespace cannot directly contain members such as fields or methods
                // string f = "Hi!";
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "f").WithLocation(13, 8),
                // (14,26): error CS0103: The name 'f' does not exist in the current context
                // System.Console.WriteLine(f);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(14, 26),
                // (16,14): error CS0116: A namespace cannot directly contain members such as fields or methods
                // const string g = "Hi!";
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "g").WithLocation(16, 14),
                // (17,26): error CS0103: The name 'g' does not exist in the current context
                // System.Console.WriteLine(g);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g").WithLocation(17, 26)
                );
        }

        [Fact]
        public void LocalDeclarationStatement_03()
        {
            var text = @"
string a = ""1"";
string a = ""2"";
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,8): warning CS0219: The variable 'a' is assigned but its value is never used
                // string a = "1";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a").WithLocation(2, 8),
                // (3,8): error CS0128: A local variable or function named 'a' is already defined in this scope
                // string a = "2";
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "a").WithArguments("a").WithLocation(3, 8),
                // (3,8): warning CS0219: The variable 'a' is assigned but its value is never used
                // string a = "2";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a").WithLocation(3, 8)
                );
        }

        [Fact]
        public void LocalDeclarationStatement_04()
        {
            var text = @"
using System;
using System.Threading.Tasks;

var s = await local();
System.Console.WriteLine(s);

async Task<string> local()
{
    await Task.Factory.StartNew(() => 5);
    return ""Hi!"";
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void LocalDeclarationStatement_05()
        {
            var text = @"
const string s = ""Hi!"";
System.Console.WriteLine(s);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void LocalDeclarationStatement_06()
        {
            var text = @"
a.ToString();
string a = ""2"";
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS0841: Cannot use local variable 'a' before it is declared
                // a.ToString();
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "a").WithArguments("a").WithLocation(2, 1)
                );
        }

        [Fact]
        public void LocalDeclarationStatement_07()
        {
            var text1 = @"
string x = ""1"";
System.Console.Write(x);
";
            var text2 = @"
int x = 1;
System.Console.Write(x);
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // int x = 1;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "int").WithLocation(2, 1),
                // (2,5): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // int x = 1;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(2, 5),
                // (2,5): warning CS0219: The variable 'x' is assigned but its value is never used
                // int x = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(2, 5)
                );

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var symbol1 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single());
            Assert.Equal("System.String x", symbol1.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single()).Symbol);

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            var symbol2 = model2.GetDeclaredSymbol(tree2.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single());
            Assert.Equal("System.Int32 x", symbol2.ToTestDisplayString());
            Assert.Same(symbol1, model2.GetSymbolInfo(tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single()).Symbol);
        }

        [Fact]
        public void LocalUsedBeforeDeclaration_01()
        {
            var text1 = @"
const string x = y;
System.Console.Write(x);
";
            var text2 = @"
const string y = x;
System.Console.Write(y);
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // const string y = x;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "const").WithLocation(2, 1),
                // (2,18): error CS0841: Cannot use local variable 'y' before it is declared
                // const string x = y;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(2, 18)
                );

            comp = CreateCompilation(new[] { "System.Console.WriteLine();", text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // const string x = y;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "const").WithLocation(2, 1),
                // (2,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // const string y = x;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "const").WithLocation(2, 1),
                // (2,18): error CS0841: Cannot use local variable 'y' before it is declared
                // const string x = y;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(2, 18)
                );
        }

        [Fact]
        public void LocalUsedBeforeDeclaration_02()
        {
            var text1 = @"
var x = y;
System.Console.Write(x);
";
            var text2 = @"
var y = x;
System.Console.Write(y);
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // var y = x;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "var").WithLocation(2, 1),
                // (2,9): error CS0841: Cannot use local variable 'y' before it is declared
                // var x = y;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(2, 9)
                );

            comp = CreateCompilation(new[] { "System.Console.WriteLine();", text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // var x = y;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "var").WithLocation(2, 1),
                // (2,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // var y = x;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "var").WithLocation(2, 1),
                // (2,9): error CS0841: Cannot use local variable 'y' before it is declared
                // var x = y;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(2, 9)
                );
        }

        [Fact]
        public void LocalUsedBeforeDeclaration_03()
        {
            var text1 = @"
string x = ""x"";
System.Console.Write(x);
";
            var text2 = @"
class C1
{
    void Test()
    {
        System.Console.Write(x);
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,30): error CS9000: Cannot use local variable or local function 'x' declared in a top-level statement in this context.
                //         System.Console.Write(x);
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "x").WithArguments("x").WithLocation(6, 30)
                );

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            var nameRef = tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            var symbol2 = model2.GetSymbolInfo(nameRef).Symbol;
            Assert.Equal("System.String x", symbol2.ToTestDisplayString());
            Assert.Equal("System.String", model2.GetTypeInfo(nameRef).Type.ToTestDisplayString());
            Assert.Null(model2.GetOperation(tree2.GetRoot()));

            comp = CreateCompilation(new[] { text2, text1 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,30): error CS9000: Cannot use local variable or local function 'x' declared in a top-level statement in this context.
                //         System.Console.Write(x);
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "x").WithArguments("x").WithLocation(6, 30)
                );

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            tree2 = comp.SyntaxTrees[0];
            model2 = comp.GetSemanticModel(tree2);
            nameRef = tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            symbol2 = model2.GetSymbolInfo(nameRef).Symbol;
            Assert.Equal("System.String x", symbol2.ToTestDisplayString());
            Assert.Equal("System.String", model2.GetTypeInfo(nameRef).Type.ToTestDisplayString());
            Assert.Null(model2.GetOperation(tree2.GetRoot()));
        }

        [Fact]
        public void LocalUsedBeforeDeclaration_04()
        {
            var text1 = @"
string x = ""x"";
local();
";
            var text2 = @"
void local()
{
    System.Console.Write(x);
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "x");

            comp = CreateCompilation(new[] { text2, text1 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "x");
        }

        [Fact]
        public void FlowAnalysis_01()
        {
            var text = @"
#nullable enable
string a = ""1"";
string? b;
System.Console.WriteLine(b);
string? c = null;
c.ToString();
d: System.Console.WriteLine();
string e() => ""1"";

";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (3,8): warning CS0219: The variable 'a' is assigned but its value is never used
                // string a = "1";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a").WithLocation(3, 8),
                // (5,26): error CS0165: Use of unassigned local variable 'b'
                // System.Console.WriteLine(b);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "b").WithArguments("b").WithLocation(5, 26),
                // (7,1): warning CS8602: Dereference of a possibly null reference.
                // c.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(7, 1),
                // (8,1): warning CS0164: This label has not been referenced
                // d: System.Console.WriteLine();
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "d").WithLocation(8, 1),
                // (9,8): warning CS8321: The local function 'e' is declared but never used
                // string e() => "1";
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "e").WithArguments("e").WithLocation(9, 8)
                );
        }

        [Fact]
        public void Scope_01()
        {
            var text = @"
using alias1 = Test;

string Test = ""1"";
System.Console.WriteLine(Test);

class Test {}

class Derived : Test
{
    void M()
    {
        Test x = null;
        alias1 y = x;
        System.Console.WriteLine(y);
        System.Console.WriteLine(Test); // 1
        Test.ToString(); // 2
        Test.EndsWith(null); // 3
        _ = nameof(Test); // 4
    }
}

namespace N1
{
    using alias2 = Test;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            System.Console.WriteLine(Test); // 5
            Test.ToString(); // 6
            Test.EndsWith(null); // 7
            _ = nameof(Test); // 8
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,20): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 20),
                // (34,38): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(34, 38),
                // (35,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test.ToString(); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(35, 13),
                // (36,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test.EndsWith(null); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(36, 13),
                // (37,24): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             _ = nameof(Test); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(37, 24)
                );

            var getHashCode = ((Compilation)comp).GetMember("System.Object." + nameof(GetHashCode));
            var testType = ((Compilation)comp).GetTypeByMetadataName("Test");

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var localDecl = tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var declSymbol = model1.GetDeclaredSymbol(localDecl);
            Assert.Equal("System.String Test", declSymbol.ToTestDisplayString());
            var names = model1.LookupNames(localDecl.SpanStart);
            Assert.Contains(getHashCode.Name, names);
            var symbols = model1.LookupSymbols(localDecl.SpanStart);
            Assert.Contains(getHashCode, symbols);
            Assert.Same(getHashCode, model1.LookupSymbols(localDecl.SpanStart, name: getHashCode.Name).Single());

            Assert.Contains("Test", names);
            Assert.DoesNotContain(testType, symbols);
            Assert.Contains(declSymbol, symbols);
            Assert.Same(declSymbol, model1.LookupSymbols(localDecl.SpanStart, name: "Test").Single());

            symbols = model1.LookupNamespacesAndTypes(localDecl.SpanStart);
            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model1.LookupNamespacesAndTypes(localDecl.SpanStart, name: "Test").Single());

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var nameRefs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Test").ToArray();

            var nameRef = nameRefs[0];
            Assert.Equal("using alias1 = Test;", nameRef.Parent.ToString());

            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            names = model.LookupNames(nameRef.SpanStart);
            Assert.DoesNotContain(getHashCode.Name, names);
            Assert.Contains("Test", names);

            symbols = model.LookupSymbols(nameRef.SpanStart);
            Assert.DoesNotContain(getHashCode, symbols);
            Assert.Empty(model.LookupSymbols(nameRef.SpanStart, name: getHashCode.Name));

            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model.LookupSymbols(nameRef.SpanStart, name: "Test").Single());

            nameRef = nameRefs[2];
            Assert.Equal(": Test", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            Assert.DoesNotContain(getHashCode.Name, model.LookupNames(nameRef.SpanStart));
            verifyModel(declSymbol, model, nameRef);

            nameRef = nameRefs[4];
            Assert.Equal("System.Console.WriteLine(Test)", nameRef.Parent.Parent.Parent.ToString());
            Assert.Same(declSymbol, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model, nameRef);

            nameRef = nameRefs[8];
            Assert.Equal("using alias2 = Test;", nameRef.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model, nameRef);

            nameRef = nameRefs[9];
            Assert.Equal(": Test", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            Assert.DoesNotContain(getHashCode.Name, model.LookupNames(nameRef.SpanStart));
            verifyModel(declSymbol, model, nameRef);

            nameRef = nameRefs[11];
            Assert.Equal("System.Console.WriteLine(Test)", nameRef.Parent.Parent.Parent.ToString());
            Assert.Same(declSymbol, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model, nameRef);

            void verifyModel(ISymbol declSymbol, SemanticModel model, IdentifierNameSyntax nameRef)
            {
                var names = model.LookupNames(nameRef.SpanStart);
                Assert.Contains("Test", names);

                var symbols = model.LookupSymbols(nameRef.SpanStart);
                Assert.DoesNotContain(testType, symbols);
                Assert.Contains(declSymbol, symbols);
                Assert.Same(declSymbol, model.LookupSymbols(nameRef.SpanStart, name: "Test").Single());

                symbols = model.LookupNamespacesAndTypes(nameRef.SpanStart);
                Assert.Contains(testType, symbols);
                Assert.DoesNotContain(declSymbol, symbols);
                Assert.Same(testType, model.LookupNamespacesAndTypes(nameRef.SpanStart, name: "Test").Single());
            }
        }

        [Fact]
        public void Scope_02()
        {
            var text1 = @"
string Test = ""1"";
System.Console.WriteLine(Test);
";
            var text2 = @"
using alias1 = Test;

class Test {}

class Derived : Test
{
    void M()
    {
        Test x = null;
        alias1 y = x;
        System.Console.WriteLine(y);
        System.Console.WriteLine(Test); // 1
        Test.ToString(); // 2
        Test.EndsWith(null); // 3
        _ = nameof(Test); // 4
    }
}

namespace N1
{
    using alias2 = Test;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            System.Console.WriteLine(Test); // 5
            Test.ToString(); // 6
            Test.EndsWith(null); // 7
            _ = nameof(Test); // 8
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (13,34): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,20): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 20),
                // (31,38): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(31, 38),
                // (32,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test.ToString(); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(32, 13),
                // (33,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test.EndsWith(null); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(33, 13),
                // (34,24): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             _ = nameof(Test); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(34, 24)
                );

            var getHashCode = ((Compilation)comp).GetMember("System.Object." + nameof(GetHashCode));
            var testType = ((Compilation)comp).GetTypeByMetadataName("Test");

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var localDecl = tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var declSymbol = model1.GetDeclaredSymbol(localDecl);
            Assert.Equal("System.String Test", declSymbol.ToTestDisplayString());
            var names = model1.LookupNames(localDecl.SpanStart);
            Assert.Contains(getHashCode.Name, names);
            var symbols = model1.LookupSymbols(localDecl.SpanStart);
            Assert.Contains(getHashCode, symbols);
            Assert.Same(getHashCode, model1.LookupSymbols(localDecl.SpanStart, name: getHashCode.Name).Single());

            Assert.Contains("Test", names);
            Assert.DoesNotContain(testType, symbols);
            Assert.Contains(declSymbol, symbols);
            Assert.Same(declSymbol, model1.LookupSymbols(localDecl.SpanStart, name: "Test").Single());

            symbols = model1.LookupNamespacesAndTypes(localDecl.SpanStart);
            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model1.LookupNamespacesAndTypes(localDecl.SpanStart, name: "Test").Single());

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            var nameRefs = tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Test").ToArray();

            var nameRef = nameRefs[0];
            Assert.Equal("using alias1 = Test;", nameRef.Parent.ToString());

            Assert.Same(testType, model2.GetSymbolInfo(nameRef).Symbol);
            names = model2.LookupNames(nameRef.SpanStart);
            Assert.DoesNotContain(getHashCode.Name, names);
            Assert.Contains("Test", names);

            symbols = model2.LookupSymbols(nameRef.SpanStart);
            Assert.DoesNotContain(getHashCode, symbols);
            Assert.Empty(model2.LookupSymbols(nameRef.SpanStart, name: getHashCode.Name));

            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model2.LookupSymbols(nameRef.SpanStart, name: "Test").Single());

            nameRef = nameRefs[1];
            Assert.Equal(": Test", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model2.GetSymbolInfo(nameRef).Symbol);
            Assert.DoesNotContain(getHashCode.Name, model2.LookupNames(nameRef.SpanStart));
            verifyModel(declSymbol, model2, nameRef);

            nameRef = nameRefs[3];
            Assert.Equal("System.Console.WriteLine(Test)", nameRef.Parent.Parent.Parent.ToString());
            Assert.Same(declSymbol, model2.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model2, nameRef);

            nameRef = nameRefs[7];
            Assert.Equal("using alias2 = Test;", nameRef.Parent.ToString());
            Assert.Same(testType, model2.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model2, nameRef);

            nameRef = nameRefs[8];
            Assert.Equal(": Test", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model2.GetSymbolInfo(nameRef).Symbol);
            Assert.DoesNotContain(getHashCode.Name, model2.LookupNames(nameRef.SpanStart));
            verifyModel(declSymbol, model2, nameRef);

            nameRef = nameRefs[10];
            Assert.Equal("System.Console.WriteLine(Test)", nameRef.Parent.Parent.Parent.ToString());
            Assert.Same(declSymbol, model2.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model2, nameRef);

            void verifyModel(ISymbol declSymbol, SemanticModel model2, IdentifierNameSyntax nameRef)
            {
                var names = model2.LookupNames(nameRef.SpanStart);
                Assert.Contains("Test", names);

                var symbols = model2.LookupSymbols(nameRef.SpanStart);
                Assert.DoesNotContain(testType, symbols);
                Assert.Contains(declSymbol, symbols);
                Assert.Same(declSymbol, model2.LookupSymbols(nameRef.SpanStart, name: "Test").Single());

                symbols = model2.LookupNamespacesAndTypes(nameRef.SpanStart);
                Assert.Contains(testType, symbols);
                Assert.DoesNotContain(declSymbol, symbols);
                Assert.Same(testType, model2.LookupNamespacesAndTypes(nameRef.SpanStart, name: "Test").Single());
            }
        }

        [Fact]
        public void Scope_03()
        {
            var text1 = @"
string Test = ""1"";
System.Console.WriteLine(Test);
";
            var text2 = @"
class Test {}

class Derived : Test
{
    void M()
    {
        int Test = 0;
        System.Console.WriteLine(Test++);
    }
}

namespace N1
{
    class Derived : Test
    {
        void M()
        {
            int Test = 1;
            System.Console.WriteLine(Test++);
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(text1 + text2, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();

            Assert.Throws<System.ArgumentException>(() => CreateCompilation(new[] { Parse(text1, filename: "text1", DefaultParseOptions),
                                                                                    Parse(text1, filename: "text2", TestOptions.Regular6) },
                                                                            options: TestOptions.DebugExe));

            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (2,1): error CS8652: The feature 'simple programs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // string Test = "1";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"string Test = ""1"";").WithArguments("simple programs").WithLocation(2, 1)
                );
        }

        [Fact]
        public void Scope_04()
        {
            var text = @"
using alias1 = Test;

string Test() => ""1"";
System.Console.WriteLine(Test());

class Test {}

class Derived : Test
{
    void M()
    {
        Test x = null;
        alias1 y = x;
        System.Console.WriteLine(y);
        System.Console.WriteLine(Test()); // 1
        Test().ToString(); // 2
        Test().EndsWith(null); // 3
        System.Func<string> d = Test; // 4
        d();
        _ = nameof(Test); // 5
    }
}

namespace N1
{
    using alias2 = Test;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            System.Console.WriteLine(Test()); // 6
            Test().ToString(); // 7
            Test().EndsWith(null); // 8
            var d = new System.Func<string>(Test); // 9
            d();
            _ = nameof(Test); // 10
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,33): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 33),
                // (21,20): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(21, 20),
                // (36,38): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test()); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(36, 38),
                // (37,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().ToString(); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(37, 13),
                // (38,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().EndsWith(null); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(38, 13),
                // (39,45): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             var d = new System.Func<string>(Test); // 9
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(39, 45),
                // (41,24): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             _ = nameof(Test); // 10
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(41, 24)
                );

            var testType = ((Compilation)comp).GetTypeByMetadataName("Test");

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var localDecl = tree1.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var declSymbol = model1.GetDeclaredSymbol(localDecl);
            Assert.Equal("System.String Test()", declSymbol.ToTestDisplayString());
            var names = model1.LookupNames(localDecl.SpanStart);
            var symbols = model1.LookupSymbols(localDecl.SpanStart);

            Assert.Contains("Test", names);
            Assert.DoesNotContain(testType, symbols);
            Assert.Contains(declSymbol, symbols);
            Assert.Same(declSymbol, model1.LookupSymbols(localDecl.SpanStart, name: "Test").Single());

            symbols = model1.LookupNamespacesAndTypes(localDecl.SpanStart);
            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model1.LookupNamespacesAndTypes(localDecl.SpanStart, name: "Test").Single());

            var nameRefs = tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Test").ToArray();

            var nameRef = nameRefs[0];
            Assert.Equal("using alias1 = Test;", nameRef.Parent.ToString());

            Assert.Same(testType, model1.GetSymbolInfo(nameRef).Symbol);
            names = model1.LookupNames(nameRef.SpanStart);
            Assert.Contains("Test", names);

            symbols = model1.LookupSymbols(nameRef.SpanStart);

            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model1.LookupSymbols(nameRef.SpanStart, name: "Test").Single());

            nameRef = nameRefs[2];
            Assert.Equal(": Test", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model1.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model1, nameRef);

            nameRef = nameRefs[4];
            Assert.Equal("System.Console.WriteLine(Test())", nameRef.Parent.Parent.Parent.Parent.ToString());
            Assert.Same(declSymbol, model1.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model1, nameRef);

            nameRef = nameRefs[9];
            Assert.Equal("using alias2 = Test;", nameRef.Parent.ToString());
            Assert.Same(testType, model1.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model1, nameRef);

            nameRef = nameRefs[10];
            Assert.Equal(": Test", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model1.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model1, nameRef);

            nameRef = nameRefs[12];
            Assert.Equal("System.Console.WriteLine(Test())", nameRef.Parent.Parent.Parent.Parent.ToString());
            Assert.Same(declSymbol, model1.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model1, nameRef);

            void verifyModel(ISymbol declSymbol, SemanticModel model2, IdentifierNameSyntax nameRef)
            {
                var names = model2.LookupNames(nameRef.SpanStart);
                Assert.Contains("Test", names);

                var symbols = model2.LookupSymbols(nameRef.SpanStart);
                Assert.DoesNotContain(testType, symbols);
                Assert.Contains(declSymbol, symbols);
                Assert.Same(declSymbol, model2.LookupSymbols(nameRef.SpanStart, name: "Test").Single());

                symbols = model2.LookupNamespacesAndTypes(nameRef.SpanStart);
                Assert.Contains(testType, symbols);
                Assert.DoesNotContain(declSymbol, symbols);
                Assert.Same(testType, model2.LookupNamespacesAndTypes(nameRef.SpanStart, name: "Test").Single());
            }
        }

        [Fact]
        public void Scope_05()
        {
            var text1 = @"
string Test() => ""1"";
System.Console.WriteLine(Test());
";
            var text2 = @"
using alias1 = Test;

class Test {}

class Derived : Test
{
    void M()
    {
        Test x = null;
        alias1 y = x;
        System.Console.WriteLine(y);
        System.Console.WriteLine(Test()); // 1
        Test().ToString(); // 2
        Test().EndsWith(null); // 3
        System.Func<string> d = Test; // 4
        d();
        _ = nameof(Test); // 5
    }
}

namespace N1
{
    using alias2 = Test;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            System.Console.WriteLine(Test()); // 6
            Test().ToString(); // 7
            Test().EndsWith(null); // 8
            var d = new System.Func<string>(Test); // 9
            d();
            _ = nameof(Test); // 10
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (13,34): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,33): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 33),
                // (18,20): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 20),
                // (33,38): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test()); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(33, 38),
                // (34,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().ToString(); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(34, 13),
                // (35,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().EndsWith(null); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(35, 13),
                // (36,45): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             var d = new System.Func<string>(Test); // 9
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(36, 45),
                // (38,24): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             _ = nameof(Test); // 10
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(38, 24)
                );

            var testType = ((Compilation)comp).GetTypeByMetadataName("Test");

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var localDecl = tree1.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var declSymbol = model1.GetDeclaredSymbol(localDecl);
            Assert.Equal("System.String Test()", declSymbol.ToTestDisplayString());
            var names = model1.LookupNames(localDecl.SpanStart);
            var symbols = model1.LookupSymbols(localDecl.SpanStart);

            Assert.Contains("Test", names);
            Assert.DoesNotContain(testType, symbols);
            Assert.Contains(declSymbol, symbols);
            Assert.Same(declSymbol, model1.LookupSymbols(localDecl.SpanStart, name: "Test").Single());

            symbols = model1.LookupNamespacesAndTypes(localDecl.SpanStart);
            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model1.LookupNamespacesAndTypes(localDecl.SpanStart, name: "Test").Single());

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            var nameRefs = tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Test").ToArray();

            var nameRef = nameRefs[0];
            Assert.Equal("using alias1 = Test;", nameRef.Parent.ToString());

            Assert.Same(testType, model2.GetSymbolInfo(nameRef).Symbol);
            names = model2.LookupNames(nameRef.SpanStart);
            Assert.Contains("Test", names);

            symbols = model2.LookupSymbols(nameRef.SpanStart);

            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model2.LookupSymbols(nameRef.SpanStart, name: "Test").Single());

            nameRef = nameRefs[1];
            Assert.Equal(": Test", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model2.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model2, nameRef);

            nameRef = nameRefs[3];
            Assert.Equal("System.Console.WriteLine(Test())", nameRef.Parent.Parent.Parent.Parent.ToString());
            Assert.Same(declSymbol, model2.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model2, nameRef);

            nameRef = nameRefs[8];
            Assert.Equal("using alias2 = Test;", nameRef.Parent.ToString());
            Assert.Same(testType, model2.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model2, nameRef);

            nameRef = nameRefs[9];
            Assert.Equal(": Test", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model2.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model2, nameRef);

            nameRef = nameRefs[11];
            Assert.Equal("System.Console.WriteLine(Test())", nameRef.Parent.Parent.Parent.Parent.ToString());
            Assert.Same(declSymbol, model2.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model2, nameRef);

            void verifyModel(ISymbol declSymbol, SemanticModel model2, IdentifierNameSyntax nameRef)
            {
                var names = model2.LookupNames(nameRef.SpanStart);
                Assert.Contains("Test", names);

                var symbols = model2.LookupSymbols(nameRef.SpanStart);
                Assert.DoesNotContain(testType, symbols);
                Assert.Contains(declSymbol, symbols);
                Assert.Same(declSymbol, model2.LookupSymbols(nameRef.SpanStart, name: "Test").Single());

                symbols = model2.LookupNamespacesAndTypes(nameRef.SpanStart);
                Assert.Contains(testType, symbols);
                Assert.DoesNotContain(declSymbol, symbols);
                Assert.Same(testType, model2.LookupNamespacesAndTypes(nameRef.SpanStart, name: "Test").Single());
            }
        }

        [Fact]
        public void Scope_06()
        {
            var text1 = @"
string Test() => ""1"";
System.Console.WriteLine(Test());
";
            var text2 = @"
class Test {}

class Derived : Test
{
    void M()
    {
        int Test() => 1;
        int x = Test() + 1;
        System.Console.WriteLine(x);
    }
}

namespace N1
{
    class Derived : Test
    {
        void M()
        {
            int Test() => 1;
            int x = Test() + 1;
            System.Console.WriteLine(x);
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(text1 + text2, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (2,1): error CS8652: The feature 'simple programs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // string Test() => "1";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"string Test() => ""1"";").WithArguments("simple programs").WithLocation(2, 1)
                );
        }

        [Fact]
        public void Scope_07()
        {
            var text = @"
using alias1 = Test;
goto Test;
Test: System.Console.WriteLine(""1"");

class Test {}

class Derived : Test
{
    void M()
    {
        Test x = null;
        alias1 y = x;
        System.Console.WriteLine(y);
        goto Test; // 1
    }
}

namespace N1
{
    using alias2 = Test;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            goto Test; // 2
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (15,14): error CS0159: No such label 'Test' within the scope of the goto statement
                //         goto Test; // 1
                Diagnostic(ErrorCode.ERR_LabelNotFound, "Test").WithArguments("Test").WithLocation(15, 14),
                // (30,18): error CS0159: No such label 'Test' within the scope of the goto statement
                //             goto Test; // 2
                Diagnostic(ErrorCode.ERR_LabelNotFound, "Test").WithArguments("Test").WithLocation(30, 18)
                );

            var testType = ((Compilation)comp).GetTypeByMetadataName("Test");

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var labelDecl = tree1.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
            var declSymbol = model1.GetDeclaredSymbol(labelDecl);
            Assert.Equal("Test", declSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Label, declSymbol.Kind);
            var names = model1.LookupNames(labelDecl.SpanStart);
            var symbols = model1.LookupSymbols(labelDecl.SpanStart);

            Assert.Contains("Test", names);
            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model1.LookupSymbols(labelDecl.SpanStart, name: "Test").Single());

            symbols = model1.LookupNamespacesAndTypes(labelDecl.SpanStart);
            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model1.LookupNamespacesAndTypes(labelDecl.SpanStart, name: "Test").Single());

            Assert.Same(declSymbol, model1.LookupLabels(labelDecl.SpanStart).Single());
            Assert.Same(declSymbol, model1.LookupLabels(labelDecl.SpanStart, name: "Test").Single());

            var nameRefs = tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Test").ToArray();

            var nameRef = nameRefs[0];
            Assert.Equal("using alias1 = Test;", nameRef.Parent.ToString());

            Assert.Same(testType, model1.GetSymbolInfo(nameRef).Symbol);
            names = model1.LookupNames(nameRef.SpanStart);
            Assert.Contains("Test", names);

            symbols = model1.LookupSymbols(nameRef.SpanStart);

            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model1.LookupSymbols(nameRef.SpanStart, name: "Test").Single());
            Assert.Empty(model1.LookupLabels(nameRef.SpanStart));
            Assert.Empty(model1.LookupLabels(nameRef.SpanStart, name: "Test"));

            nameRef = nameRefs[1];
            Assert.Equal("goto Test;", nameRef.Parent.ToString());
            Assert.Same(declSymbol, model1.GetSymbolInfo(nameRef).Symbol);

            names = model1.LookupNames(nameRef.SpanStart);
            Assert.Contains("Test", names);

            symbols = model1.LookupSymbols(nameRef.SpanStart);

            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(declSymbol, symbols);
            Assert.Same(testType, model1.LookupSymbols(nameRef.SpanStart, name: "Test").Single());
            Assert.Same(declSymbol, model1.LookupLabels(nameRef.SpanStart).Single());
            Assert.Same(declSymbol, model1.LookupLabels(nameRef.SpanStart, name: "Test").Single());

            nameRef = nameRefs[2];
            Assert.Equal(": Test", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model1.GetSymbolInfo(nameRef).Symbol);
            verifyModel(model1, nameRef);

            nameRef = nameRefs[4];
            Assert.Equal("goto Test;", nameRef.Parent.ToString());
            Assert.Null(model1.GetSymbolInfo(nameRef).Symbol);
            verifyModel(model1, nameRef);

            nameRef = nameRefs[5];
            Assert.Equal("using alias2 = Test;", nameRef.Parent.ToString());
            Assert.Same(testType, model1.GetSymbolInfo(nameRef).Symbol);
            verifyModel(model1, nameRef);

            nameRef = nameRefs[6];
            Assert.Equal(": Test", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model1.GetSymbolInfo(nameRef).Symbol);
            verifyModel(model1, nameRef);

            nameRef = nameRefs[8];
            Assert.Null(model1.GetSymbolInfo(nameRef).Symbol);
            Assert.Equal("goto Test;", nameRef.Parent.ToString());
            verifyModel(model1, nameRef);

            void verifyModel(SemanticModel model2, IdentifierNameSyntax nameRef)
            {
                var names = model2.LookupNames(nameRef.SpanStart);
                Assert.Contains("Test", names);

                var symbols = model2.LookupSymbols(nameRef.SpanStart);
                Assert.Contains(testType, symbols);
                Assert.DoesNotContain(declSymbol, symbols);
                Assert.Same(testType, model2.LookupSymbols(nameRef.SpanStart, name: "Test").Single());

                symbols = model2.LookupNamespacesAndTypes(nameRef.SpanStart);
                Assert.Contains(testType, symbols);
                Assert.DoesNotContain(declSymbol, symbols);
                Assert.Same(testType, model2.LookupNamespacesAndTypes(nameRef.SpanStart, name: "Test").Single());

                Assert.Empty(model2.LookupLabels(nameRef.SpanStart));
                Assert.Empty(model2.LookupLabels(nameRef.SpanStart, name: "Test"));
            }
        }

        [Fact]
        public void Scope_08()
        {
            var text = @"
goto Test;
Test: System.Console.WriteLine(""1"");

class Test {}

class Derived : Test
{
    void M()
    {
        goto Test;
        Test: System.Console.WriteLine();
    }
}

namespace N1
{
    class Derived : Test
    {
        void M()
        {
            goto Test;
            Test: System.Console.WriteLine();
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Scope_09()
        {
            var text = @"
string Test = ""1"";
System.Console.WriteLine(Test);

new void M()
{
    int Test = 0;
    System.Console.WriteLine(Test++);
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (5,10): error CS0116: A namespace cannot directly contain members such as fields or methods
                // new void M()
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "M").WithLocation(5, 10),
                // (5,10): warning CS0109: The member '<invalid-global-code>.M()' does not hide an accessible member. The new keyword is not required.
                // new void M()
                Diagnostic(ErrorCode.WRN_NewNotRequired, "M").WithArguments("<invalid-global-code>.M()").WithLocation(5, 10)
                );
        }

        [Fact]
        public void Scope_10()
        {
            var text = @"
string Test = ""1"";
System.Console.WriteLine(Test);

new int F = C1.GetInt(out var Test);

class C1
{
    public static int GetInt(out int v)
    {
        v = 1;
        return v;
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (5,9): error CS0116: A namespace cannot directly contain members such as fields or methods
                // new int F = C1.GetInt(out var Test);
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "F").WithLocation(5, 9),
                // (5,9): warning CS0109: The member '<invalid-global-code>.F' does not hide an accessible member. The new keyword is not required.
                // new int F = C1.GetInt(out var Test);
                Diagnostic(ErrorCode.WRN_NewNotRequired, "F").WithArguments("<invalid-global-code>.F").WithLocation(5, 9)
                );
        }

        [Fact]
        public void Scope_11()
        {
            var text = @"
goto Test;
Test: System.Console.WriteLine();

new void M()
{
    goto Test;
    Test: System.Console.WriteLine();
}";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (5,10): error CS0116: A namespace cannot directly contain members such as fields or methods
                // new void M()
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "M").WithLocation(5, 10),
                // (5,10): warning CS0109: The member '<invalid-global-code>.M()' does not hide an accessible member. The new keyword is not required.
                // new void M()
                Diagnostic(ErrorCode.WRN_NewNotRequired, "M").WithArguments("<invalid-global-code>.M()").WithLocation(5, 10)
                );
        }

        [Fact]
        public void Scope_12()
        {
            var text = @"
using alias1 = Test;

string Test() => ""1"";
System.Console.WriteLine(Test());

class Test {}

struct Derived
{
    void M()
    {
        Test x = null;
        alias1 y = x;
        System.Console.WriteLine(y);
        System.Console.WriteLine(Test()); // 1
        Test().ToString(); // 2
        Test().EndsWith(null); // 3
        System.Func<string> d = Test; // 4
        d();
        _ = nameof(Test); // 5
    }
}

namespace N1
{
    using alias2 = Test;

    struct Derived
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            System.Console.WriteLine(Test()); // 6
            Test().ToString(); // 7
            Test().EndsWith(null); // 8
            var d = new System.Func<string>(Test); // 9
            d();
            _ = nameof(Test); // 10
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,33): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 33),
                // (21,20): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(21, 20),
                // (36,38): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test()); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(36, 38),
                // (37,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().ToString(); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(37, 13),
                // (38,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().EndsWith(null); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(38, 13),
                // (39,45): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             var d = new System.Func<string>(Test); // 9
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(39, 45),
                // (41,24): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             _ = nameof(Test); // 10
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(41, 24)
                );
        }

        [Fact]
        public void Scope_13()
        {
            var text = @"
using alias1 = Test;

string Test() => ""1"";
System.Console.WriteLine(Test());

class Test {}

interface Derived
{
    void M()
    {
        Test x = null;
        alias1 y = x;
        System.Console.WriteLine(y);
        System.Console.WriteLine(Test()); // 1
        Test().ToString(); // 2
        Test().EndsWith(null); // 3
        System.Func<string> d = Test; // 4
        d();
        _ = nameof(Test); // 5
    }
}

namespace N1
{
    using alias2 = Test;

    interface Derived
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            System.Console.WriteLine(Test()); // 6
            Test().ToString(); // 7
            Test().EndsWith(null); // 8
            var d = new System.Func<string>(Test); // 9
            d();
            _ = nameof(Test); // 10
        }
    }
}
";

            var comp = CreateCompilation(text, targetFramework: TargetFramework.NetStandardLatest, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,33): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 33),
                // (21,20): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(21, 20),
                // (36,38): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test()); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(36, 38),
                // (37,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().ToString(); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(37, 13),
                // (38,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().EndsWith(null); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(38, 13),
                // (39,45): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             var d = new System.Func<string>(Test); // 9
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(39, 45),
                // (41,24): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             _ = nameof(Test); // 10
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(41, 24)
                );
        }

        [Fact]
        public void Scope_14()
        {
            var text = @"
using alias1 = Test;

string Test() => ""1"";
System.Console.WriteLine(Test());

class Test {}

delegate Test D(alias1 x);

namespace N1
{
    using alias2 = Test;

    delegate Test D(alias2 x);
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Scope_15()
        {
            var text = @"
const int Test = 1;
System.Console.WriteLine(Test);

class Test {}

enum E1
{
    T = Test,
}

namespace N1
{
    enum E1
    {
        T = Test,
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (9,9): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //     T = Test,
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(9, 9),
                // (16,13): error CS9000: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         T = Test,
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 13)
                );
        }

        [Fact]
        public void Scope_16()
        {
            var text1 = @"
using alias1 = System.String;
alias1 x = ""1"";
alias2 y = ""1"";
System.Console.WriteLine(x);
System.Console.WriteLine(y);
local();
";
            var text2 = @"
using alias2 = System.String;
void local()
{
    alias1 a = ""2"";
    alias2 b = ""2"";
    System.Console.WriteLine(a);
    System.Console.WriteLine(b);
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (4,1): error CS0246: The type or namespace name 'alias2' could not be found (are you missing a using directive or an assembly reference?)
                // alias2 y = "1";
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias2").WithArguments("alias2").WithLocation(4, 1),
                // (5,5): error CS0246: The type or namespace name 'alias1' could not be found (are you missing a using directive or an assembly reference?)
                //     alias1 a = "2";
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias1").WithArguments("alias1").WithLocation(5, 5)
                );

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);

            var nameRef = tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "alias1" && !id.Parent.IsKind(SyntaxKind.NameEquals)).Single();

            Assert.NotEmpty(model1.LookupNamespacesAndTypes(nameRef.SpanStart, name: "alias1"));
            Assert.Empty(model1.LookupNamespacesAndTypes(nameRef.SpanStart, name: "alias2"));

            nameRef = tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "alias2").Single();
            model1.GetDiagnostics(nameRef.Ancestors().OfType<StatementSyntax>().First().Span).Verify(
                // (4,1): error CS0246: The type or namespace name 'alias2' could not be found (are you missing a using directive or an assembly reference?)
                // alias2 y = "1";
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias2").WithArguments("alias2").WithLocation(4, 1)
                );
            model1.GetDiagnostics().Verify(
                // (4,1): error CS0246: The type or namespace name 'alias2' could not be found (are you missing a using directive or an assembly reference?)
                // alias2 y = "1";
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias2").WithArguments("alias2").WithLocation(4, 1)
                );

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            nameRef = tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "alias2" && !id.Parent.IsKind(SyntaxKind.NameEquals)).Single();

            Assert.Empty(model2.LookupNamespacesAndTypes(nameRef.SpanStart, name: "alias1"));
            Assert.NotEmpty(model2.LookupNamespacesAndTypes(nameRef.SpanStart, name: "alias2"));

            nameRef = tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "alias1").Single();
            model2.GetDiagnostics(nameRef.Ancestors().OfType<StatementSyntax>().First().Span).Verify(
                // (5,5): error CS0246: The type or namespace name 'alias1' could not be found (are you missing a using directive or an assembly reference?)
                //     alias1 a = "2";
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias1").WithArguments("alias1").WithLocation(5, 5)
                );
            model2.GetDiagnostics().Verify(
                // (5,5): error CS0246: The type or namespace name 'alias1' could not be found (are you missing a using directive or an assembly reference?)
                //     alias1 a = "2";
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias1").WithArguments("alias1").WithLocation(5, 5)
                );
        }

        [Fact]
        public void LocalFunctionStatement_01()
        {
            var text = @"
local();

void local()
{
    System.Console.WriteLine(""Hi!"");
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var reference = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "local").Single();

            var local = model.GetDeclaredSymbol(declarator);
            Assert.Same(local, model.GetSymbolInfo(reference).Symbol);
            Assert.Equal("void local()", local.ToTestDisplayString());
            Assert.Equal(MethodKind.LocalFunction, ((IMethodSymbol)local).MethodKind);

            Assert.Equal(SymbolKind.Method, local.ContainingSymbol.Kind);
            Assert.True(local.ContainingSymbol.IsImplicitlyDeclared);
            Assert.Equal(SymbolKind.NamedType, local.ContainingSymbol.ContainingSymbol.Kind);
            Assert.True(local.ContainingSymbol.ContainingSymbol.IsImplicitlyDeclared);
            Assert.True(((INamespaceSymbol)local.ContainingSymbol.ContainingSymbol.ContainingSymbol).IsGlobalNamespace);
        }

        [Fact]
        public void LocalFunctionStatement_02()
        {
            var text = @"
local();

void local() => System.Console.WriteLine(""Hi!"");
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void LocalFunctionStatement_03()
        {
            var text = @"
local();

void I1.local()
{
    System.Console.WriteLine(""Hi!"");
}

interface I1
{
    void local();
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS0103: The name 'local' does not exist in the current context
                // local();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(2, 1),
                // (4,6): error CS0540: '<invalid-global-code>.I1.local()': containing type does not implement interface 'I1'
                // void I1.local()
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I1").WithArguments("<invalid-global-code>.I1.local()", "I1").WithLocation(4, 6),
                // (4,9): error CS0116: A namespace cannot directly contain members such as fields or methods
                // void I1.local()
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "local").WithLocation(4, 9)
                );
        }

        [Fact]
        public void LocalFunctionStatement_04()
        {
            var text = @"
new void localA() => System.Console.WriteLine();
localA();
public void localB() => System.Console.WriteLine();
localB();
virtual void localC() => System.Console.WriteLine();
localC();
sealed void localD() => System.Console.WriteLine();
localD();
override void localE() => System.Console.WriteLine();
localE();
abstract void localF() => System.Console.WriteLine();
localF();
partial void localG() => System.Console.WriteLine();
localG();
extern void localH() => System.Console.WriteLine();
localH();
[System.Obsolete()]
void localI() => System.Console.WriteLine();
localI();
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,10): error CS0116: A namespace cannot directly contain members such as fields or methods
                // new void localA() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "localA").WithLocation(2, 10),
                // (2,10): warning CS0109: The member '<invalid-global-code>.localA()' does not hide an accessible member. The new keyword is not required.
                // new void localA() => System.Console.WriteLine();
                Diagnostic(ErrorCode.WRN_NewNotRequired, "localA").WithArguments("<invalid-global-code>.localA()").WithLocation(2, 10),
                // (3,1): error CS0103: The name 'localA' does not exist in the current context
                // localA();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "localA").WithArguments("localA").WithLocation(3, 1),
                // (4,1): error CS0106: The modifier 'public' is not valid for this item
                // public void localB() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "public").WithArguments("public").WithLocation(4, 1),
                // (6,14): error CS0116: A namespace cannot directly contain members such as fields or methods
                // virtual void localC() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "localC").WithLocation(6, 14),
                // (6,14): error CS0621: '<invalid-global-code>.localC()': virtual or abstract members cannot be private
                // virtual void localC() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "localC").WithArguments("<invalid-global-code>.localC()").WithLocation(6, 14),
                // (7,1): error CS0103: The name 'localC' does not exist in the current context
                // localC();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "localC").WithArguments("localC").WithLocation(7, 1),
                // (8,13): error CS0116: A namespace cannot directly contain members such as fields or methods
                // sealed void localD() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "localD").WithLocation(8, 13),
                // (8,13): error CS0238: '<invalid-global-code>.localD()' cannot be sealed because it is not an override
                // sealed void localD() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "localD").WithArguments("<invalid-global-code>.localD()").WithLocation(8, 13),
                // (9,1): error CS0103: The name 'localD' does not exist in the current context
                // localD();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "localD").WithArguments("localD").WithLocation(9, 1),
                // (10,15): error CS0116: A namespace cannot directly contain members such as fields or methods
                // override void localE() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "localE").WithLocation(10, 15),
                // (10,15): error CS0621: '<invalid-global-code>.localE()': virtual or abstract members cannot be private
                // override void localE() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "localE").WithArguments("<invalid-global-code>.localE()").WithLocation(10, 15),
                // (10,15): error CS0115: '<invalid-global-code>.localE()': no suitable method found to override
                // override void localE() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "localE").WithArguments("<invalid-global-code>.localE()").WithLocation(10, 15),
                // (11,1): error CS0103: The name 'localE' does not exist in the current context
                // localE();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "localE").WithArguments("localE").WithLocation(11, 1),
                // (12,15): error CS0116: A namespace cannot directly contain members such as fields or methods
                // abstract void localF() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "localF").WithLocation(12, 15),
                // (12,15): error CS0500: '<invalid-global-code>.localF()' cannot declare a body because it is marked abstract
                // abstract void localF() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "localF").WithArguments("<invalid-global-code>.localF()").WithLocation(12, 15),
                // (12,15): error CS0621: '<invalid-global-code>.localF()': virtual or abstract members cannot be private
                // abstract void localF() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "localF").WithArguments("<invalid-global-code>.localF()").WithLocation(12, 15),
                // (13,1): error CS0103: The name 'localF' does not exist in the current context
                // localF();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "localF").WithArguments("localF").WithLocation(13, 1),
                // (14,14): error CS0116: A namespace cannot directly contain members such as fields or methods
                // partial void localG() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "localG").WithLocation(14, 14),
                // (14,14): error CS0759: No defining declaration found for implementing declaration of partial method '<invalid-global-code>.localG()'
                // partial void localG() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "localG").WithArguments("<invalid-global-code>.localG()").WithLocation(14, 14),
                // (14,14): error CS0751: A partial method must be declared within a partial class, partial struct, or partial interface
                // partial void localG() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyInPartialClass, "localG").WithLocation(14, 14),
                // (15,1): error CS0103: The name 'localG' does not exist in the current context
                // localG();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "localG").WithArguments("localG").WithLocation(15, 1),
                // (16,13): error CS0116: A namespace cannot directly contain members such as fields or methods
                // extern void localH() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "localH").WithLocation(16, 13),
                // (16,13): error CS0179: '<invalid-global-code>.localH()' cannot be extern and declare a body
                // extern void localH() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_ExternHasBody, "localH").WithArguments("<invalid-global-code>.localH()").WithLocation(16, 13),
                // (17,1): error CS0103: The name 'localH' does not exist in the current context
                // localH();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "localH").WithArguments("localH").WithLocation(17, 1),
                // (19,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                // void localI() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "localI").WithLocation(19, 6),
                // (20,1): error CS0103: The name 'localI' does not exist in the current context
                // localI();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "localI").WithArguments("localI").WithLocation(20, 1)
                );
        }

        [Fact]
        public void LocalFunctionStatement_05()
        {
            var text = @"
void local1() => System.Console.Write(""1"");
local1();
void local2() => System.Console.Write(""2"");
local2();
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "12");
        }

        [Fact]
        public void LocalFunctionStatement_06()
        {
            var text = @"
local();

static void local()
{
    System.Console.WriteLine(""Hi!"");
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void LocalFunctionStatement_07()
        {
            var text1 = @"
local1(1);
void local1(int x)
{}
local2();
";
            var text2 = @"
void local1(byte y)
{}

void local2()
{
    local1(2);
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,6): error CS0136: A local or parameter named 'local1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // void local1(byte y)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "local1").WithArguments("local1").WithLocation(2, 6),
                // (2,6): warning CS8321: The local function 'local1' is declared but never used
                // void local1(byte y)
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local1").WithArguments("local1").WithLocation(2, 6)
                );

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var symbol1 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single());
            Assert.Equal("void local1(System.Int32 x)", symbol1.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "local1").Single()).Symbol);

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            var symbol2 = model2.GetDeclaredSymbol(tree2.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().First());
            Assert.Equal("void local1(System.Byte y)", symbol2.ToTestDisplayString());
            Assert.Same(symbol1, model2.GetSymbolInfo(tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "local1").Single()).Symbol);
        }

        [Fact]
        public void LocalFunctionStatement_08()
        {
            var text = @"
void local()
{
    System.Console.WriteLine(""Hi!"");
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (2,6): warning CS8321: The local function 'local' is declared but never used
                // void local()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(2, 6)
                );

            CompileAndVerify(comp, expectedOutput: "");
        }

        [Fact]
        public void PropertyDeclaration_01()
        {
            var text = @"
_ = local;

int local => 1;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,5): error CS0103: The name 'local' does not exist in the current context
                // _ = local;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(2, 5),
                // (4,5): error CS0116: A namespace cannot directly contain members such as fields or methods
                // int local => 1;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "local").WithLocation(4, 5)
                );
        }

        [Fact]
        public void PropertyDeclaration_02()
        {
            var text = @"
_ = local;

int local { get => 1; }
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,5): error CS0103: The name 'local' does not exist in the current context
                // _ = local;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(2, 5),
                // (4,5): error CS0116: A namespace cannot directly contain members such as fields or methods
                // int local { get => 1; }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "local").WithLocation(4, 5)
                );
        }

        [Fact]
        public void PropertyDeclaration_03()
        {
            var text = @"
_ = local;

int local { get { return 1; } }
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,5): error CS0103: The name 'local' does not exist in the current context
                // _ = local;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(2, 5),
                // (4,5): error CS0116: A namespace cannot directly contain members such as fields or methods
                // int local { get { return 1; } }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "local").WithLocation(4, 5)
                );
        }

        [Fact]
        public void EventDeclaration_01()
        {
            var text = @"
local += null;

event System.Action local;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS0103: The name 'local' does not exist in the current context
                // local += null;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(2, 1),
                // (4,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                // event System.Action local;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "local").WithLocation(4, 21)
                );
        }

        [Fact]
        public void EventDeclaration_02()
        {
            var text = @"
local -= null;

event System.Action local
{
    add {}
    remove {}
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS0103: The name 'local' does not exist in the current context
                // local -= null;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(2, 1),
                // (4,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                // event System.Action local
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "local").WithLocation(4, 21)
                );
        }

        [Fact]
        public void LabeledStatement_01()
        {
            var text = @"
goto label1;
label1: System.Console.WriteLine(""Hi!"");
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
            var reference = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "label1").Single();

            var label = model.GetDeclaredSymbol(declarator);
            Assert.Same(label, model.GetSymbolInfo(reference).Symbol);
            Assert.Equal("label1", label.ToTestDisplayString());
            Assert.Equal(SymbolKind.Label, label.Kind);

            Assert.Equal(SymbolKind.Method, label.ContainingSymbol.Kind);
            Assert.True(label.ContainingSymbol.IsImplicitlyDeclared);
            Assert.Equal(SymbolKind.NamedType, label.ContainingSymbol.ContainingSymbol.Kind);
            Assert.True(label.ContainingSymbol.ContainingSymbol.IsImplicitlyDeclared);
            Assert.True(((INamespaceSymbol)label.ContainingSymbol.ContainingSymbol.ContainingSymbol).IsGlobalNamespace);
        }

        [Fact]
        public void LabeledStatement_02()
        {
            var text = @"
goto label1;
label1: System.Console.WriteLine(""Hi!"");
label1: System.Console.WriteLine();
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (4,1): error CS0140: The label 'label1' is a duplicate
                // label1: System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_DuplicateLabel, "label1").WithArguments("label1").WithLocation(4, 1)
                );
        }

        [Fact]
        public void LabeledStatement_03()
        {
            var text1 = @"
goto label1;
label1: System.Console.Write(1);
";
            var text2 = @"
label1: System.Console.Write(2);
goto label1;
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS9001: In all but one compilation unit the top-level statements must all be local function declarations.
                // label1: System.Console.Write(2);
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, "label1").WithLocation(2, 1),
                // (2,1): error CS0140: The label 'label1' is a duplicate
                // label1: System.Console.Write(2);
                Diagnostic(ErrorCode.ERR_DuplicateLabel, "label1").WithArguments("label1").WithLocation(2, 1)
                );

            Assert.False(comp.NullableSemanticAnalysisEnabled); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var symbol1 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single());
            Assert.Equal("label1", symbol1.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "label1").Single()).Symbol);

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            var symbol2 = model2.GetDeclaredSymbol(tree2.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single());
            Assert.Equal("label1", symbol2.ToTestDisplayString());
            Assert.NotEqual(symbol1, symbol2);
            Assert.Same(symbol1, model2.GetSymbolInfo(tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "label1").Single()).Symbol);
        }

        [Fact]
        public void ExplicitMain_01()
        {
            var text = @"
static void Main()
{}

System.Console.Write(""Hi!"");
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,13): warning CS8321: The local function 'Main' is declared but never used
                // static void Main()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Main").WithArguments("Main").WithLocation(2, 13)
                );

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_02()
        {
            var text = @"
System.Console.Write(""H"");
Main();
System.Console.Write(""!"");

static void Main()
{
    System.Console.Write(""i"");
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(); // PROTOTYPE(SimplePrograms): Should we still warn that Main is not the entry point?
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_03()
        {
            var text = @"
using System;
using System.Threading.Tasks;

System.Console.Write(""Hi!"");

class Program
{
    static async Task Main()
    {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (9,23): warning CS7022: The entry point of the program is global code; ignoring 'Program.Main()' entry point.
                //     static async Task Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program.Main()").WithLocation(9, 23)
                );

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_04()
        {
            var text = @"
using System;
using System.Threading.Tasks;

await Task.Factory.StartNew(() => 5);
System.Console.Write(""Hi!"");

class Program
{
    static async Task Main()
    {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (10,23): warning CS7022: The entry point of the program is global code; ignoring 'Program.Main()' entry point.
                //     static async Task Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program.Main()").WithLocation(10, 23)
                );

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_05()
        {
            var text = @"
using System;
using System.Threading.Tasks;

await Task.Factory.StartNew(() => 5);
System.Console.Write(""Hi!"");

class Program
{
    static void Main()
    {
        Console.Write(""hello "");
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (10,17): warning CS7022: The entry point of the program is global code; ignoring 'Program.Main()' entry point.
                //     static void Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program.Main()").WithLocation(10, 17)
                );

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_06()
        {
            var text = @"
System.Console.Write(""Hi!"");

class Program
{
    static void Main()
    {
        System.Console.Write(""hello "");
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,17): warning CS7022: The entry point of the program is global code; ignoring 'Program.Main()' entry point.
                //     static void Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program.Main()").WithLocation(6, 17)
                );

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_07()
        {
            var text = @"
using System;
using System.Threading.Tasks;

System.Console.Write(""Hi!"");

class Program
{
    static void Main(string[] args)
    {
        Console.Write(""hello "");
    }

    static async Task Main()
    {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (9,17): warning CS7022: The entry point of the program is global code; ignoring 'Program.Main(string[])' entry point.
                //     static void Main(string[] args)
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program.Main(string[])").WithLocation(9, 17),
                // (14,23): warning CS7022: The entry point of the program is global code; ignoring 'Program.Main()' entry point.
                //     static async Task Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program.Main()").WithLocation(14, 23)
                );

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_08()
        {
            var text = @"
using System;
using System.Threading.Tasks;

await Task.Factory.StartNew(() => 5);
System.Console.Write(""Hi!"");

class Program
{
    static void Main()
    {
        Console.Write(""hello "");
    }

    static async Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (10,17): warning CS7022: The entry point of the program is global code; ignoring 'Program.Main()' entry point.
                //     static void Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program.Main()").WithLocation(10, 17),
                // (15,23): warning CS7022: The entry point of the program is global code; ignoring 'Program.Main(string[])' entry point.
                //     static async Task Main(string[] args)
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program.Main(string[])").WithLocation(15, 23)
                );

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void Yield_01()
        {
            var text = @"yield break;";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (1,1): error CS1624: The body of '<simple-program-entry-point>' cannot be an iterator block because 'void' is not an iterator interface type
                // yield break;
                Diagnostic(ErrorCode.ERR_BadIteratorReturn, "yield break;").WithArguments("<simple-program-entry-point>", "void").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Yield_02()
        {
            var text = @"{yield return 0;}";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (1,1): error CS1624: The body of '<simple-program-entry-point>' cannot be an iterator block because 'void' is not an iterator interface type
                // {yield return 0;}
                Diagnostic(ErrorCode.ERR_BadIteratorReturn, "{yield return 0;}").WithArguments("<simple-program-entry-point>", "void").WithLocation(1, 1)
                );
        }
    }
}
