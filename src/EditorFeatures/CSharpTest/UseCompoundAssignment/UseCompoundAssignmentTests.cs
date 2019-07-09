// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCompoundAssignment
{
    public class UseCompoundAssignmentTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseCompoundAssignmentDiagnosticAnalyzer(), new CSharpUseCompoundAssignmentCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestAddExpression()
        {
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
}", parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScript1Async(
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
            await TestInRegular73AndScript1Async(
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
            await TestInRegular73AndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestTrivia()
        {
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
            await TestInRegular73AndScriptAsync(
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
    }
}
