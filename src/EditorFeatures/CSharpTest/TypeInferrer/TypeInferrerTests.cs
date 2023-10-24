// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.TypeInferrer;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TypeInferrer
{
    [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
    public partial class TypeInferrerTests : TypeInferrerTestBase<CSharpTestWorkspaceFixture>
    {
        protected override async Task TestWorkerAsync(Document document, TextSpan textSpan, string expectedType, TestMode mode)
        {
            var root = await document.GetSyntaxRootAsync();
            var node = FindExpressionSyntaxFromSpan(root, textSpan);
            var typeInference = document.GetLanguageService<ITypeInferenceService>();

            ITypeSymbol inferredType;

            if (mode == TestMode.Position)
            {
                var position = node?.SpanStart ?? textSpan.Start;
                inferredType = typeInference.InferType(await document.ReuseExistingSpeculativeModelAsync(position, CancellationToken.None), position, objectAsDefault: true, cancellationToken: CancellationToken.None);
            }
            else
            {
                inferredType = typeInference.InferType(await document.ReuseExistingSpeculativeModelAsync(node?.Span ?? textSpan, CancellationToken.None), node, objectAsDefault: true, cancellationToken: CancellationToken.None);
            }

            var typeSyntax = inferredType.GenerateTypeSyntax().NormalizeWhitespace();
            Assert.Equal(expectedType, typeSyntax.ToString());
        }

        private async Task TestInClassAsync(string text, string expectedType, TestMode mode)
        {
            text = """
                class C
                {
                    $
                }
                """.Replace("$", text);
            await TestAsync(text, expectedType, mode);
        }

        private async Task TestInMethodAsync(string text, string expectedType, TestMode mode)
        {
            text = """
                class C
                {
                    void M()
                    {
                        $
                    }
                }
                """.Replace("$", text);
            await TestAsync(text, expectedType, mode);
        }

        private static ExpressionSyntax FindExpressionSyntaxFromSpan(SyntaxNode root, TextSpan textSpan)
        {
            var token = root.FindToken(textSpan.Start);
            var currentNode = token.Parent;
            while (currentNode != null)
            {
                if (currentNode is ExpressionSyntax result && result.Span == textSpan)
                {
                    return result;
                }

                currentNode = currentNode.Parent;
            }

            return null;
        }

        [Fact]
        public async Task TestConditional1()
        {
            // We do not support position inference here as we're before the ? and we only look
            // backwards to infer a type here.
            await TestInMethodAsync(
@"var q = [|Goo()|] ? 1 : 2;", "global::System.Boolean",
                TestMode.Node);
        }

        [Theory, CombinatorialData]
        public async Task TestConditional2(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = a ? [|Goo()|] : 2;", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestConditional3(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = a ? """" : [|Goo()|];", "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestVariableDeclarator1(TestMode mode)
        {
            await TestInMethodAsync(
@"int q = [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestVariableDeclarator2(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = [|Goo()|];", "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestVariableDeclaratorNullableReferenceType(TestMode mode)
        {
            await TestInMethodAsync(
                """
                #nullable enable
                string? q = [|Goo()|];
                """, "global::System.String?", mode);
        }

        [Fact]
        public async Task TestCoalesce1()
        {
            await TestInMethodAsync(
@"var q = [|Goo()|] ?? 1;", "global::System.Int32?", TestMode.Node);
        }

        [Theory, CombinatorialData]
        public async Task TestCoalesce2(TestMode mode)
        {
            await TestInMethodAsync(
                """
                bool? b;
                var q = b ?? [|Goo()|];
                """, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestCoalesce3(TestMode mode)
        {
            await TestInMethodAsync(
                """
                string s;
                var q = s ?? [|Goo()|];
                """, "global::System.String", mode);
        }

        [Fact]
        public async Task TestCoalesce4()
        {
            await TestInMethodAsync(
@"var q = [|Goo()|] ?? string.Empty;", "global::System.String?", TestMode.Node);
        }

        [Fact]
        public async Task TestCoalesceWithErrorType()
        {
            // We could be smart and infer this as an ErrorType?, but in the #nullable disable case we don't know if this is intended to be
            // a struct (where the question mark is legal) or a class (where it isn't). We'll thus avoid sticking question marks in this case.
            // https://github.com/dotnet/roslyn/issues/37852 tracks fixing this is a much fancier way.
            await TestInMethodAsync(
                """
                ErrorType s;
                var q = [|Goo()|] ?? s;
                """, "ErrorType", TestMode.Node);
        }

        [Theory, CombinatorialData]
        public async Task TestBinaryExpression1(TestMode mode)
        {
            await TestInMethodAsync(
                """
                string s;
                var q = s + [|Goo()|];
                """, "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestBinaryExpression2(TestMode mode)
        {
            await TestInMethodAsync(
                """
                var s;
                var q = s || [|Goo()|];
                """, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestBinaryOperator1(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = x << [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestBinaryOperator2(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = x >> [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestBinaryOperator3(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = x >>> [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestAssignmentOperator3(TestMode mode)
        {
            await TestInMethodAsync(
@"var q <<= [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestAssignmentOperator4(TestMode mode)
        {
            await TestInMethodAsync(
@"var q >>= [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestOverloadedConditionalLogicalOperatorsInferBool(TestMode mode)
        {
            await TestAsync(
                """
                using System;

                class C
                {
                    public static C operator &(C c, C d)
                    {
                        return null;
                    }

                    public static bool operator true(C c)
                    {
                        return true;
                    }

                    public static bool operator false(C c)
                    {
                        return false;
                    }

                    static void Main(string[] args)
                    {
                        var c = new C() && [|Goo()|];
                    }
                }
                """, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestConditionalLogicalOrOperatorAlwaysInfersBool(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = a || [|7|];
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestConditionalLogicalAndOperatorAlwaysInfersBool(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = a && [|7|];
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference1()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = [|a|] | true;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", TestMode.Node);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference2()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = [|a|] | b | c || d;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference3(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = a | b | [|c|] || d;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference4()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = Goo([|a|] | b);
                    }
                    static object Goo(Program p)
                    {
                        return p;
                    }
                }
                """;
            await TestAsync(text, "Program", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference5(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = Goo([|a|] | b);
                    }
                    static object Goo(bool p)
                    {
                        return p;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference6()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        if (([|x|] | y) != 0) {}
                    }
                }
                """;
            await TestAsync(text, "global::System.Int32", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference7(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        if ([|x|] | y) {}
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference1()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = [|a|] & true;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", TestMode.Node);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference2()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = [|a|] & b & c && d;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference3(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = a & b & [|c|] && d;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference4()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = Goo([|a|] & b);
                    }
                    static object Goo(Program p)
                    {
                        return p;
                    }
                }
                """;
            await TestAsync(text, "Program", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference5(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = Goo([|a|] & b);
                    }
                    static object Goo(bool p)
                    {
                        return p;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference6()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        if (([|x|] & y) != 0) {}
                    }
                }
                """;
            await TestAsync(text, "global::System.Int32", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference7(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        if ([|x|] & y) {}
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference1()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = [|a|] ^ true;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", TestMode.Node);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference2()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = [|a|] ^ b ^ c && d;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference3(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = a ^ b ^ [|c|] && d;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference4()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = Goo([|a|] ^ b);
                    }
                    static object Goo(Program p)
                    {
                        return p;
                    }
                }
                """;
            await TestAsync(text, "Program", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference5(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        var x = Goo([|a|] ^ b);
                    }
                    static object Goo(bool p)
                    {
                        return p;
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference6()
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        if (([|x|] ^ y) != 0) {}
                    }
                }
                """;
            await TestAsync(text, "global::System.Int32", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference7(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        if ([|x|] ^ y) {}
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrEqualsOperatorInference1(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        if ([|x|] |= y) {}
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrEqualsOperatorInference2(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        int z = [|x|] |= y;
                    }
                }
                """;
            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndEqualsOperatorInference1(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        if ([|x|] &= y) {}
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndEqualsOperatorInference2(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        int z = [|x|] &= y;
                    }
                }
                """;
            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorEqualsOperatorInference1(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        if ([|x|] ^= y) {}
                    }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorEqualsOperatorInference2(TestMode mode)
        {
            var text = """
                using System;
                class C
                {
                    static void Main(string[] args)
                    {
                        int z = [|x|] ^= y;
                    }
                }
                """;
            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInConstructor(TestMode mode)
        {
            await TestInClassAsync(
                """
                C()
                {
                    return [|Goo()|];
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInDestructor(TestMode mode)
        {
            await TestInClassAsync(
                """
                ~C()
                {
                    return [|Goo()|];
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInMethod(TestMode mode)
        {
            await TestInClassAsync(
                """
                int M()
                {
                    return [|Goo()|];
                }
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInMethodNullableReference(TestMode mode)
        {
            await TestInClassAsync(
                """
                #nullable enable
                string? M()
                {
                    return [|Goo()|];
                }
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInVoidMethod(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    return [|Goo()|];
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncTaskOfTMethod(TestMode mode)
        {
            await TestInClassAsync(
                """
                async System.Threading.Tasks.Task<int> M()
                {
                    return [|Goo()|];
                }
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncTaskOfTMethodNestedNullability(TestMode mode)
        {
            await TestInClassAsync(
                """
                async System.Threading.Tasks.Task<string?> M()
                {
                    return [|Goo()|];
                }
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncTaskMethod(TestMode mode)
        {
            await TestInClassAsync(
                """
                async System.Threading.Tasks.Task M()
                {
                    return [|Goo()|];
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncVoidMethod(TestMode mode)
        {
            await TestInClassAsync(
                """
                async void M()
                {
                    return [|Goo()|];
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInOperator(TestMode mode)
        {
            await TestInClassAsync(
                """
                public static C operator ++(C c)
                {
                    return [|Goo()|];
                }
                """, "global::C", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInConversionOperator(TestMode mode)
        {
            await TestInClassAsync(
                """
                public static implicit operator int(C c)
                {
                    return [|Goo()|];
                }
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInPropertyGetter(TestMode mode)
        {
            await TestInClassAsync(
                """
                int P
                {
                    get
                    {
                        return [|Goo()|];
                    }
                }
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInPropertyGetterNullableReference(TestMode mode)
        {
            await TestInClassAsync(
                """
                #nullable enable
                string? P
                {
                    get
                    {
                        return [|Goo()|];
                    }
                }
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInPropertySetter(TestMode mode)
        {
            await TestInClassAsync(
                """
                int P
                {
                    set
                    {
                        return [|Goo()|];
                    }
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInIndexerGetter(TestMode mode)
        {
            await TestInClassAsync(
                """
                int this[int i]
                {
                    get
                    {
                        return [|Goo()|];
                    }
                }
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInIndexerGetterNullableReference(TestMode mode)
        {
            await TestInClassAsync(
                """
                #nullable enable
                string? this[int i]
                {
                    get
                    {
                        return [|Goo()|];
                    }
                }
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInIndexerSetter(TestMode mode)
        {
            await TestInClassAsync(
                """
                int this[int i]
                {
                    set
                    {
                        return [|Goo()|];
                    }
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInEventAdder(TestMode mode)
        {
            await TestInClassAsync(
                """
                event System.EventHandler E
                {
                    add
                    {
                        return [|Goo()|];
                    }
                    remove { }
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInEventRemover(TestMode mode)
        {
            await TestInClassAsync(
                """
                event System.EventHandler E
                {
                    add { }
                    remove
                    {
                        return [|Goo()|];
                    }
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInLocalFunction(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    int F()
                    {
                        return [|Goo()|];
                    }
                }
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInLocalFunctionNullableReference(TestMode mode)
        {
            await TestInClassAsync(
                """
                #nullable enable
                void M()
                {
                    string? F()
                    {
                        return [|Goo()|];
                    }
                }
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncTaskOfTLocalFunction(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    async System.Threading.Tasks.Task<int> F()
                    {
                        return [|Goo()|];
                    }
                }
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncTaskLocalFunction(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    async System.Threading.Tasks.Task F()
                    {
                        return [|Goo()|];
                    }
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncVoidLocalFunction(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    async void F()
                    {
                        return [|Goo()|];
                    }
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedConstructor(TestMode mode)
        {
            await TestInClassAsync(
@"C() => [|Goo()|];", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedDestructor(TestMode mode)
        {
            await TestInClassAsync(
@"~C() => [|Goo()|];", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedMethod(TestMode mode)
        {
            await TestInClassAsync(
@"int M() => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedVoidMethod(TestMode mode)
        {
            await TestInClassAsync(
@"void M() => [|Goo()|];", "void", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/27647"), CombinatorialData]
        public async Task TestExpressionBodiedAsyncTaskOfTMethod(TestMode mode)
        {
            await TestInClassAsync(
@"async System.Threading.Tasks.Task<int> M() => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/27647"), CombinatorialData]
        public async Task TestExpressionBodiedAsyncTaskOfTMethodNullableReference(TestMode mode)
        {
            await TestInClassAsync(
                """
                #nullable enable
                async System.Threading.Tasks.Task<string?> M() => [|Goo()|];
                """, "global::System.String?", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/27647"), CombinatorialData]
        public async Task TestExpressionBodiedAsyncTaskMethod(TestMode mode)
        {
            await TestInClassAsync(
@"async System.Threading.Tasks.Task M() => [|Goo()|];", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedAsyncVoidMethod(TestMode mode)
        {
            await TestInClassAsync(
@"async void M() => [|Goo()|];", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedOperator(TestMode mode)
        {
            await TestInClassAsync(
@"public static C operator ++(C c) => [|Goo()|];", "global::C", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedConversionOperator(TestMode mode)
        {
            await TestInClassAsync(
@"public static implicit operator int(C c) => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedProperty(TestMode mode)
        {
            await TestInClassAsync(
@"int P => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedIndexer(TestMode mode)
        {
            await TestInClassAsync(
@"int this[int i] => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedPropertyGetter(TestMode mode)
        {
            await TestInClassAsync(
@"int P { get => [|Goo()|]; }", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedPropertySetter(TestMode mode)
        {
            await TestInClassAsync(
@"int P { set => [|Goo()|]; }", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedIndexerGetter(TestMode mode)
        {
            await TestInClassAsync(
@"int this[int i] { get => [|Goo()|]; }", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedIndexerSetter(TestMode mode)
        {
            await TestInClassAsync(
@"int this[int i] { set => [|Goo()|]; }", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedEventAdder(TestMode mode)
        {
            await TestInClassAsync(
@"event System.EventHandler E { add => [|Goo()|]; remove { } }", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedEventRemover(TestMode mode)
        {
            await TestInClassAsync(
@"event System.EventHandler E { add { } remove => [|Goo()|]; }", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedLocalFunction(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    int F() => [|Goo()|];
                }
                """, "global::System.Int32", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/27647"), CombinatorialData]
        public async Task TestExpressionBodiedAsyncTaskOfTLocalFunction(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    async System.Threading.Tasks.Task<int> F() => [|Goo()|];
                }
                """, "global::System.Int32", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/27647"), CombinatorialData]
        public async Task TestExpressionBodiedAsyncTaskLocalFunction(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    async System.Threading.Tasks.Task F() => [|Goo()|];
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionBodiedAsyncVoidLocalFunction(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    async void F() => [|Goo()|];
                }
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
        public async Task TestYieldReturnInMethod([CombinatorialValues("IEnumerable", "IEnumerator", "InvalidGenericType")] string returnTypeName, TestMode mode)
        {
            var markup =
$@"using System.Collections.Generic;

class C
{{
    {returnTypeName}<int> M()
    {{
        yield return [|abc|]
    }}
}}";
            await TestAsync(markup, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestYieldReturnInMethodNullableReference([CombinatorialValues("IEnumerable", "IEnumerator", "InvalidGenericType")] string returnTypeName, TestMode mode)
        {
            var markup =
$@"#nullable enable
using System.Collections.Generic;

class C
{{
    {returnTypeName}<string?> M()
    {{
        yield return [|abc|]
    }}
}}";
            await TestAsync(markup, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestYieldReturnInAsyncMethod([CombinatorialValues("IAsyncEnumerable", "IAsyncEnumerator", "InvalidGenericType")] string returnTypeName, TestMode mode)
        {
            var markup =
$@"namespace System.Collections.Generic
{{
    interface {returnTypeName}<T> {{ }}
    class C
    {{
        async {returnTypeName}<int> M()
        {{
            yield return [|abc|]
        }}
    }}
}}";
            await TestAsync(markup, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestYieldReturnInvalidTypeInMethod([CombinatorialValues("int[]", "InvalidNonGenericType", "InvalidGenericType<int, int>")] string returnType, TestMode mode)
        {
            var markup =
$@"class C
{{
    {returnType} M()
    {{
        yield return [|abc|]
    }}
}}";
            await TestAsync(markup, "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/30235")]
        public async Task TestYieldReturnInLocalFunction(TestMode mode)
        {
            var markup =
                """
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        IEnumerable<int> F()
                        {
                            yield return [|abc|]
                        }
                    }
                }
                """;
            await TestAsync(markup, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestYieldReturnInPropertyGetter(TestMode mode)
        {
            var markup =
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> P
                    {
                        get
                        {
                            yield return [|abc|]
                        }
                    }
                }
                """;
            await TestAsync(markup, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestYieldReturnInPropertySetter(TestMode mode)
        {
            var markup =
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> P
                    {
                        set
                        {
                            yield return [|abc|]
                        }
                    }
                }
                """;
            await TestAsync(markup, "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestYieldReturnAsGlobalStatement(TestMode mode)
        {
            await TestAsync(
@"yield return [|abc|]", "global::System.Object", mode, sourceCodeKind: SourceCodeKind.Script);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInSimpleLambda(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Func<string, int> f = s =>
                {
                    return [|Goo()|];
                };
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInParenthesizedLambda(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Func<int> f = () =>
                {
                    return [|Goo()|];
                };
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInLambdaWithNullableReturn(TestMode mode)
        {
            await TestInMethodAsync(
                """
                #nullable enable
                System.Func<string, string?> f = s =>
                {
                    return [|Goo()|];
                };
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAnonymousMethod(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Func<int> f = delegate ()
                {
                    return [|Goo()|];
                };
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAnonymousMethodWithNullableReturn(TestMode mode)
        {
            await TestInMethodAsync(
                """
                #nullable enable
                System.Func<string?> f = delegate ()
                {
                    return [|Goo()|];
                };
                """, "global::System.String?", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/4486"), CombinatorialData]
        public async Task TestReturnInAsyncTaskOfTSimpleLambda(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Func<string, System.Threading.Tasks.Task<int>> f = async s =>
                {
                    return [|Goo()|];
                };
                """, "global::System.Int32", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/4486"), CombinatorialData]
        public async Task TestReturnInAsyncTaskOfTParenthesizedLambda(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Func<System.Threading.Tasks.Task<int>> f = async () =>
                {
                    return [|Goo()|];
                };
                """, "global::System.Int32", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/4486"), CombinatorialData]
        public async Task TestReturnInAsyncTaskOfTAnonymousMethod(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Func<System.Threading.Tasks.Task<int>> f = async delegate ()
                {
                    return [|Goo()|];
                };
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncTaskOfTAnonymousMethodWithNullableReference(TestMode mode)
        {
            await TestInMethodAsync(
                """
                #nullable enable
                System.Func<System.Threading.Tasks.Task<string?>> f = async delegate ()
                {
                    return [|Goo()|];
                };
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncTaskSimpleLambda(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Func<string, System.Threading.Tasks.Task> f = async s =>
                {
                    return [|Goo()|];
                };
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncTaskParenthesizedLambda(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Func<System.Threading.Tasks.Task> f = async () =>
                {
                    return [|Goo()|];
                };
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncTaskAnonymousMethod(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Func<System.Threading.Tasks.Task> f = async delegate ()
                {
                    return [|Goo()|];
                };
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncVoidSimpleLambda(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Action<string> f = async s =>
                {
                    return [|Goo()|];
                };
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncVoidParenthesizedLambda(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Action f = async () =>
                {
                    return [|Goo()|];
                };
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnInAsyncVoidAnonymousMethod(TestMode mode)
        {
            await TestInMethodAsync(
                """
                System.Action f = async delegate ()
                {
                    return [|Goo()|];
                };
                """, "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestReturnAsGlobalStatement(TestMode mode)
        {
            await TestAsync(
@"return [|Goo()|];", "global::System.Object", mode, sourceCodeKind: SourceCodeKind.Script);
        }

        [Theory, CombinatorialData]
        public async Task TestSimpleLambda(TestMode mode)
        {
            await TestInMethodAsync(
@"System.Func<string, int> f = s => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestParenthesizedLambda(TestMode mode)
        {
            await TestInMethodAsync(
@"System.Func<int> f = () => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/30232")]
        public async Task TestAsyncTaskOfTSimpleLambda(TestMode mode)
        {
            await TestInMethodAsync(
@"System.Func<string, System.Threading.Tasks.Task<int>> f = async s => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestAsyncTaskOfTSimpleLambdaWithNullableReturn(TestMode mode)
        {
            await TestInMethodAsync(
                """
                #nullable enable
                System.Func<string, System.Threading.Tasks.Task<string?>> f = async s => [|Goo()|];
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/30232")]
        public async Task TestAsyncTaskOfTParenthesizedLambda(TestMode mode)
        {
            await TestInMethodAsync(
@"System.Func<System.Threading.Tasks.Task<int>> f = async () => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/30232")]
        public async Task TestAsyncTaskSimpleLambda(TestMode mode)
        {
            await TestInMethodAsync(
@"System.Func<string, System.Threading.Tasks.Task> f = async s => [|Goo()|];", "void", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/30232"), CombinatorialData]
        public async Task TestAsyncTaskParenthesizedLambda(TestMode mode)
        {
            await TestInMethodAsync(
@"System.Func<System.Threading.Tasks.Task> f = async () => [|Goo()|];", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestAsyncVoidSimpleLambda(TestMode mode)
        {
            await TestInMethodAsync(
@"System.Action<string> f = async s => [|Goo()|];", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestAsyncVoidParenthesizedLambda(TestMode mode)
        {
            await TestInMethodAsync(
@"System.Action f = async () => [|Goo()|];", "void", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionTreeSimpleLambda(TestMode mode)
        {
            await TestInMethodAsync(
@"System.Linq.Expressions.Expression<System.Func<string, int>> f = s => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestExpressionTreeParenthesizedLambda(TestMode mode)
        {
            await TestInMethodAsync(
@"System.Linq.Expressions.Expression<System.Func<int>> f = () => [|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestThrow(TestMode mode)
        {
            await TestInMethodAsync(
@"throw [|Goo()|];", "global::System.Exception", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestCatch(TestMode mode)
            => await TestInMethodAsync("try { } catch ([|Goo|] ex) { }", "global::System.Exception", mode);

        [Theory, CombinatorialData]
        public async Task TestIf(TestMode mode)
            => await TestInMethodAsync(@"if ([|Goo()|]) { }", "global::System.Boolean", mode);

        [Theory, CombinatorialData]
        public async Task TestWhile(TestMode mode)
            => await TestInMethodAsync(@"while ([|Goo()|]) { }", "global::System.Boolean", mode);

        [Theory, CombinatorialData]
        public async Task TestDo(TestMode mode)
            => await TestInMethodAsync(@"do { } while ([|Goo()|])", "global::System.Boolean", mode);

        [Theory, CombinatorialData]
        public async Task TestFor1(TestMode mode)
        {
            await TestInMethodAsync(
                """
                for (int i = 0; [|Goo()|];

                i++) { }
                """, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestFor2(TestMode mode)
            => await TestInMethodAsync(@"for (string i = [|Goo()|]; ; ) { }", "global::System.String", mode);

        [Theory, CombinatorialData]
        public async Task TestFor3(TestMode mode)
            => await TestInMethodAsync(@"for (var i = [|Goo()|]; ; ) { }", "global::System.Int32", mode);

        [Theory, CombinatorialData]
        public async Task TestForNullableReference(TestMode mode)
        {
            await TestInMethodAsync(
                """
                #nullable enable
                for (string? s = [|Goo()|]; ; ) { }
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestUsing1(TestMode mode)
            => await TestInMethodAsync(@"using ([|Goo()|]) { }", "global::System.IDisposable", mode);

        [Theory, CombinatorialData]
        public async Task TestUsing2(TestMode mode)
            => await TestInMethodAsync(@"using (int i = [|Goo()|]) { }", "global::System.Int32", mode);

        [Theory, CombinatorialData]
        public async Task TestUsing3(TestMode mode)
            => await TestInMethodAsync(@"using (var v = [|Goo()|]) { }", "global::System.IDisposable", mode);

        [Theory, CombinatorialData]
        public async Task TestForEach(TestMode mode)
            => await TestInMethodAsync(@"foreach (int v in [|Goo()|]) { }", "global::System.Collections.Generic.IEnumerable<global::System.Int32>", mode);

        [Theory(Skip = "https://github.com/dotnet/roslyn/issues/37309"), CombinatorialData]
        public async Task TestForEachNullableElements(TestMode mode)
        {
            await TestInMethodAsync(
                """
                #nullable enable
                foreach (string? v in [|Goo()|]) { }
                """, "global::System.Collections.Generic.IEnumerable<global::System.String?>", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestPrefixExpression1(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = +[|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestPrefixExpression2(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = -[|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestPrefixExpression3(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = ~[|Goo()|];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestPrefixExpression4(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = ![|Goo()|];", "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestPrefixExpression5(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = System.DayOfWeek.Monday & ~[|Goo()|];", "global::System.DayOfWeek", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestArrayRankSpecifier(TestMode mode)
        {
            await TestInMethodAsync(
@"var q = new string[[|Goo()|]];", "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestSwitch1(TestMode mode)
            => await TestInMethodAsync(@"switch ([|Goo()|]) { }", "global::System.Int32", mode);

        [Theory, CombinatorialData]
        public async Task TestSwitch2(TestMode mode)
            => await TestInMethodAsync(@"switch ([|Goo()|]) { default: }", "global::System.Int32", mode);

        [Theory, CombinatorialData]
        public async Task TestSwitch3(TestMode mode)
            => await TestInMethodAsync(@"switch ([|Goo()|]) { case ""a"": }", "global::System.String", mode);

        [Theory, CombinatorialData]
        public async Task TestMethodCall1(TestMode mode)
        {
            await TestInMethodAsync(
@"Bar([|Goo()|]);", "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestMethodCall2(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    Bar([|Goo()|]);
                }

                void Bar(int i);
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestMethodCall3(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    Bar([|Goo()|]);
                }

                void Bar();
                """, "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestMethodCall4(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    Bar([|Goo()|]);
                }

                void Bar(int i, string s);
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestMethodCall5(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    Bar(s: [|Goo()|]);
                }

                void Bar(int i, string s);
                """, "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestMethodCallNullableReference(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    Bar([|Goo()|]);
                }

                void Bar(string? s);
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestConstructorCall1(TestMode mode)
        {
            await TestInMethodAsync(
@"new C([|Goo()|]);", "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestConstructorCall2(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    new C([|Goo()|]);
                }

                C(int i)
                {
                }
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestConstructorCall3(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    new C([|Goo()|]);
                }

                C()
                {
                }
                """, "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestConstructorCall4(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    new C([|Goo()|]);
                }

                C(int i, string s)
                {
                }
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestConstructorCall5(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    new C(s: [|Goo()|]);
                }

                C(int i, string s)
                {
                }
                """, "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestConstructorCallNullableParameter(TestMode mode)
        {
            await TestInClassAsync(
                """
                #nullable enable

                void M()
                {
                    new C([|Goo()|]);
                }

                C(string? s)
                {
                }
                """, "global::System.String?", mode);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112"), CombinatorialData]
        public async Task TestThisConstructorInitializer1(TestMode mode)
        {
            await TestAsync(
                """
                class MyClass
                {
                    public MyClass(int x) : this([|test|])
                    {
                    }
                }
                """, "global::System.Int32", mode);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112"), CombinatorialData]
        public async Task TestThisConstructorInitializer2(TestMode mode)
        {
            await TestAsync(
                """
                class MyClass
                {
                    public MyClass(int x, string y) : this(5, [|test|])
                    {
                    }
                }
                """, "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestThisConstructorInitializerNullableParameter(TestMode mode)
        {
            await TestAsync(
                """
                #nullable enable

                class MyClass
                {
                    public MyClass(string? y) : this([|test|])
                    {
                    }
                }
                """, "global::System.String?", mode);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112"), CombinatorialData]
        public async Task TestBaseConstructorInitializer(TestMode mode)
        {
            await TestAsync(
                """
                class B
                {
                    public B(int x)
                    {
                    }
                }

                class D : B
                {
                    public D() : base([|test|])
                    {
                    }
                }
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestBaseConstructorInitializerNullableParameter(TestMode mode)
        {
            await TestAsync(
                """
                #nullable enable

                class B
                {
                    public B(string? x)
                    {
                    }
                }

                class D : B
                {
                    public D() : base([|test|])
                    {
                    }
                }
                """, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestIndexAccess1(TestMode mode)
        {
            await TestInMethodAsync(
                """
                string[] i;

                i[[|Goo()|]];
                """, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestIndexerCall1(TestMode mode)
            => await TestInMethodAsync(@"this[[|Goo()|]];", "global::System.Int32", mode);

        [Theory, CombinatorialData]
        public async Task TestIndexerCall2(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    this[[|Goo()|]];
                }

                int this[long i] { get; }
                """, "global::System.Int64", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestIndexerCall3(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    this[42, [|Goo()|]];
                }

                int this[int i, string s] { get; }
                """, "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestIndexerCall5(TestMode mode)
        {
            await TestInClassAsync(
                """
                void M()
                {
                    this[s: [|Goo()|]];
                }

                int this[int i, string s] { get; }
                """, "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestArrayInitializerInImplicitArrayCreationSimple(TestMode mode)
        {
            var text =
                """
                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                       var a = new[] { 1, [|2|] };
                  }
                }
                """;

            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestArrayInitializerInImplicitArrayCreation1(TestMode mode)
        {
            var text =
                """
                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                       var a = new[] { Bar(), [|Goo()|] };
                  }

                  int Bar() { return 1; }
                  int Goo() { return 2; }
                }
                """;

            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestArrayInitializerInImplicitArrayCreation2(TestMode mode)
        {
            var text =
                """
                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                       var a = new[] { Bar(), [|Goo()|] };
                  }

                  int Bar() { return 1; }
                }
                """;

            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestArrayInitializerInImplicitArrayCreation3(TestMode mode)
        {
            var text =
                """
                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                       var a = new[] { Bar(), [|Goo()|] };
                  }
                }
                """;

            await TestAsync(text, "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestArrayInitializerInImplicitArrayCreationInferredAsNullable(TestMode mode)
        {
            var text =
                """
                #nullable enable

                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                       var a = new[] { Bar(), [|Goo()|] };
                  }

                  object? Bar() { return null; }
                }
                """;

            await TestAsync(text, "global::System.Object?", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestArrayInitializerInEqualsValueClauseSimple(TestMode mode)
        {
            var text =
                """
                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                       int[] a = { 1, [|2|] };
                  }
                }
                """;

            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestArrayInitializerInEqualsValueClause(TestMode mode)
        {
            var text =
                """
                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                       int[] a = { Bar(), [|Goo()|] };
                  }

                  int Bar() { return 1; }
                }
                """;

            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestArrayInitializerInEqualsValueClauseNullableElement(TestMode mode)
        {
            var text =
                """
                #nullable enable

                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                       string?[] a = { [|Goo()|] };
                  }
                }
                """;

            await TestAsync(text, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        public async Task TestCollectionInitializer1(TestMode mode)
        {
            var text =
                """
                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                    new List<int>() { [|Goo()|] };
                  }
                }
                """;

            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestCollectionInitializerNullableElement(TestMode mode)
        {
            var text =
                """
                #nullable enable

                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                    new List<string?>() { [|Goo()|] };
                  }
                }
                """;

            await TestAsync(text, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        public async Task TestCollectionInitializer2(TestMode mode)
        {
            var text =
                """
                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                    new Dictionary<int,string>() { { [|Goo()|], "" } };
                  }
                }
                """;

            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        public async Task TestCollectionInitializer3(TestMode mode)
        {
            var text =
                """
                using System.Collections.Generic;

                class C
                {
                  void M()
                  {
                    new Dictionary<int,string>() { { 0, [|Goo()|] } };
                  }
                }
                """;

            await TestAsync(text, "global::System.String", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        public async Task TestCustomCollectionInitializerAddMethod1()
        {
            var text =
                """
                class C : System.Collections.IEnumerable
                {
                    void M()
                    {
                        var x = new C() { [|a|] };
                    }

                    void Add(int i) { }
                    void Add(string s, bool b) { }

                    public System.Collections.IEnumerator GetEnumerator()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await TestAsync(text, "global::System.Int32", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        public async Task TestCustomCollectionInitializerAddMethod2(TestMode mode)
        {
            var text =
                """
                class C : System.Collections.IEnumerable
                {
                    void M()
                    {
                        var x = new C() { { "test", [|b|] } };
                    }

                    void Add(int i) { }
                    void Add(string s, bool b) { }

                    public System.Collections.IEnumerator GetEnumerator()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        public async Task TestCustomCollectionInitializerAddMethod3(TestMode mode)
        {
            var text =
                """
                class C : System.Collections.IEnumerable
                {
                    void M()
                    {
                        var x = new C() { { [|s|], true } };
                    }

                    void Add(int i) { }
                    void Add(string s, bool b) { }

                    public System.Collections.IEnumerator GetEnumerator()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await TestAsync(text, "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestCustomCollectionInitializerAddMethodWithNullableParameter(TestMode mode)
        {
            var text =
                """
                class C : System.Collections.IEnumerable
                {
                    void M()
                    {
                        var x = new C() { { "test", [|s|] } };
                    }

                    void Add(int i) { }
                    void Add(string s, string? s2) { }

                    public System.Collections.IEnumerator GetEnumerator()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await TestAsync(text, "global::System.String?", mode);
        }

        [Fact]
        public async Task TestArrayInference1()
        {
            var text =
                """
                class A
                {
                    void Goo()
                    {
                        A[] x = new [|C|][] { };
                    }
                }
                """;

            await TestAsync(text, "global::A", TestMode.Node);
        }

        [Fact]
        public async Task TestArrayInference1_Position()
        {
            var text =
                """
                class A
                {
                    void Goo()
                    {
                        A[] x = new [|C|][] { };
                    }
                }
                """;

            await TestAsync(text, "global::A[]", TestMode.Position);
        }

        [Fact]
        public async Task TestArrayInference2()
        {
            var text =
                """
                class A
                {
                    void Goo()
                    {
                        A[][] x = new [|C|][][] { };
                    }
                }
                """;

            await TestAsync(text, "global::A", TestMode.Node);
        }

        [Fact]
        public async Task TestArrayInference2_Position()
        {
            var text =
                """
                class A
                {
                    void Goo()
                    {
                        A[][] x = new [|C|][][] { };
                    }
                }
                """;

            await TestAsync(text, "global::A[][]", TestMode.Position);
        }

        [Fact]
        public async Task TestArrayInference3()
        {
            var text =
                """
                class A
                {
                    void Goo()
                    {
                        A[][] x = new [|C|][] { };
                    }
                }
                """;

            await TestAsync(text, "global::A[]", TestMode.Node);
        }

        [Fact]
        public async Task TestArrayInference3_Position()
        {
            var text =
                """
                class A
                {
                    void Goo()
                    {
                        A[][] x = new [|C|][] { };
                    }
                }
                """;

            await TestAsync(text, "global::A[][]", TestMode.Position);
        }

        [Theory, CombinatorialData]
        public async Task TestArrayInference4(TestMode mode)
        {
            var text =
                """
                using System;
                class A
                {
                    void Goo()
                    {
                        Func<int, int>[] x = new Func<int, int>[] { [|Bar()|] };
                    }
                }
                """;

            await TestAsync(text, "global::System.Func<global::System.Int32, global::System.Int32>", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538993")]
        public async Task TestInsideLambda2(TestMode mode)
        {
            var text =
                """
                using System;
                class C
                {
                  void M()
                  {
                    Func<int,int> f = i => [|here|]
                  }
                }
                """;

            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestInsideLambdaNullableReturn(TestMode mode)
        {
            var text =
                """
                #nullable enable

                using System;
                class C
                {
                  void M()
                  {
                    Func<int, string?> f = i => [|here|]
                  }
                }
                """;

            await TestAsync(text, "global::System.String?", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539813")]
        public async Task TestPointer1(TestMode mode)
        {
            var text =
                """
                class C
                {
                  void M(int* i)
                  {
                    var q = i[[|Goo()|]];
                  }
                }
                """;

            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539813")]
        public async Task TestDynamic1(TestMode mode)
        {
            var text =
                """
                class C
                {
                  void M(dynamic i)
                  {
                    var q = i[[|Goo()|]];
                  }
                }
                """;

            await TestAsync(text, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestChecked1(TestMode mode)
        {
            var text =
                """
                class C
                {
                  void M()
                  {
                    string q = checked([|Goo()|]);
                  }
                }
                """;

            await TestAsync(text, "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553584")]
        public async Task TestAwaitTaskOfT(TestMode mode)
        {
            var text =
                """
                using System.Threading.Tasks;
                class C
                {
                  void M()
                  {
                    int x = await [|Goo()|];
                  }
                }
                """;

            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Int32>", mode);
        }

        [Theory, CombinatorialData]
        public async Task TestAwaitTaskOfTNullableValue(TestMode mode)
        {
            var text =
                """
                #nullable enable

                using System.Threading.Tasks;
                class C
                {
                  void M()
                  {
                    string? x = await [|Goo()|];
                  }
                }
                """;

            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.String?>", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553584")]
        public async Task TestAwaitTaskOfTaskOfT(TestMode mode)
        {
            var text =
                """
                using System.Threading.Tasks;
                class C
                {
                  void M()
                  {
                    Task<int> x = await [|Goo()|];
                  }
                }
                """;

            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Threading.Tasks.Task<global::System.Int32>>", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553584")]
        public async Task TestAwaitTask(TestMode mode)
        {
            var text =
                """
                using System.Threading.Tasks;
                class C
                {
                  void M()
                  {
                    await [|Goo()|];
                  }
                }
                """;

            await TestAsync(text, "global::System.Threading.Tasks.Task", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617622")]
        public async Task TestLockStatement(TestMode mode)
        {
            var text =
                """
                class C
                {
                  void M()
                  {
                    lock([|Goo()|])
                    {
                    }
                  }
                }
                """;

            await TestAsync(text, "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617622")]
        public async Task TestAwaitExpressionInLockStatement(TestMode mode)
        {
            var text =
                """
                class C
                {
                  async void M()
                  {
                    lock(await [|Goo()|])
                    {
                    }
                  }
                }
                """;

            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Object>", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
        public async Task TestReturnFromAsyncTaskOfT(TestMode mode)
        {
            var markup =
                """
                using System.Threading.Tasks;
                class Program
                {
                    async Task<int> M()
                    {
                        await Task.Delay(1);
                        return [|ab|]
                    }
                }
                """;
            await TestAsync(markup, "global::System.Int32", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853840")]
        public async Task TestAttributeArguments1(TestMode mode)
        {
            var markup =
                """
                [A([|dd|], ee, Y = ff)]
                class AAttribute : System.Attribute
                {
                    public int X;
                    public string Y;

                    public AAttribute(System.DayOfWeek a, double b)
                    {

                    }
                }
                """;
            await TestAsync(markup, "global::System.DayOfWeek", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853840")]
        public async Task TestAttributeArguments2(TestMode mode)
        {
            var markup =
                """
                [A(dd, [|ee|], Y = ff)]
                class AAttribute : System.Attribute
                {
                    public int X;
                    public string Y;

                    public AAttribute(System.DayOfWeek a, double b)
                    {

                    }
                }
                """;
            await TestAsync(markup, "global::System.Double", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853840")]
        public async Task TestAttributeArguments3(TestMode mode)
        {
            var markup =
                """
                [A(dd, ee, Y = [|ff|])]
                class AAttribute : System.Attribute
                {
                    public int X;
                    public string Y;

                    public AAttribute(System.DayOfWeek a, double b)
                    {

                    }
                }
                """;
            await TestAsync(markup, "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/757111")]
        public async Task TestReturnStatementWithinDelegateWithinAMethodCall(TestMode mode)
        {
            var text =
                """
                using System;

                class Program
                {
                    delegate string A(int i);

                    static void Main(string[] args)
                    {
                        B(delegate(int i) { return [|M()|]; });
                    }

                    private static void B(A a)
                    {
                    }
                }
                """;

            await TestAsync(text, "global::System.String", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994388")]
        public async Task TestCatchFilterClause(TestMode mode)
        {
            var text =
                """
                try
                { }
                catch (Exception) if ([|M()|])
                }
                """;
            await TestInMethodAsync(text, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994388")]
        public async Task TestCatchFilterClause1(TestMode mode)
        {
            var text =
                """
                try
                { }
                catch (Exception) if ([|M|])
                }
                """;
            await TestInMethodAsync(text, "global::System.Boolean", mode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994388")]
        public async Task TestCatchFilterClause2()
        {
            var text =
                """
                try
                { }
                catch (Exception) if ([|M|].N)
                }
                """;
            await TestInMethodAsync(text, "global::System.Object", TestMode.Node);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/643")]
        public async Task TestAwaitExpressionWithChainingMethod()
        {
            var text =
                """
                using System;
                using System.Threading.Tasks;

                class C
                {
                    static async void T()
                    {
                        bool x = await [|M()|].ConfigureAwait(false);
                    }
                }
                """;
            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Boolean>", TestMode.Node);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/643")]
        public async Task TestAwaitExpressionWithChainingMethod2()
        {
            var text =
                """
                using System;
                using System.Threading.Tasks;

                class C
                {
                    static async void T()
                    {
                        bool x = await [|M|].ContinueWith(a => { return true; }).ContinueWith(a => { return false; });
                    }
                }
                """;
            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Object>", TestMode.Node);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4233")]
        public async Task TestAwaitExpressionWithGenericMethod1()
        {
            var text =
                """
                using System.Threading.Tasks;

                public class C
                {
                    private async void M()
                    {
                        bool merged = await X([|Test()|]);
                    }

                    private async Task<T> X<T>(T t) { return t; }
                }
                """;
            await TestAsync(text, "global::System.Boolean", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/4233")]
        public async Task TestAwaitExpressionWithGenericMethod2(TestMode mode)
        {
            var text =
                """
                using System.Threading.Tasks;

                public class C
                {
                    private async void M()
                    {
                        bool merged = await Task.Run(() => [|Test()|]);;
                    }

                    private async Task<T> X<T>(T t) { return t; }
                }
                """;
            await TestAsync(text, "global::System.Boolean", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/4483")]
        public async Task TestNullCoalescingOperator1(TestMode mode)
        {
            var text =
    """
    class C
    {
        void M()
        {
            object z = [|a|] ?? null;
        }
    }
    """;
            // In position mode, we are inferring that the thing to the right is an object, because it's being assigned to a local of type object.
            // In node mode, we are inferring the node is an object? because it's to the left of the ??.
            await TestAsync(text, mode == TestMode.Node ? "global::System.Object?" : "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/4483")]
        public async Task TestNullCoalescingOperator2(TestMode mode)
        {
            var text =
    """
    class C
    {
        void M()
        {
            object z = [|a|] ?? b ?? c;
        }
    }
    """;
            // In position mode, we are inferring that the thing to the right is an object, because it's being assigned to a local of type object.
            // In node mode, we are inferring the node is an object? because it's to the left of the ??.
            await TestAsync(text, mode == TestMode.Node ? "global::System.Object?" : "global::System.Object", mode);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/4483")]
        public async Task TestNullCoalescingOperator3(TestMode mode)
        {
            var text =
    """
    class C
    {
        void M()
        {
            object z = a ?? [|b|] ?? c;
        }
    }
    """;
            // In position mode, we are inferring that the thing to the right is an object, because it's to the right of the first ??
            // and thus must be the same type as the object being assigned to.
            // In node mode, we are inferring the node is an object? because it's to the left of the ??.
            await TestAsync(text, mode == TestMode.Node ? "global::System.Object?" : "global::System.Object", mode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5126")]
        public async Task TestSelectLambda()
        {
            var text =
    """
    using System.Collections.Generic;
    using System.Linq;

    class C
    {
        void M(IEnumerable<string> args)
        {
            args = args.Select(a =>[||])
        }
    }
    """;
            await TestAsync(text, "global::System.Object", TestMode.Node);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5126")]
        public async Task TestSelectLambda2()
        {
            var text =
    """
    using System.Collections.Generic;
    using System.Linq;

    class C
    {
        void M(IEnumerable<string> args)
        {
            args = args.Select(a =>[|b|])
        }
    }
    """;
            await TestAsync(text, "global::System.String", TestMode.Node);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/1903")]
        public async Task TestSelectLambda3(TestMode mode)
        {
            var text =
                """
                using System.Collections.Generic;
                using System.Linq;

                class A { }
                class B { }
                class C
                {
                    IEnumerable<B> GetB(IEnumerable<A> a)
                    {
                        return a.Select(i => [|Goo(i)|]);
                    }
                }
                """;
            await TestAsync(text, "global::B", mode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6765")]
        public async Task TestDefaultStatement1()
        {
            var text =
    """
    class C
    {
        static void Main(string[] args)
        {
            System.ConsoleModifiers c = default([||])
        }
    }
    """;
            await TestAsync(text, "global::System.ConsoleModifiers", TestMode.Position);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6765")]
        public async Task TestDefaultStatement2()
        {
            var text =
    """
    class C
    {
        static void Goo(System.ConsoleModifiers arg)
        {
            Goo(default([||])
        }
    }
    """;
            await TestAsync(text, "global::System.ConsoleModifiers", TestMode.Position);
        }

        [Fact]
        public async Task TestWhereCall()
        {
            var text =
    """
    using System.Collections.Generic;
    class C
    {
        void Goo()
        {
            [|ints|].Where(i => i > 10);
        }
    }
    """;
            await TestAsync(text, "global::System.Collections.Generic.IEnumerable<global::System.Int32>", TestMode.Node);
        }

        [Fact]
        public async Task TestWhereCall2()
        {
            var text =
    """
    using System.Collections.Generic;
    class C
    {
        void Goo()
        {
            [|ints|].Where(i => null);
        }
    }
    """;
            await TestAsync(text, "global::System.Collections.Generic.IEnumerable<global::System.Object>", TestMode.Node);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12755")]
        public async Task TestObjectCreationBeforeArrayIndexing()
        {
            var text =
                """
                using System;
                class C
                {
                  void M()
                  {
                        int[] array;
                        C p = new [||]
                        array[4] = 4;
                  }
                }
                """;

            await TestAsync(text, "global::C", TestMode.Position);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25305")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/15468")]
        public async Task TestDeconstruction()
        {
            await TestInMethodAsync(
@"[|(int i, _)|] =", "(global::System.Int32 i, global::System.Object _)", TestMode.Node);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25305")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/15468")]
        public async Task TestDeconstruction2()
        {
            await TestInMethodAsync(
@"(int i, _) =  [||]", "(global::System.Int32 i, global::System.Object _)", TestMode.Position);
        }

        [Fact]
        public async Task TestDeconstructionWithNullableElement()
        {
            await TestInMethodAsync(
@"[|(string? s, _)|] =", "(global::System.String? s, global::System.Object _)", TestMode.Node);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13402")]
        public async Task TestObjectCreationBeforeBlock()
        {
            var text =
                """
                class Program
                {
                    static void Main(string[] args)
                    {
                        Program p = new [||] 
                        { }
                    }
                }
                """;

            await TestAsync(text, "global::Program", TestMode.Position);
        }

        [Theory, CombinatorialData]
        public async Task TestInferringThroughGenericFunctionWithNullableReturn(TestMode mode)
        {
            var text =
                """
                #nullable enable

                class Program
                {
                    static void Main(string[] args)
                    {
                        string? s = Identity([|input|]);
                    }

                    static T Identity<T>(T value) { return value; }
                }
                """;

            await TestAsync(text, "global::System.String?", mode);
        }

        [Fact]
        public async Task TestInferringThroughGenericFunctionMissingArgument()
        {
            var text =
                """
                class Program
                {
                    static void Main(string[] args)
                    {
                        string s = Identity([||]);
                    }

                    static T Identity<T>(T value) { return value; }
                }
                """;

            await TestAsync(text, "global::System.String", TestMode.Position);
        }

        [Theory, CombinatorialData]
        public async Task TestInferringThroughGenericFunctionTooManyArguments(TestMode mode)
        {
            var text =
                """
                class Program
                {
                    static void Main(string[] args)
                    {
                        string s = Identity("test", [||]);
                    }

                    static T Identity<T>(T value) { return value; }
                }
                """;

            await TestAsync(text, "global::System.Object", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/14277"), CombinatorialData]
        public async Task TestValueInNestedTuple1(TestMode mode)
        {
            await TestInMethodAsync(
@"(int, (string, bool)) x = ([|Goo()|], ("""", true));", "global::System.Int32", mode);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/14277"), CombinatorialData]
        public async Task TestValueInNestedTuple2(TestMode mode)
        {
            await TestInMethodAsync(
@"(int, (string, bool)) x = (1, ("""", [|Goo()|]));", "global::System.Boolean", mode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14277")]
        public async Task TestValueInNestedTuple3()
        {
            await TestInMethodAsync(
@"(int, string) x = (1, [||]);", "global::System.String", TestMode.Position);
        }

        [Theory, CombinatorialData]
        public async Task TestInferringInEnumHasFlags(TestMode mode)
        {
            var text =
                """
                using System.IO;

                class Program
                {
                    static void Main(string[] args)
                    {
                        FileInfo f;
                        f.Attributes.HasFlag([|flag|]);
                    }
                }
                """;

            await TestAsync(text, "global::System.IO.FileAttributes", mode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Re")]
        [InlineData("Col")]
        [InlineData("Color.Green or", false)]
        [InlineData("Color.Green or ")]
        [InlineData("(Color.Green or ")] // start of: is (Color.Red or Color.Green) and not Color.Blue
        [InlineData("Color.Green or Re")]
        [InlineData("Color.Green or Color.Red or ")]
        [InlineData("Color.Green orWrittenWrong ", false)]
        [InlineData("not ")]
        [InlineData("not Re")]
        public async Task TestEnumInPatterns_Is_ConstUnaryAndBinaryPattern(string isPattern, bool shouldInferColor = true)
        {
            var markup = @$"
class C
{{
    public enum Color
    {{
        Red,
        Green,
    }}

    public void M(Color c)
    {{
        var isRed = c is {isPattern}[||];
    }}
}}
";

            var expectedType = shouldInferColor
                ? "global::C.Color"
                : "global::System.Object";
            await TestAsync(markup, expectedType, TestMode.Position);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Col")]
        [InlineData("Color.R")]
        [InlineData("Red")]
        [InlineData("Color.Green or ")]
        [InlineData("Color.Green or Re")]
        [InlineData("not ")]
        [InlineData("not Re")]
        public async Task TestEnumInPatterns_Is_PropertyPattern(string partialWritten)
        {
            var markup = @$"
public enum Color
{{
    Red,
    Green,
}}

class C
{{
    public Color Color {{ get; }}

    public void M()
    {{
        var isRed = this is {{ Color: {partialWritten}[||]
    }}
}}
";
            await TestAsync(markup, "global::Color", TestMode.Position);
        }

        [Fact]
        public async Task TestEnumInPatterns_SwitchStatement_PropertyPattern()
        {
            var markup = """
                public enum Color
                {
                    Red,
                    Green,
                }

                class C
                {
                    public Color Color { get; }

                    public void M()
                    {
                        switch (this)
                        {
                            case { Color: [||]
                    }
                }
                """;
            await TestAsync(markup, "global::Color", TestMode.Position);
        }

        [Fact]
        public async Task TestEnumInPatterns_SwitchExpression_PropertyPattern()
        {
            var markup = """
                public enum Color
                {
                    Red,
                    Green,
                }

                class C
                {
                    public Color Color { get; }

                    public void M()
                    {
                        var isRed = this switch
                        {
                            { Color: [||]
                    }
                }
                """;
            await TestAsync(markup, "global::Color", TestMode.Position);
        }

        [Fact]
        public async Task TestEnumInPatterns_SwitchStatement_ExtendedPropertyPattern()
        {
            var markup = """
                public enum Color
                {
                    Red,
                    Green,
                }

                class C
                {
                    public C AnotherC { get; }
                    public Color Color { get; }

                    public void M()
                    {
                        switch (this)
                        {
                            case { AnotherC.Color: [||]
                    }
                }
                """;
            await TestAsync(markup, "global::Color", TestMode.Position);
        }

        [Fact]
        public async Task TestEnumInPatterns_SwitchStatement_ExtendedPropertyPattern_Field()
        {
            var markup = """
                public enum Color
                {
                    Red,
                    Green,
                }

                class C
                {
                    public C AnotherC { get; }
                    public Color Color;

                    public void M()
                    {
                        switch (this)
                        {
                            case { AnotherC.Color: [||]
                    }
                }
                """;
            await TestAsync(markup, "global::Color", TestMode.Position);
        }
    }
}
