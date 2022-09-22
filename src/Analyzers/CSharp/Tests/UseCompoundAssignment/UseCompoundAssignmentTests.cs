// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCompoundAssignment
{
    public class UseCompoundAssignmentTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public UseCompoundAssignmentTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseCompoundAssignmentDiagnosticAnalyzer(), new CSharpUseCompoundAssignmentCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestAddExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a + 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestSubtractExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a - 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a -= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestMultiplyExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a * 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a *= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestDivideExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a / 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a /= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestModuloExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a % 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a %= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestBitwiseAndExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a & 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a &= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestExclusiveOrExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a ^ 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a ^= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestBitwiseOrExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a | 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a |= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestLeftShiftExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a << 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a <<= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestRightShiftExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a >> 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a >>= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestCoalesceExpressionCSharp8OrGreater()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int? a)
    {
        a [||]= a ?? 10;
    }
}",
@"public class C
{
    void M(int? a)
    {
        a ??= 10;
    }
}", new TestParameters(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestCoalesceExpressionCSharp7()
        {
            await TestMissingAsync(
@"public class C
{
    void M(int? a)
    {
        a [||]= a ?? 10;
    }
}",
    new TestParameters(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        [WorkItem(36467, "https://github.com/dotnet/roslyn/issues/36467")]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotSuggestedWhenRightHandIsThrowExpression()
        {
            await TestMissingAsync(
@"using System;
public class C
{
    void M(int? a)
    {
        a [||]= a ?? throw new Exception();
    }
}",
    new TestParameters(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestField()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    int a;

    void M()
    {
        a [||]= a + 10;
    }
}",
@"public class C
{
    int a;

    void M()
    {
        a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestFieldWithThis()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    int a;

    void M()
    {
        this.a [||]= this.a + 10;
    }
}",
@"public class C
{
    int a;

    void M()
    {
        this.a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestTriviaInsensitive()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    int a;

    void M()
    {
        this  .  /*trivia*/ a [||]= this /*comment*/ .a + 10;
    }
}",
@"public class C
{
    int a;

    void M()
    {
        this  .  /*trivia*/ a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestStaticFieldThroughType()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    static int a;

    void M()
    {
        C.a [||]= C.a + 10;
    }
}",
@"public class C
{
    static int a;

    void M()
    {
        C.a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestStaticFieldThroughNamespaceAndType()
        {
            await TestInRegularAndScript1Async(
@"namespace NS
{
    public class C
    {
        static int a;

        void M()
        {
            NS.C.a [||]= NS.C.a + 10;
        }
    }
}",
@"namespace NS
{
    public class C
    {
        static int a;

        void M()
        {
            NS.C.a += 10;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestParenthesized()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    int a;

    void M()
    {
        (a) [||]= (a) + 10;
    }
}",
@"public class C
{
    int a;

    void M()
    {
        (a) += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestThroughBase()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    public int a;
}

public class D : C
{
    void M()
    {
        base.a [||]= base.a + 10;
    }
}",
@"public class C
{
    public int a;
}

public class D : C
{
    void M()
    {
        base.a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestMultiAccess()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    public int a;
}

public class D
{
    C c;

    void M()
    {
        this.c.a [||]= this.c.a + 10;
    }
}",
@"public class C
{
    public int a;
}

public class D
{
    C c;

    void M()
    {
        this.c.a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestOnTopLevelProp1()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    int a { get; set; }

    void M()
    {
        a [||]= a + 10;
    }
}",
@"public class C
{
    int a { get; set; }

    void M()
    {
        a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestOnTopLevelProp2()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    int a { get; set; }

    void M()
    {
        this.a [||]= this.a + 10;
    }
}",
@"public class C
{
    int a { get; set; }

    void M()
    {
        this.a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestOnTopLevelProp3()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    int a { get; set; }

    void M()
    {
        (this.a) [||]= (this.a) + 10;
    }
}",
@"public class C
{
    int a { get; set; }

    void M()
    {
        (this.a) += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotOnTopLevelRefProp()
        {
            await TestMissingAsync(
@"public class C
{
    int x;
    ref int a { get { return ref x; } }

    void M()
    {
        a [||]= a + 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotOnNestedProp1()
        {
            await TestMissingAsync(
@"
public class A
{
    public int x;
}

public class C
{
    A a { get; }

    void M()
    {
        a.x [||]= a.x + 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotOnNestedProp2()
        {
            await TestMissingAsync(
@"
public class A
{
    public int x;
}

public class C
{
    A a { get; }

    void M()
    {
        this.a.x [||]= this.a.x + 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotOnNestedProp3()
        {
            await TestMissingAsync(
@"
public class A
{
    public int x;
}

public class C
{
    A a { get; }

    void M()
    {
        (a.x) [||]= (a.x) + 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotOnUnboundSymbol()
        {
            await TestMissingAsync(
@"public class C
{
    void M()
    {
        a [||]= a + 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotOnUnboundThisAccess()
        {
            await TestMissingAsync(
@"public class C
{
    void M()
    {
        this.a [||]= this.a + 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotWithSideEffects()
        {
            await TestMissingAsync(
@"public class C
{
    int i;

    C Goo() => this;

    void M()
    {
        this.Goo().i [||]= this.Goo().i + 10;
    }
}");
        }

        [WorkItem(35870, "https://github.com/dotnet/roslyn/issues/35870")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestRightExpressionOnNextLine()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a +
            10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a += 10;
    }
}");
        }

        [WorkItem(35870, "https://github.com/dotnet/roslyn/issues/35870")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestRightExpressionSeparatedWithSeveralLines()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a +

            10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestTrivia()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        // before
        a [||]= a + 10; // after
    }
}",
@"public class C
{
    void M(int a)
    {
        // before
        a += 10; // after
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestTrivia2()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a /*mid1*/ [||]= /*mid2*/ a + 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a /*mid1*/ += /*mid2*/ 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestFixAll()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a, int b)
    {
        a {|FixAllInDocument:|}= a + 10;
        b = b - a;
    }
}",
@"public class C
{
    void M(int a, int b)
    {
        a += 10;
        b -= a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNestedAssignment()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a, int b)
    {
        b = (a [||]= a + 10);
    }
}",
@"public class C
{
    void M(int a, int b)
    {
        b = (a += 10);
    }
}");
        }

        [WorkItem(33382, "https://github.com/dotnet/roslyn/issues/33382")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotOnObjectInitializer()
        {
            await TestMissingAsync(
@"
struct InsertionPoint
{
    int level;
    
    InsertionPoint Up()
    {
        return new InsertionPoint
        {
            level [||]= level - 1,
        };
    }
}");
        }

        [WorkItem(49294, "https://github.com/dotnet/roslyn/issues/49294")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotOnImplicitObjectInitializer()
        {
            await TestMissingAsync(
@"
struct InsertionPoint
{
    int level;
    
    InsertionPoint Up()
    {
        return new()
        {
            level [||]= level - 1,
        };
    }
}");
        }

        [WorkItem(49294, "https://github.com/dotnet/roslyn/issues/49294")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNotOnRecord()
        {
            await TestMissingAsync(
@"
record InsertionPoint(int level)
{
    InsertionPoint Up()
    {
        return this with
        {
            level [||]= level - 1,
        };
    }
}");
        }

        [WorkItem(38137, "https://github.com/dotnet/roslyn/issues/38137")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestParenthesizedExpression()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= (a + 10);
    }
}",
@"public class C
{
    void M(int a)
    {
        a += 10;
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestIncrement()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a + 1;
    }
}",
@"public class C
{
    void M(int a)
    {
        a++;
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestDecrement()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a - 1;
    }
}",
@"public class C
{
    void M(int a)
    {
        a--;
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestMinusIncrement()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int a)
    {
        a [||]= a + (-1);
    }
}",
@"public class C
{
    void M(int a)
    {
        a--;
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestIncrementDouble()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(double a)
    {
        a [||]= a + 1.0;
    }
}",
@"public class C
{
    void M(double a)
    {
        a++;
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestIncrementNotOnString()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(string a)
    {
        a [||]= a + ""1"";
    }
}",
@"public class C
{
    void M(string a)
    {
        a += ""1"";
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestIncrementChar()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(char a)
    {
        a [||]= a + 1;
    }
}",
@"public class C
{
    void M(char a)
    {
        a++;
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestIncrementEnum()
        {
            await TestInRegularAndScript1Async(
@"public enum E {}
public class C
{
    void M(E a)
    {
        a [||]= a + 1;
    }
}",
@"public enum E {}
public class C
{
    void M(E a)
    {
        a++;
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestIncrementDecimal()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(decimal a)
    {
        a [||]= a + 1.0m;
    }
}",
@"public class C
{
    void M(decimal a)
    {
        a++;
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        [InlineData("byte")]
        [InlineData("short")]
        [InlineData("long")]
        [InlineData("float")]
        [InlineData("decimal")]
        public async Task TestIncrementLiteralConversion(string typeName)
        {
            await TestInRegularAndScript1Async(
$@"public class C
{{
    void M({typeName} a)
    {{
        a [||]= a + ({typeName})1;
    }}
}}",
$@"public class C
{{
    void M({typeName} a)
    {{
        a++;
    }}
}}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        [InlineData("byte")]
        [InlineData("short")]
        [InlineData("long")]
        [InlineData("float")]
        [InlineData("decimal")]
        public async Task TestIncrementImplicitLiteralConversion(string typeName)
        {
            await TestInRegularAndScript1Async(
$@"public class C
{{
    void M({typeName} a)
    {{
        a [||]= a + 1;
    }}
}}",
$@"public class C
{{
    void M({typeName} a)
    {{
        a++;
    }}
}}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/38054")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestIncrementLoopVariable()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M()
    {
        for (int i = 0; i < 10; i [||]= i + 1)
        {
        }
    }
}",
@"public class C
{
    void M()
    {
        for (int i = 0; i < 10; i++)
        {
        }
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/53969")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestIncrementInExpressionContext()
        {
            await TestInRegularAndScript1Async(
@"public class C
{
    void M(int i)
    {
        M(i [||]= i + 1);
    }
}",
@"public class C
{
    void M(int i)
    {
        M(++i);
    }
}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/53969")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        [InlineData("switch($$) { }")]
        [InlineData("while(($$) > 0) { }")]
        [InlineData("_ = true ? $$ : 0;")]
        [InlineData("_ = ($$);")]
        public async Task TestPrefixIncrement1(string expressionContext)
        {
            var before = expressionContext.Replace("$$", "i [||]= i + 1");
            var after = expressionContext.Replace("$$", "++i");
            await TestInRegularAndScript1Async(
@$"public class C
{{
    void M(int i)
    {{
        {before}
    }}
}}",
@$"public class C
{{
    void M(int i)
    {{
        {after}
    }}
}}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/53969")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        [InlineData("return $$;")]
        [InlineData("return true ? $$ : 0;")]
        [InlineData("return ($$);")]
        public async Task TestPrefixIncrement2(string expressionContext)
        {
            var before = expressionContext.Replace("$$", "i [||]= i + 1");
            var after = expressionContext.Replace("$$", "++i");
            await TestInRegularAndScript1Async(
@$"public class C
{{
    int M(int i)
    {{
        {before}
    }}
}}",
@$"public class C
{{
    int M(int i)
    {{
        {after}
    }}
}}");
        }

        [WorkItem(38054, "https://github.com/dotnet/roslyn/issues/53969")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        [InlineData(
            "/* Before */ i [||]= i + 1; /* After */",
            "/* Before */ i++; /* After */")]
        [InlineData(
            "M( /* Before */ i [||]= i + 1 /* After */ );",
            "M( /* Before */ ++i /* After */ );")]
        [InlineData(
            "M( /* Before */ i [||]= i - 1 /* After */ );",
            "M( /* Before */ --i /* After */ );")]
        public async Task TestTriviaPreserved(string before, string after)
        {
            await TestInRegularAndScript1Async(
@$"public class C
{{
    int M(int i)
    {{
        {before}
    }}
}}",
@$"public class C
{{
    int M(int i)
    {{
        {after}
    }}
}}");
        }
    }
}
