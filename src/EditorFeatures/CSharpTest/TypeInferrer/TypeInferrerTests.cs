// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
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

        protected override void TestWorker(Document document, TextSpan textSpan, string expectedType, bool useNodeStartPosition)
        {
            var root = document.GetSyntaxTreeAsync().Result.GetRoot();
            var node = FindExpressionSyntaxFromSpan(root, textSpan);
            var typeInference = document.GetLanguageService<ITypeInferenceService>();

            var inferredType = useNodeStartPosition
                ? typeInference.InferType(document.GetSemanticModelForSpanAsync(new TextSpan(node?.SpanStart ?? textSpan.Start, 0), CancellationToken.None).Result, node?.SpanStart ?? textSpan.Start, objectAsDefault: true, cancellationToken: CancellationToken.None)
                : typeInference.InferType(document.GetSemanticModelForSpanAsync(node?.Span ?? textSpan, CancellationToken.None).Result, node, objectAsDefault: true, cancellationToken: CancellationToken.None);
            var typeSyntax = inferredType.GenerateTypeSyntax();
            Assert.Equal(expectedType, typeSyntax.ToString());
        }

        private void TestInClass(string text, string expectedType)
        {
            text = @"class C
{
    $
}".Replace("$", text);
            Test(text, expectedType);
        }

        private void TestInMethod(string text, string expectedType, bool testNode = true, bool testPosition = true)
        {
            text = @"class C
{
    void M()
    {
        $
    }
}".Replace("$", text);
            Test(text, expectedType, testNode: testNode, testPosition: testPosition);
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
        public void TestConditional1()
        {
            // We do not support position inference here as we're before the ? and we only look
            // backwards to infer a type here.
            TestInMethod("var q = [|Foo()|] ? 1 : 2;", "System.Boolean",
                testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestConditional2()
        {
            TestInMethod("var q = a ? [|Foo()|] : 2;", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestConditional3()
        {
            TestInMethod(@"var q = a ? """" : [|Foo()|];", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestVariableDeclarator1()
        {
            TestInMethod("int q = [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestVariableDeclarator2()
        {
            TestInMethod("var q = [|Foo()|];", "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCoalesce1()
        {
            TestInMethod("var q = [|Foo()|] ?? 1;", "System.Int32?", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCoalesce2()
        {
            TestInMethod(@"bool? b;
    var q = b ?? [|Foo()|];", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCoalesce3()
        {
            TestInMethod(@"string s;
    var q = s ?? [|Foo()|];", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCoalesce4()
        {
            TestInMethod("var q = [|Foo()|] ?? string.Empty;", "System.String", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestBinaryExpression1()
        {
            TestInMethod(@"string s;
    var q = s + [|Foo()|];", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestBinaryExpression2()
        {
            TestInMethod(@"var s;
    var q = s || [|Foo()|];", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestBinaryOperator1()
        {
            TestInMethod(@"var q = x << [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestBinaryOperator2()
        {
            TestInMethod(@"var q = x >> [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestAssignmentOperator3()
        {
            TestInMethod(@"var q <<= [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestAssignmentOperator4()
        {
            TestInMethod(@"var q >>= [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestOverloadedConditionalLogicalOperatorsInferBool()
        {
            Test(@"using System;
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
        public void TestConditionalLogicalOrOperatorAlwaysInfersBool()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = a || [|7|];
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestConditionalLogicalAndOperatorAlwaysInfersBool()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = a && [|7|];
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalOrOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] | true;
    }
}";
            Test(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalOrOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] | b | c || d;
    }
}";
            Test(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalOrOperatorInference3()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = a | b | [|c|] || d;
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalOrOperatorInference4()
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
            Test(text, "Program", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalOrOperatorInference5()
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
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalOrOperatorInference6()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if (([|x|] | y) != 0) {}
    }
}";
            Test(text, "System.Int32", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalOrOperatorInference7()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] | y) {}
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalAndOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] & true;
    }
}";
            Test(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalAndOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] & b & c && d;
    }
}";
            Test(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalAndOperatorInference3()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = a & b & [|c|] && d;
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalAndOperatorInference4()
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
            Test(text, "Program", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalAndOperatorInference5()
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
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalAndOperatorInference6()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if (([|x|] & y) != 0) {}
    }
}";
            Test(text, "System.Int32", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalAndOperatorInference7()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] & y) {}
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalXorOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] ^ true;
    }
}";
            Test(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalXorOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = [|a|] ^ b ^ c && d;
    }
}";
            Test(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalXorOperatorInference3()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        var x = a ^ b ^ [|c|] && d;
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalXorOperatorInference4()
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
            Test(text, "Program", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalXorOperatorInference5()
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
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalXorOperatorInference6()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if (([|x|] ^ y) != 0) {}
    }
}";
            Test(text, "System.Int32", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalXorOperatorInference7()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] ^ y) {}
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalOrEqualsOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] |= y) {}
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalOrEqualsOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        int z = [|x|] |= y;
    }
}";
            Test(text, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalAndEqualsOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] &= y) {}
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalAndEqualsOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        int z = [|x|] &= y;
    }
}";
            Test(text, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalXorEqualsOperatorInference1()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        if ([|x|] ^= y) {}
    }
}";
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617633)]
        public void TestLogicalXorEqualsOperatorInference2()
        {
            var text = @"using System;
class C
{
    static void Main(string[] args)
    {
        int z = [|x|] ^= y;
    }
}";
            Test(text, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestReturn1()
        {
            TestInClass(@"int M() { return [|Foo()|]; }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestReturn2()
        {
            TestInMethod("return [|Foo()|];", "void");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestReturn3()
        {
            TestInClass(@"int Property { get { return [|Foo()|]; } }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(827897)]
        public void TestYieldReturn()
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
            Test(markup, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestReturnInLambda()
        {
            TestInMethod("System.Func<string,int> f = s => { return [|Foo()|]; };", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestLambda()
        {
            TestInMethod("System.Func<string, int> f = s => [|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestThrow()
        {
            TestInMethod("throw [|Foo()|];", "global::System.Exception");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCatch()
        {
            TestInMethod("try { } catch ([|Foo|] ex) { }", "global::System.Exception");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestIf()
        {
            TestInMethod(@"if ([|Foo()|]) { }", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestWhile()
        {
            TestInMethod(@"while ([|Foo()|]) { }", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestDo()
        {
            TestInMethod(@"do { } while ([|Foo()|])", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestFor1()
        {
            TestInMethod(@"for (int i = 0; [|Foo()|]; i++) { }", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestFor2()
        {
            TestInMethod(@"for (string i = [|Foo()|]; ; ) { }", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestFor3()
        {
            TestInMethod(@"for (var i = [|Foo()|]; ; ) { }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestUsing1()
        {
            TestInMethod(@"using ([|Foo()|]) { }", "global::System.IDisposable");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestUsing2()
        {
            TestInMethod(@"using (int i = [|Foo()|]) { }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestUsing3()
        {
            TestInMethod(@"using (var v = [|Foo()|]) { }", "global::System.IDisposable");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestForEach()
        {
            TestInMethod(@"foreach (int v in [|Foo()|]) { }", "global::System.Collections.Generic.IEnumerable<System.Int32>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestPrefixExpression1()
        {
            TestInMethod(@"var q = +[|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestPrefixExpression2()
        {
            TestInMethod(@"var q = -[|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestPrefixExpression3()
        {
            TestInMethod(@"var q = ~[|Foo()|];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestPrefixExpression4()
        {
            TestInMethod(@"var q = ![|Foo()|];", "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestArrayRankSpecifier()
        {
            TestInMethod(@"var q = new string[[|Foo()|]];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestSwitch1()
        {
            TestInMethod(@"switch ([|Foo()|]) { }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestSwitch2()
        {
            TestInMethod(@"switch ([|Foo()|]) { default: }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestSwitch3()
        {
            TestInMethod(@"switch ([|Foo()|]) { case ""a"": }", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestMethodCall1()
        {
            TestInMethod(@"Bar([|Foo()|]);", "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestMethodCall2()
        {
            TestInClass(@"void M() { Bar([|Foo()|]); } void Bar(int i);", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestMethodCall3()
        {
            TestInClass(@"void M() { Bar([|Foo()|]); } void Bar();", "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestMethodCall4()
        {
            TestInClass(@"void M() { Bar([|Foo()|]); } void Bar(int i, string s);", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestMethodCall5()
        {
            TestInClass(@"void M() { Bar(s: [|Foo()|]); } void Bar(int i, string s);", "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestConstructorCall1()
        {
            TestInMethod(@"new C([|Foo()|]);", "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestConstructorCall2()
        {
            TestInClass(@"void M() { new C([|Foo()|]); } C(int i) { }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestConstructorCall3()
        {
            TestInClass(@"void M() { new C([|Foo()|]); } C() { }", "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestConstructorCall4()
        {
            TestInClass(@"void M() { new C([|Foo()|]); } C(int i, string s) { }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestConstructorCall5()
        {
            TestInClass(@"void M() { new C(s: [|Foo()|]); } C(int i, string s) { }", "System.String");
        }

        [WorkItem(858112)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestThisConstructorInitializer1()
        {
            Test(@"class MyClass { public MyClass(int x) : this([|test|]) { } }", "System.Int32");
        }

        [WorkItem(858112)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestThisConstructorInitializer2()
        {
            Test(@"class MyClass { public MyClass(int x, string y) : this(5, [|test|]) { } }", "System.String");
        }

        [WorkItem(858112)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestBaseConstructorInitializer()
        {
            Test(@"class B { public B(int x) { } } class D : B { public D() : base([|test|]) { } }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestIndexAccess1()
        {
            TestInMethod(@"string[] i; i[[|Foo()|]];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestIndexerCall1()
        {
            TestInMethod(@"this[[|Foo()|]];", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestIndexerCall2()
        {
            // Update this when binding of indexers is working.
            TestInClass(@"void M() { this[[|Foo()|]]; } int this [int i] { get; }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestIndexerCall3()
        {
            // Update this when binding of indexers is working.
            TestInClass(@"void M() { this[[|Foo()|]]; } int this [int i, string s] { get; }", "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestIndexerCall5()
        {
            TestInClass(@"void M() { this[s: [|Foo()|]]; } int this [int i, string s] { get; }", "System.String");
        }

        [WpfFact]
        public void TestArrayInitializerInImplicitArrayCreationSimple()
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

            Test(text, "System.Int32");
        }

        [WpfFact]
        public void TestArrayInitializerInImplicitArrayCreation1()
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

            Test(text, "System.Int32");
        }

        [WpfFact]
        public void TestArrayInitializerInImplicitArrayCreation2()
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

            Test(text, "System.Int32");
        }

        [WpfFact]
        public void TestArrayInitializerInImplicitArrayCreation3()
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

            Test(text, "System.Object");
        }

        [WpfFact]
        public void TestArrayInitializerInEqualsValueClauseSimple()
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

            Test(text, "System.Int32");
        }

        [WpfFact]
        public void TestArrayInitializerInEqualsValueClause()
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

            Test(text, "System.Int32");
        }

        [WpfFact]
        [WorkItem(529480)]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCollectionInitializer1()
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

            Test(text, "System.Int32");
        }

        [WpfFact]
        [WorkItem(529480)]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCollectionInitializer2()
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

            Test(text, "System.Int32");
        }

        [WpfFact]
        [WorkItem(529480)]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCollectionInitializer3()
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

            Test(text, "System.String");
        }

        [WpfFact]
        [WorkItem(529480)]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCustomCollectionInitializerAddMethod1()
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

            Test(text, "System.Int32", testPosition: false);
        }

        [WpfFact]
        [WorkItem(529480)]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCustomCollectionInitializerAddMethod2()
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

            Test(text, "System.Boolean");
        }

        [WpfFact]
        [WorkItem(529480)]
        [Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCustomCollectionInitializerAddMethod3()
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

            Test(text, "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestArrayInference1()
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

            Test(text, "global::A", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestArrayInference1_Position()
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

            Test(text, "global::A[]", testNode: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestArrayInference2()
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

            Test(text, "global::A", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestArrayInference2_Position()
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

            Test(text, "global::A[][]", testNode: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestArrayInference3()
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

            Test(text, "global::A[]", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestArrayInference3_Position()
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

            Test(text, "global::A[][]", testNode: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestArrayInference4()
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

            Test(text, "global::System.Func<System.Int32,System.Int32>");
        }

        [WorkItem(538993)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestInsideLambda2()
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

            Test(text, "System.Int32");
        }

        [WorkItem(539813)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestPointer1()
        {
            var text =
@"class C
{
  void M(int* i)
  {
    var q = i[[|Foo()|]];
  }
}";

            Test(text, "System.Int32");
        }

        [WorkItem(539813)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestDynamic1()
        {
            var text =
@"class C
{
  void M(dynamic i)
  {
    var q = i[[|Foo()|]];
  }
}";

            Test(text, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestChecked1()
        {
            var text =
@"class C
{
  void M()
  {
    string q = checked([|Foo()|]);
  }
}";

            Test(text, "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(553584)]
        public void TestAwaitTaskOfT()
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

            Test(text, "global::System.Threading.Tasks.Task<System.Int32>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(553584)]
        public void TestAwaitTaskOfTaskOfT()
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

            Test(text, "global::System.Threading.Tasks.Task<global::System.Threading.Tasks.Task<System.Int32>>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(553584)]
        public void TestAwaitTask()
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

            Test(text, "global::System.Threading.Tasks.Task");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617622)]
        public void TestLockStatement()
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

            Test(text, "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(617622)]
        public void TestAwaitExpressionInLockStatement()
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

            Test(text, "global::System.Threading.Tasks.Task<System.Object>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(827897)]
        public void TestReturnFromAsyncTaskOfT()
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
            Test(markup, "System.Int32");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(853840)]
        public void TestAttributeArguments1()
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
            Test(markup, "global::System.DayOfWeek");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(853840)]
        public void TestAttributeArguments2()
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
            Test(markup, "System.Double");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(853840)]
        public void TestAttributeArguments3()
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
            Test(markup, "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(757111)]
        public void TestReturnStatementWithinDelegateWithinAMethodCall()
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

            Test(text, "System.String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(994388)]
        public void TestCatchFilterClause()
        {
            var text =
@"
try
{ }
catch (Exception) if ([|M()|])
}";
            TestInMethod(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(994388)]
        public void TestCatchFilterClause1()
        {
            var text =
@"
try
{ }
catch (Exception) if ([|M|])
}";
            TestInMethod(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(994388)]
        public void TestCatchFilterClause2()
        {
            var text =
@"
try
{ }
catch (Exception) if ([|M|].N)
}";
            TestInMethod(text, "System.Object", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")]
        public void TestAwaitExpressionWithChainingMethod()
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
            Test(text, "global::System.Threading.Tasks.Task<System.Boolean>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")]
        public void TestAwaitExpressionWithChainingMethod2()
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
            Test(text, "global::System.Threading.Tasks.Task<System.Boolean>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4233, "https://github.com/dotnet/roslyn/issues/4233")]
        public void TestAwaitExpressionWithGenericMethod1()
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
            Test(text, "System.Boolean", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4233, "https://github.com/dotnet/roslyn/issues/4233")]
        public void TestAwaitExpressionWithGenericMethod2()
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
            Test(text, "System.Boolean");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4483, "https://github.com/dotnet/roslyn/issues/4483")]
        public void TestNullCoalescingOperator1()
        {
            var text =
    @"class C
{
    void M()
    {
        object z = [|a|]?? null;
    }
}";
            Test(text, "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4483, "https://github.com/dotnet/roslyn/issues/4483")]
        public void TestNullCoalescingOperator2()
        {
            var text =
    @"class C
{
    void M()
    {
        object z = [|a|] ?? b ?? c;
    }
}";
            Test(text, "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4483, "https://github.com/dotnet/roslyn/issues/4483")]
        public void TestNullCoalescingOperator3()
        {
            var text =
    @"class C
{
    void M()
    {
        object z = a ?? [|b|] ?? c;
    }
}";
            Test(text, "System.Object");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(5126, "https://github.com/dotnet/roslyn/issues/5126")]
        public void TestSelectLambda()
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
            Test(text, "System.Object", testPosition: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(5126, "https://github.com/dotnet/roslyn/issues/5126")]
        public void TestSelectLambda2()
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
            Test(text, "System.Object", testPosition: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4486, "https://github.com/dotnet/roslyn/issues/4486")]
        public void TestReturnInAsyncLambda1()
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
            Test(text, "System.Int32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        [WorkItem(4486, "https://github.com/dotnet/roslyn/issues/4486")]
        public void TestReturnInAsyncLambda2()
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
            Test(text, "System.Int32");
        }
    }
}
