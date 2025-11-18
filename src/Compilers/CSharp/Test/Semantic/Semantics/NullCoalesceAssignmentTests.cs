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
                        c ??= /*<bind>*/new(a)/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,29): error CS0103: The name 'a' does not exist in the current context
                //         c ??= /*<bind>*/new(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 29));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString());

            Assert.Null(semanticInfo.Symbol);
            Assert.Collection(semanticInfo.CandidateSymbols,
                static c => Assert.Equal("C..ctor()", c.ToTestDisplayString()));
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
                        s ??= /*<bind>*/new(a)/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,29): error CS0103: The name 'a' does not exist in the current context
                //         s ??= /*<bind>*/new(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 29));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Equal("S", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("S", semanticInfo.ConvertedType.ToTestDisplayString());

            Assert.Null(semanticInfo.Symbol);
            Assert.Collection(semanticInfo.CandidateSymbols,
                static c => Assert.Equal("S..ctor()", c.ToTestDisplayString()));
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
                        s ??= /*<bind>*/new(a)/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,29): error CS0103: The name 'a' does not exist in the current context
                //         s ??= /*<bind>*/new(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 29));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.True(semanticInfo.Type.IsErrorType());
            Assert.True(semanticInfo.ConvertedType.IsErrorType());

            Assert.Null(semanticInfo.Symbol);
            Assert.Empty(semanticInfo.CandidateSymbols);
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
                        t ??= /*<bind>*/new(a)/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,29): error CS0103: The name 'a' does not exist in the current context
                //         t ??= /*<bind>*/new(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 29));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Equal("T", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("T", semanticInfo.ConvertedType.ToTestDisplayString());

            Assert.Null(semanticInfo.Symbol);
            Assert.Collection(semanticInfo.CandidateSymbols,
                static c => Assert.Equal("T", c.ToTestDisplayString()));
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

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Equal("T", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("T", semanticInfo.ConvertedType.ToTestDisplayString());

            Assert.Null(semanticInfo.Symbol);
            Assert.Collection(semanticInfo.CandidateSymbols,
                static c => Assert.Equal("T", c.ToTestDisplayString()));
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
                        t ??= /*<bind>*/new(a)/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,29): error CS0103: The name 'a' does not exist in the current context
                //         t ??= /*<bind>*/new(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 29));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Equal("T", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("T", semanticInfo.ConvertedType.ToTestDisplayString());

            Assert.Null(semanticInfo.Symbol);
            Assert.Collection(semanticInfo.CandidateSymbols,
                static c => Assert.Equal("T", c.ToTestDisplayString()));
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
                        t ??= /*<bind>*/new(a)/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,29): error CS0103: The name 'a' does not exist in the current context
                //         t ??= /*<bind>*/new(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 29));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.True(semanticInfo.Type.IsErrorType());
            Assert.True(semanticInfo.ConvertedType.IsErrorType());

            Assert.Null(semanticInfo.Symbol);
            Assert.Empty(semanticInfo.CandidateSymbols);
        }

        [Fact]
        public void ErrorRecovery_CollectionExpression_ReferenceType()
        {
            var source = """
                class C
                {
                    void M()
                    {
                        int[] arr = default;
                        arr ??= /*<bind>*/[a]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,28): error CS0103: The name 'a' does not exist in the current context
                //         arr ??= /*<bind>*/[a]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 28));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.Int32[]", semanticInfo.ConvertedType.ToTestDisplayString());
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
                        ImmutableArray<int>? arr = default;
                        arr ??= /*<bind>*/[a]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            comp.VerifyDiagnostics(
                // (8,28): error CS0103: The name 'a' does not exist in the current context
                //         arr ??= /*<bind>*/[a]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 28));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.Collections.Immutable.ImmutableArray<System.Int32>", semanticInfo.ConvertedType.ToTestDisplayString());
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
                        arr ??= /*<bind>*/[a]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
            comp.VerifyDiagnostics(
                // (8,28): error CS0103: The name 'a' does not exist in the current context
                //         arr ??= /*<bind>*/[a]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 28));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
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
                        t ??= /*<bind>*/(new(a), 1)/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,30): error CS0103: The name 'a' does not exist in the current context
                //         t ??= /*<bind>*/(new(a), 1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 30));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("(C, System.Int32)", semanticInfo.ConvertedType.ToTestDisplayString());
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
                        c ??= /*<bind>*/b ? new(a) : default/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS0103: The name 'a' does not exist in the current context
                //         c ??= /*<bind>*/b ? new(a) : default/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 33));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString());
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
                        s ??= /*<bind>*/b ? new(a) : default/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS0103: The name 'a' does not exist in the current context
                //         s ??= /*<bind>*/b ? new(a) : default/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 33));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("S", semanticInfo.ConvertedType.ToTestDisplayString());
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
                        s ??= /*<bind>*/b ? new(a) : default/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS0103: The name 'a' does not exist in the current context
                //         s ??= /*<bind>*/b ? new(a) : default/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 33));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.True(semanticInfo.Type.IsErrorType());
            Assert.True(semanticInfo.ConvertedType.IsErrorType());
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
                        c ??= /*<bind>*/i switch
                        {
                            1 => new(a),
                            _ => default,
                        }/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,22): error CS0103: The name 'a' does not exist in the current context
                //             1 => new(a),
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 22));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString());
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
                        s ??= /*<bind>*/i switch
                        {
                            1 => new(a),
                            _ => default,
                        }/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,22): error CS0103: The name 'a' does not exist in the current context
                //             1 => new(a),
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 22));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("S", semanticInfo.ConvertedType.ToTestDisplayString());
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
                        s ??= /*<bind>*/i switch
                        {
                            1 => new(a),
                            _ => default,
                        }/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,22): error CS0103: The name 'a' does not exist in the current context
                //             1 => new(a),
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 22));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.True(semanticInfo.Type.IsErrorType());
            Assert.True(semanticInfo.ConvertedType.IsErrorType());
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
                        h ??= /*<bind>*/$"The value is {a}"/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation([source, GetInterpolatedStringCustomHandlerType("CustomInterpolatedStringHandler", "class", useBoolReturns: false)]);
            comp.VerifyDiagnostics(
                // (6,41): error CS0103: The name 'a' does not exist in the current context
                //         h ??= /*<bind>*/$"The value is {a}"/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 41));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("CustomInterpolatedStringHandler", semanticInfo.ConvertedType.ToTestDisplayString());
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
                        h ??= /*<bind>*/$"The value is {a}"/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation([source, GetInterpolatedStringCustomHandlerType("CustomInterpolatedStringHandler", "struct", useBoolReturns: false)]);
            comp.VerifyDiagnostics(
                // (6,41): error CS0103: The name 'a' does not exist in the current context
                //         h ??= /*<bind>*/$"The value is {a}"/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 41));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("CustomInterpolatedStringHandler", semanticInfo.ConvertedType.ToTestDisplayString());
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
                        h ??= /*<bind>*/$"The value is {a}"/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation([source, GetInterpolatedStringCustomHandlerType("CustomInterpolatedStringHandler", "struct", useBoolReturns: false)]);
            comp.VerifyDiagnostics(
                // (6,41): error CS0103: The name 'a' does not exist in the current context
                //         h ??= /*<bind>*/$"The value is {a}"/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 41));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
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
                        o ??= /*<bind>*/new C(a)/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,29): error CS1729: 'C' does not contain a constructor that takes 1 arguments
                //         o ??= /*<bind>*/new C(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("C", "1").WithLocation(6, 29),
                // (6,31): error CS0103: The name 'a' does not exist in the current context
                //         o ??= /*<bind>*/new C(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 31));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());

            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);
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
                        s ??= /*<bind>*/new ConvertibleToS(a)/*</bind>*/;
                    }
                }

                class ConvertibleToS
                {
                    public static implicit operator S(ConvertibleToS c) => default;
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,29): error CS1729: 'ConvertibleToS' does not contain a constructor that takes 1 arguments
                //         s ??= /*<bind>*/new ConvertibleToS(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "ConvertibleToS").WithArguments("ConvertibleToS", "1").WithLocation(6, 29),
                // (6,44): error CS0103: The name 'a' does not exist in the current context
                //         s ??= /*<bind>*/new ConvertibleToS(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 44));

            var semanticInfo = GetSemanticInfoForTest(comp);

            Assert.Equal("ConvertibleToS", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("S", semanticInfo.ConvertedType.ToTestDisplayString());

            Assert.Equal(ConversionKind.ImplicitUserDefined, semanticInfo.ImplicitConversion.Kind);
            Assert.Equal("S ConvertibleToS.op_Implicit(ConvertibleToS c)", semanticInfo.ImplicitConversion.Method.ToTestDisplayString());
        }
    }
}
