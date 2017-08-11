// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching
{
    public partial class CSharpAsAndNullCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpAsAndNullCheckDiagnosticAnalyzer(), new CSharpAsAndNullCheckCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheck1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (x != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (o is string x)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckInvertedCheck1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (null != x)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (o is string x)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingInCSharp6()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (x != null)
        {
        }
    }
}", new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingInWrongName()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] y = o as string;
        if (x != null)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnNonDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|y|] = o as string;
        if (x != null)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnIsExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o is string;
        if (x != null)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckComplexExpression1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = (o ? z : w) as string;
        if (x != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if ((o ? z : w) is string x)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnNullEquality()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o is string;
        if (x == null)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestInlineTypeCheckWithElse()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (null != x)
        {
        }
        else
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (o is string x)
        {
        }
        else
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestComments1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        // prefix comment
        [|var|] x = o as string;
        if (x != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        // prefix comment
        if (o is string x)
        {
        }
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestComments2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string; // suffix comment
        if (x != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        // suffix comment
        if (o is string x)
        {
        }
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestComments3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        // prefix comment
        [|var|] x = o as string; // suffix comment
        if (x != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        // prefix comment
        // suffix comment
        if (o is string x)
        {
        }
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckComplexCondition1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (x != null ? 0 : 1)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (o is string x ? 0 : 1)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckComplexCondition2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if ((x != null))
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if ((o is string x))
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckComplexCondition3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (x != null && x.Length > 0)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (o is string x && x.Length > 0)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestDefiniteAssignment1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (x != null && x.Length > 0)
        {
        }
        else if (x != null)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestDefiniteAssignment2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (x != null && x.Length > 0)
        {
        }

        Console.WriteLine(x);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestDefiniteAssignment3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|var|] x = o as string;
        if (x != null && x.Length > 0)
        {
        }

        x = null;
        Console.WriteLine(x);
    }
}",

@"class C
{
    void M()
    {
        if (o is string x && x.Length > 0)
        {
        }

        x = null;
        Console.WriteLine(x);
    }
}");
        }

        [WorkItem(15957, "https://github.com/dotnet/roslyn/issues/15957")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object y)
    {
        if (y != null)
        {
        }

        [|var|] x = o as string;
        if (x != null)
        {
        }
    }
}",
@"class C
{
    void M(object y)
    {
        if (y != null)
        {
        }

        if (o is string x)
        {
        }
    }
}", ignoreTrivia: false);
        }

        [WorkItem(17129, "https://github.com/dotnet/roslyn/issues/17129")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestTrivia2()
        {
            await TestInRegularAndScriptAsync(
@"using System;
namespace N
{
    class Program
    {
        public static void Main()
        {
            object o = null;
            int i = 0;
            [|var|] s = o as string;
            if (s != null && i == 0 && i == 1 &&
                i == 2 && i == 3 &&
                i == 4 && i == 5)
            {
                Console.WriteLine();
            }
        }
    }
}",
@"using System;
namespace N
{
    class Program
    {
        public static void Main()
        {
            object o = null;
            int i = 0;
            if (o is string s && i == 0 && i == 1 &&
                i == 2 && i == 3 &&
                i == 4 && i == 5)
            {
                Console.WriteLine();
            }
        }
    }
}", ignoreTrivia: false);
        }

        [WorkItem(17122, "https://github.com/dotnet/roslyn/issues/17122")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnNullableType()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
namespace N
{
    class Program
    {
        public static void Main()
        {
            object o = null;
            [|var|] i = o as int?;
            if (i != null)
                Console.WriteLine(i);
        }
    }
}");
        }

        [WorkItem(18053, "https://github.com/dotnet/roslyn/issues/18053")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingWhenTypesDoNotMatch()
        {
            await TestMissingInRegularAndScriptAsync(
@"class SyntaxNode
{
    public SyntaxNode Parent;
}

class BaseParameterListSyntax : SyntaxNode
{
}

class ParameterSyntax : SyntaxNode
{

}

public static class C
{
    static void M(ParameterSyntax parameter)
    {
        [|SyntaxNode|] parent = parameter.Parent as BaseParameterListSyntax;

        if (parent != null)
        {
            parent = parent.Parent;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestInlineNullCheck1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|string|] x;
        if ((x = o as string) != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (o is string x)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestInlineNullCheck2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|string|] x;
        if (null != (x = o as string))
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (o is string x)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestInlineNullCheck3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|string|] x;
        while ((x = o as string) != null)
        {
        }
    }
}",
@"class C
{
    void M()
    {
        while (o is string x)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestInlineNullCheck4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|string|] x;
        while (null != (x = o as string))
        {
        }
    }
}",
@"class C
{
    void M()
    {
        while (o is string x)
        {
        }
    }
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestWhileDefiniteAssignment1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|string|] x;
        while ((x = o as string) != null)
        {
        }

        var readAfterWhile = x;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestWhileDefiniteAssignment2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|string|] x;
        while ((x = o as string) != null)
        {
        }

        x = ""writeAfterWhile"";
    }
}");
        }
        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestWhileDefiniteAssignment3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|string|] x;
        x = ""writeBeforeWhile"";
        while ((x = o as string) != null)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestWhileDefiniteAssignment4()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|string|] x = null;
        var readBeforeWhile = x;
        while ((x = o as string) != null)
        {
        }
    }
}");
        }
    }
}
