// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.OutVar)]
    public class OutVarTests : CompilingTestBase
    {
        [Fact]
        public void OldVersion()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out int x1), x1);
    }

    static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, int y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            compilation.VerifyDiagnostics(
                // (6,29): error CS8058: Feature 'out var' is experimental and unsupported; use '/features:outVar' to enable.
                //         Test2(Test1(out int x1), x1);
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "x1").WithArguments("out var", "outVar").WithLocation(6, 29)
                );
        }

        [Fact]
        public void Simple_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out int x1), x1);
        int x2;
        Test3(out x2);
    }

    static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, int y)
    {
        System.Console.WriteLine(y);
    }

    static void Test3(out int y)
    {
        y = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            Assert.Null(model.GetDeclaredSymbol(x2Ref));
            Assert.Null(model.GetDeclaredSymbol((ArgumentSyntax)x2Ref.Parent));
        }

        private static void VerifyModelForOutVar(SemanticModel model, ArgumentSyntax decl, params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(LocalDeclarationKind.RegularVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));
            Assert.Same(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

            foreach (var reference in references)
            {
                Assert.Same(symbol, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: decl.Identifier.ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(decl.Identifier.ValueText));
            }

            var dataFlowParent = decl.Ancestors().OfType<ExpressionSyntax>().First();
            var dataFlow = model.AnalyzeDataFlow(dataFlowParent);

            Assert.True(dataFlow.Succeeded);
            Assert.True(dataFlow.VariablesDeclared.Contains(symbol, ReferenceEqualityComparer.Instance));
            Assert.True(dataFlow.AlwaysAssigned.Contains(symbol, ReferenceEqualityComparer.Instance));
            Assert.True(dataFlow.WrittenInside.Contains(symbol, ReferenceEqualityComparer.Instance));
            var flowsIn = FlowsIn(dataFlowParent, decl, references);
            Assert.Equal(flowsIn,
                         dataFlow.DataFlowsIn.Contains(symbol, ReferenceEqualityComparer.Instance));
            Assert.Equal(flowsIn,
                         dataFlow.ReadInside.Contains(symbol, ReferenceEqualityComparer.Instance));

            var flowsOut = FlowsOut(dataFlowParent, references);
            Assert.Equal(flowsOut, 
                         dataFlow.DataFlowsOut.Contains(symbol, ReferenceEqualityComparer.Instance));
            Assert.Equal(flowsOut,
                         dataFlow.ReadOutside.Contains(symbol, ReferenceEqualityComparer.Instance));

            Assert.Equal(WrittenOutside(dataFlowParent, references),
                         dataFlow.WrittenOutside.Contains(symbol, ReferenceEqualityComparer.Instance));
        }

        private static bool FlowsIn(ExpressionSyntax dataFlowParent, ArgumentSyntax decl, IdentifierNameSyntax[] references)
        {
            foreach (var reference in references)
            {
                if (dataFlowParent.Span.Contains(reference.Span) && reference.SpanStart > decl.SpanStart)
                {
                    if (IsRead(reference))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsRead(IdentifierNameSyntax reference)
        {
            switch (reference.Parent.Kind())
            {
                case SyntaxKind.Argument:
                    if (((ArgumentSyntax)reference.Parent).RefOrOutKeyword.Kind() != SyntaxKind.OutKeyword)
                    {
                        return true;
                    }
                    break;

                case SyntaxKind.SimpleAssignmentExpression:
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                    if (((AssignmentExpressionSyntax)reference.Parent).Left != reference)
                    {
                        return true;
                    }
                    break;

                default:
                    return true;
            }

            return false;
        }

        private static bool FlowsOut(ExpressionSyntax dataFlowParent, IdentifierNameSyntax[] references)
        {
            foreach (var reference in references)
            {
                if (!dataFlowParent.Span.Contains(reference.Span))
                {
                    if (IsRead(reference))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool WrittenOutside(ExpressionSyntax dataFlowParent, IdentifierNameSyntax[] references)
        {
            foreach (var reference in references)
            {
                if (!dataFlowParent.Span.Contains(reference.Span))
                {
                    if (IsWrite(reference))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsWrite(IdentifierNameSyntax reference)
        {
            switch (reference.Parent.Kind())
            {
                case SyntaxKind.Argument:
                    if (((ArgumentSyntax)reference.Parent).RefOrOutKeyword.Kind() != SyntaxKind.None)
                    {
                        return true;
                    }
                    break;

                case SyntaxKind.SimpleAssignmentExpression:
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                    if (((AssignmentExpressionSyntax)reference.Parent).Left == reference)
                    {
                        return true;
                    }
                    break;

                default:
                    return true;
            }

            return false;
        }

        [Fact]
        public void Simple_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out System.Int32 x1), x1);
        int x2 = 0;
        Test3(x2);
    }

    static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, int y)
    {
        System.Console.WriteLine(y);
    }

    static void Test3(int y)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            Assert.Null(model.GetDeclaredSymbol(x2Ref));
            Assert.Null(model.GetDeclaredSymbol((ArgumentSyntax)x2Ref.Parent));
        }

        [Fact]
        public void Simple_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out (int, int) x1), x1);
    }

    static object Test1(out (int, int) x)
    {
        x = (123, 124);
        return null;
    }

    static void Test2(object x, (int, int) y)
    {
        System.Console.WriteLine(y);
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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature().WithTuplesFeature());

            CompileAndVerify(compilation, expectedOutput: @"{123, 124}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void Simple_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out System.Collections.Generic.IEnumerable<System.Int32> x1), x1);
    }

    static object Test1(out System.Collections.Generic.IEnumerable<System.Int32> x)
    {
        x = new System.Collections.Generic.List<System.Int32>();
        return null;
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"System.Collections.Generic.List`1[System.Int32]").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void Simple_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out int x1, out x1), x1);
    }

    static object Test1(out int x, out int y)
    {
        x = 123;
        y = 124;
        return null;
    }

    static void Test2(object x, int y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"124").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void Simple_06()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out int x1, x1 = 124), x1);
    }

    static object Test1(out int x, int y)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, int y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void Simple_07()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out int x1), x1, x1 = 124);
    }

    static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, int y, int z)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void Simple_08()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out int x1), ref x1);
        int x2 = 0;
        Test3(ref x2);
    }

    static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, ref int y)
    {
        System.Console.WriteLine(y);
    }

    static void Test3(ref int y)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            Assert.Null(model.GetDeclaredSymbol(x2Ref));
            Assert.Null(model.GetDeclaredSymbol((ArgumentSyntax)x2Ref.Parent));
        }

        [Fact]
        public void Simple_09()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out int x1), out x1);
    }

    static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, out int y)
    {
        y = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void Scope_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int x1 = 0;
        Test1(out int x1);
        Test2(Test1(out int x2), 
                    out int x2);
    }

    static object Test1(out int x)
    {
        x = 1;
        return null;
    }

    static void Test2(object y, out int x)
    {
        x = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (7,23): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Test1(out int x1);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(7, 23),
                // (9,29): error CS0128: A local variable named 'x2' is already defined in this scope
                //                     out int x2);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(9, 29),
                // (6,13): warning CS0219: The variable 'x1' is assigned but its value is never used
                //         int x1 = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x1").WithArguments("x1").WithLocation(6, 13)
                );
        }

        [Fact]
        public void DataFlow_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out int x1, 
                     x1);
    }

    static void Test(out int x, int y)
    {
        x = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (7,22): error CS0165: Use of unassigned local variable 'x1'
                //                      x1);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(7, 22)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void DataFlow_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out int x1, 
                 ref x1);
    }

    static void Test(out int x, ref int y)
    {
        x = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (7,22): error CS0165: Use of unassigned local variable 'x1'
                //                  ref x1);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(7, 22)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void TypeMismatch_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out int x1);
    }

    static void Test(out short x)
    {
        x = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,14): error CS1503: Argument 1: cannot convert from 'out int' to 'out short'
                //         Test(out int x1);
                Diagnostic(ErrorCode.ERR_BadArgType, "out int x1").WithArguments("1", "out int", "out short").WithLocation(6, 14)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            VerifyModelForOutVar(model, x1Decl);
        }

        [Fact]
        public void Parse_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(int x1);
        Test(ref int x2);
    }

    static void Test(out int x)
    {
        x = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,14): error CS1525: Invalid expression term 'int'
                //         Test(int x1);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 14),
                // (6,18): error CS1003: Syntax error, ',' expected
                //         Test(int x1);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x1").WithArguments(",", "").WithLocation(6, 18),
                // (7,18): error CS1525: Invalid expression term 'int'
                //         Test(ref int x2);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(7, 18),
                // (7,22): error CS1003: Syntax error, ',' expected
                //         Test(ref int x2);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x2").WithArguments(",", "").WithLocation(7, 22),
                // (6,18): error CS0103: The name 'x1' does not exist in the current context
                //         Test(int x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(6, 18),
                // (7,22): error CS0103: The name 'x2' does not exist in the current context
                //         Test(ref int x2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(7, 22)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            Assert.False(tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.Kind() != SyntaxKind.None).Any());
        }

        [Fact]
        public void Parse_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out int x1.);
    }

    static void Test(out int x)
    {
        x = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,24): error CS1003: Syntax error, ',' expected
                //         Test(out int x1.);
                Diagnostic(ErrorCode.ERR_SyntaxError, ".").WithArguments(",", ".").WithLocation(6, 24)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            VerifyModelForOutVar(model, x1Decl);
        }

        [Fact]
        public void Parse_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out System.Collections.Generic.IEnumerable<System.Int32>);
    }

    static void Test(out System.Collections.Generic.IEnumerable<System.Int32> x)
    {
        x = null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,18): error CS0118: 'IEnumerable<int>' is a type but is used like a variable
                //         Test(out System.Collections.Generic.IEnumerable<System.Int32>);
                Diagnostic(ErrorCode.ERR_BadSKknown, "System.Collections.Generic.IEnumerable<System.Int32>").WithArguments("System.Collections.Generic.IEnumerable<int>", "type", "variable").WithLocation(6, 18)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            Assert.False(tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.Kind() != SyntaxKind.None).Any());
        }

        [Fact(Skip = "Needs adjustment. + No 'var' support yet.")]
        public void OutVar_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(out var y);
        Print(y);
        Test2(out (var z));
        Print(z);
        var notused = new Cls(out var u);
        Print(u);

        Test1(out checked(var v));
        Print(v);
        Test2(out unchecked(var w));
        Print(w);

        notused = new Cls(out (checked(unchecked((checked(unchecked(var a)))))));
        Print(a);
    }

    static void Test1(out int x)
    {
        x = 123;
    }

    static void Test2(out short x)
    {
        x = 1234;
    }

    static void Print<T>(T val)
    {
        System.Console.WriteLine(val);
        System.Console.WriteLine(typeof(T));
    }

    Cls(out byte x)
    {
        x = 31;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123
System.Int32
1234
System.Int16
31
System.Byte
123
System.Int32
1234
System.Int16
31
System.Byte").VerifyDiagnostics();

            //TestSemanticModelAPI(compilation);
        }
    }
}
