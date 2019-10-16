// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.InitializeParameter;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
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
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSimpleReferenceType_CSharp6()
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
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
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
        if (i is null)
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
        public async Task TestNotOnExternParameter()
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
        public async Task TestNotOnPartialMethodDefinition1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    partial void M([||]string s);

    partial void M(string s)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnPartialMethodDefinition2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    partial void M(string s)
    {
    }

    partial void M([||]string s);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnPartialMethodImplementation1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    partial void M(string s);

    partial void M([||]string s)
    {
    }
}",
@"
using System;

class C
{
    partial void M(string s);

    partial void M(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnPartialMethodImplementation2()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    partial void M([||]string s)
    {
    }

    partial void M(string s);
}",
@"
using System;

class C
{
    partial void M(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }

    partial void M(string s);
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
        public async Task TestMultiNullableParameters()
        {
            await TestInRegularAndScript1Async(

@"
using System;

class C
{
    public C([||]string a, string b, string c)
    {
    }
}",
@"
using System;

class C
{
    public C(string a, string b, string c)
    {
        if (string.IsNullOrEmpty(a))
        {
            throw new ArgumentException(""message"", nameof(a));
        }

        if (string.IsNullOrEmpty(b))
        {
            throw new ArgumentException(""message"", nameof(b));
        }

        if (string.IsNullOrEmpty(c))
        {
            throw new ArgumentException(""message"", nameof(c));
        }
    }
}", index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCursorNotOnParameters()
        {
            await TestMissingInRegularAndScriptAsync(

@"
using System;

class C
{
    public C(string a[|,|] string b, string c)
    {
    }
}"
);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableWithCursorOnNonNullable()
        {
            await TestInRegularAndScript1Async(

@"
using System;

class C
{
    public C(string a, [||]bool b, string c)
    {
    }
}",
@"
using System;

class C
{
    public C(string a, bool b, string c)
    {
        if (string.IsNullOrEmpty(a))
        {
            throw new ArgumentException(""message"", nameof(a));
        }

        if (string.IsNullOrEmpty(c))
        {
            throw new ArgumentException(""message"", nameof(c));
        }
    }
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableNonNullable()
        {
            await TestInRegularAndScript1Async(

@"
using System;

class C
{
    public C([||]string a, bool b, string c)
    {
    }
}",
@"
using System;

class C
{
    public C(string a, bool b, string c)
    {
        if (string.IsNullOrEmpty(a))
        {
            throw new ArgumentException(""message"", nameof(a));
        }

        if (string.IsNullOrEmpty(c))
        {
            throw new ArgumentException(""message"", nameof(c));
        }
    }
}", index: 3);

        }
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableStringsAndObjects()
        {
            await TestInRegularAndScript1Async(

@"
using System;

class C
{
    public C([||]string a, object b, string c)
    {
    }
}",
@"
using System;

class C
{
    public C(string a, object b, string c)
    {
        if (string.IsNullOrEmpty(a))
        {
            throw new ArgumentException(""message"", nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }

        if (string.IsNullOrEmpty(c))
        {
            throw new ArgumentException(""message"", nameof(c));
        }
    }
}", index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableObjects()
        {
            await TestInRegularAndScript1Async(

@"
using System;

class C
{
    public C([||]object a, object b, object c)
    {
    }
}",
@"
using System;

class C
{
    public C(object a, object b, object c)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }

        if (c is null)
        {
            throw new ArgumentNullException(nameof(c));
        }
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableStructs()
        {
            await TestInRegularAndScript1Async(

@"
using System;

class C
{
    public C([||]int ? a, bool ? b, double ? c)
    {
    }
}",
@"
using System;

class C
{
    public C(int ? a, bool ? b, double ? c)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }

        if (c is null)
        {
            throw new ArgumentNullException(nameof(c));
        }
    }
}", index: 1);
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
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        S = s;
    }
}", parameters: new TestParameters(options:
    Option(CSharpCodeStyleOptions.PreferThrowExpression, CodeStyleOptions.FalseWithSilentEnforcement)));
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
        if (s is null)
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
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        Init();
    }
}", parameters: new TestParameters(options:
    Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement)));
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateLocalFunctionExpressionBody_NonVoid()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    void M()
    {
        int F([||]string s) => Init();
    }
}",
@"
using System;

class C
{
    void M()
    {
        int F(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return Init();
        }
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateLocalFunctionExpressionBody_Void()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    void M()
    {
        void F([||]string s) => Init();
    }
}",
@"
using System;

class C
{
    void M()
    {
        void F(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            Init();
        }
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateLambdaExpressionBody_NonVoid()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    void M()
    {
        Func<string, int> f = [||]s => GetValue();

        int GetValue() => 0;
    }
}",
@"
using System;

class C
{
    void M()
    {
        Func<string, int> f = s =>
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return GetValue();
        };

        int GetValue() => 0;
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateLambdaExpressionBody_Void()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    void M()
    {
        Action<string> f = [||]s => NoValue();

        void NoValue() { }
    }
}",
@"
using System;

class C
{
    void M()
    {
        Action<string> f = s =>
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            NoValue();
        };

        void NoValue() { }
    }
}");
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

        if (s is null)
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
        if (a is null)
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
        public async Task TestMissingWithExistingNullCheck6()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C([||]string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException();
        }
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheckInLocalFunction()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C()
    {
        void F([||]string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException();
            }
        }
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheckInLambda()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    public C()
    {
        Action<string> f = ([||]string s) => { if (s == null) { throw new ArgumentNullException(nameof(s)); } }
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
        if (s is null)
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
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnSimpleLambdaParameter()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = [||]s => { return 0; };
    }
}",
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = s =>
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return 0;
        };
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnSimpleLambdaParameter_EmptyBlock()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C()
    {
        Action<string> f = [||]s => { };
    }
}",
@"
using System;

class C
{
    public C()
    {
        Action<string> f = s =>
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }
        };
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnParenthesizedLambdaParameter()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = ([||]string s) => { return 0; };
    }
}",
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = (string s) =>
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return 0;
        };
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnAnonymousMethodParameter()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = delegate ([||]string s) { return 0; };
    }
}",
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = delegate (string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return 0;
        };
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnLocalFunctionParameter()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C()
    {
        void F([||]string s)
        {
        }
    }
}",
@"
using System;

class C
{
    public C()
    {
        void F(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnIndexerParameter()
        {
            await TestMissingAsync(
@"
class C
{
    int this[[||]string s]
    {
        get
        {
            return 0;
        }
    }
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnIndexerParameters()
        {
            await TestMissingAsync(
@"
class C
{
    int this[[|object a|], object b, object c]
    {
        get
        {
            return 0;
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

        [WorkItem(19172, "https://github.com/dotnet/roslyn/issues/19172")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [InlineData((int)PreferBracesPreference.None)]
        [InlineData((int)PreferBracesPreference.WhenMultiline)]
        public async Task TestPreferNoBlock(int preferBraces)
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
        if (s is null)
            throw new ArgumentNullException(nameof(s));
    }
}",
    parameters: new TestParameters(options:
        Option(CSharpCodeStyleOptions.PreferBraces, new CodeStyleOption<PreferBracesPreference>((PreferBracesPreference)preferBraces, NotificationOption.Silent))));
        }

        [WorkItem(19956, "https://github.com/dotnet/roslyn/issues/19956")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNoBlock()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C(string s[||])
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [WorkItem(21501, "https://github.com/dotnet/roslyn/issues/21501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInArrowExpression1()
        {
            await TestInRegularAndScript1Async(
@"
using System;
using System.Linq;

class C
{
    public int Foo(int[] array[||]) =>
        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
}",
@"
using System;
using System.Linq;

class C
{
    public int Foo(int[] array)
    {
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        return array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
    }
}");
        }

        [WorkItem(21501, "https://github.com/dotnet/roslyn/issues/21501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInArrowExpression2()
        {
            await TestInRegularAndScript1Async(
@"
using System;
using System.Linq;

class C
{
    public int Foo(int[] array[||]) /* Bar */ => /* Bar */
        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count(); /* Bar */
}",
@"
using System;
using System.Linq;

class C
{
    public int Foo(int[] array) /* Bar */
    {
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        /* Bar */
        return array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count(); /* Bar */
    }
}");
        }

        [WorkItem(21501, "https://github.com/dotnet/roslyn/issues/21501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingInArrowExpression1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;
using System.Linq;

class C
{
    public void Foo(string bar[||]) =>
#if DEBUG
        Console.WriteLine(""debug"" + bar);
#else
        Console.WriteLine(""release"" + bar);
#endif
}");
        }

        [WorkItem(21501, "https://github.com/dotnet/roslyn/issues/21501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingInArrowExpression2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;
using System.Linq;

class C
{
    public int Foo(int[] array[||]) =>
#if DEBUG
        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
#else
        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
#endif
}");
        }

        [WorkItem(21501, "https://github.com/dotnet/roslyn/issues/21501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInArrowExpression3()
        {
            await TestInRegularAndScript1Async(
@"
using System;
using System.Linq;

class C
{
    public void Foo(int[] array[||]) =>
        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
}",
@"
using System;
using System.Linq;

class C
{
    public void Foo(int[] array)
    {
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
    }
}");
        }

        [WorkItem(29190, "https://github.com/dotnet/roslyn/issues/29190")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSimpleReferenceTypeWithParameterNameSelected1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    public C(string [|s|])
    {
    }
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [WorkItem(29333, "https://github.com/dotnet/roslyn/issues/29333")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestLambdaWithIncorrectNumberOfParameters()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class C
{
    void M(Action<int, int> a)
    {
        M((x[||]
    }
}");
        }
    }
}
