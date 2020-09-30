// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching
{
    public partial class CSharpIsAndCastCheckDiagnosticAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public CSharpIsAndCastCheckDiagnosticAnalyzerTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpIsAndCastCheckDiagnosticAnalyzer(), new CSharpIsAndCastCheckCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheck1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x;
        }
    }
}",
@"class C
{
    void M()
    {
        if (x is string v)
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
        if (x is string)
        {
            [|var|] v = (string)x;
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
        if (x is string)
        {
            [|var|] v = (string)y;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingInWrongType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (bool)x;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnMultiVar()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            var [|v|] = (string)x, v1 = "";
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
        if (x is string)
        {
            [|v|] = (string)x;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingOnAsExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (x as string)
        {
            [|var|] v = (string)x;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckComplexExpression1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if ((x ? y : z) is string)
        {
            [|var|] v = (string)(x ? y : z);
        }
    }
}",
@"class C
{
    void M()
    {
        if ((x ? y : z) is string v)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestInlineTypeCheckWithElse()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x;
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
        if (x is string v)
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
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (x is string)
        {
            // prefix comment
            [|var|] v = (string)x;
        } 
    }
}",
@"class C
{
    void M()
    {
        // prefix comment
        if (x is string v)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestComments2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x; // suffix comment
        } 
    }
}",
@"class C
{
    void M()
    {
        // suffix comment
        if (x is string v)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestComments3()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (x is string)
        {
            // prefix comment
            [|var|] v = (string)x; // suffix comment
        } 
    }
}",
@"class C
{
    void M()
    {
        // prefix comment
        // suffix comment
        if (x is string v)
        {
        }
    }
}");
        }

        [WorkItem(17126, "https://github.com/dotnet/roslyn/issues/17126")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestComments4()
        {
            await TestInRegularAndScript1Async(
@"using System;
namespace N {
    class Program {
        public static void Main()
        {
            object o = null;
            if (o is int)
                Console.WriteLine();
            else if (o is string)
            {
                // some comment
                [|var|] s = (string)o;
                Console.WriteLine(s);
            }
        }
    }
}",
@"using System;
namespace N {
    class Program {
        public static void Main()
        {
            object o = null;
            if (o is int)
                Console.WriteLine();
            else if (o is string s) // some comment
            {
                Console.WriteLine(s);
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckParenthesized1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if ((x) is string)
        {
            [|var|] v = (string)x;
        }
    }
}",
@"class C
{
    void M()
    {
        if ((x) is string v)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckParenthesized2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)(x);
        }
    }
}",
@"class C
{
    void M()
    {
        if (x is string v)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckParenthesized3()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = ((string)x);
        }
    }
}",
@"class C
{
    void M()
    {
        if (x is string v)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckScopeConflict1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x;
        }
        else
        {
            var v = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckScopeConflict2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x;
        }

        if (true)
        {
            var v = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckScopeConflict3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (x is string)
        {
            var v = (string)x;
        }

        if (x is bool)
        {
            [|var|] v = (bool)x;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task InlineTypeCheckScopeNonConflict1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        {
            if (x is string)
            {
                [|var|] v = ((string)x);
            }
        }

        {
            var v = 1;
        }
    }
}",
@"class C
{
    void M()
    {
        {
            if (x is string v)
            {
            }
        }

        {
            var v = 1;
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
    static void N(ParameterSyntax parameter)
    {
        if (parameter.Parent is BaseParameterListSyntax)
        {
            [|SyntaxNode|] parent = (BaseParameterListSyntax)parameter.Parent;
            parent = parent.Parent;
        }
    }
}");
        }

        [WorkItem(429612, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/429612")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingWithNullableType()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    public object Convert(object value)
    {
        if (value is bool?)
        {
            [|bool?|] tmp = (bool?)value;
        }

        return null;
    }
}");
        }

        [WorkItem(21172, "https://github.com/dotnet/roslyn/issues/21172")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestMissingWithDynamic()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    public object Convert(object value)
    {
        if (value is dynamic)
        {
            [|dynamic|] tmp = (dynamic)value;
        }

        return null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task TestSeverity()
        {
            var source =

@"class C
{
    void M()
    {
        if (x is string)
        {
            [|var|] v = (string)x;
        } 
    }
}";
            var warningOption = new CodeStyleOption2<bool>(true, NotificationOption2.Warning);
            var options = Option(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, warningOption);
            var testParameters = new TestParameters(options: options, parseOptions: TestOptions.Regular8);

            using var workspace = CreateWorkspaceFromOptions(source, testParameters);
            var diag = (await GetDiagnosticsAsync(workspace, testParameters)).Single();
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal(IDEDiagnosticIds.InlineIsTypeCheckId, diag.Id);

        }
    }
}
