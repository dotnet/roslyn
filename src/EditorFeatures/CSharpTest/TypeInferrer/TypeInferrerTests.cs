// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.TypeInferrer;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
            var root = (await document.GetSyntaxTreeAsync()).GetRoot();
            var node = FindExpressionSyntaxFromSpan(root, textSpan);
            var typeInference = document.GetLanguageService<ITypeInferenceService>();

            var inferredType = useNodeStartPosition
                ? typeInference.InferType(await document.GetSemanticModelForSpanAsync(new TextSpan(node?.SpanStart ?? textSpan.Start, 0), CancellationToken.None), node?.SpanStart ?? textSpan.Start, objectAsDefault: true, cancellationToken: CancellationToken.None)
                : typeInference.InferType(await document.GetSemanticModelForSpanAsync(node?.Span ?? textSpan, CancellationToken.None), node, objectAsDefault: true, cancellationToken: CancellationToken.None);
            var typeSyntax = inferredType.GenerateTypeSyntax();
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
                ExpressionSyntax result = currentNode as ExpressionSyntax;
                if (result != null && result.Span == textSpan)
                {
                    return result;
                }

                currentNode = currentNode.Parent;
            }

            return null;
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConditional1()
        {
            // We do not support position inference here as we're before the ? and we only look
            // backwards to infer a type here.
            await TestInMethodAsync("var q = [|Foo()|] ? 1 : 2;", "System.Boolean",
                testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConditional2()
        {
            await TestInMethodAsync("var q = a ? [|Foo()|] : 2;", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConditional3()
        {
            await TestInMethodAsync(@"var q = a ? """" : [|Foo()|];", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestVariableDeclarator1()
        {
            await TestInMethodAsync("int q = [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestVariableDeclarator2()
        {
            await TestInMethodAsync("var q = [|Foo()|];", "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCoalesce1()
        {
            await TestInMethodAsync("var q = [|Foo()|] ?? 1;", "System.Int32?", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCoalesce2()
        {
            await TestInMethodAsync(@"bool? b;
    var q = b ?? [|Foo()|];", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCoalesce3()
        {
            await TestInMethodAsync(@"string s;
    var q = s ?? [|Foo()|];", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCoalesce4()
        {
            await TestInMethodAsync("var q = [|Foo()|] ?? string.Empty;", "System.String", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBinaryExpression1()
        {
            await TestInMethodAsync(@"string s;
    var q = s + [|Foo()|];", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBinaryExpression2()
        {
            await TestInMethodAsync(@"var s;
    var q = s || [|Foo()|];", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBinaryOperator1()
        {
            await TestInMethodAsync(@"var q = x << [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBinaryOperator2()
        {
            await TestInMethodAsync(@"var q = x >> [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAssignmentOperator3()
        {
            await TestInMethodAsync(@"var q <<= [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAssignmentOperator4()
        {
            await TestInMethodAsync(@"var q >>= [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public async Task TestOverloadedConditionalLogicalOperatorsInferBool()
        {
            await TestAsync(@"using System;
class C
{
    public static C operator &(C c, C d) { return null; }
    public static bool operator true(C c) { return true; }
    public static bool operator false(C c) { return false; }

    static void Main(string[] args)
    {
        var c = new C() && [|Foo()|];
    }
}", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public async Task TestLogicalOrOperatorInference4()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = Foo([|a|] | b);
    }
    static object Foo(Program p)
    {
        return p;
    }
}";
            await TestAsync(text, "Program", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public async Task TestLogicalOrOperatorInference5()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = Foo([|a|] | b);
    }
    static object Foo(bool p)
    {
        return p;
    }
}";
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Int32", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public async Task TestLogicalAndOperatorInference4()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = Foo([|a|] & b);
    }
    static object Foo(Program p)
    {
        return p;
    }
}";
            await TestAsync(text, "Program", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public async Task TestLogicalAndOperatorInference5()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = Foo([|a|] & b);
    }
    static object Foo(bool p)
    {
        return p;
    }
}";
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Int32", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public async Task TestLogicalXorOperatorInference4()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = Foo([|a|] ^ b);
    }
    static object Foo(Program p)
    {
        return p;
    }
}";
            await TestAsync(text, "Program", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public async Task TestLogicalXorOperatorInference5()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = Foo([|a|] ^ b);
    }
    static object Foo(bool p)
    {
        return p;
    }
}";
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Int32", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
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
            await TestAsync(text, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturn1()
        {
            await TestInClassAsync(@"int M() { return [|Foo()|]; }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturn2()
        {
            await TestInMethodAsync("return [|Foo()|];", "void");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturn3()
        {
            await TestInClassAsync(@"int Property { get { return [|Foo()|]; } }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(827897)]
        public async Task TestYieldReturn()
        {
            var markup =
@"using System.Collections.Generic;

class Program
{
    IEnumerable<int> M()
    {
        yield return [|abc|]
    }
}";
            await TestAsync(markup, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnInLambda()
        {
            await TestInMethodAsync("System.Func<string,int> f = s => { return [|Foo()|]; };", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestLambda()
        {
            await TestInMethodAsync("System.Func<string, int> f = s => [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestThrow()
        {
            await TestInMethodAsync("throw [|Foo()|];", "global::System.Exception");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCatch()
        {
            await TestInMethodAsync("try { } catch ([|Foo|] ex) { }", "global::System.Exception");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIf()
        {
            await TestInMethodAsync(@"if ([|Foo()|]) { }", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestWhile()
        {
            await TestInMethodAsync(@"while ([|Foo()|]) { }", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestDo()
        {
            await TestInMethodAsync(@"do { } while ([|Foo()|])", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestFor1()
        {
            await TestInMethodAsync(@"for (int i = 0; [|Foo()|]; i++) { }", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestFor2()
        {
            await TestInMethodAsync(@"for (string i = [|Foo()|]; ; ) { }", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestFor3()
        {
            await TestInMethodAsync(@"for (var i = [|Foo()|]; ; ) { }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestUsing1()
        {
            await TestInMethodAsync(@"using ([|Foo()|]) { }", "global::System.IDisposable");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestUsing2()
        {
            await TestInMethodAsync(@"using (int i = [|Foo()|]) { }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestUsing3()
        {
            await TestInMethodAsync(@"using (var v = [|Foo()|]) { }", "global::System.IDisposable");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestForEach()
        {
            await TestInMethodAsync(@"foreach (int v in [|Foo()|]) { }", "global::System.Collections.Generic.IEnumerable<System.Int32>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPrefixExpression1()
        {
            await TestInMethodAsync(@"var q = +[|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPrefixExpression2()
        {
            await TestInMethodAsync(@"var q = -[|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPrefixExpression3()
        {
            await TestInMethodAsync(@"var q = ~[|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPrefixExpression4()
        {
            await TestInMethodAsync(@"var q = ![|Foo()|];", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayRankSpecifier()
        {
            await TestInMethodAsync(@"var q = new string[[|Foo()|]];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestSwitch1()
        {
            await TestInMethodAsync(@"switch ([|Foo()|]) { }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestSwitch2()
        {
            await TestInMethodAsync(@"switch ([|Foo()|]) { default: }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestSwitch3()
        {
            await TestInMethodAsync(@"switch ([|Foo()|]) { case ""a"": }", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCall1()
        {
            await TestInMethodAsync(@"Bar([|Foo()|]);", "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCall2()
        {
            await TestInClassAsync(@"void M() { Bar([|Foo()|]); } void Bar(int i);", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCall3()
        {
            await TestInClassAsync(@"void M() { Bar([|Foo()|]); } void Bar();", "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCall4()
        {
            await TestInClassAsync(@"void M() { Bar([|Foo()|]); } void Bar(int i, string s);", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestMethodCall5()
        {
            await TestInClassAsync(@"void M() { Bar(s: [|Foo()|]); } void Bar(int i, string s);", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCall1()
        {
            await TestInMethodAsync(@"new C([|Foo()|]);", "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCall2()
        {
            await TestInClassAsync(@"void M() { new C([|Foo()|]); } C(int i) { }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCall3()
        {
            await TestInClassAsync(@"void M() { new C([|Foo()|]); } C() { }", "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCall4()
        {
            await TestInClassAsync(@"void M() { new C([|Foo()|]); } C(int i, string s) { }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructorCall5()
        {
            await TestInClassAsync(@"void M() { new C(s: [|Foo()|]); } C(int i, string s) { }", "System.String");
        }

        [WorkItem(858112)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestThisConstructorInitializer1()
        {
            await TestAsync(@"class MyClass { public MyClass(int x) : this([|test|]) { } }", "System.Int32");
        }

        [WorkItem(858112)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestThisConstructorInitializer2()
        {
            await TestAsync(@"class MyClass { public MyClass(int x, string y) : this(5, [|test|]) { } }", "System.String");
        }

        [WorkItem(858112)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestBaseConstructorInitializer()
        {
            await TestAsync(@"class B { public B(int x) { } } class D : B { public D() : base([|test|]) { } }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIndexAccess1()
        {
            await TestInMethodAsync(@"string[] i; i[[|Foo()|]];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIndexerCall1()
        {
            await TestInMethodAsync(@"this[[|Foo()|]];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIndexerCall2()
        {
            // Update this when binding of indexers is working.
            await TestInClassAsync(@"void M() { this[[|Foo()|]]; } int this [int i] { get; }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIndexerCall3()
        {
            // Update this when binding of indexers is working.
            await TestInClassAsync(@"void M() { this[[|Foo()|]]; } int this [int i, string s] { get; }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestIndexerCall5()
        {
            await TestInClassAsync(@"void M() { this[s: [|Foo()|]]; } int this [int i, string s] { get; }", "System.String");
        }

        [WpfFact]
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

            await TestAsync(text, "System.Int32");
        }

        [WpfFact]
        public async Task TestArrayInitializerInImplicitArrayCreation1()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
       var a = new[] { Bar(), [|Foo()|] };
  }

  int Bar() { return 1; }
  int Foo() { return 2; }
}";

            await TestAsync(text, "System.Int32");
        }

        [WpfFact]
        public async Task TestArrayInitializerInImplicitArrayCreation2()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
       var a = new[] { Bar(), [|Foo()|] };
  }

  int Bar() { return 1; }
}";

            await TestAsync(text, "System.Int32");
        }

        [WpfFact]
        public async Task TestArrayInitializerInImplicitArrayCreation3()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
       var a = new[] { Bar(), [|Foo()|] };
  }
}";

            await TestAsync(text, "System.Object");
        }

        [WpfFact]
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

            await TestAsync(text, "System.Int32");
        }

        [WpfFact]
        public async Task TestArrayInitializerInEqualsValueClause()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
       int[] a = { Bar(), [|Foo()|] };
  }

  int Bar() { return 1; }
}";

            await TestAsync(text, "System.Int32");
        }

        [WpfFact]
        [WorkItem(529480)]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCollectionInitializer1()
        {
            var text =
@"using System.Collections.Generic;

class C
{
  void M()
  {
    new List<int>() { [|Foo()|] };
  }
}";

            await TestAsync(text, "System.Int32");
        }

        [WpfFact]
        [WorkItem(529480)]
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
    new Dictionary<int,string>() { { [|Foo()|], """" } };
  }
}";

            await TestAsync(text, "System.Int32");
        }

        [WpfFact]
        [WorkItem(529480)]
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
    new Dictionary<int,string>() { { 0, [|Foo()|] } };
  }
}";

            await TestAsync(text, "System.String");
        }

        [WpfFact]
        [WorkItem(529480)]
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

            await TestAsync(text, "System.Int32", testPosition: false);
        }

        [WpfFact]
        [WorkItem(529480)]
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

            await TestAsync(text, "System.Boolean");
        }

        [WpfFact]
        [WorkItem(529480)]
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

            await TestAsync(text, "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference1()
        {
            var text =
@"
class A
{
    void Foo()
    {
        A[] x = new [|C|][] { };
    }
}";

            await TestAsync(text, "global::A", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference1_Position()
        {
            var text =
@"
class A
{
    void Foo()
    {
        A[] x = new [|C|][] { };
    }
}";

            await TestAsync(text, "global::A[]", testNode: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference2()
        {
            var text =
@"
class A
{
    void Foo()
    {
        A[][] x = new [|C|][][] { };
    }
}";

            await TestAsync(text, "global::A", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference2_Position()
        {
            var text =
@"
class A
{
    void Foo()
    {
        A[][] x = new [|C|][][] { };
    }
}";

            await TestAsync(text, "global::A[][]", testNode: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference3()
        {
            var text =
@"
class A
{
    void Foo()
    {
        A[][] x = new [|C|][] { };
    }
}";

            await TestAsync(text, "global::A[]", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference3_Position()
        {
            var text =
@"
class A
{
    void Foo()
    {
        A[][] x = new [|C|][] { };
    }
}";

            await TestAsync(text, "global::A[][]", testNode: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArrayInference4()
        {
            var text =
@"
using System;
class A
{
    void Foo()
    {
        Func<int, int>[] x = new Func<int, int>[] { [|Bar()|] };
    }
}";

            await TestAsync(text, "global::System.Func<System.Int32,System.Int32>");
        }

        [WorkItem(538993)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
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

            await TestAsync(text, "System.Int32");
        }

        [WorkItem(539813)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestPointer1()
        {
            var text =
@"class C
{
  void M(int* i)
  {
    var q = i[[|Foo()|]];
  }
}";

            await TestAsync(text, "System.Int32");
        }

        [WorkItem(539813)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestDynamic1()
        {
            var text =
@"class C
{
  void M(dynamic i)
  {
    var q = i[[|Foo()|]];
  }
}";

            await TestAsync(text, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestChecked1()
        {
            var text =
@"class C
{
  void M()
  {
    string q = checked([|Foo()|]);
  }
}";

            await TestAsync(text, "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(553584)]
        public async Task TestAwaitTaskOfT()
        {
            var text =
@"using System.Threading.Tasks;
class C
{
  void M()
  {
    int x = await [|Foo()|];
  }
}";

            await TestAsync(text, "global::System.Threading.Tasks.Task<System.Int32>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(553584)]
        public async Task TestAwaitTaskOfTaskOfT()
        {
            var text =
@"using System.Threading.Tasks;
class C
{
  void M()
  {
    Task<int> x = await [|Foo()|];
  }
}";

            await TestAsync(text, "global::System.Threading.Tasks.Task<global::System.Threading.Tasks.Task<System.Int32>>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(553584)]
        public async Task TestAwaitTask()
        {
            var text =
@"using System.Threading.Tasks;
class C
{
  void M()
  {
    await [|Foo()|];
  }
}";

            await TestAsync(text, "global::System.Threading.Tasks.Task");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617622)]
        public async Task TestLockStatement()
        {
            var text =
@"class C
{
  void M()
  {
    lock([|Foo()|])
    {
    }
  }
}";

            await TestAsync(text, "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617622)]
        public async Task TestAwaitExpressionInLockStatement()
        {
            var text =
@"class C
{
  async void M()
  {
    lock(await [|Foo()|])
    {
    }
  }
}";

            await TestAsync(text, "global::System.Threading.Tasks.Task<System.Object>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(827897)]
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
            await TestAsync(markup, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(853840)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(853840)]
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
            await TestAsync(markup, "System.Double");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(853840)]
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
            await TestAsync(markup, "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(757111)]
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

            await TestAsync(text, "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(994388)]
        public async Task TestCatchFilterClause()
        {
            var text =
@"
try
{ }
catch (Exception) if ([|M()|])
}";
            await TestInMethodAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(994388)]
        public async Task TestCatchFilterClause1()
        {
            var text =
@"
try
{ }
catch (Exception) if ([|M|])
}";
            await TestInMethodAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(994388)]
        public async Task TestCatchFilterClause2()
        {
            var text =
@"
try
{ }
catch (Exception) if ([|M|].N)
}";
            await TestInMethodAsync(text, "System.Object", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
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
            await TestAsync(text, "global::System.Threading.Tasks.Task<System.Boolean>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
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
            await TestAsync(text, "global::System.Threading.Tasks.Task<System.Boolean>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
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
            await TestAsync(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
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
            await TestAsync(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
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
            await TestAsync(text, "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
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
            await TestAsync(text, "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
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
            await TestAsync(text, "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
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
            await TestAsync(text, "System.Object", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
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
            await TestAsync(text, "System.Object", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4486, "https://github.com/dotnet/roslyn/issues/4486")]
        public async Task TestReturnInAsyncLambda1()
        {
            var text =
    @"using System;
using System.IO;
using System.Threading.Tasks;

public class C
{
    public async void M()
    {
        Func<Task<int>> t2 = async () => { return [|a|]; };
    }
}";
            await TestAsync(text, "System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4486, "https://github.com/dotnet/roslyn/issues/4486")]
        public async Task TestReturnInAsyncLambda2()
        {
            var text =
    @"using System;
using System.IO;
using System.Threading.Tasks;

public class C
{
    public async void M()
    {
        Func<Task<int>> t2 = async delegate () { return [|a|]; };
    }
}";
            await TestAsync(text, "System.Int32");
        }
    }
}
