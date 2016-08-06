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
                // (6,29): error CS8059: Feature 'out variable declaration' is not available in C# 6.  Please use language version 7 or greater.
                //         Test2(Test1(out int x1), x1);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "x1").WithArguments("out variable declaration", "7").WithLocation(6, 29)
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

        private static DeclarationExpressionSyntax GetOutVarDeclaration(SyntaxTree tree, string name)
        {
            return GetOutVarDeclarations(tree, name).Single();
        }

        private static IEnumerable<DeclarationExpressionSyntax> GetOutVarDeclarations(SyntaxTree tree, string name)
        {
            return tree.GetRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().Where(p => p.Identifier().ValueText == name);
        }

        private static IEnumerable<DeclarationExpressionSyntax> GetOutVarDeclarations(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>();
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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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

        private static void VerifyModelForOutVar(SemanticModel model, DeclarationExpressionSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForOutVar(model, decl, false, true, references);
        }

        private static void VerifyModelForOutVarInNotExecutableCode(SemanticModel model, DeclarationExpressionSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForOutVar(model, decl, false, false, references);
        }

        private static void VerifyModelForOutVar(SemanticModel model, DeclarationExpressionSyntax decl, bool isDelegateCreation, bool isExecutableCode, params IdentifierNameSyntax[] references)
        {
            var variableDeclaratorSyntax = GetVariableDeclarator(decl);
            var symbol = model.GetDeclaredSymbol(variableDeclaratorSyntax);
            Assert.Equal(decl.Identifier().ValueText, symbol.Name);
            Assert.Equal(LocalDeclarationKind.RegularVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)variableDeclaratorSyntax));
            Assert.Same(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier().ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier().ValueText));

            var local = (SourceLocalSymbol)symbol;

            if (decl.Type().IsVar && local.IsVar && local.Type.IsErrorType())
            {
                Assert.Null(model.GetSymbolInfo(decl.Type()).Symbol);
            }
            else
            {
                Assert.Equal(local.Type, model.GetSymbolInfo(decl.Type()).Symbol);
            }

            foreach (var reference in references)
            {
                Assert.Same(symbol, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: decl.Identifier().ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(decl.Identifier().ValueText));
                Assert.Equal(local.Type, model.GetTypeInfo(reference).Type);
            }

            VerifyDataFlow(model, decl, isDelegateCreation, isExecutableCode, references, symbol);
        }

        private static void VerifyDataFlow(SemanticModel model, DeclarationExpressionSyntax decl, bool isDelegateCreation, bool isExecutableCode, IdentifierNameSyntax[] references, ISymbol symbol)
        {
            var dataFlowParent = decl.Parent.Parent.Parent as ExpressionSyntax;

            if (dataFlowParent == null)
            {
                Assert.IsAssignableFrom<ConstructorInitializerSyntax>(decl.Parent.Parent.Parent);
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

        private static void VerifyModelForOutVarDuplicateInSameScope(SemanticModel model, DeclarationExpressionSyntax decl)
        {
            var variableDeclaratorSyntax = GetVariableDeclarator(decl);
            var symbol = model.GetDeclaredSymbol(variableDeclaratorSyntax);
            Assert.Equal(decl.Identifier().ValueText, symbol.Name);
            Assert.Equal(LocalDeclarationKind.RegularVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)variableDeclaratorSyntax));
            Assert.NotEqual(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier().ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier().ValueText));

            var local = (SourceLocalSymbol)symbol;

            if (decl.Type().IsVar && local.IsVar && local.Type.IsErrorType())
            {
                Assert.Null(model.GetSymbolInfo(decl.Type()).Symbol);
            }
            else
            {
                Assert.Equal(local.Type, model.GetSymbolInfo(decl.Type()).Symbol);
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
                    var parent1 = ((VariableDeclarationSyntax)((VariableDeclaratorSyntax)parent).Parent).Parent;
                    switch (parent1.Kind())
                    {
                        case SyntaxKind.FixedStatement:
                        case SyntaxKind.ForStatement:
                        case SyntaxKind.UsingStatement:
                            break;
                        default:
                            Assert.Equal(SyntaxKind.LocalDeclarationStatement, parent1.Kind());
                            break;
                    }
                }
            }

            Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(reference.SpanStart).Contains(reference.Identifier.ValueText));
        }

        private static VariableDeclaratorSyntax GetVariableDeclarator(DeclarationExpressionSyntax decl)
        {
            return decl.Declaration.Variables.Single();
        }

        private static bool FlowsIn(ExpressionSyntax dataFlowParent, DeclarationExpressionSyntax decl, IdentifierNameSyntax[] references)
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

        private static bool FlowsOut(ExpressionSyntax dataFlowParent, DeclarationExpressionSyntax decl, IdentifierNameSyntax[] references)
        {
            ForStatementSyntax forStatement;

            if ((forStatement = decl.Ancestors().OfType<ForStatementSyntax>().FirstOrDefault()) != null &&
                 forStatement.Incrementors.Span.Contains(decl.Position) &&
                 forStatement.Statement.DescendantNodes().OfType<ForStatementSyntax>().Any(f => f.Condition == null))
            {
                return false;
            }

            var containingStatement = decl.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
            var containingReturnOrThrow = containingStatement as ReturnStatementSyntax ?? (StatementSyntax)(containingStatement as ThrowStatementSyntax);

            MethodDeclarationSyntax methodDeclParent;

            if (containingReturnOrThrow != null && decl.Identifier().ValueText == "x1" && 
                ((methodDeclParent = containingReturnOrThrow.Parent.Parent as MethodDeclarationSyntax) == null ||
                  methodDeclParent.Body.Statements.First() != containingReturnOrThrow))
            {
                return false;
            }

            foreach (var reference in references)
            {
                if (!dataFlowParent.Span.Contains(reference.Span) && 
                    (containingReturnOrThrow == null || containingReturnOrThrow.Span.Contains(reference.SpanStart)) &&
                    (reference.SpanStart > decl.SpanStart || 
                     (containingReturnOrThrow == null &&
                     reference.Ancestors().OfType<DoStatementSyntax>().Join(
                         decl.Ancestors().OfType<DoStatementSyntax>(), d => d, d => d, (d1, d2) => true).Any())))
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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
" + TestResources.NetFX.ValueTuple.tupleattributes_cs;
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void Scope_ExpressionBodiedProperties_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Test3 => TakeOutParam(3, out int x3) && x3 > 0;

    bool Test4 => x4 && TakeOutParam(4, out int x4);

    bool Test5 => TakeOutParam(51, out int x5) && 
                  TakeOutParam(52, out int x5) && 
                  x5 > 0;

    bool Test61 => TakeOutParam(6, out int x6) && x6 > 0; bool Test62 => TakeOutParam(6, out int x6) && x6 > 0;

    bool Test71 => TakeOutParam(7, out int x7) && x7 > 0; 
    bool Test72 => Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool this[object x11] => TakeOutParam(1, out int x11) && 
                             x11 > 0;

    bool Dummy(params object[] x) {return true;}

    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (10,19): error CS0841: Cannot use local variable 'x4' before it is declared
                //     bool Test4 => x4 && TakeOutParam(4, out int x4);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 19),
                // (13,44): error CS0128: A local variable named 'x5' is already defined in this scope
                //                   TakeOutParam(52, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 44),
                // (19,26): error CS0103: The name 'x7' does not exist in the current context
                //     bool Test72 => Dummy(x7, 2); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 26),
                // (20,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27),
                // (22,54): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     bool this[object x11] => TakeOutParam(1, out int x11) && 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(22, 54)
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
        public void ExpressionBodiedProperties_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1);
        System.Console.WriteLine(new X()[0]);
    }

    static bool Test1 => TakeOutParam(2, out int x1) && Dummy(x1); 

    bool this[object x] => TakeOutParam(1, out int x1) && Dummy(x1); 

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput: @"2
True
1
True");
        }

        [Fact]
        public void ExpressionBodiedProperties_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1);
        System.Console.WriteLine(new X()[0]);
    }

    static bool Test1 => TakeOutParam(2, out var x1) && Dummy(x1); 

    bool this[object x] => TakeOutParam(1, out var x1) && Dummy(x1); 

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput: @"2
True
1
True");
        }

        [Fact]
        public void Scope_ExpressionStatement_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        Dummy(TakeOutParam(true, out var x1), x1);
        {
            Dummy(TakeOutParam(true, out var x1), x1);
        }
        Dummy(TakeOutParam(true, out var x1), x1);
    }

    void Test2()
    {
        Dummy(x2, TakeOutParam(true, out var x2));
    }

    void Test3(int x3)
    {
        Dummy(TakeOutParam(true, out var x3), x3);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);
        Dummy(TakeOutParam(true, out var x4), x4);
    }

    void Test5()
    {
        Dummy(TakeOutParam(true, out var x5), x5);
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    Dummy(TakeOutParam(true, out var x6), x6);
    //}

    //void Test7()
    //{
    //    Dummy(TakeOutParam(true, out var x7), x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8()
    {
        Dummy(TakeOutParam(true, out var x8), x8, TakeOutParam(false, out var x8), x8);
    }

    void Test9(bool y9)
    {
        if (y9)
            Dummy(TakeOutParam(true, out var x9), x9);
    }

    System.Action Test10(bool y10)
    {
        return () =>
                {
                    if (y10)
                        Dummy(TakeOutParam(true, out var x10), x10);
                };
    }

    void Test11()
    {
        Dummy(x11);
        Dummy(TakeOutParam(true, out var x11), x11);
    }

    void Test12()
    {
        Dummy(TakeOutParam(true, out var x12), x12);
        Dummy(x12);
    }

    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (21,15): error CS0841: Cannot use local variable 'x2' before it is declared
                //         Dummy(x2, TakeOutParam(true, out var x2));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 15),
                // (26,42): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Dummy(TakeOutParam(true, out var x3), x3);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 42),
                // (33,42): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Dummy(TakeOutParam(true, out var x4), x4);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(33, 42),
                // (38,42): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Dummy(TakeOutParam(true, out var x5), x5);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(38, 42),
                // (59,79): error CS0128: A local variable named 'x8' is already defined in this scope
                //         Dummy(TakeOutParam(true, out var x8), x8, TakeOutParam(false, out var x8), x8);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 79),
                // (79,15): error CS0103: The name 'x11' does not exist in the current context
                //         Dummy(x11);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(79, 15),
                // (86,15): error CS0103: The name 'x12' does not exist in the current context
                //         Dummy(x12);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(86, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForOutVar(model, x1Decl[i], x1Ref[i]);
            }

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForOutVar(model, x2Decl, x2Ref);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAnOutLocal(model, x4Ref[0]);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1]);

            var x5Decl = GetOutVarDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForOutVar(model, x5Decl, x5Ref[0]);
            VerifyNotAnOutLocal(model, x5Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            for (int i = 0; i < x8Decl.Length; i++)
            {
                VerifyModelForOutVar(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForOutVarDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetOutVarDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").Single();
            VerifyModelForOutVar(model, x9Decl, x9Ref);

            var x10Decl = GetOutVarDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForOutVar(model, x10Decl, x10Ref);

            var x11Decl = GetOutVarDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyModelForOutVar(model, x11Decl, x11Ref[1]);

            var x12Decl = GetOutVarDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForOutVar(model, x12Decl, x12Ref[0]);
            VerifyNotInScope(model, x12Ref[1]);
        }

        [Fact]
        public void Scope_FieldInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Test3 = TakeOutParam(3, out int x3) && x3 > 0;

    bool Test4 = x4 && TakeOutParam(4, out int x4);

    bool Test5 = TakeOutParam(51, out int x5) && 
                 TakeOutParam(52, out int x5) && 
                 x5 > 0;

    bool Test61 = TakeOutParam(6, out int x6) && x6 > 0, Test62 = TakeOutParam(6, out int x6) && x6 > 0;

    bool Test71 = TakeOutParam(7, out int x7) && x7 > 0; 
    bool Test72 = Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (10,18): error CS0841: Cannot use local variable 'x4' before it is declared
                //     bool Test4 = x4 && TakeOutParam(4, out int x4);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 18),
                // (13,43): error CS0128: A local variable named 'x5' is already defined in this scope
                //                  TakeOutParam(52, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 43),
                // (19,25): error CS0103: The name 'x7' does not exist in the current context
                //     bool Test72 = Dummy(x7, 2); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 25),
                // (20,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27)
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
        }

        [Fact]
        public void Scope_FieldInitializers_02()
        {
            var source =
@"using static Test;
public enum X
{
    Test3 = TakeOutParam(3, out int x3) ? x3 : 0,

    Test4 = x4 && TakeOutParam(4, out int x4) ? 1 : 0,

    Test5 = TakeOutParam(51, out int x5) && 
            TakeOutParam(52, out int x5) && 
            x5 > 0 ? 1 : 0,

    Test61 = TakeOutParam(6, out int x6) && x6 > 0 ? 1 : 0, Test62 = TakeOutParam(6, out int x6) && x6 > 0 ? 1 : 0,

    Test71 = TakeOutParam(7, out int x7) && x7 > 0 ? 1 : 0, 
    Test72 = x7, 
}

class Test
{
    public static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (6,13): error CS0841: Cannot use local variable 'x4' before it is declared
                //     Test4 = x4 && TakeOutParam(4, out int x4) ? 1 : 0,
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(6, 13),
                // (9,38): error CS0128: A local variable named 'x5' is already defined in this scope
                //             TakeOutParam(52, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(9, 38),
                // (8,13): error CS0133: The expression being assigned to 'X.Test5' must be constant
                //     Test5 = TakeOutParam(51, out int x5) && 
                Diagnostic(ErrorCode.ERR_NotConstantExpression, @"TakeOutParam(51, out int x5) && 
            TakeOutParam(52, out int x5) && 
            x5 > 0 ? 1 : 0").WithArguments("X.Test5").WithLocation(8, 13),
                // (12,14): error CS0133: The expression being assigned to 'X.Test61' must be constant
                //     Test61 = TakeOutParam(6, out int x6) && x6 > 0 ? 1 : 0, Test62 = TakeOutParam(6, out int x6) && x6 > 0 ? 1 : 0,
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "TakeOutParam(6, out int x6) && x6 > 0 ? 1 : 0").WithArguments("X.Test61").WithLocation(12, 14),
                // (12,70): error CS0133: The expression being assigned to 'X.Test62' must be constant
                //     Test61 = TakeOutParam(6, out int x6) && x6 > 0 ? 1 : 0, Test62 = TakeOutParam(6, out int x6) && x6 > 0 ? 1 : 0,
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "TakeOutParam(6, out int x6) && x6 > 0 ? 1 : 0").WithArguments("X.Test62").WithLocation(12, 70),
                // (14,14): error CS0133: The expression being assigned to 'X.Test71' must be constant
                //     Test71 = TakeOutParam(7, out int x7) && x7 > 0 ? 1 : 0, 
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "TakeOutParam(7, out int x7) && x7 > 0 ? 1 : 0").WithArguments("X.Test71").WithLocation(14, 14),
                // (15,14): error CS0103: The name 'x7' does not exist in the current context
                //     Test72 = x7, 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(15, 14),
                // (4,13): error CS0133: The expression being assigned to 'X.Test3' must be constant
                //     Test3 = TakeOutParam(3, out int x3) ? x3 : 0,
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "TakeOutParam(3, out int x3) ? x3 : 0").WithArguments("X.Test3").WithLocation(4, 13)
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
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
        }

        [Fact]
        public void Scope_FieldInitializers_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    const bool Test3 = TakeOutParam(3, out int x3) && x3 > 0;

    const bool Test4 = x4 && TakeOutParam(4, out int x4);

    const bool Test5 = TakeOutParam(51, out int x5) && 
                       TakeOutParam(52, out int x5) && 
                       x5 > 0;

    const bool Test61 = TakeOutParam(6, out int x6) && x6 > 0, Test62 = TakeOutParam(6, out int x6) && x6 > 0;

    const bool Test71 = TakeOutParam(7, out int x7) && x7 > 0; 
    const bool Test72 = x7 > 2; 
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (8,24): error CS0133: The expression being assigned to 'X.Test3' must be constant
                //     const bool Test3 = TakeOutParam(3, out int x3) && x3 > 0;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "TakeOutParam(3, out int x3) && x3 > 0").WithArguments("X.Test3").WithLocation(8, 24),
                // (10,24): error CS0841: Cannot use local variable 'x4' before it is declared
                //     const bool Test4 = x4 && TakeOutParam(4, out int x4);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 24),
                // (13,49): error CS0128: A local variable named 'x5' is already defined in this scope
                //                        TakeOutParam(52, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 49),
                // (12,24): error CS0133: The expression being assigned to 'X.Test5' must be constant
                //     const bool Test5 = TakeOutParam(51, out int x5) && 
                Diagnostic(ErrorCode.ERR_NotConstantExpression, @"TakeOutParam(51, out int x5) && 
                       TakeOutParam(52, out int x5) && 
                       x5 > 0").WithArguments("X.Test5").WithLocation(12, 24),
                // (16,25): error CS0133: The expression being assigned to 'X.Test61' must be constant
                //     const bool Test61 = TakeOutParam(6, out int x6) && x6 > 0, Test62 = TakeOutParam(6, out int x6) && x6 > 0;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "TakeOutParam(6, out int x6) && x6 > 0").WithArguments("X.Test61").WithLocation(16, 25),
                // (16,73): error CS0133: The expression being assigned to 'X.Test62' must be constant
                //     const bool Test61 = TakeOutParam(6, out int x6) && x6 > 0, Test62 = TakeOutParam(6, out int x6) && x6 > 0;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "TakeOutParam(6, out int x6) && x6 > 0").WithArguments("X.Test62").WithLocation(16, 73),
                // (18,25): error CS0133: The expression being assigned to 'X.Test71' must be constant
                //     const bool Test71 = TakeOutParam(7, out int x7) && x7 > 0; 
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "TakeOutParam(7, out int x7) && x7 > 0").WithArguments("X.Test71").WithLocation(18, 25),
                // (19,25): error CS0103: The name 'x7' does not exist in the current context
                //     const bool Test72 = x7 > 2; 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 25),
                // (20,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27)
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
        }

        [Fact]
        public void FieldInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1);
    }

    static bool Test1 = TakeOutParam(1, out int x1) && Dummy(x1); 

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        [WorkItem(10487, "https://github.com/dotnet/roslyn/issues/10487")]
        public void FieldInitializers_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1);
    }

    static bool Test1 = TakeOutParam(1, out int x1) && Dummy(() => x1); 

    static bool Dummy(System.Func<int> x) 
    {
        System.Console.WriteLine(x());
        return true;
    }

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void Scope_Fixed_01()
        {
            var source =
@"
public unsafe class X
{
    public static void Main()
    {
    }

    int[] Dummy(params object[] x) {return null;}

    void Test1()
    {
        fixed (int* p = Dummy(TakeOutParam(true, out var x1) && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        fixed (int* p = Dummy(TakeOutParam(true, out var x2) && x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        fixed (int* p = Dummy(TakeOutParam(true, out var x4) && x4))
            Dummy(x4);
    }

    void Test6()
    {
        fixed (int* p = Dummy(x6 && TakeOutParam(true, out var x6)))
            Dummy(x6);
    }

    void Test7()
    {
        fixed (int* p = Dummy(TakeOutParam(true, out var x7) && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        fixed (int* p = Dummy(TakeOutParam(true, out var x8) && x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        fixed (int* p1 = Dummy(TakeOutParam(true, out var x9) && x9))
        {   
            Dummy(x9);
            fixed (int* p2 = Dummy(TakeOutParam(true, out var x9) && x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        fixed (int* p = Dummy(TakeOutParam(y10, out var x10)))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    fixed (int* p = Dummy(TakeOutParam(y11, out var x11)))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        fixed (int* p = Dummy(TakeOutParam(y12, out var x12)))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    fixed (int* p = Dummy(TakeOutParam(y13, out var x13)))
    //        let y13 = 12;
    //}

    void Test14()
    {
        fixed (int* p = Dummy(TakeOutParam(1, out var x14), 
                              TakeOutParam(2, out var x14), 
                              x14))
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam(object y, out bool x) 
    {
        x = true;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
                // (29,58): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         fixed (int* p = Dummy(TakeOutParam(true, out var x4) && x4))
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 58),
                // (35,31): error CS0841: Cannot use local variable 'x6' before it is declared
                //         fixed (int* p = Dummy(x6 && TakeOutParam(true, out var x6)))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 31),
                // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
                // (53,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
                // (61,63): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             fixed (int* p2 = Dummy(TakeOutParam(true, out var x9) && x9)) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 63),
                // (68,44): error CS0103: The name 'y10' does not exist in the current context
                //         fixed (int* p = Dummy(TakeOutParam(y10, out var x10)))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 44),
                // (86,44): error CS0103: The name 'y12' does not exist in the current context
                //         fixed (int* p = Dummy(TakeOutParam(y12, out var x12)))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 44),
                // (99,55): error CS0128: A local variable named 'x14' is already defined in this scope
                //                               TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 55)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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
        public void Scope_Fixed_02()
        {
            var source =
@"
public unsafe class X
{
    public static void Main()
    {
    }

    int[] Dummy(params object[] x) {return null;}
    int[] Dummy(int* x) {return null;}

    void Test1()
    {
        fixed (int* x1 = 
                         Dummy(TakeOutParam(true, out var x1) && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        fixed (int* p = Dummy(TakeOutParam(true, out var x2) && x2),
                    x2 = Dummy())
        {
            Dummy(x2);
        }
    }

    void Test3()
    {
        fixed (int* x3 = Dummy(),
                    p = Dummy(TakeOutParam(true, out var x3) && x3))
        {
            Dummy(x3);
        }
    }

    void Test4()
    {
        fixed (int* p1 = Dummy(TakeOutParam(true, out var x4) && x4),
                    p2 = Dummy(TakeOutParam(true, out var x4) && x4))
        {
            Dummy(x4);
        }
    }

    static bool TakeOutParam(object y, out bool x) 
    {
        x = true;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (14,59): error CS0128: A local variable named 'x1' is already defined in this scope
                //                          Dummy(TakeOutParam(true, out var x1) && x1))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(14, 59),
                // (14,32): error CS0019: Operator '&&' cannot be applied to operands of type 'bool' and 'int*'
                //                          Dummy(TakeOutParam(true, out var x1) && x1))
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "TakeOutParam(true, out var x1) && x1").WithArguments("&&", "bool", "int*").WithLocation(14, 32),
                // (14,66): error CS0165: Use of unassigned local variable 'x1'
                //                          Dummy(TakeOutParam(true, out var x1) && x1))
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(14, 66),
                // (23,21): error CS0128: A local variable named 'x2' is already defined in this scope
                //                     x2 = Dummy())
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(23, 21),
                // (32,58): error CS0128: A local variable named 'x3' is already defined in this scope
                //                     p = Dummy(TakeOutParam(true, out var x3) && x3))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(32, 58),
                // (32,31): error CS0019: Operator '&&' cannot be applied to operands of type 'bool' and 'int*'
                //                     p = Dummy(TakeOutParam(true, out var x3) && x3))
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "TakeOutParam(true, out var x3) && x3").WithArguments("&&", "bool", "int*").WithLocation(32, 31),
                // (41,59): error CS0128: A local variable named 'x4' is already defined in this scope
                //                     p2 = Dummy(TakeOutParam(true, out var x4) && x4))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(41, 59)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForOutVarDuplicateInSameScope(model, x1Decl);
            VerifyNotAnOutLocal(model, x1Ref[0]);
            VerifyNotAnOutLocal(model, x1Ref[1]);

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForOutVar(model, x2Decl, x2Ref);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForOutVarDuplicateInSameScope(model, x3Decl);
            VerifyNotAnOutLocal(model, x3Ref[0]);
            VerifyNotAnOutLocal(model, x3Ref[1]);

            var x4Decl = GetOutVarDeclarations(tree, "x4").ToArray();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Decl.Length);
            Assert.Equal(3, x4Ref.Length);
            VerifyModelForOutVar(model, x4Decl[0], x4Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x4Decl[1]);
        }

        [Fact]
        public void Fixed_01()
        {
            var source =
@"
public unsafe class X
{
    public static void Main()
    {
        fixed (int* p = Dummy(TakeOutParam(""fixed"", out var x1), x1))
        {
            System.Console.WriteLine(x1);
        }
    }

    static int[] Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new int[1];
    }

    static bool TakeOutParam(string y, out string x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"fixed
fixed");
        }

        [Fact]
        public void Fixed_02()
        {
            var source =
@"
public unsafe class X
{
    public static void Main()
    {
        fixed (int* p = Dummy(TakeOutParam(""fixed"", out string x1), x1))
        {
            System.Console.WriteLine(x1);
        }
    }

    static int[] Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new int[1];
    }

    static bool TakeOutParam(string y, out string x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"fixed
fixed");
        }

        [Fact]
        public void Scope_For_01()
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
        for (
             Dummy(TakeOutParam(true, out var x1) && x1)
             ;;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (
             Dummy(TakeOutParam(true, out var x2) && x2)
             ;;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (
             Dummy(TakeOutParam(true, out var x4) && x4)
             ;;)
            Dummy(x4);
    }

    void Test6()
    {
        for (
             Dummy(x6 && TakeOutParam(true, out var x6))
             ;;)
            Dummy(x6);
    }

    void Test7()
    {
        for (
             Dummy(TakeOutParam(true, out var x7) && x7)
             ;;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (
             Dummy(TakeOutParam(true, out var x8) && x8)
             ;;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (
             Dummy(TakeOutParam(true, out var x9) && x9)
             ;;)
        {   
            Dummy(x9);
            for (
                 Dummy(TakeOutParam(true, out var x9) && x9) // 2
                 ;;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (
             Dummy(TakeOutParam(y10, out var x10))
             ;;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (
    //         Dummy(TakeOutParam(y11, out var x11))
    //         ;;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (
             Dummy(TakeOutParam(y12, out var x12))
             ;;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (
    //         Dummy(TakeOutParam(y13, out var x13))
    //         ;;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (
             Dummy(TakeOutParam(1, out var x14), 
                   TakeOutParam(2, out var x14), 
                   x14)
             ;;)
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam(object y, out bool x) 
    {
        x = true;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
                // (34,47): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //              Dummy(TakeOutParam(true, out var x4) && x4)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 47),
                // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
                //              Dummy(x6 && TakeOutParam(true, out var x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
                // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
                // (65,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
                // (65,9): warning CS0162: Unreachable code detected
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
                // (76,51): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                  Dummy(TakeOutParam(true, out var x9) && x9) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 51),
                // (85,33): error CS0103: The name 'y10' does not exist in the current context
                //              Dummy(TakeOutParam(y10, out var x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 33),
                // (107,33): error CS0103: The name 'y12' does not exist in the current context
                //              Dummy(TakeOutParam(y12, out var x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 33),
                // (124,44): error CS0128: A local variable named 'x14' is already defined in this scope
                //                    TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 44)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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
        public void Scope_For_02()
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
        for (;
             Dummy(TakeOutParam(true, out var x1) && x1)
             ;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (;
             Dummy(TakeOutParam(true, out var x2) && x2)
             ;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (;
             Dummy(TakeOutParam(true, out var x4) && x4)
             ;)
            Dummy(x4);
    }

    void Test6()
    {
        for (;
             Dummy(x6 && TakeOutParam(true, out var x6))
             ;)
            Dummy(x6);
    }

    void Test7()
    {
        for (;
             Dummy(TakeOutParam(true, out var x7) && x7)
             ;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (;
             Dummy(TakeOutParam(true, out var x8) && x8)
             ;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (;
             Dummy(TakeOutParam(true, out var x9) && x9)
             ;)
        {   
            Dummy(x9);
            for (;
                 Dummy(TakeOutParam(true, out var x9) && x9) // 2
                 ;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (;
             Dummy(TakeOutParam(y10, out var x10))
             ;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (;
    //         Dummy(TakeOutParam(y11, out var x11))
    //         ;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (;
             Dummy(TakeOutParam(y12, out var x12))
             ;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (;
    //         Dummy(TakeOutParam(y13, out var x13))
    //         ;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (;
             Dummy(TakeOutParam(1, out var x14), 
                   TakeOutParam(2, out var x14), 
                   x14)
             ;)
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam(object y, out bool x) 
    {
        x = true;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
                // (34,47): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //              Dummy(TakeOutParam(true, out var x4) && x4)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 47),
                // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
                //              Dummy(x6 && TakeOutParam(true, out var x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
                // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
                // (65,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
                // (76,51): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                  Dummy(TakeOutParam(true, out var x9) && x9) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 51),
                // (85,33): error CS0103: The name 'y10' does not exist in the current context
                //              Dummy(TakeOutParam(y10, out var x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 33),
                // (107,33): error CS0103: The name 'y12' does not exist in the current context
                //              Dummy(TakeOutParam(y12, out var x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 33),
                // (124,44): error CS0128: A local variable named 'x14' is already defined in this scope
                //                    TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 44)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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
        public void Scope_For_03()
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
        for (;;
             Dummy(TakeOutParam(true, out var x1) && x1)
             )
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (;;
             Dummy(TakeOutParam(true, out var x2) && x2)
             )
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (;;
             Dummy(TakeOutParam(true, out var x4) && x4)
             )
            Dummy(x4);
    }

    void Test6()
    {
        for (;;
             Dummy(x6 && TakeOutParam(true, out var x6))
             )
            Dummy(x6);
    }

    void Test7()
    {
        for (;;
             Dummy(TakeOutParam(true, out var x7) && x7)
             )
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (;;
             Dummy(TakeOutParam(true, out var x8) && x8)
             )
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (;;
             Dummy(TakeOutParam(true, out var x9) && x9)
             )
        {   
            Dummy(x9);
            for (;;
                 Dummy(TakeOutParam(true, out var x9) && x9) // 2
                 )
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (;;
             Dummy(TakeOutParam(y10, out var x10))
             )
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (;;
    //         Dummy(TakeOutParam(y11, out var x11))
    //         )
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (;;
             Dummy(TakeOutParam(y12, out var x12))
             )
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (;;
    //         Dummy(TakeOutParam(y13, out var x13))
    //         )
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (;;
             Dummy(TakeOutParam(1, out var x14), 
                   TakeOutParam(2, out var x14), 
                   x14)
             )
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam(object y, out bool x) 
    {
        x = true;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
                // (16,19): error CS0165: Use of unassigned local variable 'x1'
                //             Dummy(x1);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(16, 19),
                // (25,19): error CS0165: Use of unassigned local variable 'x2'
                //             Dummy(x2);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(25, 19),
                // (34,47): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //              Dummy(TakeOutParam(true, out var x4) && x4)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 47),
                // (36,19): error CS0165: Use of unassigned local variable 'x4'
                //             Dummy(x4);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x4").WithArguments("x4").WithLocation(36, 19),
                // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
                //              Dummy(x6 && TakeOutParam(true, out var x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
                // (44,19): error CS0165: Use of unassigned local variable 'x6'
                //             Dummy(x6);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x6").WithArguments("x6").WithLocation(44, 19),
                // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
                // (65,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
                // (65,9): warning CS0162: Unreachable code detected
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
                // (63,19): error CS0165: Use of unassigned local variable 'x8'
                //             Dummy(x8);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x8").WithArguments("x8").WithLocation(63, 19),
                // (76,51): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                  Dummy(TakeOutParam(true, out var x9) && x9) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 51),
                // (71,14): warning CS0162: Unreachable code detected
                //              Dummy(TakeOutParam(true, out var x9) && x9)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(71, 14),
                // (74,19): error CS0165: Use of unassigned local variable 'x9'
                //             Dummy(x9);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x9").WithArguments("x9").WithLocation(74, 19),
                // (78,23): error CS0165: Use of unassigned local variable 'x9'
                //                 Dummy(x9);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x9").WithArguments("x9").WithLocation(78, 23),
                // (85,33): error CS0103: The name 'y10' does not exist in the current context
                //              Dummy(TakeOutParam(y10, out var x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 33),
                // (107,33): error CS0103: The name 'y12' does not exist in the current context
                //              Dummy(TakeOutParam(y12, out var x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 33),
                // (124,44): error CS0128: A local variable named 'x14' is already defined in this scope
                //                    TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 44),
                // (128,19): error CS0165: Use of unassigned local variable 'x14'
                //             Dummy(x14);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x14").WithArguments("x14").WithLocation(128, 19)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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
        public void Scope_For_04()
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
        for (var b =
             Dummy(TakeOutParam(true, out var x1) && x1)
             ;;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (var b =
             Dummy(TakeOutParam(true, out var x2) && x2)
             ;;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (var b =
             Dummy(TakeOutParam(true, out var x4) && x4)
             ;;)
            Dummy(x4);
    }

    void Test6()
    {
        for (var b =
             Dummy(x6 && TakeOutParam(true, out var x6))
             ;;)
            Dummy(x6);
    }

    void Test7()
    {
        for (var b =
             Dummy(TakeOutParam(true, out var x7) && x7)
             ;;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (var b =
             Dummy(TakeOutParam(true, out var x8) && x8)
             ;;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (var b1 =
             Dummy(TakeOutParam(true, out var x9) && x9)
             ;;)
        {   
            Dummy(x9);
            for (var b2 =
                 Dummy(TakeOutParam(true, out var x9) && x9) // 2
                 ;;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (var b =
             Dummy(TakeOutParam(y10, out var x10))
             ;;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (var b =
    //         Dummy(TakeOutParam(y11, out var x11))
    //         ;;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (var b =
             Dummy(TakeOutParam(y12, out var x12))
             ;;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (var b =
    //         Dummy(TakeOutParam(y13, out var x13))
    //         ;;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (var b =
             Dummy(TakeOutParam(1, out var x14), 
                   TakeOutParam(2, out var x14), 
                   x14)
             ;;)
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam(object y, out bool x) 
    {
        x = true;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
                // (34,47): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //              Dummy(TakeOutParam(true, out var x4) && x4)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 47),
                // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
                //              Dummy(x6 && TakeOutParam(true, out var x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
                // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
                // (65,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
                // (65,9): warning CS0162: Unreachable code detected
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
                // (76,51): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                  Dummy(TakeOutParam(true, out var x9) && x9) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 51),
                // (85,33): error CS0103: The name 'y10' does not exist in the current context
                //              Dummy(TakeOutParam(y10, out var x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 33),
                // (107,33): error CS0103: The name 'y12' does not exist in the current context
                //              Dummy(TakeOutParam(y12, out var x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 33),
                // (124,44): error CS0128: A local variable named 'x14' is already defined in this scope
                //                    TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 44)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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
        public void Scope_For_05()
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
        for (bool b =
             Dummy(TakeOutParam(true, out var x1) && x1)
             ;;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (bool b =
             Dummy(TakeOutParam(true, out var x2) && x2)
             ;;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (bool b =
             Dummy(TakeOutParam(true, out var x4) && x4)
             ;;)
            Dummy(x4);
    }

    void Test6()
    {
        for (bool b =
             Dummy(x6 && TakeOutParam(true, out var x6))
             ;;)
            Dummy(x6);
    }

    void Test7()
    {
        for (bool b =
             Dummy(TakeOutParam(true, out var x7) && x7)
             ;;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (bool b =
             Dummy(TakeOutParam(true, out var x8) && x8)
             ;;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (bool b1 =
             Dummy(TakeOutParam(true, out var x9) && x9)
             ;;)
        {   
            Dummy(x9);
            for (bool b2 =
                 Dummy(TakeOutParam(true, out var x9) && x9) // 2
                 ;;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (bool b =
             Dummy(TakeOutParam(y10, out var x10))
             ;;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (bool b =
    //         Dummy(TakeOutParam(y11, out var x11))
    //         ;;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (bool b =
             Dummy(TakeOutParam(y12, out var x12))
             ;;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (bool b =
    //         Dummy(TakeOutParam(y13, out var x13))
    //         ;;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (bool b =
             Dummy(TakeOutParam(1, out var x14), 
                   TakeOutParam(2, out var x14), 
                   x14)
             ;;)
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam(object y, out bool x) 
    {
        x = true;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
                // (34,47): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //              Dummy(TakeOutParam(true, out var x4) && x4)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 47),
                // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
                //              Dummy(x6 && TakeOutParam(true, out var x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
                // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
                // (65,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
                // (65,9): warning CS0162: Unreachable code detected
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
                // (76,51): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                  Dummy(TakeOutParam(true, out var x9) && x9) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 51),
                // (85,33): error CS0103: The name 'y10' does not exist in the current context
                //              Dummy(TakeOutParam(y10, out var x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 33),
                // (107,33): error CS0103: The name 'y12' does not exist in the current context
                //              Dummy(TakeOutParam(y12, out var x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 33),
                // (124,44): error CS0128: A local variable named 'x14' is already defined in this scope
                //                    TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 44)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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
        public void Scope_For_06()
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
        for (var x1 =
             Dummy(TakeOutParam(true, out var x1) && x1)
             ;;)
        {}
    }

    void Test2()
    {
        for (var x2 = true;
             Dummy(TakeOutParam(true, out var x2) && x2)
             ;)
        {}
    }

    void Test3()
    {
        for (var x3 = true;;
             Dummy(TakeOutParam(true, out var x3) && x3)
             )
        {}
    }

    void Test4()
    {
        for (bool x4 =
             Dummy(TakeOutParam(true, out var x4) && x4)
             ;;)
        {}
    }

    void Test5()
    {
        for (bool x5 = true;
             Dummy(TakeOutParam(true, out var x5) && x5)
             ;)
        {}
    }

    void Test6()
    {
        for (bool x6 = true;;
             Dummy(TakeOutParam(true, out var x6) && x6)
             )
        {}
    }

    void Test7()
    {
        for (bool x7 = true, b =
             Dummy(TakeOutParam(true, out var x7) && x7)
             ;;)
        {}
    }

    void Test8()
    {
        for (bool b1 = Dummy(TakeOutParam(true, out var x8) && x8), 
             b2 = Dummy(TakeOutParam(true, out var x8) && x8);
             Dummy(TakeOutParam(true, out var x8) && x8);
             Dummy(TakeOutParam(true, out var x8) && x8))
        {}
    }

    void Test9()
    {
        for (bool b = x9, 
             b2 = Dummy(TakeOutParam(true, out var x9) && x9);
             Dummy(TakeOutParam(true, out var x9) && x9);
             Dummy(TakeOutParam(true, out var x9) && x9))
        {}
    }

    void Test10()
    {
        for (var b = x10;
             Dummy(TakeOutParam(true, out var x10) && x10) &&
             Dummy(TakeOutParam(true, out var x10) && x10);
             Dummy(TakeOutParam(true, out var x10) && x10))
        {}
    }

    void Test11()
    {
        for (bool b = x11;
             Dummy(TakeOutParam(true, out var x11) && x11) &&
             Dummy(TakeOutParam(true, out var x11) && x11);
             Dummy(TakeOutParam(true, out var x11) && x11))
        {}
    }

    void Test12()
    {
        for (Dummy(x12);
             Dummy(x12) &&
             Dummy(TakeOutParam(true, out var x12) && x12);
             Dummy(TakeOutParam(true, out var x12) && x12))
        {}
    }

    void Test13()
    {
        for (var b = x13;
             Dummy(x13);
             Dummy(TakeOutParam(true, out var x13) && x13),
             Dummy(TakeOutParam(true, out var x13) && x13))
        {}
    }

    void Test14()
    {
        for (bool b = x14;
             Dummy(x14);
             Dummy(TakeOutParam(true, out var x14) && x14),
             Dummy(TakeOutParam(true, out var x14) && x14))
        {}
    }

    void Test15()
    {
        for (Dummy(x15);
             Dummy(x15);
             Dummy(x15),
             Dummy(TakeOutParam(true, out var x15) && x15))
        {}
    }

    static bool TakeOutParam(object y, out bool x) 
    {
        x = true;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (13,47): error CS0128: A local variable named 'x1' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x1) && x1)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(13, 47),
                // (13,54): error CS0841: Cannot use local variable 'x1' before it is declared
                //              Dummy(TakeOutParam(true, out var x1) && x1)
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(13, 54),
                // (13,54): error CS0165: Use of unassigned local variable 'x1'
                //              Dummy(TakeOutParam(true, out var x1) && x1)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(13, 54),
                // (21,47): error CS0128: A local variable named 'x2' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x2) && x2)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(21, 47),
                // (29,47): error CS0128: A local variable named 'x3' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x3) && x3)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(29, 47),
                // (37,47): error CS0128: A local variable named 'x4' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x4) && x4)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(37, 47),
                // (37,54): error CS0165: Use of unassigned local variable 'x4'
                //              Dummy(TakeOutParam(true, out var x4) && x4)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x4").WithArguments("x4").WithLocation(37, 54),
                // (45,47): error CS0128: A local variable named 'x5' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x5) && x5)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(45, 47),
                // (53,47): error CS0128: A local variable named 'x6' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x6) && x6)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(53, 47),
                // (61,47): error CS0128: A local variable named 'x7' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x7) && x7)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x7").WithArguments("x7").WithLocation(61, 47),
                // (69,52): error CS0128: A local variable named 'x8' is already defined in this scope
                //              b2 = Dummy(TakeOutParam(true, out var x8) && x8);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(69, 52),
                // (70,47): error CS0128: A local variable named 'x8' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x8) && x8);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(70, 47),
                // (71,47): error CS0128: A local variable named 'x8' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x8) && x8))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(71, 47),
                // (77,23): error CS0841: Cannot use local variable 'x9' before it is declared
                //         for (bool b = x9, 
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(77, 23),
                // (79,47): error CS0128: A local variable named 'x9' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x9) && x9);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x9").WithArguments("x9").WithLocation(79, 47),
                // (80,47): error CS0128: A local variable named 'x9' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x9) && x9))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x9").WithArguments("x9").WithLocation(80, 47),
                // (86,22): error CS0841: Cannot use local variable 'x10' before it is declared
                //         for (var b = x10;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x10").WithArguments("x10").WithLocation(86, 22),
                // (88,47): error CS0128: A local variable named 'x10' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x10) && x10);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x10").WithArguments("x10").WithLocation(88, 47),
                // (89,47): error CS0128: A local variable named 'x10' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x10) && x10))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x10").WithArguments("x10").WithLocation(89, 47),
                // (95,23): error CS0841: Cannot use local variable 'x11' before it is declared
                //         for (bool b = x11;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x11").WithArguments("x11").WithLocation(95, 23),
                // (97,47): error CS0128: A local variable named 'x11' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x11) && x11);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x11").WithArguments("x11").WithLocation(97, 47),
                // (98,47): error CS0128: A local variable named 'x11' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x11) && x11))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x11").WithArguments("x11").WithLocation(98, 47),
                // (104,20): error CS0841: Cannot use local variable 'x12' before it is declared
                //         for (Dummy(x12);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(104, 20),
                // (105,20): error CS0841: Cannot use local variable 'x12' before it is declared
                //              Dummy(x12) &&
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(105, 20),
                // (107,47): error CS0128: A local variable named 'x12' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x12) && x12))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x12").WithArguments("x12").WithLocation(107, 47),
                // (113,22): error CS0841: Cannot use local variable 'x13' before it is declared
                //         for (var b = x13;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x13").WithArguments("x13").WithLocation(113, 22),
                // (114,20): error CS0841: Cannot use local variable 'x13' before it is declared
                //              Dummy(x13);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x13").WithArguments("x13").WithLocation(114, 20),
                // (116,47): error CS0128: A local variable named 'x13' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x13) && x13))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x13").WithArguments("x13").WithLocation(116, 47),
                // (122,23): error CS0841: Cannot use local variable 'x14' before it is declared
                //         for (bool b = x14;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(122, 23),
                // (123,20): error CS0841: Cannot use local variable 'x14' before it is declared
                //              Dummy(x14);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(123, 20),
                // (125,47): error CS0128: A local variable named 'x14' is already defined in this scope
                //              Dummy(TakeOutParam(true, out var x14) && x14))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(125, 47),
                // (131,20): error CS0841: Cannot use local variable 'x15' before it is declared
                //         for (Dummy(x15);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(131, 20),
                // (132,20): error CS0841: Cannot use local variable 'x15' before it is declared
                //              Dummy(x15);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(132, 20),
                // (133,20): error CS0841: Cannot use local variable 'x15' before it is declared
                //              Dummy(x15),
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(133, 20)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForOutVarDuplicateInSameScope(model, x1Decl);
            VerifyNotAnOutLocal(model, x1Ref);

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForOutVarDuplicateInSameScope(model, x2Decl);
            VerifyNotAnOutLocal(model, x2Ref);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForOutVarDuplicateInSameScope(model, x3Decl);
            VerifyNotAnOutLocal(model, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForOutVarDuplicateInSameScope(model, x4Decl);
            VerifyNotAnOutLocal(model, x4Ref);

            var x5Decl = GetOutVarDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").Single();
            VerifyModelForOutVarDuplicateInSameScope(model, x5Decl);
            VerifyNotAnOutLocal(model, x5Ref);

            var x6Decl = GetOutVarDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").Single();
            VerifyModelForOutVarDuplicateInSameScope(model, x6Decl);
            VerifyNotAnOutLocal(model, x6Ref);

            var x7Decl = GetOutVarDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").Single();
            VerifyModelForOutVarDuplicateInSameScope(model, x7Decl);
            VerifyNotAnOutLocal(model, x7Ref);

            var x8Decl = GetOutVarDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(4, x8Decl.Length);
            Assert.Equal(4, x8Ref.Length);
            VerifyModelForOutVar(model, x8Decl[0], x8Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x8Decl[1]);
            VerifyModelForOutVarDuplicateInSameScope(model, x8Decl[2]);
            VerifyModelForOutVarDuplicateInSameScope(model, x8Decl[3]);

            var x9Decl = GetOutVarDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(3, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForOutVar(model, x9Decl[0], x9Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x9Decl[1]);
            VerifyModelForOutVarDuplicateInSameScope(model, x9Decl[2]);

            var x10Decl = GetOutVarDeclarations(tree, "x10").ToArray();
            var x10Ref = GetReferences(tree, "x10").ToArray();
            Assert.Equal(3, x10Decl.Length);
            Assert.Equal(4, x10Ref.Length);
            VerifyModelForOutVar(model, x10Decl[0], x10Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x10Decl[1]);
            VerifyModelForOutVarDuplicateInSameScope(model, x10Decl[2]);

            var x11Decl = GetOutVarDeclarations(tree, "x11").ToArray();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(3, x11Decl.Length);
            Assert.Equal(4, x11Ref.Length);
            VerifyModelForOutVar(model, x11Decl[0], x11Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x11Decl[1]);
            VerifyModelForOutVarDuplicateInSameScope(model, x11Decl[2]);

            var x12Decl = GetOutVarDeclarations(tree, "x12").ToArray();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Decl.Length);
            Assert.Equal(4, x12Ref.Length);
            VerifyModelForOutVar(model, x12Decl[0], x12Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x12Decl[1]);

            var x13Decl = GetOutVarDeclarations(tree, "x13").ToArray();
            var x13Ref = GetReferences(tree, "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(4, x13Ref.Length);
            VerifyModelForOutVar(model, x13Decl[0], x13Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x13Decl[1]);

            var x14Decl = GetOutVarDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(4, x14Ref.Length);
            VerifyModelForOutVar(model, x14Decl[0], x14Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = GetOutVarDeclarations(tree, "x15").Single();
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(4, x15Ref.Length);
            VerifyModelForOutVar(model, x15Decl, x15Ref);
        }

        [Fact]
        public void For_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        for (Dummy(f, TakeOutParam((f ? 10 : 20), out var x0), x0); 
             Dummy(f, TakeOutParam((f ? 1 : 2), out var x1), x1); 
             Dummy(f, TakeOutParam((f ? 100 : 200), out var x2), x2))
        {
            System.Console.WriteLine(x0);
            System.Console.WriteLine(x1);
            f = false;
        }
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"10
1
10
1
200
2");
        }

        [Fact]
        public void Scope_Foreach_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.Collections.IEnumerable Dummy(params object[] x) {return null;}

    void Test1()
    {
        foreach (var i in Dummy(TakeOutParam(true, out var x1) && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        foreach (var i in Dummy(TakeOutParam(true, out var x2) && x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        foreach (var i in Dummy(TakeOutParam(true, out var x4) && x4))
            Dummy(x4);
    }

    void Test6()
    {
        foreach (var i in Dummy(x6 && TakeOutParam(true, out var x6)))
            Dummy(x6);
    }

    void Test7()
    {
        foreach (var i in Dummy(TakeOutParam(true, out var x7) && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        foreach (var i in Dummy(TakeOutParam(true, out var x8) && x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        foreach (var i1 in Dummy(TakeOutParam(true, out var x9) && x9))
        {   
            Dummy(x9);
            foreach (var i2 in Dummy(TakeOutParam(true, out var x9) && x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        foreach (var i in Dummy(TakeOutParam(y10, out var x10)))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    foreach (var i in Dummy(TakeOutParam(y11, out var x11)))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        foreach (var i in Dummy(TakeOutParam(y12, out var x12)))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    foreach (var i in Dummy(TakeOutParam(y13, out var x13)))
    //        let y13 = 12;
    //}

    void Test14()
    {
        foreach (var i in Dummy(TakeOutParam(1, out var x14), 
                                TakeOutParam(2, out var x14), 
                                x14))
        {
            Dummy(x14);
        }
    }

    void Test15()
    {
        foreach (var x15 in 
                            Dummy(TakeOutParam(1, out var x15), x15))
        {
            Dummy(x15);
        }
    }

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }

    static bool TakeOutParam(bool y, out bool x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
                // (29,60): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         foreach (var i in Dummy(TakeOutParam(true, out var x4) && x4))
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 60),
                // (35,33): error CS0841: Cannot use local variable 'x6' before it is declared
                //         foreach (var i in Dummy(x6 && TakeOutParam(true, out var x6)))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 33),
                // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
                // (53,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
                // (61,65): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             foreach (var i2 in Dummy(TakeOutParam(true, out var x9) && x9)) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 65),
                // (68,46): error CS0103: The name 'y10' does not exist in the current context
                //         foreach (var i in Dummy(TakeOutParam(y10, out var x10)))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 46),
                // (86,46): error CS0103: The name 'y12' does not exist in the current context
                //         foreach (var i in Dummy(TakeOutParam(y12, out var x12)))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 46),
                // (99,57): error CS0128: A local variable named 'x14' is already defined in this scope
                //                                 TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 57),
                // (108,22): error CS0136: A local or parameter named 'x15' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         foreach (var x15 in 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x15").WithArguments("x15").WithLocation(108, 22)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetOutVarDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForOutVar(model, x14Decl[0], x14Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = GetOutVarDeclarations(tree, "x15").Single();
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(2, x15Ref.Length);
            VerifyModelForOutVar(model, x15Decl, x15Ref[0]);
            VerifyNotAnOutLocal(model, x15Ref[1]);
        }

        [Fact]
        public void Foreach_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        foreach (var i in Dummy(TakeOutParam(3, out var x1), x1))
        {
            System.Console.WriteLine(x1);
        }
    }

    static System.Collections.IEnumerable Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return ""a"";
    }

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"3
3");
        }

        [Fact]
        public void Scope_If_01()
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
        if (TakeOutParam(true, out var x1))
        {
            Dummy(x1);
        }
        else
        {
            System.Console.WriteLine(x1);
        }
    }

    void Test2()
    {
        if (TakeOutParam(true, out var x2))
            Dummy(x2);
        else
            System.Console.WriteLine(x2);
    }

    void Test3()
    {
        if (TakeOutParam(true, out var x3))
            Dummy(x3);
        else
        {
            var x3 = 12;
            System.Console.WriteLine(x3);
        }
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        if (TakeOutParam(true, out var x4))
            Dummy(x4);
    }

    void Test5(int x5)
    {
        if (TakeOutParam(true, out var x5))
            Dummy(x5);
    }

    void Test6()
    {
        if (x6 && TakeOutParam(true, out var x6))
            Dummy(x6);
    }

    void Test7()
    {
        if (TakeOutParam(true, out var x7) && x7)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        if (TakeOutParam(true, out var x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        if (TakeOutParam(true, out var x9))
        {   
            Dummy(x9);
            if (TakeOutParam(true, out var x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        if (TakeOutParam(y10, out var x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    void Test12()
    {
        if (TakeOutParam(y12, out var x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    if (TakeOutParam(y13, out var x13))
    //        let y13 = 12;
    //}

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }

    static bool TakeOutParam(bool y, out bool x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (101,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(101, 13),
                // (18,38): error CS0103: The name 'x1' does not exist in the current context
                //             System.Console.WriteLine(x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(18, 38),
                // (27,38): error CS0103: The name 'x2' does not exist in the current context
                //             System.Console.WriteLine(x2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(27, 38),
                // (46,40): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         if (TakeOutParam(true, out var x4))
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(46, 40),
                // (52,40): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         if (TakeOutParam(true, out var x5))
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(52, 40),
                // (58,13): error CS0841: Cannot use local variable 'x6' before it is declared
                //         if (x6 && TakeOutParam(true, out var x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(58, 13),
                // (59,19): error CS0165: Use of unassigned local variable 'x6'
                //             Dummy(x6);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x6").WithArguments("x6").WithLocation(59, 19),
                // (66,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(66, 17),
                // (76,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(76, 34),
                // (84,44): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             if (TakeOutParam(true, out var x9)) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(84, 44),
                // (91,26): error CS0103: The name 'y10' does not exist in the current context
                //         if (TakeOutParam(y10, out var x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(91, 26),
                // (100,26): error CS0103: The name 'y12' does not exist in the current context
                //         if (TakeOutParam(y12, out var x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(100, 26)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref[0]);
            VerifyNotInScope(model, x1Ref[1]);

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForOutVar(model, x2Decl, x2Ref[0]);
            VerifyNotInScope(model, x2Ref[1]);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForOutVar(model, x3Decl, x3Ref[0]);
            VerifyNotAnOutLocal(model, x3Ref[1]);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1]);
            VerifyNotAnOutLocal(model, x4Ref[0]);

            var x5Decl = GetOutVarDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").Single();
            VerifyModelForOutVar(model, x5Decl, x5Ref);

            var x6Decl = GetOutVarDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForOutVar(model, x6Decl, x6Ref);

            var x7Decl = GetOutVarDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForOutVar(model, x8Decl, x8Ref[0]);
            VerifyNotInScope(model, x8Ref[1]);

            var x9Decl = GetOutVarDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(2, x9Ref.Length);
            VerifyModelForOutVar(model, x9Decl[0], x9Ref[0]);
            VerifyModelForOutVar(model, x9Decl[1], x9Ref[1]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAnOutLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);
        }

        [Fact]
        public void Scope_Lambda_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    System.Action<object> Test1()
    {
        return (o) => let x1 = o;
    }

    System.Action<object> Test2()
    {
        return (o) => let var x2 = o;
    }

    void Test3()
    {
        Dummy((System.Func<object, bool>) (o => TakeOutParam(o, out int x3) && x3 > 0));
    }

    void Test4()
    {
        Dummy((System.Func<object, bool>) (o => x4 && TakeOutParam(o, out int x4)));
    }

    void Test5()
    {
        Dummy((System.Func<object, object, bool>) ((o1, o2) => TakeOutParam(o1, out int x5) && 
                                                               TakeOutParam(o2, out int x5) && 
                                                               x5 > 0));
    }

    void Test6()
    {
        Dummy((System.Func<object, bool>) (o => TakeOutParam(o, out int x6) && x6 > 0), (System.Func<object, bool>) (o => TakeOutParam(o, out int x6) && x6 > 0));
    }

    void Test7()
    {
        Dummy(x7, 1);
        Dummy(x7, 
             (System.Func<object, bool>) (o => TakeOutParam(o, out int x7) && x7 > 0), 
              x7);
        Dummy(x7, 2); 
    }

    void Test8()
    {
        Dummy(TakeOutParam(true, out var x8) && x8, (System.Func<object, bool>) (o => TakeOutParam(o, out int y8) && x8));
    }

    void Test9()
    {
        Dummy(TakeOutParam(true, out var x9), 
              (System.Func<object, bool>) (o => TakeOutParam(o, out int x9) && 
                                                x9 > 0), x9);
    }

    void Test10()
    {
        Dummy((System.Func<object, bool>) (o => TakeOutParam(o, out int x10) && 
                                                x10 > 0),
              TakeOutParam(true, out var x10), x10);
    }

    void Test11()
    {
        var x11 = 11;
        Dummy(x11);
        Dummy((System.Func<object, bool>) (o => TakeOutParam(o, out int x11) && 
                                                x11 > 0), x11);
    }

    void Test12()
    {
        Dummy((System.Func<object, bool>) (o => TakeOutParam(o, out int x12) && 
                                                x12 > 0), 
              x12);
        var x12 = 11;
        Dummy(x12);
    }

    static bool TakeOutParam(object y, out int x) 
    {
        x = 123;
        return true;
    }

    static bool TakeOutParam(bool y, out bool x) 
    {
        x = true;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (12,27): error CS1002: ; expected
                //         return (o) => let x1 = o;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "x1").WithLocation(12, 27),
                // (17,27): error CS1002: ; expected
                //         return (o) => let var x2 = o;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(17, 27),
                // (12,23): error CS0103: The name 'let' does not exist in the current context
                //         return (o) => let x1 = o;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(12, 23),
                // (12,23): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         return (o) => let x1 = o;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(12, 23),
                // (12,27): error CS0103: The name 'x1' does not exist in the current context
                //         return (o) => let x1 = o;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(12, 27),
                // (12,32): error CS0103: The name 'o' does not exist in the current context
                //         return (o) => let x1 = o;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(12, 32),
                // (12,27): warning CS0162: Unreachable code detected
                //         return (o) => let x1 = o;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x1").WithLocation(12, 27),
                // (17,23): error CS0103: The name 'let' does not exist in the current context
                //         return (o) => let var x2 = o;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(17, 23),
                // (17,23): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         return (o) => let var x2 = o;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(17, 23),
                // (17,36): error CS0103: The name 'o' does not exist in the current context
                //         return (o) => let var x2 = o;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(17, 36),
                // (17,27): warning CS0162: Unreachable code detected
                //         return (o) => let var x2 = o;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(17, 27),
                // (27,49): error CS0841: Cannot use local variable 'x4' before it is declared
                //         Dummy((System.Func<object, bool>) (o => x4 && TakeOutParam(o, out int x4)));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(27, 49),
                // (33,89): error CS0128: A local variable named 'x5' is already defined in this scope
                //                                                                TakeOutParam(o2, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(33, 89),
                // (44,15): error CS0103: The name 'x7' does not exist in the current context
                //         Dummy(x7, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(44, 15),
                // (45,15): error CS0103: The name 'x7' does not exist in the current context
                //         Dummy(x7, 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(45, 15),
                // (47,15): error CS0103: The name 'x7' does not exist in the current context
                //               x7);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(47, 15),
                // (48,15): error CS0103: The name 'x7' does not exist in the current context
                //         Dummy(x7, 2); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(48, 15),
                // (59,73): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //               (System.Func<object, bool>) (o => TakeOutParam(o, out int x9) && 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(59, 73),
                // (65,73): error CS0136: A local or parameter named 'x10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Dummy((System.Func<object, bool>) (o => TakeOutParam(o, out int x10) && 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x10").WithArguments("x10").WithLocation(65, 73),
                // (74,73): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Dummy((System.Func<object, bool>) (o => TakeOutParam(o, out int x11) && 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(74, 73),
                // (80,73): error CS0136: A local or parameter named 'x12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Dummy((System.Func<object, bool>) (o => TakeOutParam(o, out int x12) && 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x12").WithArguments("x12").WithLocation(80, 73),
                // (82,15): error CS0841: Cannot use local variable 'x12' before it is declared
                //               x12);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(82, 15)
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
            Assert.Equal(5, x7Ref.Length);
            VerifyNotInScope(model, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyModelForOutVar(model, x7Decl, x7Ref[2]);
            VerifyNotInScope(model, x7Ref[3]);
            VerifyNotInScope(model, x7Ref[4]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForOutVar(model, x8Decl, x8Ref);

            var x9Decl = GetOutVarDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(2, x9Ref.Length);
            VerifyModelForOutVar(model, x9Decl[0], x9Ref[1]);
            VerifyModelForOutVar(model, x9Decl[1], x9Ref[0]);

            var x10Decl = GetOutVarDeclarations(tree, "x10").ToArray();
            var x10Ref = GetReferences(tree, "x10").ToArray();
            Assert.Equal(2, x10Decl.Length);
            Assert.Equal(2, x10Ref.Length);
            VerifyModelForOutVar(model, x10Decl[0], x10Ref[0]);
            VerifyModelForOutVar(model, x10Decl[1], x10Ref[1]);

            var x11Decl = GetOutVarDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(3, x11Ref.Length);
            VerifyNotAnOutLocal(model, x11Ref[0]);
            VerifyModelForOutVar(model, x11Decl, x11Ref[1]);
            VerifyNotAnOutLocal(model, x11Ref[2]);

            var x12Decl = GetOutVarDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(3, x12Ref.Length);
            VerifyModelForOutVar(model, x12Decl, x12Ref[0]);
            VerifyNotAnOutLocal(model, x12Ref[1]);
            VerifyNotAnOutLocal(model, x12Ref[2]);
        }

        [Fact]
        public void Lambda_01()
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
        System.Func<bool> l = () => TakeOutParam(1, out int x1) && Dummy(x1); 
        return l();
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void Scope_LocalDeclarationStmt_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        var d = Dummy(TakeOutParam(true, out var x1), x1);
    }
    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        var d = Dummy(TakeOutParam(true, out var x4), x4);
    }

    void Test6()
    {
        var d = Dummy(x6 && TakeOutParam(true, out var x6));
    }

    void Test8()
    {
        var d = Dummy(TakeOutParam(true, out var x8), x8);
        System.Console.WriteLine(x8);
    }

    void Test14()
    {
        var d = Dummy(TakeOutParam(1, out var x14), 
                      TakeOutParam(2, out var x14), 
                      x14);
    }

    static bool TakeOutParam(object y, out object x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (19,50): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         var d = Dummy(TakeOutParam(true, out var x4), x4);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(19, 50),
                // (24,23): error CS0841: Cannot use local variable 'x6' before it is declared
                //         var d = Dummy(x6 && TakeOutParam(true, out var x6));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(24, 23),
                // (30,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(30, 34),
                // (36,47): error CS0128: A local variable named 'x14' is already defined in this scope
                //                       TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(36, 47)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAnOutLocal(model, x4Ref[0]);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").Single();
            VerifyModelForOutVar(model, x6Decl, x6Ref);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForOutVar(model, x8Decl, x8Ref[0]);
            VerifyNotInScope(model, x8Ref[1]);

            var x14Decl = GetOutVarDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").Single();
            Assert.Equal(2, x14Decl.Length);
            VerifyModelForOutVar(model, x14Decl[0], x14Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void Scope_LocalDeclarationStmt_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        object d = Dummy(TakeOutParam(true, out var x1), x1);
    }
    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        object d = Dummy(TakeOutParam(true, out var x4), x4);
    }

    void Test6()
    {
        object d = Dummy(x6 && TakeOutParam(true, out var x6));
    }

    void Test8()
    {
        object d = Dummy(TakeOutParam(true, out var x8), x8);
        System.Console.WriteLine(x8);
    }

    void Test14()
    {
        object d = Dummy(TakeOutParam(1, out var x14), 
                         TakeOutParam(2, out var x14), 
                         x14);
    }

    static bool TakeOutParam(object y, out object x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (19,53): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         object d = Dummy(TakeOutParam(true, out var x4), x4);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(19, 53),
                // (24,26): error CS0841: Cannot use local variable 'x6' before it is declared
                //         object d = Dummy(x6 && TakeOutParam(true, out var x6));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(24, 26),
                // (30,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(30, 34),
                // (36,50): error CS0128: A local variable named 'x14' is already defined in this scope
                //                          TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(36, 50)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAnOutLocal(model, x4Ref[0]);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1]);

            var x6Decl = GetOutVarDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").Single();
            VerifyModelForOutVar(model, x6Decl, x6Ref);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForOutVar(model, x8Decl, x8Ref[0]);
            VerifyNotInScope(model, x8Ref[1]);

            var x14Decl = GetOutVarDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").Single();
            Assert.Equal(2, x14Decl.Length);
            VerifyModelForOutVar(model, x14Decl[0], x14Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void Scope_LocalDeclarationStmt_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        var x1 = 
                 Dummy(TakeOutParam(true, out var x1), x1);
        Dummy(x1);
    }

    void Test2()
    {
        object x2 = 
                    Dummy(TakeOutParam(true, out var x2), x2);
        Dummy(x2);
    }

    static bool TakeOutParam(object y, out object x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (13,51): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                  Dummy(TakeOutParam(true, out var x1), x1);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(13, 51),
                // (20,54): error CS0136: A local or parameter named 'x2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                     Dummy(TakeOutParam(true, out var x2), x2);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x2").WithArguments("x2").WithLocation(20, 54)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref[0]);
            VerifyNotAnOutLocal(model, x1Ref[1]);

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForOutVar(model, x2Decl, x2Ref[0]);
            VerifyNotAnOutLocal(model, x2Ref[1]);
        }

        [Fact]
        public void Scope_LocalDeclarationStmt_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

   object Dummy(params object[] x) {return null;}

    void Test1()
    {
        object d = Dummy(TakeOutParam(true, out var x1), x1), 
               x1 = Dummy(x1);
        Dummy(x1);
    }

    void Test2()
    {
        object d1 = Dummy(TakeOutParam(true, out var x2), x2), 
               d2 = Dummy(TakeOutParam(true, out var x2), x2);
    }

    void Test3()
    {
        object d1 = Dummy(TakeOutParam(true, out var x3), x3), 
               d2 = Dummy(x3);
    }

    void Test4()
    {
        object d1 = Dummy(x4), 
               d2 = Dummy(TakeOutParam(true, out var x4), x4);
    }

    static bool TakeOutParam(object y, out object x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (12,53): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         object d = Dummy(TakeOutParam(true, out var x1), x1), 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(12, 53),
                // (20,54): error CS0128: A local variable named 'x2' is already defined in this scope
                //                d2 = Dummy(TakeOutParam(true, out var x2), x2);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(20, 54),
                // (31,27): error CS0841: Cannot use local variable 'x4' before it is declared
                //         object d1 = Dummy(x4), 
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(31, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref[0], x1Ref[1]);
            VerifyNotAnOutLocal(model, x1Ref[2]);

            var x2Decl = GetOutVarDeclarations(tree, "x2").ToArray();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Decl.Length);
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForOutVar(model, x2Decl[0], x2Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x2Decl[1]);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyModelForOutVar(model, x4Decl, x4Ref);
        }

        [Fact]
        public void LocalDeclarationStmt_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        object d1 = Dummy(new C(""a""), TakeOutParam(new C(""b""), out var x1), x1),
               d2 = Dummy(new C(""c""), TakeOutParam(new C(""d""), out var x2), x2);
        System.Console.WriteLine(d1);
        System.Console.WriteLine(d2);
    }

    static object Dummy(object x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}

class C
{
    private readonly string _val;

    public C(string val)
    {
        _val = val;
    }

    public override string ToString()
    {
        return _val;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"b
d
a
c");
        }

        [Fact]
        public void Scope_Lock_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        lock (Dummy(TakeOutParam(true, out var x1) && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        lock (Dummy(TakeOutParam(true, out var x2) && x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        lock (Dummy(TakeOutParam(true, out var x4) && x4))
            Dummy(x4);
    }

    void Test6()
    {
        lock (Dummy(x6 && TakeOutParam(true, out var x6)))
            Dummy(x6);
    }

    void Test7()
    {
        lock (Dummy(TakeOutParam(true, out var x7) && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        lock (Dummy(TakeOutParam(true, out var x8) && x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        lock (Dummy(TakeOutParam(true, out var x9) && x9))
        {   
            Dummy(x9);
            lock (Dummy(TakeOutParam(true, out var x9) && x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        lock (Dummy(TakeOutParam(y10, out var x10)))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    lock (Dummy(TakeOutParam(y11, out var x11)))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        lock (Dummy(TakeOutParam(y12, out var x12)))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    lock (Dummy(TakeOutParam(y13, out var x13)))
    //        let y13 = 12;
    //}

    void Test14()
    {
        lock (Dummy(TakeOutParam(1, out var x14), 
                    TakeOutParam(2, out var x14), 
                    x14))
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam(bool y, out bool x) 
    {
        x = y;
        return true;
    }

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
                // (29,48): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         lock (Dummy(TakeOutParam(true, out var x4) && x4))
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 48),
                // (35,21): error CS0841: Cannot use local variable 'x6' before it is declared
                //         lock (Dummy(x6 && TakeOutParam(true, out var x6)))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 21),
                // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
                // (53,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
                // (61,52): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             lock (Dummy(TakeOutParam(true, out var x9) && x9)) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 52),
                // (68,34): error CS0103: The name 'y10' does not exist in the current context
                //         lock (Dummy(TakeOutParam(y10, out var x10)))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 34),
                // (86,34): error CS0103: The name 'y12' does not exist in the current context
                //         lock (Dummy(TakeOutParam(y12, out var x12)))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 34),
                // (99,45): error CS0128: A local variable named 'x14' is already defined in this scope
                //                     TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 45)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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
        public void Lock_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        lock (Dummy(TakeOutParam(""lock"", out var x1), x1))
        {
            System.Console.WriteLine(x1);
        }
    }

    static object Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new object();
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"lock
lock");
        }

        [Fact]
        public void Scope_ParameterDefault_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Test3(bool p = TakeOutParam(3, out int x3) && x3 > 0)
    {}

    void Test4(bool p = x4 && TakeOutParam(4, out int x4))
    {}

    void Test5(bool p = TakeOutParam(51, out int x5) && 
                        TakeOutParam(52, out int x5) && 
                        x5 > 0)
    {}

    void Test61(bool p1 = TakeOutParam(6, out int x6) && x6 > 0, bool p2 = TakeOutParam(6, out int x6) && x6 > 0)
    {}

    void Test71(bool p = TakeOutParam(7, out int x7) && x7 > 0)
    {
    }

    void Test72(bool p = x7 > 2)
    {}

    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (8,25): error CS1736: Default parameter value for 'p' must be a compile-time constant
                //     void Test3(bool p = TakeOutParam(3, out int x3) && x3 > 0)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "TakeOutParam(3, out int x3) && x3 > 0").WithArguments("p").WithLocation(8, 25),
                // (11,25): error CS0841: Cannot use local variable 'x4' before it is declared
                //     void Test4(bool p = x4 && TakeOutParam(4, out int x4))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(11, 25),
                // (11,21): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'bool'
                //     void Test4(bool p = x4 && TakeOutParam(4, out int x4))
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p").WithArguments("?", "bool").WithLocation(11, 21),
                // (15,50): error CS0128: A local variable named 'x5' is already defined in this scope
                //                         TakeOutParam(52, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(15, 50),
                // (14,25): error CS1736: Default parameter value for 'p' must be a compile-time constant
                //     void Test5(bool p = TakeOutParam(51, out int x5) && 
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"TakeOutParam(51, out int x5) && 
                        TakeOutParam(52, out int x5) && 
                        x5 > 0").WithArguments("p").WithLocation(14, 25),
                // (19,27): error CS1736: Default parameter value for 'p1' must be a compile-time constant
                //     void Test61(bool p1 = TakeOutParam(6, out int x6) && x6 > 0, bool p2 = TakeOutParam(6, out int x6) && x6 > 0)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "TakeOutParam(6, out int x6) && x6 > 0").WithArguments("p1").WithLocation(19, 27),
                // (19,76): error CS1736: Default parameter value for 'p2' must be a compile-time constant
                //     void Test61(bool p1 = TakeOutParam(6, out int x6) && x6 > 0, bool p2 = TakeOutParam(6, out int x6) && x6 > 0)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "TakeOutParam(6, out int x6) && x6 > 0").WithArguments("p2").WithLocation(19, 76),
                // (22,26): error CS1736: Default parameter value for 'p' must be a compile-time constant
                //     void Test71(bool p = TakeOutParam(7, out int x7) && x7 > 0)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "TakeOutParam(7, out int x7) && x7 > 0").WithArguments("p").WithLocation(22, 26),
                // (26,26): error CS0103: The name 'x7' does not exist in the current context
                //     void Test72(bool p = x7 > 2)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(26, 26),
                // (29,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(29, 27)
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
        }

        [Fact]
        public void Scope_PropertyInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Test3 {get;} = TakeOutParam(3, out int x3) && x3 > 0;

    bool Test4 {get;} = x4 && TakeOutParam(4, out int x4);

    bool Test5 {get;} = TakeOutParam(51, out int x5) && 
                 TakeOutParam(52, out int x5) && 
                 x5 > 0;

    bool Test61 {get;} = TakeOutParam(6, out int x6) && x6 > 0; bool Test62 {get;} = TakeOutParam(6, out int x6) && x6 > 0;

    bool Test71 {get;} = TakeOutParam(7, out int x7) && x7 > 0; 
    bool Test72 {get;} = Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}

    static bool TakeOutParam(int y, out int x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (10,25): error CS0841: Cannot use local variable 'x4' before it is declared
                //     bool Test4 {get;} = x4 && TakeOutParam(4, out int x4);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 25),
                // (13,43): error CS0128: A local variable named 'x5' is already defined in this scope
                //                  TakeOutParam(52, out int x5) && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 43),
                // (19,32): error CS0103: The name 'x7' does not exist in the current context
                //     bool Test72 {get;} = Dummy(x7, 2); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 32),
                // (20,27): error CS0103: The name 'x7' does not exist in the current context
                //     void Test73() { Dummy(x7, 3); } 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27)
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
        }

        [Fact]
        public void PropertyInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1);
    }

    static bool Test1 {get;} = TakeOutParam(1, out int x1) && Dummy(x1); 

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void Scope_Query_01()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from x in new[] { TakeOutParam(1, out var y1) ? y1 : 0, y1}
                  select x + y1;

        Dummy(y1); 
    }

    void Test2()
    {
        var res = from x1 in new[] { TakeOutParam(1, out var y2) ? y2 : 0}
                  from x2 in new[] { TakeOutParam(x1, out var z2) ? z2 : 0, z2, y2}
                  select x1 + x2 + y2 + 
                         z2;

        Dummy(z2); 
    }

    void Test3()
    {
        var res = from x1 in new[] { TakeOutParam(1, out var y3) ? y3 : 0}
                  let x2 = TakeOutParam(x1, out var z3) && z3 > 0 && y3 < 0 
                  select new { x1, x2, y3,
                               z3};

        Dummy(z3); 
    }

    void Test4()
    {
        var res = from x1 in new[] { TakeOutParam(1, out var y4) ? y4 : 0}
                  join x2 in new[] { TakeOutParam(2, out var z4) ? z4 : 0, z4, y4}
                            on x1 + y4 + z4 + (TakeOutParam(3, out var u4) ? u4 : 0) + 
                                  v4 
                               equals x2 + y4 + z4 + (TakeOutParam(4, out var v4) ? v4 : 0) +
                                  u4 
                  select new { x1, x2, y4, z4, 
                               u4, v4 };

        Dummy(z4); 
        Dummy(u4); 
        Dummy(v4); 
    }

    void Test5()
    {
        var res = from x1 in new[] { TakeOutParam(1, out var y5) ? y5 : 0}
                  join x2 in new[] { TakeOutParam(2, out var z5) ? z5 : 0, z5, y5}
                            on x1 + y5 + z5 + (TakeOutParam(3, out var u5) ? u5 : 0) + 
                                  v5 
                               equals x2 + y5 + z5 + (TakeOutParam(4, out var v5) ? v5 : 0) +
                                  u5 
                  into g
                  select new { x1, y5, z5, g,
                               u5, v5 };

        Dummy(z5); 
        Dummy(u5); 
        Dummy(v5); 
    }

    void Test6()
    {
        var res = from x in new[] { TakeOutParam(1, out var y6) ? y6 : 0}
                  where x > y6 && TakeOutParam(1, out var z6) && z6 == 1
                  select x + y6 +
                         z6;

        Dummy(z6); 
    }

    void Test7()
    {
        var res = from x in new[] { TakeOutParam(1, out var y7) ? y7 : 0}
                  orderby x > y7 && TakeOutParam(1, out var z7) && z7 == 
                          u7,
                          x > y7 && TakeOutParam(1, out var u7) && u7 == 
                          z7   
                  select x + y7 +
                         z7 + u7;

        Dummy(z7); 
        Dummy(u7); 
    }

    void Test8()
    {
        var res = from x in new[] { TakeOutParam(1, out var y8) ? y8 : 0}
                  select x > y8 && TakeOutParam(1, out var z8) && z8 == 1;

        Dummy(z8); 
    }

    void Test9()
    {
        var res = from x in new[] { TakeOutParam(1, out var y9) ? y9 : 0}
                  group x > y9 && TakeOutParam(1, out var z9) && z9 == 
                        u9
                  by
                        x > y9 && TakeOutParam(1, out var u9) && u9 == 
                        z9;   

        Dummy(z9); 
        Dummy(u9); 
    }

    void Test10()
    {
        var res = from x1 in new[] { TakeOutParam(1, out var y10) ? y10 : 0}
                  from y10 in new[] { 1 }
                  select x1 + y10;
    }

    void Test11()
    {
        var res = from x1 in new[] { TakeOutParam(1, out var y11) ? y11 : 0}
                  let y11 = x1 + 1
                  select x1 + y11;
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (17,15): error CS0103: The name 'y1' does not exist in the current context
                //         Dummy(y1); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y1").WithArguments("y1").WithLocation(17, 15),
                // (25,26): error CS0103: The name 'z2' does not exist in the current context
                //                          z2;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(25, 26),
                // (27,15): error CS0103: The name 'z2' does not exist in the current context
                //         Dummy(z2); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(27, 15),
                // (35,32): error CS0103: The name 'z3' does not exist in the current context
                //                                z3};
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(35, 32),
                // (37,15): error CS0103: The name 'z3' does not exist in the current context
                //         Dummy(z3); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(37, 15),
                // (45,35): error CS0103: The name 'v4' does not exist in the current context
                //                                   v4 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(45, 35),
                // (47,35): error CS1938: The name 'u4' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
                //                                   u4 
                Diagnostic(ErrorCode.ERR_QueryInnerKey, "u4").WithArguments("u4").WithLocation(47, 35),
                // (49,32): error CS0103: The name 'u4' does not exist in the current context
                //                                u4, v4 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(49, 32),
                // (49,36): error CS0103: The name 'v4' does not exist in the current context
                //                                u4, v4 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(49, 36),
                // (51,15): error CS0103: The name 'z4' does not exist in the current context
                //         Dummy(z4); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z4").WithArguments("z4").WithLocation(51, 15),
                // (52,15): error CS0103: The name 'u4' does not exist in the current context
                //         Dummy(u4); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(52, 15),
                // (53,15): error CS0103: The name 'v4' does not exist in the current context
                //         Dummy(v4); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(53, 15),
                // (61,35): error CS0103: The name 'v5' does not exist in the current context
                //                                   v5 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(61, 35),
                // (63,35): error CS1938: The name 'u5' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
                //                                   u5 
                Diagnostic(ErrorCode.ERR_QueryInnerKey, "u5").WithArguments("u5").WithLocation(63, 35),
                // (66,32): error CS0103: The name 'u5' does not exist in the current context
                //                                u5, v5 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(66, 32),
                // (66,36): error CS0103: The name 'v5' does not exist in the current context
                //                                u5, v5 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(66, 36),
                // (68,15): error CS0103: The name 'z5' does not exist in the current context
                //         Dummy(z5); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z5").WithArguments("z5").WithLocation(68, 15),
                // (69,15): error CS0103: The name 'u5' does not exist in the current context
                //         Dummy(u5); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(69, 15),
                // (70,15): error CS0103: The name 'v5' does not exist in the current context
                //         Dummy(v5); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(70, 15),
                // (78,26): error CS0103: The name 'z6' does not exist in the current context
                //                          z6;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(78, 26),
                // (80,15): error CS0103: The name 'z6' does not exist in the current context
                //         Dummy(z6); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(80, 15),
                // (87,27): error CS0103: The name 'u7' does not exist in the current context
                //                           u7,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(87, 27),
                // (89,27): error CS0103: The name 'z7' does not exist in the current context
                //                           z7   
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(89, 27),
                // (91,31): error CS0103: The name 'u7' does not exist in the current context
                //                          z7 + u7;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(91, 31),
                // (91,26): error CS0103: The name 'z7' does not exist in the current context
                //                          z7 + u7;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(91, 26),
                // (93,15): error CS0103: The name 'z7' does not exist in the current context
                //         Dummy(z7); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(93, 15),
                // (94,15): error CS0103: The name 'u7' does not exist in the current context
                //         Dummy(u7); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(94, 15),
                // (88,68): error CS0165: Use of unassigned local variable 'u7'
                //                           x > y7 && TakeOutParam(1, out var u7) && u7 == 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "u7").WithArguments("u7").WithLocation(88, 68),
                // (102,15): error CS0103: The name 'z8' does not exist in the current context
                //         Dummy(z8); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z8").WithArguments("z8").WithLocation(102, 15),
                // (112,25): error CS0103: The name 'z9' does not exist in the current context
                //                         z9;   
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(112, 25),
                // (109,25): error CS0103: The name 'u9' does not exist in the current context
                //                         u9
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u9").WithArguments("u9").WithLocation(109, 25),
                // (114,15): error CS0103: The name 'z9' does not exist in the current context
                //         Dummy(z9); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(114, 15),
                // (115,15): error CS0103: The name 'u9' does not exist in the current context
                //         Dummy(u9); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u9").WithArguments("u9").WithLocation(115, 15),
                // (108,66): error CS0165: Use of unassigned local variable 'z9'
                //                   group x > y9 && TakeOutParam(1, out var z9) && z9 == 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "z9").WithArguments("z9").WithLocation(108, 66),
                // (121,24): error CS1931: The range variable 'y10' conflicts with a previous declaration of 'y10'
                //                   from y10 in new[] { 1 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y10").WithArguments("y10").WithLocation(121, 24),
                // (128,23): error CS1931: The range variable 'y11' conflicts with a previous declaration of 'y11'
                //                   let y11 = x1 + 1
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y11").WithArguments("y11").WithLocation(128, 23)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var y1Decl = GetOutVarDeclarations(tree, "y1").Single();
            var y1Ref = GetReferences(tree, "y1").ToArray();
            Assert.Equal(4, y1Ref.Length);
            VerifyModelForOutVar(model, y1Decl, y1Ref[0], y1Ref[1], y1Ref[2]);
            VerifyNotInScope(model, y1Ref[3]);

            var y2Decl = GetOutVarDeclarations(tree, "y2").Single();
            var y2Ref = GetReferences(tree, "y2").ToArray();
            Assert.Equal(3, y2Ref.Length);
            VerifyModelForOutVar(model, y2Decl, y2Ref);

            var z2Decl = GetOutVarDeclarations(tree, "z2").Single();
            var z2Ref = GetReferences(tree, "z2").ToArray();
            Assert.Equal(4, z2Ref.Length);
            VerifyModelForOutVar(model, z2Decl, z2Ref[0], z2Ref[1]);
            VerifyNotInScope(model, z2Ref[2]);
            VerifyNotInScope(model, z2Ref[3]);

            var y3Decl = GetOutVarDeclarations(tree, "y3").Single();
            var y3Ref = GetReferences(tree, "y3").ToArray();
            Assert.Equal(3, y3Ref.Length);
            VerifyModelForOutVar(model, y3Decl, y3Ref);

            var z3Decl = GetOutVarDeclarations(tree, "z3").Single();
            var z3Ref = GetReferences(tree, "z3").ToArray();
            Assert.Equal(3, z3Ref.Length);
            VerifyModelForOutVar(model, z3Decl, z3Ref[0]);
            VerifyNotInScope(model, z3Ref[1]);
            VerifyNotInScope(model, z3Ref[2]);

            var y4Decl = GetOutVarDeclarations(tree, "y4").Single();
            var y4Ref = GetReferences(tree, "y4").ToArray();
            Assert.Equal(5, y4Ref.Length);
            VerifyModelForOutVar(model, y4Decl, y4Ref);

            var z4Decl = GetOutVarDeclarations(tree, "z4").Single();
            var z4Ref = GetReferences(tree, "z4").ToArray();
            Assert.Equal(6, z4Ref.Length);
            VerifyModelForOutVar(model, z4Decl, z4Ref[0], z4Ref[1], z4Ref[2], z4Ref[3], z4Ref[4]);
            VerifyNotInScope(model, z4Ref[5]);

            var u4Decl = GetOutVarDeclarations(tree, "u4").Single();
            var u4Ref = GetReferences(tree, "u4").ToArray();
            Assert.Equal(4, u4Ref.Length);
            VerifyModelForOutVar(model, u4Decl, u4Ref[0]);
            VerifyNotInScope(model, u4Ref[1]);
            VerifyNotInScope(model, u4Ref[2]);
            VerifyNotInScope(model, u4Ref[3]);

            var v4Decl = GetOutVarDeclarations(tree, "v4").Single();
            var v4Ref = GetReferences(tree, "v4").ToArray();
            Assert.Equal(4, v4Ref.Length);
            VerifyNotInScope(model, v4Ref[0]);
            VerifyModelForOutVar(model, v4Decl, v4Ref[1]);
            VerifyNotInScope(model, v4Ref[2]);
            VerifyNotInScope(model, v4Ref[3]);

            var y5Decl = GetOutVarDeclarations(tree, "y5").Single();
            var y5Ref = GetReferences(tree, "y5").ToArray();
            Assert.Equal(5, y5Ref.Length);
            VerifyModelForOutVar(model, y5Decl, y5Ref);

            var z5Decl = GetOutVarDeclarations(tree, "z5").Single();
            var z5Ref = GetReferences(tree, "z5").ToArray();
            Assert.Equal(6, z5Ref.Length);
            VerifyModelForOutVar(model, z5Decl, z5Ref[0], z5Ref[1], z5Ref[2], z5Ref[3], z5Ref[4]);
            VerifyNotInScope(model, z5Ref[5]);

            var u5Decl = GetOutVarDeclarations(tree, "u5").Single();
            var u5Ref = GetReferences(tree, "u5").ToArray();
            Assert.Equal(4, u5Ref.Length);
            VerifyModelForOutVar(model, u5Decl, u5Ref[0]);
            VerifyNotInScope(model, u5Ref[1]);
            VerifyNotInScope(model, u5Ref[2]);
            VerifyNotInScope(model, u5Ref[3]);

            var v5Decl = GetOutVarDeclarations(tree, "v5").Single();
            var v5Ref = GetReferences(tree, "v5").ToArray();
            Assert.Equal(4, v5Ref.Length);
            VerifyNotInScope(model, v5Ref[0]);
            VerifyModelForOutVar(model, v5Decl, v5Ref[1]);
            VerifyNotInScope(model, v5Ref[2]);
            VerifyNotInScope(model, v5Ref[3]);

            var y6Decl = GetOutVarDeclarations(tree, "y6").Single();
            var y6Ref = GetReferences(tree, "y6").ToArray();
            Assert.Equal(3, y6Ref.Length);
            VerifyModelForOutVar(model, y6Decl, y6Ref);

            var z6Decl = GetOutVarDeclarations(tree, "z6").Single();
            var z6Ref = GetReferences(tree, "z6").ToArray();
            Assert.Equal(3, z6Ref.Length);
            VerifyModelForOutVar(model, z6Decl, z6Ref[0]);
            VerifyNotInScope(model, z6Ref[1]);
            VerifyNotInScope(model, z6Ref[2]);

            var y7Decl = GetOutVarDeclarations(tree, "y7").Single();
            var y7Ref = GetReferences(tree, "y7").ToArray();
            Assert.Equal(4, y7Ref.Length);
            VerifyModelForOutVar(model, y7Decl, y7Ref);

            var z7Decl = GetOutVarDeclarations(tree, "z7").Single();
            var z7Ref = GetReferences(tree, "z7").ToArray();
            Assert.Equal(4, z7Ref.Length);
            VerifyModelForOutVar(model, z7Decl, z7Ref[0]);
            VerifyNotInScope(model, z7Ref[1]);
            VerifyNotInScope(model, z7Ref[2]);
            VerifyNotInScope(model, z7Ref[3]);

            var u7Decl = GetOutVarDeclarations(tree, "u7").Single();
            var u7Ref = GetReferences(tree, "u7").ToArray();
            Assert.Equal(4, u7Ref.Length);
            VerifyNotInScope(model, u7Ref[0]);
            VerifyModelForOutVar(model, u7Decl, u7Ref[1]);
            VerifyNotInScope(model, u7Ref[2]);
            VerifyNotInScope(model, u7Ref[3]);

            var y8Decl = GetOutVarDeclarations(tree, "y8").Single();
            var y8Ref = GetReferences(tree, "y8").ToArray();
            Assert.Equal(2, y8Ref.Length);
            VerifyModelForOutVar(model, y8Decl, y8Ref);

            var z8Decl = GetOutVarDeclarations(tree, "z8").Single();
            var z8Ref = GetReferences(tree, "z8").ToArray();
            Assert.Equal(2, z8Ref.Length);
            VerifyModelForOutVar(model, z8Decl, z8Ref[0]);
            VerifyNotInScope(model, z8Ref[1]);

            var y9Decl = GetOutVarDeclarations(tree, "y9").Single();
            var y9Ref = GetReferences(tree, "y9").ToArray();
            Assert.Equal(3, y9Ref.Length);
            VerifyModelForOutVar(model, y9Decl, y9Ref);

            var z9Decl = GetOutVarDeclarations(tree, "z9").Single();
            var z9Ref = GetReferences(tree, "z9").ToArray();
            Assert.Equal(3, z9Ref.Length);
            VerifyModelForOutVar(model, z9Decl, z9Ref[0]);
            VerifyNotInScope(model, z9Ref[1]);
            VerifyNotInScope(model, z9Ref[2]);

            var u9Decl = GetOutVarDeclarations(tree, "u9").Single();
            var u9Ref = GetReferences(tree, "u9").ToArray();
            Assert.Equal(3, u9Ref.Length);
            VerifyNotInScope(model, u9Ref[0]);
            VerifyModelForOutVar(model, u9Decl, u9Ref[1]);
            VerifyNotInScope(model, u9Ref[2]);

            var y10Decl = GetOutVarDeclarations(tree, "y10").Single();
            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyModelForOutVar(model, y10Decl, y10Ref[0]);
            VerifyNotAnOutLocal(model, y10Ref[1]);

            var y11Decl = GetOutVarDeclarations(tree, "y11").Single();
            var y11Ref = GetReferences(tree, "y11").ToArray();
            Assert.Equal(2, y11Ref.Length);
            VerifyModelForOutVar(model, y11Decl, y11Ref[0]);
            VerifyNotAnOutLocal(model, y11Ref[1]);
        }

        [Fact]
        public void Scope_Query_03()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test4()
    {
        var res = from x1 in new[] { TakeOutParam(1, out var y4) ? y4 : 0}
                  select x1 into x1
                  join x2 in new[] { TakeOutParam(2, out var z4) ? z4 : 0, z4, y4}
                            on x1 + y4 + z4 + (TakeOutParam(3, out var u4) ? u4 : 0) + 
                                  v4 
                               equals x2 + y4 + z4 + (TakeOutParam(4, out var v4) ? v4 : 0) +
                                  u4 
                  select new { x1, x2, y4, z4, 
                               u4, v4 };

        Dummy(z4); 
        Dummy(u4); 
        Dummy(v4); 
    }

    void Test5()
    {
        var res = from x1 in new[] { TakeOutParam(1, out var y5) ? y5 : 0}
                  select x1 into x1
                  join x2 in new[] { TakeOutParam(2, out var z5) ? z5 : 0, z5, y5}
                            on x1 + y5 + z5 + (TakeOutParam(3, out var u5) ? u5 : 0) + 
                                  v5 
                               equals x2 + y5 + z5 + (TakeOutParam(4 , out var v5) ? v5 : 0) +
                                  u5 
                  into g
                  select new { x1, y5, z5, g,
                               u5, v5 };

        Dummy(z5); 
        Dummy(u5); 
        Dummy(v5); 
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (18,35): error CS0103: The name 'v4' does not exist in the current context
                //                                   v4 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(18, 35),
                // (20,35): error CS1938: The name 'u4' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
                //                                   u4 
                Diagnostic(ErrorCode.ERR_QueryInnerKey, "u4").WithArguments("u4").WithLocation(20, 35),
                // (22,32): error CS0103: The name 'u4' does not exist in the current context
                //                                u4, v4 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(22, 32),
                // (22,36): error CS0103: The name 'v4' does not exist in the current context
                //                                u4, v4 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(22, 36),
                // (24,15): error CS0103: The name 'z4' does not exist in the current context
                //         Dummy(z4); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z4").WithArguments("z4").WithLocation(24, 15),
                // (25,15): error CS0103: The name 'u4' does not exist in the current context
                //         Dummy(u4); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(25, 15),
                // (26,15): error CS0103: The name 'v4' does not exist in the current context
                //         Dummy(v4); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(26, 15),
                // (35,35): error CS0103: The name 'v5' does not exist in the current context
                //                                   v5 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(35, 35),
                // (37,35): error CS1938: The name 'u5' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
                //                                   u5 
                Diagnostic(ErrorCode.ERR_QueryInnerKey, "u5").WithArguments("u5").WithLocation(37, 35),
                // (40,32): error CS0103: The name 'u5' does not exist in the current context
                //                                u5, v5 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(40, 32),
                // (40,36): error CS0103: The name 'v5' does not exist in the current context
                //                                u5, v5 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(40, 36),
                // (42,15): error CS0103: The name 'z5' does not exist in the current context
                //         Dummy(z5); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z5").WithArguments("z5").WithLocation(42, 15),
                // (43,15): error CS0103: The name 'u5' does not exist in the current context
                //         Dummy(u5); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(43, 15),
                // (44,15): error CS0103: The name 'v5' does not exist in the current context
                //         Dummy(v5); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(44, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var y4Decl = GetOutVarDeclarations(tree, "y4").Single();
            var y4Ref = GetReferences(tree, "y4").ToArray();
            Assert.Equal(5, y4Ref.Length);
            VerifyModelForOutVar(model, y4Decl, y4Ref);

            var z4Decl = GetOutVarDeclarations(tree, "z4").Single();
            var z4Ref = GetReferences(tree, "z4").ToArray();
            Assert.Equal(6, z4Ref.Length);
            VerifyModelForOutVar(model, z4Decl, z4Ref[0], z4Ref[1], z4Ref[2], z4Ref[3], z4Ref[4]);
            VerifyNotInScope(model, z4Ref[5]);

            var u4Decl = GetOutVarDeclarations(tree, "u4").Single();
            var u4Ref = GetReferences(tree, "u4").ToArray();
            Assert.Equal(4, u4Ref.Length);
            VerifyModelForOutVar(model, u4Decl, u4Ref[0]);
            VerifyNotInScope(model, u4Ref[1]);
            VerifyNotInScope(model, u4Ref[2]);
            VerifyNotInScope(model, u4Ref[3]);

            var v4Decl = GetOutVarDeclarations(tree, "v4").Single();
            var v4Ref = GetReferences(tree, "v4").ToArray();
            Assert.Equal(4, v4Ref.Length);
            VerifyNotInScope(model, v4Ref[0]);
            VerifyModelForOutVar(model, v4Decl, v4Ref[1]);
            VerifyNotInScope(model, v4Ref[2]);
            VerifyNotInScope(model, v4Ref[3]);

            var y5Decl = GetOutVarDeclarations(tree, "y5").Single();
            var y5Ref = GetReferences(tree, "y5").ToArray();
            Assert.Equal(5, y5Ref.Length);
            VerifyModelForOutVar(model, y5Decl, y5Ref);

            var z5Decl = GetOutVarDeclarations(tree, "z5").Single();
            var z5Ref = GetReferences(tree, "z5").ToArray();
            Assert.Equal(6, z5Ref.Length);
            VerifyModelForOutVar(model, z5Decl, z5Ref[0], z5Ref[1], z5Ref[2], z5Ref[3], z5Ref[4]);
            VerifyNotInScope(model, z5Ref[5]);

            var u5Decl = GetOutVarDeclarations(tree, "u5").Single();
            var u5Ref = GetReferences(tree, "u5").ToArray();
            Assert.Equal(4, u5Ref.Length);
            VerifyModelForOutVar(model, u5Decl, u5Ref[0]);
            VerifyNotInScope(model, u5Ref[1]);
            VerifyNotInScope(model, u5Ref[2]);
            VerifyNotInScope(model, u5Ref[3]);

            var v5Decl = GetOutVarDeclarations(tree, "v5").Single();
            var v5Ref = GetReferences(tree, "v5").ToArray();
            Assert.Equal(4, v5Ref.Length);
            VerifyNotInScope(model, v5Ref[0]);
            VerifyModelForOutVar(model, v5Decl, v5Ref[1]);
            VerifyNotInScope(model, v5Ref[2]);
            VerifyNotInScope(model, v5Ref[3]);
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void Scope_Query_05()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        int y1 = 0, y2 = 0, y3 = 0, y4 = 0, y5 = 0, y6 = 0, y7 = 0, y8 = 0, y9 = 0, y10 = 0, y11 = 0, y12 = 0;

        var res = from x1 in new[] { TakeOutParam(1, out var y1) ? y1 : 0}
                  from x2 in new[] { TakeOutParam(2, out var y2) ? y2 : 0}
                  join x3 in new[] { TakeOutParam(3, out var y3) ? y3 : 0}
                       on TakeOutParam(4, out var y4) ? y4 : 0
                          equals TakeOutParam(5, out var y5) ? y5 : 0
                  where TakeOutParam(6, out var y6) && y6 == 1
                  orderby TakeOutParam(7, out var y7) && y7 > 0, 
                          TakeOutParam(8, out var y8) && y8 > 0 
                  group TakeOutParam(9, out var y9) && y9 > 0 
                  by TakeOutParam(10, out var y10) && y10 > 0
                  into g
                  let x11 = TakeOutParam(11, out var y11) && y11 > 0
                  select TakeOutParam(12, out var y12) && y12 > 0
                  into s
                  select y1 + y2 + y3 + y4 + y5 + y6 + y7 + y8 + y9 + y10 + y11 + y12;

        Dummy(y1, y2, y3, y4, y5, y6, y7, y8, y9, y10, y11, y12); 
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (16,62): error CS0136: A local or parameter named 'y1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         var res = from x1 in new[] { TakeOutParam(1, out var y1) ? y1 : 0}
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y1").WithArguments("y1").WithLocation(16, 62),
                // (17,62): error CS0136: A local or parameter named 'y2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   from x2 in new[] { TakeOutParam(2, out var y2) ? y2 : 0}
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y2").WithArguments("y2").WithLocation(17, 62),
                // (18,62): error CS0136: A local or parameter named 'y3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   join x3 in new[] { TakeOutParam(3, out var y3) ? y3 : 0}
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y3").WithArguments("y3").WithLocation(18, 62),
                // (19,51): error CS0136: A local or parameter named 'y4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                        on TakeOutParam(4, out var y4) ? y4 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y4").WithArguments("y4").WithLocation(19, 51),
                // (20,58): error CS0136: A local or parameter named 'y5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           equals TakeOutParam(5, out var y5) ? y5 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y5").WithArguments("y5").WithLocation(20, 58),
                // (21,49): error CS0136: A local or parameter named 'y6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   where TakeOutParam(6, out var y6) && y6 == 1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y6").WithArguments("y6").WithLocation(21, 49),
                // (22,51): error CS0136: A local or parameter named 'y7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   orderby TakeOutParam(7, out var y7) && y7 > 0, 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y7").WithArguments("y7").WithLocation(22, 51),
                // (23,51): error CS0136: A local or parameter named 'y8' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           TakeOutParam(8, out var y8) && y8 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y8").WithArguments("y8").WithLocation(23, 51),
                // (25,47): error CS0136: A local or parameter named 'y10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   by TakeOutParam(10, out var y10) && y10 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y10").WithArguments("y10").WithLocation(25, 47),
                // (24,49): error CS0136: A local or parameter named 'y9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   group TakeOutParam(9, out var y9) && y9 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y9").WithArguments("y9").WithLocation(24, 49),
                // (27,54): error CS0136: A local or parameter named 'y11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   let x11 = TakeOutParam(11, out var y11) && y11 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y11").WithArguments("y11").WithLocation(27, 54),
                // (28,51): error CS0136: A local or parameter named 'y12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   select TakeOutParam(12, out var y12) && y12 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y12").WithArguments("y12").WithLocation(28, 51)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 13; i++)
            {
                var id = "y" + i;
                var yDecl = GetOutVarDeclarations(tree, id).Single();
                var yRef = GetReferences(tree, id).ToArray();
                Assert.Equal(3, yRef.Length);
                VerifyModelForOutVar(model, yDecl, yRef[0]);
                VerifyNotAnOutLocal(model, yRef[2]);

                switch (i)
                {
                    case 1:
                    case 3:
                    case 12:
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAnOutLocal(model, yRef[1]);
                        break;
                    default:
                        VerifyNotAnOutLocal(model, yRef[1]);
                        break;
                }

            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void Scope_Query_06()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        Dummy(TakeOutParam(out int y1), 
              TakeOutParam(out int y2), 
              TakeOutParam(out int y3), 
              TakeOutParam(out int y4), 
              TakeOutParam(out int y5), 
              TakeOutParam(out int y6), 
              TakeOutParam(out int y7), 
              TakeOutParam(out int y8), 
              TakeOutParam(out int y9), 
              TakeOutParam(out int y10), 
              TakeOutParam(out int y11), 
              TakeOutParam(out int y12),
                  from x1 in new[] { TakeOutParam(1, out var y1) ? y1 : 0}
                  from x2 in new[] { TakeOutParam(2, out var y2) ? y2 : 0}
                  join x3 in new[] { TakeOutParam(3, out var y3) ? y3 : 0}
                       on TakeOutParam(4, out var y4) ? y4 : 0
                          equals TakeOutParam(5, out var y5) ? y5 : 0
                  where TakeOutParam(6, out var y6) && y6 == 1
                  orderby TakeOutParam(7, out var y7) && y7 > 0, 
                          TakeOutParam(8, out var y8) && y8 > 0 
                  group TakeOutParam(9, out var y9) && y9 > 0 
                  by TakeOutParam(10, out var y10) && y10 > 0
                  into g
                  let x11 = TakeOutParam(11, out var y11) && y11 > 0
                  select TakeOutParam(12, out var y12) && y12 > 0
                  into s
                  select y1 + y2 + y3 + y4 + y5 + y6 + y7 + y8 + y9 + y10 + y11 + y12);
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }

    static bool TakeOutParam(out int x) 
    {
        x = 0;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (26,62): error CS0128: A local variable named 'y1' is already defined in this scope
                //                   from x1 in new[] { TakeOutParam(1, out var y1) ? y1 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y1").WithArguments("y1").WithLocation(26, 62),
                // (27,62): error CS0136: A local or parameter named 'y2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   from x2 in new[] { TakeOutParam(2, out var y2) ? y2 : 0}
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y2").WithArguments("y2").WithLocation(27, 62),
                // (28,62): error CS0128: A local variable named 'y3' is already defined in this scope
                //                   join x3 in new[] { TakeOutParam(3, out var y3) ? y3 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y3").WithArguments("y3").WithLocation(28, 62),
                // (29,51): error CS0136: A local or parameter named 'y4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                        on TakeOutParam(4, out var y4) ? y4 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y4").WithArguments("y4").WithLocation(29, 51),
                // (30,58): error CS0136: A local or parameter named 'y5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           equals TakeOutParam(5, out var y5) ? y5 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y5").WithArguments("y5").WithLocation(30, 58),
                // (31,49): error CS0136: A local or parameter named 'y6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   where TakeOutParam(6, out var y6) && y6 == 1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y6").WithArguments("y6").WithLocation(31, 49),
                // (32,51): error CS0136: A local or parameter named 'y7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   orderby TakeOutParam(7, out var y7) && y7 > 0, 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y7").WithArguments("y7").WithLocation(32, 51),
                // (33,51): error CS0136: A local or parameter named 'y8' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           TakeOutParam(8, out var y8) && y8 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y8").WithArguments("y8").WithLocation(33, 51),
                // (35,47): error CS0136: A local or parameter named 'y10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   by TakeOutParam(10, out var y10) && y10 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y10").WithArguments("y10").WithLocation(35, 47),
                // (34,49): error CS0136: A local or parameter named 'y9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   group TakeOutParam(9, out var y9) && y9 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y9").WithArguments("y9").WithLocation(34, 49),
                // (37,54): error CS0136: A local or parameter named 'y11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   let x11 = TakeOutParam(11, out var y11) && y11 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y11").WithArguments("y11").WithLocation(37, 54),
                // (38,51): error CS0136: A local or parameter named 'y12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   select TakeOutParam(12, out var y12) && y12 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y12").WithArguments("y12").WithLocation(38, 51)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 13; i++)
            {
                var id = "y" + i;
                var yDecl = GetOutVarDeclarations(tree, id).ToArray();
                var yRef = GetReferences(tree, id).ToArray();
                Assert.Equal(2, yDecl.Length);
                Assert.Equal(2, yRef.Length);

                switch (i)
                {
                    case 1:
                    case 3:
                        VerifyModelForOutVar(model, yDecl[0], yRef);
                        VerifyModelForOutVarDuplicateInSameScope(model, yDecl[1]);
                        break;
                    case 12:
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyModelForOutVar(model, yDecl[0], yRef[1]);
                        VerifyModelForOutVar(model, yDecl[1], yRef[0]);
                        break;

                    default:
                        VerifyModelForOutVar(model, yDecl[0], yRef[1]);
                        VerifyModelForOutVar(model, yDecl[1], yRef[0]);
                        break;
                }
            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void Scope_Query_07()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        Dummy(TakeOutParam(out int y3), 
                  from x1 in new[] { 0 }
                  select x1
                  into x1
                  join x3 in new[] { TakeOutParam(3, out var y3) ? y3 : 0}
                       on x1 equals x3
                  select y3);
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }

    static bool TakeOutParam(out int x) 
    {
        x = 0;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (18,62): error CS0128: A local variable named 'y3' is already defined in this scope
                //                   join x3 in new[] { TakeOutParam(3, out var y3) ? y3 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y3").WithArguments("y3").WithLocation(18, 62)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            const string id = "y3";
            var yDecl = GetOutVarDeclarations(tree, id).ToArray();
            var yRef = GetReferences(tree, id).ToArray();
            Assert.Equal(2, yDecl.Length);
            Assert.Equal(2, yRef.Length);
            VerifyModelForOutVar(model, yDecl[0], yRef[1]);
            // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
            //VerifyModelForOutVar(model, yDecl[1], yRef[0]);
        }

        [Fact]
        public void Scope_Query_08()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from x1 in new[] { Dummy(TakeOutParam(out var y1), 
                                           TakeOutParam(out var y2),
                                           TakeOutParam(out var y3),
                                           TakeOutParam(out var y4)
                                          ) ? 1 : 0}
                  from y1 in new[] { 1 }
                  join y2 in new[] { 0 }
                       on y1 equals y2
                  let y3 = 0
                  group y3 
                  by x1
                  into y4
                  select y4;
    }

    static bool TakeOutParam(out int x) 
    {
        x = 0;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (19,24): error CS1931: The range variable 'y1' conflicts with a previous declaration of 'y1'
                //                   from y1 in new[] { 1 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y1").WithArguments("y1").WithLocation(19, 24),
                // (20,24): error CS1931: The range variable 'y2' conflicts with a previous declaration of 'y2'
                //                   join y2 in new[] { 0 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y2").WithArguments("y2").WithLocation(20, 24),
                // (22,23): error CS1931: The range variable 'y3' conflicts with a previous declaration of 'y3'
                //                   let y3 = 0
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y3").WithArguments("y3").WithLocation(22, 23),
                // (25,24): error CS1931: The range variable 'y4' conflicts with a previous declaration of 'y4'
                //                   into y4
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y4").WithArguments("y4").WithLocation(25, 24)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 5; i++)
            {
                var id = "y" + i;
                var yDecl = GetOutVarDeclarations(tree, id).Single();
                var yRef = GetReferences(tree, id).Single();
                VerifyModelForOutVar(model, yDecl);
                VerifyNotAnOutLocal(model, yRef);
            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void Scope_Query_09()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from y1 in new[] { 0 }
                  join y2 in new[] { 0 }
                       on y1 equals y2
                  let y3 = 0
                  group y3 
                  by 1
                  into y4
                  select y4 == null ? 1 : 0
                  into x2
                  join y5 in new[] { Dummy(TakeOutParam(out var y1), 
                                           TakeOutParam(out var y2),
                                           TakeOutParam(out var y3),
                                           TakeOutParam(out var y4),
                                           TakeOutParam(out var y5)
                                          ) ? 1 : 0 }
                       on x2 equals y5
                  select x2;
    }

    static bool TakeOutParam(out int x) 
    {
        x = 0;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (14,24): error CS1931: The range variable 'y1' conflicts with a previous declaration of 'y1'
                //         var res = from y1 in new[] { 0 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y1").WithArguments("y1").WithLocation(14, 24),
                // (15,24): error CS1931: The range variable 'y2' conflicts with a previous declaration of 'y2'
                //                   join y2 in new[] { 0 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y2").WithArguments("y2").WithLocation(15, 24),
                // (17,23): error CS1931: The range variable 'y3' conflicts with a previous declaration of 'y3'
                //                   let y3 = 0
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y3").WithArguments("y3").WithLocation(17, 23),
                // (20,24): error CS1931: The range variable 'y4' conflicts with a previous declaration of 'y4'
                //                   into y4
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y4").WithArguments("y4").WithLocation(20, 24),
                // (23,24): error CS1931: The range variable 'y5' conflicts with a previous declaration of 'y5'
                //                   join y5 in new[] { Dummy(TakeOutParam(out var y1), 
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y5").WithArguments("y5").WithLocation(23, 24)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 6; i++)
            {
                var id = "y" + i;
                var yDecl = GetOutVarDeclarations(tree, id).Single();
                var yRef = GetReferences(tree, id).Single();

                switch (i)
                {
                    case 4:
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyModelForOutVar(model, yDecl);
                        VerifyNotAnOutLocal(model, yRef);
                        break;
                    case 5:
                        VerifyModelForOutVar(model, yDecl);
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAnOutLocal(model, yRef);
                        break;
                    default:
                        VerifyModelForOutVar(model, yDecl);
                        VerifyNotAnOutLocal(model, yRef);
                        break;
                }
            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        [WorkItem(12052, "https://github.com/dotnet/roslyn/issues/12052")]
        public void Scope_Query_10()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from y1 in new[] { 0 }
                  from x2 in new[] { TakeOutParam(out var y1) ? y1 : 1 }
                  select y1;
    }

    void Test2()
    {
        var res = from y2 in new[] { 0 }
                  join x3 in new[] { 1 }
                       on TakeOutParam(out var y2) ? y2 : 0 
                       equals x3
                  select y2;
    }

    void Test3()
    {
        var res = from x3 in new[] { 0 }
                  join y3 in new[] { 1 }
                       on x3 
                       equals TakeOutParam(out var y3) ? y3 : 0
                  select y3;
    }

    void Test4()
    {
        var res = from y4 in new[] { 0 }
                  where TakeOutParam(out var y4) && y4 == 1
                  select y4;
    }

    void Test5()
    {
        var res = from y5 in new[] { 0 }
                  orderby TakeOutParam(out var y5) && y5 > 1, 
                          1 
                  select y5;
    }

    void Test6()
    {
        var res = from y6 in new[] { 0 }
                  orderby 1, 
                          TakeOutParam(out var y6) && y6 > 1 
                  select y6;
    }

    void Test7()
    {
        var res = from y7 in new[] { 0 }
                  group TakeOutParam(out var y7) && y7 == 3 
                  by y7;
    }

    void Test8()
    {
        var res = from y8 in new[] { 0 }
                  group y8 
                  by TakeOutParam(out var y8) && y8 == 3;
    }

    void Test9()
    {
        var res = from y9 in new[] { 0 }
                  let x4 = TakeOutParam(out var y9) && y9 > 0
                  select y9;
    }

    void Test10()
    {
        var res = from y10 in new[] { 0 }
                  select TakeOutParam(out var y10) && y10 > 0;
    }

    static bool TakeOutParam(out int x) 
    {
        x = 0;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            // error CS0412 is misleading and reported due to preexisting bug https://github.com/dotnet/roslyn/issues/12052
            compilation.VerifyDiagnostics(
                // (15,59): error CS0412: 'y1': a parameter or local variable cannot have the same name as a method type parameter
                //                   from x2 in new[] { TakeOutParam(out var y1) ? y1 : 1 }
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y1").WithArguments("y1").WithLocation(15, 59),
                // (23,48): error CS0412: 'y2': a parameter or local variable cannot have the same name as a method type parameter
                //                        on TakeOutParam(out var y2) ? y2 : 0 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y2").WithArguments("y2").WithLocation(23, 48),
                // (33,52): error CS0412: 'y3': a parameter or local variable cannot have the same name as a method type parameter
                //                        equals TakeOutParam(out var y3) ? y3 : 0
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y3").WithArguments("y3").WithLocation(33, 52),
                // (40,46): error CS0412: 'y4': a parameter or local variable cannot have the same name as a method type parameter
                //                   where TakeOutParam(out var y4) && y4 == 1
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y4").WithArguments("y4").WithLocation(40, 46),
                // (47,48): error CS0412: 'y5': a parameter or local variable cannot have the same name as a method type parameter
                //                   orderby TakeOutParam(out var y5) && y5 > 1, 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y5").WithArguments("y5").WithLocation(47, 48),
                // (56,48): error CS0412: 'y6': a parameter or local variable cannot have the same name as a method type parameter
                //                           TakeOutParam(out var y6) && y6 > 1 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y6").WithArguments("y6").WithLocation(56, 48),
                // (63,46): error CS0412: 'y7': a parameter or local variable cannot have the same name as a method type parameter
                //                   group TakeOutParam(out var y7) && y7 == 3 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y7").WithArguments("y7").WithLocation(63, 46),
                // (71,43): error CS0412: 'y8': a parameter or local variable cannot have the same name as a method type parameter
                //                   by TakeOutParam(out var y8) && y8 == 3;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y8").WithArguments("y8").WithLocation(71, 43),
                // (77,49): error CS0412: 'y9': a parameter or local variable cannot have the same name as a method type parameter
                //                   let x4 = TakeOutParam(out var y9) && y9 > 0
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y9").WithArguments("y9").WithLocation(77, 49),
                // (84,47): error CS0412: 'y10': a parameter or local variable cannot have the same name as a method type parameter
                //                   select TakeOutParam(out var y10) && y10 > 0;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y10").WithArguments("y10").WithLocation(84, 47)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 11; i++)
            {
                var id = "y" + i;
                var yDecl = GetOutVarDeclarations(tree, id).Single();
                var yRef = GetReferences(tree, id).ToArray();
                Assert.Equal(i == 10 ? 1 : 2, yRef.Length);

                switch (i)
                {
                    case 4:
                    case 6:
                        VerifyModelForOutVar(model, yDecl, yRef[0]);
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAnOutLocal(model, yRef[1]);
                        break;
                    case 8:
                        VerifyModelForOutVar(model, yDecl, yRef[1]);
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAnOutLocal(model, yRef[0]);
                        break;
                    case 10:
                        VerifyModelForOutVar(model, yDecl, yRef[0]);
                        break;
                    default:
                        VerifyModelForOutVar(model, yDecl, yRef[0]);
                        VerifyNotAnOutLocal(model, yRef[1]);
                        break;
                }
            }
        }

        [Fact]
        public void Query_01()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
        Test1();
    }

    static void Test1()
    {
        var res = from x1 in new[] { TakeOutParam(1, out var y1) && Print(y1) ? 1 : 0}
                  from x2 in new[] { TakeOutParam(2, out var y2) && Print(y2) ? 1 : 0}
                  join x3 in new[] { TakeOutParam(3, out var y3) && Print(y3) ? 1 : 0}
                       on TakeOutParam(4, out var y4) && Print(y4) ? 1 : 0
                          equals TakeOutParam(5, out var y5) && Print(y5) ? 1 : 0
                  where TakeOutParam(6, out var y6) && Print(y6)
                  orderby TakeOutParam(7, out var y7) && Print(y7), 
                          TakeOutParam(8, out var y8) && Print(y8) 
                  group TakeOutParam(9, out var y9) && Print(y9) 
                  by TakeOutParam(10, out var y10) && Print(y10)
                  into g
                  let x11 = TakeOutParam(11, out var y11) && Print(y11)
                  select TakeOutParam(12, out var y12) && Print(y12);

        res.ToArray(); 
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }

    static bool Print(object x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"1
3
5
2
4
6
7
8
10
9
11
12
");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 13; i++)
            {
                var id = "y" + i;
                var yDecl = GetOutVarDeclarations(tree, id).Single();
                var yRef = GetReferences(tree, id).Single();
                VerifyModelForOutVar(model, yDecl, yRef);
            }
        }

        [Fact]
        public void Scope_ReturnStatement_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) { return null; }

    object Test1()
    {
        return Dummy(TakeOutParam(true, out var x1), x1);
        {
            return Dummy(TakeOutParam(true, out var x1), x1);
        }
        return Dummy(TakeOutParam(true, out var x1), x1);
    }

    object Test2()
    {
        return Dummy(x2, TakeOutParam(true, out var x2));
    }

    object Test3(int x3)
    {
        return Dummy(TakeOutParam(true, out var x3), x3);
    }

    object Test4()
    {
        var x4 = 11;
        Dummy(x4);
        return Dummy(TakeOutParam(true, out var x4), x4);
    }

    object Test5()
    {
        return Dummy(TakeOutParam(true, out var x5), x5);
        var x5 = 11;
        Dummy(x5);
    }

    //object Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    return Dummy(TakeOutParam(true, out var x6), x6);
    //}

    //object Test7()
    //{
    //    return Dummy(TakeOutParam(true, out var x7), x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    object Test8()
    {
        return Dummy(TakeOutParam(true, out var x8), x8, TakeOutParam(false, out var x8), x8);
    }

    object Test9(bool y9)
    {
        if (y9)
            return Dummy(TakeOutParam(true, out var x9), x9);
        return null;
    }
    System.Func<object> Test10(bool y10)
    {
        return () =>
                {
                    if (y10)
                        return Dummy(TakeOutParam(true, out var x10), x10);
                    return null;};
    }

    object Test11()
    {
        Dummy(x11);
        return Dummy(TakeOutParam(true, out var x11), x11);
    }

    object Test12()
    {
        return Dummy(TakeOutParam(true, out var x12), x12);
        Dummy(x12);
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (14,13): warning CS0162: Unreachable code detected
                //             return Dummy(TakeOutParam(true, out var x1), x1);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(14, 13),
                // (21,22): error CS0841: Cannot use local variable 'x2' before it is declared
                //         return Dummy(x2, TakeOutParam(true, out var x2));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 22),
                // (26,49): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         return Dummy(TakeOutParam(true, out var x3), x3);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 49),
                // (33,49): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         return Dummy(TakeOutParam(true, out var x4), x4);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(33, 49),
                // (38,49): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         return Dummy(TakeOutParam(true, out var x5), x5);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(38, 49),
                // (39,9): warning CS0162: Unreachable code detected
                //         var x5 = 11;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(39, 9),
                // (59,86): error CS0128: A local variable named 'x8' is already defined in this scope
                //         return Dummy(TakeOutParam(true, out var x8), x8, TakeOutParam(false, out var x8), x8);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 86),
                // (79,15): error CS0103: The name 'x11' does not exist in the current context
                //         Dummy(x11);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(79, 15),
                // (86,15): error CS0103: The name 'x12' does not exist in the current context
                //         Dummy(x12);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(86, 15),
                // (86,9): warning CS0162: Unreachable code detected
                //         Dummy(x12);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(86, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForOutVar(model, x1Decl[i], x1Ref[i]);
            }

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForOutVar(model, x2Decl, x2Ref);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAnOutLocal(model, x4Ref[0]);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1]);

            var x5Decl = GetOutVarDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForOutVar(model, x5Decl, x5Ref[0]);
            VerifyNotAnOutLocal(model, x5Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForOutVar(model, x8Decl[0], x8Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetOutVarDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").Single();
            VerifyModelForOutVar(model, x9Decl, x9Ref);

            var x10Decl = GetOutVarDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForOutVar(model, x10Decl, x10Ref);

            var x11Decl = GetOutVarDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyModelForOutVar(model, x11Decl, x11Ref[1]);

            var x12Decl = GetOutVarDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForOutVar(model, x12Decl, x12Ref[0]);
            VerifyNotInScope(model, x12Ref[1]);
        }

        [Fact]
        public void Return_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        Test();
    }

    static object Test()
    {
        return Dummy(TakeOutParam(""return"", out var x1), x1);
    }

    static object Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new object();
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput: @"return");
        }

        [Fact]
        public void Scope_Switch_01()
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
        switch (TakeOutParam(1, out var x1) ? x1 : 0)
        {
            case 0:
                Dummy(x1, 0);
                break;
        }

        Dummy(x1, 1);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        switch (TakeOutParam(4, out var x4) ? x4 : 0)
        {
            case 4:
                Dummy(x4);
                break;
        }
    }

    void Test5(int x5)
    {
        switch (TakeOutParam(5, out var x5) ? x5 : 0)
        {
            case 5:
                Dummy(x5);
                break;
        }
    }

    void Test6()
    {
        switch (x6 + (TakeOutParam(6, out var x6) ? x6 : 0))
        {
            case 6:
                Dummy(x6);
                break;
        }
    }

    void Test7()
    {
        switch (TakeOutParam(7, out var x7) ? x7 : 0)
        {
            case 7:
                var x7 = 12;
                Dummy(x7);
                break;
        }
    }

    void Test9()
    {
        switch (TakeOutParam(9, out var x9) ? x9 : 0)
        {
            case 9:
                Dummy(x9, 0);
                switch (TakeOutParam(9, out var x9) ? x9 : 0)
                {
                    case 9:
                        Dummy(x9, 1);
                        break;
                }
                break;
        }

    }

    void Test10()
    {
        switch (y10 + (TakeOutParam(10, out var x10) ? x10 : 0))
        {
            case 10:
                var y10 = 12;
                Dummy(y10);
                break;
        }
    }

    //void Test11()
    //{
    //    switch (y11 + (TakeOutParam(11, out var x11) ? x11 : 0))
    //    {
    //        case 11:
    //            let y11 = 12;
    //            Dummy(y11);
    //            break;
    //    }
    //}

    void Test14()
    {
        switch (Dummy(TakeOutParam(1, out var x14), 
                  TakeOutParam(2, out var x14), 
                  x14) ? 1 : 0)
        {
            case 0:
                Dummy(x14);
                break;
        }
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (19,15): error CS0103: The name 'x1' does not exist in the current context
                //         Dummy(x1, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(19, 15),
                // (27,41): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         switch (TakeOutParam(4, out var x4) ? x4 : 0)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(27, 41),
                // (37,41): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         switch (TakeOutParam(5, out var x5) ? x5 : 0)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(37, 41),
                // (47,17): error CS0841: Cannot use local variable 'x6' before it is declared
                //         switch (x6 + (TakeOutParam(6, out var x6) ? x6 : 0))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(47, 17),
                // (60,21): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(60, 21),
                // (72,49): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 switch (TakeOutParam(9, out var x9) ? x9 : 0)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(72, 49),
                // (85,17): error CS0103: The name 'y10' does not exist in the current context
                //         switch (y10 + (TakeOutParam(10, out var x10) ? x10 : 0))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 17),
                // (108,43): error CS0128: A local variable named 'x14' is already defined in this scope
                //                   TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(108, 43)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref[0], x1Ref[1]);
            VerifyNotInScope(model, x1Ref[2]);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1], x4Ref[2]);
            VerifyNotAnOutLocal(model, x4Ref[0]);

            var x5Decl = GetOutVarDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForOutVar(model, x5Decl, x5Ref);

            var x6Decl = GetOutVarDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(3, x6Ref.Length);
            VerifyModelForOutVar(model, x6Decl, x6Ref);

            var x7Decl = GetOutVarDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

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
        }

        [Fact]
        public void Switch_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        Test1(0);
        Test1(1);
    }

    static bool Dummy1(bool val, params object[] x) {return val;}
    static T Dummy2<T>(T val, params object[] x) {return val;}

    static void Test1(int val)
    {
        switch (Dummy2(val, TakeOutParam(""Test1 {0}"", out var x1)))
        {
            case 0 when Dummy1(true, TakeOutParam(""case 0"", out var y1)):
                System.Console.WriteLine(x1, y1);
                break;
            case int z1:
                System.Console.WriteLine(x1, z1);
                break;
        }
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"Test1 case 0
Test1 1");
        }

        [Fact]
        public void Scope_SwitchLabelGuard_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) { return true; }

    void Test1(int val)
    {
        switch (val)
        {
            case 0 when Dummy(TakeOutParam(true, out var x1), x1):
                Dummy(x1);
                break;
            case 1 when Dummy(TakeOutParam(true, out var x1), x1):
                Dummy(x1);
                break;
            case 2 when Dummy(TakeOutParam(true, out var x1), x1):
                Dummy(x1);
                break;
        }
    }

    void Test2(int val)
    {
        switch (val)
        {
            case 0 when Dummy(x2, TakeOutParam(true, out var x2)):
                Dummy(x2);
                break;
        }
    }

    void Test3(int x3, int val)
    {
        switch (val)
        {
            case 0 when Dummy(TakeOutParam(true, out var x3), x3):
                Dummy(x3);
                break;
        }
    }

    void Test4(int val)
    {
        var x4 = 11;
        switch (val)
        {
            case 0 when Dummy(TakeOutParam(true, out var x4), x4):
                Dummy(x4);
                break;
            case 1 when Dummy(x4): Dummy(x4); break;
        }
    }

    void Test5(int val)
    {
        switch (val)
        {
            case 0 when Dummy(TakeOutParam(true, out var x5), x5):
                Dummy(x5);
                break;
        }
        
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6(int val)
    //{
    //    let x6 = 11;
    //    switch (val)
    //    {
    //        case 0 when Dummy(x6):
    //            Dummy(x6);
    //            break;
    //        case 1 when Dummy(TakeOutParam(true, out var x6), x6):
    //            Dummy(x6);
    //            break;
    //    }
    //}

    //void Test7(int val)
    //{
    //    switch (val)
    //    {
    //        case 0 when Dummy(TakeOutParam(true, out var x7), x7):
    //            Dummy(x7);
    //            break;
    //    }
        
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8(int val)
    {
        switch (val)
        {
            case 0 when Dummy(TakeOutParam(true, out var x8), x8, TakeOutParam(false, out var x8), x8):
                Dummy(x8);
                break;
        }
    }

    void Test9(int val)
    {
        switch (val)
        {
            case 0 when Dummy(x9):
                int x9 = 9;
                Dummy(x9);
                break;
            case 2 when Dummy(x9 = 9):
                Dummy(x9);
                break;
            case 1 when Dummy(TakeOutParam(true, out var x9), x9):
                Dummy(x9);
                break;
        }
    }

    //void Test10(int val)
    //{
    //    switch (val)
    //    {
    //        case 1 when Dummy(TakeOutParam(true, out var x10), x10):
    //            Dummy(x10);
    //            break;
    //        case 0 when Dummy(x10):
    //            let x10 = 10;
    //            Dummy(x10);
    //            break;
    //        case 2 when Dummy(x10 = 10, x10):
    //            Dummy(x10);
    //            break;
    //    }
    //}

    void Test11(int val)
    {
        switch (x11 ? val : 0)
        {
            case 0 when Dummy(x11):
                Dummy(x11, 0);
                break;
            case 1 when Dummy(TakeOutParam(true, out var x11), x11):
                Dummy(x11, 1);
                break;
        }
    }

    void Test12(int val)
    {
        switch (x12 ? val : 0)
        {
            case 0 when Dummy(TakeOutParam(true, out var x12), x12):
                Dummy(x12, 0);
                break;
            case 1 when Dummy(x12):
                Dummy(x12, 1);
                break;
        }
    }

    void Test13()
    {
        switch (TakeOutParam(1, out var x13) ? x13 : 0)
        {
            case 0 when Dummy(x13):
                Dummy(x13);
                break;
            case 1 when Dummy(TakeOutParam(true, out var x13), x13):
                Dummy(x13);
                break;
        }
    }

    void Test14(int val)
    {
        switch (val)
        {
            case 1 when Dummy(TakeOutParam(true, out var x14), x14):
                Dummy(x14);
                Dummy(TakeOutParam(true, out var x14), x14);
                Dummy(x14);
                break;
        }
    }

    void Test15(int val)
    {
        switch (val)
        {
            case 0 when Dummy(TakeOutParam(true, out var x15), x15):
            case 1 when Dummy(TakeOutParam(true, out var x15), x15):
                Dummy(x15);
                break;
        }
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (30,31): error CS0841: Cannot use local variable 'x2' before it is declared
                //             case 0 when Dummy(x2, TakeOutParam(true, out var x2)):
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(30, 31),
                // (40,58): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 0 when Dummy(TakeOutParam(true, out var x3), x3):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(40, 58),
                // (51,58): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 0 when Dummy(TakeOutParam(true, out var x4), x4):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(51, 58),
                // (62,58): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 0 when Dummy(TakeOutParam(true, out var x5), x5):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(62, 58),
                // (102,95): error CS0128: A local variable named 'x8' is already defined in this scope
                //             case 0 when Dummy(TakeOutParam(true, out var x8), x8, TakeOutParam(false, out var x8), x8):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(102, 95),
                // (112,31): error CS0841: Cannot use local variable 'x9' before it is declared
                //             case 0 when Dummy(x9):
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(112, 31),
                // (119,58): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 1 when Dummy(TakeOutParam(true, out var x9), x9):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(119, 58),
                // (144,17): error CS0103: The name 'x11' does not exist in the current context
                //         switch (x11 ? val : 0)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(144, 17),
                // (146,31): error CS0103: The name 'x11' does not exist in the current context
                //             case 0 when Dummy(x11):
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(146, 31),
                // (147,23): error CS0103: The name 'x11' does not exist in the current context
                //                 Dummy(x11, 0);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(147, 23),
                // (157,17): error CS0103: The name 'x12' does not exist in the current context
                //         switch (x12 ? val : 0)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(157, 17),
                // (162,31): error CS0103: The name 'x12' does not exist in the current context
                //             case 1 when Dummy(x12):
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(162, 31),
                // (163,23): error CS0103: The name 'x12' does not exist in the current context
                //                 Dummy(x12, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(163, 23),
                // (175,58): error CS0136: A local or parameter named 'x13' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 1 when Dummy(TakeOutParam(true, out var x13), x13):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x13").WithArguments("x13").WithLocation(175, 58),
                // (187,50): error CS0136: A local or parameter named 'x14' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 Dummy(TakeOutParam(true, out var x14), x14);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x14").WithArguments("x14").WithLocation(187, 50),
                // (198,58): error CS0128: A local variable named 'x15' is already defined in this scope
                //             case 1 when Dummy(TakeOutParam(true, out var x15), x15):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(198, 58),
                // (198,64): error CS0165: Use of unassigned local variable 'x15'
                //             case 1 when Dummy(TakeOutParam(true, out var x15), x15):
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x15").WithArguments("x15").WithLocation(198, 64)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(6, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForOutVar(model, x1Decl[i], x1Ref[i * 2], x1Ref[i * 2 + 1]);
            }

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForOutVar(model, x2Decl, x2Ref);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(4, x4Ref.Length);
            VerifyModelForOutVar(model, x4Decl, x4Ref[0], x4Ref[1]);
            VerifyNotAnOutLocal(model, x4Ref[2]);
            VerifyNotAnOutLocal(model, x4Ref[3]);

            var x5Decl = GetOutVarDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(3, x5Ref.Length);
            VerifyModelForOutVar(model, x5Decl, x5Ref[0], x5Ref[1]);
            VerifyNotAnOutLocal(model, x5Ref[2]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(3, x8Ref.Length);
            for (int i = 0; i < x8Ref.Length; i++)
            {
                VerifyModelForOutVar(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForOutVarDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetOutVarDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(6, x9Ref.Length);
            VerifyNotAnOutLocal(model, x9Ref[0]);
            VerifyNotAnOutLocal(model, x9Ref[1]);
            VerifyNotAnOutLocal(model, x9Ref[2]);
            VerifyNotAnOutLocal(model, x9Ref[3]);
            VerifyModelForOutVar(model, x9Decl, x9Ref[4], x9Ref[5]);

            var x11Decl = GetOutVarDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(5, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyNotInScope(model, x11Ref[1]);
            VerifyNotInScope(model, x11Ref[2]);
            VerifyModelForOutVar(model, x11Decl, x11Ref[3], x11Ref[4]);

            var x12Decl = GetOutVarDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(5, x12Ref.Length);
            VerifyNotInScope(model, x12Ref[0]);
            VerifyModelForOutVar(model, x12Decl, x12Ref[1], x12Ref[2]);
            VerifyNotInScope(model, x12Ref[3]);
            VerifyNotInScope(model, x12Ref[4]);

            var x13Decl = GetOutVarDeclarations(tree, "x13").ToArray();
            var x13Ref = GetReferences(tree, "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(5, x13Ref.Length);
            VerifyModelForOutVar(model, x13Decl[0], x13Ref[0], x13Ref[1], x13Ref[2]);
            VerifyModelForOutVar(model, x13Decl[1], x13Ref[3], x13Ref[4]);

            var x14Decl = GetOutVarDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(4, x14Ref.Length);
            VerifyModelForOutVar(model, x14Decl[0], x14Ref[0], x14Ref[1], x14Ref[3]);
            VerifyModelForOutVar(model, x14Decl[1], x14Ref[2]);

            var x15Decl = GetOutVarDeclarations(tree, "x15").ToArray();
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(2, x15Decl.Length);
            Assert.Equal(3, x15Ref.Length);
            for (int i = 0; i < x15Ref.Length; i++)
            {
                VerifyModelForOutVar(model, x15Decl[0], x15Ref[i]);
            }
            VerifyModelForOutVarDuplicateInSameScope(model, x15Decl[1]);
        }

        [Fact]
        public void Scope_SwitchLabelPattern_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) { return true; }

    void Test8(object val)
    {
        switch (val)
        {
            case int x8 
                    when Dummy(x8, TakeOutParam(false, out var x8), x8):
                Dummy(x8);
                break;
        }
    }

    void Test13()
    {
        switch (TakeOutParam(1, out var x13) ? x13 : 0)
        {
            case 0 when Dummy(x13):
                Dummy(x13);
                break;
            case int x13 when Dummy(x13):
                Dummy(x13);
                break;
        }
    }

    void Test14(object val)
    {
        switch (val)
        {
            case int x14 when Dummy(x14):
                Dummy(x14);
                Dummy(TakeOutParam(true, out var x14), x14);
                Dummy(x14);
                break;
        }
    }

    void Test16(object val)
    {
        switch (val)
        {
            case int x16 when Dummy(x16):
            case 1 when Dummy(TakeOutParam(true, out var x16), x16):
                Dummy(x16);
                break;
        }
    }

    void Test17(object val)
    {
        switch (val)
        {
            case 0 when Dummy(TakeOutParam(true, out var x17), x17):
            case int x17 when Dummy(x17):
                Dummy(x17);
                break;
        }
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (15,64): error CS0128: A local variable named 'x8' is already defined in this scope
                //                     when Dummy(x8, TakeOutParam(false, out var x8), x8):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(15, 64),
                // (28,22): error CS0136: A local or parameter named 'x13' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x13 when Dummy(x13):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x13").WithArguments("x13").WithLocation(28, 22),
                // (40,50): error CS0136: A local or parameter named 'x14' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 Dummy(TakeOutParam(true, out var x14), x14);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x14").WithArguments("x14").WithLocation(40, 50),
                // (51,58): error CS0128: A local variable named 'x16' is already defined in this scope
                //             case 1 when Dummy(TakeOutParam(true, out var x16), x16):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x16").WithArguments("x16").WithLocation(51, 58),
                // (51,64): error CS0165: Use of unassigned local variable 'x16'
                //             case 1 when Dummy(TakeOutParam(true, out var x16), x16):
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x16").WithArguments("x16").WithLocation(51, 64),
                // (62,22): error CS0128: A local variable named 'x17' is already defined in this scope
                //             case int x17 when Dummy(x17):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x17").WithArguments("x17").WithLocation(62, 22),
                // (62,37): error CS0165: Use of unassigned local variable 'x17'
                //             case int x17 when Dummy(x17):
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x17").WithArguments("x17").WithLocation(62, 37)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            for (int i = 0; i < x8Ref.Length; i++)
            {
                VerifyNotAnOutLocal(model, x8Ref[i]);
            }
            VerifyModelForOutVarDuplicateInSameScope(model, x8Decl);

            var x13Decl = GetOutVarDeclarations(tree, "x13").Single();
            var x13Ref = GetReferences(tree, "x13").ToArray();
            Assert.Equal(5, x13Ref.Length);
            VerifyModelForOutVar(model, x13Decl, x13Ref[0], x13Ref[1], x13Ref[2]);
            VerifyNotAnOutLocal(model, x13Ref[3]);
            VerifyNotAnOutLocal(model, x13Ref[4]);

            var x14Decl = GetOutVarDeclarations(tree, "x14").Single();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(4, x14Ref.Length);
            VerifyNotAnOutLocal(model, x14Ref[0]);
            VerifyNotAnOutLocal(model, x14Ref[1]);
            VerifyModelForOutVar(model, x14Decl, x14Ref[2]);
            VerifyNotAnOutLocal(model, x14Ref[3]);

            var x16Decl = GetOutVarDeclarations(tree, "x16").Single();
            var x16Ref = GetReferences(tree, "x16").ToArray();
            Assert.Equal(3, x16Ref.Length);
            for (int i = 0; i < x16Ref.Length; i++)
            {
                VerifyNotAnOutLocal(model, x16Ref[i]);
            }
            VerifyModelForOutVarDuplicateInSameScope(model, x16Decl);

            var x17Decl = GetOutVarDeclarations(tree, "x17").Single();
            var x17Ref = GetReferences(tree, "x17").ToArray();
            Assert.Equal(3, x17Ref.Length);
            VerifyModelForOutVar(model, x17Decl, x17Ref);
        }

        [Fact]
        public void Scope_ThrowStatement_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.Exception Dummy(params object[] x) { return null;}

    void Test1()
    {
        throw Dummy(TakeOutParam(true, out var x1), x1);
        {
            throw Dummy(TakeOutParam(true, out var x1), x1);
        }
        throw Dummy(TakeOutParam(true, out var x1), x1);
    }

    void Test2()
    {
        throw Dummy(x2, TakeOutParam(true, out var x2));
    }

    void Test3(int x3)
    {
        throw Dummy(TakeOutParam(true, out var x3), x3);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);
        throw Dummy(TakeOutParam(true, out var x4), x4);
    }

    void Test5()
    {
        throw Dummy(TakeOutParam(true, out var x5), x5);
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    throw Dummy(TakeOutParam(true, out var x6), x6);
    //}

    //void Test7()
    //{
    //    throw Dummy(TakeOutParam(true, out var x7), x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8()
    {
        throw Dummy(TakeOutParam(true, out var x8), x8, TakeOutParam(false, out var x8), x8);
    }

    void Test9(bool y9)
    {
        if (y9)
            throw Dummy(TakeOutParam(true, out var x9), x9);
    }

    System.Action Test10(bool y10)
    {
        return () =>
                {
                    if (y10)
                        throw Dummy(TakeOutParam(true, out var x10), x10);
                };
    }

    void Test11()
    {
        Dummy(x11);
        throw Dummy(TakeOutParam(true, out var x11), x11);
    }

    void Test12()
    {
        throw Dummy(TakeOutParam(true, out var x12), x12);
        Dummy(x12);
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (21,21): error CS0841: Cannot use local variable 'x2' before it is declared
                //         throw Dummy(x2, TakeOutParam(true, out var x2));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 21),
                // (26,48): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         throw Dummy(TakeOutParam(true, out var x3), x3);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 48),
                // (33,48): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         throw Dummy(TakeOutParam(true, out var x4), x4);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(33, 48),
                // (38,48): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         throw Dummy(TakeOutParam(true, out var x5), x5);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(38, 48),
                // (39,9): warning CS0162: Unreachable code detected
                //         var x5 = 11;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(39, 9),
                // (59,85): error CS0128: A local variable named 'x8' is already defined in this scope
                //         throw Dummy(TakeOutParam(true, out var x8), x8, TakeOutParam(false, out var x8), x8);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 85),
                // (79,15): error CS0103: The name 'x11' does not exist in the current context
                //         Dummy(x11);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(79, 15),
                // (86,15): error CS0103: The name 'x12' does not exist in the current context
                //         Dummy(x12);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(86, 15),
                // (86,9): warning CS0162: Unreachable code detected
                //         Dummy(x12);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(86, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForOutVar(model, x1Decl[i], x1Ref[i]);
            }

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForOutVar(model, x2Decl, x2Ref);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAnOutLocal(model, x4Ref[0]);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1]);

            var x5Decl = GetOutVarDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForOutVar(model, x5Decl, x5Ref[0]);
            VerifyNotAnOutLocal(model, x5Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForOutVar(model, x8Decl[0], x8Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetOutVarDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").Single();
            VerifyModelForOutVar(model, x9Decl, x9Ref);

            var x10Decl = GetOutVarDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForOutVar(model, x10Decl, x10Ref);

            var x11Decl = GetOutVarDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyModelForOutVar(model, x11Decl, x11Ref[1]);

            var x12Decl = GetOutVarDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForOutVar(model, x12Decl, x12Ref[0]);
            VerifyNotInScope(model, x12Ref[1]);
        }

        [Fact]
        public void Throw_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        Test();
    }

    static void Test()
    {
        try
        {
            throw Dummy(TakeOutParam(""throw"", out var x1), x1);
        }
        catch
        {
        }
    }

    static System.Exception Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new System.ArgumentException();
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput: @"throw");
        }

        [Fact]
        public void Scope_Using_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (Dummy(TakeOutParam(true, out var x1), x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (Dummy(TakeOutParam(true, out var x2), x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        using (Dummy(TakeOutParam(true, out var x4), x4))
            Dummy(x4);
    }

    void Test6()
    {
        using (Dummy(x6 && TakeOutParam(true, out var x6)))
            Dummy(x6);
    }

    void Test7()
    {
        using (Dummy(TakeOutParam(true, out var x7) && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        using (Dummy(TakeOutParam(true, out var x8), x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        using (Dummy(TakeOutParam(true, out var x9), x9))
        {   
            Dummy(x9);
            using (Dummy(TakeOutParam(true, out var x9), x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        using (Dummy(TakeOutParam(y10, out var x10), x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    using (Dummy(TakeOutParam(y11, out var x11), x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        using (Dummy(TakeOutParam(y12, out var x12), x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    using (Dummy(TakeOutParam(y13, out var x13), x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        using (Dummy(TakeOutParam(1, out var x14), 
                     TakeOutParam(2, out var x14), 
                     x14))
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
                // (29,49): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         using (Dummy(TakeOutParam(true, out var x4), x4))
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 49),
                // (35,22): error CS0841: Cannot use local variable 'x6' before it is declared
                //         using (Dummy(x6 && TakeOutParam(true, out var x6)))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 22),
                // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
                // (53,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
                // (61,53): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             using (Dummy(TakeOutParam(true, out var x9), x9)) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 53),
                // (68,35): error CS0103: The name 'y10' does not exist in the current context
                //         using (Dummy(TakeOutParam(y10, out var x10), x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 35),
                // (86,35): error CS0103: The name 'y12' does not exist in the current context
                //         using (Dummy(TakeOutParam(y12, out var x12), x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 35),
                // (99,46): error CS0128: A local variable named 'x14' is already defined in this scope
                //                      TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 46)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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

            var x10Decl = GetOutVarDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForOutVar(model, x10Decl, x10Ref);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAnOutLocal(model, y10Ref[1]);

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
        public void Scope_Using_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (var d = Dummy(TakeOutParam(true, out var x1), x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (var d = Dummy(TakeOutParam(true, out var x2), x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        using (var d = Dummy(TakeOutParam(true, out var x4), x4))
            Dummy(x4);
    }

    void Test6()
    {
        using (var d = Dummy(x6 && TakeOutParam(true, out var x6)))
            Dummy(x6);
    }

    void Test7()
    {
        using (var d = Dummy(TakeOutParam(true, out var x7) && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        using (var d = Dummy(TakeOutParam(true, out var x8), x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        using (var d = Dummy(TakeOutParam(true, out var x9), x9))
        {   
            Dummy(x9);
            using (var e = Dummy(TakeOutParam(true, out var x9), x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        using (var d = Dummy(TakeOutParam(y10, out var x10), x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    using (var d = Dummy(TakeOutParam(y11, out var x11), x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        using (var d = Dummy(TakeOutParam(y12, out var x12), x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    using (var d = Dummy(TakeOutParam(y13, out var x13), x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        using (var d = Dummy(TakeOutParam(1, out var x14), 
                             TakeOutParam(2, out var x14), 
                             x14))
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
                // (29,57): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         using (var d = Dummy(TakeOutParam(true, out var x4), x4))
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 57),
                // (35,30): error CS0841: Cannot use local variable 'x6' before it is declared
                //         using (var d = Dummy(x6 && TakeOutParam(true, out var x6)))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 30),
                // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
                // (53,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
                // (61,61): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             using (var e = Dummy(TakeOutParam(true, out var x9), x9)) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 61),
                // (68,43): error CS0103: The name 'y10' does not exist in the current context
                //         using (var d = Dummy(TakeOutParam(y10, out var x10), x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 43),
                // (86,43): error CS0103: The name 'y12' does not exist in the current context
                //         using (var d = Dummy(TakeOutParam(y12, out var x12), x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 43),
                // (99,54): error CS0128: A local variable named 'x14' is already defined in this scope
                //                              TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 54)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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

            var x10Decl = GetOutVarDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForOutVar(model, x10Decl, x10Ref);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAnOutLocal(model, y10Ref[1]);

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
        public void Scope_Using_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (System.IDisposable d = Dummy(TakeOutParam(true, out var x1), x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (System.IDisposable d = Dummy(TakeOutParam(true, out var x2), x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        using (System.IDisposable d = Dummy(TakeOutParam(true, out var x4), x4))
            Dummy(x4);
    }

    void Test6()
    {
        using (System.IDisposable d = Dummy(x6 && TakeOutParam(true, out var x6)))
            Dummy(x6);
    }

    void Test7()
    {
        using (System.IDisposable d = Dummy(TakeOutParam(true, out var x7) && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        using (System.IDisposable d = Dummy(TakeOutParam(true, out var x8), x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        using (System.IDisposable d = Dummy(TakeOutParam(true, out var x9), x9))
        {   
            Dummy(x9);
            using (System.IDisposable c = Dummy(TakeOutParam(true, out var x9), x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        using (System.IDisposable d = Dummy(TakeOutParam(y10, out var x10), x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    using (System.IDisposable d = Dummy(TakeOutParam(y11, out var x11), x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        using (System.IDisposable d = Dummy(TakeOutParam(y12, out var x12), x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    using (System.IDisposable d = Dummy(TakeOutParam(y13, out var x13), x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        using (System.IDisposable d = Dummy(TakeOutParam(1, out var x14), 
                                            TakeOutParam(2, out var x14), 
                                            x14))
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
                // (29,72): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         using (System.IDisposable d = Dummy(TakeOutParam(true, out var x4), x4))
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 72),
                // (35,45): error CS0841: Cannot use local variable 'x6' before it is declared
                //         using (System.IDisposable d = Dummy(x6 && TakeOutParam(true, out var x6)))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 45),
                // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
                // (53,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
                // (61,76): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             using (System.IDisposable c = Dummy(TakeOutParam(true, out var x9), x9)) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 76),
                // (68,58): error CS0103: The name 'y10' does not exist in the current context
                //         using (System.IDisposable d = Dummy(TakeOutParam(y10, out var x10), x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 58),
                // (86,58): error CS0103: The name 'y12' does not exist in the current context
                //         using (System.IDisposable d = Dummy(TakeOutParam(y12, out var x12), x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 58),
                // (99,69): error CS0128: A local variable named 'x14' is already defined in this scope
                //                                             TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 69)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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

            var x10Decl = GetOutVarDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForOutVar(model, x10Decl, x10Ref);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAnOutLocal(model, y10Ref[1]);

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
        public void Scope_Using_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (var x1 = Dummy(TakeOutParam(true, out var x1), x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (System.IDisposable x2 = Dummy(TakeOutParam(true, out var x2), x2))
        {
            Dummy(x2);
        }
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (12,58): error CS0128: A local variable named 'x1' is already defined in this scope
                //         using (var x1 = Dummy(TakeOutParam(true, out var x1), x1))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(12, 58),
                // (12,63): error CS0841: Cannot use local variable 'x1' before it is declared
                //         using (var x1 = Dummy(TakeOutParam(true, out var x1), x1))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(12, 63),
                // (20,73): error CS0128: A local variable named 'x2' is already defined in this scope
                //         using (System.IDisposable x2 = Dummy(TakeOutParam(true, out var x2), x2))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(20, 73)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForOutVarDuplicateInSameScope(model, x1Decl);
            VerifyNotAnOutLocal(model, x1Ref[0]);
            VerifyNotAnOutLocal(model, x1Ref[1]);

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForOutVarDuplicateInSameScope(model, x2Decl);
            VerifyNotAnOutLocal(model, x2Ref[0]);
            VerifyNotAnOutLocal(model, x2Ref[1]);
        }

        [Fact]
        public void Scope_Using_05()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (System.IDisposable d = Dummy(TakeOutParam(true, out var x1), x1), 
                                  x1 = Dummy(x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (System.IDisposable d1 = Dummy(TakeOutParam(true, out var x2), x2), 
                                  d2 = Dummy(TakeOutParam(true, out var x2), x2))
        {
            Dummy(x2);
        }
    }

    void Test3()
    {
        using (System.IDisposable d1 = Dummy(TakeOutParam(true, out var x3), x3), 
                                  d2 = Dummy(x3))
        {
            Dummy(x3);
        }
    }

    void Test4()
    {
        using (System.IDisposable d1 = Dummy(x4), 
                                  d2 = Dummy(TakeOutParam(true, out var x4), x4))
        {
            Dummy(x4);
        }
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (13,35): error CS0128: A local variable named 'x1' is already defined in this scope
                //                                   x1 = Dummy(x1))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(13, 35),
                // (22,73): error CS0128: A local variable named 'x2' is already defined in this scope
                //                                   d2 = Dummy(TakeOutParam(true, out var x2), x2))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(22, 73),
                // (39,46): error CS0841: Cannot use local variable 'x4' before it is declared
                //         using (System.IDisposable d1 = Dummy(x4), 
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(39, 46)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            var x2Decl = GetOutVarDeclarations(tree, "x2").ToArray();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Decl.Length);
            Assert.Equal(3, x2Ref.Length);
            VerifyModelForOutVar(model, x2Decl[0], x2Ref);
            VerifyModelForOutVarDuplicateInSameScope(model, x2Decl[1]);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(3, x3Ref.Length);
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyModelForOutVar(model, x4Decl, x4Ref);
        }

        [Fact]
        public void Using_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        using (System.IDisposable d1 = Dummy(new C(""a""), TakeOutParam(new C(""b""), out var x1)),
                                  d2 = Dummy(new C(""c""), TakeOutParam(new C(""d""), out var x2)))
        {
            System.Console.WriteLine(d1);
            System.Console.WriteLine(x1);
            System.Console.WriteLine(d2);
            System.Console.WriteLine(x2);
        }

        using (Dummy(new C(""e""), TakeOutParam(new C(""f""), out var x1)))
        {
            System.Console.WriteLine(x1);
        }
    }

    static System.IDisposable Dummy(System.IDisposable x, params object[] y) {return x;}

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}

class C : System.IDisposable
{
    private readonly string _val;

    public C(string val)
    {
        _val = val;
    }

    public void Dispose()
    {
        System.Console.WriteLine(""Disposing {0}"", _val);
    }

    public override string ToString()
    {
        return _val;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"a
b
c
d
Disposing c
Disposing a
f
Disposing e");
        }

        [Fact]
        public void Scope_While_01()
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
        while (TakeOutParam(true, out var x1) && x1)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        while (TakeOutParam(true, out var x2) && x2)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        while (TakeOutParam(true, out var x4) && x4)
            Dummy(x4);
    }

    void Test6()
    {
        while (x6 && TakeOutParam(true, out var x6))
            Dummy(x6);
    }

    void Test7()
    {
        while (TakeOutParam(true, out var x7) && x7)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        while (TakeOutParam(true, out var x8) && x8)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        while (TakeOutParam(true, out var x9) && x9)
        {   
            Dummy(x9);
            while (TakeOutParam(true, out var x9) && x9) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        while (TakeOutParam(y10, out var x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    while (TakeOutParam(y11, out var x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        while (TakeOutParam(y12, out var x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    while (TakeOutParam(y13, out var x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        while (Dummy(TakeOutParam(1, out var x14), 
                     TakeOutParam(2, out var x14), 
                     x14))
        {
            Dummy(x14);
        }
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
                // (29,43): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         while (TakeOutParam(true, out var x4) && x4)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 43),
                // (35,16): error CS0841: Cannot use local variable 'x6' before it is declared
                //         while (x6 && TakeOutParam(true, out var x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 16),
                // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
                // (53,34): error CS0103: The name 'x8' does not exist in the current context
                //         System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
                // (61,47): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             while (TakeOutParam(true, out var x9) && x9) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 47),
                // (68,29): error CS0103: The name 'y10' does not exist in the current context
                //         while (TakeOutParam(y10, out var x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 29),
                // (86,29): error CS0103: The name 'y12' does not exist in the current context
                //         while (TakeOutParam(y12, out var x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 29),
                // (99,46): error CS0128: A local variable named 'x14' is already defined in this scope
                //                      TakeOutParam(2, out var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 46)
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
            VerifyModelForOutVar(model, x7Decl, x7Ref[0]);
            VerifyNotAnOutLocal(model, x7Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").Single();
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
        public void While_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        bool f = true;

        while (Dummy(f, TakeOutParam((f ? 1 : 2), out var x1), x1))
        {
            System.Console.WriteLine(x1);
            f = false;
        }
    }

    static bool Dummy(bool x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"1
1
2");
        }

        [Fact]
        public void Scope_Yield_01()
        {
            var source =
@"
using System.Collections;

public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) { return null;}

    IEnumerable Test1()
    {
        yield return Dummy(TakeOutParam(true, out var x1), x1);
        {
            yield return Dummy(TakeOutParam(true, out var x1), x1);
        }
        yield return Dummy(TakeOutParam(true, out var x1), x1);
    }

    IEnumerable Test2()
    {
        yield return Dummy(x2, TakeOutParam(true, out var x2));
    }

    IEnumerable Test3(int x3)
    {
        yield return Dummy(TakeOutParam(true, out var x3), x3);
    }

    IEnumerable Test4()
    {
        var x4 = 11;
        Dummy(x4);
        yield return Dummy(TakeOutParam(true, out var x4), x4);
    }

    IEnumerable Test5()
    {
        yield return Dummy(TakeOutParam(true, out var x5), x5);
        var x5 = 11;
        Dummy(x5);
    }

    //IEnumerable Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    yield return Dummy(TakeOutParam(true, out var x6), x6);
    //}

    //IEnumerable Test7()
    //{
    //    yield return Dummy(TakeOutParam(true, out var x7), x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    IEnumerable Test8()
    {
        yield return Dummy(TakeOutParam(true, out var x8), x8, TakeOutParam(false, out var x8), x8);
    }

    IEnumerable Test9(bool y9)
    {
        if (y9)
            yield return Dummy(TakeOutParam(true, out var x9), x9);
    }

    IEnumerable Test11()
    {
        Dummy(x11);
        yield return Dummy(TakeOutParam(true, out var x11), x11);
    }

    IEnumerable Test12()
    {
        yield return Dummy(TakeOutParam(true, out var x12), x12);
        Dummy(x12);
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (23,28): error CS0841: Cannot use local variable 'x2' before it is declared
                //         yield return Dummy(x2, TakeOutParam(true, out var x2));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(23, 28),
                // (28,55): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         yield return Dummy(TakeOutParam(true, out var x3), x3);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(28, 55),
                // (35,55): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         yield return Dummy(TakeOutParam(true, out var x4), x4);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(35, 55),
                // (40,55): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         yield return Dummy(TakeOutParam(true, out var x5), x5);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(40, 55),
                // (61,92): error CS0128: A local variable named 'x8' is already defined in this scope
                //         yield return Dummy(TakeOutParam(true, out var x8), x8, TakeOutParam(false, out var x8), x8);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(61, 92),
                // (72,15): error CS0103: The name 'x11' does not exist in the current context
                //         Dummy(x11);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(72, 15),
                // (79,15): error CS0103: The name 'x12' does not exist in the current context
                //         Dummy(x12);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(79, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForOutVar(model, x1Decl[i], x1Ref[i]);
            }

            var x2Decl = GetOutVarDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForOutVar(model, x2Decl, x2Ref);

            var x3Decl = GetOutVarDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForOutVar(model, x3Decl, x3Ref);

            var x4Decl = GetOutVarDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAnOutLocal(model, x4Ref[0]);
            VerifyModelForOutVar(model, x4Decl, x4Ref[1]);

            var x5Decl = GetOutVarDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForOutVar(model, x5Decl, x5Ref[0]);
            VerifyNotAnOutLocal(model, x5Ref[1]);

            var x8Decl = GetOutVarDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            for (int i = 0; i < x8Decl.Length; i++)
            {
                VerifyModelForOutVar(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForOutVarDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetOutVarDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").Single();
            VerifyModelForOutVar(model, x9Decl, x9Ref);

            var x11Decl = GetOutVarDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyModelForOutVar(model, x11Decl, x11Ref[1]);

            var x12Decl = GetOutVarDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForOutVar(model, x12Decl, x12Ref[0]);
            VerifyNotInScope(model, x12Ref[1]);
        }

        [Fact]
        public void Yield_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        foreach (var o in Test())
        {}
    }

    static System.Collections.IEnumerable Test()
    {
        yield return Dummy(TakeOutParam(""yield1"", out var x1), x1);
        yield return Dummy(TakeOutParam(""yield2"", out var x1), x1);
    }

    static object Dummy(object y, object z) 
    {
        System.Console.WriteLine(z);
        return new object();
    }

    static bool TakeOutParam<T>(T y, out T x) 
    {
        x = y;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"yield1
yield2");
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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (6,18): error CS1503: Argument 1: cannot convert from 'out int' to 'out short'
                //         Test(out int x1);
                Diagnostic(ErrorCode.ERR_BadArgType, "int x1").WithArguments("1", "out int", "out short").WithLocation(6, 18)
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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            VerifyModelForOutVar(model, x1Decl);

            Assert.Equal("a=System.Int32", model.GetAliasInfo(x1Decl.Type()).ToTestDisplayString());
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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type()));
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
                                                            parseOptions: TestOptions.Regular);

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

            Assert.Null(model.GetAliasInfo(x1Decl.Type()));
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
                                                            parseOptions: TestOptions.Regular);

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

            Assert.Null(model.GetAliasInfo(x1Decl.Type()));
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
                                                            parseOptions: TestOptions.Regular);

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

            Assert.Null(model.GetAliasInfo(x1Decl.Type()));
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
                                                            parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (6,25): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         Test2(Test1(out var x1), x1);
                Diagnostic(ErrorCode.ERR_BadArgRef, "var x1").WithArguments("1", "ref").WithLocation(6, 25)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type()));
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
                                                            parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (6,25): error CS1615: Argument 1 may not be passed with the 'out' keyword
                //         Test2(Test1(out var x1), x1);
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var x1").WithArguments("1", "out").WithLocation(6, 25)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type()));
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
                                                            parseOptions: TestOptions.Regular);

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

            Assert.Null(model.GetAliasInfo(x1Decl.Type()));
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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (6,37): error CS0149: Method name expected
                //         Test2(new System.Action(out var x1), 
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "var x1").WithLocation(6, 37),
                // (6,37): error CS0165: Use of unassigned local variable 'x1'
                //         Test2(new System.Action(out var x1), 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "var x1").WithArguments("x1").WithLocation(6, 37)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, true, true, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type()));
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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");
            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type()));
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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetOutVarDeclaration(tree, "x1");
            var x1Ref = GetReference(tree, "x1");

            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref).Type.ToTestDisplayString());

            VerifyModelForOutVar(model, x1Decl, x1Ref);

            Assert.Null(model.GetAliasInfo(x1Decl.Type()));
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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
                                                            parseOptions: TestOptions.Regular);

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
        [WorkItem(12058, "https://github.com/dotnet/roslyn/issues/12058")]
        public void MissingArgumentAndNamedOutVarArgument()
        {
            var source =
@"class Program
{
    public static void Main(string[] args)
    {
        if (M(s: out var s))
        {
            string s2 = s;
        }
    }
    public static bool M(int i, out string s)
    {
        s = i.ToString();
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (5,13): error CS7036: There is no argument given that corresponds to the required formal parameter 'i' of 'Program.M(int, out string)'
                //         if (M(s: out var s))
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("i", "Program.M(int, out string)").WithLocation(5, 13)
                );
        }

        [Fact]
        [WorkItem(12266, "https://github.com/dotnet/roslyn/issues/12266")]
        public void LocalVariableTypeInferenceAndOutVar_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var y = Test1(out var x);
        System.Console.WriteLine(y);
    }

    static int Test1(out int x)
    {
        x = 123;
        return 124;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"124").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").Single();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(12266, "https://github.com/dotnet/roslyn/issues/12266")]
        public void LocalVariableTypeInferenceAndOutVar_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var y = Test1(out int x) + x;
        System.Console.WriteLine(y);
    }

    static int Test1(out int x)
    {
        x = 123;
        return 124;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"247").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").Single();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(12266, "https://github.com/dotnet/roslyn/issues/12266")]
        public void LocalVariableTypeInferenceAndOutVar_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var y = Test1(out var x) + x;
        System.Console.WriteLine(y);
    }

    static int Test1(out int x)
    {
        x = 123;
        return 124;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"247").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").Single();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(12266, "https://github.com/dotnet/roslyn/issues/12266")]
        public void LocalVariableTypeInferenceAndOutVar_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        for (var y = Test1(out var x) + x; y != 0 ; y = 0)
        {
            System.Console.WriteLine(y);
        }
    }

    static int Test1(out int x)
    {
        x = 123;
        return 124;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"247").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").Last();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }
        
        [Fact]
        [WorkItem(12266, "https://github.com/dotnet/roslyn/issues/12266")]
        public void LocalVariableTypeInferenceAndOutVar_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        foreach (var y in new [] {Test1(out var x) + x})
        {
            System.Console.WriteLine(y);
        }
    }

    static int Test1(out int x)
    {
        x = 123;
        return 124;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text,
                                                            options: TestOptions.ReleaseExe,
                                                            parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"247").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").Last();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }
    }
}
