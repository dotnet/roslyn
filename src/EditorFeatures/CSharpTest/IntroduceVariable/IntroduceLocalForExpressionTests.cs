﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.IntroduceVariable;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task IntroduceLocal_Inside_Expression()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        new TimeSpan() +[||] new TimeSpan();
    }
}",
@"
using System;

class C
{
    void M()
    {
        TimeSpan {|Rename:timeSpan|} = new TimeSpan() + new TimeSpan();
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
        public async Task MissingOnAssignmentExpressionStatement()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    void M()
    {
        int a = 42;
        [||]a = 42;
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
}", options: new OptionsCollection(GetLanguage())
    {
        { CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions2.TrueWithSuggestionEnforcement },
        { CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOptions2.TrueWithSuggestionEnforcement },
    });
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
