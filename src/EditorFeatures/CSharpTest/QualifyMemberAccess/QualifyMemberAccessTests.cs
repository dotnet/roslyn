﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.QualifyMemberAccess;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QualifyMemberAccess
{
    public partial class QualifyMemberAccessTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpQualifyMemberAccessDiagnosticAnalyzer(), new CSharpQualifyMemberAccessCodeFixProvider());

        private Task TestAsyncWithOption(string code, string expected, PerLanguageOption<CodeStyleOption<bool>> option)
        {
            return TestAsyncWithOptionAndNotificationOption(code, expected, option, NotificationOption.Error);
        }

        private Task TestAsyncWithOptionAndNotificationOption(string code, string expected, PerLanguageOption<CodeStyleOption<bool>> option, NotificationOption notification)
        {
            return TestInRegularAndScriptAsync(code, expected, options: Option(option, true, notification));
        }

        private Task TestMissingAsyncWithOption(string code, PerLanguageOption<CodeStyleOption<bool>> option)
        {
            return TestMissingAsyncWithOptionAndNotificationOption(code, option, NotificationOption.Error);
        }

        private Task TestMissingAsyncWithOptionAndNotificationOption(string code, PerLanguageOption<CodeStyleOption<bool>> option, NotificationOption notification)
            => TestMissingInRegularAndScriptAsync(code, new TestParameters(options: Option(option, true, notification)));

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_LHS()
        {
            await TestAsyncWithOption(
@"class Class
{
    int i;

    void M()
    {
        [|i|] = 1;
    }
}",
@"class Class
{
    int i;

    void M()
    {
        this.i = 1;
    }
}",
CodeStyleOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_RHS()
        {
            await TestAsyncWithOption(
@"class Class
{
    int i;

    void M()
    {
        var x = [|i|];
    }
}",
@"class Class
{
    int i;

    void M()
    {
        var x = this.i;
    }
}",
CodeStyleOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_MethodArgument()
        {
            await TestAsyncWithOption(
@"class Class
{
    int i;

    void M(int ii)
    {
        M([|i|]);
    }
}",
@"class Class
{
    int i;

    void M(int ii)
    {
        M(this.i);
    }
}",
CodeStyleOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_ChainedAccess()
        {
            await TestAsyncWithOption(
@"class Class
{
    int i;

    void M()
    {
        var s = [|i|].ToString();
    }
}",
@"class Class
{
    int i;

    void M()
    {
        var s = this.i.ToString();
    }
}",
CodeStyleOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_ConditionalAccess()
        {
            await TestAsyncWithOption(
@"class Class
{
    string s;

    void M()
    {
        var x = [|s|]?.ToString();
    }
}",
@"class Class
{
    string s;

    void M()
    {
        var x = this.s?.ToString();
    }
}",
CodeStyleOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_OnBase()
        {
            await TestAsyncWithOption(
@"class Base
{
    protected int i;
}

class Derived : Base
{
    void M()
    {
        [|i|] = 1;
    }
}",
@"class Base
{
    protected int i;
}

class Derived : Base
{
    void M()
    {
        this.i = 1;
    }
}",
CodeStyleOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_NotSuggestedOnInstance()
        {
            await TestMissingAsyncWithOption(
@"class Class
{
    int i;

    void M()
    {
        Class c = new Class();
        c.[|i|] = 1;
    }
}",
CodeStyleOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_NotSuggestedOnStatic()
        {
            await TestMissingAsyncWithOption(
@"class C
{
    static int i;

    void M()
    {
        [|i|] = 1;
    }
}",
CodeStyleOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_LHS()
        {
            await TestAsyncWithOption(
@"class Class
{
    int i { get; set; }

    void M()
    {
        [|i|] = 1;
    }
}",
@"class Class
{
    int i { get; set; }

    void M()
    {
        this.i = 1;
    }
}",
CodeStyleOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_RHS()
        {
            await TestAsyncWithOption(
@"class Class
{
    int i { get; set; }

    void M()
    {
        var x = [|i|];
    }
}",
@"class Class
{
    int i { get; set; }

    void M()
    {
        var x = this.i;
    }
}",
CodeStyleOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_MethodArgument()
        {
            await TestAsyncWithOption(
@"class Class
{
    int i { get; set; }

    void M(int ii)
    {
        M([|i|]);
    }
}",
@"class Class
{
    int i { get; set; }

    void M(int ii)
    {
        M(this.i);
    }
}",
CodeStyleOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_ChainedAccess()
        {
            await TestAsyncWithOption(
@"class Class
{
    int i { get; set; }

    void M()
    {
        var s = [|i|].ToString();
    }
}",
@"class Class
{
    int i { get; set; }

    void M()
    {
        var s = this.i.ToString();
    }
}",
CodeStyleOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_ConditionalAccess()
        {
            await TestAsyncWithOption(
@"class Class
{
    string s { get; set; }

    void M()
    {
        var x = [|s|]?.ToString();
    }
}",
@"class Class
{
    string s { get; set; }

    void M()
    {
        var x = this.s?.ToString();
    }
}",
CodeStyleOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_OnBase()
        {
            await TestAsyncWithOption(
@"class Base
{
    protected int i { get; set; }
}

class Derived : Base
{
    void M()
    {
        [|i|] = 1;
    }
}",
@"class Base
{
    protected int i { get; set; }
}

class Derived : Base
{
    void M()
    {
        this.i = 1;
    }
}",
CodeStyleOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_NotSuggestedOnInstance()
        {
            await TestMissingAsyncWithOption(
@"class Class
{
    int i { get; set; }

    void M(Class c)
    {
        c.[|i|] = 1;
    }
}",
CodeStyleOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_NotSuggestedOnStatic()
        {
            await TestMissingAsyncWithOption(
@"class C
{
    static int i { get; set; }

    void M()
    {
        [|i|] = 1;
    }
}",
CodeStyleOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_VoidCallWithArguments()
        {
            await TestAsyncWithOption(
@"class Class
{
    void M(int i)
    {
        [|M|](0);
    }
}",
@"class Class
{
    void M(int i)
    {
        this.M(0);
    }
}",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_AsReturn()
        {
            await TestAsyncWithOption(
@"class Class
{
    int M()
    {
        return [|M|]();
    }",
@"class Class
{
    int M()
    {
        return this.M();
    }",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_ChainedAccess()
        {
            await TestAsyncWithOption(
@"class Class
{
    string M()
    {
        var s = [|M|]().ToString();
    }",
@"class Class
{
    string M()
    {
        var s = this.M().ToString();
    }",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_ConditionalAccess()
        {
            await TestAsyncWithOption(
@"class Class
{
    string M()
    {
        return [|M|]()?.ToString();
    }",
@"class Class
{
    string M()
    {
        return this.M()?.ToString();
    }",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_EventSubscription1()
        {
            await TestAsyncWithOption(
@"using System;

class C
{
    event EventHandler e;

    void Handler(object sender, EventArgs args)
    {
        e += [|Handler|];
    }
}",
@"using System;

class C
{
    event EventHandler e;

    void Handler(object sender, EventArgs args)
    {
        e += this.Handler;
    }
}",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_EventSubscription2()
        {
            await TestAsyncWithOption(
@"using System;

class C
{
    event EventHandler e;

    void Handler(object sender, EventArgs args)
    {
        e += new EventHandler([|Handler|]);
    }
}",
@"using System;

class C
{
    event EventHandler e;

    void Handler(object sender, EventArgs args)
    {
        e += new EventHandler(this.Handler);
    }
}",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_OnBase()
        {
            await TestAsyncWithOption(
@"class Base
{
    protected void Method()
    {
    }
}

class Derived : Base
{
    void M()
    {
        [|Method|]();
    }
}",
@"class Base
{
    protected void Method()
    {
    }
}

class Derived : Base
{
    void M()
    {
        this.Method();
    }
}",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_NotSuggestedOnInstance()
        {
            await TestMissingAsyncWithOption(
@"class Class
{
    void M(Class c)
    {
        c.[|M|]();
    }
}",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_NotSuggestedOnStatic()
        {
            await TestMissingAsyncWithOption(
@"class C
{
    static void Method()
    {
    }

    void M()
    {
        [|Method|]();
    }
}",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_EventSubscription()
        {
            await TestAsyncWithOption(
@"using System;

class C
{
    event EventHandler e;

    void Handler(object sender, EventArgs args)
    {
        [|e|] += Handler;
    }
}",
@"using System;

class C
{
    event EventHandler e;

    void Handler(object sender, EventArgs args)
    {
        this.e += Handler;
    }
}",
CodeStyleOptions.QualifyEventAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccessAsProperty_EventSubscription()
        {
            await TestAsyncWithOption(
@"using System;

class C
{
    event EventHandler e
    {
        add
        {
        }

        remove
        {
        }
    }

    void Handler(object sender, EventArgs args)
    {
        [|e|] += Handler;
    }
}",
@"using System;

class C
{
    event EventHandler e
    {
        add
        {
        }

        remove
        {
        }
    }

    void Handler(object sender, EventArgs args)
    {
        this.e += Handler;
    }
}",
CodeStyleOptions.QualifyEventAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_InvokeEvent1()
        {
            await TestAsyncWithOption(
@"using System;

class C
{
    event EventHandler e;

    void OnSomeEvent()
    {
        [|e|](this, new EventArgs());
    }
}",
@"using System;

class C
{
    event EventHandler e;

    void OnSomeEvent()
    {
        this.e(this, new EventArgs());
    }
}",
CodeStyleOptions.QualifyEventAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_InvokeEvent2()
        {
            await TestAsyncWithOption(
@"using System;

class C
{
    event EventHandler e;

    void OnSomeEvent()
    {
        [|e|].Invoke(this, new EventArgs());
    }
}",
@"using System;

class C
{
    event EventHandler e;

    void OnSomeEvent()
    {
        this.e.Invoke(this, new EventArgs());
    }
}",
CodeStyleOptions.QualifyEventAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_InvokeEvent3()
        {
            await TestAsyncWithOption(
@"using System;

class C
{
    event EventHandler e;

    void OnSomeEvent()
    {
        [|e|]?.Invoke(this, new EventArgs());
    }
}",
@"using System;

class C
{
    event EventHandler e;

    void OnSomeEvent()
    {
        this.e?.Invoke(this, new EventArgs());
    }
}",
CodeStyleOptions.QualifyEventAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_OnBase()
        {
            await TestAsyncWithOption(
@"using System;

class Base
{
    protected event EventHandler e;
}

class Derived : Base
{
    void Handler(object sender, EventArgs args)
    {
        [|e|] += Handler;
    }
}",
@"using System;

class Base
{
    protected event EventHandler e;
}

class Derived : Base
{
    void Handler(object sender, EventArgs args)
    {
        this.e += Handler;
    }
}",
CodeStyleOptions.QualifyEventAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_NotSuggestedOnInstance()
        {
            await TestMissingAsyncWithOption(
@"using System;

class Class
{
    event EventHandler e;

    void M(Class c)
    {
        c.[|e|] += Handler;
    }

    void Handler(object sender, EventArgs args)
    {
    }
}",
CodeStyleOptions.QualifyEventAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_NotSuggestedOnStatic()
        {
            await TestMissingAsyncWithOption(
@"using System;

class C
{
    static event EventHandler e;
}

void Handler(object sender, EventArgs args)
{
    [|e|] += Handler;
} }",
CodeStyleOptions.QualifyEventAccess);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMemberAccessNotPresentOnNotificationOptionNone()
        {
            await TestMissingAsyncWithOptionAndNotificationOption(
@"class Class
{
    int Property { get; set; };

    void M()
    {
        [|Property|] = 1;
    }
}",
CodeStyleOptions.QualifyPropertyAccess, NotificationOption.None);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMemberAccessOnNotificationOptionInfo()
        {
            await TestAsyncWithOptionAndNotificationOption(
@"class Class
{
    int Property { get; set; };

    void M()
    {
        [|Property|] = 1;
    }
}",
@"class Class
{
    int Property { get; set; };

    void M()
    {
        this.Property = 1;
    }
}",
CodeStyleOptions.QualifyPropertyAccess, NotificationOption.Suggestion);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMemberAccessOnNotificationOptionWarning()
        {
            await TestAsyncWithOptionAndNotificationOption(
@"class Class
{
    int Property { get; set; };

    void M()
    {
        [|Property|] = 1;
    }
}",
@"class Class
{
    int Property { get; set; };

    void M()
    {
        this.Property = 1;
    }
}",
CodeStyleOptions.QualifyPropertyAccess, NotificationOption.Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMemberAccessOnNotificationOptionError()
        {
            await TestAsyncWithOptionAndNotificationOption(
@"class Class
{
    int Property { get; set; };

    void M()
    {
        [|Property|] = 1;
    }
}",
@"class Class
{
    int Property { get; set; };

    void M()
    {
        this.Property = 1;
    }
}",
CodeStyleOptions.QualifyPropertyAccess, NotificationOption.Error);
        }

        [WorkItem(15325, "https://github.com/dotnet/roslyn/issues/15325")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18839"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyInstanceMethodInDelegateCreation()
        {
            await TestAsyncWithOption(
@"using System;

class A
{
    int Function(int x) => x + x;

    void Error()
    { 
        var func = new Func<int, int>([|Function|]);
        func(1);
    }
}",
@"using System;

class A
{
    int Function(int x) => x + x;

    void Error()
    { 
        var func = new Func<int, int>(this.Function);
        func(1);
    }
}",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(15325, "https://github.com/dotnet/roslyn/issues/15325")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task DoNotQualifyStaticMethodInDelegateCreation()
        {
            await TestMissingAsyncWithOption(
@"using System;

class A
{
    static int Function(int x) => x + x;

    void Error()
    { 
        var func = new Func<int, int>([|Function|]);
        func(1);
    }
}",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(17711, "https://github.com/dotnet/roslyn/issues/17711")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task DoNotReportToQualify_IfBaseQualificationOnField()
        {
            await TestMissingAsyncWithOption(
@"class Base
{
    protected int field;
}
class Derived : Base
{
    void M() { [|base.field|] = 0; }
}",
CodeStyleOptions.QualifyFieldAccess);
        }

        [WorkItem(17711, "https://github.com/dotnet/roslyn/issues/17711")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task DoNotReportToQualify_IfBaseQualificationOnProperty()
        {
            await TestMissingAsyncWithOption(
@"class Base
{
    protected virtual int Property { get; }
}
class Derived : Base
{
    protected override int Property { get { return [|base.Property|]; } }
}",
CodeStyleOptions.QualifyPropertyAccess);
        }

        [WorkItem(17711, "https://github.com/dotnet/roslyn/issues/17711")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task DoNotReportToQualify_IfBaseQualificationOnMethod()
        {
            await TestMissingAsyncWithOption(
@"class Base
{
    protected virtual void M() { }
}
class Derived : Base
{
    protected override void M() { [|base.M()|]; }
}",
CodeStyleOptions.QualifyMethodAccess);
        }

        [WorkItem(17711, "https://github.com/dotnet/roslyn/issues/17711")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task DoNotReportToQualify_IfBaseQualificationOnEvent()
        {
            await TestMissingAsyncWithOption(
@"class Base
{
    protected virtual event EventHandler Event;
}
class Derived : Base
{
    protected override event EventHandler Event 
    {
        add { [|base.Event|] += value; }
        remove { }
    }
}",
CodeStyleOptions.QualifyEventAccess);
        }
    }
}
