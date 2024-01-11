// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.TopLevelStatements)]
    public class TopLevelStatementsTests : CompilingTestBase
    {
        private static CSharpParseOptions DefaultParseOptions => TestOptions.Regular9;

        private static bool IsNullableAnalysisEnabled(CSharpCompilation compilation)
        {
            var type = compilation.GlobalNamespace.GetTypeMember("Program");
            var methods = type.GetMembers().OfType<SynthesizedSimpleProgramEntryPointSymbol>();
            return methods.Any(m => m.IsNullableAnalysisEnabled());
        }

        [Fact]
        public void Simple_01()
        {
            var text = @"System.Console.WriteLine(""Hi!"");";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Void", entryPoint.ReturnType.ToTestDisplayString());
            Assert.True(entryPoint.ReturnsVoid);
            AssertEntryPointParameter(entryPoint);
            CompileAndVerify(comp, expectedOutput: "Hi!");
            Assert.Same(entryPoint, comp.GetEntryPoint(default));
            Assert.False(entryPoint.CanBeReferencedByName);
            Assert.True(entryPoint.ContainingType.CanBeReferencedByName);
            Assert.Equal("<Main>$", entryPoint.Name);
            Assert.Equal("Program", entryPoint.ContainingType.Name);
        }

        private static void AssertEntryPointParameter(SynthesizedSimpleProgramEntryPointSymbol entryPoint)
        {
            Assert.Equal(1, entryPoint.ParameterCount);
            ParameterSymbol parameter = entryPoint.Parameters.Single();
            Assert.Equal("System.String[] args", parameter.ToTestDisplayString(includeNonNullable: true));
            Assert.True(parameter.IsImplicitlyDeclared);
            Assert.Same(entryPoint, parameter.ContainingSymbol);
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
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Threading.Tasks.Task", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            AssertEntryPointParameter(entryPoint);
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
                // (3,1): error CS8802: Only one compilation unit can have top-level statements.
                // System.Console.Write("2");
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "System").WithLocation(3, 1)
                );

            comp = CreateCompilation(new[] { text1, text2, text3 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (3,1): error CS8802: Only one compilation unit can have top-level statements.
                // System.Console.Write("2");
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "System").WithLocation(3, 1),
                // (4,1): error CS8802: Only one compilation unit can have top-level statements.
                // System.Console.Write("3");
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "System").WithLocation(4, 1)
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
        public void Simple_06_01()
        {
            var text1 =
@"
local();
void local() => System.Console.WriteLine(2);
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");

            verifyModel(comp, comp.SyntaxTrees[0]);

            comp = CreateCompilation(text1, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Enable), parseOptions: DefaultParseOptions);
            verifyModel(comp, comp.SyntaxTrees[0], nullableEnabled: true);

            static void verifyModel(CSharpCompilation comp, SyntaxTree tree1, bool nullableEnabled = false)
            {
                Assert.Equal(nullableEnabled, IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel
                var model1 = comp.GetSemanticModel(tree1);

                verifyModelForGlobalStatements(tree1, model1);

                var unit1 = (CompilationUnitSyntax)tree1.GetRoot();
                var localRef = unit1.DescendantNodes().OfType<IdentifierNameSyntax>().First();
                var refSymbol = model1.GetSymbolInfo(localRef).Symbol;
                Assert.Equal("void local()", refSymbol.ToTestDisplayString());
                Assert.Contains(refSymbol.Name, model1.LookupNames(localRef.SpanStart));
                Assert.Contains(refSymbol, model1.LookupSymbols(localRef.SpanStart));
                Assert.Same(refSymbol, model1.LookupSymbols(localRef.SpanStart, name: refSymbol.Name).Single());
                var operation1 = model1.GetOperation(localRef.Parent);
                Assert.NotNull(operation1);
                Assert.IsAssignableFrom<IInvocationOperation>(operation1);

                Assert.NotNull(ControlFlowGraph.Create((IMethodBodyOperation)((IBlockOperation)operation1.Parent.Parent).Parent));

                model1.VerifyOperationTree(unit1,
@"
IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'local(); ... iteLine(2);')
  BlockBody: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'local(); ... iteLine(2);')
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
  ExpressionBody: 
    null
");
                var localDecl = unit1.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
                var declSymbol = model1.GetDeclaredSymbol(localDecl);
                Assert.Same(declSymbol.ContainingSymbol, model1.GetDeclaredSymbol(unit1));
                Assert.Same(declSymbol.ContainingSymbol, model1.GetDeclaredSymbol((SyntaxNode)unit1));
                Assert.Same(refSymbol, declSymbol);
                Assert.Contains(declSymbol.Name, model1.LookupNames(localDecl.SpanStart));
                Assert.Contains(declSymbol, model1.LookupSymbols(localDecl.SpanStart));
                Assert.Same(declSymbol, model1.LookupSymbols(localDecl.SpanStart, name: declSymbol.Name).Single());
                var operation2 = model1.GetOperation(localDecl);
                Assert.NotNull(operation2);
                Assert.IsAssignableFrom<ILocalFunctionOperation>(operation2);

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
        public void Simple_06_02()
        {
            var text1 = @"local();";
            var text2 = @"void local() => System.Console.WriteLine(2);";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (1,1): error CS8802: Only one compilation unit can have top-level statements.
                // void local() => System.Console.WriteLine(2);
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "void").WithLocation(1, 1),
                // (1,1): error CS0103: The name 'local' does not exist in the current context
                // local();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(1, 1),
                // (1,6): warning CS8321: The local function 'local' is declared but never used
                // void local() => System.Console.WriteLine(2);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(1, 6)
                );

            verifyModel(comp, comp.SyntaxTrees[0], comp.SyntaxTrees[1]);

            comp = CreateCompilation(new[] { text2, text1 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (1,1): error CS8802: Only one compilation unit can have top-level statements.
                // local();
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "local").WithLocation(1, 1),
                // (1,1): error CS0103: The name 'local' does not exist in the current context
                // local();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(1, 1),
                // (1,6): warning CS8321: The local function 'local' is declared but never used
                // void local() => System.Console.WriteLine(2);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(1, 6)
                );

            verifyModel(comp, comp.SyntaxTrees[1], comp.SyntaxTrees[0]);

            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Enable), parseOptions: DefaultParseOptions);
            verifyModel(comp, comp.SyntaxTrees[0], comp.SyntaxTrees[1], nullableEnabled: true);

            static void verifyModel(CSharpCompilation comp, SyntaxTree tree1, SyntaxTree tree2, bool nullableEnabled = false)
            {
                Assert.Equal(nullableEnabled, IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel
                var model1 = comp.GetSemanticModel(tree1);

                verifyModelForGlobalStatements(tree1, model1);

                var unit1 = (CompilationUnitSyntax)tree1.GetRoot();
                var localRef = unit1.DescendantNodes().OfType<IdentifierNameSyntax>().Single();
                var refSymbol = model1.GetSymbolInfo(localRef).Symbol;
                var refMethod = model1.GetDeclaredSymbol(unit1);
                Assert.NotNull(refMethod);
                Assert.Null(refSymbol);
                var name = localRef.Identifier.ValueText;
                Assert.DoesNotContain(name, model1.LookupNames(localRef.SpanStart));
                Assert.Empty(model1.LookupSymbols(localRef.SpanStart).Where(s => s.Name == name));
                Assert.Empty(model1.LookupSymbols(localRef.SpanStart, name: name));
                var operation1 = model1.GetOperation(localRef.Parent);
                Assert.NotNull(operation1);
                Assert.IsAssignableFrom<IInvalidOperation>(operation1);

                Assert.NotNull(ControlFlowGraph.Create((IMethodBodyOperation)((IBlockOperation)operation1.Parent.Parent).Parent));

                model1.VerifyOperationTree(unit1,
@"
IMethodBodyOperation (OperationKind.MethodBody, Type: null, IsInvalid) (Syntax: 'local();')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'local();')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'local();')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'local()')
            Children(1):
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'local')
                  Children(0)
  ExpressionBody: 
    null
");

                SyntaxTreeSemanticModel syntaxTreeModel = ((SyntaxTreeSemanticModel)model1);
                MemberSemanticModel mm = syntaxTreeModel.TestOnlyMemberModels[unit1];

                var model2 = comp.GetSemanticModel(tree2);

                verifyModelForGlobalStatements(tree2, model2);

                var unit2 = (CompilationUnitSyntax)tree2.GetRoot();
                var declMethod = model2.GetDeclaredSymbol(unit2);
                var localDecl = unit2.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
                var declSymbol = model2.GetDeclaredSymbol(localDecl);
                Assert.Equal("void local()", declSymbol.ToTestDisplayString());
                Assert.Same(declSymbol.ContainingSymbol, declMethod);
                Assert.NotEqual(refMethod, declMethod);
                Assert.Contains(declSymbol.Name, model2.LookupNames(localDecl.SpanStart));
                Assert.Contains(declSymbol, model2.LookupSymbols(localDecl.SpanStart));
                Assert.Same(declSymbol, model2.LookupSymbols(localDecl.SpanStart, name: declSymbol.Name).Single());
                var operation2 = model2.GetOperation(localDecl);
                Assert.NotNull(operation2);
                Assert.IsAssignableFrom<ILocalFunctionOperation>(operation2);

                Assert.NotNull(ControlFlowGraph.Create((IMethodBodyOperation)((IBlockOperation)operation2.Parent).Parent));

                var isInvalid = comp.SyntaxTrees[1] == tree2 ? ", IsInvalid" : "";

                model2.VerifyOperationTree(unit2,
@"
IMethodBodyOperation (OperationKind.MethodBody, Type: null" + isInvalid + @") (Syntax: 'void local( ... iteLine(2);')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null" + isInvalid + @", IsImplicit) (Syntax: 'void local( ... iteLine(2);')
      ILocalFunctionOperation (Symbol: void local()) (OperationKind.LocalFunction, Type: null" + isInvalid + @") (Syntax: 'void local( ... iteLine(2);')
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
  ExpressionBody: 
    null
");

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
            comp.VerifyDiagnostics(
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // void local() => System.Console.WriteLine(i);
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "void").WithLocation(2, 1),
                // (2,5): warning CS0219: The variable 'i' is assigned but its value is never used
                // var i = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(2, 5),
                // (2,6): warning CS8321: The local function 'local' is declared but never used
                // void local() => System.Console.WriteLine(i);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(2, 6),
                // (2,42): error CS0103: The name 'i' does not exist in the current context
                // void local() => System.Console.WriteLine(i);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "i").WithArguments("i").WithLocation(2, 42),
                // (3,1): error CS0103: The name 'local' does not exist in the current context
                // local();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(3, 1)
                );

            verifyModel(comp, comp.SyntaxTrees[0], comp.SyntaxTrees[1]);

            comp = CreateCompilation(new[] { text2, text1 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // var i = 1;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "var").WithLocation(2, 1),
                // (2,5): warning CS0219: The variable 'i' is assigned but its value is never used
                // var i = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(2, 5),
                // (2,6): warning CS8321: The local function 'local' is declared but never used
                // void local() => System.Console.WriteLine(i);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(2, 6),
                // (2,42): error CS0103: The name 'i' does not exist in the current context
                // void local() => System.Console.WriteLine(i);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "i").WithArguments("i").WithLocation(2, 42),
                // (3,1): error CS0103: The name 'local' does not exist in the current context
                // local();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(3, 1)
                );

            verifyModel(comp, comp.SyntaxTrees[1], comp.SyntaxTrees[0]);

            static void verifyModel(CSharpCompilation comp, SyntaxTree tree1, SyntaxTree tree2)
            {
                Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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

                Assert.DoesNotContain(declSymbol, model1.AnalyzeDataFlow(localDecl.Ancestors().OfType<StatementSyntax>().First()).DataFlowsOut);

                var model2 = comp.GetSemanticModel(tree2);
                var localRef = tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "i").Single();
                var refSymbol = model2.GetSymbolInfo(localRef).Symbol;
                Assert.Null(refSymbol);
                var name = localRef.Identifier.ValueText;
                Assert.DoesNotContain(name, model2.LookupNames(localRef.SpanStart));
                Assert.Empty(model2.LookupSymbols(localRef.SpanStart).Where(s => s.Name == name));
                Assert.Empty(model2.LookupSymbols(localRef.SpanStart, name: name));
                Assert.NotNull(model2.GetOperation(tree2.GetRoot()));
                var operation2 = model2.GetOperation(localRef);
                Assert.NotNull(operation2);
                Assert.IsAssignableFrom<IInvalidOperation>(operation2);

                Assert.DoesNotContain(declSymbol, model2.AnalyzeDataFlow(localRef).DataFlowsIn);
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

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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
        public void Simple_09()
        {
            var text1 = @"
var i = 1;
local();
void local() => System.Console.WriteLine(i);
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1");

            verifyModel(comp, comp.SyntaxTrees[0]);

            static void verifyModel(CSharpCompilation comp, SyntaxTree tree1)
            {
                Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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

                var localRef = tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "i").Single();
                var refSymbol = model1.GetSymbolInfo(localRef).Symbol;
                Assert.Same(declSymbol, refSymbol);
                Assert.Contains(refSymbol.Name, model1.LookupNames(localRef.SpanStart));
                Assert.Contains(refSymbol, model1.LookupSymbols(localRef.SpanStart));
                Assert.Same(refSymbol, model1.LookupSymbols(localRef.SpanStart, name: refSymbol.Name).Single());
                var operation2 = model1.GetOperation(localRef);
                Assert.NotNull(operation2);
                Assert.IsAssignableFrom<ILocalReferenceOperation>(operation2);

                // The following assert fails due to https://github.com/dotnet/roslyn/issues/41853, enable once the issue is fixed.
                //Assert.Contains(declSymbol, model1.AnalyzeDataFlow(localRef).DataFlowsIn);
            }
        }

        [Fact]
        public void LanguageVersion_01()
        {
            var text = @"System.Console.WriteLine(""Hi!"");";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular8);

            comp.VerifyDiagnostics(
                // (1,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                // System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, @"System.Console.WriteLine(""Hi!"");").WithArguments("top-level statements", "9.0").WithLocation(1, 1)
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
                // (4,29): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
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
                // (4,30): error CS1519: Invalid token '"Hi!"' in class, record, struct, or interface member declaration
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

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var reference = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "s").Single();

            var local = model.GetDeclaredSymbol(declarator);
            Assert.Same(local, model.GetSymbolInfo(reference).Symbol);
            Assert.Equal("System.String s", local.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, local.Kind);

            Assert.Equal(SymbolKind.Method, local.ContainingSymbol.Kind);
            Assert.False(local.ContainingSymbol.IsImplicitlyDeclared);
            Assert.Equal(SymbolKind.NamedType, local.ContainingSymbol.ContainingSymbol.Kind);
            Assert.Equal("Program", local.ContainingSymbol.ContainingSymbol.Name);
            Assert.False(local.ContainingSymbol.ContainingSymbol.IsImplicitlyDeclared);
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
                // (12,1): error CS7014: Attributes are not valid in this context.
                // [System.Obsolete()]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[System.Obsolete()]").WithLocation(12, 1),
                // (15,1): error CS7014: Attributes are not valid in this context.
                // [System.Obsolete()]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[System.Obsolete()]").WithLocation(15, 1)
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
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // int x = 1;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "int").WithLocation(2, 1)
                );

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var symbol1 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single());
            Assert.Equal("System.String x", symbol1.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single()).Symbol);

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            var symbol2 = model2.GetDeclaredSymbol(tree2.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single());
            Assert.Equal("System.Int32 x", symbol2.ToTestDisplayString());
            Assert.Same(symbol2, model2.GetSymbolInfo(tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single()).Symbol);
        }

        [Fact]
        public void LocalDeclarationStatement_08()
        {
            var text = @"
int a = 0;
int b = 0;
int c = -100;

ref int d = ref c;
d = 300;
d = ref local(true, ref a, ref b);
d = 100;
d = ref local(false, ref a, ref b);
d = 200;

System.Console.Write(a);
System.Console.Write(' ');
System.Console.Write(b);
System.Console.Write(' ');
System.Console.Write(c);

ref int local(bool flag, ref int a, ref int b)
{
    return ref flag ? ref a : ref b;
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "100 200 300", verify: Verification.Skipped);
        }

        [Fact]
        public void LocalDeclarationStatement_09()
        {
            var text = @"
using var a = new MyDisposable();
System.Console.Write(1);

class MyDisposable : System.IDisposable
{
    public void Dispose()
    {
        System.Console.Write(2);
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "12", verify: Verification.Skipped);
        }

        [Fact]
        public void LocalDeclarationStatement_10()
        {
            string source = @"
await using var x = new C();
System.Console.Write(""body "");

class C : System.IAsyncDisposable, System.IDisposable
{
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
    public void Dispose()
    {
        System.Console.Write(""IGNORED"");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [Fact]
        public void LocalDeclarationStatement_11()
        {
            var text1 = @"
string x = ""1"";
System.Console.Write(x);
int x = 1;
System.Console.Write(x);
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (4,5): error CS0128: A local variable or function named 'x' is already defined in this scope
                // int x = 1;
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(4, 5),
                // (4,5): warning CS0219: The variable 'x' is assigned but its value is never used
                // int x = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(4, 5)
                );

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var symbol1 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First());
            Assert.Equal("System.String x", symbol1.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").First()).Symbol);

            var symbol2 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Skip(1).Single());
            Assert.Equal("System.Int32 x", symbol2.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Skip(1).Single()).Symbol);
        }

        [Fact]
        public void LocalDeclarationStatement_12()
        {
            var text = @"
(int x, int y) = (1, 2);
System.Console.WriteLine(x+y);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "3");
        }

        [Fact]
        public void LocalDeclarationStatement_13()
        {
            var text = @"
var (x, y) = (1, 2);
System.Console.WriteLine(x+y);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "3");
        }

        [Fact]
        public void LocalDeclarationStatement_14()
        {
            var text1 = @"
string args = ""1"";
System.Console.Write(args);
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,8): error CS0136: A local or parameter named 'args' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // string args = "1";
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "args").WithArguments("args").WithLocation(2, 8)
                );

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var symbol1 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First());
            Assert.Equal("System.String args", symbol1.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "args").Single()).Symbol);
        }

        [Fact]
        public void LocalDeclarationStatement_15()
        {
            var text1 = @"
using System.Linq;
string x = null;
_ = from x in new object[0] select x;
System.Console.Write(x);
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (4,10): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                // _ = from x in new object[0] select x;
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(4, 10)
                );
        }

        [Fact]
        public void LocalDeclarationStatement_16()
        {
            var text = @"
System.Console.WriteLine();
string await = ""Hi!"";
System.Console.WriteLine(await);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (3,8): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                // string await = "Hi!";
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(3, 8),
                // (3,8): warning CS0219: The variable 'await' is assigned but its value is never used
                // string await = "Hi!";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "await").WithArguments("await").WithLocation(3, 8),
                // (4,31): error CS1525: Invalid expression term ')'
                // System.Console.WriteLine(await);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(4, 31)
                );

            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Threading.Tasks.Task", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnType.IsErrorType());
            AssertEntryPointParameter(entryPoint);
        }

        [Fact]
        public void LocalDeclarationStatement_17()
        {
            var text = @"
string async = ""Hi!"";
System.Console.WriteLine(async);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void LocalDeclarationStatement_18()
        {
            var text = @"
int c = -100;
ref int d = ref c;
System.Console.Write(d);
await System.Threading.Tasks.Task.Yield();
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (3,9): error CS8177: Async methods cannot have by-reference locals
                // ref int d = ref c;
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "d").WithLocation(3, 9)
                );
        }

        [Fact]
        public void UsingStatement_01()
        {
            string source = @"
await using (var x = new C())
{
    System.Console.Write(""body "");
}

class C : System.IAsyncDisposable, System.IDisposable
{
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
    public void Dispose()
    {
        System.Console.Write(""IGNORED"");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [Fact]
        public void UsingStatement_02()
        {
            string source = @"
await using (new C())
{
    System.Console.Write(""body "");
}

class C : System.IAsyncDisposable, System.IDisposable
{
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        System.Console.Write(""DisposeAsync"");
        return new System.Threading.Tasks.ValueTask(System.Threading.Tasks.Task.CompletedTask);
    }
    public void Dispose()
    {
        System.Console.Write(""IGNORED"");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body DisposeAsync");
        }

        [Fact]
        public void UsingStatement_03()
        {
            string source = @"
using (new C())
{
    System.Console.Write(""body "");
}

class C : System.IDisposable
{
    public void Dispose()
    {
        System.Console.Write(""Dispose"");
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "body Dispose");
        }

        [Fact]
        public void ForeachStatement_01()
        {
            string source = @"
using System.Threading.Tasks;

await foreach (var i in new C())
{
}

System.Console.Write(""Done"");

class C
{
    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator();
    }
    public sealed class Enumerator
    {
        public async Task<bool> MoveNextAsync()
        {
            System.Console.Write(""MoveNextAsync "");
            await Task.Yield();
            return false;
        }
        public int Current
        {
            get => throw null;
        }
        public async Task DisposeAsync()
        {
            System.Console.Write(""DisposeAsync "");
            await Task.Yield();
        }
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "MoveNextAsync DisposeAsync Done");
        }

        [Fact]
        public void ForeachStatement_02()
        {
            var text = @"
int i = 0;
foreach (var j in new [] {2, 3})
{
    i += j;
} 

System.Console.WriteLine(i);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "5");
        }

        [Fact]
        public void ForeachStatement_03()
        {
            var text = @"
int i = 0;
foreach (var (j, k) in new [] {(2,200), (3,300)})
{
    i += j;
} 

System.Console.WriteLine(i);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "5");
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
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // const string y = x;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "const").WithLocation(2, 1),
                // (2,18): error CS0103: The name 'y' does not exist in the current context
                // const string x = y;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(2, 18),
                // (2,18): error CS0103: The name 'x' does not exist in the current context
                // const string y = x;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(2, 18)
                );

            comp = CreateCompilation(new[] { "System.Console.WriteLine();", text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // const string x = y;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "const").WithLocation(2, 1),
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // const string y = x;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "const").WithLocation(2, 1),
                // (2,18): error CS0103: The name 'y' does not exist in the current context
                // const string x = y;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(2, 18),
                // (2,18): error CS0103: The name 'x' does not exist in the current context
                // const string y = x;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(2, 18)
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
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // var y = x;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "var").WithLocation(2, 1),
                // (2,9): error CS0103: The name 'y' does not exist in the current context
                // var x = y;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(2, 9),
                // (2,9): error CS0103: The name 'x' does not exist in the current context
                // var y = x;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(2, 9)
                );

            comp = CreateCompilation(new[] { "System.Console.WriteLine();", text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // var x = y;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "var").WithLocation(2, 1),
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // var y = x;
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "var").WithLocation(2, 1),
                // (2,9): error CS0103: The name 'y' does not exist in the current context
                // var x = y;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(2, 9),
                // (2,9): error CS0103: The name 'x' does not exist in the current context
                // var y = x;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(2, 9)
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
                // (6,30): error CS8801: Cannot use local variable or local function 'x' declared in a top-level statement in this context.
                //         System.Console.Write(x);
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "x").WithArguments("x").WithLocation(6, 30)
                );

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            var nameRef = tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            var symbol2 = model2.GetSymbolInfo(nameRef).Symbol;
            Assert.Equal("System.String x", symbol2.ToTestDisplayString());
            Assert.Equal("System.String", model2.GetTypeInfo(nameRef).Type.ToTestDisplayString());
            Assert.Null(model2.GetOperation(tree2.GetRoot()));

            comp = CreateCompilation(new[] { text2, text1 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,30): error CS8801: Cannot use local variable or local function 'x' declared in a top-level statement in this context.
                //         System.Console.Write(x);
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "x").WithArguments("x").WithLocation(6, 30)
                );

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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
            comp.VerifyDiagnostics(
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // void local()
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "void").WithLocation(2, 1),
                // (2,6): warning CS8321: The local function 'local' is declared but never used
                // void local()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(2, 6),
                // (2,8): warning CS0219: The variable 'x' is assigned but its value is never used
                // string x = "x";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(2, 8),
                // (3,1): error CS0103: The name 'local' does not exist in the current context
                // local();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(3, 1),
                // (4,26): error CS0103: The name 'x' does not exist in the current context
                //     System.Console.Write(x);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(4, 26)
                );

            comp = CreateCompilation(new[] { text2, text1 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // string x = "x";
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "string").WithLocation(2, 1),
                // (2,6): warning CS8321: The local function 'local' is declared but never used
                // void local()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(2, 6),
                // (2,8): warning CS0219: The variable 'x' is assigned but its value is never used
                // string x = "x";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(2, 8),
                // (3,1): error CS0103: The name 'local' does not exist in the current context
                // local();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(3, 1),
                // (4,26): error CS0103: The name 'x' does not exist in the current context
                //     System.Console.Write(x);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(4, 26)
                );
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

            var tree = comp.SyntaxTrees.Single();
            var reference = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "c").Single();

            var model1 = comp.GetSemanticModel(tree);
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, model1.GetTypeInfo(reference).Nullability.FlowState);

            var model2 = comp.GetSemanticModel(tree);
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, model1.GetTypeInfo(reference).Nullability.FlowState);
        }

        [Fact]
        public void FlowAnalysis_02()
        {
            var text = @"
System.Console.WriteLine();

if (args.Length == 0)
{
    return 10;
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (2,1): error CS0161: '<top-level-statements-entry-point>': not all code paths return a value
                // System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_ReturnExpected, @"System.Console.WriteLine();

if (args.Length == 0)
{
    return 10;
}
").WithArguments("<top-level-statements-entry-point>").WithLocation(2, 1)
                );
        }

        [Fact]
        public void NullableRewrite_01()
        {
            var text1 = @"
void local1()
{
    System.Console.WriteLine(""local1 - "" + s);
}
";
            var text2 = @"
using System;

string s = ""Hello world!"";

foreach (var c in s)
{
    Console.Write(c);
}

goto label1;
label1: Console.WriteLine();

local1();
local2();
";
            var text3 = @"
void local2()
{
    System.Console.WriteLine(""local2 - "" + s);
}
";

            var comp = CreateCompilation(new[] { text1, text2, text3 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            var tree = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree);

            foreach (var id in tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                _ = model1.GetTypeInfo(id).Nullability;
            }

            var model2 = comp.GetSemanticModel(tree);
            foreach (var id in tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                _ = model2.GetTypeInfo(id).Nullability;
            }
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
                // (16,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 20),
                // (34,38): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(34, 38),
                // (35,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test.ToString(); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(35, 13),
                // (36,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test.EndsWith(null); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(36, 13),
                // (37,24): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             _ = nameof(Test); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(37, 24)
                );

            var getHashCode = ((Compilation)comp).GetMember("System.Object." + nameof(GetHashCode));
            var testType = ((Compilation)comp).GetTypeByMetadataName("Test");

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var nameRefs = tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Test").ToArray();

            var nameRef = nameRefs[1];
            Assert.Equal("System.Console.WriteLine(Test)", nameRef.Parent.Parent.Parent.ToString());
            Assert.Same(declSymbol, model1.GetSymbolInfo(nameRef).Symbol);
            verifyModel(declSymbol, model1, nameRef);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            nameRef = nameRefs[0];
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
                // (13,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 20),
                // (31,38): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(31, 38),
                // (32,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test.ToString(); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(32, 13),
                // (33,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test.EndsWith(null); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(33, 13),
                // (34,24): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             _ = nameof(Test); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(34, 24)
                );

            var getHashCode = ((Compilation)comp).GetMember("System.Object." + nameof(GetHashCode));
            var testType = ((Compilation)comp).GetTypeByMetadataName("Test");

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            Assert.Null(model2.GetDeclaredSymbol((CompilationUnitSyntax)tree2.GetRoot()));
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
                // (2,1): error CS8107: Feature 'top-level statements' is not available in C# 7.0. Please use language version 9.0 or greater.
                // string Test = "1";
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"string Test = ""1"";").WithArguments("top-level statements", "9.0").WithLocation(2, 1)
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
                // (16,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 33),
                // (21,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(21, 20),
                // (36,38): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test()); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(36, 38),
                // (37,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().ToString(); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(37, 13),
                // (38,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().EndsWith(null); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(38, 13),
                // (39,45): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             var d = new System.Func<string>(Test); // 9
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(39, 45),
                // (41,24): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             _ = nameof(Test); // 10
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(41, 24)
                );

            var testType = ((Compilation)comp).GetTypeByMetadataName("Test");

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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
                // (13,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 33),
                // (18,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 20),
                // (33,38): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test()); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(33, 38),
                // (34,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().ToString(); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(34, 13),
                // (35,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().EndsWith(null); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(35, 13),
                // (36,45): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             var d = new System.Func<string>(Test); // 9
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(36, 45),
                // (38,24): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             _ = nameof(Test); // 10
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(38, 24)
                );

            var testType = ((Compilation)comp).GetTypeByMetadataName("Test");

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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
                // (2,1): error CS8107: Feature 'top-level statements' is not available in C# 7.0. Please use language version 9.0 or greater.
                // string Test() => "1";
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"string Test() => ""1"";").WithArguments("top-level statements", "9.0").WithLocation(2, 1)
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

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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
                // (16,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 33),
                // (21,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(21, 20),
                // (36,38): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test()); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(36, 38),
                // (37,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().ToString(); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(37, 13),
                // (38,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().EndsWith(null); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(38, 13),
                // (39,45): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             var d = new System.Func<string>(Test); // 9
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(39, 45),
                // (41,24): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
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

            var comp = CreateCompilation(text, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 33),
                // (21,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(21, 20),
                // (36,38): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             System.Console.WriteLine(Test()); // 6
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(36, 38),
                // (37,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().ToString(); // 7
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(37, 13),
                // (38,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             Test().EndsWith(null); // 8
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(38, 13),
                // (39,45): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //             var d = new System.Func<string>(Test); // 9
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(39, 45),
                // (41,24): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
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
                // (9,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //     T = Test,
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(9, 9),
                // (16,13): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
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
                // (3,1): error CS8802: Only one compilation unit can have top-level statements.
                // void local()
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "void").WithLocation(3, 1),
                // (3,6): warning CS8321: The local function 'local' is declared but never used
                // void local()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(3, 6),
                // (4,1): error CS0246: The type or namespace name 'alias2' could not be found (are you missing a using directive or an assembly reference?)
                // alias2 y = "1";
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias2").WithArguments("alias2").WithLocation(4, 1),
                // (5,5): error CS0246: The type or namespace name 'alias1' could not be found (are you missing a using directive or an assembly reference?)
                //     alias1 a = "2";
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias1").WithArguments("alias1").WithLocation(5, 5),
                // (7,1): error CS0103: The name 'local' does not exist in the current context
                // local();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(7, 1)
                );

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias2").WithArguments("alias2").WithLocation(4, 1),
                // (7,1): error CS0103: The name 'local' does not exist in the current context
                // local();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(7, 1)
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
                // (3,1): error CS8802: Only one compilation unit can have top-level statements.
                // void local()
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "void").WithLocation(3, 1),
                // (3,6): warning CS8321: The local function 'local' is declared but never used
                // void local()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(3, 6),
                // (5,5): error CS0246: The type or namespace name 'alias1' could not be found (are you missing a using directive or an assembly reference?)
                //     alias1 a = "2";
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias1").WithArguments("alias1").WithLocation(5, 5)
                );
        }

        [Fact]
        public void Scope_17()
        {
            var text = @"
using alias1 = N2.Test;
using N2;
string Test = ""1"";
System.Console.WriteLine(Test);

namespace N2 { class Test {} }

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
    using N2;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 20)
                );
        }

        [Fact]
        public void Scope_18()
        {
            var text1 = @"
string Test = ""1"";
System.Console.WriteLine(Test);
";
            var text2 = @"
using alias1 = N2.Test;
using N2;
namespace N2 { class Test {} }

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
    using N2;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (13,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 20)
                );
        }

        [Fact]
        public void Scope_19()
        {
            var text = @"
using alias1 = N2.Test;
using N2;
string Test() => ""1"";
System.Console.WriteLine(Test());

namespace N2 { class Test {} }

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
    using N2;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 33),
                // (21,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(21, 20)
                );
        }

        [Fact]
        public void Scope_20()
        {
            var text1 = @"
string Test() => ""1"";
System.Console.WriteLine(Test());
";
            var text2 = @"
using alias1 = N2.Test;
using N2;
namespace N2 { class Test {} }

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
    using N2;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (13,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 33),
                // (18,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 20)
                );
        }

        [Fact]
        public void Scope_21()
        {
            var text = @"
using Test = N2.Test;

string Test = ""1"";
System.Console.WriteLine(Test);

namespace N2 { class Test {} }

class Derived : Test
{
    void M()
    {
        Test x = null;

        System.Console.WriteLine(x);
        System.Console.WriteLine(Test); // 1
        Test.ToString(); // 2
        Test.EndsWith(null); // 3
        _ = nameof(Test); // 4
    }
}

namespace N1
{
    using alias2 = Test;
    using Test = N2.Test;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 20)
                );
        }

        [Fact]
        public void Scope_22()
        {
            var text1 = @"
string Test = ""1"";
System.Console.WriteLine(Test);
";
            var text2 = @"
using Test = N2.Test;

namespace N2 { class Test {} }

class Derived : Test
{
    void M()
    {
        Test x = null;

        System.Console.WriteLine(x);
        System.Console.WriteLine(Test); // 1
        Test.ToString(); // 2
        Test.EndsWith(null); // 3
        _ = nameof(Test); // 4
    }
}

namespace N1
{
    using alias2 = Test;
    using Test = N2.Test;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (13,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 20)
                );
        }

        [Fact]
        public void Scope_23()
        {
            var text = @"
using Test = N2.Test;

string Test() => ""1"";
System.Console.WriteLine(Test());

namespace N2 { class Test {} }

class Derived : Test
{
    void M()
    {
        Test x = null;

        System.Console.WriteLine(x);
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
    using Test = N2.Test;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 33),
                // (21,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(21, 20)
                );
        }

        [Fact]
        public void Scope_24()
        {
            var text1 = @"
string Test() => ""1"";
System.Console.WriteLine(Test());
";
            var text2 = @"
using Test = N2.Test;

namespace N2 { class Test {} }

class Derived : Test
{
    void M()
    {
        Test x = null;

        System.Console.WriteLine(x);
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
    using Test = N2.Test;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (13,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 33),
                // (18,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 20)
                );
        }

        [Fact]
        public void Scope_25()
        {
            var text = @"
using alias1 = N2.Test;
using static N2;
string Test = ""1"";
System.Console.WriteLine(Test);

class N2 { public class Test {} }

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
    using static N2;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 20)
                );
        }

        [Fact]
        public void Scope_26()
        {
            var text1 = @"
string Test = ""1"";
System.Console.WriteLine(Test);
";
            var text2 = @"
using alias1 = N2.Test;
using static N2;
class N2 { public class Test {} }

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
    using static N2;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (13,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test.EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 20)
                );
        }

        [Fact]
        public void Scope_27()
        {
            var text = @"
using alias1 = N2.Test;
using static N2;
string Test() => ""1"";
System.Console.WriteLine(Test());

class N2 { public class Test {} }

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
    using static N2;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (16,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 34),
                // (17,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(17, 9),
                // (18,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 9),
                // (19,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(19, 33),
                // (21,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(21, 20)
                );
        }

        [Fact]
        public void Scope_28()
        {
            var text1 = @"
string Test() => ""1"";
System.Console.WriteLine(Test());
";
            var text2 = @"
using alias1 = N2.Test;
using static N2;
class N2 { public class Test {} }

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
    using static N2;

    class Derived : Test
    {
        void M()
        {
            Test x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (13,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 33),
                // (18,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 20)
                );
        }

        [Fact]
        public void Scope_29()
        {
            var text = @"
using static N2;

string Test() => ""1"";
System.Console.WriteLine(Test());

class N2 { public static string Test() => null; }

class Derived
{
    void M()
    {
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
    using static N2;

    class Derived
    {
        void M()
        {
            System.Console.WriteLine(Test());
            Test().ToString();
            Test().EndsWith(null);
            var d = new System.Func<string>(Test);
            d();
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using static N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static N2;").WithLocation(2, 1),
                // (13,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 34),
                // (14,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(14, 9),
                // (15,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 9),
                // (16,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(16, 33),
                // (18,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(18, 20)
                );
        }

        [Fact]
        public void Scope_30()
        {
            var text1 = @"
string Test() => ""1"";
System.Console.WriteLine(Test());
";
            var text2 = @"
using static N2;

class N2 { public static string Test() => null; }

class Derived
{
    void M()
    {
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
    using static N2;

    class Derived
    {
        void M()
        {
            System.Console.WriteLine(Test());
            Test().ToString();
            Test().EndsWith(null);
            var d = new System.Func<string>(Test);
            d();
            _ = nameof(Test);
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using static N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static N2;").WithLocation(2, 1),
                // (10,34): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Console.WriteLine(Test()); // 1
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(10, 34),
                // (11,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().ToString(); // 2
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(11, 9),
                // (12,9): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         Test().EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(12, 9),
                // (13,33): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         System.Func<string> d = Test; // 4
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(13, 33),
                // (15,20): error CS8801: Cannot use local variable or local function 'Test' declared in a top-level statement in this context.
                //         _ = nameof(Test); // 5
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "Test").WithArguments("Test").WithLocation(15, 20)
                );
        }

        [Fact]
        public void Scope_31()
        {
            var text = @"
using alias1 = args;

System.Console.WriteLine(args);

class @args {}

class Derived : args
{
    void M()
    {
        args x = null;
        alias1 y = x;
        System.Console.WriteLine(y);
        System.Console.WriteLine(args); // 1
        args.ToString(); // 2
        args[0].EndsWith(null); // 3
        _ = nameof(args);
    }
}

namespace N1
{
    using alias2 = args;

    class Derived : args
    {
        void M()
        {
            args x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            System.Console.WriteLine(args); // 4
            args.ToString(); // 5
            args[0].EndsWith(null); // 6
            _ = nameof(args);
        }
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (15,34): error CS0119: 'args' is a type, which is not valid in the given context
                //         System.Console.WriteLine(args); // 1
                Diagnostic(ErrorCode.ERR_BadSKunknown, "args").WithArguments("args", "type").WithLocation(15, 34),
                // (16,9): error CS0120: An object reference is required for the non-static field, method, or property 'object.ToString()'
                //         args.ToString(); // 2
                Diagnostic(ErrorCode.ERR_ObjectRequired, "args.ToString").WithArguments("object.ToString()").WithLocation(16, 9),
                // (17,9): error CS0119: 'args' is a type, which is not valid in the given context
                //         args[0].EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_BadSKunknown, "args").WithArguments("args", "type").WithLocation(17, 9),
                // (33,38): error CS0119: 'args' is a type, which is not valid in the given context
                //             System.Console.WriteLine(args); // 4
                Diagnostic(ErrorCode.ERR_BadSKunknown, "args").WithArguments("args", "type").WithLocation(33, 38),
                // (34,13): error CS0120: An object reference is required for the non-static field, method, or property 'object.ToString()'
                //             args.ToString(); // 5
                Diagnostic(ErrorCode.ERR_ObjectRequired, "args.ToString").WithArguments("object.ToString()").WithLocation(34, 13),
                // (35,13): error CS0119: 'args' is a type, which is not valid in the given context
                //             args[0].EndsWith(null); // 6
                Diagnostic(ErrorCode.ERR_BadSKunknown, "args").WithArguments("args", "type").WithLocation(35, 13)
                );

            var testType = ((Compilation)comp).GetTypeByMetadataName("args");

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var nameRefs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "args").ToArray();

            var nameRef = nameRefs[0];
            Assert.Equal("using alias1 = args;", nameRef.Parent.ToString());

            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            var names = model.LookupNames(nameRef.SpanStart);
            Assert.Contains("args", names);

            var symbols = model.LookupSymbols(nameRef.SpanStart);
            Assert.Contains(testType, symbols);
            Assert.False(symbols.Any(s => s.Kind == SymbolKind.Parameter));
            Assert.Same(testType, model.LookupSymbols(nameRef.SpanStart, name: "args").Single());

            nameRef = nameRefs[1];
            Assert.Equal("System.Console.WriteLine(args)", nameRef.Parent.Parent.Parent.ToString());
            var parameter = model.GetSymbolInfo(nameRef).Symbol;
            Assert.Equal("System.String[] args", parameter.ToTestDisplayString());
            Assert.Equal("<top-level-statements-entry-point>", parameter.ContainingSymbol.ToTestDisplayString());

            names = model.LookupNames(nameRef.SpanStart);
            Assert.Contains("args", names);

            symbols = model.LookupSymbols(nameRef.SpanStart);
            Assert.DoesNotContain(testType, symbols);
            Assert.Contains(parameter, symbols);
            Assert.Same(parameter, model.LookupSymbols(nameRef.SpanStart, name: "args").Single());

            symbols = model.LookupNamespacesAndTypes(nameRef.SpanStart);
            Assert.Contains(testType, symbols);
            Assert.DoesNotContain(parameter, symbols);
            Assert.Same(testType, model.LookupNamespacesAndTypes(nameRef.SpanStart, name: "args").Single());

            nameRef = nameRefs[2];
            Assert.Equal(": args", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(parameter, model, nameRef);

            nameRef = nameRefs[4];
            Assert.Equal("System.Console.WriteLine(args)", nameRef.Parent.Parent.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(parameter, model, nameRef);

            nameRef = nameRefs[8];
            Assert.Equal("using alias2 = args;", nameRef.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(parameter, model, nameRef);

            nameRef = nameRefs[9];
            Assert.Equal(": args", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(parameter, model, nameRef);

            nameRef = nameRefs[11];
            Assert.Equal("System.Console.WriteLine(args)", nameRef.Parent.Parent.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(parameter, model, nameRef);

            void verifyModel(ISymbol declSymbol, SemanticModel model, IdentifierNameSyntax nameRef)
            {
                var names = model.LookupNames(nameRef.SpanStart);
                Assert.Contains("args", names);

                var symbols = model.LookupSymbols(nameRef.SpanStart);
                Assert.Contains(testType, symbols);
                Assert.DoesNotContain(declSymbol, symbols);
                Assert.Same(testType, model.LookupSymbols(nameRef.SpanStart, name: "args").Single());

                symbols = model.LookupNamespacesAndTypes(nameRef.SpanStart);
                Assert.Contains(testType, symbols);
                Assert.DoesNotContain(declSymbol, symbols);
                Assert.Same(testType, model.LookupNamespacesAndTypes(nameRef.SpanStart, name: "args").Single());
            }
        }

        [Fact]
        public void Scope_32()
        {
            var text1 = @"
System.Console.WriteLine(args);
";
            var text2 = @"
using alias1 = args;

class @args {}

class Derived : args
{
    void M()
    {
        args x = null;
        alias1 y = x;
        System.Console.WriteLine(y);
        System.Console.WriteLine(args); // 1
        args.ToString(); // 2
        args[0].EndsWith(null); // 3
        _ = nameof(args);
    }
}

namespace N1
{
    using alias2 = args;

    class Derived : args
    {
        void M()
        {
            args x = null;
            alias2 y = x;
            System.Console.WriteLine(y);
            System.Console.WriteLine(args); // 4
            args.ToString(); // 5
            args[0].EndsWith(null); // 6
            _ = nameof(args);
        }
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (13,34): error CS0119: 'args' is a type, which is not valid in the given context
                //         System.Console.WriteLine(args); // 1
                Diagnostic(ErrorCode.ERR_BadSKunknown, "args").WithArguments("args", "type").WithLocation(13, 34),
                // (14,9): error CS0120: An object reference is required for the non-static field, method, or property 'object.ToString()'
                //         args.ToString(); // 2
                Diagnostic(ErrorCode.ERR_ObjectRequired, "args.ToString").WithArguments("object.ToString()").WithLocation(14, 9),
                // (15,9): error CS0119: 'args' is a type, which is not valid in the given context
                //         args[0].EndsWith(null); // 3
                Diagnostic(ErrorCode.ERR_BadSKunknown, "args").WithArguments("args", "type").WithLocation(15, 9),
                // (31,38): error CS0119: 'args' is a type, which is not valid in the given context
                //             System.Console.WriteLine(args); // 4
                Diagnostic(ErrorCode.ERR_BadSKunknown, "args").WithArguments("args", "type").WithLocation(31, 38),
                // (32,13): error CS0120: An object reference is required for the non-static field, method, or property 'object.ToString()'
                //             args.ToString(); // 5
                Diagnostic(ErrorCode.ERR_ObjectRequired, "args.ToString").WithArguments("object.ToString()").WithLocation(32, 13),
                // (33,13): error CS0119: 'args' is a type, which is not valid in the given context
                //             args[0].EndsWith(null); // 6
                Diagnostic(ErrorCode.ERR_BadSKunknown, "args").WithArguments("args", "type").WithLocation(33, 13)
                );

            var testType = ((Compilation)comp).GetTypeByMetadataName("args");

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree = comp.SyntaxTrees[1];
            var model = comp.GetSemanticModel(tree);
            var nameRefs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "args").ToArray();

            var nameRef = nameRefs[0];
            Assert.Equal("using alias1 = args;", nameRef.Parent.ToString());

            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            var names = model.LookupNames(nameRef.SpanStart);
            Assert.Contains("args", names);

            var symbols = model.LookupSymbols(nameRef.SpanStart);
            Assert.Contains(testType, symbols);
            Assert.False(symbols.Any(s => s.Kind == SymbolKind.Parameter));
            Assert.Same(testType, model.LookupSymbols(nameRef.SpanStart, name: "args").Single());

            nameRef = nameRefs[1];
            Assert.Equal(": args", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(model, nameRef);

            nameRef = nameRefs[3];
            Assert.Equal("System.Console.WriteLine(args)", nameRef.Parent.Parent.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(model, nameRef);

            nameRef = nameRefs[7];
            Assert.Equal("using alias2 = args;", nameRef.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(model, nameRef);

            nameRef = nameRefs[8];
            Assert.Equal(": args", nameRef.Parent.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(model, nameRef);

            nameRef = nameRefs[10];
            Assert.Equal("System.Console.WriteLine(args)", nameRef.Parent.Parent.Parent.ToString());
            Assert.Same(testType, model.GetSymbolInfo(nameRef).Symbol);
            verifyModel(model, nameRef);

            void verifyModel(SemanticModel model, IdentifierNameSyntax nameRef)
            {
                var names = model.LookupNames(nameRef.SpanStart);
                Assert.Contains("args", names);

                var symbols = model.LookupSymbols(nameRef.SpanStart);
                Assert.Contains(testType, symbols);
                Assert.False(symbols.Any(s => s.Kind == SymbolKind.Parameter));
                Assert.Same(testType, model.LookupSymbols(nameRef.SpanStart, name: "args").Single());

                symbols = model.LookupNamespacesAndTypes(nameRef.SpanStart);
                Assert.Contains(testType, symbols);
                Assert.False(symbols.Any(s => s.Kind == SymbolKind.Parameter));
                Assert.Same(testType, model.LookupNamespacesAndTypes(nameRef.SpanStart, name: "args").Single());
            }
        }

        [Fact]
        public void Scope_33()
        {
            var text = @"
System.Console.WriteLine(args);

class Test
{
    void M()
    {
        System.Console.WriteLine(args);
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (8,34): error CS0103: The name 'args' does not exist in the current context
                //         System.Console.WriteLine(args);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "args").WithArguments("args").WithLocation(8, 34)
                );
        }

        [Fact]
        public void Scope_34()
        {
            var text1 = @"
System.Console.WriteLine(args);
";
            var text2 = @"
class Test
{
    void M()
    {
        System.Console.WriteLine(args);
    }
}
";

            var comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,34): error CS0103: The name 'args' does not exist in the current context
                //         System.Console.WriteLine(args);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "args").WithArguments("args").WithLocation(6, 34)
                );
        }

        [Fact]
        public void LocalFunctionStatement_01()
        {
            var text = @"
local();

void local()
{
    System.Console.WriteLine(15);
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "15");

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var reference = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "local").Single();

            var local = model.GetDeclaredSymbol(declarator);
            Assert.Same(local, model.GetSymbolInfo(reference).Symbol);
            Assert.Equal("void local()", local.ToTestDisplayString());
            Assert.Equal(MethodKind.LocalFunction, ((IMethodSymbol)local).MethodKind);

            Assert.Equal(SymbolKind.Method, local.ContainingSymbol.Kind);
            Assert.False(local.ContainingSymbol.IsImplicitlyDeclared);
            Assert.Equal(SymbolKind.NamedType, local.ContainingSymbol.ContainingSymbol.Kind);
            Assert.False(local.ContainingSymbol.ContainingSymbol.IsImplicitlyDeclared);
            Assert.True(((INamespaceSymbol)local.ContainingSymbol.ContainingSymbol.ContainingSymbol).IsGlobalNamespace);

            VerifyFlowGraph(comp, tree.GetRoot(),
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Methods: [void local()]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'local();')
              Expression: 
                IInvocationOperation (void local()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'local()')
                  Instance Receiver: 
                    null
                  Arguments(0)
        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local()
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Block
            Predecessors: [B0#0R1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... teLine(15);')
                  Expression: 
                    IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... iteLine(15)')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '15')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 15) (Syntax: '15')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B2#0R1]
        Block[B2#0R1] - Exit
            Predecessors: [B1#0R1]
            Statements (0)
    }
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
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
                // (4,1): error CS1547: Keyword 'void' cannot be used in this context
                // void I1.local()
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(4, 1),
                // (4,6): warning CS0168: The variable 'I1' is declared but never used
                // void I1.local()
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "I1").WithArguments("I1").WithLocation(4, 6),
                // (4,8): error CS1003: Syntax error, ',' expected
                // void I1.local()
                Diagnostic(ErrorCode.ERR_SyntaxError, ".").WithArguments(",").WithLocation(4, 8),
                // (4,9): error CS1002: ; expected
                // void I1.local()
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "local").WithLocation(4, 9),
                // (4,9): error CS0103: The name 'local' does not exist in the current context
                // void I1.local()
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local").WithArguments("local").WithLocation(4, 9),
                // (4,16): error CS1002: ; expected
                // void I1.local()
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 16)
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
                // (14,14): error CS0751: A partial method must be declared within a partial type
                // partial void localG() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyInPartialClass, "localG").WithLocation(14, 14),
                // (15,1): error CS0103: The name 'localG' does not exist in the current context
                // localG();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "localG").WithArguments("localG").WithLocation(15, 1),
                // (16,13): error CS0179: 'localH()' cannot be extern and declare a body
                // extern void localH() => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_ExternHasBody, "localH").WithArguments("localH()").WithLocation(16, 13),
                // (20,1): warning CS0612: 'localI()' is obsolete
                // localI();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "localI()").WithArguments("localI()").WithLocation(20, 1)
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
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // void local1(byte y)
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "void").WithLocation(2, 1),
                // (5,1): error CS0103: The name 'local2' does not exist in the current context
                // local2();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "local2").WithArguments("local2").WithLocation(5, 1),
                // (5,6): warning CS8321: The local function 'local2' is declared but never used
                // void local2()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local2").WithArguments("local2").WithLocation(5, 6)
                );

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var symbol1 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single());
            Assert.Equal("void local1(System.Int32 x)", symbol1.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "local1").Single()).Symbol);

            var tree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(tree2);
            var symbol2 = model2.GetDeclaredSymbol(tree2.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().First());
            Assert.Equal("void local1(System.Byte y)", symbol2.ToTestDisplayString());
            Assert.Same(symbol2, model2.GetSymbolInfo(tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "local1").Single()).Symbol);
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
        public void LocalFunctionStatement_09()
        {
            var text1 = @"
local1(1);
void local1(int x)
{}
local2();

void local1(byte y)
{}

void local2()
{
    local1(2);
}
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (7,6): error CS0128: A local variable or function named 'local1' is already defined in this scope
                // void local1(byte y)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "local1").WithArguments("local1").WithLocation(7, 6),
                // (7,6): warning CS8321: The local function 'local1' is declared but never used
                // void local1(byte y)
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local1").WithArguments("local1").WithLocation(7, 6)
                );

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var symbol1 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().First());
            Assert.Equal("void local1(System.Int32 x)", symbol1.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "local1").First()).Symbol);

            var symbol2 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Skip(1).First());
            Assert.Equal("void local1(System.Byte y)", symbol2.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "local1").Skip(1).Single()).Symbol);
        }

        [Fact]
        public void LocalFunctionStatement_10()
        {
            var text = @"
int i = 1;
local();
System.Console.WriteLine(i);

void local()
{
    i++;
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "2");
        }

        [Fact]
        public void LocalFunctionStatement_11()
        {
            var text1 = @"
args(1);
void args(int x)
{}
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (3,6): error CS0136: A local or parameter named 'args' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // void args(int x)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "args").WithArguments("args").WithLocation(3, 6)
                );

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(tree1);
            var symbol1 = model1.GetDeclaredSymbol(tree1.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().First());
            Assert.Equal("void args(System.Int32 x)", symbol1.ToTestDisplayString());
            Assert.Same(symbol1, model1.GetSymbolInfo(tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "args").Single()).Symbol);
        }

        [Fact]
        public void LocalFunctionStatement_12()
        {
            var text1 = @"
local(1);
void local<args>(args x)
{
    System.Console.WriteLine(x);
}
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "1");
        }

        [Fact]
        public void LocalFunctionStatement_13()
        {
            var text1 = @"
local();
void local()
{
    var args = 2;
    System.Console.WriteLine(args);
}
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "2");
        }

        [Fact]
        public void LocalFunctionStatement_14()
        {
            var text1 = @"
local(3);
void local(int args)
{
    System.Console.WriteLine(args);
}
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "3");
        }

        [Fact]
        public void LocalFunctionStatement_15()
        {
            var text1 = @"
local();
void local()
{
    args(4);
    void args(int x)
    {
        System.Console.WriteLine(x);
    }
}
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "4");
        }

        [Fact]
        public void LocalFunctionStatement_16()
        {
            var text1 = @"
using System.Linq;
_ = from local in new object[0] select local;
local();
void local() {}
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (3,10): error CS1931: The range variable 'local' conflicts with a previous declaration of 'local'
                // _ = from local in new object[0] select local;
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "local").WithArguments("local").WithLocation(3, 10)
                );
        }

        [Fact]
        public void LocalFunctionStatement_17()
        {
            var text = @"
System.Console.WriteLine();
string await() => ""Hi!"";
System.Console.WriteLine(await());
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (3,8): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                // string await() => "Hi!";
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(3, 8),
                // (3,8): warning CS8321: The local function 'await' is declared but never used
                // string await() => "Hi!";
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "await").WithArguments("await").WithLocation(3, 8),
                // (4,32): error CS1525: Invalid expression term ')'
                // System.Console.WriteLine(await());
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(4, 32)
                );
        }

        [Fact]
        public void LocalFunctionStatement_18()
        {
            var text = @"
string async() => ""Hi!"";
System.Console.WriteLine(async());
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void Lambda_01()
        {
            var text = @"
int i = 1;
System.Action l = () => i++;
l();
System.Console.WriteLine(i);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "2");
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

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
            var reference = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "label1").Single();

            var label = model.GetDeclaredSymbol(declarator);
            Assert.Same(label, model.GetSymbolInfo(reference).Symbol);
            Assert.Equal("label1", label.ToTestDisplayString());
            Assert.Equal(SymbolKind.Label, label.Kind);

            Assert.Equal(SymbolKind.Method, label.ContainingSymbol.Kind);
            Assert.False(label.ContainingSymbol.IsImplicitlyDeclared);
            Assert.Equal(SymbolKind.NamedType, label.ContainingSymbol.ContainingSymbol.Kind);
            Assert.False(label.ContainingSymbol.ContainingSymbol.IsImplicitlyDeclared);
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
                // (2,1): error CS8802: Only one compilation unit can have top-level statements.
                // label1: System.Console.Write(2);
                Diagnostic(ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, "label1").WithLocation(2, 1)
                );

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

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
            Assert.Same(symbol2, model2.GetSymbolInfo(tree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "label1").Single()).Symbol);
        }

        [Fact]
        public void LabeledStatement_04()
        {
            var text = @"
goto args;
args: System.Console.WriteLine(""Hi!"");
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            CompileAndVerify(comp, expectedOutput: "Hi!");

            Assert.False(IsNullableAnalysisEnabled(comp)); // To make sure we test incremental binding for SemanticModel

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Single();
            var reference = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "args").Single();

            var label = model.GetDeclaredSymbol(declarator);
            Assert.Same(label, model.GetSymbolInfo(reference).Symbol);
            Assert.Equal("args", label.ToTestDisplayString());
            Assert.Equal(SymbolKind.Label, label.Kind);

            Assert.Equal(SymbolKind.Method, label.ContainingSymbol.Kind);
            Assert.False(label.ContainingSymbol.IsImplicitlyDeclared);
            Assert.Equal(SymbolKind.NamedType, label.ContainingSymbol.ContainingSymbol.Kind);
            Assert.False(label.ContainingSymbol.ContainingSymbol.IsImplicitlyDeclared);
            Assert.True(((INamespaceSymbol)label.ContainingSymbol.ContainingSymbol.ContainingSymbol).IsGlobalNamespace);
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
                // (2,13): warning CS7022: The entry point of the program is global code; ignoring 'Main()' entry point.
                // static void Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()").WithLocation(2, 13),
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

            comp.VerifyDiagnostics(
                // (6,13): warning CS7022: The entry point of the program is global code; ignoring 'Main()' entry point.
                // static void Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()").WithLocation(6, 13)
                );
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_03()
        {
            var text = @"
using System;
using System.Threading.Tasks;

System.Console.Write(""Hi!"");

partial class Program
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

partial class Program
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

partial class Program
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

partial class Program
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

class Program2
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
                // (9,17): warning CS7022: The entry point of the program is global code; ignoring 'Program2.Main(string[])' entry point.
                //     static void Main(string[] args)
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program2.Main(string[])").WithLocation(9, 17),
                // (14,23): warning CS7022: The entry point of the program is global code; ignoring 'Program2.Main()' entry point.
                //     static async Task Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program2.Main()").WithLocation(14, 23)
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

class Program2
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
                // (10,17): warning CS7022: The entry point of the program is global code; ignoring 'Program2.Main()' entry point.
                //     static void Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program2.Main()").WithLocation(10, 17),
                // (15,23): warning CS7022: The entry point of the program is global code; ignoring 'Program2.Main(string[])' entry point.
                //     static async Task Main(string[] args)
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program2.Main(string[])").WithLocation(15, 23)
                );

            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_09()
        {
            var text1 = @"
using System;
using System.Threading.Tasks;

string s = ""Hello world!"";

foreach (var c in s)
{
    await N1.Helpers.Wait();
    Console.Write(c);
}

Console.WriteLine();

namespace N1
{
    class Helpers
    {
        static void Main()
        { }

        public static async Task Wait()
        {
            await Task.Delay(500);
        }
    }
}";
            var text4 = @"
using System.Threading.Tasks;

class Helpers
{
    public static async Task Wait()
    {
        await Task.Delay(500);
    }
}
";

            var comp = CreateCompilation(new[] { text1, text4 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyEmitDiagnostics(
                // (19,21): warning CS7022: The entry point of the program is global code; ignoring 'Helpers.Main()' entry point.
                //         static void Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("N1.Helpers.Main()").WithLocation(19, 21)
                );
        }

        [Fact]
        public void ExplicitMain_10()
        {
            var text = @"
using System.Threading.Tasks;

System.Console.Write(""Hi!"");

class Program2
{
    static void Main()
    {
    }

    static async Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => 5);
    }
}

class Program3
{
    static void Main(string[] args)
    {
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe.WithMainTypeName("Program2"), parseOptions: DefaultParseOptions);

            comp.VerifyEmitDiagnostics(
                // error CS8804: Cannot specify /main if there is a compilation unit with top-level statements.
                Diagnostic(ErrorCode.ERR_SimpleProgramDisallowsMainType).WithLocation(1, 1),
                // (12,23): warning CS8892: Method 'Program2.Main(string[])' will not be used as an entry point because a synchronous entry point 'Program2.Main()' was found.
                //     static async Task Main(string[] args)
                Diagnostic(ErrorCode.WRN_SyncAndAsyncEntryPoints, "Main").WithArguments("Program2.Main(string[])", "Program2.Main()").WithLocation(12, 23)
                );
        }

        [Fact]
        public void ExplicitMain_11()
        {
            var text = @"
using System.Threading.Tasks;

System.Console.Write(""Hi!"");

partial class Program
{
    static void Main()
    {
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe.WithMainTypeName(""), parseOptions: DefaultParseOptions);

            comp.VerifyEmitDiagnostics(
                // error CS7088: Invalid 'MainTypeName' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("MainTypeName", "").WithLocation(1, 1),
                // error CS8804: Cannot specify /main if there is a compilation unit with top-level statements.
                Diagnostic(ErrorCode.ERR_SimpleProgramDisallowsMainType).WithLocation(1, 1)
                );
        }

        [Fact]
        public void ExplicitMain_12()
        {
            var text = @"
System.Console.Write(""H"");
Main();
System.Console.Write(""!"");

void Main()
{
    System.Console.Write(""i"");
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_13()
        {
            var text = @"
System.Console.Write(""H"");
Main("""");
System.Console.Write(""!"");

static void Main(string args)
{
    System.Console.Write(""i"");
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_14()
        {
            var text = @"
System.Console.Write(""H"");
Main();
System.Console.Write(""!"");

static long Main()
{
    System.Console.Write(""i"");
    return 0;
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_15()
        {
            var text = @"
System.Console.Write(""H"");
Main();
System.Console.Write(""!"");

static int Main()
{
    System.Console.Write(""i"");
    return 0;
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,12): warning CS7022: The entry point of the program is global code; ignoring 'Main()' entry point.
                // static int Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()").WithLocation(6, 12)
                );
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_16()
        {
            var text = @"
System.Console.Write(""H"");
Main(null);
System.Console.Write(""!"");

static void Main(string[] args)
{
    System.Console.Write(""i"");
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,13): warning CS7022: The entry point of the program is global code; ignoring 'Main(string[])' entry point.
                // static void Main(string[] args)
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main(string[])").WithLocation(6, 13)
                );
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_17()
        {
            var text = @"
System.Console.Write(""H"");
Main(null);
System.Console.Write(""!"");

static int Main(string[] args)
{
    System.Console.Write(""i"");
    return 0;
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,12): warning CS7022: The entry point of the program is global code; ignoring 'Main(string[])' entry point.
                // static int Main(string[] args)
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main(string[])").WithLocation(6, 12)
                );
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_18()
        {
            var text = @"
using System.Threading.Tasks;

System.Console.Write(""H"");
await Main();
System.Console.Write(""!"");

async static Task Main()
{
    System.Console.Write(""i"");
    await Task.Yield();
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (8,19): warning CS7022: The entry point of the program is global code; ignoring 'Main()' entry point.
                // async static Task Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()").WithLocation(8, 19)
                );
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_19()
        {
            var text = @"
using System.Threading.Tasks;

System.Console.Write(""H"");
await Main();
System.Console.Write(""!"");

static async Task<int> Main()
{
    System.Console.Write(""i"");
    await Task.Yield();
    return 0;
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (8,24): warning CS7022: The entry point of the program is global code; ignoring 'Main()' entry point.
                // static async Task<int> Main()
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()").WithLocation(8, 24)
                );
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_20()
        {
            var text = @"
using System.Threading.Tasks;

System.Console.Write(""H"");
await Main(null);
System.Console.Write(""!"");

static async Task Main(string[] args)
{
    System.Console.Write(""i"");
    await Task.Yield();
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (8,19): warning CS7022: The entry point of the program is global code; ignoring 'Main(string[])' entry point.
                // static async Task Main(string[] args)
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main(string[])").WithLocation(8, 19)
                );
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_21()
        {
            var text = @"
using System.Threading.Tasks;

System.Console.Write(""H"");
await Main(null);
System.Console.Write(""!"");

static async Task<int> Main(string[] args)
{
    System.Console.Write(""i"");
    await Task.Yield();
    return 0;
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (8,24): warning CS7022: The entry point of the program is global code; ignoring 'Main(string[])' entry point.
                // static async Task<int> Main(string[] args)
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main(string[])").WithLocation(8, 24)
                );
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_22()
        {
            var text = @"
System.Console.Write(""H"");
Main<int>();
System.Console.Write(""!"");

static void Main<T>()
{
    System.Console.Write(""i"");
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_23()
        {
            var text = @"
System.Console.Write(""H"");
local();
System.Console.Write(""!"");

static void local()
{
    Main();

    static void Main()
    {
        System.Console.Write(""i"");
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void ExplicitMain_24()
        {
            var text = @"
System.Console.Write(42);

partial class Program
{
    static partial void Main(string[] args);
}
";
            var comp = CreateCompilation(text);
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Yield_01()
        {
            var text = @"yield break;";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (1,1): error CS1624: The body of '<top-level-statements-entry-point>' cannot be an iterator block because 'void' is not an iterator interface type
                // yield break;
                Diagnostic(ErrorCode.ERR_BadIteratorReturn, "yield break;").WithArguments("<top-level-statements-entry-point>", "void").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Yield_02()
        {
            var text = @"{yield return 0;}";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (1,1): error CS1624: The body of '<top-level-statements-entry-point>' cannot be an iterator block because 'void' is not an iterator interface type
                // {yield return 0;}
                Diagnostic(ErrorCode.ERR_BadIteratorReturn, "{yield return 0;}").WithArguments("<top-level-statements-entry-point>", "void").WithLocation(1, 1)
                );
        }

        [Fact]
        public void OutOfOrder_01()
        {
            var text = @"
class C {}

System.Console.WriteLine(1);
System.Console.WriteLine(2);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (4,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "System.Console.WriteLine(1);").WithLocation(4, 1)
                );
        }

        [Fact]
        public void OutOfOrder_02()
        {
            var text = @"
System.Console.WriteLine(0);

namespace C {}

System.Console.WriteLine(1);
System.Console.WriteLine(2);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "System.Console.WriteLine(1);").WithLocation(6, 1)
                );
        }

        [Fact]
        public void OutOfOrder_03()
        {
            var text = @"
class C {}

System.Console.WriteLine(1);
System.Console.WriteLine(2);

class D {}

System.Console.WriteLine(3);
System.Console.WriteLine(4);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (4,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "System.Console.WriteLine(1);").WithLocation(4, 1)
                );
        }

        [Fact]
        public void OutOfOrder_04()
        {
            var text = @"
System.Console.WriteLine(0);

namespace C {}

System.Console.WriteLine(1);
System.Console.WriteLine(2);

namespace D {}

System.Console.WriteLine(3);
System.Console.WriteLine(4);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "System.Console.WriteLine(1);").WithLocation(6, 1)
                );
        }

        [Fact]
        public void OutOfOrder_05()
        {
            var text = @"
System.Console.WriteLine(0);

struct S {}

System.Console.WriteLine(1);
System.Console.WriteLine(2);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "System.Console.WriteLine(1);").WithLocation(6, 1)
                );
        }

        [Fact]
        public void OutOfOrder_06()
        {
            var text = @"
System.Console.WriteLine(0);

enum C { V }

System.Console.WriteLine(1);
System.Console.WriteLine(2);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "System.Console.WriteLine(1);").WithLocation(6, 1)
                );
        }

        [Fact]
        public void OutOfOrder_07()
        {
            var text = @"
System.Console.WriteLine(0);

interface C {}

System.Console.WriteLine(1);
System.Console.WriteLine(2);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "System.Console.WriteLine(1);").WithLocation(6, 1)
                );
        }

        [Fact]
        public void OutOfOrder_08()
        {
            var text = @"
System.Console.WriteLine(0);

delegate void D ();

System.Console.WriteLine(1);
System.Console.WriteLine(2);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "System.Console.WriteLine(1);").WithLocation(6, 1)
                );
        }

        [Fact]
        public void OutOfOrder_09()
        {
            var text = @"
System.Console.WriteLine(0);

using System;

Console.WriteLine(1);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (4,1): error CS1529: A using clause must precede all other elements defined in the namespace except extern alias declarations
                // using System;
                Diagnostic(ErrorCode.ERR_UsingAfterElements, "using System;").WithLocation(4, 1),
                // (6,1): error CS0103: The name 'Console' does not exist in the current context
                // Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Console").WithArguments("Console").WithLocation(6, 1)
                );
        }

        [Fact]
        public void OutOfOrder_10()
        {
            var text = @"
System.Console.WriteLine(0);

[module: MyAttribute]

class MyAttribute : System.Attribute
{}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (4,2): error CS1730: Assembly and module attributes must precede all other elements defined in a file except using clauses and extern alias declarations
                // [module: MyAttribute]
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotFirst, "module").WithLocation(4, 2)
                );
        }

        [Fact]
        public void OutOfOrder_11()
        {
            var text = @"
System.Console.WriteLine(0);

extern alias A;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (4,1): error CS0439: An extern alias declaration must precede all other elements defined in the namespace
                // extern alias A;
                Diagnostic(ErrorCode.ERR_ExternAfterElements, "extern").WithLocation(4, 1)
                );
        }

        [Fact]
        public void OutOfOrder_12()
        {
            var text = @"
extern alias A;
using System;

[module: MyAttribute]

Console.WriteLine(1);

class MyAttribute : System.Attribute
{}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (2,1): hidden CS8020: Unused extern alias.
                // extern alias A;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias A;").WithLocation(2, 1),
                // (2,14): error CS0430: The extern alias 'A' was not specified in a /reference option
                // extern alias A;
                Diagnostic(ErrorCode.ERR_BadExternAlias, "A").WithArguments("A").WithLocation(2, 14)
                );
        }

        [Fact]
        public void OutOfOrder_13()
        {
            var text = @"
local();

class C {}

void local() => System.Console.WriteLine(1);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (6,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // void local() => System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "void local() => System.Console.WriteLine(1);").WithLocation(6, 1)
                );
        }

        [Fact]
        public void Attributes_01()
        {
            var text1 = @"
[MyAttribute(i)]
const int i = 1;

[MyAttribute(i + 1)]
System.Console.Write(i);

[MyAttribute(i + 2)]
int j = i;
System.Console.Write(j);

[MyAttribute(i + 3)]
new MyAttribute(i);

[MyAttribute(i + 4)]
local();

[MyAttribute(i + 5)]
void local() {}

class MyAttribute : System.Attribute
{
    public MyAttribute(int x) {}
}
";
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (2,1): error CS7014: Attributes are not valid in this context.
                // [MyAttribute(i)]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[MyAttribute(i)]").WithLocation(2, 1),
                // (5,1): error CS7014: Attributes are not valid in this context.
                // [MyAttribute(i + 1)]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[MyAttribute(i + 1)]").WithLocation(5, 1),
                // (8,1): error CS7014: Attributes are not valid in this context.
                // [MyAttribute(i + 2)]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[MyAttribute(i + 2)]").WithLocation(8, 1),
                // (12,1): error CS7014: Attributes are not valid in this context.
                // [MyAttribute(i + 3)]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[MyAttribute(i + 3)]").WithLocation(12, 1),
                // (16,1): error CS0246: The type or namespace name 'local' could not be found (are you missing a using directive or an assembly reference?)
                // local();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "local").WithArguments("local").WithLocation(16, 1),
                // (16,6): error CS1001: Identifier expected
                // local();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(16, 6),
                // (16,6): error CS8112: Local function '()' must declare a body because it is not marked 'static extern'.
                // local();
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "").WithArguments("()").WithLocation(16, 6),
                // (19,6): warning CS8321: The local function 'local' is declared but never used
                // void local() {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(19, 6)
                );

            var tree1 = comp.SyntaxTrees[0];

            var model1 = comp.GetSemanticModel(tree1);
            var localDecl = tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var declSymbol = model1.GetDeclaredSymbol(localDecl);
            Assert.Equal("System.Int32 i", declSymbol.ToTestDisplayString());

            var localRefs = tree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "i").ToArray();
            Assert.Equal(9, localRefs.Length);

            foreach (var localRef in localRefs)
            {
                var refSymbol = model1.GetSymbolInfo(localRef).Symbol;
                Assert.Same(declSymbol, refSymbol);
                Assert.Contains(declSymbol.Name, model1.LookupNames(localRef.SpanStart));
                Assert.Contains(declSymbol, model1.LookupSymbols(localRef.SpanStart));
                Assert.Same(declSymbol, model1.LookupSymbols(localRef.SpanStart, name: declSymbol.Name).Single());
            }

            localDecl = tree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ElementAt(1);
            declSymbol = model1.GetDeclaredSymbol(localDecl);
            Assert.Equal("System.Int32 j", declSymbol.ToTestDisplayString());
        }

        [Fact]
        public void Attributes_02()
        {
            var source = @"
using System.Runtime.CompilerServices;

return;

#pragma warning disable 8321 // Unreferenced local function

[MethodImpl(MethodImplOptions.ForwardRef)]
static void forwardRef()  { System.Console.WriteLine(0); }

[MethodImpl(MethodImplOptions.NoInlining)]
static void noInlining() { System.Console.WriteLine(1); }

[MethodImpl(MethodImplOptions.NoOptimization)]
static void noOptimization() { System.Console.WriteLine(2); }

[MethodImpl(MethodImplOptions.Synchronized)]
static void synchronized() { System.Console.WriteLine(3); }

[MethodImpl(MethodImplOptions.InternalCall)]
extern static void internalCallStatic();
";
            var verifier = CompileAndVerify(
                source,
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                parseOptions: DefaultParseOptions,
                assemblyValidator: validateAssembly,
                verify: Verification.Skipped);

            var comp = verifier.Compilation;
            var syntaxTree = comp.SyntaxTrees.Single();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var localFunctions = syntaxTree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().ToList();

            checkImplAttributes(localFunctions[0], MethodImplAttributes.ForwardRef);
            checkImplAttributes(localFunctions[1], MethodImplAttributes.NoInlining);
            checkImplAttributes(localFunctions[2], MethodImplAttributes.NoOptimization);
            checkImplAttributes(localFunctions[3], MethodImplAttributes.Synchronized);
            checkImplAttributes(localFunctions[4], MethodImplAttributes.InternalCall);

            void checkImplAttributes(LocalFunctionStatementSyntax localFunctionStatement, MethodImplAttributes expectedFlags)
            {
                var localFunction = semanticModel.GetDeclaredSymbol(localFunctionStatement).GetSymbol<LocalFunctionSymbol>();
                Assert.Equal(expectedFlags, localFunction.ImplementationAttributes);
            }

            void validateAssembly(PEAssembly assembly)
            {
                var peReader = assembly.GetMetadataReader();

                foreach (var methodHandle in peReader.MethodDefinitions)
                {
                    var methodDef = peReader.GetMethodDefinition(methodHandle);
                    var actualFlags = methodDef.ImplAttributes;

                    var methodName = peReader.GetString(methodDef.Name);
                    var expectedFlags = methodName switch
                    {
                        "<" + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName + ">g__forwardRef|0_0" => MethodImplAttributes.ForwardRef,
                        "<" + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName + ">g__noInlining|0_1" => MethodImplAttributes.NoInlining,
                        "<" + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName + ">g__noOptimization|0_2" => MethodImplAttributes.NoOptimization,
                        "<" + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName + ">g__synchronized|0_3" => MethodImplAttributes.Synchronized,
                        "<" + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName + ">g__internalCallStatic|0_4" => MethodImplAttributes.InternalCall,
                        ".ctor" => MethodImplAttributes.IL,
                        WellKnownMemberNames.TopLevelStatementsEntryPointMethodName => MethodImplAttributes.IL,
                        _ => throw TestExceptionUtilities.UnexpectedValue(methodName)
                    };

                    Assert.Equal(expectedFlags, actualFlags);
                }
            }
        }

        [Fact]
        public void Attributes_03()
        {
            var source = @"
using System.Runtime.InteropServices;

local1();

[DllImport(
    ""something.dll"",
    EntryPoint = ""a"",
    CharSet = CharSet.Ansi,
    SetLastError = true,
    ExactSpelling = true,
    PreserveSig = false,
    CallingConvention = CallingConvention.Cdecl,
    BestFitMapping = false,
    ThrowOnUnmappableChar = true)]
static extern void local1();
";
            var verifier = CompileAndVerify(
                source,
                options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                parseOptions: DefaultParseOptions,
                symbolValidator: validate,
                sourceSymbolValidator: validate,
                verify: Verification.Skipped);

            var comp = verifier.Compilation;
            var syntaxTree = comp.SyntaxTrees.Single();
            var semanticModel = comp.GetSemanticModel(syntaxTree);

            var localFunction = semanticModel
                .GetDeclaredSymbol(syntaxTree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single())
                .GetSymbol<LocalFunctionSymbol>();

            Assert.Equal(new[] { "DllImportAttribute" }, GetAttributeNames(localFunction.GetAttributes()));
            validateLocalFunction(localFunction);

            void validate(ModuleSymbol module)
            {
                var fromSource = module is SourceModuleSymbol;

                var program = module.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName);
                var programAttributes = GetAttributeNames(program.GetAttributes().As<CSharpAttributeData>());
                Assert.False(program.IsImplicitlyDeclared);
                if (fromSource)
                {
                    Assert.Empty(programAttributes);
                }
                else
                {
                    Assert.Equal(new[] { "CompilerGeneratedAttribute" }, programAttributes);
                }

                MethodSymbol method = program.GetMethod(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName);
                Assert.Empty(method.GetAttributes());
                Assert.False(method.IsImplicitlyDeclared);

                if (!fromSource)
                {
                    var localFn1 = program.GetMethod("<" + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName + ">g__local1|0_0");
                    Assert.Equal(new[] { "CompilerGeneratedAttribute" }, GetAttributeNames(localFn1.GetAttributes().As<CSharpAttributeData>()));
                    validateLocalFunction(localFn1);
                }
            }

            static void validateLocalFunction(MethodSymbol localFunction)
            {
                Assert.True(localFunction.IsExtern);

                var importData = localFunction.GetDllImportData();
                Assert.NotNull(importData);
                Assert.Equal("something.dll", importData.ModuleName);
                Assert.Equal("a", importData.EntryPointName);
                Assert.Equal(CharSet.Ansi, importData.CharacterSet);
                Assert.True(importData.SetLastError);
                Assert.True(importData.ExactSpelling);
                Assert.Equal(MethodImplAttributes.IL, localFunction.ImplementationAttributes);
                Assert.Equal(CallingConvention.Cdecl, importData.CallingConvention);
                Assert.False(importData.BestFitMapping);
                Assert.True(importData.ThrowOnUnmappableCharacter);
            }
        }

        [Fact]
        public void ModelWithIgnoredAccessibility_01()
        {
            var source = @"
new A().M();

class A
{
    A M() { return new A(); }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (2,9): error CS0122: 'A.M()' is inaccessible due to its protection level
                // new A().M();
                Diagnostic(ErrorCode.ERR_BadAccess, "M").WithArguments("A.M()").WithLocation(2, 9)
                );

            var a = ((Compilation)comp).SourceModule.GlobalNamespace.GetTypeMember("A");
            var syntaxTree = comp.SyntaxTrees.Single();
            var invocation = syntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            var semanticModel = comp.GetSemanticModel(syntaxTree);

            Assert.Equal("A", semanticModel.GetTypeInfo(invocation).Type.Name);
            Assert.Null(semanticModel.GetSymbolInfo(invocation).Symbol);
            Assert.Equal("M", semanticModel.GetSymbolInfo(invocation).CandidateSymbols.Single().Name);
            Assert.Equal(CandidateReason.Inaccessible, semanticModel.GetSymbolInfo(invocation).CandidateReason);
            Assert.Empty(semanticModel.LookupSymbols(invocation.SpanStart, container: a, name: "M"));

            semanticModel = comp.GetSemanticModel(syntaxTree, ignoreAccessibility: true);

            Assert.Equal("A", semanticModel.GetTypeInfo(invocation).Type.Name);
            Assert.Equal("M", semanticModel.GetSymbolInfo(invocation).Symbol.Name);
            Assert.NotEmpty(semanticModel.LookupSymbols(invocation.SpanStart, container: a, name: "M"));
        }

        [Fact]
        public void ModelWithIgnoredAccessibility_02()
        {
            var source = @"
var x = new A().M();

class A
{
    A M() 
    {
        x = null;
        return new A(); 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (2,17): error CS0122: 'A.M()' is inaccessible due to its protection level
                // var x = new A().M();
                Diagnostic(ErrorCode.ERR_BadAccess, "M").WithArguments("A.M()").WithLocation(2, 17),
                // (8,9): error CS8801: Cannot use local variable or local function 'x' declared in a top-level statement in this context.
                //         x = null;
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "x").WithArguments("x").WithLocation(8, 9)
                );

            var a = ((Compilation)comp).SourceModule.GlobalNamespace.GetTypeMember("A");
            var syntaxTree = comp.SyntaxTrees.Single();
            var localDecl = syntaxTree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var localRef = syntaxTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();

            var semanticModel = comp.GetSemanticModel(syntaxTree, ignoreAccessibility: true);

            var x = semanticModel.GetDeclaredSymbol(localDecl);
            Assert.Same(x, semanticModel.LookupSymbols(localDecl.SpanStart, name: "x").Single());
            Assert.Same(x, semanticModel.GetSymbolInfo(localRef).Symbol);
            Assert.Same(x, semanticModel.LookupSymbols(localRef.SpanStart, name: "x").Single());
        }

        [Fact]
        public void ModelWithIgnoredAccessibility_03()
        {
            var source = @"
var x = new B().M(1);

class A
{
    public long M(long i) => i; 
}

class B : A
{
    protected int M(int i)
    {
        _ = x;
        return i;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (13,13): error CS8801: Cannot use local variable or local function 'x' declared in a top-level statement in this context.
                //         _ = x;
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "x").WithArguments("x").WithLocation(13, 13)
                );

            var a = ((Compilation)comp).SourceModule.GlobalNamespace.GetTypeMember("A");
            var syntaxTree1 = comp.SyntaxTrees.Single();
            var localDecl = syntaxTree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var localRef = syntaxTree1.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();

            verifyModel(ignoreAccessibility: true, "System.Int32");
            verifyModel(ignoreAccessibility: false, "System.Int64");

            void verifyModel(bool ignoreAccessibility, string expectedType)
            {
                var semanticModel1 = comp.GetSemanticModel(syntaxTree1, ignoreAccessibility);

                var xDecl = semanticModel1.GetDeclaredSymbol(localDecl);
                Assert.Same(xDecl, semanticModel1.LookupSymbols(localDecl.SpanStart, name: "x").Single());
                var xRef = semanticModel1.GetSymbolInfo(localRef).Symbol;
                Assert.Same(xRef, semanticModel1.LookupSymbols(localRef.SpanStart, name: "x").Single());
                Assert.Equal(expectedType, ((ILocalSymbol)xRef).Type.ToTestDisplayString());
                Assert.Equal(expectedType, ((ILocalSymbol)xDecl).Type.ToTestDisplayString());
                Assert.Same(xDecl, xRef);
            }
        }

        [Fact]
        public void ModelWithIgnoredAccessibility_04()
        {
            var source1 = @"
var x = new B().M(1);
";
            var source2 = @"
class A
{
    public long M(long i) => i; 
}

class B : A
{
    protected int M(int i)
    {
        _ = x;
        return i;
    }
}
";
            var comp = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (11,13): error CS8801: Cannot use local variable or local function 'x' declared in a top-level statement in this context.
                //         _ = x;
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "x").WithArguments("x").WithLocation(11, 13)
                );

            var a = ((Compilation)comp).SourceModule.GlobalNamespace.GetTypeMember("A");
            var syntaxTree1 = comp.SyntaxTrees.First();
            var localDecl = syntaxTree1.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var syntaxTree2 = comp.SyntaxTrees[1];
            var localRef = syntaxTree2.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();

            verifyModel(ignoreAccessibility: true, "System.Int32");
            verifyModel(ignoreAccessibility: false, "System.Int64");

            void verifyModel(bool ignoreAccessibility, string expectedType)
            {
                var semanticModel1 = comp.GetSemanticModel(syntaxTree1, ignoreAccessibility);

                var xDecl = semanticModel1.GetDeclaredSymbol(localDecl);
                Assert.Same(xDecl, semanticModel1.LookupSymbols(localDecl.SpanStart, name: "x").Single());
                Assert.Equal(expectedType, ((ILocalSymbol)xDecl).Type.ToTestDisplayString());

                var semanticModel2 = comp.GetSemanticModel(syntaxTree2, ignoreAccessibility);

                var xRef = semanticModel2.GetSymbolInfo(localRef).Symbol;
                Assert.Same(xRef, semanticModel2.LookupSymbols(localRef.SpanStart, name: "x").Single());
                Assert.Equal(expectedType, ((ILocalSymbol)xRef).Type.ToTestDisplayString());
                Assert.Same(xDecl, xRef);
            }
        }

        [Fact]
        public void AnalyzerActions_01()
        {
            var text1 = @"System.Console.WriteLine(1);";

            var analyzer = new AnalyzerActions_01_Analyzer();
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(0, analyzer.FireCount4);

            var text2 = @"System.Console.WriteLine(2);";

            analyzer = new AnalyzerActions_01_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
        }

        private class AnalyzerActions_01_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(Handle1, SyntaxKind.GlobalStatement);
                context.RegisterSyntaxNodeAction(Handle2, SyntaxKind.CompilationUnit);
            }

            private void Handle1(SyntaxNodeAnalysisContext context)
            {
                var model = context.SemanticModel;
                var globalStatement = (GlobalStatementSyntax)context.Node;
                var syntaxTreeModel = ((SyntaxTreeSemanticModel)model);

                switch (globalStatement.ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Equal("<top-level-statements-entry-point>", context.ContainingSymbol.ToTestDisplayString());
                Assert.Same(globalStatement.SyntaxTree, context.ContainingSymbol.DeclaringSyntaxReferences.Single().SyntaxTree);
                Assert.True(syntaxTreeModel.TestOnlyMemberModels.ContainsKey(globalStatement.Parent));

                MemberSemanticModel mm = syntaxTreeModel.TestOnlyMemberModels[globalStatement.Parent];

                Assert.False(mm.TestOnlyTryGetBoundNodesFromMap(globalStatement.Statement).IsEmpty);

                Assert.Same(mm, syntaxTreeModel.GetMemberModel(globalStatement.Statement));
            }

            private void Handle2(SyntaxNodeAnalysisContext context)
            {
                var model = context.SemanticModel;
                var unit = (CompilationUnitSyntax)context.Node;
                var syntaxTreeModel = ((SyntaxTreeSemanticModel)model);

                switch (unit.ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount3);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount4);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "<global namespace>":
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_02()
        {
            var text1 = @"System.Console.WriteLine(1);";

            var analyzer = new AnalyzerActions_02_Analyzer();
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount2);

            var text2 = @"System.Console.WriteLine(2);";

            analyzer = new AnalyzerActions_02_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
        }

        private class AnalyzerActions_02_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(Handle, SymbolKind.Method);
            }

            private void Handle(SymbolAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.Symbol.ToTestDisplayString());

                switch (context.Symbol.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_03()
        {
            var text1 = @"System.Console.WriteLine(1);";

            var analyzer = new AnalyzerActions_03_Analyzer();
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(0, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(0, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);

            var text2 = @"System.Console.WriteLine(2);";

            analyzer = new AnalyzerActions_03_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
        }

        private class AnalyzerActions_03_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
            public int FireCount7;
            public int FireCount8;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolStartAction(Handle1, SymbolKind.Method);
                context.RegisterSymbolStartAction(Handle2, SymbolKind.NamedType);
            }

            private void Handle1(SymbolStartAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.Symbol.ToTestDisplayString());

                switch (context.Symbol.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount1);
                        context.RegisterSymbolEndAction(Handle3);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount2);
                        context.RegisterSymbolEndAction(Handle4);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void Handle2(SymbolStartAnalysisContext context)
            {
                Assert.Equal(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName, context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount3);
                context.RegisterSymbolEndAction(Handle5);

                foreach (var syntaxReference in context.Symbol.DeclaringSyntaxReferences)
                {
                    switch (syntaxReference.GetSyntax().ToString())
                    {
                        case "System.Console.WriteLine(1);":
                            Interlocked.Increment(ref FireCount4);
                            break;
                        case "System.Console.WriteLine(2);":
                            Interlocked.Increment(ref FireCount5);
                            break;
                        default:
                            Assert.True(false);
                            break;
                    }
                }
            }

            private void Handle3(SymbolAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount6);
                Assert.Equal("System.Console.WriteLine(1);", context.Symbol.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            }

            private void Handle4(SymbolAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount7);
                Assert.Equal("System.Console.WriteLine(2);", context.Symbol.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            }

            private void Handle5(SymbolAnalysisContext context)
            {
                Assert.Equal(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName, context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount8);
            }
        }

        [Fact]
        public void AnalyzerActions_04()
        {
            var text1 = @"System.Console.WriteLine(1);";

            var analyzer = new AnalyzerActions_04_Analyzer();
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(0, analyzer.FireCount4);

            var text2 = @"System.Console.WriteLine(2);";

            analyzer = new AnalyzerActions_04_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
        }

        private class AnalyzerActions_04_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationAction(Handle1, OperationKind.Invocation);
                context.RegisterOperationAction(Handle2, OperationKind.Block);
            }

            private void Handle1(OperationAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.ContainingSymbol.ToTestDisplayString());
                Assert.Same(context.ContainingSymbol.DeclaringSyntaxReferences.Single().SyntaxTree, context.Operation.Syntax.SyntaxTree);

                Assert.Equal(SyntaxKind.InvocationExpression, context.Operation.Syntax.Kind());

                switch (context.Operation.Syntax.ToString())
                {
                    case "System.Console.WriteLine(1)":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Console.WriteLine(2)":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void Handle2(OperationAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.ContainingSymbol.ToTestDisplayString());
                Assert.Same(context.ContainingSymbol.DeclaringSyntaxReferences.Single().GetSyntax(), context.Operation.Syntax);
                Assert.Equal(SyntaxKind.CompilationUnit, context.Operation.Syntax.Kind());

                switch (context.Operation.Syntax.ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount3);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount4);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_05()
        {
            var text1 = @"System.Console.WriteLine(1);";

            var analyzer = new AnalyzerActions_05_Analyzer();
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount2);

            var text2 = @"System.Console.WriteLine(2);";

            analyzer = new AnalyzerActions_05_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
        }

        private class AnalyzerActions_05_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationBlockAction(Handle);
            }

            private void Handle(OperationBlockAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.OwningSymbol.ToTestDisplayString());
                Assert.Equal(SyntaxKind.CompilationUnit, context.OperationBlocks.Single().Syntax.Kind());

                switch (context.OperationBlocks.Single().Syntax.ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_06()
        {
            var text1 = @"System.Console.WriteLine(1);";

            var analyzer = new AnalyzerActions_06_Analyzer();
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount2);

            var text2 = @"System.Console.WriteLine(2);";

            analyzer = new AnalyzerActions_06_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
        }

        private class AnalyzerActions_06_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationBlockStartAction(Handle);
            }

            private void Handle(OperationBlockStartAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.OwningSymbol.ToTestDisplayString());
                Assert.Equal(SyntaxKind.CompilationUnit, context.OperationBlocks.Single().Syntax.Kind());

                switch (context.OperationBlocks.Single().Syntax.ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_07()
        {
            var text1 = @"System.Console.WriteLine(1);";

            var analyzer = new AnalyzerActions_07_Analyzer();
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount2);

            var text2 = @"System.Console.WriteLine(2);";

            analyzer = new AnalyzerActions_07_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
        }

        private class AnalyzerActions_07_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCodeBlockAction(Handle);
            }

            private void Handle(CodeBlockAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.OwningSymbol.ToTestDisplayString());
                Assert.Equal(SyntaxKind.CompilationUnit, context.CodeBlock.Kind());

                switch (context.CodeBlock.ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                var model = context.SemanticModel;
                var unit = (CompilationUnitSyntax)context.CodeBlock;
                var syntaxTreeModel = ((SyntaxTreeSemanticModel)model);

                MemberSemanticModel mm = syntaxTreeModel.TestOnlyMemberModels[unit];

                Assert.False(mm.TestOnlyTryGetBoundNodesFromMap(unit).IsEmpty);

                Assert.Same(mm, syntaxTreeModel.GetMemberModel(unit));
            }
        }

        [Fact]
        public void AnalyzerActions_08()
        {
            var text1 = @"System.Console.WriteLine(1);";

            var analyzer = new AnalyzerActions_08_Analyzer();
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount2);

            var text2 = @"System.Console.WriteLine(2);";

            analyzer = new AnalyzerActions_08_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
        }

        private class AnalyzerActions_08_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCodeBlockStartAction<SyntaxKind>(Handle);
            }

            private void Handle(CodeBlockStartAnalysisContext<SyntaxKind> context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.OwningSymbol.ToTestDisplayString());
                Assert.Equal(SyntaxKind.CompilationUnit, context.CodeBlock.Kind());

                switch (context.CodeBlock.ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                var model = context.SemanticModel;
                var unit = (CompilationUnitSyntax)context.CodeBlock;
                var syntaxTreeModel = ((SyntaxTreeSemanticModel)model);

                MemberSemanticModel mm = syntaxTreeModel.TestOnlyMemberModels[unit];

                Assert.False(mm.TestOnlyTryGetBoundNodesFromMap(unit).IsEmpty);

                Assert.Same(mm, syntaxTreeModel.GetMemberModel(unit));
            }
        }

        [Fact]
        public void AnalyzerActions_09()
        {
            var text1 = @"
System.Console.WriteLine(""Hi!"");
";
            var text2 = @"
class Test
{
    void M()
    {
        M();
    }
}
";

            var analyzer = new AnalyzerActions_09_Analyzer();
            var comp = CreateCompilation(text1 + text2, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);

            analyzer = new AnalyzerActions_09_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(2, analyzer.FireCount3);
        }

        private class AnalyzerActions_09_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(Handle1, SyntaxKind.InvocationExpression);
                context.RegisterSyntaxNodeAction(Handle2, SyntaxKind.CompilationUnit);
            }

            private void Handle1(SyntaxNodeAnalysisContext context)
            {
                var model = context.SemanticModel;
                var node = (CSharpSyntaxNode)context.Node;
                var syntaxTreeModel = ((SyntaxTreeSemanticModel)model);

                switch (node.ToString())
                {
                    case @"System.Console.WriteLine(""Hi!"")":
                        Interlocked.Increment(ref FireCount1);
                        Assert.Equal("<top-level-statements-entry-point>", context.ContainingSymbol.ToTestDisplayString());
                        break;

                    case "M()":
                        Interlocked.Increment(ref FireCount2);
                        Assert.Equal("void Test.M()", context.ContainingSymbol.ToTestDisplayString());
                        break;

                    default:
                        Assert.True(false);
                        break;
                }

                var decl = (CSharpSyntaxNode)context.ContainingSymbol.DeclaringSyntaxReferences.Single().GetSyntax();

                Assert.True(syntaxTreeModel.TestOnlyMemberModels.ContainsKey(decl));

                MemberSemanticModel mm = syntaxTreeModel.TestOnlyMemberModels[decl];

                Assert.False(mm.TestOnlyTryGetBoundNodesFromMap(node).IsEmpty);

                Assert.Same(mm, syntaxTreeModel.GetMemberModel(node));
            }

            private void Handle2(SyntaxNodeAnalysisContext context)
            {
                var model = context.SemanticModel;
                var node = (CSharpSyntaxNode)context.Node;
                var syntaxTreeModel = ((SyntaxTreeSemanticModel)model);

                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "<global namespace>":
                        Interlocked.Increment(ref FireCount3);
                        break;

                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_10()
        {
            var text1 = @"
[assembly: MyAttribute(1)]
";
            var text2 = @"
System.Console.WriteLine(""Hi!"");
";
            var text3 = @"
[MyAttribute(2)]
class Test
{
    [MyAttribute(3)]
    void M()
    {
    }
}

class MyAttribute : System.Attribute
{
    public MyAttribute(int x) {}
}
";

            var analyzer = new AnalyzerActions_10_Analyzer();
            var comp = CreateCompilation(text1 + text2 + text3, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);

            analyzer = new AnalyzerActions_10_Analyzer();
            comp = CreateCompilation(new[] { text1, text2, text3 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(3, analyzer.FireCount4);
        }

        private class AnalyzerActions_10_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(Handle1, SyntaxKind.Attribute);
                context.RegisterSyntaxNodeAction(Handle2, SyntaxKind.CompilationUnit);
            }

            private void Handle1(SyntaxNodeAnalysisContext context)
            {
                var node = (CSharpSyntaxNode)context.Node;

                switch (node.ToString())
                {
                    case @"MyAttribute(1)":
                        Interlocked.Increment(ref FireCount1);
                        Assert.Equal("<global namespace>", context.ContainingSymbol.ToTestDisplayString());
                        break;

                    case @"MyAttribute(2)":
                        Interlocked.Increment(ref FireCount2);
                        Assert.Equal("Test", context.ContainingSymbol.ToTestDisplayString());
                        break;

                    case @"MyAttribute(3)":
                        Interlocked.Increment(ref FireCount3);
                        Assert.Equal("void Test.M()", context.ContainingSymbol.ToTestDisplayString());
                        break;

                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void Handle2(SyntaxNodeAnalysisContext context)
            {
                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case @"<global namespace>":
                        Interlocked.Increment(ref FireCount4);
                        break;

                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_11()
        {
            var text1 = @"
System.Console.WriteLine(""Hi!"");
";
            var text2 = @"
namespace N1
{}

class C1
{}
";

            var analyzer = new AnalyzerActions_11_Analyzer();
            var comp = CreateCompilation(text1 + text2, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);

            analyzer = new AnalyzerActions_11_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
        }

        private class AnalyzerActions_11_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(Handle1, SymbolKind.Method);
                context.RegisterSymbolAction(Handle2, SymbolKind.Namespace);
                context.RegisterSymbolAction(Handle3, SymbolKind.NamedType);
            }

            private void Handle1(SymbolAnalysisContext context)
            {
                Interlocked.Increment(ref FireCount1);
                Assert.Equal("<top-level-statements-entry-point>", context.Symbol.ToTestDisplayString());
            }

            private void Handle2(SymbolAnalysisContext context)
            {
                Interlocked.Increment(ref FireCount2);
                Assert.Equal("N1", context.Symbol.ToTestDisplayString());
            }

            private void Handle3(SymbolAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "C1":
                        Interlocked.Increment(ref FireCount3);
                        break;
                    case WellKnownMemberNames.TopLevelStatementsEntryPointTypeName:
                        Interlocked.Increment(ref FireCount4);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_12()
        {
            var text1 = @"System.Console.WriteLine(1);";

            var analyzer = new AnalyzerActions_12_Analyzer();
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);

            var text2 = @"System.Console.WriteLine(2);";

            analyzer = new AnalyzerActions_12_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(2, analyzer.FireCount3);
        }

        private class AnalyzerActions_12_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationBlockStartAction(Handle1);
            }

            private void Handle1(OperationBlockStartAnalysisContext context)
            {
                Interlocked.Increment(ref FireCount3);
                context.RegisterOperationBlockEndAction(Handle2);
            }

            private void Handle2(OperationBlockAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.OwningSymbol.ToTestDisplayString());
                Assert.Equal(SyntaxKind.CompilationUnit, context.OperationBlocks.Single().Syntax.Kind());

                switch (context.OperationBlocks.Single().Syntax.ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_13()
        {
            var text1 = @"System.Console.WriteLine(1);";

            var analyzer = new AnalyzerActions_13_Analyzer();
            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);

            var text2 = @"System.Console.WriteLine(2);";

            analyzer = new AnalyzerActions_13_Analyzer();
            comp = CreateCompilation(new[] { text1, text2 }, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(2, analyzer.FireCount3);
        }

        private class AnalyzerActions_13_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationBlockStartAction(Handle1);
            }

            private void Handle1(OperationBlockStartAnalysisContext context)
            {
                Interlocked.Increment(ref FireCount3);
                context.RegisterOperationAction(Handle2, OperationKind.Block);
            }

            private void Handle2(OperationAnalysisContext context)
            {
                Assert.Equal("<top-level-statements-entry-point>", context.ContainingSymbol.ToTestDisplayString());
                Assert.Same(context.ContainingSymbol.DeclaringSyntaxReferences.Single().GetSyntax(), context.Operation.Syntax);
                Assert.Equal(SyntaxKind.CompilationUnit, context.Operation.Syntax.Kind());

                switch (context.Operation.Syntax.ToString())
                {
                    case "System.Console.WriteLine(1);":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Console.WriteLine(2);":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void MissingTypes_01()
        {
            var text = @"return;";

            var comp = CreateEmptyCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (1,1): error CS0518: Predefined type 'System.Object' is not defined or imported
                // return;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "return").WithArguments("System.Object").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Void' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Void").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.String' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.String").WithLocation(1, 1),
                // (1,1): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                // return;
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "return").WithArguments("object", "0").WithLocation(1, 1)
                );
        }

        [Fact]
        public void MissingTypes_02()
        {
            var text = @"await Test();";

            var comp = CreateCompilation(text, targetFramework: TargetFramework.Minimal, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyEmitDiagnostics(
                // error CS0518: Predefined type 'System.Threading.Tasks.Task' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Threading.Tasks.Task").WithLocation(1, 1),
                // (1,1): warning CS0028: '<top-level-statements-entry-point>' has the wrong signature to be an entry point
                // await Test();
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "await Test();").WithArguments("<top-level-statements-entry-point>").WithLocation(1, 1),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (1,7): error CS0103: The name 'Test' does not exist in the current context
                // await Test();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Test").WithArguments("Test").WithLocation(1, 7)
                );
        }

        [Fact]
        public void MissingTypes_03()
        {
            var text = @"
System.Console.WriteLine(""Hi!"");
return 10;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.MakeTypeMissing(SpecialType.System_Int32);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Int32[missing]", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            comp.VerifyEmitDiagnostics(
                // error CS0518: Predefined type 'System.Int32' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Int32").WithLocation(1, 1),
                // (3,8): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // return 10;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "10").WithArguments("System.Int32").WithLocation(3, 8)
                );
        }

        [Fact]
        public void MissingTypes_04()
        {
            var text = @"
await System.Threading.Tasks.Task.Factory.StartNew(() => 5L);
return 11;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.MakeTypeMissing(SpecialType.System_Int32);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32[missing]>", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            comp.VerifyEmitDiagnostics(
                // error CS0518: Predefined type 'System.Int32' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Int32").WithLocation(1, 1),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (2,1): warning CS0028: '<top-level-statements-entry-point>' has the wrong signature to be an entry point
                // await System.Threading.Tasks.Task.Factory.StartNew(() => 5L);
                Diagnostic(ErrorCode.WRN_InvalidMainSig, @"await System.Threading.Tasks.Task.Factory.StartNew(() => 5L);
return 11;
").WithArguments("<top-level-statements-entry-point>").WithLocation(2, 1),
                // (2,1): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // await System.Threading.Tasks.Task.Factory.StartNew(() => 5L);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await System.Threading.Tasks.Task.Factory.StartNew(() => 5L);").WithArguments("System.Int32").WithLocation(2, 1),
                // (2,1): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // await System.Threading.Tasks.Task.Factory.StartNew(() => 5L);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await System.Threading.Tasks.Task.Factory.StartNew(() => 5L);").WithArguments("System.Int32").WithLocation(2, 1),
                // (2,1): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // await System.Threading.Tasks.Task.Factory.StartNew(() => 5L);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await System.Threading.Tasks.Task.Factory.StartNew(() => 5L);").WithArguments("System.Int32").WithLocation(2, 1),
                // (3,8): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // return 11;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "11").WithArguments("System.Int32").WithLocation(3, 8)
                );
        }

        [Fact]
        public void MissingTypes_05()
        {
            var text = @"
await System.Threading.Tasks.Task.Factory.StartNew(() => ""5"");
return 11;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_Task_T);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32>[missing]", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            comp.VerifyEmitDiagnostics(
                // error CS0518: Predefined type 'System.Threading.Tasks.Task`1' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Threading.Tasks.Task`1").WithLocation(1, 1),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (2,1): warning CS0028: '<top-level-statements-entry-point>' has the wrong signature to be an entry point
                // await System.Threading.Tasks.Task.Factory.StartNew(() => "5");
                Diagnostic(ErrorCode.WRN_InvalidMainSig, @"await System.Threading.Tasks.Task.Factory.StartNew(() => ""5"");
return 11;
").WithArguments("<top-level-statements-entry-point>").WithLocation(2, 1)
                );
        }

        [Fact]
        public void MissingTypes_06()
        {
            var text = @"
await System.Threading.Tasks.Task.Factory.StartNew(() => ""5"");
return 11;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.MakeTypeMissing(SpecialType.System_Int32);
            comp.MakeTypeMissing(WellKnownType.System_Threading_Tasks_Task_T);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32[missing]>[missing]", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            comp.VerifyEmitDiagnostics(
                // error CS0518: Predefined type 'System.Threading.Tasks.Task`1' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Threading.Tasks.Task`1").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Int32' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Int32").WithLocation(1, 1),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (2,1): warning CS0028: '<top-level-statements-entry-point>' has the wrong signature to be an entry point
                // await System.Threading.Tasks.Task.Factory.StartNew(() => "5");
                Diagnostic(ErrorCode.WRN_InvalidMainSig, @"await System.Threading.Tasks.Task.Factory.StartNew(() => ""5"");
return 11;
").WithArguments("<top-level-statements-entry-point>").WithLocation(2, 1),
                // (3,8): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // return 11;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "11").WithArguments("System.Int32").WithLocation(3, 8)
                );
        }

        [Fact]
        public void MissingTypes_07()
        {
            var text = @"
System.Console.WriteLine();
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.MakeTypeMissing(SpecialType.System_String);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.String[missing][] args", entryPoint.Parameters.Single().ToTestDisplayString());
            comp.VerifyEmitDiagnostics(
                // error CS0518: Predefined type 'System.String' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.String").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Return_01()
        {
            var text = @"
System.Console.WriteLine(args[0]);
return;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Void", entryPoint.ReturnType.ToTestDisplayString());
            Assert.True(entryPoint.ReturnsVoid);
            AssertEntryPointParameter(entryPoint);
            CompileAndVerify(comp, expectedOutput: "Return_01", args: new[] { "Return_01" });

            if (ExecutionConditionUtil.IsWindows)
            {
                _ = ConditionalSkipReason.NativePdbRequiresDesktop;

                comp.VerifyPdb(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName + "." + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName,
@$"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}"" methodName=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)}"" parameterNames=""args"" />
  <methods>
    <method containingType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}"" name=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)}"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""2"" startColumn=""1"" endLine=""2"" endColumn=""35"" document=""1"" />
        <entry offset=""0x9"" startLine=""3"" startColumn=""1"" endLine=""3"" endColumn=""8"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", options: PdbValidationOptions.SkipConversionValidation);
            }
        }

        private static string EscapeForXML(string toEscape)
        {
            return toEscape.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        [Fact]
        public void Return_02()
        {
            var text = @"
System.Console.WriteLine(args[0]);
return 10;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Int32", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            AssertEntryPointParameter(entryPoint);
            CompileAndVerify(comp, expectedOutput: "Return_02", args: new[] { "Return_02" }, expectedReturnCode: 10);

            if (ExecutionConditionUtil.IsWindows)
            {
                _ = ConditionalSkipReason.NativePdbRequiresDesktop;

                comp.VerifyPdb(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName + "." + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName,
@$"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}"" methodName=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)}"" parameterNames=""args"" />
  <methods>
    <method containingType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}"" name=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)}"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""2"" startColumn=""1"" endLine=""2"" endColumn=""35"" document=""1"" />
        <entry offset=""0x9"" startLine=""3"" startColumn=""1"" endLine=""3"" endColumn=""11"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", options: PdbValidationOptions.SkipConversionValidation);
            }
        }

        [Fact]
        public void Return_03()
        {
            var text = @"
using System;
using System.Threading.Tasks;

Console.Write(""hello "");
await Task.Factory.StartNew(() => 5);
Console.Write(args[0]);
return;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Threading.Tasks.Task", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            AssertEntryPointParameter(entryPoint);
            CompileAndVerify(comp, expectedOutput: "hello Return_03", args: new[] { "Return_03" });

            if (ExecutionConditionUtil.IsWindows)
            {
                _ = ConditionalSkipReason.NativePdbRequiresDesktop;

                comp.VerifyPdb(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName + "+<" + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName + ">d__0.MoveNext",
@$"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}"" methodName=""&lt;Main&gt;"" parameterNames=""args"" />
  <methods>
    <method containingType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}+&lt;{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)}&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""2"" />
          <slot kind=""33"" offset=""76"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe"" startLine=""5"" startColumn=""1"" endLine=""5"" endColumn=""25"" document=""1"" />
        <entry offset=""0x19"" startLine=""6"" startColumn=""1"" endLine=""6"" endColumn=""38"" document=""1"" />
        <entry offset=""0x48"" hidden=""true"" document=""1"" />
        <entry offset=""0x99"" startLine=""7"" startColumn=""1"" endLine=""7"" endColumn=""24"" document=""1"" />
        <entry offset=""0xa7"" startLine=""8"" startColumn=""1"" endLine=""8"" endColumn=""8"" document=""1"" />
        <entry offset=""0xa9"" hidden=""true"" document=""1"" />
        <entry offset=""0xc1"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xd6"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <catchHandler offset=""0xa9"" />
        <kickoffMethod declaringType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}"" methodName=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)}"" parameterNames=""args"" />
        <await yield=""0x5a"" resume=""0x75"" declaringType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}+&lt;{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)}&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>", options: PdbValidationOptions.SkipConversionValidation);
            }
        }

        [Fact]
        public void Return_04()
        {
            var text = @"
using System;
using System.Threading.Tasks;

Console.Write(""hello "");
await Task.Factory.StartNew(() => 5);
Console.Write(args[0]);
return 11;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32>", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            AssertEntryPointParameter(entryPoint);
            CompileAndVerify(comp, expectedOutput: "hello Return_04", args: new[] { "Return_04" }, expectedReturnCode: 11);

            if (ExecutionConditionUtil.IsWindows)
            {
                _ = ConditionalSkipReason.NativePdbRequiresDesktop;

                comp.VerifyPdb(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName + "+<" + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName + ">d__0.MoveNext",
@$"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}"" methodName=""&lt;Main&gt;"" parameterNames=""args"" />
  <methods>
    <method containingType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}+&lt;{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)}&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""2"" />
          <slot kind=""20"" offset=""2"" />
          <slot kind=""33"" offset=""76"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe"" startLine=""5"" startColumn=""1"" endLine=""5"" endColumn=""25"" document=""1"" />
        <entry offset=""0x19"" startLine=""6"" startColumn=""1"" endLine=""6"" endColumn=""38"" document=""1"" />
        <entry offset=""0x48"" hidden=""true"" document=""1"" />
        <entry offset=""0x99"" startLine=""7"" startColumn=""1"" endLine=""7"" endColumn=""24"" document=""1"" />
        <entry offset=""0xa7"" startLine=""8"" startColumn=""1"" endLine=""8"" endColumn=""11"" document=""1"" />
        <entry offset=""0xac"" hidden=""true"" document=""1"" />
        <entry offset=""0xc6"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xdc"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <catchHandler offset=""0xac"" />
        <kickoffMethod declaringType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}"" methodName=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)}"" parameterNames=""args"" />
        <await yield=""0x5a"" resume=""0x75"" declaringType=""{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)}+&lt;{EscapeForXML(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)}&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>", options: PdbValidationOptions.SkipConversionValidation);
            }
        }

        [Fact]
        public void Return_05()
        {
            var text = @"
System.Console.WriteLine(""Hi!"");
return ""error"";
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Int32", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            comp.VerifyDiagnostics(
                // (3,8): error CS0029: Cannot implicitly convert type 'string' to 'int'
                // return "error";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""error""").WithArguments("string", "int").WithLocation(3, 8)
                );
        }

        [Fact]
        public void Return_06()
        {
            var text = @"
System.Func<int, int> d = n =>
    {
        System.Console.WriteLine(""Hi!"");
        return n;
    };
d(0);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Void", entryPoint.ReturnType.ToTestDisplayString());
            Assert.True(entryPoint.ReturnsVoid);
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void Return_07()
        {
            var text = @"
System.Func<int, int> d = delegate(int n)
    {
        System.Console.WriteLine(""Hi!"");
        return n;
    };
d(0);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Void", entryPoint.ReturnType.ToTestDisplayString());
            Assert.True(entryPoint.ReturnsVoid);
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void Return_08()
        {
            var text = @"
System.Func<int, int> d = (n) =>
    {
        System.Console.WriteLine(""Hi!"");
        return n;
    };
d(0);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Void", entryPoint.ReturnType.ToTestDisplayString());
            Assert.True(entryPoint.ReturnsVoid);
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void Return_09()
        {
            var text = @"
int local(int n)
{
    System.Console.WriteLine(""Hi!"");
    return n;
}

local(0);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Void", entryPoint.ReturnType.ToTestDisplayString());
            Assert.True(entryPoint.ReturnsVoid);
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void Return_10()
        {
            var text = @"
bool b = true;
if (b)
    return 0;
else
    return;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Int32", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            comp.VerifyDiagnostics(
                // (6,5): error CS0126: An object of a type convertible to 'int' is required
                //     return;
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("int").WithLocation(6, 5)
                );
        }

        [Fact]
        public void Return_11()
        {
            var text = @"
bool b = true;
if (b)
    return;
else
    return 0;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Int32", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            comp.VerifyDiagnostics(
                // (4,5): error CS0126: An object of a type convertible to 'int' is required
                //     return;
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("int").WithLocation(4, 5)
                );
        }

        [Fact]
        public void Return_12()
        {
            var text = @"
System.Console.WriteLine(1);
return;
System.Console.WriteLine(2);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Void", entryPoint.ReturnType.ToTestDisplayString());
            CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics(
                // (4,1): warning CS0162: Unreachable code detected
                // System.Console.WriteLine(2);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(4, 1)
                );
        }

        [Fact]
        public void Return_13()
        {
            var text = @"
System.Console.WriteLine(1);
return 13;
System.Console.WriteLine(2);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Int32", entryPoint.ReturnType.ToTestDisplayString());
            CompileAndVerify(comp, expectedOutput: "1", expectedReturnCode: 13).VerifyDiagnostics(
                // (4,1): warning CS0162: Unreachable code detected
                // System.Console.WriteLine(2);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(4, 1)
                );
        }

        [Fact]
        public void Return_14()
        {
            var text = @"
System.Console.WriteLine(""Hi!"");
return default;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Int32", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            CompileAndVerify(comp, expectedOutput: "Hi!", expectedReturnCode: 0);
        }

        [Fact]
        public void Return_15()
        {
            var text = @"
using System;
using System.Threading.Tasks;

Console.Write(""hello "");
await Task.Factory.StartNew(() => 5);
Console.Write(""async main"");
return default;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32>", entryPoint.ReturnType.ToTestDisplayString());
            Assert.False(entryPoint.ReturnsVoid);
            CompileAndVerify(comp, expectedOutput: "hello async main", expectedReturnCode: 0);
        }

        [Fact]
        public void Dll_01()
        {
            var text = @"System.Console.WriteLine(""Hi!"");";

            var comp = CreateCompilation(text, options: TestOptions.DebugDll, parseOptions: DefaultParseOptions);
            comp.VerifyEmitDiagnostics(
                // (1,1): error CS8805: Program using top-level statements must be an executable.
                // System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable, @"System.Console.WriteLine(""Hi!"");").WithLocation(1, 1)
                );
        }

        [Fact]
        public void NetModule_01()
        {
            var text = @"System.Console.WriteLine(""Hi!"");";

            var comp = CreateCompilation(text, options: TestOptions.ReleaseModule, parseOptions: DefaultParseOptions);
            comp.VerifyEmitDiagnostics(
                // (1,1): error CS8805: Program using top-level statements must be an executable.
                // System.Console.WriteLine("Hi!");
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable, @"System.Console.WriteLine(""Hi!"");").WithLocation(1, 1)
                );
        }

        [Fact]
        public void ExpressionStatement_01()
        {
            // Await expression is covered in other tests
            var text = @"
new Test(0); // ObjectCreationExpression:
Test x;
x = new Test(1); // SimpleAssignmentExpression:
x += 1; // AddAssignmentExpression:
x -= 2; // SubtractAssignmentExpression:
x *= 3; // MultiplyAssignmentExpression:
x /= 4; // DivideAssignmentExpression:
x %= 5; // ModuloAssignmentExpression:
x &= 6; // AndAssignmentExpression:
x |= 7; // OrAssignmentExpression:
x ^= 8; // ExclusiveOrAssignmentExpression:
x <<= 9; // LeftShiftAssignmentExpression:
x >>= 10; // RightShiftAssignmentExpression:
x++; // PostIncrementExpression:
x--; // PostDecrementExpression:
++x; // PreIncrementExpression:
--x; // PreDecrementExpression:
System.Console.WriteLine(x.Count); // InvocationExpression:
x?.WhenNotNull(); // ConditionalAccessExpression:
x = null;
x ??= new Test(-1); // CoalesceAssignmentExpression:
System.Console.WriteLine(x.Count); // InvocationExpression:

class Test
{
    public readonly int Count;

    public Test(int count)
    {
        Count = count;
        if (count == 0)
        {
            System.Console.WriteLine(""Test..ctor"");
        }
    }

    public static Test operator +(Test x, int y)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator -(Test x, int y)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator *(Test x, int y)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator /(Test x, int y)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator %(Test x, int y)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator &(Test x, int y)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator |(Test x, int y)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator ^(Test x, int y)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator <<(Test x, int y)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator >>(Test x, int y)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator ++(Test x)
    {
        return new Test(x.Count + 1);
    }

    public static Test operator --(Test x)
    {
        return new Test(x.Count + 1);
    }

    public void WhenNotNull()
    {
        System.Console.WriteLine(""WhenNotNull"");
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput:
@"Test..ctor
15
WhenNotNull
-1
");
        }

        [Fact]
        public void Block_01()
        {
            var text = @"
{
    System.Console.WriteLine(""Hi!"");
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void EmptyStatement_01()
        {
            var text = @"
;
System.Console.WriteLine(""Hi!"");
;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void BreakStatement_01()
        {
            var text = @"break;";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (1,1): error CS0139: No enclosing loop out of which to break or continue
                // break;
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "break;").WithLocation(1, 1)
                );
        }

        [Fact]
        public void ContinueStatement_01()
        {
            var text = @"continue;";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (1,1): error CS0139: No enclosing loop out of which to break or continue
                // continue;
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "continue;").WithLocation(1, 1)
                );
        }

        [Fact]
        public void ThrowStatement_01()
        {
            var text = @"throw;";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (1,1): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                // throw;
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw").WithLocation(1, 1)
                );
        }

        [Fact]
        public void ThrowStatement_02()
        {
            var text = @"throw null;";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp).VerifyIL("<top-level-statements-entry-point>", sequencePoints: WellKnownMemberNames.TopLevelStatementsEntryPointTypeName + "." + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName, source: text, expectedIL:
@"
{
  // Code size        2 (0x2)
  .maxstack  1
  // sequence point: throw null;
  IL_0000:  ldnull
  IL_0001:  throw
}
");
        }

        [Fact]
        public void DoStatement_01()
        {
            var text = @"
int i = 1;
do
{
    i++;
} while (i < 4);

System.Console.WriteLine(i);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "4");
        }

        [Fact]
        public void WhileStatement_01()
        {
            var text = @"
int i = 1;
while (i < 4)
{
    i++;
} 

System.Console.WriteLine(i);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "4");
        }

        [Fact]
        public void ForStatement_01()
        {
            var text = @"
int i = 1;
for (;i < 4;)
{
    i++;
} 

System.Console.WriteLine(i);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "4");
        }

        [Fact]
        public void CheckedStatement_01()
        {
            var text = @"
int i = 1;
i++;
checked
{
    i++;
} 

System.Console.WriteLine(i);
";
            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "3").VerifyIL("<top-level-statements-entry-point>", sequencePoints: WellKnownMemberNames.TopLevelStatementsEntryPointTypeName + "." + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName, source: text, expectedIL:
@"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (int V_0) //i
  // sequence point: int i = 1;
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  // sequence point: i++;
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  add
  IL_0005:  stloc.0
  // sequence point: {
  IL_0006:  nop
  // sequence point: i++;
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  add.ovf
  IL_000a:  stloc.0
  // sequence point: }
  IL_000b:  nop
  // sequence point: System.Console.WriteLine(i);
  IL_000c:  ldloc.0
  IL_000d:  call       ""void System.Console.WriteLine(int)""
  IL_0012:  nop
  IL_0013:  ret
}
");
        }

        [Fact]
        public void UncheckedStatement_01()
        {
            var text = @"
int i = 1;
i++;
unchecked
{
    i++;
} 

System.Console.WriteLine(i);
";
            var comp = CreateCompilation(text, options: TestOptions.DebugExe.WithOverflowChecks(true), parseOptions: DefaultParseOptions);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "3").VerifyIL("<top-level-statements-entry-point>", sequencePoints: WellKnownMemberNames.TopLevelStatementsEntryPointTypeName + "." + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName, source: text, expectedIL:
@"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (int V_0) //i
  // sequence point: int i = 1;
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  // sequence point: i++;
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  add.ovf
  IL_0005:  stloc.0
  // sequence point: {
  IL_0006:  nop
  // sequence point: i++;
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  add
  IL_000a:  stloc.0
  // sequence point: }
  IL_000b:  nop
  // sequence point: System.Console.WriteLine(i);
  IL_000c:  ldloc.0
  IL_000d:  call       ""void System.Console.WriteLine(int)""
  IL_0012:  nop
  IL_0013:  ret
}
");
        }

        [Fact]
        public void UnsafeStatement_01()
        {
            var text = @"
unsafe
{
    int* p = (int*)0;
    p++;
    System.Console.WriteLine((int)p);
} 
";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "4", verify: Verification.Skipped);
        }

        [Fact]
        public void FixedStatement_01()
        {
            var text = @"
fixed(int *p = &new C().i) {}

class C
{
    public int i = 2;
}
";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // fixed(int *p = &new C().i) {}
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "fixed(int *p = &new C().i) {}").WithLocation(2, 1),
                // (2,7): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // fixed(int *p = &new C().i) {}
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int *").WithLocation(2, 7),
                // (2,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // fixed(int *p = &new C().i) {}
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&new C().i").WithLocation(2, 16)
                );
        }

        [Fact]
        public void LockStatement_01()
        {
            var text = @"
int i = 1;
lock (new object())
{
    i++;
} 

System.Console.WriteLine(i);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "2");
        }

        [Fact]
        public void IfStatement_01()
        {
            var text = @"
int i = 1;
if (i == 1)
{
    i++;
}
else
{
    i--;
}

if (i != 2)
{
    i--;
}
else
{
    i++;
}

System.Console.WriteLine(i);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "3");
        }

        [Fact]
        public void SwitchStatement_01()
        {
            var text = @"
int i = 1;
switch (i)
{
    case 1:
        i++;
        break;
    default:
        i--;
        break;
}

System.Console.WriteLine(i);
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "2");
        }

        [Fact]
        public void TryStatement_01()
        {
            var text = @"
try
{
    System.Console.Write(1);
    throw null;
}
catch
{
    System.Console.Write(2);
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "12");
        }

        [Fact]
        public void Args_01()
        {
            var text = @"
#nullable enable
System.Console.WriteLine(args.Length == 0 ? 0 : -args[0].Length);
System.Console.Write("""");

System.Console.Write("""");
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "0").VerifyDiagnostics();
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            AssertEntryPointParameter(entryPoint);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var invocations = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();

            var symbols = model.LookupSymbols(invocations[0].Position, name: "args");
            Assert.Empty(symbols);
            symbols = model.LookupSymbols(invocations[0].SpanStart, name: "args");
            Assert.Equal("System.String[] args", symbols.Single().ToTestDisplayString());

            symbols = model.LookupSymbols(invocations[1].Position, name: "args");
            Assert.Equal("System.String[] args", symbols.Single().ToTestDisplayString());
            symbols = model.LookupSymbols(invocations[1].SpanStart, name: "args");
            Assert.Equal("System.String[] args", symbols.Single().ToTestDisplayString());

            symbols = model.LookupSymbols(invocations[2].Position, name: "args");
            Assert.Equal("System.String[] args", symbols.Single().ToTestDisplayString());
            symbols = model.LookupSymbols(invocations[2].SpanStart, name: "args");
            Assert.Equal("System.String[] args", symbols.Single().ToTestDisplayString());
        }

        [Fact]
        public void Args_02()
        {
            var text1 = @"
using System.Linq;
_ = from args in new object[0] select args;
";

            var comp = CreateCompilation(text1, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (3,10): error CS1931: The range variable 'args' conflicts with a previous declaration of 'args'
                // _ = from args in new object[0] select args;
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "args").WithArguments("args").WithLocation(3, 10)
                );
        }

        [Fact]
        public void Args_03()
        {
            var text = @"
local();
void local()
{
    System.Console.WriteLine(args[0]);
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "Args_03", args: new[] { "Args_03" }).VerifyDiagnostics();
        }

        [Fact]
        public void Args_04()
        {
            var text = @"
System.Action lambda = () => System.Console.WriteLine(args[0]);
lambda();
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "Args_04", args: new[] { "Args_04" }).VerifyDiagnostics();
        }

        [Fact]
        public void Args_05()
        {
            var text = @"
ar
";

            var comp = CreateCompilation(text);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var id = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Single();

            for (int i = id.SpanStart; i <= id.Span.End; i++)
            {
                var symbols = model.LookupSymbols(i, name: "args");
                Assert.Equal("System.String[] args", symbols.Single().ToTestDisplayString());
            }
        }

        [Fact]
        public void Args_06()
        {
            var text = @"
ar
// See https://aka.ms/new-console-template for more information
Console.WriteLine(""Hello, World!"");
";

            var comp = CreateCompilation(text);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var id = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "ar").Single();

            for (int i = id.SpanStart; i <= id.Span.End; i++)
            {
                var symbols = model.LookupSymbols(i, name: "args");
                Assert.Equal("System.String[] args", symbols.Single().ToTestDisplayString());
            }
        }

        [Fact]
        public void Args_07()
        {
            var text =
@"// See https://aka.ms/new-console-template for more information
Console.WriteLine(""Hello, World!"");
ar
";

            var comp = CreateCompilation(text);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var id = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "ar").Single();

            for (int i = id.SpanStart; i <= id.Span.End; i++)
            {
                var symbols = model.LookupSymbols(i, name: "args");
                Assert.Equal("System.String[] args", symbols.Single().ToTestDisplayString());
            }
        }

        [Fact]
        public void Span_01()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

Span<int> span = default;
_ = new { Span = span };

", options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (5,11): error CS0828: Cannot assign 'Span<int>' to anonymous type property
                // _ = new { Span = span };
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "Span = span").WithArguments("System.Span<int>").WithLocation(5, 11)
                );
        }

        [Fact]
        public void Span_02()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

Span<int> outer;
for (Span<int> inner = stackalloc int[10];; inner = outer)
{
    outer = inner;
}
", options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);

            comp.VerifyDiagnostics(
                // (7,13): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //     outer = inner;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(7, 13)
                );
        }

        [Fact]
        [WorkItem(1179569, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1179569")]
        public void Issue1179569()
        {
            var text1 =
@"Task v0123456789012345678901234567()
{
    Console.Write(""start v0123456789012345678901234567"");
    await Task.Delay(2 * 1000);
    Console.Write(""end v0123456789012345678901234567"");
}

Task On01234567890123456()
{
    return v0123456789012345678901234567();
}

string

return Task.WhenAll(
    Task.WhenAll(this.c01234567890123456789012345678.Select(v01234567 => On01234567890123456(v01234567))),
    Task.WhenAll(this.c01234567890123456789.Select(v01234567 => v01234567.U0123456789012345678901234())));
";

            var oldTree = Parse(text: text1, options: TestOptions.RegularDefault);

            var text2 =
@"Task v0123456789012345678901234567()
{
    Console.Write(""start v0123456789012345678901234567"");
    await Task.Delay(2 * 1000);
    Console.Write(""end v0123456789012345678901234567"");
}

Task On01234567890123456()
{
    return v0123456789012345678901234567();
}

string[

return Task.WhenAll(
    Task.WhenAll(this.c01234567890123456789012345678.Select(v01234567 => On01234567890123456(v01234567))),
    Task.WhenAll(this.c01234567890123456789.Select(v01234567 => v01234567.U0123456789012345678901234())));
";

            var newText = SourceText.From(text2, Encoding.UTF8, SourceHashAlgorithms.Default);
            using var lexer = new Syntax.InternalSyntax.Lexer(newText, TestOptions.RegularDefault);
            using var parser = new Syntax.InternalSyntax.LanguageParser(lexer,
                                       (CSharpSyntaxNode)oldTree.GetRoot(), new[] { new TextChangeRange(new TextSpan(282, 0), 1) });

            var compilationUnit = (CompilationUnitSyntax)parser.ParseCompilationUnit().CreateRed();
            var tree = CSharpSyntaxTree.Create(compilationUnit, TestOptions.RegularDefault, path: "", Encoding.UTF8, SourceHashAlgorithms.Default);
            Assert.Equal(text2, tree.GetText().ToString());
            tree.VerifySource();

            var fullParseTree = Parse(text: text2, options: TestOptions.RegularDefault);
            var nodes1 = tree.GetRoot().DescendantNodesAndTokensAndSelf(descendIntoTrivia: true).ToArray();
            var nodes2 = fullParseTree.GetRoot().DescendantNodesAndTokensAndSelf(descendIntoTrivia: true).ToArray();
            Assert.Equal(nodes1.Length, nodes2.Length);

            for (int i = 0; i < nodes1.Length; i++)
            {
                var node1 = nodes1[i];
                var node2 = nodes2[i];
                Assert.Equal(node1.RawKind, node2.RawKind);
                Assert.Equal(node1.Span, node2.Span);
                Assert.Equal(node1.FullSpan, node2.FullSpan);
                Assert.Equal(node1.ToString(), node2.ToString());
                Assert.Equal(node1.ToFullString(), node2.ToFullString());
            }
        }

        [Fact]
        public void TopLevelLocalReferencedInClass_IOperation()
        {
            var comp = CreateCompilation(@"
int i = 1;
class C
{
    void M()
    /*<bind>*/{
        _ = i;
    }/*</bind>*/
}
", options: TestOptions.ReleaseExe);

            var diagnostics = new[]
            {
                // (2,5): warning CS0219: The variable 'i' is assigned but its value is never used
                // int i = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(2, 5),
                // (7,13): error CS8801: Cannot use local variable or local function 'i' declared in a top-level statement in this context.
                //         _ = i;
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "i").WithArguments("i").WithLocation(7, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_ = i;')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: var, IsInvalid) (Syntax: '_ = i')
        Left: 
          IDiscardOperation (Symbol: var _) (OperationKind.Discard, Type: var) (Syntax: '_')
        Right: 
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'i')
", diagnostics);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_ = i;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: var, IsInvalid) (Syntax: '_ = i')
              Left: 
                IDiscardOperation (Symbol: var _) (OperationKind.Discard, Type: var) (Syntax: '_')
              Right: 
                ILocalReferenceOperation: i (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'i')
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [Fact]
        public void TopLevelLocalFunctionReferencedInClass_IOperation()
        {
            var comp = CreateCompilation(@"
_ = """";
static void M1() {}
void M2() {}
class C
{
    void M()
    /*<bind>*/{
        M1();
        M2();
    }/*</bind>*/
}
", options: TestOptions.ReleaseExe);

            var diagnostics = new[]
            {
                // (3,13): warning CS8321: The local function 'M1' is declared but never used
                // static void M1() {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M1").WithArguments("M1").WithLocation(3, 13),
                // (4,6): warning CS8321: The local function 'M2' is declared but never used
                // void M2() {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M2").WithArguments("M2").WithLocation(4, 6),
                // (9,9): error CS8801: Cannot use local variable or local function 'M1' declared in a top-level statement in this context.
                //         M1();
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "M1").WithArguments("M1").WithLocation(9, 9),
                // (10,9): error CS8801: Cannot use local variable or local function 'M2' declared in a top-level statement in this context.
                //         M2();
                Diagnostic(ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, "M2").WithArguments("M2").WithLocation(10, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'M1();')
    Expression: 
      IInvocationOperation (void M1()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M1()')
        Instance Receiver: 
          null
        Arguments(0)
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'M2();')
    Expression: 
      IInvocationOperation (void M2()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M2()')
        Instance Receiver: 
          null
        Arguments(0)
", diagnostics);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'M1();')
          Expression: 
            IInvocationOperation (void M1()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M1()')
              Instance Receiver: 
                null
              Arguments(0)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'M2();')
          Expression: 
            IInvocationOperation (void M2()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M2()')
              Instance Receiver: 
                null
              Arguments(0)
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [Fact]
        public void EmptyStatements_01()
        {
            var text = @";";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (1,1): error CS8937: At least one top-level statement must be non-empty.
                // ;
                Diagnostic(ErrorCode.ERR_SimpleProgramIsEmpty, ";").WithLocation(1, 1)
                );
        }

        [Fact]
        public void EmptyStatements_02()
        {
            var text = @";;


;;
;";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (1,1): error CS8937: At least one top-level statement must be non-empty.
                // ;;
                Diagnostic(ErrorCode.ERR_SimpleProgramIsEmpty, ";").WithLocation(1, 1)
                );
        }

        [Fact]
        public void EmptyStatements_03()
        {
            var text = @"
System.Console.WriteLine(""Hi!"");
;;
;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void EmptyStatements_04()
        {
            var text = @"
;;
;
System.Console.WriteLine(""Hi!"");";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void EmptyStatements_05()
        {
            var text = @"
;
System.Console.WriteLine(""Hi!"");
;
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            CompileAndVerify(comp, expectedOutput: "Hi!");
        }

        [Fact]
        public void EmptyStatements_06()
        {
            var text =
@"
using System;
;

class Program2
{
    static void Main(String[] args) {}
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(
                // (3,1): error CS8937: At least one top-level statement must be non-empty.
                // ;
                Diagnostic(ErrorCode.ERR_SimpleProgramIsEmpty, ";").WithLocation(3, 1),
                // (7,17): warning CS7022: The entry point of the program is global code; ignoring 'Program2.Main(string[])' entry point.
                //     static void Main(String[] args) {}
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program2.Main(string[])").WithLocation(7, 17)
                );
        }

        [Fact]
        public void SpeakableEntryPoint()
        {
            var text = @"
System.Console.WriteLine(""Hi!"");
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All));
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Void", entryPoint.ReturnType.ToTestDisplayString());
            Assert.True(entryPoint.ReturnsVoid);
            AssertEntryPointParameter(entryPoint);
            CompileAndVerify(comp, expectedOutput: "Hi!", sourceSymbolValidator: validate, symbolValidator: validate);
            Assert.Same(entryPoint, comp.GetEntryPoint(default));
            Assert.False(entryPoint.CanBeReferencedByName);
            Assert.True(entryPoint.ContainingType.CanBeReferencedByName);
            Assert.Equal("<Main>$", entryPoint.Name);
            Assert.Equal("Program", entryPoint.ContainingType.Name);
            Assert.Equal(Accessibility.Internal, entryPoint.ContainingType.DeclaredAccessibility);
            Assert.Equal(Accessibility.Private, entryPoint.DeclaredAccessibility);

            void validate(ModuleSymbol module)
            {
                bool fromSource = module is SourceModuleSymbol;
                var program = module.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName);
                Assert.False(program.IsImplicitlyDeclared);
                if (fromSource)
                {
                    Assert.Empty(program.GetAttributes().As<CSharpAttributeData>());
                }
                else
                {
                    Assert.Equal(new[] { "CompilerGeneratedAttribute" }, GetAttributeNames(program.GetAttributes().As<CSharpAttributeData>()));
                }
                Assert.Empty(program.GetMethod(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName).GetAttributes());

                if (fromSource)
                {
                    Assert.Equal(new[] { "<top-level-statements-entry-point>", "Program..ctor()" }, program.GetMembers().ToTestDisplayStrings());
                }
                else
                {
                    Assert.Equal(new[] { "void Program.<Main>$(System.String[] args)", "Program..ctor()" }, program.GetMembers().ToTestDisplayStrings());
                }
            }
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsPartial()
        {
            var text = @"
M();

public partial class Program
{
    private static void M()
    {
        System.Console.WriteLine(""Hi!"");
    }
}
";

            var comp = CreateCompilation(text, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All));
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Void", entryPoint.ReturnType.ToTestDisplayString());
            Assert.True(entryPoint.ReturnsVoid);
            AssertEntryPointParameter(entryPoint);
            CompileAndVerify(comp, expectedOutput: "Hi!", sourceSymbolValidator: validate, symbolValidator: validate);
            Assert.Same(entryPoint, comp.GetEntryPoint(default));
            Assert.False(entryPoint.CanBeReferencedByName);
            Assert.True(entryPoint.ContainingType.CanBeReferencedByName);
            Assert.Equal("<Main>$", entryPoint.Name);
            Assert.Equal("Program", entryPoint.ContainingType.Name);
            Assert.Equal(Accessibility.Public, entryPoint.ContainingType.DeclaredAccessibility);
            Assert.Equal(Accessibility.Private, entryPoint.DeclaredAccessibility);

            void validate(ModuleSymbol module)
            {
                var program = module.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName);
                Assert.Empty(program.GetAttributes().As<CSharpAttributeData>());
                Assert.False(program.IsImplicitlyDeclared);
                Assert.Empty(program.GetMethod(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName).GetAttributes().As<CSharpAttributeData>());
            }
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsNotPartial()
        {
            var text = @"
M();

public class Program
{
    private static void M()
    {
        System.Console.WriteLine(""Hi!"");
    }
}
";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,14): error CS0260: Missing partial modifier on declaration of type 'Program'; another partial declaration of this type exists
                // public class Program
                Diagnostic(ErrorCode.ERR_MissingPartial, "Program").WithArguments("Program").WithLocation(4, 14)
                );
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal("System.Void", entryPoint.ReturnType.ToTestDisplayString());
            Assert.True(entryPoint.ReturnsVoid);
            AssertEntryPointParameter(entryPoint);
            Assert.Same(entryPoint, comp.GetEntryPoint(default));
            Assert.False(entryPoint.CanBeReferencedByName);
            Assert.True(entryPoint.ContainingType.CanBeReferencedByName);
            Assert.Equal("<Main>$", entryPoint.Name);
            Assert.Equal("Program", entryPoint.ContainingType.Name);
            Assert.Equal(Accessibility.Public, entryPoint.ContainingType.DeclaredAccessibility);
            Assert.Equal(Accessibility.Private, entryPoint.DeclaredAccessibility);
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsInternal()
        {
            var text = @"
M();

internal partial class Program
{
    private static void M()
    {
        System.Console.WriteLine(""Hi!"");
    }
}
";

            var comp = CreateCompilation(text);
            CompileAndVerify(comp, expectedOutput: "Hi!");
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal(Accessibility.Internal, entryPoint.ContainingType.DeclaredAccessibility);
            Assert.Equal(Accessibility.Private, entryPoint.DeclaredAccessibility);
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramWithoutDeclaredAccessibility()
        {
            var text = @"
M();

partial class Program
{
    private static void M()
    {
        System.Console.WriteLine(""Hi!"");
    }
}
";

            var comp = CreateCompilation(text);
            CompileAndVerify(comp, expectedOutput: "Hi!");
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal(Accessibility.Internal, entryPoint.ContainingType.DeclaredAccessibility);
            Assert.Equal(Accessibility.Private, entryPoint.DeclaredAccessibility);
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsStruct()
        {
            var text = @"
M();

partial struct Program
{
    private static void M()
    {
        System.Console.WriteLine(""Hi!"");
    }
}
";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (2,1): error CS0261: Partial declarations of 'Program' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // M();
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "M").WithArguments("Program").WithLocation(2, 1),
                // (2,1): error CS0103: The name 'M' does not exist in the current context
                // M();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(2, 1)
                );
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal(Accessibility.Internal, entryPoint.ContainingType.DeclaredAccessibility);
            Assert.Equal(Accessibility.Private, entryPoint.DeclaredAccessibility);
            Assert.True(entryPoint.ContainingType.IsReferenceType);
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsRecord()
        {
            var text = @"
M();

partial record Program
{
    private static void M()
    {
    }
}
";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (2,1): error CS0261: Partial declarations of 'Program' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // M();
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "M").WithArguments("Program").WithLocation(2, 1),
                // (2,1): error CS0103: The name 'M' does not exist in the current context
                // M();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(2, 1)
                );
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsInterface()
        {
            var text = @"
System.Console.Write(42);

partial interface Program
{
}
";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (2,1): error CS0261: Partial declarations of 'Program' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // System.Console.Write(42);
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "System").WithArguments("Program").WithLocation(2, 1)
                );
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramCallsMain()
        {
            var text = @"
M(args);

partial class Program
{
    private static void M(string[] args)
    {
        Main();
    }
}
";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (8,9): error CS0103: The name 'Main' does not exist in the current context
                //         Main();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Main").WithArguments("Main").WithLocation(8, 9)
                );
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramHasBaseList()
        {
            var text = @"
new Program().M();

class Base
{
    public void M()
    {
        System.Console.Write(42);
    }
}

partial class Program : Base
{
}
";

            var comp = CreateCompilation(text);
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyDiagnostics();
            var entryPoint = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(comp);
            Assert.Equal(Accessibility.Private, entryPoint.DeclaredAccessibility);
            Assert.True(entryPoint.IsStatic);

            Assert.Equal("Base", entryPoint.ContainingType.BaseType().ToTestDisplayString());
            Assert.Equal(Accessibility.Internal, entryPoint.ContainingType.DeclaredAccessibility);
            Assert.False(entryPoint.ContainingType.IsStatic);
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramImplementsInterface()
        {
            var text = @"
((Interface)new Program()).M();

interface Interface
{
    void M();
}

partial class Program : Interface
{
    void Interface.M()
    {
        System.Console.Write(42);
    }
}
";

            var comp = CreateCompilation(text);
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsAbstractPartial()
        {
            var text = @"
System.Console.Write(42);

abstract partial class Program
{
}
";

            var comp = CreateCompilation(text);
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsSealedPartial()
        {
            var text = @"
System.Console.Write(42);

sealed partial class Program
{
}
";

            var comp = CreateCompilation(text);
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsStaticPartial()
        {
            var text = @"
System.Console.Write(42);

static partial class Program
{
}
";

            var comp = CreateCompilation(text);
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsObsolete()
        {
            var text = @"
System.Console.Write(42);

[System.Obsolete(""error"")]
public partial class Program
{
}

public class C
{
    public Program f;
}
";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (11,12): warning CS0618: 'Program' is obsolete: 'error'
                //     public Program f;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Program").WithArguments("Program", "error").WithLocation(11, 12)
                );
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramIsObsolete_UsedWithinProgram()
        {
            var text = @"
Program p = new Program();

[System.Obsolete(""error"")]
public partial class Program
{
}
";

            var comp = CreateCompilation(text);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramHasMain()
        {
            var text = @"
System.Console.Write(42);

partial class Program
{
    public static void Main(string[] args) { }
}
";
            var comp = CreateCompilation(text);

            comp.VerifyDiagnostics(
                // (6,24): warning CS7022: The entry point of the program is global code; ignoring 'Program.Main(string[])' entry point.
                //     public static void Main(string[] args) { }
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program.Main(string[])").WithLocation(6, 24)
                );
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramHasMain_DifferentSignature()
        {
            var text = @"
System.Console.Write(42);

partial class Program
{
    public static void Main() { }
}
";
            var comp = CreateCompilation(text);
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyDiagnostics(
                // (6,24): warning CS7022: The entry point of the program is global code; ignoring 'Program.Main()' entry point.
                //     public static void Main() { }
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Program.Main()").WithLocation(6, 24)
                );
        }

        [Fact]
        public void SpeakableEntryPoint_ProgramHasBackingField()
        {
            var text = @"
System.Console.Write(42);

partial class Program
{
    public int Property { get; set; }
}
";
            var comp = CreateCompilation(text, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate);
            comp.VerifyEmitDiagnostics();

            void validate(ModuleSymbol module)
            {
                bool fromSource = module is SourceModuleSymbol;
                var field = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").GetField("<Property>k__BackingField");
                Assert.False(field.ContainingType.IsImplicitlyDeclared);
                var fieldAttributes = GetAttributeNames(field.GetAttributes().As<CSharpAttributeData>());
                if (fromSource)
                {
                    Assert.Empty(fieldAttributes);

                    Assert.Equal(new[] { "<top-level-statements-entry-point>", "System.Int32 Program.<Property>k__BackingField",
                            "System.Int32 Program.Property { get; set; }", "System.Int32 Program.Property.get",
                            "void Program.Property.set", "Program..ctor()" },
                        field.ContainingType.GetMembers().ToTestDisplayStrings());
                }
                else
                {
                    Assert.Equal(new[] { "CompilerGeneratedAttribute", "DebuggerBrowsableAttribute" }, fieldAttributes);

                    Assert.Equal(new[] { "System.Int32 Program.<Property>k__BackingField", "void Program.<Main>$(System.String[] args)",
                            "System.Int32 Program.Property.get", "void Program.Property.set",
                            "Program..ctor()", "System.Int32 Program.Property { get; set; }" },
                        field.ContainingType.GetMembers().ToTestDisplayStrings());
                }
            }
        }

        [Fact]
        public void SpeakableEntryPoint_XmlDoc()
        {
            var src = @"
System.Console.Write(42);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments);
            comp.VerifyDiagnostics();

            var cMember = comp.GetMember<NamedTypeSymbol>("Program");
            Assert.Equal("", cMember.GetDocumentationCommentXml());
        }

        [Fact]
        public void SpeakableEntryPoint_XmlDoc_ProgramIsPartial()
        {
            var src = @"
System.Console.Write(42);

/// <summary>Summary</summary>
public partial class Program { }
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments);
            comp.VerifyDiagnostics();

            var cMember = comp.GetMember<NamedTypeSymbol>("Program");
            Assert.Equal(
@"<member name=""T:Program"">
    <summary>Summary</summary>
</member>
", cMember.GetDocumentationCommentXml());
        }

        [Fact]
        public void SpeakableEntryPoint_XmlDoc_ProgramIsPartial_NotCommented()
        {
            var src = @"
System.Console.Write(42);

public partial class Program { }
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments);
            comp.VerifyDiagnostics(
                // (4,22): warning CS1591: Missing XML comment for publicly visible type or member 'Program'
                // public partial class Program { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Program").WithArguments("Program").WithLocation(4, 22)
                );

            var cMember = comp.GetMember<NamedTypeSymbol>("Program");
            Assert.Equal("", cMember.GetDocumentationCommentXml());
        }

        [Fact]
        public void SpeakableEntryPoint_TypeOf()
        {
            var src = @"
C.M();

public class C
{
    public static void M()
    {
        System.Console.Write(typeof(Program));
    }
}
";

            var comp = CreateCompilation(src);
            var verifier = CompileAndVerify(comp, expectedOutput: "Program");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Ordering_InFileScopedNamespace_01()
        {
            var src = @"
namespace NS;
System.Console.Write(42);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,16): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // System.Console.Write(42);
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "Write").WithLocation(3, 16),
                // (3,22): error CS1026: ) expected
                // System.Console.Write(42);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "42").WithLocation(3, 22),
                // (3,22): error CS1022: Type or namespace definition, or end-of-file expected
                // System.Console.Write(42);
                Diagnostic(ErrorCode.ERR_EOFExpected, "42").WithLocation(3, 22)
                );
        }

        [Fact]
        public void Ordering_InFileScopedNamespace_02()
        {
            var src = @"
namespace NS;
var x = 1;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,1): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                // var x = 1;
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(3, 1),
                // (3,5): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // var x = 1;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "x").WithLocation(3, 5)
                );
        }

        [Fact]
        public void Ordering_AfterNamespace()
        {
            var src = @"
namespace NS
{
}

var x = 1;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (6,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // var x = 1;
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "var x = 1;").WithLocation(6, 1),
                // (6,5): warning CS0219: The variable 'x' is assigned but its value is never used
                // var x = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 5)
                );
        }

        [Fact]
        [WorkItem(58521, "https://github.com/dotnet/roslyn/issues/58521")]
        public void BindCompilationUnitInSemanticModelWhenLocalFunctionIsAtTheTop()
        {
            var source = @"
void F<T>(T t)
{
    var f = (ref T x) => 0;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            var tree = compilation.SyntaxTrees[0];
            var identifier = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().First(id => id.Identifier.Text == "var");
            var model = compilation.GetSemanticModel(tree);

            model.GetOperation(identifier);
            Assert.Equal(OperationKind.Literal, model.GetOperation(tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single()).Kind);
        }

        [Fact]
        [WorkItem(60248, "https://github.com/dotnet/roslyn/issues/60248")]
        public void SpeculativeSemanticModel()
        {
            var source = @"
int x = 1;
System.Console.WriteLine(0);

public class C
{
    public void M()
    {
        System.Console.WriteLine(2);
    }
}
";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetRoot();
            var model = compilation.GetSemanticModel(tree);
            var nodeToSpeculate = SyntaxFactory.ParseStatement("int y = x;");

            // Speculate inside a valid top-level position.
            model.TryGetSpeculativeSemanticModel(root.DescendantNodes().Single(n => n is ExpressionStatementSyntax { Parent: GlobalStatementSyntax }).Span.End, nodeToSpeculate, out var speculativeModelInTopLevel);
            var conversionInTopLevel = speculativeModelInTopLevel.GetConversion(nodeToSpeculate.DescendantTokens().Single(n => n.ValueText == "x").Parent);
            Assert.Equal(ConversionKind.Identity, conversionInTopLevel.Kind);

            // Speculate outside a top-level position.
            model.TryGetSpeculativeSemanticModel(root.DescendantNodes().Single(n => n is ExpressionStatementSyntax { Parent: BlockSyntax }).Span.End, nodeToSpeculate, out var speculativeModelOutsideTopLevel);
            var conversionOutsideTopLevel = speculativeModelOutsideTopLevel.GetConversion(nodeToSpeculate.DescendantTokens().Single(n => n.ValueText == "x").Parent);
            Assert.Equal(ConversionKind.NoConversion, conversionOutsideTopLevel.Kind);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67050")]
        public void EmptyLocalDeclaration()
        {
            var src = """
struct S { }
partial ext X
""";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (1,13): error CS1031: Type expected
                // struct S { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 13),
                // (1,13): error CS1525: Invalid expression term 'partial'
                // struct S { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("partial").WithLocation(1, 13),
                // (1,13): error CS1003: Syntax error, ',' expected
                // struct S { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(1, 13),
                // (2,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // partial ext X
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "").WithLocation(2, 1),
                // (2,14): error CS1002: ; expected
                // partial ext X
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(2, 14)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/70909")]
        public void ExplicitBase_01()
        {
            var src1 = """
PrintLine();
""";

            var src2 = """
﻿public class ProgramBase
{
    public static void PrintLine()
    {
        System.Console.WriteLine("Done");
    }
}

partial class Program : ProgramBase
{
}
""";
            var comp = CreateCompilation(new[] { src1, src2 }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Done").VerifyDiagnostics();

            comp = CreateCompilation(new[] { src2, src1 }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Done").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/70909")]
        public void ExplicitBase_02()
        {
            var src1 = """
ProgramBase.PrintLine();
""";

            var src2 = """
﻿public class ProgramBase
{
    public static void PrintLine()
    {
        System.Console.WriteLine("Done");
    }
}

partial class Program : object
{
}
""";
            var comp = CreateCompilation(new[] { src1, src2 }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Done").VerifyDiagnostics();

            comp = CreateCompilation(new[] { src2, src1 }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Done").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/70909")]
        public void ExplicitBase_03()
        {
            var src1 = """
ProgramBase.PrintLine();
""";

            var src2 = """
﻿public class ProgramBase
{
    public static void PrintLine()
    {
        System.Console.WriteLine("Done");
    }
}

partial class Program
{
}
""";
            var comp = CreateCompilation(new[] { src1, src2 }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Done").VerifyDiagnostics();

            comp = CreateCompilation(new[] { src2, src1 }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "Done").VerifyDiagnostics();
        }
    }
}
