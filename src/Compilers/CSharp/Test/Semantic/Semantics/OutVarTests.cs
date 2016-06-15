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
            VerifyModelForOutVar(model, decl, false, true, references);
        }

        private static void VerifyModelForOutVarInNotExecutableCode(SemanticModel model, ArgumentSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForOutVar(model, decl, false, false, references);
        }

        private static void VerifyModelForOutVar(SemanticModel model, ArgumentSyntax decl, bool isDelegateCreation, bool isExecutableCode, params IdentifierNameSyntax[] references)
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

            VerifyDataFlow(model, decl, isDelegateCreation, isExecutableCode, references, symbol);
        }

        private static void VerifyDataFlow(SemanticModel model, ArgumentSyntax decl, bool isDelegateCreation, bool isExecutableCode, IdentifierNameSyntax[] references, ISymbol symbol)
        {
            var dataFlowParent = decl.Parent.Parent as ExpressionSyntax;

            if (dataFlowParent == null)
            {
                Assert.IsAssignableFrom<ConstructorInitializerSyntax>(decl.Parent.Parent);
                return;
            }

            var dataFlow = model.AnalyzeDataFlow(dataFlowParent);

            Assert.Equal(isExecutableCode, dataFlow.Succeeded);

            if (isExecutableCode)
            {
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

                    Assert.Equal(FlowsOut(dataFlowParent, decl, references),
                                 dataFlow.DataFlowsOut.Contains(symbol, ReferenceEqualityComparer.Instance));
                    Assert.Equal(ReadOutside(dataFlowParent, references),
                                 dataFlow.ReadOutside.Contains(symbol, ReferenceEqualityComparer.Instance));

                    Assert.Equal(WrittenOutside(dataFlowParent, references),
                                 dataFlow.WrittenOutside.Contains(symbol, ReferenceEqualityComparer.Instance));
                }
            }
        }

        private static void VerifyModelForOutVarDuplicateInSameScope(SemanticModel model, ArgumentSyntax decl)
        {
            var variableDeclaratorSyntax = GetVariableDeclarator(decl);
            var symbol = model.GetDeclaredSymbol(variableDeclaratorSyntax);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(LocalDeclarationKind.RegularVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)variableDeclaratorSyntax));
            Assert.NotEqual(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
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
        }

        private static void VerifyNotInScope(SemanticModel model, IdentifierNameSyntax reference)
        {
            Assert.Null(model.GetSymbolInfo(reference).Symbol);
            Assert.False(model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Any());
            Assert.False(model.LookupNames(reference.SpanStart).Contains(reference.Identifier.ValueText));
        }

        private static void VerifyNotAnOutLocal(SemanticModel model, IdentifierNameSyntax reference)
        {
            var symbol = model.GetSymbolInfo(reference).Symbol;

            if (symbol.Kind == SymbolKind.Local)
            {
                var local = (SourceLocalSymbol)symbol;
                var parent = local.IdentifierToken.Parent;

                if (parent.Kind() == SyntaxKind.VariableDeclarator)
                {
                    Assert.Equal(SyntaxKind.LocalDeclarationStatement, ((VariableDeclarationSyntax)((VariableDeclaratorSyntax)parent).Parent).Parent.Kind());
                }
            }

            Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(reference.SpanStart).Contains(reference.Identifier.ValueText));
        }

        private static VariableDeclaratorSyntax GetVariableDeclarator(ArgumentSyntax decl)
        {
            return decl.Declaration.Variables.Single();
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

        private static bool ReadOutside(ExpressionSyntax dataFlowParent, IdentifierNameSyntax[] references)
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

        private static bool FlowsOut(ExpressionSyntax dataFlowParent, ArgumentSyntax decl, IdentifierNameSyntax[] references)
        {
            foreach (var reference in references)
            {
                if (!dataFlowParent.Span.Contains(reference.Span) && 
                    (reference.SpanStart > decl.SpanStart || 
                     reference.Ancestors().OfType<DoStatementSyntax>().Join(
                         decl.Ancestors().OfType<DoStatementSyntax>(), d => d, d => d, (d1, d2) => true).Any()))
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
                    return false;
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
        public void Scope_Attribute_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    [Test(p = TakeOutParam(out int x3) && x3 > 0)]
    [Test(p = x4 && TakeOutParam(out int x4))]
    [Test(p = TakeOutParam(51, out int x5) && 
              TakeOutParam(52, out int x5) && 
              x5 > 0)]
    [Test(p1 = TakeOutParam(out int x6) && x6 > 0, 
          p2 = TakeOutParam(out int x6) && x6 > 0)]
    [Test(p = TakeOutParam(out int x7) && x7 > 0)]
    [Test(p = x7 > 2)]
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}

    static bool TakeOutParam(out int x) 
    {
        x = 123;
        return true;
    }
    static bool TakeOutParam(object y, out int x)
    {
        x = 123;
        return true;
    }
}

class Test : System.Attribute
{
    public bool p {get; set;}
    public bool p1 {get; set;}
    public bool p2 {get; set;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (8,15): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(p = TakeOutParam(out int x3) && x3 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out int x3) && x3 > 0").WithLocation(8, 15),
                // (9,15): error CS0841: Cannot use local variable 'x4' before it is declared
                //     [Test(p = x4 && TakeOutParam(out int x4))]
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(9, 15),
                // (11,40): error CS0128: A local variable named 'x5' is already defined in this scope
                //               TakeOutParam(52, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(11, 40),
                // (10,15): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(p = TakeOutParam(51, out int x5) && 
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"TakeOutParam(51, out int x5) && 
              TakeOutParam(52, out int x5) && 
              x5 > 0").WithLocation(10, 15),
                // (14,37): error CS0128: A local variable named 'x6' is already defined in this scope
                //           p2 = TakeOutParam(out int x6) && x6 > 0)]
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(14, 37),
                // (13,16): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(p1 = TakeOutParam(out int x6) && x6 > 0, 
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out int x6) && x6 > 0").WithLocation(13, 16),
                // (14,16): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //           p2 = TakeOutParam(out int x6) && x6 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out int x6) && x6 > 0").WithLocation(14, 16),
                // (15,15): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(p = TakeOutParam(out int x7) && x7 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out int x7) && x7 > 0").WithLocation(15, 15),
                // (16,15): error CS0103: The name 'x7' does not exist in the current context
                //     [Test(p = x7 > 2)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(16, 15),
                // (17,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(17, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetOutVarDeclaration(tree, "x3");
            var x3Ref = GetReference(tree, "x3");
            VerifyModelForOutVarInNotExecutableCode(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclaration(tree, "x4");
            var x4Ref = GetReference(tree, "x4");
            VerifyModelForOutVarInNotExecutableCode(model, x4Decl, x4Ref);

            var x5Decl = GetOutVarDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReference(tree, "x5");
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x5Decl[0], x5Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x6Decl[0], x6Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = GetOutVarDeclaration(tree, "x7");
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void Scope_Attribute_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    [Test(TakeOutParam(out int x3) && x3 > 0)]
    [Test(x4 && TakeOutParam(out int x4))]
    [Test(TakeOutParam(51, out int x5) && 
          TakeOutParam(52, out int x5) && 
          x5 > 0)]
    [Test(TakeOutParam(out int x6) && x6 > 0, 
          TakeOutParam(out int x6) && x6 > 0)]
    [Test(TakeOutParam(out int x7) && x7 > 0)]
    [Test(x7 > 2)]
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}

    static bool TakeOutParam(out int x) 
    {
        x = 123;
        return true;
    }
    static bool TakeOutParam(object y, out int x)
    {
        x = 123;
        return true;
    }
}

class Test : System.Attribute
{
    public Test(bool p) {}
    public Test(bool p1, bool p2) {}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (8,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(TakeOutParam(out int x3) && x3 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out int x3) && x3 > 0").WithLocation(8, 11),
                // (9,11): error CS0841: Cannot use local variable 'x4' before it is declared
                //     [Test(x4 && TakeOutParam(out int x4))]
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(9, 11),
                // (11,36): error CS0128: A local variable named 'x5' is already defined in this scope
                //           TakeOutParam(52, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(11, 36),
                // (10,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(TakeOutParam(51, out int x5) && 
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"TakeOutParam(51, out int x5) && 
          TakeOutParam(52, out int x5) && 
          x5 > 0").WithLocation(10, 11),
                // (14,32): error CS0128: A local variable named 'x6' is already defined in this scope
                //           TakeOutParam(out int x6) && x6 > 0)]
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(14, 32),
                // (13,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(TakeOutParam(out int x6) && x6 > 0, 
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out int x6) && x6 > 0").WithLocation(13, 11),
                // (14,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //           TakeOutParam(out int x6) && x6 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out int x6) && x6 > 0").WithLocation(14, 11),
                // (15,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(TakeOutParam(out int x7) && x7 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out int x7) && x7 > 0").WithLocation(15, 11),
                // (16,11): error CS0103: The name 'x7' does not exist in the current context
                //     [Test(x7 > 2)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(16, 11),
                // (17,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(17, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetOutVarDeclaration(tree, "x3");
            var x3Ref = GetReference(tree, "x3");
            VerifyModelForOutVarInNotExecutableCode(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclaration(tree, "x4");
            var x4Ref = GetReference(tree, "x4");
            VerifyModelForOutVarInNotExecutableCode(model, x4Decl, x4Ref);

            var x5Decl = GetOutVarDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReference(tree, "x5");
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x5Decl[0], x5Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x6Decl[0], x6Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = GetOutVarDeclaration(tree, "x7");
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }


        [Fact]
        public void Scope_Attribute_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    [Test(p = TakeOutParam(out var x3) && x3 > 0)]
    [Test(p = x4 && TakeOutParam(out var x4))]
    [Test(p = TakeOutParam(51, out var x5) && 
              TakeOutParam(52, out var x5) && 
              x5 > 0)]
    [Test(p1 = TakeOutParam(out var x6) && x6 > 0, 
          p2 = TakeOutParam(out var x6) && x6 > 0)]
    [Test(p = TakeOutParam(out var x7) && x7 > 0)]
    [Test(p = x7 > 2)]
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}

    static bool TakeOutParam(out int x) 
    {
        x = 123;
        return true;
    }
    static bool TakeOutParam(object y, out int x)
    {
        x = 123;
        return true;
    }
}

class Test : System.Attribute
{
    public bool p {get; set;}
    public bool p1 {get; set;}
    public bool p2 {get; set;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (8,15): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(p = TakeOutParam(out var x3) && x3 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out var x3) && x3 > 0").WithLocation(8, 15),
                // (9,15): error CS0841: Cannot use local variable 'x4' before it is declared
                //     [Test(p = x4 && TakeOutParam(out var x4))]
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(9, 15),
                // (11,40): error CS0128: A local variable named 'x5' is already defined in this scope
                //               TakeOutParam(52, out var x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(11, 40),
                // (10,15): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(p = TakeOutParam(51, out var x5) && 
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"TakeOutParam(51, out var x5) && 
              TakeOutParam(52, out var x5) && 
              x5 > 0").WithLocation(10, 15),
                // (14,37): error CS0128: A local variable named 'x6' is already defined in this scope
                //           p2 = TakeOutParam(out var x6) && x6 > 0)]
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(14, 37),
                // (13,16): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(p1 = TakeOutParam(out var x6) && x6 > 0, 
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out var x6) && x6 > 0").WithLocation(13, 16),
                // (14,16): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //           p2 = TakeOutParam(out var x6) && x6 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out var x6) && x6 > 0").WithLocation(14, 16),
                // (15,15): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(p = TakeOutParam(out var x7) && x7 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out var x7) && x7 > 0").WithLocation(15, 15),
                // (16,15): error CS0103: The name 'x7' does not exist in the current context
                //     [Test(p = x7 > 2)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(16, 15),
                // (17,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(17, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetOutVarDeclaration(tree, "x3");
            var x3Ref = GetReference(tree, "x3");
            VerifyModelForOutVarInNotExecutableCode(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclaration(tree, "x4");
            var x4Ref = GetReference(tree, "x4");
            VerifyModelForOutVarInNotExecutableCode(model, x4Decl, x4Ref);

            var x5Decl = GetOutVarDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReference(tree, "x5");
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x5Decl[0], x5Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x6Decl[0], x6Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = GetOutVarDeclaration(tree, "x7");
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void Scope_Attribute_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    [Test(TakeOutParam(out var x3) && x3 > 0)]
    [Test(x4 && TakeOutParam(out var x4))]
    [Test(TakeOutParam(51, out var x5) && 
          TakeOutParam(52, out var x5) && 
          x5 > 0)]
    [Test(TakeOutParam(out var x6) && x6 > 0, 
          TakeOutParam(out var x6) && x6 > 0)]
    [Test(TakeOutParam(out var x7) && x7 > 0)]
    [Test(x7 > 2)]
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}

    static bool TakeOutParam(out int x) 
    {
        x = 123;
        return true;
    }
    static bool TakeOutParam(object y, out int x)
    {
        x = 123;
        return true;
    }
}

class Test : System.Attribute
{
    public Test(bool p) {}
    public Test(bool p1, bool p2) {}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (8,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(TakeOutParam(out var x3) && x3 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out var x3) && x3 > 0").WithLocation(8, 11),
                // (9,11): error CS0841: Cannot use local variable 'x4' before it is declared
                //     [Test(x4 && TakeOutParam(out var x4))]
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(9, 11),
                // (11,36): error CS0128: A local variable named 'x5' is already defined in this scope
                //           TakeOutParam(52, out var x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(11, 36),
                // (10,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(TakeOutParam(51, out var x5) && 
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, @"TakeOutParam(51, out var x5) && 
          TakeOutParam(52, out var x5) && 
          x5 > 0").WithLocation(10, 11),
                // (14,32): error CS0128: A local variable named 'x6' is already defined in this scope
                //           TakeOutParam(out var x6) && x6 > 0)]
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(14, 32),
                // (13,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(TakeOutParam(out var x6) && x6 > 0, 
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out var x6) && x6 > 0").WithLocation(13, 11),
                // (14,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //           TakeOutParam(out var x6) && x6 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out var x6) && x6 > 0").WithLocation(14, 11),
                // (15,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Test(TakeOutParam(out var x7) && x7 > 0)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "TakeOutParam(out var x7) && x7 > 0").WithLocation(15, 11),
                // (16,11): error CS0103: The name 'x7' does not exist in the current context
                //     [Test(x7 > 2)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(16, 11),
                // (17,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(17, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetOutVarDeclaration(tree, "x3");
            var x3Ref = GetReference(tree, "x3");
            VerifyModelForOutVarInNotExecutableCode(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclaration(tree, "x4");
            var x4Ref = GetReference(tree, "x4");
            VerifyModelForOutVarInNotExecutableCode(model, x4Decl, x4Ref);

            var x5Decl = GetOutVarDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReference(tree, "x5");
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x5Decl[0], x5Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x6Decl[0], x6Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = GetOutVarDeclaration(tree, "x7");
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForOutVarInNotExecutableCode(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void AttributeArgument_01()
        {
            var source =
@"
public class X
{
    [Test(out var x3)]
    [Test(out int x4)]
    [Test(p: out var x5)]
    [Test(p: out int x6)]
    public static void Main()
    {
    }
}

class Test : System.Attribute
{
    public Test(out int p) { p = 100; }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (4,11): error CS1041: Identifier expected; 'out' is a keyword
                //     [Test(out var x3)]
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "out").WithArguments("", "out").WithLocation(4, 11),
                // (4,19): error CS1003: Syntax error, ',' expected
                //     [Test(out var x3)]
                Diagnostic(ErrorCode.ERR_SyntaxError, "x3").WithArguments(",", "").WithLocation(4, 19),
                // (5,11): error CS1041: Identifier expected; 'out' is a keyword
                //     [Test(out int x4)]
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "out").WithArguments("", "out").WithLocation(5, 11),
                // (5,15): error CS1525: Invalid expression term 'int'
                //     [Test(out int x4)]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(5, 15),
                // (5,19): error CS1003: Syntax error, ',' expected
                //     [Test(out int x4)]
                Diagnostic(ErrorCode.ERR_SyntaxError, "x4").WithArguments(",", "").WithLocation(5, 19),
                // (6,14): error CS1525: Invalid expression term 'out'
                //     [Test(p: out var x5)]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(6, 14),
                // (6,14): error CS1003: Syntax error, ',' expected
                //     [Test(p: out var x5)]
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",", "out").WithLocation(6, 14),
                // (6,18): error CS1003: Syntax error, ',' expected
                //     [Test(p: out var x5)]
                Diagnostic(ErrorCode.ERR_SyntaxError, "var").WithArguments(",", "").WithLocation(6, 18),
                // (6,22): error CS1003: Syntax error, ',' expected
                //     [Test(p: out var x5)]
                Diagnostic(ErrorCode.ERR_SyntaxError, "x5").WithArguments(",", "").WithLocation(6, 22),
                // (7,14): error CS1525: Invalid expression term 'out'
                //     [Test(p: out int x6)]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(7, 14),
                // (7,14): error CS1003: Syntax error, ',' expected
                //     [Test(p: out int x6)]
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",", "out").WithLocation(7, 14),
                // (7,18): error CS1003: Syntax error, ',' expected
                //     [Test(p: out int x6)]
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",", "int").WithLocation(7, 18),
                // (7,18): error CS1525: Invalid expression term 'int'
                //     [Test(p: out int x6)]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(7, 18),
                // (7,22): error CS1003: Syntax error, ',' expected
                //     [Test(p: out int x6)]
                Diagnostic(ErrorCode.ERR_SyntaxError, "x6").WithArguments(",", "").WithLocation(7, 22),
                // (4,15): error CS0103: The name 'var' does not exist in the current context
                //     [Test(out var x3)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(4, 15),
                // (4,19): error CS0103: The name 'x3' does not exist in the current context
                //     [Test(out var x3)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(4, 19),
                // (5,19): error CS0103: The name 'x4' does not exist in the current context
                //     [Test(out int x4)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(5, 19),
                // (6,18): error CS0103: The name 'var' does not exist in the current context
                //     [Test(p: out var x5)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 18),
                // (6,18): error CS1738: Named argument specifications must appear after all fixed arguments have been specified
                //     [Test(p: out var x5)]
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "var").WithLocation(6, 18),
                // (6,22): error CS0103: The name 'x5' does not exist in the current context
                //     [Test(p: out var x5)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(6, 22),
                // (7,18): error CS1738: Named argument specifications must appear after all fixed arguments have been specified
                //     [Test(p: out int x6)]
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "int").WithLocation(7, 18),
                // (7,22): error CS0103: The name 'x6' does not exist in the current context
                //     [Test(p: out int x6)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x6").WithArguments("x6").WithLocation(7, 22)
                );

            var tree = compilation.SyntaxTrees.Single();

            Assert.False(GetOutVarDeclarations(tree, "x3").Any());
            Assert.False(GetOutVarDeclarations(tree, "x4").Any());
            Assert.False(GetOutVarDeclarations(tree, "x5").Any());
            Assert.False(GetOutVarDeclarations(tree, "x6").Any());
        }

        [Fact]
        public void Scope_Catch_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        try {}
        catch when (TakeOutParam(out var x1) && x1 > 0)
        {
            Dummy(x1);
        }
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        try {}
        catch when (TakeOutParam(out var x4) && x4 > 0)
        {
            Dummy(x4);
        }
    }

    void Test6()
    {
        try {}
        catch when (x6 && TakeOutParam(out var x6))
        {
            Dummy(x6);
        }
    }

    void Test7()
    {
        try {}
        catch when (TakeOutParam(out var x7) && x7 > 0)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        try {}
        catch when (TakeOutParam(out var x8) && x8 > 0)
        {
            Dummy(x8);
        }

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        try {}
        catch when (TakeOutParam(out var x9) && x9 > 0)
        {   
            Dummy(x9);
            try {}
            catch when (TakeOutParam(out var x9) && x9 > 0) // 2
            {
                Dummy(x9);
            }
        }
    }

    void Test10()
    {
        try {}
        catch when (TakeOutParam(y10, out var x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    try {}
    //    catch when (TakeOutParam(y11, out var x11)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test14()
    {
        try {}
        catch when (Dummy(TakeOutParam(out var x14), 
                          TakeOutParam(out var x14), // 2
                          x14))
        {
            Dummy(x14);
        }
    }

    void Test15()
    {
        try {}
        catch (System.Exception x15)
              when (Dummy(TakeOutParam(out var x15), x15))
        {
            Dummy(x15);
        }
    }

    static bool TakeOutParam(out int x) 
    {
        x = 123;
        return true;
    }
    static bool TakeOutParam(object y, out int x)
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (25,42): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         catch when (TakeOutParam(out var x4) && x4 > 0)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(25, 42),
                // (34,21): error CS0841: Cannot use local variable 'x6' before it is declared
                //         catch when (x6 && TakeOutParam(out var x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(34, 21),
                // (45,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(45, 17),
                // (58,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(58, 34),
                // (68,46): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             catch when (TakeOutParam(out var x9) && x9 > 0) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(68, 46),
                // (78,34): error CS0103: The name 'y10' does not exist in the current context
                //         catch when (TakeOutParam(y10, out var x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(78, 34),
                // (99,48): error CS0128: A local variable named 'x14' is already defined in this scope
                //                           TakeOutParam(out var x14), // 2
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 48),
                // (110,48): error CS0128: A local variable named 'x15' is already defined in this scope
                //               when (Dummy(TakeOutParam(out var x15), x15))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(110, 48)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x4Decl = GetOutVarDeclaration(tree, "x4");
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAnOutLocal(model, x4Ref[0]);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetOutVarDeclaration(tree, "x6");
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVar(model, x6Decl, x6Ref);

            var x7Decl = GetOutVarDeclaration(tree, "x7");
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclaration(tree, "x8");
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForOutVar(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetOutVarDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForOutVar(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForOutVar(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAnOutLocal(model, y10Ref[1]);

            var x14Decl = GetOutVarDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForOutVar(model, x14Decl[0], x14Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = GetOutVarDeclaration(tree, "x15");
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(2, x15Ref.Length);
            VerifyModelForOutVarDuplicateInSameScope(model, x15Decl);
            VerifyNotAnOutLocal(model, x15Ref[0]);
            VerifyNotAnOutLocal(model, x15Ref[1]);
        }

        [Fact]
        public void Scope_Catch_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        try {}
        catch when (TakeOutParam(out int x1) && x1 > 0)
        {
            Dummy(x1);
        }
    }

    void Test4()
    {
        int x4 = 11;
        Dummy(x4);

        try {}
        catch when (TakeOutParam(out int x4) && x4 > 0)
        {
            Dummy(x4);
        }
    }

    void Test6()
    {
        try {}
        catch when (x6 && TakeOutParam(out int x6))
        {
            Dummy(x6);
        }
    }

    void Test7()
    {
        try {}
        catch when (TakeOutParam(out int x7) && x7 > 0)
        {
            int x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        try {}
        catch when (TakeOutParam(out int x8) && x8 > 0)
        {
            Dummy(x8);
        }

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        try {}
        catch when (TakeOutParam(out int x9) && x9 > 0)
        {   
            Dummy(x9);
            try {}
            catch when (TakeOutParam(out int x9) && x9 > 0) // 2
            {
                Dummy(x9);
            }
        }
    }

    void Test10()
    {
        try {}
        catch when (TakeOutParam(y10, out int x10))
        {   
            int y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    try {}
    //    catch when (TakeOutParam(y11, out int x11)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test14()
    {
        try {}
        catch when (Dummy(TakeOutParam(out int x14), 
                          TakeOutParam(out int x14), // 2
                          x14))
        {
            Dummy(x14);
        }
    }

    void Test15()
    {
        try {}
        catch (System.Exception x15)
              when (Dummy(TakeOutParam(out int x15), x15))
        {
            Dummy(x15);
        }
    }

    static bool TakeOutParam(out int x) 
    {
        x = 123;
        return true;
    }
    static bool TakeOutParam(object y, out int x)
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (25,42): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         catch when (TakeOutParam(out int x4) && x4 > 0)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(25, 42),
                // (34,21): error CS0841: Cannot use local variable 'x6' before it is declared
                //         catch when (x6 && TakeOutParam(out int x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(34, 21),
                // (45,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(45, 17),
                // (58,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(58, 34),
                // (68,46): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             catch when (TakeOutParam(out int x9) && x9 > 0) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(68, 46),
                // (78,34): error CS0103: The name 'y10' does not exist in the current context
                //         catch when (TakeOutParam(y10, out int x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(78, 34),
                // (99,48): error CS0128: A local variable named 'x14' is already defined in this scope
                //                           TakeOutParam(out int x14), // 2
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 48),
                // (110,48): error CS0128: A local variable named 'x15' is already defined in this scope
                //               when (Dummy(TakeOutParam(out int x15), x15))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(110, 48)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x4Decl = GetOutVarDeclaration(tree, "x4");
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAnOutLocal(model, x4Ref[0]);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetOutVarDeclaration(tree, "x6");
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVar(model, x6Decl, x6Ref);

            var x7Decl = GetOutVarDeclaration(tree, "x7");
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclaration(tree, "x8");
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForOutVar(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetOutVarDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForOutVar(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForOutVar(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAnOutLocal(model, y10Ref[1]);

            var x14Decl = GetOutVarDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForOutVar(model, x14Decl[0], x14Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = GetOutVarDeclaration(tree, "x15");
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(2, x15Ref.Length);
            VerifyModelForOutVarDuplicateInSameScope(model, x15Decl);
            VerifyNotAnOutLocal(model, x15Ref[0]);
            VerifyNotAnOutLocal(model, x15Ref[1]);
        }

        [Fact]
        public void Catch_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.Exception e) when (Dummy(TakeOutParam(e, out var x1), x1))
        {
            System.Console.WriteLine(x1.GetType());
        }
    }

    static bool Dummy(object y, object z) 
    {
        System.Console.WriteLine(z.GetType());
        return true;
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException");
        }

        [Fact]
        public void Catch_01_ExplicitType()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.Exception e) when (Dummy(TakeOutParam(e, out System.Exception x1), x1))
        {
            System.Console.WriteLine(x1.GetType());
        }
    }

    static bool Dummy(object y, object z) 
    {
        System.Console.WriteLine(z.GetType());
        return true;
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException");
        }

        [Fact]
        public void Catch_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.Exception e) when (Dummy(TakeOutParam(e, out var x1), x1))
        {
            System.Action d = () =>
                                {
                                    System.Console.WriteLine(x1.GetType());
                                };

            System.Console.WriteLine(x1.GetType());
            d();
        }
    }

    static bool Dummy(object y, object z) 
    {
        System.Console.WriteLine(z.GetType());
        return true;
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException
System.InvalidOperationException");
        }

        [Fact]
        public void Catch_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.Exception e) when (Dummy(TakeOutParam(e, out var x1), x1))
        {
            System.Action d = () =>
                                {
                                    e = new System.NullReferenceException();
                                    System.Console.WriteLine(x1.GetType());
                                };

            System.Console.WriteLine(x1.GetType());
            d();
            System.Console.WriteLine(e.GetType());
        }
    }

    static bool Dummy(object y, object z) 
    {
        System.Console.WriteLine(z.GetType());
        return true;
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException
System.InvalidOperationException
System.NullReferenceException");
        }

        [Fact]
        public void Catch_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.Exception e) when (Dummy(TakeOutParam(e, out var x1), x1))
        {
            System.Action d = () =>
                                {
                                    e = new System.NullReferenceException();
                                };

            System.Console.WriteLine(x1.GetType());
            d();
            System.Console.WriteLine(e.GetType());
        }
    }

    static bool Dummy(object y, object z) 
    {
        System.Console.WriteLine(z.GetType());
        return true;
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException
System.NullReferenceException");
        }

        [Fact]
        public void Scope_ConstructorInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    X(byte x)
        : this(TakeOutParam(3, out int x3) && x3 > 0)
    {}

    X(sbyte x)
        : this(x4 && TakeOutParam(4, out int x4))
    {}

    X(short x)
        : this(TakeOutParam(51, out int x5) && 
               TakeOutParam(52, out int x5) && 
               x5 > 0)
    {}

    X(ushort x)
        : this(TakeOutParam(6, out int x6) && x6 > 0, 
               TakeOutParam(6, out int x6) && x6 > 0) // 2
    {}
    X(int x)
        : this(TakeOutParam(7, out int x7) && x7 > 0)
    {}
    X(uint x)
        : this(x7, 2)
    {}
    void Test73() { Dummy(x7, 3); } 

    X(params object[] x) {}
    bool Dummy(params object[] x) {return true;}

    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (13,16): error CS0841: Cannot use local variable 'x4' before it is declared
                //         : this(x4 && TakeOutParam(4, out int x4))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(13, 16),
                // (18,41): error CS0128: A local variable named 'x5' is already defined in this scope
                //                TakeOutParam(52, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(18, 41),
                // (24,40): error CS0128: A local variable named 'x6' is already defined in this scope
                //                TakeOutParam(6, out int x6) && x6 > 0) // 2
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(24, 40),
                // (30,16): error CS0103: The name 'x7' does not exist in the current context
                //         : this(x7, 2)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(30, 16),
                // (32,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(32, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForOutVar(model, x4Decl, x4Ref);

            var x5Decl = GetOutVarDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForOutVar(model, x5Decl[0], x5Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVar(model, x6Decl[0], x6Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = GetOutVarDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void Scope_ConstructorInitializers_02()
        {
            var source =
@"
public class X : Y
{
    public static void Main()
    {
    }

    X(byte x)
        : base(TakeOutParam(3, out int x3) && x3 > 0)
    {}

    X(sbyte x)
        : base(x4 && TakeOutParam(4, out int x4))
    {}

    X(short x)
        : base(TakeOutParam(51, out int x5) && 
               TakeOutParam(52, out int x5) && 
               x5 > 0)
    {}

    X(ushort x)
        : base(TakeOutParam(6, out int x6) && x6 > 0, 
               TakeOutParam(6, out int x6) && x6 > 0) // 2
    {}
    X(int x)
        : base(TakeOutParam(7, out int x7) && x7 > 0)
    {}
    X(uint x)
        : base(x7, 2)
    {}
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}

    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}

public class Y
{
    public Y(params object[] x) {}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (13,16): error CS0841: Cannot use local variable 'x4' before it is declared
                //         : base(x4 && TakeOutParam(4, out int x4))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(13, 16),
                // (18,41): error CS0128: A local variable named 'x5' is already defined in this scope
                //                TakeOutParam(52, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(18, 41),
                // (24,40): error CS0128: A local variable named 'x6' is already defined in this scope
                //                TakeOutParam(6, out int x6) && x6 > 0) // 2
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(24, 40),
                // (30,16): error CS0103: The name 'x7' does not exist in the current context
                //         : base(x7, 2)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(30, 16),
                // (32,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(32, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForOutVar(model, x4Decl, x4Ref);

            var x5Decl = GetOutVarDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForOutVar(model, x5Decl[0], x5Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVar(model, x6Decl[0], x6Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = GetOutVarDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void Scope_ConstructorInitializers_03()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        new D(1);
        new D(10);
        new D(1.2);
    }
}
class D
{
    public D(object o) : this(TakeOutParam(o, out int x) && x >= 5) 
    {
        Console.WriteLine(x);
    }

    public D(bool b) { Console.WriteLine(b); }

    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
    // (15,27): error CS0103: The name 'x' does not exist in the current context
    //         Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(15, 27)
                );
        }

        [Fact]
        public void Scope_ConstructorInitializers_04()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        new D(1);
        new D(10);
        new D(1.2);
    }
}
class D : C
{
    public D(object o) : base(TakeOutParam(o, out int x) && x >= 5) 
    {
        Console.WriteLine(x);
    }

    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}

class C
{
    public C(bool b) { Console.WriteLine(b); }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
    // (15,27): error CS0103: The name 'x' does not exist in the current context
    //         Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(15, 27)
                );
        }

        [Fact]
        public void Scope_Do_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        do
        {
            Dummy(x1);
        }
        while (TakeOutParam(true, out var x1) && x1);
    }

    void Test2()
    {
        do
            Dummy(x2);
        while (TakeOutParam(true, out var x2) && x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        do
            Dummy(x4);
        while (TakeOutParam(true, out var x4) && x4);
    }

    void Test6()
    {
        do
            Dummy(x6);
        while (x6 && TakeOutParam(true, out var x6));
    }

    void Test7()
    {
        do
        {
            var x7 = 12;
            Dummy(x7);
        }
        while (TakeOutParam(true, out var x7) && x7);
    }

    void Test8()
    {
        do
            Dummy(x8);
        while (TakeOutParam(true, out var x8) && x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        do
        {   
            Dummy(x9);
            do
                Dummy(x9);
            while (TakeOutParam(true, out var x9) && x9); // 2
        }
        while (TakeOutParam(true, out var x9) && x9);
    }

    void Test10()
    {
        do
        {   
            var y10 = 12;
            Dummy(y10);
        }
        while (TakeOutParam(y10, out var x10));
    }

    //void Test11()
    //{
    //    do
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //    while (TakeOutParam(y11, out var x11));
    //}

    void Test12()
    {
        do
            var y12 = 12;
        while (TakeOutParam(y12, out var x12));
    }

    //void Test13()
    //{
    //    do
    //        let y13 = 12;
    //    while (TakeOutParam(y13, out var x13));
    //}

    void Test14()
    {
        do
        {
            Dummy(x14);
        }
        while (Dummy(TakeOutParam(1, out var x14), 
                     TakeOutParam(2, out var x14), 
                     x14));
    }

    static bool TakeOutParam(object y, out bool x) 
    {
        x = true;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (97,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(97, 13),
                // (14,19): error CS0841: Cannot use local variable 'x1' before it is declared
                //             Dummy(x1);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(14, 19),
                // (22,19): error CS0841: Cannot use local variable 'x2' before it is declared
                //             Dummy(x2);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(22, 19),
                // (33,43): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         while (TakeOutParam(true, out var x4) && x4);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(33, 43),
                // (32,19): error CS0841: Cannot use local variable 'x4' before it is declared
                //             Dummy(x4);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(32, 19),
                // (40,16): error CS0841: Cannot use local variable 'x6' before it is declared
                //         while (x6 && TakeOutParam(true, out var x6));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(40, 16),
                // (39,19): error CS0841: Cannot use local variable 'x6' before it is declared
                //             Dummy(x6);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(39, 19),
                // (47,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(47, 17),
                // (56,19): error CS0841: Cannot use local variable 'x8' before it is declared
                //             Dummy(x8);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x8").WithArguments("x8").WithLocation(56, 19),
                // (59,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(59, 34),
                // (66,19): error CS0841: Cannot use local variable 'x9' before it is declared
                //             Dummy(x9);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(66, 19),
                // (69,47): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             while (TakeOutParam(true, out var x9) && x9); // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(69, 47),
                // (68,23): error CS0841: Cannot use local variable 'x9' before it is declared
                //                 Dummy(x9);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(68, 23),
                // (81,29): error CS0103: The name 'y10' does not exist in the current context
                //         while (TakeOutParam(y10, out var x10));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(81, 29),
                // (98,29): error CS0103: The name 'y12' does not exist in the current context
                //         while (TakeOutParam(y12, out var x12));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(98, 29),
                // (115,46): error CS0128: A local variable named 'x14' is already defined in this scope
                //                      TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(115, 46),
                // (112,19): error CS0841: Cannot use local variable 'x14' before it is declared
                //             Dummy(x14);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(112, 19)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForOutVar(model, x2Decl, x2Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAnOutLocal(model, x4Ref[0]);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVar(model, x6Decl, x6Ref);

            var x7Decl = GetOutVarDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForOutVar(model, x7Decl, x7Ref[1]);
            VerifyNotAnOutLocal(model, x7Ref[0]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForOutVar(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetOutVarDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForOutVar(model, x9Decl[1], x9Ref[0], x9Ref[3]);
            VerifyModelForOutVar(model, x9Decl[0], x9Ref[1], x9Ref[2]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[1]);
            VerifyNotAnOutLocal(model, y10Ref[0]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetOutVarDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForOutVar(model, x14Decl[0], x14Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void Do_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f;

        do
        {
            f = false;
        }
        while (Dummy(f, TakeOutParam((f ? 1 : 2), out var x1), x1));
    }

    static bool Dummy(bool x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            CompileAndVerify(compilation, expectedOutput: @"2");
        }

        [Fact]
        public void Do_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f;

        do
        {
            f = false;
        }
        while (Dummy(f, TakeOutParam((f ? 1 : 2), out int x1), x1));
    }

    static bool Dummy(bool x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            CompileAndVerify(compilation, expectedOutput: @"2");
        }

        [Fact]
        public void Scope_ExpressionBodiedFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Test3(object o) => TakeOutParam(o, out int x3) && x3 > 0;

    bool Test4(object o) => x4 && TakeOutParam(o, out int x4);

    bool Test5(object o1, object o2) => TakeOutParam(o1, out int x5) && 
                                         TakeOutParam(o2, out int x5) && 
                                         x5 > 0;

    bool Test61 (object o) => TakeOutParam(o, out int x6) && x6 > 0; bool Test62 (object o) => TakeOutParam(o, out int x6) && x6 > 0;

    bool Test71(object o) => TakeOutParam(o, out int x7) && x7 > 0; 
    void Test72() => Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool Test11(object x11) => TakeOutParam(1, out int x11) && 
                             x11 > 0;

    bool Dummy(params object[] x) {return true;}

    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            compilation.VerifyDiagnostics(
                // (10,29): error CS0841: Cannot use local variable 'x4' before it is declared
                //     bool Test4(object o) => x4 && TakeOutParam(o, out int x4);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 29),
                // (13,67): error CS0128: A local variable named 'x5' is already defined in this scope
                //                                          TakeOutParam(o2, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 67),
                // (19,28): error CS0103: The name 'x7' does not exist in the current context
                //     void Test72() => Dummy(x7, 2); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 28),
                // (20,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27),
                // (22,56): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     bool Test11(object x11) => TakeOutParam(1, out int x11) && 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(22, 56)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForOutVar(model, x4Decl, x4Ref);

            var x5Decl = GetOutVarDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForOutVar(model, x5Decl[0], x5Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVar(model, x6Decl[0], x6Ref[0]);
            VerifyModelForOutVar(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetOutVarDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);

            var x11Decl = GetOutVarDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").Single();
            VerifyModelForOutVar(model, x11Decl, x11Ref);
        }

        [Fact]
        public void ExpressionBodiedFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static bool Test1() => TakeOutParam(1, out int x1) && Dummy(x1); 

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void ExpressionBodiedFunctions_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static bool Test1() => TakeOutParam(1, out var x1) && Dummy(x1); 

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature());
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void Scope_ExpressionBodiedLocalFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test3()
    {
        bool f (object o) => TakeOutParam(o, out int x3) && x3 > 0;
        f(null);
    }

    void Test4()
    {
        bool f (object o) => x4 && TakeOutParam(o, out int x4);
        f(null);
    }

    void Test5()
    {
        bool f (object o1, object o2) => TakeOutParam(o1, out int x5) && 
                                         TakeOutParam(o2, out int x5) && 
                                         x5 > 0;
        f(null, null);
    }

    void Test6()
    {
        bool f1 (object o) => TakeOutParam(o, out int x6) && x6 > 0; bool f2 (object o) => TakeOutParam(o, out int x6) && x6 > 0;
        f1(null);
        f2(null);
    }

    void Test7()
    {
        Dummy(x7, 1);
         
        bool f (object o) => TakeOutParam(o, out int x7) && x7 > 0; 

        Dummy(x7, 2); 
        f(null);
    }

    void Test11()
    {
        var x11 = 11;
        Dummy(x11);
        bool f (object o) => TakeOutParam(o, out int x11) && 
                             x11 > 0;
        f(null);
    }

    void Test12()
    {
        bool f (object o) => TakeOutParam(o, out int x12) && 
                             x12 > 0;
        var x12 = 11;
        Dummy(x12);
        f(null);
    }

    System.Action Test13()
    {
        return () =>
                    {
                        bool f (object o) => TakeOutParam(o, out int x13) && x13 > 0;
                        f(null);
                    };
    }

    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature().WithLocalFunctionsFeature());
            compilation.VerifyDiagnostics(
                // (18,30): error CS0841: Cannot use local variable 'x4' before it is declared
                //         bool f (object o) => x4 && TakeOutParam(o, out int x4);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(18, 30),
                // (25,67): error CS0128: A local variable named 'x5' is already defined in this scope
                //                                          TakeOutParam(o2, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(25, 67),
                // (39,15): error CS0103: The name 'x7' does not exist in the current context
                //         Dummy(x7, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(39, 15),
                // (43,15): error CS0103: The name 'x7' does not exist in the current context
                //         Dummy(x7, 2); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(43, 15),
                // (51,54): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         bool f (object o) => TakeOutParam(o, out int x11) && 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(51, 54),
                // (58,54): error CS0136: A local or parameter named 'x12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         bool f (object o) => TakeOutParam(o, out int x12) && 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x12").WithArguments("x12").WithLocation(58, 54)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForOutVar(model, x4Decl, x4Ref);

            var x5Decl = GetOutVarDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForOutVar(model, x5Decl[0], x5Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVar(model, x6Decl[0], x6Ref[0]);
            VerifyModelForOutVar(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetOutVarDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyNotInScope(model, x7Ref[0]);
            VerifyModelForOutVar(model, x7Decl, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);

            var x11Decl = GetOutVarDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotAnOutLocal(model, x11Ref[0]);
            VerifyModelForOutVar(model, x11Decl, x11Ref[1]);

            var x12Decl = GetOutVarDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForOutVar(model, x12Decl, x12Ref[0]);
            VerifyNotAnOutLocal(model, x12Ref[1]);

            var x13Decl = GetOutVarDeclarations(tree, "x13").Single();
            var x13Ref = GetReferences(tree, "x13").Single();
            VerifyModelForOutVar(model, x13Decl, x13Ref);
        }

        [Fact]
        public void ExpressionBodiedLocalFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static bool Test1()
    {
        bool f() =>  TakeOutParam(1, out int x1) && Dummy(x1); 
        return f();
    }

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature().WithLocalFunctionsFeature());
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void ExpressionBodiedLocalFunctions_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static bool Test1()
    {
        bool f() =>  TakeOutParam(1, out var x1) && Dummy(x1); 
        return f();
    }

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithOutVarFeature().WithLocalFunctionsFeature());
            CompileAndVerify(compilation, expectedOutput: @"1
True");
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
            VerifyModelForOutVar(model, x1Decl, true, true, x1Ref);

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
        Do(new Test2());
    }

    static void Do(object x){}

    public static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    class Test2
    {
        Test2(object x, object y)
        {
            System.Console.WriteLine(y);
        }

        public Test2()
        : this(Test1(out var x1), x1)
        {}
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
        public void SimpleVar_11()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Do(new Test3());
    }

    static void Do(object x){}

    public static object Test1(out int x)
    {
        x = 123;
        return null;
    }

    class Test2
    {
        public Test2(object x, object y)
        {
            System.Console.WriteLine(y);
        }
    }
    
    class Test3 : Test2
    {

        public Test3()
        : base(Test1(out var x1), x1)
        {}
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

            CompileAndVerify(compilation).VerifyDiagnostics();

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

            CompileAndVerify(compilation).VerifyDiagnostics();

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

        [Fact]
        public void SyntaxModel_Factory_01()
        {
            var declarator = SyntaxFactory.VariableDeclarator("a");
            VerifyArgumentException(() => SyntaxFactory.Argument((CSharpSyntaxNode)declarator), "expressionOrDeclaration");

            var identifierName = SyntaxFactory.IdentifierName("type");
            var argument = SyntaxFactory.Argument((CSharpSyntaxNode)identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var declaration = SyntaxFactory.VariableDeclaration(identifierName,
                                                                SyntaxFactory.SeparatedList(new[] { declarator }));

            // The parameter name isn't quite right, but it is fine since this factory is internal.
            // The is chosen to work well with public factories.
            VerifyArgumentException(() => SyntaxFactory.Argument((CSharpSyntaxNode)declaration), "outKeyword");

            var invalidDeclaration1 = SyntaxFactory.VariableDeclaration(identifierName);
            var invalidDeclaration2 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] { declarator, declarator }));
            var invalidDeclaration3 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, SyntaxFactory.BracketedArgumentList(), null) }));

            var invalidDeclaration4 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, null, SyntaxFactory.EqualsValueClause(identifierName)) }));

            VerifyArgumentException(() => SyntaxFactory.Argument((CSharpSyntaxNode)invalidDeclaration1), "outKeyword");
            VerifyArgumentException(() => SyntaxFactory.Argument((CSharpSyntaxNode)invalidDeclaration2), "outKeyword");
            VerifyArgumentException(() => SyntaxFactory.Argument((CSharpSyntaxNode)invalidDeclaration3), "outKeyword");
            VerifyArgumentException(() => SyntaxFactory.Argument((CSharpSyntaxNode)invalidDeclaration4), "outKeyword");
        }

        [Fact]
        public void SyntaxModel_Factory_02()
        {
            var declarator = SyntaxFactory.VariableDeclarator("a");
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), default(SyntaxToken), declarator), "expressionOrDeclaration");

            var identifierName = SyntaxFactory.IdentifierName("type");
            var argument = SyntaxFactory.Argument(default(NameColonSyntax), default(SyntaxToken), (CSharpSyntaxNode)identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            argument = SyntaxFactory.Argument(default(NameColonSyntax), refKeyword, (CSharpSyntaxNode)identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
            argument = SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var declaration = SyntaxFactory.VariableDeclaration(identifierName,
                                                                SyntaxFactory.SeparatedList(new[] { declarator }));

            // The parameter name isn't quite right, but it is fine since this factory is internal.
            // The is chosen to work well with public factories.
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), default(SyntaxToken), (CSharpSyntaxNode)declaration), "outKeyword");
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), refKeyword, (CSharpSyntaxNode)declaration), "outKeyword");

            argument = SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)declaration);
            Assert.Equal(declaration.ToString(), argument.Declaration.ToString());
            Assert.Null(argument.Expression);

            var invalidDeclaration1 = SyntaxFactory.VariableDeclaration(identifierName);
            var invalidDeclaration2 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] { declarator, declarator }));
            var invalidDeclaration3 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, SyntaxFactory.BracketedArgumentList(), null) }));

            var invalidDeclaration4 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, null, SyntaxFactory.EqualsValueClause(identifierName)) }));

            // The parameter name isn't quite right, but it is fine since this factory is internal.
            // The is chosen to work well with public factories.
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)invalidDeclaration1), "declaration");
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)invalidDeclaration2), "declaration");
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)invalidDeclaration3), "declaration");
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)invalidDeclaration4), "declaration");
        }

        [Fact]
        public void SyntaxModel_Factory_03()
        {
            var identifierName = SyntaxFactory.IdentifierName("type");
            var argument = SyntaxFactory.Argument(identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);
        }
        
        [Fact]
        public void SyntaxModel_Factory_04()
        {
            var identifierName = SyntaxFactory.IdentifierName("type");
            var argument = SyntaxFactory.Argument(default(NameColonSyntax), default(SyntaxToken), identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            argument = SyntaxFactory.Argument(default(NameColonSyntax), refKeyword, identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
            argument = SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);
        }

        [Fact]
        public void SyntaxModel_Factory_05()
        {
            var declarator = SyntaxFactory.VariableDeclarator("a");
            var identifierName = SyntaxFactory.IdentifierName("type");
            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
            var declaration = SyntaxFactory.VariableDeclaration(identifierName,
                                                                SyntaxFactory.SeparatedList(new[] { declarator }));

            VerifyArgumentException(() => SyntaxFactory.Argument(default(SyntaxToken), declaration), "outKeyword");
            VerifyArgumentException(() => SyntaxFactory.Argument(refKeyword, declaration), "outKeyword");

            var argument = SyntaxFactory.Argument(outKeyword, declaration);
            Assert.Equal(declaration.ToString(), argument.Declaration.ToString());
            Assert.Null(argument.Expression);

            var invalidDeclaration1 = SyntaxFactory.VariableDeclaration(identifierName);
            var invalidDeclaration2 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] { declarator, declarator }));
            var invalidDeclaration3 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, SyntaxFactory.BracketedArgumentList(), null) }));

            var invalidDeclaration4 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, null, SyntaxFactory.EqualsValueClause(identifierName)) }));

            // The parameter name isn't quite right, but it is fine since this factory is internal.
            // The is chosen to work well with public factories.
            VerifyArgumentException(() => SyntaxFactory.Argument(outKeyword, invalidDeclaration1), "declaration");
            VerifyArgumentException(() => SyntaxFactory.Argument(outKeyword, invalidDeclaration2), "declaration");
            VerifyArgumentException(() => SyntaxFactory.Argument(outKeyword, invalidDeclaration3), "declaration");
            VerifyArgumentException(() => SyntaxFactory.Argument(outKeyword, invalidDeclaration4), "declaration");
        }

        [Fact]
        public void SyntaxModel_Factory_06()
        {
            var declarator = SyntaxFactory.VariableDeclarator("a");
            var identifierName = SyntaxFactory.IdentifierName("type");
            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
            var declaration = SyntaxFactory.VariableDeclaration(identifierName,
                                                                SyntaxFactory.SeparatedList(new[] { declarator }));

            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), default(SyntaxToken), declaration), "outKeyword");
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), refKeyword, declaration), "outKeyword");

            var argument = SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, declaration);
            Assert.Equal(declaration.ToString(), argument.Declaration.ToString());
            Assert.Null(argument.Expression);

            var invalidDeclaration1 = SyntaxFactory.VariableDeclaration(identifierName);
            var invalidDeclaration2 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] { declarator, declarator }));
            var invalidDeclaration3 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, SyntaxFactory.BracketedArgumentList(), null) }));

            var invalidDeclaration4 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, null, SyntaxFactory.EqualsValueClause(identifierName)) }));

            // The parameter name isn't quite right, but it is fine since this factory is internal.
            // The is chosen to work well with public factories.
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, invalidDeclaration1), "declaration");
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, invalidDeclaration2), "declaration");
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, invalidDeclaration3), "declaration");
            VerifyArgumentException(() => SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, invalidDeclaration4), "declaration");
        }

        [Fact]
        public void SyntaxModel_Update_01()
        {
            var initial = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("a"));

            var declarator = SyntaxFactory.VariableDeclarator("a");
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), default(SyntaxToken), declarator), "expressionOrDeclaration");

            var identifierName = SyntaxFactory.IdentifierName("type");
            var argument = initial.Update(default(NameColonSyntax), default(SyntaxToken), (CSharpSyntaxNode)identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            argument = initial.Update(default(NameColonSyntax), refKeyword, (CSharpSyntaxNode)identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
            argument = initial.Update(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var declaration = SyntaxFactory.VariableDeclaration(identifierName,
                                                                SyntaxFactory.SeparatedList(new[] { declarator }));

            // The parameter name isn't quite right, but it is fine since this method is internal.
            // The is chosen to work well with public factories.
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), default(SyntaxToken), (CSharpSyntaxNode)declaration), "outKeyword");
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), refKeyword, (CSharpSyntaxNode)declaration), "outKeyword");

            argument = initial.Update(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)declaration);
            Assert.Equal(declaration.ToString(), argument.Declaration.ToString());
            Assert.Null(argument.Expression);

            var invalidDeclaration1 = SyntaxFactory.VariableDeclaration(identifierName);
            var invalidDeclaration2 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] { declarator, declarator }));
            var invalidDeclaration3 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, SyntaxFactory.BracketedArgumentList(), null) }));

            var invalidDeclaration4 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, null, SyntaxFactory.EqualsValueClause(identifierName)) }));

            // The parameter name isn't quite right, but it is fine since this method is internal.
            // The is chosen to work well with public factories.
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)invalidDeclaration1), "declaration");
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)invalidDeclaration2), "declaration");
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)invalidDeclaration3), "declaration");
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), outKeyword, (CSharpSyntaxNode)invalidDeclaration4), "declaration");
        }

        [Fact]
        public void SyntaxModel_Update_02()
        {
            var initial = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("a"));

            var identifierName = SyntaxFactory.IdentifierName("type");
            var argument = initial.Update(default(NameColonSyntax), default(SyntaxToken), identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            argument = initial.Update(default(NameColonSyntax), refKeyword, identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
            argument = initial.Update(default(NameColonSyntax), outKeyword, identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);
        }

        [Fact]
        public void SyntaxModel_Update_03()
        {
            var initial = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("a"));

            var declarator = SyntaxFactory.VariableDeclarator("a");
            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);

            var identifierName = SyntaxFactory.IdentifierName("type");
            var declaration = SyntaxFactory.VariableDeclaration(identifierName,
                                                                SyntaxFactory.SeparatedList(new[] { declarator }));

            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), default(SyntaxToken), declaration), "outKeyword");
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), refKeyword, declaration), "outKeyword");

            var argument = initial.Update(default(NameColonSyntax), outKeyword, declaration);
            Assert.Equal(declaration.ToString(), argument.Declaration.ToString());
            Assert.Null(argument.Expression);

            var invalidDeclaration1 = SyntaxFactory.VariableDeclaration(identifierName);
            var invalidDeclaration2 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] { declarator, declarator }));
            var invalidDeclaration3 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, SyntaxFactory.BracketedArgumentList(), null) }));

            var invalidDeclaration4 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, null, SyntaxFactory.EqualsValueClause(identifierName)) }));

            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), outKeyword, invalidDeclaration1), "declaration");
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), outKeyword, invalidDeclaration2), "declaration");
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), outKeyword, invalidDeclaration3), "declaration");
            VerifyArgumentException(() => initial.Update(default(NameColonSyntax), outKeyword, invalidDeclaration4), "declaration");
        }

        [Fact]
        public void SyntaxModel_WithExpressionOrDeclaration()
        {
            var initial = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("a"));

            var declarator = SyntaxFactory.VariableDeclarator("a");
            VerifyArgumentException(() => initial.WithExpressionOrDeclaration(declarator), "expressionOrDeclaration");

            var identifierName = SyntaxFactory.IdentifierName("type");
            var argument = initial.WithExpressionOrDeclaration((CSharpSyntaxNode)identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            var initialRef = SyntaxFactory.Argument(default(NameColonSyntax), refKeyword, SyntaxFactory.IdentifierName("a"));
            argument = initialRef.WithExpressionOrDeclaration((CSharpSyntaxNode)identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
            var initialOut = SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, SyntaxFactory.IdentifierName("a"));
            argument = initialOut.WithExpressionOrDeclaration((CSharpSyntaxNode)identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var declaration = SyntaxFactory.VariableDeclaration(identifierName,
                                                                SyntaxFactory.SeparatedList(new[] { declarator }));

            // The parameter name isn't quite right, but it is fine since this method is internal.
            // The is chosen to work well with public factories.
            VerifyArgumentException(() => initial.WithExpressionOrDeclaration((CSharpSyntaxNode)declaration), "outKeyword");
            VerifyArgumentException(() => initialRef.WithExpressionOrDeclaration((CSharpSyntaxNode)declaration), "outKeyword");

            argument = initialOut.WithExpressionOrDeclaration((CSharpSyntaxNode)declaration);
            Assert.Equal(declaration.ToString(), argument.Declaration.ToString());
            Assert.Null(argument.Expression);

            var invalidDeclaration1 = SyntaxFactory.VariableDeclaration(identifierName);
            var invalidDeclaration2 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] { declarator, declarator }));
            var invalidDeclaration3 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, SyntaxFactory.BracketedArgumentList(), null) }));

            var invalidDeclaration4 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, null, SyntaxFactory.EqualsValueClause(identifierName)) }));

            // The parameter name isn't quite right, but it is fine since this method is internal.
            // The is chosen to work well with public factories.
            VerifyArgumentException(() => initialOut.WithExpressionOrDeclaration((CSharpSyntaxNode)invalidDeclaration1), "declaration");
            VerifyArgumentException(() => initialOut.WithExpressionOrDeclaration((CSharpSyntaxNode)invalidDeclaration2), "declaration");
            VerifyArgumentException(() => initialOut.WithExpressionOrDeclaration((CSharpSyntaxNode)invalidDeclaration3), "declaration");
            VerifyArgumentException(() => initialOut.WithExpressionOrDeclaration((CSharpSyntaxNode)invalidDeclaration4), "declaration");
        }

        [Fact]
        public void SyntaxModel_WithExpression()
        {
            var initial = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("a"));

            var identifierName = SyntaxFactory.IdentifierName("type");
            var argument = initial.WithExpression(identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            var initialRef = SyntaxFactory.Argument(default(NameColonSyntax), refKeyword, identifierName);
            argument = initialRef.WithExpression(identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);

            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
            var initialOut = SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, identifierName);
            argument = initialOut.WithExpression(identifierName);
            Assert.Equal(identifierName.ToString(), argument.Expression.ToString());
            Assert.Null(argument.Declaration);
        }

        [Fact]
        public void SyntaxModel_WithrDeclaration()
        {
            var initial = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("a"));

            var declarator = SyntaxFactory.VariableDeclarator("a");
            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            var initialRef = SyntaxFactory.Argument(default(NameColonSyntax), refKeyword, SyntaxFactory.IdentifierName("a"));
            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
            var initialOut = SyntaxFactory.Argument(default(NameColonSyntax), outKeyword, SyntaxFactory.IdentifierName("a"));

            var identifierName = SyntaxFactory.IdentifierName("type");
            var declaration = SyntaxFactory.VariableDeclaration(identifierName,
                                                                SyntaxFactory.SeparatedList(new[] { declarator }));

            Assert.Throws<InvalidOperationException>(() => initial.WithDeclaration(declaration));
            Assert.Throws<InvalidOperationException>(() => initialRef.WithDeclaration(declaration));

            var argument = initialOut.WithDeclaration(declaration);
            Assert.Equal(declaration.ToString(), argument.Declaration.ToString());
            Assert.Null(argument.Expression);

            var invalidDeclaration1 = SyntaxFactory.VariableDeclaration(identifierName);
            var invalidDeclaration2 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] { declarator, declarator }));
            var invalidDeclaration3 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, SyntaxFactory.BracketedArgumentList(), null) }));

            var invalidDeclaration4 = SyntaxFactory.VariableDeclaration(identifierName,
                                                                        SyntaxFactory.SeparatedList(new[] {
                                                                            SyntaxFactory.VariableDeclarator(declarator.Identifier, null, SyntaxFactory.EqualsValueClause(identifierName)) }));

            VerifyArgumentException(() => initialOut.WithDeclaration(invalidDeclaration1), "declaration");
            VerifyArgumentException(() => initialOut.WithDeclaration(invalidDeclaration2), "declaration");
            VerifyArgumentException(() => initialOut.WithDeclaration(invalidDeclaration3), "declaration");
            VerifyArgumentException(() => initialOut.WithDeclaration(invalidDeclaration4), "declaration");
        }

        [Fact]
        public void SyntaxModel_WithRefOrOutKeyword()
        {
            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            var outKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
            var initial = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("a"));

            var argument = initial.WithRefOrOutKeyword(refKeyword);
            Assert.Equal(SyntaxKind.RefKeyword, argument.RefOrOutKeyword.Kind());

            argument = initial.WithRefOrOutKeyword(outKeyword);
            Assert.Equal(SyntaxKind.OutKeyword, argument.RefOrOutKeyword.Kind());

            argument = argument.WithRefOrOutKeyword(default(SyntaxToken));
            Assert.Equal(SyntaxKind.None, argument.RefOrOutKeyword.Kind());

            var declarator = SyntaxFactory.VariableDeclarator("a");
            var identifierName = SyntaxFactory.IdentifierName("type");
            var declaration = SyntaxFactory.VariableDeclaration(identifierName,
                                                                SyntaxFactory.SeparatedList(new[] { declarator }));

            initial = initial.WithRefOrOutKeyword(outKeyword).WithDeclaration(declaration);
            VerifyArgumentException(() => initial.WithRefOrOutKeyword(default(SyntaxToken)), "refOrOutKeyword");
            VerifyArgumentException(() => initial.WithRefOrOutKeyword(refKeyword), "refOrOutKeyword");

            argument = initial.WithRefOrOutKeyword(outKeyword);
            Assert.Equal(SyntaxKind.OutKeyword, argument.RefOrOutKeyword.Kind());
        }

        private static void VerifyArgumentException(System.Action testCode, string paramName)
        {
            try
            {
                testCode();
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(paramName, ex.Message);
                return;
            }

            Assert.False(true, "Expected exception is not thrown.");
        }
    }
}
