// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.IntroduceVariable;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntroduceVariable
{
    public partial class IntroduceLocalForExpressionTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpIntroduceLocalForExpressionCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_NoSemicolon()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        new DateTime()[||]
    }
}",
@"
using System;

class C
{
    void M()
    {
        DateTime {|Rename:dateTime|} = new DateTime();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_NoSemicolon_BlankLineAfter()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        new DateTime()[||]

    }
}",
@"
using System;

class C
{
    void M()
    {
        DateTime {|Rename:dateTime|} = new DateTime();

    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_NoSemicolon_SelectExpression()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        [|new DateTime()|]
    }
}",
@"
using System;

class C
{
    void M()
    {
        DateTime {|Rename:dateTime|} = new DateTime();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_Semicolon()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        new DateTime();[||]
    }
}",
@"
using System;

class C
{
    void M()
    {
        DateTime {|Rename:dateTime|} = new DateTime();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_Semicolon_BlankLineAfter()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        new DateTime();[||]

    }
}",
@"
using System;

class C
{
    void M()
    {
        DateTime {|Rename:dateTime|} = new DateTime();

    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_Semicolon_SelectExpression()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        [|new DateTime()|];
    }
}",
@"
using System;

class C
{
    void M()
    {
        DateTime {|Rename:dateTime|} = new DateTime();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_Semicolon_SelectStatement()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        [|new DateTime();|]
    }
}",
@"
using System;

class C
{
    void M()
    {
        DateTime {|Rename:dateTime|} = new DateTime();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_Space()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        new DateTime(); [||]
    }
}",
@"
using System;

class C
{
    void M()
    {
        DateTime {|Rename:dateTime|} = new DateTime(); 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_LeadingTrivia()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        // Comment
        new DateTime();[||]
    }
}",
@"
using System;

class C
{
    void M()
    {
        // Comment
        DateTime {|Rename:dateTime|} = new DateTime();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_PreferVar()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        new DateTime();[||]
    }
}",
@"
using System;

class C
{
    void M()
    {
        var {|Rename:dateTime|} = new DateTime();
    }
}", options: OptionsSet(
    (CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions.TrueWithSuggestionEnforcement),
    (CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOptions.TrueWithSuggestionEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task MissingOnVoidCall()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        Console.WriteLine();[||]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task MissingOnDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        var v = new DateTime()[||]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
        public async Task IntroduceLocal_ArithmeticExpression()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        1 + 1[||]
    }
}",
@"
using System;

class C
{
    void M()
    {
        int {|Rename:v|} = 1 + 1;
    }
}");
        }
    }
}
