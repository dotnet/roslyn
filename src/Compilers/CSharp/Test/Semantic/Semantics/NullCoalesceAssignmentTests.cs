// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.NullCoalescingAssignment)]
    public partial class NullCoalesceAssignmentTests : SemanticModelTestBase
    {
        [Fact]
        public void CoalescingAssignment_NoConversion()
        {
            var source = @"
class C
{
    void M(C c1, C c2)
    {
        c1 ??= c2;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var cType = comp.GetTypeByMetadataName("C");

            var syntaxTree = comp.SyntaxTrees.Single();
            var syntaxRoot = syntaxTree.GetRoot();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var coalesceAssignment = syntaxRoot.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            assertTypeInfo(coalesceAssignment);
            assertTypeInfo(coalesceAssignment.Left);
            assertTypeInfo(coalesceAssignment.Right);

            void assertTypeInfo(SyntaxNode syntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(syntax);
                Assert.NotEqual(default, typeInfo);
                Assert.NotNull(typeInfo.Type);
                Assert.Equal(cType.GetPublicSymbol(), typeInfo.Type);
                Assert.Equal(cType.GetPublicSymbol(), typeInfo.ConvertedType);

            }
        }

        [Fact]
        public void CoalescingAssignment_ValueConversion()
        {
            var source = @"
class C
{
    void M(C c1, D d1)
    {
        c1 ??= d1;
    }
}
class D : C {}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var cType = comp.GetTypeByMetadataName("C");
            var dType = comp.GetTypeByMetadataName("D");

            var syntaxTree = comp.SyntaxTrees.Single();
            var syntaxRoot = syntaxTree.GetRoot();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var coalesceAssignment = syntaxRoot.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            assertTypeInfo(coalesceAssignment);
            assertTypeInfo(coalesceAssignment.Left);

            var whenNullTypeInfo = semanticModel.GetTypeInfo(coalesceAssignment.Right);
            Assert.NotEqual(default, whenNullTypeInfo);
            Assert.Equal(dType.GetPublicSymbol(), whenNullTypeInfo.Type);
            Assert.Equal(cType, whenNullTypeInfo.ConvertedType.GetSymbol());

            void assertTypeInfo(SyntaxNode syntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(syntax);
                Assert.NotEqual(default, typeInfo);
                Assert.NotNull(typeInfo.Type);
                Assert.Equal(cType, typeInfo.Type.GetSymbol());
                Assert.Equal(cType.GetPublicSymbol(), typeInfo.ConvertedType);

            }
        }

        [Fact]
        public void CoalescingAssignment_AsConvertedExpression()
        {
            var source = @"
class C
{
    void M(D d1, D d2)
    {
        M2(d1 ??= d1);
    }
    void M2(C c) {}
}
class D : C {}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var cType = comp.GetTypeByMetadataName("C");
            var dType = comp.GetTypeByMetadataName("D");

            var syntaxTree = comp.SyntaxTrees.Single();
            var syntaxRoot = syntaxTree.GetRoot();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var coalesceAssignment = syntaxRoot.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var whenNullTypeInfo = semanticModel.GetTypeInfo(coalesceAssignment);
            Assert.NotEqual(default, whenNullTypeInfo);
            Assert.Equal(dType, whenNullTypeInfo.Type.GetSymbol());
            Assert.Equal(cType.GetPublicSymbol(), whenNullTypeInfo.ConvertedType);

            assertTypeInfo(coalesceAssignment.Right);
            assertTypeInfo(coalesceAssignment.Left);

            void assertTypeInfo(SyntaxNode syntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(syntax);
                Assert.NotEqual(default, typeInfo);
                Assert.NotNull(typeInfo.Type);
                Assert.Equal(dType.GetPublicSymbol(), typeInfo.Type);
                Assert.Equal(dType, typeInfo.ConvertedType.GetSymbol());

            }
        }

        [Fact]
        public void CoalesceAssignment_ConvertedToNonNullable()
        {
            var source = @"
class C
{
    void M(int? a, int b)
    {
        a ??= b;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees.Single();
            var syntaxRoot = syntaxTree.GetRoot();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var coalesceAssignment = syntaxRoot.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var int32 = comp.GetSpecialType(SpecialType.System_Int32);
            var coalesceType = semanticModel.GetTypeInfo(coalesceAssignment).Type;

            Assert.Equal(int32.GetPublicSymbol(), coalesceType);
        }

        [Fact]
        public void CoalesceAssignment_DefaultConvertedToNonNullable()
        {
            var source = @"
class C
{
    void M(int? a)
    {
        a ??= default;
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees.Single();
            var syntaxRoot = syntaxTree.GetRoot();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var defaultLiteral = syntaxRoot.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(expr => expr.IsKind(SyntaxKind.DefaultLiteralExpression)).Single();

            Assert.Equal(SpecialType.System_Int32, semanticModel.GetTypeInfo(defaultLiteral).Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, semanticModel.GetTypeInfo(defaultLiteral).ConvertedType.SpecialType);
        }

        [Theory]
        [InlineData("=")]
        [InlineData("??=")]
        public void ErrorRecovery_ImplicitObjectCreation_ReferenceType(string assignmentOperator)
        {
            var source = $$"""
                class C
                {
                    void M()
                    {
                        C c = null;
                        c {{assignmentOperator}} new(a);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS0103: The name 'a' does not exist in the current context
                //         c = new(a);
                // (6,19): error CS0103: The name 'a' does not exist in the current context
                //         c ??= new(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 16 + assignmentOperator.Length));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("C", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("C", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);
            Assert.Collection(symbolInfo.CandidateSymbols,
                static c => Assert.Equal("C..ctor()", c.ToTestDisplayString()));

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("C", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("C", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_ImplicitObjectCreation_NullableValueType()
        {
            var source = """
                struct S
                {
                    void M()
                    {
                        S? s = null;
                        s ??= new(a);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): error CS0103: The name 'a' does not exist in the current context
                //         s ??= new(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 19));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("S", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("S", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);
            Assert.Collection(symbolInfo.CandidateSymbols,
                static c => Assert.Equal("S..ctor()", c.ToTestDisplayString()));

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("S", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("S", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_ImplicitObjectCreation_NotNullableValueType()
        {
            var source = """
                struct S
                {
                    void M()
                    {
                        S s = default;
                        s ??= new(a);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): error CS0103: The name 'a' does not exist in the current context
                //         s ??= new(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 19));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.True(typeInfo.Type.IsErrorType());
            Assert.True(typeInfo.ConvertedType.IsErrorType());

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.True(assignmentTypeInfo.Type.IsErrorType());
            Assert.True(assignmentTypeInfo.ConvertedType.IsErrorType());
        }

        [Theory]
        [InlineData("=")]
        [InlineData("??=")]
        public void ErrorRecovery_ImplicitObjectCreation_UnconstrainedGenericType(string assignmentOperator)
        {
            var source = $$"""
                class C
                {
                    void M<T>() where T : new()
                    {
                        T t = default;
                        t {{assignmentOperator}} new(a);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS0103: The name 'a' does not exist in the current context
                //         t = new(a);
                // (6,19): error CS0103: The name 'a' does not exist in the current context
                //         t ??= new(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 16 + assignmentOperator.Length));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("T", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("T", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);
            // INCONSISTENCY: Having type itself as a symbol info candidate for a constructor invocation node is odd.
            // If there were no errors symbol info would be completely empty (no symbol & no candidates).
            // Given that we know type of the expression, shouldn't symbol info be identical for normal and error cases?
            Assert.Collection(symbolInfo.CandidateSymbols,
                static c => Assert.Equal("T", c.ToTestDisplayString()));

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("T", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("T", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Theory]
        [InlineData("=")]
        [InlineData("??=")]
        public void ErrorRecovery_ImplicitObjectCreation_ReferenceGenericType(string assignmentOperator)
        {
            var source = $$"""
                class C
                {
                    void M<T>() where T : class, new()
                    {
                        T t = default;
                        t {{assignmentOperator}} new(a);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS0103: The name 'a' does not exist in the current context
                //         t = new(a);
                // (6,19): error CS0103: The name 'a' does not exist in the current context
                //         t ??= new(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 16 + assignmentOperator.Length));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("T", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("T", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);
            // INCONSISTENCY: Having type itself as a symbol info candidate for a constructor invocation node is odd.
            // If there were no errors symbol info would be completely empty (no symbol & no candidates).
            // Given that we know type of the expression, shouldn't symbol info be identical for normal and error cases?
            Assert.Collection(symbolInfo.CandidateSymbols,
                static c => Assert.Equal("T", c.ToTestDisplayString()));

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("T", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("T", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_ImplicitObjectCreation_ValueGenericType_Nullable()
        {
            var source = """
                class C
                {
                    void M<T>() where T : struct
                    {
                        T? t = default;
                        t ??= new(a);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): error CS0103: The name 'a' does not exist in the current context
                //         t ??= new(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 19));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("T", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("T", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);
            // INCONSISTENCY: Having type itself as a symbol info candidate for a constructor invocation node is odd.
            // If there were no errors symbol info would be completely empty (no symbol & no candidates).
            // Given that we know type of the expression, shouldn't symbol info be identical for normal and error cases?
            Assert.Collection(symbolInfo.CandidateSymbols,
                static c => Assert.Equal("T", c.ToTestDisplayString()));

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("T", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("T", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_ImplicitObjectCreation_ValueGenericType_NotNullable()
        {
            var source = """
                class C
                {
                    void M<T>() where T : struct
                    {
                        T t = default;
                        t ??= new(a);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): error CS0103: The name 'a' does not exist in the current context
                //         t ??= new(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 19));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ImplicitObjectCreationExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.True(typeInfo.Type.IsErrorType());
            Assert.True(typeInfo.ConvertedType.IsErrorType());

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.True(assignmentTypeInfo.Type.IsErrorType());
            Assert.True(assignmentTypeInfo.ConvertedType.IsErrorType());
        }

        [Theory]
        [InlineData("=")]
        [InlineData("??=")]
        public void ErrorRecovery_CollectionExpression_ReferenceType(string assignmentOperator)
        {
            var source = $$"""
                class C
                {
                    void M()
                    {
                        int[] arr = null;
                        arr {{assignmentOperator}} [a];
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,16): error CS0103: The name 'a' does not exist in the current context
                //         arr = [a];
                // (6,18): error CS0103: The name 'a' does not exist in the current context
                //         arr ??= [a];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 15 + assignmentOperator.Length));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<CollectionExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Int32[]", typeInfo.ConvertedType.ToTestDisplayString());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("System.Int32[]", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32[]", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_CollectionExpression_NullableValueType()
        {
            var source = """
                using System.Collections.Immutable;

                struct S
                {
                    void M()
                    {
                        ImmutableArray<int>? arr = null;
                        arr ??= [a];
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (8,18): error CS0103: The name 'a' does not exist in the current context
                //         arr ??= [a];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 18));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<CollectionExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Collections.Immutable.ImmutableArray<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("System.Collections.Immutable.ImmutableArray<System.Int32>", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Collections.Immutable.ImmutableArray<System.Int32>", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_CollectionExpression_NotNullableValueType()
        {
            var source = """
                using System.Collections.Immutable;

                struct S
                {
                    void M()
                    {
                        ImmutableArray<int> arr = default;
                        arr ??= [a];
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (8,18): error CS0103: The name 'a' does not exist in the current context
                //         arr ??= [a];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 18));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<CollectionExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Null(typeInfo.Type);
            Assert.True(typeInfo.ConvertedType.IsErrorType());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.True(assignmentTypeInfo.Type.IsErrorType());
            Assert.True(assignmentTypeInfo.ConvertedType.IsErrorType());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedTuple()
        {
            var source = """
                class C
                {
                    void M()
                    {
                        (C, int)? t = null;
                        t ??= (new(a), 1);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,20): error CS0103: The name 'a' does not exist in the current context
                //         t ??= (new(a), 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 20));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<TupleExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Null(typeInfo.Type);
            Assert.Equal("(C, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("(C, System.Int32)", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("(C, System.Int32)", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Theory]
        [InlineData("=")]
        [InlineData("??=")]
        public void ErrorRecovery_TargetTypedConditionalExpression_ReferenceType(string assignmentOperator)
        {
            var source = $$"""
                class C
                {
                    void M(bool b)
                    {
                        C c = null;
                        c {{assignmentOperator}} b ? new(a) : default;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,21): error CS0103: The name 'a' does not exist in the current context
                //         c = b ? new(a) : default;
                // (6,23): error CS0103: The name 'a' does not exist in the current context
                //         c ??= b ? new(a) : default;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 20 + assignmentOperator.Length));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ConditionalExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            // INCONSISTENCY: if conditional expression didn't have any errors, it would have a `null` type.
            // Shouldn't this be preserved given that an expression is target-typed and have no natural type anyway?
            // Moreover, target-typed tuples and collection expressions already behave that way
            Assert.True(typeInfo.Type.IsErrorType());
            Assert.Equal("C", typeInfo.ConvertedType.ToTestDisplayString());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("C", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("C", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedConditionalExpression_NullableValueType()
        {
            var source = """
                struct S
                {
                    void M(bool b)
                    {
                        S? s = null;
                        s ??= b ? new(a) : default;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,23): error CS0103: The name 'a' does not exist in the current context
                //         s ??= b ? new(a) : default;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 23));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ConditionalExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            // INCONSISTENCY: if conditional expression didn't have any errors, it would have a `null` type.
            // Shouldn't this be preserved given that an expression is target-typed and have no natural type anyway?
            // Moreover, target-typed tuples and collection expressions already behave that way
            Assert.True(typeInfo.Type.IsErrorType());
            Assert.Equal("S", typeInfo.ConvertedType.ToTestDisplayString());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("S", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("S", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedConditionalExpression_NotNullableValueType()
        {
            var source = """
                struct S
                {
                    void M(bool b)
                    {
                        S s = default;
                        s ??= b ? new(a) : default;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,23): error CS0103: The name 'a' does not exist in the current context
                //         s ??= b ? new(a) : default;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 23));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ConditionalExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            // INCONSISTENCY: if conditional expression didn't have any errors, it would have a `null` type.
            // Shouldn't this be preserved given that an expression is target-typed and have no natural type anyway?
            // Moreover, target-typed tuples and collection expressions already behave that way
            Assert.True(typeInfo.Type.IsErrorType());
            Assert.True(typeInfo.ConvertedType.IsErrorType());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.True(assignmentTypeInfo.Type.IsErrorType());
            Assert.True(assignmentTypeInfo.ConvertedType.IsErrorType());
        }

        [Theory]
        [InlineData("=")]
        [InlineData("??=")]
        public void ErrorRecovery_TargetTypedSwitchExpression_ReferenceType(string assignmentOperator)
        {
            var source = $$"""
                class C
                {
                    void M(int i)
                    {
                        C c = null;
                        c {{assignmentOperator}} i switch
                        {
                            1 => new(a),
                            _ => default,
                        };
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,22): error CS0103: The name 'a' does not exist in the current context
                //             1 => new(a),
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 22));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<SwitchExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            // INCONSISTENCY: if switch expression didn't have any errors, it would have a `null` type.
            // Shouldn't this be preserved given that an expression is target-typed and have no natural type anyway?
            // Moreover, target-typed tuples and collection expressions already behave that way
            Assert.True(typeInfo.Type.IsErrorType());
            Assert.Equal("C", typeInfo.ConvertedType.ToTestDisplayString());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("C", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("C", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedSwitchExpression_NullableValueType()
        {
            var source = """
                struct S
                {
                    void M(int i)
                    {
                        S? s = null;
                        s ??= i switch
                        {
                            1 => new(a),
                            _ => default,
                        };
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,22): error CS0103: The name 'a' does not exist in the current context
                //             1 => new(a),
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 22));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<SwitchExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            // INCONSISTENCY: if switch expression didn't have any errors, it would have a `null` type.
            // Shouldn't this be preserved given that an expression is target-typed and have no natural type anyway?
            // Moreover, target-typed tuples and collection expressions already behave that way
            Assert.True(typeInfo.Type.IsErrorType());
            Assert.Equal("S", typeInfo.ConvertedType.ToTestDisplayString());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("S", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("S", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedSwitchExpression_NotNullableValueType()
        {
            var source = """
                struct S
                {
                    void M(int i)
                    {
                        S s = default;
                        s ??= i switch
                        {
                            1 => new(a),
                            _ => default,
                        };
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,22): error CS0103: The name 'a' does not exist in the current context
                //             1 => new(a),
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 22));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<SwitchExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            // INCONSISTENCY: if switch expression didn't have any errors, it would have a `null` type.
            // Shouldn't this be preserved given that an expression is target-typed and have no natural type anyway?
            // Moreover, target-typed tuples and collection expressions already behave that way
            Assert.True(typeInfo.Type.IsErrorType());
            Assert.True(typeInfo.ConvertedType.IsErrorType());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.True(assignmentTypeInfo.Type.IsErrorType());
            Assert.True(assignmentTypeInfo.ConvertedType.IsErrorType());
        }

        [Theory]
        [InlineData("=")]
        [InlineData("??=")]
        public void ErrorRecovery_TargetTypedInterpolatedString_ReferenceType(string assignmentOperator)
        {
            var source = $$"""
                class C
                {
                    void M()
                    {
                        CustomInterpolatedStringHandler h = null;
                        h {{assignmentOperator}} $"The value is {a}";
                    }
                }
                """;

            var comp = CreateCompilation([source, GetInterpolatedStringCustomHandlerType("CustomInterpolatedStringHandler", "class", useBoolReturns: false)]);
            comp.VerifyDiagnostics(
                // (6,29): error CS0103: The name 'a' does not exist in the current context
                //         h = $"The value is {a}";
                // (6,31): error CS0103: The name 'a' does not exist in the current context
                //         h ??= $"The value is {a}";
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 28 + assignmentOperator.Length));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<InterpolatedStringExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("CustomInterpolatedStringHandler", typeInfo.ConvertedType.ToTestDisplayString());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("CustomInterpolatedStringHandler", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("CustomInterpolatedStringHandler", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedInterpolatedString_NullableValueType()
        {
            var source = """
                class C
                {
                    void M()
                    {
                        CustomInterpolatedStringHandler? h = null;
                        h ??= $"The value is {a}";
                    }
                }
                """;

            var comp = CreateCompilation([source, GetInterpolatedStringCustomHandlerType("CustomInterpolatedStringHandler", "struct", useBoolReturns: false)]);
            comp.VerifyDiagnostics(
                // (6,31): error CS0103: The name 'a' does not exist in the current context
                //         h ??= $"The value is {a}";
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 31));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<InterpolatedStringExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("CustomInterpolatedStringHandler", typeInfo.ConvertedType.ToTestDisplayString());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("CustomInterpolatedStringHandler", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("CustomInterpolatedStringHandler", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedInterpolatedString_NotNullableValueType()
        {
            var source = """
                class C
                {
                    void M()
                    {
                        CustomInterpolatedStringHandler h = default;
                        h ??= $"The value is {a}";
                    }
                }
                """;

            var comp = CreateCompilation([source, GetInterpolatedStringCustomHandlerType("CustomInterpolatedStringHandler", "struct", useBoolReturns: false)]);
            comp.VerifyDiagnostics(
                // (6,31): error CS0103: The name 'a' does not exist in the current context
                //         h ??= $"The value is {a}";
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 31));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<InterpolatedStringExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            Assert.True(typeInfo.ConvertedType.IsErrorType());

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.True(assignmentTypeInfo.Type.IsErrorType());
            Assert.True(assignmentTypeInfo.ConvertedType.IsErrorType());
        }

        [Theory]
        [InlineData("=")]
        [InlineData("??=")]
        public void ErrorRecovery_ReferenceType_ImplicitConversion(string assignmentOperator)
        {
            var source = $$"""
                class C
                {
                    void M()
                    {
                        object o = null;
                        o {{assignmentOperator}} new C(a);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS1729: 'C' does not contain a constructor that takes 1 arguments
                //         o = new C(a);
                // (6,19): error CS1729: 'C' does not contain a constructor that takes 1 arguments
                //         o ??= new C(a);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("C", "1").WithLocation(6, 16 + assignmentOperator.Length),
                // (6,19): error CS0103: The name 'a' does not exist in the current context
                //         o = new C(a);
                // (6,21): error CS0103: The name 'a' does not exist in the current context
                //         o ??= new C(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 18 + assignmentOperator.Length));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ObjectCreationExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("C", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Null(symbolInfo.Symbol);
            Assert.Collection(symbolInfo.CandidateSymbols,
                static c => Assert.Equal("C..ctor()", c.ToTestDisplayString()));

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("System.Object", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Object", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_NullableValueType_ImplicitConversion()
        {
            var source = """
                struct S
                {
                    void M()
                    {
                        S? s = null;
                        s ??= new ConvertibleToS(a);
                    }
                }

                class ConvertibleToS
                {
                    public static implicit operator S(ConvertibleToS c) => default;
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): error CS1729: 'ConvertibleToS' does not contain a constructor that takes 1 arguments
                //         s ??= new ConvertibleToS(a);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "ConvertibleToS").WithArguments("ConvertibleToS", "1").WithLocation(6, 19),
                // (6,34): error CS0103: The name 'a' does not exist in the current context
                //         s ??= new ConvertibleToS(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 34));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var descendantNodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = descendantNodes.OfType<ObjectCreationExpressionSyntax>().Single();

            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("ConvertibleToS", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("S", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("S ConvertibleToS.op_Implicit(ConvertibleToS c)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);

            var assignmentNode = descendantNodes.OfType<AssignmentExpressionSyntax>().Single();
            var assignmentTypeInfo = model.GetTypeInfo(assignmentNode);
            Assert.Equal("S", assignmentTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("S", assignmentTypeInfo.ConvertedType.ToTestDisplayString());
        }
    }
}
