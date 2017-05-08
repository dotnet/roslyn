﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.InitializeParameter;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InitializeParameter
{
    public partial class AddParameterCheckTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpAddParameterCheckCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestEmptyFile()
        {
            await TestMissingInRegularAndScriptAsync(
@"[||]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSimpleReferenceType()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C([||]string s)
    {
    }
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNullable()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C([||]int? i)
    {
    }
}",
@"
using System;

class C
{
    public C(int? i)
    {
        if (i == null)
        {
            throw new ArgumentNullException(nameof(i));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnValueType()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C([||]int i)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnInterfaceParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

interface I
{
    void M([||]string s);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnAbstractParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    abstract void M([||]string s);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnPartialMethod1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    private partial void M([||]string s);

    private void M(string s)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnPartialMethod2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    private void M(string s)
    {
    }

    private partial void M([||]string s);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnExternMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    extern void M([||]string s);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateExistingFieldAssignment()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    private string _s;

    public C([||]string s)
    {
        _s = s;
    }
}",
@"
using System;

class C
{
    private string _s;

    public C(string s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateExistingPropertyAssignment()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    private string S;

    public C([||]string s)
    {
        S = s;
    }
}",
@"
using System;

class C
{
    private string S;

    public C(string s)
    {
        S = s ?? throw new ArgumentNullException(nameof(s));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task DoNotUseThrowExpressionBeforeCSharp7()
        {
            await TestAsync(
@"
using System;

class C
{
    private string S;

    public C([||]string s)
    {
        S = s;
    }
}",
@"
using System;

class C
{
    private string S;

    public C(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        S = s;
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task RespectUseThrowExpressionOption()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    private string S;

    public C([||]string s)
    {
        S = s;
    }
}",
@"
using System;

class C
{
    private string S;

    public C(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        S = s;
    }
}", parameters: new TestParameters(options:
    Option(CodeStyleOptions.PreferThrowExpression, CodeStyleOptions.FalseWithNoneEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateExpressionBody1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    private string S;

    public C([||]string s)
        => S = s;
}",
@"
using System;

class C
{
    private string S;

    public C(string s)
        => S = s ?? throw new ArgumentNullException(nameof(s));
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateExpressionBody2()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C([||]string s)
        => Init();
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }
        
        Init();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateExpressionBody3()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C([||]string s)
        => Init();
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }
        
        Init();
    }
}", parameters: new TestParameters(options:
    Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertAfterExistingNullCheck1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C(string a, [||]string s)
    {
        if (a == null)
        {
        }
    }
}",
@"
using System;

class C
{
    public C(string a, string s)
    {
        if (a == null)
        {
        }

        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertBeforeExistingNullCheck1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C(string [||]a, string s)
    {
        if (s == null)
        {
        }
    }
}",
@"
using System;

class C
{
    public C(string a, string s)
    {
        if (a == null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (s == null)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C([||]string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C([||]string s)
    {
        _s = s ?? throw new ArgumentNullException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck3()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C([||]string s)
    {
        if (string.IsNullOrEmpty(s))
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck4()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C([||]string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck5()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C([||]string s)
    {
        if (null == s)
        {
            throw new ArgumentNullException();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithoutParameterName()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C([||]string)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInMethod()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    void F([||]string s)
    {
    }
}",
@"
using System;

class C
{
    void F(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInOperator()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public static C operator +(C c1, [||]string s)
    {
    }
}",
@"
using System;

class C
{
    public static C operator +(C c1, [||]string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnLambdaParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = ([||]string s) => { return 0; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnLocalFunctionParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C()
    {
        void Foo([||]string s)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSpecialStringCheck1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C([||]string s)
    {
    }
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            throw new ArgumentException(""message"", nameof(s));
        }
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSpecialStringCheck2()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C([||]string s)
    {
    }
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new ArgumentException(""message"", nameof(s));
        }
    }
}", index: 2);
        }

        [WorkItem(19173, "https://github.com/dotnet/roslyn/issues/19173")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingOnUnboundTypeWithExistingNullCheck()
        {
            await TestMissingAsync(
@"
class C
{
    public C(String [||]s)
    {
        if (s == null)
        {
            throw new System.Exception();
        }
    }
}");
        }

        [WorkItem(19174, "https://github.com/dotnet/roslyn/issues/19174")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestRespectPredefinedTypePreferences()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class Program
{
    static void Main([||]String bar)
    {
    }
}",
@"
using System;

class Program
{
    static void Main(String bar)
    {
        if (String.IsNullOrEmpty(bar))
        {
            throw new ArgumentException(""message"", nameof(bar));
        }
    }
}", index: 1,
    parameters: new TestParameters(
        options: Option(
            CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess,
            CodeStyleOptions.FalseWithSuggestionEnforcement)));
        }
    }
}