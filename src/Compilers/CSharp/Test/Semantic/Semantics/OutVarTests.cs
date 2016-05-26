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
                // (6,29): error CS8058: Feature 'out variable declaration' is experimental and unsupported; use '/features:outVar' to enable.
                //         Test2(Test1(out int x1), x1);
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "x1").WithArguments("out variable declaration", "outVar").WithLocation(6, 29)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        private static IdentifierNameSyntax GetReference(SyntaxTree tree, string name)
        {
            return GetReferences(tree, name).Single();
        }
        
        private static IdentifierNameSyntax[] GetReferences(SyntaxTree tree, string name, int count)
        {
            var nameRef = GetReferences(tree, name).ToArray();
            Assert.Equal(count, nameRef.Length);
            return nameRef;
        }

        private static IEnumerable<IdentifierNameSyntax> GetReferences(SyntaxTree tree, string name)
        {
            return tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == name);
        }

        private static ArgumentSyntax GetOutVarDeclaration(SyntaxTree tree, string name)
        {
            return GetOutVarDeclarations(tree, name).Single();
        }

        private static IEnumerable<ArgumentSyntax> GetOutVarDeclarations(SyntaxTree tree, string name)
        {
            return tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.ValueText == name);
        }

        private static IEnumerable<ArgumentSyntax> GetOutVarDeclarations(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Where(p => p.Identifier.Kind() != SyntaxKind.None);
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x2Ref = GetReference(tree, "x2");
            Assert.Null(model.GetDeclaredSymbol(x2Ref));
            Assert.Null(model.GetDeclaredSymbol((ArgumentSyntax)x2Ref.Parent));
        }

        private static void VerifyModelForOutVar(SemanticModel model, ArgumentSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForOutVar(model, decl, false, references);
        }

        private static void VerifyModelForOutVar(SemanticModel model, ArgumentSyntax decl, bool isDelegateCreation, params IdentifierNameSyntax[] references)
        {
            var variableDeclaratorSyntax = GetVariableDeclarator(decl);
            var symbol = model.GetDeclaredSymbol(variableDeclaratorSyntax);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(LocalDeclarationKind.RegularVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)variableDeclaratorSyntax));
            Assert.Same(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

            var local = (SourceLocalSymbol)symbol;

            if (decl.Type.IsVar && local.IsVar && local.Type.IsErrorType())
            {
                Assert.Null(model.GetSymbolInfo(decl.Type).Symbol);
            }
            else
            {
                Assert.Equal(local.Type, model.GetSymbolInfo(decl.Type).Symbol);
            }

            foreach (var reference in references)
            {
                Assert.Same(symbol, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: decl.Identifier.ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(decl.Identifier.ValueText));
                Assert.Equal(local.Type, model.GetTypeInfo(reference).Type);
            }

            if (!(decl.Parent.Parent is ConstructorInitializerSyntax))
            {
                var dataFlowParent = (ExpressionSyntax)decl.Parent.Parent;
                var dataFlow = model.AnalyzeDataFlow(dataFlowParent);

                Assert.True(dataFlow.Succeeded);
                Assert.True(dataFlow.VariablesDeclared.Contains(symbol, ReferenceEqualityComparer.Instance));

                if (!isDelegateCreation)
                {
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
            }
        }

        private static VariableDeclaratorSyntax GetVariableDeclarator(ArgumentSyntax decl)
        {
            return decl.VariableDeclaration.Variables.Single();
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x2Ref = GetReference(tree, "x2");
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1", 2);
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1", 2);
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1", 2);
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x2Ref = GetReference(tree, "x2");
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void Simple_10()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out dynamic x1), x1);
    }

    static object Test1(out dynamic x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            references: new MetadataReference[] { CSharpRef, SystemCoreRef }, 
                                                            options: TestOptions.ReleaseExe, 
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void Simple_11()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out int[] x1), x1);
    }

    static object Test1(out int[] x)
    {
        x = new [] {123};
        return null;
    }

    static void Test2(object x, int[] y)
    {
        System.Console.WriteLine(y[0]);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
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
        public void Scope_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(x1, 
              Test1(out int x1));
    }

    static object Test1(out int x)
    {
        x = 1;
        return null;
    }

    static void Test2(object y, object x)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,15): error CS0841: Cannot use local variable 'x1' before it is declared
                //         Test2(x1, 
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 15)
                );
        }

        [Fact]
        public void Scope_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(out x1, 
              Test1(out int x1));
    }

    static object Test1(out int x)
    {
        x = 1;
        return null;
    }

    static void Test2(out int y, object x)
    {
        y = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,19): error CS0841: Cannot use local variable 'x1' before it is declared
                //         Test2(out x1, 
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 19)
                );
        }

        [Fact]
        public void Scope_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(out int x1);
        System.Console.WriteLine(x1);
    }

    static object Test1(out int x)
    {
        x = 1;
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (7,34): error CS0103: The name 'x1' does not exist in the current context
                //         System.Console.WriteLine(x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(7, 34)
                );
        }

        [Fact]
        public void Scope_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(out x1, 
              Test1(out var x1));
    }

    static object Test1(out int x)
    {
        x = 1;
        return null;
    }

    static void Test2(out int y, object x)
    {
        y = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,19): error CS0841: Cannot use local variable 'x1' before it is declared
                //         Test2(out x1, 
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 19)
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }
        
        [Fact]
        public void DataFlow_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out int x1);
        var x2 = 1;
    }

    static void Test(out int x)
    {
        x = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (7,13): warning CS0219: The variable 'x2' is assigned but its value is never used
                //         var x2 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x2").WithArguments("x2").WithLocation(7, 13)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            VerifyModelForOutVar(model, x1Decl);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();

            var dataFlow = model.AnalyzeDataFlow(x2Decl);

            Assert.True(dataFlow.Succeeded);
            Assert.Equal("System.Int32 x2", dataFlow.VariablesDeclared.Single().ToTestDisplayString());
            Assert.Equal("System.Int32 x1", dataFlow.WrittenOutside.Single().ToTestDisplayString());
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
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

            Assert.False(GetOutVarDeclarations(tree).Any());
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

            var x1Decl = GetOutVarDeclaration(tree, "x1");
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

            Assert.False(GetOutVarDeclarations(tree).Any());
        }

        [Fact]
        public void GetAliasInfo_01()
        {
            var text = @"
using a = System.Int32;

public class Cls
{
    public static void Main()
    {
        Test1(out a x1);
    }

    static object Test1(out int x)
    {
        x = 123;
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            VerifyModelForOutVar(model, x1Decl);

            Assert.Equal("a=System.Int32", model.GetAliasInfo(x1Decl.Type).ToTestDisplayString());
        }

        [Fact]
        public void VarIsNotVar_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out var x1), x1);
    }

    static object Test1(out var x)
    {
        x = new var() {val = 123};
        return null;
    }

    static void Test2(object x, var y)
    {
        System.Console.WriteLine(y.val);
    }

    struct var
    {
        public int val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void VarIsNotVar_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(out var x1);
    }

    struct var
    {
        public int val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,9): error CS0103: The name 'Test1' does not exist in the current context
                //         Test1(out var x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Test1").WithArguments("Test1").WithLocation(6, 9),
                // (11,20): warning CS0649: Field 'Cls.var.val' is never assigned to, and will always have its default value 0
                //         public int val;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "val").WithArguments("Cls.var.val", "0").WithLocation(11, 20)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            VerifyModelForOutVar(model, x1Decl);

            Assert.Equal("Cls.var", ((LocalSymbol)model.GetDeclaredSymbol(GetVariableDeclarator(x1Decl))).Type.ToTestDisplayString());
        }

        [Fact]
        public void SimpleVar_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out var x1), x1);
    }

    static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type));
        }

        [Fact]
        public void SimpleVar_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out var x1), x1);
    }

    static void Test2(object x, object y)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,15): error CS0103: The name 'Test1' does not exist in the current context
                //         Test2(Test1(out var x1), x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Test1").WithArguments("Test1").WithLocation(6, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type));
        }

        [Fact]
        public void SimpleVar_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(out var x1, 
                  out x1);
    }

    static object Test1(out int x, out int x2)
    {
        x = 123;
        x2 = 124;
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (7,23): error CS8927: Reference to an implicitly-typed out variable 'x1' is not permitted in the same argument list.
                //                   out x1);
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedOutVariableUsedInTheSameArgumentList, "x1").WithArguments("x1").WithLocation(7, 23)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type));
            Assert.Equal("System.Int32 x1", model.GetDeclaredSymbol(GetVariableDeclarator(x1Decl)).ToTestDisplayString());
        }

        [Fact]
        public void SimpleVar_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(out var x1, 
              Test1(out x1,
                    3));
    }

    static object Test1(out int x, int x2)
    {
        x = 123;
        x2 = 124;
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (7,25): error CS8927: Reference to an implicitly-typed out variable 'x1' is not permitted in the same argument list.
                //               Test1(out x1,
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedOutVariableUsedInTheSameArgumentList, "x1").WithArguments("x1").WithLocation(7, 25)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type));
            Assert.Equal("System.Int32 x1", model.GetDeclaredSymbol(GetVariableDeclarator(x1Decl)).ToTestDisplayString());
        }

        [Fact]
        public void SimpleVar_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out var x1), x1);
    }

    static object Test1(ref int x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,21): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         Test2(Test1(out var x1), x1);
                Diagnostic(ErrorCode.ERR_BadArgRef, "out var x1").WithArguments("1", "ref").WithLocation(6, 21)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type));
            Assert.Equal("System.Int32 x1", model.GetDeclaredSymbol(GetVariableDeclarator(x1Decl)).ToTestDisplayString());
        }

        [Fact]
        public void SimpleVar_06()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out var x1), x1);
    }

    static object Test1(int x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,21): error CS1615: Argument 1 may not be passed with the 'out' keyword
                //         Test2(Test1(out var x1), x1);
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "out var x1").WithArguments("1", "out").WithLocation(6, 21)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type));
            Assert.Equal("System.Int32 x1", model.GetDeclaredSymbol(GetVariableDeclarator(x1Decl)).ToTestDisplayString());
        }
        
        [Fact]
        public void SimpleVar_07()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        dynamic x = null;
        Test2(x.Test1(out var x1), 
              x1);
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (7,31): error CS8928: Cannot infer the type of implicitly-typed out variable.
                //         Test2(x.Test1(out var x1), 
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedOutVariable, "x1").WithLocation(7, 31)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type));
            Assert.Equal("var x1", model.GetDeclaredSymbol(GetVariableDeclarator(x1Decl)).ToTestDisplayString());
        }

        [Fact]
        public void SimpleVar_08()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(new Test1(out var x1), x1);
    }

    class Test1
    {
        public Test1(out int x)
        {
            x = 123;
        }
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void SimpleVar_09()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(new System.Action(out var x1), 
              x1);
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,33): error CS0149: Method name expected
                //         Test2(new System.Action(out var x1), 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "out var x1").WithLocation(6, 33),
                // (6,33): error CS0165: Use of unassigned local variable 'x1'
                //         Test2(new System.Action(out var x1), 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "out var x1").WithArguments("x1").WithLocation(6, 33)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, true, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type));
            Assert.Equal("var x1", model.GetDeclaredSymbol(GetVariableDeclarator(x1Decl)).ToTestDisplayString());
        }

        [Fact]
        public void SimpleVar_10()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }

    public static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    class Test2
    {
        Test2(object x, object y)
        {
        }

        Test2()
        : this(Test1(out var x1), x1)
        {}
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (21,30): error CS8929: Out variable declarations are not allowed within constructor initializers.
                //         : this(Test1(out var x1), x1)
                Diagnostic(ErrorCode.ERR_OutVarInConstructorInitializer, "x1").WithLocation(21, 30)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void SimpleVar_11()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }

    public static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    class Test2
    {
        public Test2(object x, object y)
        {
        }
    }
    
    class Test3 : Test2
    {

        Test3()
        : base(Test1(out var x1), x1)
        {}
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (25,30): error CS8929: Out variable declarations are not allowed within constructor initializers.
                //         : base(Test1(out var x1), x1)
                Diagnostic(ErrorCode.ERR_OutVarInConstructorInitializer, "x1").WithLocation(25, 30)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void SimpleVar_12()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }

    class Test2
    {
        Test2(out int x)
        {
            x = 2;
        }

        Test2()
        : this(out var x1)
        {}
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (16,24): error CS8929: Out variable declarations are not allowed within constructor initializers.
                //         : this(out var x1)
                Diagnostic(ErrorCode.ERR_OutVarInConstructorInitializer, "x1").WithLocation(16, 24)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            VerifyModelForOutVar(model, x1Decl);
        }

        [Fact]
        public void SimpleVar_13()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }

    class Test2
    {
        public Test2(out int x)
        {
            x = 1;
        }
    }
    
    class Test3 : Test2
    {

        Test3()
        : base(out var x1)
        {}
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (20,24): error CS8929: Out variable declarations are not allowed within constructor initializers.
                //         : base(out var x1)
                Diagnostic(ErrorCode.ERR_OutVarInConstructorInitializer, "x1").WithLocation(20, 24)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            VerifyModelForOutVar(model, x1Decl);
        }

        [Fact]
        public void SimpleVar_14()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out var x1), x1);
    }

    static object Test1(out dynamic x)
    {
        x = 123;
        return null;
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            references: new MetadataReference[] { CSharpRef, SystemCoreRef },
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type));
            Assert.Equal("dynamic x1", model.GetDeclaredSymbol(GetVariableDeclarator(x1Decl)).ToTestDisplayString());
        }

        [Fact]
        public void SimpleVar_15()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(new Test1(out var var), var);
    }

    class Test1
    {
        public Test1(out int x)
        {
            x = 123;
        }
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var varDecl = GetOutVarDeclaration(tree, "var");
            var varRef = GetReferences(tree, "var").Skip(1).Single();
            VerifyModelForOutVar(model, varDecl, varRef);
        }
        
        [Fact]
        public void SimpleVar_16()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        if (Test1(out var x1))
        {
            System.Console.WriteLine(x1);
        }
    }

    static bool Test1(out int x)
    {
        x = 123;
        return true;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");

            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref).Type.ToTestDisplayString());

            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type));
        }

        [Fact]
        public void VarAndBetterness_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(out var x1, null);
        Test2(out var x2, null);
    }

    static object Test1(out int x, object y)
    {
        x = 123;
        System.Console.WriteLine(x);
        return null;
    }

    static object Test1(out short x, string y)
    {
        x = 124;
        System.Console.WriteLine(x);
        return null;
    }

    static object Test2(out int x, string y)
    {
        x = 125;
        System.Console.WriteLine(x);
        return null;
    }

    static object Test2(out short x, object y)
    {
        x = 126;
        System.Console.WriteLine(x);
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            CompileAndVerify(compilation, expectedOutput:
@"124
125").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            VerifyModelForOutVar(model, x1Decl);

            var x2Decl = GetOutVarDeclaration(tree, "x2");
            VerifyModelForOutVar(model, x2Decl);
        }

        [Fact]
        public void VarAndBetterness_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(out var x1, null);
    }

    static object Test1(out int x, object y)
    {
        x = 123;
        System.Console.WriteLine(x);
        return null;
    }

    static object Test1(out short x, object y)
    {
        x = 124;
        System.Console.WriteLine(x);
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Cls.Test1(out int, object)' and 'Cls.Test1(out short, object)'
                //         Test1(out var x1, null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test1").WithArguments("Cls.Test1(out int, object)", "Cls.Test1(out short, object)").WithLocation(6, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            VerifyModelForOutVar(model, x1Decl);
        }

        [Fact]
        public void RestrictedTypes_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out var x1), x1);
    }

    static object Test1(out System.ArgIterator x)
    {
        x = default(System.ArgIterator);
        return null;
    }

    static void Test2(object x, System.ArgIterator y)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (9,25): error CS1601: Cannot make reference to variable of type 'ArgIterator'
                //     static object Test1(out System.ArgIterator x)
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out System.ArgIterator x").WithArguments("System.ArgIterator").WithLocation(9, 25)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void RestrictedTypes_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out System.ArgIterator x1), x1);
    }

    static object Test1(out System.ArgIterator x)
    {
        x = default(System.ArgIterator);
        return null;
    }

    static void Test2(object x, System.ArgIterator y)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (9,25): error CS1601: Cannot make reference to variable of type 'ArgIterator'
                //     static object Test1(out System.ArgIterator x)
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out System.ArgIterator x").WithArguments("System.ArgIterator").WithLocation(9, 25)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }

        [Fact]
        public void RestrictedTypes_03()
        {
            var text = @"
public class Cls
{
    public static void Main() {}

    async void Test()
    {
        Test2(Test1(out var x1), x1);
    }

    static object Test1(out System.ArgIterator x)
    {
        x = default(System.ArgIterator);
        return null;
    }

    static void Test2(object x, System.ArgIterator y)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (11,25): error CS1601: Cannot make reference to variable of type 'ArgIterator'
                //     static object Test1(out System.ArgIterator x)
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out System.ArgIterator x").WithArguments("System.ArgIterator").WithLocation(11, 25),
                // (8,25): error CS4012: Parameters or locals of type 'ArgIterator' cannot be declared in async methods or lambda expressions.
                //         Test2(Test1(out var x1), x1);
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "var").WithArguments("System.ArgIterator").WithLocation(8, 25),
                // (6,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async void Test()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Test").WithLocation(6, 16)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }
        
        [Fact]
        public void RestrictedTypes_04()
        {
            var text = @"
public class Cls
{
    public static void Main() {}

    async void Test()
    {
        Test2(Test1(out System.ArgIterator x1), x1);
        var x = default(System.ArgIterator);
    }

    static object Test1(out System.ArgIterator x)
    {
        x = default(System.ArgIterator);
        return null;
    }

    static void Test2(object x, System.ArgIterator y)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (12,25): error CS1601: Cannot make reference to variable of type 'ArgIterator'
                //     static object Test1(out System.ArgIterator x)
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out System.ArgIterator x").WithArguments("System.ArgIterator").WithLocation(12, 25),
                // (8,25): error CS4012: Parameters or locals of type 'ArgIterator' cannot be declared in async methods or lambda expressions.
                //         Test2(Test1(out System.ArgIterator x1), x1);
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(8, 25),
                // (9,9): error CS4012: Parameters or locals of type 'ArgIterator' cannot be declared in async methods or lambda expressions.
                //         var x = default(System.ArgIterator);
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "var").WithArguments("System.ArgIterator").WithLocation(9, 9),
                // (9,13): warning CS0219: The variable 'x' is assigned but its value is never used
                //         var x = default(System.ArgIterator);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(9, 13),
                // (6,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async void Test()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Test").WithLocation(6, 16)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);
        }
        
        [Fact]
        public void ElementAccess_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = new [] {1};
        Test2(x[out var x1], x1);
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (7,25): error CS1003: Syntax error, ',' expected
                //         Test2(x[out var x1], x1);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x1").WithArguments(",", "").WithLocation(7, 25),
                // (7,21): error CS0103: The name 'var' does not exist in the current context
                //         Test2(x[out var x1], x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(7, 21),
                // (7,25): error CS0103: The name 'x1' does not exist in the current context
                //         Test2(x[out var x1], x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(7, 25),
                // (7,30): error CS0103: The name 'x1' does not exist in the current context
                //         Test2(x[out var x1], x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(7, 30)
                );

            var tree = compilation.SyntaxTrees.Single();
            Assert.False(GetOutVarDeclarations(tree, "x1").Any());
        }

        [Fact]
        public void ElementAccess_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = new [] {1};
        Test2(x[out int x1], x1);
    }

    static void Test2(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular.WithOutVarFeature());

            compilation.VerifyDiagnostics(
                // (7,21): error CS1525: Invalid expression term 'int'
                //         Test2(x[out int x1], x1);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(7, 21),
                // (7,25): error CS1003: Syntax error, ',' expected
                //         Test2(x[out int x1], x1);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x1").WithArguments(",", "").WithLocation(7, 25),
                // (7,25): error CS0103: The name 'x1' does not exist in the current context
                //         Test2(x[out int x1], x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(7, 25),
                // (7,30): error CS0103: The name 'x1' does not exist in the current context
                //         Test2(x[out int x1], x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(7, 30)
                );

            var tree = compilation.SyntaxTrees.Single();
            Assert.False(GetOutVarDeclarations(tree, "x1").Any());
        }
    }
}
