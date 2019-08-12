// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.TypeInferrer;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TypeInferrer
{
    public partial class TypeInferrerTests : TypeInferrerTestBase<CSharpTestWorkspaceFixture>
    {
        public TypeInferrerTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        protected override async Task TestWorkerAsync(Document document, TextSpan textSpan, string expectedType, bool useNodeStartPosition)
        {
            var root = await document.GetSyntaxRootAsync();
            var node = FindExpressionSyntaxFromSpan(root, textSpan);
            var typeInference = document.GetLanguageService<ITypeInferenceService>();

            var inferredType = useNodeStartPosition
                ? typeInference.InferType(await document.GetSemanticModelForSpanAsync(new TextSpan(node?.SpanStart ?? textSpan.Start, 0), CancellationToken.None), node?.SpanStart ?? textSpan.Start, objectAsDefault: true, cancellationToken: CancellationToken.None)
                : typeInference.InferType(await document.GetSemanticModelForSpanAsync(node?.Span ?? textSpan, CancellationToken.None), node, objectAsDefault: true, cancellationToken: CancellationToken.None);
            var typeSyntax = inferredType.GenerateTypeSyntax().NormalizeWhitespace();
            Assert.Equal(expectedType, typeSyntax.ToString());
        }

        private async Task TestInClassAsync(string text, string expectedType)
        {
            text = @"class C
{
    $
}".Replace("$", text);
            await TestAsync(text, expectedType);
        }

        private async Task TestInMethodAsync(string text, string expectedType, bool testNode = true, bool testPosition = true)
        {
            text = @"class C
{
    void M()
    {
        $
    }
}".Replace("$", text);
            await TestAsync(text, expectedType, testNode: testNode, testPosition: testPosition);
        }

        private ExpressionSyntax FindExpressionSyntaxFromSpan(SyntaxNode root, TextSpan textSpan)
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

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConditional1()
        {
            // We do not support position inference here as we're before the ? and we only look
            // backwards to infer a type here.
            await TestInMethodAsync(
@"var q = [|Goo()|] ? 1 : 2;", "global::System.Boolean",
                testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConditional2()
        {
            await TestInMethodAsync(
@"var q = a ? [|Goo()|] : 2;", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConditional3()
        {
            await TestInMethodAsync(
@"var q = a ? """" : [|Goo()|];", "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestVariableDeclarator1()
        {
            await TestInMethodAsync(
@"int q = [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestVariableDeclarator2()
        {
            await TestInMethodAsync(
@"var q = [|Goo()|];", "global::System.Object");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestVariableDeclaratorNullableReferenceType()
        {
            await TestInMethodAsync(
@"#nullable enable
string? q = [|Goo()|];", "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCoalesce1()
        {
            await TestInMethodAsync(
@"var q = [|Goo()|] ?? 1;", "global::System.Int32?", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCoalesce2()
        {
            await TestInMethodAsync(
@"bool? b;
var q = b ?? [|Goo()|];", "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCoalesce3()
        {
            await TestInMethodAsync(
@"string s;
var q = s ?? [|Goo()|];", "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCoalesce4()
        {
            await TestInMethodAsync(
@"var q = [|Goo()|] ?? string.Empty;", "global::System.String", testPosition: false);
        }

        // This is skipped for now. This is a case where we know we can unilaterally mark the reference type as nullable, as long as the user has #nullable enable on.
        // But right now there's no compiler API to know if it is, so we have to skip this. Once there is an API, we'll have it always return a nullable reference type
        // and we'll remove the ? if it's in a non-nullable context no differently than we always generate types fully qualified and then clean up based on context.
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/37178"), Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCoalesceInNullableEnabled()
        {
            await TestInMethodAsync(
@"#nullable enable
var q = [|Goo()|] ?? string.Empty;", "global::System.String?", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBinaryExpression1()
        {
            await TestInMethodAsync(
@"string s;
var q = s + [|Goo()|];", "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBinaryExpression2()
        {
            await TestInMethodAsync(
@"var s;
var q = s || [|Goo()|];", "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBinaryOperator1()
        {
            await TestInMethodAsync(
@"var q = x << [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBinaryOperator2()
        {
            await TestInMethodAsync(
@"var q = x >> [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAssignmentOperator3()
        {
            await TestInMethodAsync(
@"var q <<= [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAssignmentOperator4()
        {
            await TestInMethodAsync(
@"var q >>= [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestOverloadedConditionalLogicalOperatorsInferBool()
        {
            await TestAsync(
@"using System;

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
}", "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestConditionalLogicalOrOperatorAlwaysInfersBool()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = a || [|7|];
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestConditionalLogicalAndOperatorAlwaysInfersBool()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = a && [|7|];
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] | true;
    }
}";
            await TestAsync(text, "global::System.Boolean", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] | b | c || d;
    }
}";
            await TestAsync(text, "global::System.Boolean", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference3()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = a | b | [|c|] || d;
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference4()
        {
            var text = @"using System;
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
}";
            await TestAsync(text, "Program", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference5()
        {
            var text = @"using System;
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
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference6()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if (([|x|] | y) != 0) {}
    }
}";
            await TestAsync(text, "global::System.Int32", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrOperatorInference7()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] | y) {}
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] & true;
    }
}";
            await TestAsync(text, "global::System.Boolean", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] & b & c && d;
    }
}";
            await TestAsync(text, "global::System.Boolean", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference3()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = a & b & [|c|] && d;
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference4()
        {
            var text = @"using System;
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
}";
            await TestAsync(text, "Program", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference5()
        {
            var text = @"using System;
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
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference6()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if (([|x|] & y) != 0) {}
    }
}";
            await TestAsync(text, "global::System.Int32", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndOperatorInference7()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] & y) {}
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] ^ true;
    }
}";
            await TestAsync(text, "global::System.Boolean", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] ^ b ^ c && d;
    }
}";
            await TestAsync(text, "global::System.Boolean", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference3()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = a ^ b ^ [|c|] && d;
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference4()
        {
            var text = @"using System;
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
}";
            await TestAsync(text, "Program", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference5()
        {
            var text = @"using System;
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
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference6()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if (([|x|] ^ y) != 0) {}
    }
}";
            await TestAsync(text, "global::System.Int32", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorOperatorInference7()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] ^ y) {}
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrEqualsOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] |= y) {}
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalOrEqualsOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        int z = [|x|] |= y;
    }
}";
            await TestAsync(text, "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndEqualsOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] &= y) {}
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalAndEqualsOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        int z = [|x|] &= y;
    }
}";
            await TestAsync(text, "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorEqualsOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] ^= y) {}
    }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617633")]
        public async Task TestLogicalXorEqualsOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        int z = [|x|] ^= y;
    }
}";
            await TestAsync(text, "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInConstructor()
        {
            await TestInClassAsync(
@"C()
{
    return [|Goo()|];
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInDestructor()
        {
            await TestInClassAsync(
@"~C()
{
    return [|Goo()|];
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInMethod()
        {
            await TestInClassAsync(
@"int M()
{
    return [|Goo()|];
}", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInMethodNullableReference()
        {
            await TestInClassAsync(
@"#nullable enable
string? M()
{
    return [|Goo()|];
}", "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInVoidMethod()
        {
            await TestInClassAsync(
@"void M()
{
    return [|Goo()|];
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskOfTMethod()
        {
            await TestInClassAsync(
@"async System.Threading.Tasks.Task<int> M()
{
    return [|Goo()|];
}", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskOfTMethodNestedNullability()
        {
            await TestInClassAsync(
@"async System.Threading.Tasks.Task<string?> M()
{
    return [|Goo()|];
}", "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskMethod()
        {
            await TestInClassAsync(
@"async System.Threading.Tasks.Task M()
{
    return [|Goo()|];
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncVoidMethod()
        {
            await TestInClassAsync(
@"async void M()
{
    return [|Goo()|];
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInOperator()
        {
            await TestInClassAsync(
@"public static C operator ++(C c)
{
    return [|Goo()|];
}", "global::C");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInConversionOperator()
        {
            await TestInClassAsync(
@"public static implicit operator int(C c)
{
    return [|Goo()|];
}", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInPropertyGetter()
        {
            await TestInClassAsync(
@"int P
{
    get
    {
        return [|Goo()|];
    }
}", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInPropertyGetterNullableReference()
        {
            await TestInClassAsync(
@"#nullable enable
string? P
{
    get
    {
        return [|Goo()|];
    }
}", "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInPropertySetter()
        {
            await TestInClassAsync(
@"int P
{
    set
    {
        return [|Goo()|];
    }
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInIndexerGetter()
        {
            await TestInClassAsync(
@"int this[int i]
{
    get
    {
        return [|Goo()|];
    }
}", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInIndexerGetterNullableReference()
        {
            await TestInClassAsync(
@"#nullable enable
string? this[int i]
{
    get
    {
        return [|Goo()|];
    }
}", "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInIndexerSetter()
        {
            await TestInClassAsync(
@"int this[int i]
{
    set
    {
        return [|Goo()|];
    }
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInEventAdder()
        {
            await TestInClassAsync(
@"event System.EventHandler E
{
    add
    {
        return [|Goo()|];
    }
    remove { }
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInEventRemover()
        {
            await TestInClassAsync(
@"event System.EventHandler E
{
    add { }
    remove
    {
        return [|Goo()|];
    }
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInLocalFunction()
        {
            await TestInClassAsync(
@"void M()
{
    int F()
    {
        return [|Goo()|];
    }
}", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInLocalFunctionNullableReference()
        {
            await TestInClassAsync(
@"#nullable enable
void M()
{
    string? F()
    {
        return [|Goo()|];
    }
}", "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskOfTLocalFunction()
        {
            await TestInClassAsync(
@"void M()
{
    async System.Threading.Tasks.Task<int> F()
    {
        return [|Goo()|];
    }
}", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskLocalFunction()
        {
            await TestInClassAsync(
@"void M()
{
    async System.Threading.Tasks.Task F()
    {
        return [|Goo()|];
    }
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncVoidLocalFunction()
        {
            await TestInClassAsync(
@"void M()
{
    async void F()
    {
        return [|Goo()|];
    }
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedConstructor()
        {
            await TestInClassAsync(
@"C() => [|Goo()|];", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedDestructor()
        {
            await TestInClassAsync(
@"~C() => [|Goo()|];", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedMethod()
        {
            await TestInClassAsync(
@"int M() => [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedVoidMethod()
        {
            await TestInClassAsync(
@"void M() => [|Goo()|];", "void");
        }

        [WorkItem(27647, "https://github.com/dotnet/roslyn/issues/27647")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedAsyncTaskOfTMethod()
        {
            await TestInClassAsync(
@"async System.Threading.Tasks.Task<int> M() => [|Goo()|];", "global::System.Int32");
        }

        [WorkItem(27647, "https://github.com/dotnet/roslyn/issues/27647")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedAsyncTaskOfTMethodNullableReference()
        {
            await TestInClassAsync(
@"#nullable enable
async System.Threading.Tasks.Task<string?> M() => [|Goo()|];", "global::System.String?");
        }

        [WorkItem(27647, "https://github.com/dotnet/roslyn/issues/27647")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedAsyncTaskMethod()
        {
            await TestInClassAsync(
@"async System.Threading.Tasks.Task M() => [|Goo()|];", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedAsyncVoidMethod()
        {
            await TestInClassAsync(
@"async void M() => [|Goo()|];", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedOperator()
        {
            await TestInClassAsync(
@"public static C operator ++(C c) => [|Goo()|];", "global::C");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedConversionOperator()
        {
            await TestInClassAsync(
@"public static implicit operator int(C c) => [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedProperty()
        {
            await TestInClassAsync(
@"int P => [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedIndexer()
        {
            await TestInClassAsync(
@"int this[int i] => [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedPropertyGetter()
        {
            await TestInClassAsync(
@"int P { get => [|Goo()|]; }", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedPropertySetter()
        {
            await TestInClassAsync(
@"int P { set => [|Goo()|]; }", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedIndexerGetter()
        {
            await TestInClassAsync(
@"int this[int i] { get => [|Goo()|]; }", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedIndexerSetter()
        {
            await TestInClassAsync(
@"int this[int i] { set => [|Goo()|]; }", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedEventAdder()
        {
            await TestInClassAsync(
@"event System.EventHandler E { add => [|Goo()|]; remove { } }", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedEventRemover()
        {
            await TestInClassAsync(
@"event System.EventHandler E { add { } remove => [|Goo()|]; }", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedLocalFunction()
        {
            await TestInClassAsync(
@"void M()
{
    int F() => [|Goo()|];
}", "global::System.Int32");
        }

        [WorkItem(27647, "https://github.com/dotnet/roslyn/issues/27647")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedAsyncTaskOfTLocalFunction()
        {
            await TestInClassAsync(
@"void M()
{
    async System.Threading.Tasks.Task<int> F() => [|Goo()|];
}", "global::System.Int32");
        }

        [WorkItem(27647, "https://github.com/dotnet/roslyn/issues/27647")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedAsyncTaskLocalFunction()
        {
            await TestInClassAsync(
@"void M()
{
    async System.Threading.Tasks.Task F() => [|Goo()|];
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionBodiedAsyncVoidLocalFunction()
        {
            await TestInClassAsync(
@"void M()
{
    async void F() => [|Goo()|];
}", "void");
        }

        [WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
        [Theory, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [InlineData("IEnumerable")]
        [InlineData("IEnumerator")]
        [InlineData("InvalidGenericType")]
        public async Task TestYieldReturnInMethod(string returnTypeName)
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
            await TestAsync(markup, "global::System.Int32");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [InlineData("IEnumerable")]
        [InlineData("IEnumerator")]
        [InlineData("InvalidGenericType")]
        public async Task TestYieldReturnInMethodNullableReference(string returnTypeName)
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
            await TestAsync(markup, "global::System.String?");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [InlineData("IAsyncEnumerable")]
        [InlineData("IAsyncEnumerator")]
        [InlineData("InvalidGenericType")]
        public async Task TestYieldReturnInAsyncMethod(string returnTypeName)
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
            await TestAsync(markup, "global::System.Int32");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [InlineData("int[]")]
        [InlineData("InvalidNonGenericType")]
        [InlineData("InvalidGenericType<int, int>")]
        public async Task TestYieldReturnInvalidTypeInMethod(string returnType)
        {
            var markup =
$@"class C
{{
    {returnType} M()
    {{
        yield return [|abc|]
    }}
}}";
            await TestAsync(markup, "global::System.Object");
        }

        [WorkItem(30235, "https://github.com/dotnet/roslyn/issues/30235")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestYieldReturnInLocalFunction()
        {
            var markup =
@"using System.Collections.Generic;

class C
{
    void M()
    {
        IEnumerable<int> F()
        {
            yield return [|abc|]
        }
    }
}";
            await TestAsync(markup, "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestYieldReturnInPropertyGetter()
        {
            var markup =
@"using System.Collections.Generic;

class C
{
    IEnumerable<int> P
    {
        get
        {
            yield return [|abc|]
        }
    }
}";
            await TestAsync(markup, "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestYieldReturnInPropertySetter()
        {
            var markup =
@"using System.Collections.Generic;

class C
{
    IEnumerable<int> P
    {
        set
        {
            yield return [|abc|]
        }
    }
}";
            await TestAsync(markup, "global::System.Object");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestYieldReturnAsGlobalStatement()
        {
            await TestAsync(
@"yield return [|abc|]", "global::System.Object", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInSimpleLambda()
        {
            await TestInMethodAsync(
@"System.Func<string, int> f = s =>
{
    return [|Goo()|];
};", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInParenthesizedLambda()
        {
            await TestInMethodAsync(
@"System.Func<int> f = () =>
{
    return [|Goo()|];
};", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInLambdaWithNullableReturn()
        {
            await TestInMethodAsync(
@"#nullable enable
System.Func<string, string?> f = s =>
{
    return [|Goo()|];
};", "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAnonymousMethod()
        {
            await TestInMethodAsync(
@"System.Func<int> f = delegate ()
{
    return [|Goo()|];
};", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAnonymousMethodWithNullableReturn()
        {
            await TestInMethodAsync(
@"#nullable enable
System.Func<string?> f = delegate ()
{
    return [|Goo()|];
};", "global::System.String?");
        }

        [WorkItem(4486, "https://github.com/dotnet/roslyn/issues/4486")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskOfTSimpleLambda()
        {
            await TestInMethodAsync(
@"System.Func<string, System.Threading.Tasks.Task<int>> f = async s =>
{
    return [|Goo()|];
};", "global::System.Int32");
        }

        [WorkItem(4486, "https://github.com/dotnet/roslyn/issues/4486")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskOfTParenthesizedLambda()
        {
            await TestInMethodAsync(
@"System.Func<System.Threading.Tasks.Task<int>> f = async () =>
{
    return [|Goo()|];
};", "global::System.Int32");
        }

        [WorkItem(4486, "https://github.com/dotnet/roslyn/issues/4486")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskOfTAnonymousMethod()
        {
            await TestInMethodAsync(
@"System.Func<System.Threading.Tasks.Task<int>> f = async delegate ()
{
    return [|Goo()|];
};", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskOfTAnonymousMethodWithNullableReference()
        {
            await TestInMethodAsync(
@"#nullable enable
System.Func<System.Threading.Tasks.Task<string?>> f = async delegate ()
{
    return [|Goo()|];
};", "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskSimpleLambda()
        {
            await TestInMethodAsync(
@"System.Func<string, System.Threading.Tasks.Task> f = async s =>
{
    return [|Goo()|];
};", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskParenthesizedLambda()
        {
            await TestInMethodAsync(
@"System.Func<System.Threading.Tasks.Task> f = async () =>
{
    return [|Goo()|];
};", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncTaskAnonymousMethod()
        {
            await TestInMethodAsync(
@"System.Func<System.Threading.Tasks.Task> f = async delegate ()
{
    return [|Goo()|];
};", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncVoidSimpleLambda()
        {
            await TestInMethodAsync(
@"System.Action<string> f = async s =>
{
    return [|Goo()|];
};", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncVoidParenthesizedLambda()
        {
            await TestInMethodAsync(
@"System.Action f = async () =>
{
    return [|Goo()|];
};", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInAsyncVoidAnonymousMethod()
        {
            await TestInMethodAsync(
@"System.Action f = async delegate ()
{
    return [|Goo()|];
};", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnAsGlobalStatement()
        {
            await TestAsync(
@"return [|Goo()|];", "global::System.Object", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestSimpleLambda()
        {
            await TestInMethodAsync(
@"System.Func<string, int> f = s => [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestParenthesizedLambda()
        {
            await TestInMethodAsync(
@"System.Func<int> f = () => [|Goo()|];", "global::System.Int32");
        }

        [WorkItem(30232, "https://github.com/dotnet/roslyn/issues/30232")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAsyncTaskOfTSimpleLambda()
        {
            await TestInMethodAsync(
@"System.Func<string, System.Threading.Tasks.Task<int>> f = async s => [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAsyncTaskOfTSimpleLambdaWithNullableReturn()
        {
            await TestInMethodAsync(
@"#nullable enable
System.Func<string, System.Threading.Tasks.Task<string?>> f = async s => [|Goo()|];", "global::System.String?");
        }

        [WorkItem(30232, "https://github.com/dotnet/roslyn/issues/30232")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAsyncTaskOfTParenthesizedLambda()
        {
            await TestInMethodAsync(
@"System.Func<System.Threading.Tasks.Task<int>> f = async () => [|Goo()|];", "global::System.Int32");
        }

        [WorkItem(30232, "https://github.com/dotnet/roslyn/issues/30232")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAsyncTaskSimpleLambda()
        {
            await TestInMethodAsync(
@"System.Func<string, System.Threading.Tasks.Task> f = async s => [|Goo()|];", "void");
        }

        [WorkItem(30232, "https://github.com/dotnet/roslyn/issues/30232")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAsyncTaskParenthesizedLambda()
        {
            await TestInMethodAsync(
@"System.Func<System.Threading.Tasks.Task> f = async () => [|Goo()|];", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAsyncVoidSimpleLambda()
        {
            await TestInMethodAsync(
@"System.Action<string> f = async s => [|Goo()|];", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAsyncVoidParenthesizedLambda()
        {
            await TestInMethodAsync(
@"System.Action f = async () => [|Goo()|];", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionTreeSimpleLambda()
        {
            await TestInMethodAsync(
@"System.Linq.Expressions.Expression<System.Func<string, int>> f = s => [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestExpressionTreeParenthesizedLambda()
        {
            await TestInMethodAsync(
@"System.Linq.Expressions.Expression<System.Func<int>> f = () => [|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestThrow()
        {
            await TestInMethodAsync(
@"throw [|Goo()|];", "global::System.Exception");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCatch()
        {
            await TestInMethodAsync("try { } catch ([|Goo|] ex) { }", "global::System.Exception");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIf()
        {
            await TestInMethodAsync(@"if ([|Goo()|]) { }", "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestWhile()
        {
            await TestInMethodAsync(@"while ([|Goo()|]) { }", "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestDo()
        {
            await TestInMethodAsync(@"do { } while ([|Goo()|])", "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestFor1()
        {
            await TestInMethodAsync(
@"for (int i = 0; [|Goo()|];

i++) { }", "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestFor2()
        {
            await TestInMethodAsync(@"for (string i = [|Goo()|]; ; ) { }", "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestFor3()
        {
            await TestInMethodAsync(@"for (var i = [|Goo()|]; ; ) { }", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestForNullableReference()
        {
            await TestInMethodAsync(
@"#nullable enable
for (string? s = [|Goo()|]; ; ) { }", "global::System.String?");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestUsing1()
        {
            await TestInMethodAsync(@"using ([|Goo()|]) { }", "global::System.IDisposable");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestUsing2()
        {
            await TestInMethodAsync(@"using (int i = [|Goo()|]) { }", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestUsing3()
        {
            await TestInMethodAsync(@"using (var v = [|Goo()|]) { }", "global::System.IDisposable");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestForEach()
        {
            await TestInMethodAsync(@"foreach (int v in [|Goo()|]) { }", "global::System.Collections.Generic.IEnumerable<global::System.Int32>");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/37309"), Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestForEachNullableElements()
        {
            await TestInMethodAsync(
@"#nullable enable
foreach (string? v in [|Goo()|]) { }", "global::System.Collections.Generic.IEnumerable<global::System.String?>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPrefixExpression1()
        {
            await TestInMethodAsync(
@"var q = +[|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPrefixExpression2()
        {
            await TestInMethodAsync(
@"var q = -[|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPrefixExpression3()
        {
            await TestInMethodAsync(
@"var q = ~[|Goo()|];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPrefixExpression4()
        {
            await TestInMethodAsync(
@"var q = ![|Goo()|];", "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPrefixExpression5()
        {
            await TestInMethodAsync(
@"var q = System.DayOfWeek.Monday & ~[|Goo()|];", "global::System.DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayRankSpecifier()
        {
            await TestInMethodAsync(
@"var q = new string[[|Goo()|]];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestSwitch1()
        {
            await TestInMethodAsync(@"switch ([|Goo()|]) { }", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestSwitch2()
        {
            await TestInMethodAsync(@"switch ([|Goo()|]) { default: }", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestSwitch3()
        {
            await TestInMethodAsync(@"switch ([|Goo()|]) { case ""a"": }", "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCall1()
        {
            await TestInMethodAsync(
@"Bar([|Goo()|]);", "global::System.Object");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCall2()
        {
            await TestInClassAsync(
@"void M()
{
    Bar([|Goo()|]);
}

void Bar(int i);", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCall3()
        {
            await TestInClassAsync(
@"void M()
{
    Bar([|Goo()|]);
}

void Bar();", "global::System.Object");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCall4()
        {
            await TestInClassAsync(
@"void M()
{
    Bar([|Goo()|]);
}

void Bar(int i, string s);", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCall5()
        {
            await TestInClassAsync(
@"void M()
{
    Bar(s: [|Goo()|]);
}

void Bar(int i, string s);", "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCallNullableReference()
        {
            await TestInClassAsync(
@"void M()
{
    Bar([|Goo()|]);
}

void Bar(string? s);", "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCall1()
        {
            await TestInMethodAsync(
@"new C([|Goo()|]);", "global::System.Object");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCall2()
        {
            await TestInClassAsync(
@"void M()
{
    new C([|Goo()|]);
}

C(int i)
{
}", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCall3()
        {
            await TestInClassAsync(
@"void M()
{
    new C([|Goo()|]);
}

C()
{
}", "global::System.Object");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCall4()
        {
            await TestInClassAsync(
@"void M()
{
    new C([|Goo()|]);
}

C(int i, string s)
{
}", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCall5()
        {
            await TestInClassAsync(
@"void M()
{
    new C(s: [|Goo()|]);
}

C(int i, string s)
{
}", "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCallNullableParameter()
        {
            await TestInClassAsync(
@"#nullable enable

void M()
{
    new C([|Goo()|]);
}

C(string? s)
{
}", "global::System.String?");
        }

        [WorkItem(858112, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestThisConstructorInitializer1()
        {
            await TestAsync(
@"class MyClass
{
    public MyClass(int x) : this([|test|])
    {
    }
}", "global::System.Int32");
        }

        [WorkItem(858112, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestThisConstructorInitializer2()
        {
            await TestAsync(
@"class MyClass
{
    public MyClass(int x, string y) : this(5, [|test|])
    {
    }
}", "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestThisConstructorInitializerNullableParameter()
        {
            await TestAsync(
@"#nullable enable

class MyClass
{
    public MyClass(string? y) : this([|test|])
    {
    }
}", "global::System.String?");
        }

        [WorkItem(858112, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBaseConstructorInitializer()
        {
            await TestAsync(
@"class B
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
}", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBaseConstructorInitializerNullableParameter()
        {
            await TestAsync(
@"#nullable enable

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
}", "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIndexAccess1()
        {
            await TestInMethodAsync(
@"string[] i;

i[[|Goo()|]];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIndexerCall1()
        {
            await TestInMethodAsync(@"this[[|Goo()|]];", "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIndexerCall2()
        {
            await TestInClassAsync(
@"void M()
{
    this[[|Goo()|]];
}

int this[long i] { get; }", "global::System.Int64");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIndexerCall3()
        {
            await TestInClassAsync(
@"void M()
{
    this[42, [|Goo()|]];
}

int this[int i, string s] { get; }", "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIndexerCall5()
        {
            await TestInClassAsync(
@"void M()
{
    this[s: [|Goo()|]];
}

int this[int i, string s] { get; }", "global::System.String");
        }

        [Fact]
        public async Task TestArrayInitializerInImplicitArrayCreationSimple()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
       var a = new[] { 1, [|2|] };
  }
}";

            await TestAsync(text, "global::System.Int32");
        }

        [Fact]
        public async Task TestArrayInitializerInImplicitArrayCreation1()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
       var a = new[] { Bar(), [|Goo()|] };
  }

  int Bar() { return 1; }
  int Goo() { return 2; }
}";

            await TestAsync(text, "global::System.Int32");
        }

        [Fact]
        public async Task TestArrayInitializerInImplicitArrayCreation2()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
       var a = new[] { Bar(), [|Goo()|] };
  }

  int Bar() { return 1; }
}";

            await TestAsync(text, "global::System.Int32");
        }

        [Fact]
        public async Task TestArrayInitializerInImplicitArrayCreation3()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
       var a = new[] { Bar(), [|Goo()|] };
  }
}";

            await TestAsync(text, "global::System.Object");
        }

        [Fact]
        public async Task TestArrayInitializerInImplicitArrayCreationInferredAsNullable()
        {
            var text =
@"#nullable enable

using System.Collections.Generic;

class C
{
  void M()
  {
       var a = new[] { Bar(), [|Goo()|] };
  }

  object? Bar() { return null; }
}";

            await TestAsync(text, "global::System.Object?");
        }

        [Fact]
        public async Task TestArrayInitializerInEqualsValueClauseSimple()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
       int[] a = { 1, [|2|] };
  }
}";

            await TestAsync(text, "global::System.Int32");
        }

        [Fact]
        public async Task TestArrayInitializerInEqualsValueClause()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
       int[] a = { Bar(), [|Goo()|] };
  }

  int Bar() { return 1; }
}";

            await TestAsync(text, "global::System.Int32");
        }

        [Fact]
        public async Task TestArrayInitializerInEqualsValueClauseNullableElement()
        {
            var text =
@"#nullable enable

using System.Collections.Generic;

class C
{
  void M()
  {
       string?[] a = { [|Goo()|] };
  }
}";

            await TestAsync(text, "global::System.String?");
        }

        [Fact]
        [WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCollectionInitializer1()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
    new List<int>() { [|Goo()|] };
  }
}";

            await TestAsync(text, "global::System.Int32");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCollectionInitializerNullableElement()
        {
            var text =
@"#nullable enable

using System.Collections.Generic;

class C
{
  void M()
  {
    new List<string?>() { [|Goo()|] };
  }
}";

            await TestAsync(text, "global::System.String?");
        }

        [Fact]
        [WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCollectionInitializer2()
        {
            var text =
@"
using System.Collections.Generic;

class C
{
  void M()
  {
    new Dictionary<int,string>() { { [|Goo()|], """" } };
  }
}";

            await TestAsync(text, "global::System.Int32");
        }

        [Fact]
        [WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCollectionInitializer3()
        {
            var text =
@"
using System.Collections.Generic;

class C
{
  void M()
  {
    new Dictionary<int,string>() { { 0, [|Goo()|] } };
  }
}";

            await TestAsync(text, "global::System.String");
        }

        [Fact]
        [WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCustomCollectionInitializerAddMethod1()
        {
            var text =
@"class C : System.Collections.IEnumerable
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
}";

            await TestAsync(text, "global::System.Int32", testPosition: false);
        }

        [Fact]
        [WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCustomCollectionInitializerAddMethod2()
        {
            var text =
@"class C : System.Collections.IEnumerable
{
    void M()
    {
        var x = new C() { { ""test"", [|b|] } };
    }

    void Add(int i) { }
    void Add(string s, bool b) { }

    public System.Collections.IEnumerator GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}";

            await TestAsync(text, "global::System.Boolean");
        }

        [Fact]
        [WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCustomCollectionInitializerAddMethod3()
        {
            var text =
@"class C : System.Collections.IEnumerable
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
}";

            await TestAsync(text, "global::System.String");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCustomCollectionInitializerAddMethodWithNullableParameter()
        {
            var text =
@"class C : System.Collections.IEnumerable
{
    void M()
    {
        var x = new C() { { ""test"", [|s|] } };
    }

    void Add(int i) { }
    void Add(string s, string? s2) { }

    public System.Collections.IEnumerator GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}";

            await TestAsync(text, "global::System.String?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference1()
        {
            var text =
@"
class A
{
    void Goo()
    {
        A[] x = new [|C|][] { };
    }
}";

            await TestAsync(text, "global::A", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference1_Position()
        {
            var text =
@"
class A
{
    void Goo()
    {
        A[] x = new [|C|][] { };
    }
}";

            await TestAsync(text, "global::A[]", testNode: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference2()
        {
            var text =
@"
class A
{
    void Goo()
    {
        A[][] x = new [|C|][][] { };
    }
}";

            await TestAsync(text, "global::A", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference2_Position()
        {
            var text =
@"
class A
{
    void Goo()
    {
        A[][] x = new [|C|][][] { };
    }
}";

            await TestAsync(text, "global::A[][]", testNode: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference3()
        {
            var text =
@"
class A
{
    void Goo()
    {
        A[][] x = new [|C|][] { };
    }
}";

            await TestAsync(text, "global::A[]", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference3_Position()
        {
            var text =
@"
class A
{
    void Goo()
    {
        A[][] x = new [|C|][] { };
    }
}";

            await TestAsync(text, "global::A[][]", testNode: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference4()
        {
            var text =
@"
using System;
class A
{
    void Goo()
    {
        Func<int, int>[] x = new Func<int, int>[] { [|Bar()|] };
    }
}";

            await TestAsync(text, "global::System.Func<global::System.Int32, global::System.Int32>");
        }

        [WorkItem(538993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538993")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestInsideLambda2()
        {
            var text =
@"using System;
class C
{
  void M()
  {
    Func<int,int> f = i => [|here|]
  }
}";

            await TestAsync(text, "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestInsideLambdaNullableReturn()
        {
            var text =
@"#nullable enable

using System;
class C
{
  void M()
  {
    Func<int, string?> f = i => [|here|]
  }
}";

            await TestAsync(text, "global::System.String?");
        }

        [WorkItem(539813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539813")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPointer1()
        {
            var text =
@"class C
{
  void M(int* i)
  {
    var q = i[[|Goo()|]];
  }
}";

            await TestAsync(text, "global::System.Int32");
        }

        [WorkItem(539813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539813")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestDynamic1()
        {
            var text =
@"class C
{
  void M(dynamic i)
  {
    var q = i[[|Goo()|]];
  }
}";

            await TestAsync(text, "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestChecked1()
        {
            var text =
@"class C
{
  void M()
  {
    string q = checked([|Goo()|]);
  }
}";

            await TestAsync(text, "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(553584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553584")]
        public async Task TestAwaitTaskOfT()
        {
            var text =
@"using System.Threading.Tasks;
class C
{
  void M()
  {
    int x = await [|Goo()|];
  }
}";

            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Int32>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAwaitTaskOfTNullableValue()
        {
            var text =
@"#nullable enable

using System.Threading.Tasks;
class C
{
  void M()
  {
    string? x = await [|Goo()|];
  }
}";

            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.String?>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(553584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553584")]
        public async Task TestAwaitTaskOfTaskOfT()
        {
            var text =
@"using System.Threading.Tasks;
class C
{
  void M()
  {
    Task<int> x = await [|Goo()|];
  }
}";

            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Threading.Tasks.Task<global::System.Int32>>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(553584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553584")]
        public async Task TestAwaitTask()
        {
            var text =
@"using System.Threading.Tasks;
class C
{
  void M()
  {
    await [|Goo()|];
  }
}";

            await TestAsync(text, "global::System.Threading.Tasks.Task");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617622, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617622")]
        public async Task TestLockStatement()
        {
            var text =
@"class C
{
  void M()
  {
    lock([|Goo()|])
    {
    }
  }
}";

            await TestAsync(text, "global::System.Object");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617622, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617622")]
        public async Task TestAwaitExpressionInLockStatement()
        {
            var text =
@"class C
{
  async void M()
  {
    lock(await [|Goo()|])
    {
    }
  }
}";

            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Object>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
        public async Task TestReturnFromAsyncTaskOfT()
        {
            var markup =
@"using System.Threading.Tasks;
class Program
{
    async Task<int> M()
    {
        await Task.Delay(1);
        return [|ab|]
    }
}";
            await TestAsync(markup, "global::System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(853840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853840")]
        public async Task TestAttributeArguments1()
        {
            var markup =
@"[A([|dd|], ee, Y = ff)]
class AAttribute : System.Attribute
{
    public int X;
    public string Y;

    public AAttribute(System.DayOfWeek a, double b)
    {

    }
}";
            await TestAsync(markup, "global::System.DayOfWeek");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(853840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853840")]
        public async Task TestAttributeArguments2()
        {
            var markup =
@"[A(dd, [|ee|], Y = ff)]
class AAttribute : System.Attribute
{
    public int X;
    public string Y;

    public AAttribute(System.DayOfWeek a, double b)
    {

    }
}";
            await TestAsync(markup, "global::System.Double");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(853840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853840")]
        public async Task TestAttributeArguments3()
        {
            var markup =
@"[A(dd, ee, Y = [|ff|])]
class AAttribute : System.Attribute
{
    public int X;
    public string Y;

    public AAttribute(System.DayOfWeek a, double b)
    {

    }
}";
            await TestAsync(markup, "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(757111, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/757111")]
        public async Task TestReturnStatementWithinDelegateWithinAMethodCall()
        {
            var text =
@"using System;

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
}";

            await TestAsync(text, "global::System.String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(994388, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994388")]
        public async Task TestCatchFilterClause()
        {
            var text =
@"
try
{ }
catch (Exception) if ([|M()|])
}";
            await TestInMethodAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(994388, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994388")]
        public async Task TestCatchFilterClause1()
        {
            var text =
@"
try
{ }
catch (Exception) if ([|M|])
}";
            await TestInMethodAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(994388, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994388")]
        public async Task TestCatchFilterClause2()
        {
            var text =
@"
try
{ }
catch (Exception) if ([|M|].N)
}";
            await TestInMethodAsync(text, "global::System.Object", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")]
        public async Task TestAwaitExpressionWithChainingMethod()
        {
            var text =
@"using System;
using System.Threading.Tasks;

class C
{
    static async void T()
    {
        bool x = await [|M()|].ConfigureAwait(false);
    }
}";
            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Boolean>", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")]
        public async Task TestAwaitExpressionWithChainingMethod2()
        {
            var text =
@"using System;
using System.Threading.Tasks;

class C
{
    static async void T()
    {
        bool x = await [|M|].ContinueWith(a => { return true; }).ContinueWith(a => { return false; });
    }
}";
            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Object>", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4233, "https://github.com/dotnet/roslyn/issues/4233")]
        public async Task TestAwaitExpressionWithGenericMethod1()
        {
            var text =
@"using System.Threading.Tasks;

public class C
{
    private async void M()
    {
        bool merged = await X([|Test()|]);
    }

    private async Task<T> X<T>(T t) { return t; }
}";
            await TestAsync(text, "global::System.Boolean", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4233, "https://github.com/dotnet/roslyn/issues/4233")]
        public async Task TestAwaitExpressionWithGenericMethod2()
        {
            var text =
@"using System.Threading.Tasks;

public class C
{
    private async void M()
    {
        bool merged = await Task.Run(() => [|Test()|]);;
    }

    private async Task<T> X<T>(T t) { return t; }
}";
            await TestAsync(text, "global::System.Boolean");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4483, "https://github.com/dotnet/roslyn/issues/4483")]
        public async Task TestNullCoalescingOperator1()
        {
            var text =
    @"class C
{
    void M()
    {
        object z = [|a|]?? null;
    }
}";
            await TestAsync(text, "global::System.Object");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4483, "https://github.com/dotnet/roslyn/issues/4483")]
        public async Task TestNullCoalescingOperator2()
        {
            var text =
    @"class C
{
    void M()
    {
        object z = [|a|] ?? b ?? c;
    }
}";
            await TestAsync(text, "global::System.Object");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4483, "https://github.com/dotnet/roslyn/issues/4483")]
        public async Task TestNullCoalescingOperator3()
        {
            var text =
    @"class C
{
    void M()
    {
        object z = a ?? [|b|] ?? c;
    }
}";
            await TestAsync(text, "global::System.Object");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(5126, "https://github.com/dotnet/roslyn/issues/5126")]
        public async Task TestSelectLambda()
        {
            var text =
    @"using System.Collections.Generic;
using System.Linq;

class C
{
    void M(IEnumerable<string> args)
    {
        args = args.Select(a =>[||])
    }
}";
            await TestAsync(text, "global::System.Object", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(5126, "https://github.com/dotnet/roslyn/issues/5126")]
        public async Task TestSelectLambda2()
        {
            var text =
    @"using System.Collections.Generic;
using System.Linq;

class C
{
    void M(IEnumerable<string> args)
    {
        args = args.Select(a =>[|b|])
    }
}";
            await TestAsync(text, "global::System.String", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(1903, "https://github.com/dotnet/roslyn/issues/1903")]
        public async Task TestSelectLambda3()
        {
            var text =
@"using System.Collections.Generic;
using System.Linq;

class A { }
class B { }
class C
{
    IEnumerable<B> GetB(IEnumerable<A> a)
    {
        return a.Select(i => [|Goo(i)|]);
    }
}";
            await TestAsync(text, "global::B");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(6765, "https://github.com/dotnet/roslyn/issues/6765")]
        public async Task TestDefaultStatement1()
        {
            var text =
    @"class C
{
    static void Main(string[] args)
    {
        System.ConsoleModifiers c = default([||])
    }
}";
            await TestAsync(text, "global::System.ConsoleModifiers", testNode: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(6765, "https://github.com/dotnet/roslyn/issues/6765")]
        public async Task TestDefaultStatement2()
        {
            var text =
    @"class C
{
    static void Goo(System.ConsoleModifiers arg)
    {
        Goo(default([||])
    }
}";
            await TestAsync(text, "global::System.ConsoleModifiers", testNode: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestWhereCall()
        {
            var text =
    @"
using System.Collections.Generic;
class C
{
    void Goo()
    {
        [|ints|].Where(i => i > 10);
    }
}";
            await TestAsync(text, "global::System.Collections.Generic.IEnumerable<global::System.Int32>", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestWhereCall2()
        {
            var text =
    @"
using System.Collections.Generic;
class C
{
    void Goo()
    {
        [|ints|].Where(i => null);
    }
}";
            await TestAsync(text, "global::System.Collections.Generic.IEnumerable<global::System.Object>", testPosition: false);
        }

        [WorkItem(12755, "https://github.com/dotnet/roslyn/issues/12755")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestObjectCreationBeforeArrayIndexing()
        {
            var text =
@"using System;
class C
{
  void M()
  {
        int[] array;
        C p = new [||]
        array[4] = 4;
  }
}";

            await TestAsync(text, "global::C", testNode: false);
        }

        [WorkItem(15468, "https://github.com/dotnet/roslyn/issues/15468")]
        [WorkItem(25305, "https://github.com/dotnet/roslyn/issues/25305")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestDeconstruction()
        {
            await TestInMethodAsync(
@"[|(int i, _)|] =", "(global::System.Int32 i, global::System.Object _)", testPosition: false);
        }

        [WorkItem(15468, "https://github.com/dotnet/roslyn/issues/15468")]
        [WorkItem(25305, "https://github.com/dotnet/roslyn/issues/25305")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestDeconstruction2()
        {
            await TestInMethodAsync(
@"(int i, _) =  [||]", "(global::System.Int32 i, global::System.Object _)", testNode: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestDeconstructionWithNullableElement()
        {
            await TestInMethodAsync(
@"[|(string? s, _)|] =", "(global::System.String? s, global::System.Object _)", testPosition: false);
        }

        [WorkItem(13402, "https://github.com/dotnet/roslyn/issues/13402")]
        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestObjectCreationBeforeBlock()
        {
            var text =
@"class Program
{
    static void Main(string[] args)
    {
        Program p = new [||] 
        { }
    }
}";

            await TestAsync(text, "global::Program", testNode: false);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/37310"), Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestInferringThroughGenericFunctionWithNullableReturn()
        {
            var text =
@"#nullable enable

class Program
{
    static void Main(string[] args)
    {
        string? s = Identity([|input|]);
    }

    static T Identity<T>(T value) { return value; }
}";

            await TestAsync(text, "global::System.String?");
        }
    }
}
