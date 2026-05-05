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

        [Fact]
        public void ErrorRecovery_ImplicitObjectCreation_ReferenceType()
        {
            var source = """
                class C
                {
                    void M()
                    {
                        C c = default;
                        c ??= new(a);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): error CS0103: The name 'a' does not exist in the current context
                //         c ??= new(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 19));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var objectCreationExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionMemberGroup.ToTestDisplayStrings());
        }

        [Fact]
        public void ErrorRecovery_ImplicitObjectCreation_NullableValueType()
        {
            var source = """
                struct S
                {
                    void M()
                    {
                        S? s = default;
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

            var objectCreationExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("S", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("S", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["S..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            AssertEx.SetEqual(["S..ctor()"], objectCreationExpressionMemberGroup.ToTestDisplayStrings());
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

            var objectCreationExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.True(objectCreationExpressionTypeInfo.Type.IsErrorType());
            Assert.True(objectCreationExpressionTypeInfo.ConvertedType.IsErrorType());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.True(objectCreationExpressionSymbolInfo.IsEmpty);
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            Assert.Empty(objectCreationExpressionMemberGroup);
        }

        [Fact]
        public void ErrorRecovery_ImplicitObjectCreation_UnconstrainedGenericType()
        {
            var source = """
                class C
                {
                    void M<T>() where T : new()
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

            var objectCreationExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("T", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("T", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["T"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            Assert.Empty(objectCreationExpressionMemberGroup);
        }

        [Fact]
        public void ErrorRecovery_ImplicitObjectCreation_ReferenceGenericType()
        {
            var source = """
                class C
                {
                    void M<T>() where T : class, new()
                    {
                        T t = default;
                        t ??= /*<bind>*/new(a)/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,29): error CS0103: The name 'a' does not exist in the current context
                //         t ??= /*<bind>*/new(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 29));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var objectCreationExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("T", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("T", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["T"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            Assert.Empty(objectCreationExpressionMemberGroup);
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

            var objectCreationExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("T", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("T", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["T"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            Assert.Empty(objectCreationExpressionMemberGroup);
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

            var objectCreationExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.True(objectCreationExpressionTypeInfo.Type.IsErrorType());
            Assert.True(objectCreationExpressionTypeInfo.ConvertedType.IsErrorType());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.True(objectCreationExpressionSymbolInfo.IsEmpty);
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            Assert.Empty(objectCreationExpressionMemberGroup);
        }

        [Fact]
        public void ErrorRecovery_CollectionExpression_ReferenceType()
        {
            var source = """
                class C
                {
                    void M()
                    {
                        C[] arr = default;
                        arr ??= [new(a)];
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,22): error CS0103: The name 'a' does not exist in the current context
                //         arr ??= [new(a)];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 22));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var collectionExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<CollectionExpressionSyntax>().Single();
            var collectionExpressionTypeInfo = model.GetTypeInfo(collectionExpression);
            Assert.Null(collectionExpressionTypeInfo.Type);
            Assert.Equal("C[]", collectionExpressionTypeInfo.ConvertedType.ToTestDisplayString());

            var objectCreationExpression = collectionExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionMemberGroup.ToTestDisplayStrings());
        }

        [Fact]
        public void ErrorRecovery_CollectionExpression_NullableValueType()
        {
            var source = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableArray<C>? arr = default;
                        arr ??= [new(a)];
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            comp.VerifyDiagnostics(
                // (8,22): error CS0103: The name 'a' does not exist in the current context
                //         arr ??= [new(a)];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 22));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var collectionExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<CollectionExpressionSyntax>().Single();
            var collectionExpressionTypeInfo = model.GetTypeInfo(collectionExpression);
            Assert.Null(collectionExpressionTypeInfo.Type);
            Assert.Equal("System.Collections.Immutable.ImmutableArray<C>", collectionExpressionTypeInfo.ConvertedType.ToTestDisplayString());

            var objectCreationExpression = collectionExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionMemberGroup.ToTestDisplayStrings());
        }

        [Fact]
        public void ErrorRecovery_CollectionExpression_NotNullableValueType()
        {
            var source = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableArray<int> arr = default;
                        arr ??= [new(a)];
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            comp.VerifyDiagnostics(
                // (8,22): error CS0103: The name 'a' does not exist in the current context
                //         arr ??= [new(a)];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 22));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var collectionExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<CollectionExpressionSyntax>().Single();
            var collectionExpressionTypeInfo = model.GetTypeInfo(collectionExpression);
            Assert.Null(collectionExpressionTypeInfo.Type);
            Assert.Null(collectionExpressionTypeInfo.ConvertedType);

            var objectCreationExpression = collectionExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.True(objectCreationExpressionTypeInfo.Type.IsErrorType());
            Assert.True(objectCreationExpressionTypeInfo.ConvertedType.IsErrorType());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.True(objectCreationExpressionSymbolInfo.IsEmpty);
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            Assert.Empty(objectCreationExpressionMemberGroup);
        }

        [Fact]
        public void ErrorRecovery_TargetTypedTuple()
        {
            var source = """
                class C
                {
                    void M()
                    {
                        (C, int)? t = default;
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

            var tupleExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            var tupleTypeInfo = model.GetTypeInfo(tupleExpression);
            Assert.Null(tupleTypeInfo.Type);
            Assert.Equal("(C, System.Int32)", tupleTypeInfo.ConvertedType.ToTestDisplayString());

            var objectCreationExpression = tupleExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionMemberGroup.ToTestDisplayStrings());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedConditionalExpression_ReferenceType()
        {
            var source = """
                class C
                {
                    void M(bool b)
                    {
                        C c = default;
                        c ??= b ? new(a) : default;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,23): error CS0103: The name 'a' does not exist in the current context
                //         c ??= b ? new(a) : default;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 23));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var conditionalExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();
            var conditionalExpressionTypeInfo = model.GetTypeInfo(conditionalExpression);
            Assert.Null(conditionalExpressionTypeInfo.Type);
            Assert.Equal("C", conditionalExpressionTypeInfo.ConvertedType.ToTestDisplayString());

            var objectCreationExpression = conditionalExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionMemberGroup.ToTestDisplayStrings());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedConditionalExpression_NullableValueType()
        {
            var source = """
                struct S
                {
                    void M(bool b)
                    {
                        S? s = default;
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

            var conditionalExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();
            var conditionalExpressionTypeInfo = model.GetTypeInfo(conditionalExpression);
            Assert.Null(conditionalExpressionTypeInfo.Type);
            Assert.Equal("S", conditionalExpressionTypeInfo.ConvertedType.ToTestDisplayString());

            var objectCreationExpression = conditionalExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("S", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("S", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["S..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            AssertEx.SetEqual(["S..ctor()"], objectCreationExpressionMemberGroup.ToTestDisplayStrings());
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

            var conditionalExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();
            var conditionalExpressionTypeInfo = model.GetTypeInfo(conditionalExpression);
            Assert.True(conditionalExpressionTypeInfo.Type.IsErrorType());
            Assert.True(conditionalExpressionTypeInfo.ConvertedType.IsErrorType());

            var objectCreationExpression = conditionalExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.True(objectCreationExpressionTypeInfo.Type.IsErrorType());
            Assert.True(objectCreationExpressionTypeInfo.ConvertedType.IsErrorType());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.True(objectCreationExpressionSymbolInfo.IsEmpty);
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            Assert.Empty(objectCreationExpressionMemberGroup);
        }

        [Fact]
        public void ErrorRecovery_TargetTypedSwitchExpression_ReferenceType()
        {
            var source = """
                class C
                {
                    void M(int i)
                    {
                        C c = default;
                        c ??= i switch
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

            var switchExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
            var switchExpressionTypeInfo = model.GetTypeInfo(switchExpression);
            Assert.Null(switchExpressionTypeInfo.Type);
            Assert.Equal("C", switchExpressionTypeInfo.ConvertedType.ToTestDisplayString());

            var objectCreationExpression = switchExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("C", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            AssertEx.SetEqual(["C..ctor()"], objectCreationExpressionMemberGroup.ToTestDisplayStrings());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedSwitchExpression_NullableValueType()
        {
            var source = """
                struct S
                {
                    void M(int i)
                    {
                        S? s = default;
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

            var switchExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
            var switchExpressionTypeInfo = model.GetTypeInfo(switchExpression);
            Assert.Null(switchExpressionTypeInfo.Type);
            Assert.Equal("S", switchExpressionTypeInfo.ConvertedType.ToTestDisplayString());

            var objectCreationExpression = switchExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("S", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("S", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.Null(objectCreationExpressionSymbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, objectCreationExpressionSymbolInfo.CandidateReason);
            AssertEx.SetEqual(["S..ctor()"], objectCreationExpressionSymbolInfo.CandidateSymbols.ToTestDisplayStrings());
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            AssertEx.SetEqual(["S..ctor()"], objectCreationExpressionMemberGroup.ToTestDisplayStrings());
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

            var switchExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
            var switchExpressionTypeInfo = model.GetTypeInfo(switchExpression);
            Assert.True(switchExpressionTypeInfo.Type.IsErrorType());
            Assert.True(switchExpressionTypeInfo.ConvertedType.IsErrorType());

            var objectCreationExpression = switchExpression.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.True(objectCreationExpressionTypeInfo.Type.IsErrorType());
            Assert.True(objectCreationExpressionTypeInfo.ConvertedType.IsErrorType());
            var objectCreationExpressionSymbolInfo = model.GetSymbolInfo(objectCreationExpression);
            Assert.True(objectCreationExpressionSymbolInfo.IsEmpty);
            var objectCreationExpressionMemberGroup = model.GetMemberGroup(objectCreationExpression);
            Assert.Empty(objectCreationExpressionMemberGroup);
        }

        [Fact]
        public void ErrorRecovery_TargetTypedInterpolatedString_ReferenceType()
        {
            var source = """
                class C
                {
                    void M(int i)
                    {
                        CustomInterpolatedStringHandler h = default;
                        h ??= $"The value is {a}";
                    }
                }
                """;

            var comp = CreateCompilation([source, GetInterpolatedStringCustomHandlerType("CustomInterpolatedStringHandler", "class", useBoolReturns: false)]);
            comp.VerifyDiagnostics(
                // (6,31): error CS0103: The name 'a' does not exist in the current context
                //         h ??= $"The value is {a}";
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 31));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var interpolatedString = tree.GetCompilationUnitRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>().Single();
            var interpolatedStringTypeInfo = model.GetTypeInfo(interpolatedString);
            Assert.Equal("System.String", interpolatedStringTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("CustomInterpolatedStringHandler", interpolatedStringTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedInterpolatedString_NullableValueType()
        {
            var source = """
                class C
                {
                    void M(int i)
                    {
                        CustomInterpolatedStringHandler? h = default;
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

            var interpolatedString = tree.GetCompilationUnitRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>().Single();
            var interpolatedStringTypeInfo = model.GetTypeInfo(interpolatedString);
            Assert.Equal("System.String", interpolatedStringTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("CustomInterpolatedStringHandler", interpolatedStringTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_TargetTypedInterpolatedString_NotNullableValueType()
        {
            var source = """
                class C
                {
                    void M(int i)
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

            var interpolatedString = tree.GetCompilationUnitRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>().Single();
            var interpolatedStringTypeInfo = model.GetTypeInfo(interpolatedString);
            Assert.Equal("System.String", interpolatedStringTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.String", interpolatedStringTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ErrorRecovery_ReferenceType_ImplicitConversion()
        {
            var source = """
                class C
                {
                    void M()
                    {
                        object o = default;
                        o ??= new C(a);
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,19): error CS1729: 'C' does not contain a constructor that takes 1 arguments
                //         o ??= new C(a);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("C", "1").WithLocation(6, 19),
                // (6,21): error CS0103: The name 'a' does not exist in the current context
                //         o ??= new C(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 21));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var objectCreationExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("C", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Object", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());

            var conversion = model.GetConversion(objectCreationExpression);
            Assert.Equal(ConversionKind.ImplicitReference, conversion.Kind);
        }

        [Fact]
        public void ErrorRecovery_NullableValueType_ImplicitConversion()
        {
            var source = """
                struct S
                {
                    void M()
                    {
                        S? s = default;
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

            var objectCreationExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            var objectCreationExpressionTypeInfo = model.GetTypeInfo(objectCreationExpression);
            Assert.Equal("ConvertibleToS", objectCreationExpressionTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("S", objectCreationExpressionTypeInfo.ConvertedType.ToTestDisplayString());

            var conversion = model.GetConversion(objectCreationExpression);
            Assert.Equal(ConversionKind.ImplicitUserDefined, conversion.Kind);
            Assert.Equal("S ConvertibleToS.op_Implicit(ConvertibleToS c)", conversion.MethodSymbol.ToTestDisplayString());
        }
    }
}
