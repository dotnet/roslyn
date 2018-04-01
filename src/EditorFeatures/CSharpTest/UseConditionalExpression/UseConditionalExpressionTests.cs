// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseConditionalExpression
{
    public partial class UseConditionalExpressionForAssignmentTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer(),
                new CSharpUseConditionalExpressionForAssignmentCodeRefactoringProvider());

        private static readonly Dictionary<OptionKey, object> s_preferImplicitTypeAlways = new Dictionary<OptionKey, object>
        {
            { CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, CodeStyleOptions.TrueWithNoneEnforcement },
            { CSharpCodeStyleOptions.UseImplicitTypeWherePossible, CodeStyleOptions.TrueWithNoneEnforcement },
            { CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, CodeStyleOptions.TrueWithNoneEnforcement },
        };

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleAssignment()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleAssignmentNoBlocks()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnNonUniformTargetSyntax()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalLiteralInitializer()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
}", options: new Dictionary<OptionKey, object> {
    {  CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, CodeStyleOptions.TrueWithNoneEnforcement }
});
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestUseImplicitTypeWhereApparent()
        {
            await TestInRegularAndScriptAsync(
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
}", options: new Dictionary<OptionKey, object> {
    {  CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, CodeStyleOptions.TrueWithNoneEnforcement }
});
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestUseImplicitTypeWherePossible()
        {
            await TestInRegularAndScriptAsync(
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
}", options: new Dictionary<OptionKey, object> {
    {  CSharpCodeStyleOptions.UseImplicitTypeWherePossible, CodeStyleOptions.TrueWithNoneEnforcement }
});
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversionWithUseVarForAll_CannotUseVarBecauseTypeWouldChange()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        // keep object even though both values are strings
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
        // keep object even though both values are strings
        object o = true ? ""a"" : ""b"";
    }
}", options: s_preferImplicitTypeAlways);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversionWithUseVarForAll_CanUseVarBecauseConditionalTypeMatches()
        {
            await TestInRegularAndScriptAsync(
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
}", options: s_preferImplicitTypeAlways);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversionWithUseVarForAll_CanUseVarButRequiresCastOfConditionalBranch()
        {
            await TestInRegularAndScriptAsync(
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
}", options: s_preferImplicitTypeAlways);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestKeepTriviaAroundIf()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
    }
}
