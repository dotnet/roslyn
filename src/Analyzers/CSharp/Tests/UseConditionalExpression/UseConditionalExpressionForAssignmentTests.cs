// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseConditionalExpression
{
    public partial class UseConditionalExpressionForAssignmentTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer(),
                new CSharpUseConditionalExpressionForAssignmentCodeFixProvider());

        private static OptionsCollection PreferImplicitTypeAlways => new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOptions2.TrueWithSilentEnforcement },
            { CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions2.TrueWithSilentEnforcement },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOptions2.TrueWithSilentEnforcement },
        };

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleAssignment()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        i = true ? 0 : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleAssignment_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        i = true ? throw new System.Exception() : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleAssignment_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        i = true ? 0 : throw new System.Exception();
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotWithTwoThrows()
        {
            await TestMissingAsync(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            throw new System.Exception();
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotOnSimpleAssignment_Throw1_CSharp6()
        {
            await TestMissingAsync(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            i = 1;
        }
    }
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestWithSimpleThrow()
        {
            await TestMissingAsync(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            throw;
        }
        else
        {
            i = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleAssignmentNoBlocks()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
            i = 0;
        else
            i = 1;
    }
}",
@"
class C
{
    void M(int i)
    {
        i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleAssignmentNoBlocks_NotInBlock()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        if (true)
            [||]if (true)
                i = 0;
            else
                i = 1;
    }
}",
@"
class C
{
    void M(int i)
    {
        if (true)
            i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotOnSimpleAssignmentToDifferentTargets()
        {
            await TestMissingAsync(
@"
class C
{
    void M(int i, int j)
    {
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            j = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToUndefinedField()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            this.i = 0;
        }
        else
        {
            this.i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        this.i = true ? 0 : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToUndefinedField_Throw()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            this.i = 0;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    void M()
    {
        this.i = true ? 0 : throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnNonUniformTargetSyntax()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            this.i = 0;
        }
        else
        {
            this . i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        this.i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToDefinedField()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int i;

    void M()
    {
        [||]if (true)
        {
            this.i = 0;
        }
        else
        {
            this.i = 1;
        }
    }
}",
@"
class C
{
    int i;

    void M()
    {
        this.i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalNoInitializer()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i;
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = true ? 0 : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalNoInitializer_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i;
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = true ? 0 : throw new System.Exception();
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalNoInitializer_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i;
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = true ? throw new System.Exception() : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalLiteralInitializer()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i = 0;
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalDefaultLiteralInitializer()
        {
            await TestAsync(
@"
class C
{
    void M()
    {
        int i = default;
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = true ? 0 : 1;
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalDefaultExpressionInitializer()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i = default(int);
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestDoNotMergeAssignmentToAboveLocalWithComplexInitializer()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i = Foo();
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = Foo();
        i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestDoNotMergeAssignmentToAboveLocalIfIntermediaryStatement()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i = 0;
        Console.WriteLine();
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = 0;
        Console.WriteLine();
        i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestDoNotMergeAssignmentToAboveIfLocalUsedInIfCondition()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i = 0;
        [||]if (Bar(i))
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = 0;
        i = Bar(i) ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestDoNotMergeAssignmentToAboveIfMultiDecl()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i = 0, j = 0;
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = 0, j = 0;
        i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestUseImplicitTypeForIntrinsicTypes()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i = 0;
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        var i = true ? 0 : 1;
    }
}", new TestParameters(options: Option(CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOptions2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestUseImplicitTypeWhereApparent()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i = 0;
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = true ? 0 : 1;
    }
}", new TestParameters(options: Option(CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOptions2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestUseImplicitTypeWherePossible()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        int i = 0;
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        int i = true ? 0 : 1;
    }
}", new TestParameters(options: Option(CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingWithoutElse()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            i = 0;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingWithoutElseWithStatementAfterwards()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            i = 0;
        }

        i = 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingWithoutElseWithThrowStatementAfterwards()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            i = 0;
        }

        throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        // cast will be necessary, otherwise 'var' would get the type 'string'.
        object o;
        [||]if (true)
        {
            o = ""a"";
        }
        else
        {
            o = ""b"";
        }
    }
}",
@"
class C
{
    void M()
    {
        // cast will be necessary, otherwise 'var' would get the type 'string'.
        var o = true ? ""a"" : (object)""b"";
    }
}", new TestParameters(options: PreferImplicitTypeAlways));
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44036"), Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        object o;
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            o = ""b"";
        }
    }
}",
@"
class C
{
    void M()
    {
        object o = true ? throw new System.Exception() : ""b"";
    }
}", new TestParameters(options: PreferImplicitTypeAlways));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44036"), Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        object o;
        [||]if (true)
        {
            o = ""a"";
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    void M()
    {
        object o = true ? ""a"" : throw new System.Exception();
    }
}", new TestParameters(options: PreferImplicitTypeAlways));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversionWithUseVarForAll_CanUseVarBecauseConditionalTypeMatches()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        string s;
        [||]if (true)
        {
            s = ""a"";
        }
        else
        {
            s = null;
        }
    }
}",
@"
class C
{
    void M()
    {
        var s = true ? ""a"" : null;
    }
}", new TestParameters(options: PreferImplicitTypeAlways));
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversionWithUseVarForAll_CanUseVarBecauseConditionalTypeMatches_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        string s;
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            s = null;
        }
    }
}",
@"
class C
{
    void M()
    {
        string s = true ? throw new System.Exception() : (string)null;
    }
}", new TestParameters(options: PreferImplicitTypeAlways));
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversionWithUseVarForAll_CanUseVarBecauseConditionalTypeMatches_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        string s;
        [||]if (true)
        {
            s = ""a"";
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    void M()
    {
        string s = true ? ""a"" : throw new System.Exception();
    }
}", new TestParameters(options: PreferImplicitTypeAlways));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversionWithUseVarForAll_CanUseVarButRequiresCastOfConditionalBranch()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        string s;
        [||]if (true)
        {
            s = null;
        }
        else
        {
            s = null;
        }
    }
}",
@"
class C
{
    void M()
    {
        var s = true ? null : (string)null;
    }
}", new TestParameters(options: PreferImplicitTypeAlways));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestKeepTriviaAroundIf()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        // leading
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        } // trailing
    }
}",
@"
class C
{
    void M(int i)
    {
        // leading
        i = true ? 0 : 1; // trailing
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        {|FixAllInDocument:if|} (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }

        string s;
        if (true)
        {
            s = ""a"";
        }
        else
        {
            s = ""b"";
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        i = true ? 0 : 1;

        string s = true ? ""a"" : ""b"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMultiLine1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            i = Foo(
                1, 2, 3);
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        i = true
            ? Foo(
                1, 2, 3)
            : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMultiLine2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = Foo(
                1, 2, 3);
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        i = true
            ? 0
            : Foo(
                1, 2, 3);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMultiLine3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            i = Foo(
                1, 2, 3);
        }
        else
        {
            i = Foo(
                4, 5, 6);
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        i = true
            ? Foo(
                1, 2, 3)
            : Foo(
                4, 5, 6);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestElseIfWithBlock()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        if (true)
        {
        }
        else [||]if (false)
        {
            i = 1;
        }
        else
        {
            i = 0;
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        if (true)
        {
        }
        else
        {
            i = false ? 1 : 0;
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestElseIfWithBlock_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        if (true)
        {
        }
        else [||]if (false)
        {
            throw new System.Exception();
        }
        else
        {
            i = 0;
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        if (true)
        {
        }
        else
        {
            i = false ? throw new System.Exception() : 0;
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestElseIfWithBlock_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        if (true)
        {
        }
        else [||]if (false)
        {
            i = 1;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        if (true)
        {
        }
        else
        {
            i = false ? 1 : throw new System.Exception();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestElseIfWithoutBlock()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        if (true) i = 2;
        else [||]if (false) i = 1;
        else i = 0;
    }
}",
@"
class C
{
    void M(int i)
    {
        if (true) i = 2;
        else i = false ? 1 : 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestRefAssignment1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(ref int i, ref int j)
    {
        ref int x = ref i;
        [||]if (true)
        {
            x = ref i;
        }
        else
        {
            x = ref j
        }
    }
}",
@"
class C
{
    void M(ref int i, ref int j)
    {
        ref int x = ref i;
        x = ref true ? ref i : ref j;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestRefAssignment1_Throw1()
        {
            await TestMissingAsync(
@"
class C
{
    void M(ref int i, ref int j)
    {
        ref int x = ref i;
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            x = ref j;
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestRefAssignment1_Throw2()
        {
            await TestMissingAsync(
@"
class C
{
    void M(ref int i, ref int j)
    {
        ref int x = ref i;
        [||]if (true)
        {
            x = ref i;
        }
        else
        {
            throw new System.Exception();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestTrueFalse1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(bool i, int j)
    {
        [||]if (j == 0)
        {
            i = true;
        }
        else
        {
            i = false;
        }
    }
}",
@"
class C
{
    void M(bool i, int j)
    {
        i = j == 0;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestTrueFalse_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(bool i, int j)
    {
        [||]if (j == 0)
        {
            i = true;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    void M(bool i, int j)
    {
        i = j == 0 ? true : throw new System.Exception();
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestTrueFalse_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(bool i, int j)
    {
        [||]if (j == 0)
        {
            throw new System.Exception();
        }
        else
        {
            i = false;
        }
    }
}",
@"
class C
{
    void M(bool i, int j)
    {
        i = j == 0 ? throw new System.Exception() : false;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestTrueFalse2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(bool i, int j)
    {
        [||]if (j == 0)
        {
            i = false;
        }
        else
        {
            i = true;
        }
    }
}",
@"
class C
{
    void M(bool i, int j)
    {
        i = j != 0;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestFalseTrue_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(bool i, int j)
    {
        [||]if (j == 0)
        {
            throw new System.Exception();
        }
        else
        {
            i = true;
        }
    }
}",
@"
class C
{
    void M(bool i, int j)
    {
        i = j == 0 ? throw new System.Exception() : true;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestFalseTrue_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(bool i, int j)
    {
        [||]if (j == 0)
        {
            i = false;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    void M(bool i, int j)
    {
        i = j == 0 ? false : throw new System.Exception();
    }
}");
        }
    }
}
