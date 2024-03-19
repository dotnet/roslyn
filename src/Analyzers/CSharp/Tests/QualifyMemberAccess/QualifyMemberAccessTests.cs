// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.QualifyMemberAccess;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QualifyMemberAccess
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
    public partial class QualifyMemberAccessTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        public QualifyMemberAccessTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpQualifyMemberAccessDiagnosticAnalyzer(), new CSharpQualifyMemberAccessCodeFixProvider());

        private Task TestAsyncWithOption(string code, string expected, PerLanguageOption2<CodeStyleOption2<bool>> option)
            => TestAsyncWithOptionAndNotificationOption(code, expected, option, NotificationOption2.Error);

        private Task TestAsyncWithOptionAndNotificationOption(string code, string expected, PerLanguageOption2<CodeStyleOption2<bool>> option, NotificationOption2 notification)
            => TestInRegularAndScriptAsync(code, expected, options: Option(option, true, notification));

        private Task TestMissingAsyncWithOption(string code, PerLanguageOption2<CodeStyleOption2<bool>> option)
            => TestMissingAsyncWithOptionAndNotificationOption(code, option, NotificationOption2.Error);

        private Task TestMissingAsyncWithOptionAndNotificationOption(string code, PerLanguageOption2<CodeStyleOption2<bool>> option, NotificationOption2 notification)
            => TestMissingInRegularAndScriptAsync(code, new TestParameters(options: Option(option, true, notification)));

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyFieldAccess_LHS()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M()
                    {
                        [|i|] = 1;
                    }
                }
                """,
                """
                class Class
                {
                    int i;

                    void M()
                    {
                        this.i = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyFieldAccess_RHS()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M()
                    {
                        var x = [|i|];
                    }
                }
                """,
                """
                class Class
                {
                    int i;

                    void M()
                    {
                        var x = this.i;
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyFieldAccess_MethodArgument()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M(int ii)
                    {
                        M([|i|]);
                    }
                }
                """,
                """
                class Class
                {
                    int i;

                    void M(int ii)
                    {
                        M(this.i);
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyFieldAccess_ChainedAccess()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M()
                    {
                        var s = [|i|].ToString();
                    }
                }
                """,
                """
                class Class
                {
                    int i;

                    void M()
                    {
                        var s = this.i.ToString();
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyFieldAccess_ConditionalAccess()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    string s;

                    void M()
                    {
                        var x = [|s|]?.ToString();
                    }
                }
                """,
                """
                class Class
                {
                    string s;

                    void M()
                    {
                        var x = this.s?.ToString();
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyFieldAccess_OnBase()
        {
            await TestAsyncWithOption(
                """
                class Base
                {
                    protected int i;
                }

                class Derived : Base
                {
                    void M()
                    {
                        [|i|] = 1;
                    }
                }
                """,
                """
                class Base
                {
                    protected int i;
                }

                class Derived : Base
                {
                    void M()
                    {
                        this.i = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyFieldAccess_InObjectInitializer()
        {
            await TestAsyncWithOption(
                """
                class C
                {
                    int i = 1;
                    void M()
                    {
                        var test = new System.Collections.Generic.List<int> { [|i|] };
                    }
                }
                """,
                """
                class C
                {
                    int i = 1;
                    void M()
                    {
                        var test = new System.Collections.Generic.List<int> { this.i };
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyFieldAccess_InCollectionInitializer()
        {
            await TestAsyncWithOption(
                """
                class C
                {
                    int i = 1;
                    void M()
                    {
                        var test = new System.Collections.Generic.List<int> { [|i|] };
                    }
                }
                """,
                """
                class C
                {
                    int i = 1;
                    void M()
                    {
                        var test = new System.Collections.Generic.List<int> { this.i };
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyFieldAccess_NotSuggestedOnInstance()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M()
                    {
                        Class c = new Class();
                        c.[|i|] = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyFieldAccess_NotSuggestedOnStatic()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    static int i;

                    void M()
                    {
                        [|i|] = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyFieldAccess_NotSuggestedOnLocalVarInObjectInitializer()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    void M()
                    {
                         var foo = 1;
                         var test = new System.Collections.Generic.List<int> { [|foo|] };
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyFieldAccess_NotSuggestedOnLocalVarInCollectionInitializer()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    void M()
                    {
                         var foo = 1;
                         var test = new System.Collections.Generic.List<int> { [|foo|] };
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28091")]
        public async Task QualifyFieldAccess_NotSuggestedOnLocalVarInDictionaryInitializer()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    void M()
                    {
                         var foo = 1;
                         var test = new System.Collections.Generic.Dictionary<int, int> { { 2, [|foo|] } };
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyFieldAccess_Subpattern1()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M(Class c)
                    {
                        if (c is { [|i|]: 1 })
                        {
                        }
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyFieldAccess_Subpattern2()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M(Class c)
                    {
                        switch (t)
                        {
                            case Class { [|i|]: 1 }:
                                return;
                        }
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyFieldAccess_Subpattern3()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M(Class c)
                    {
                        var a = c switch
                        {
                            { [|i|]: 0 } => 1,
                            _ => 0
                        };
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyPropertyAccess_LHS()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    int i { get; set; }

                    void M()
                    {
                        [|i|] = 1;
                    }
                }
                """,
                """
                class Class
                {
                    int i { get; set; }

                    void M()
                    {
                        this.i = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyPropertyAccess_RHS()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    int i { get; set; }

                    void M()
                    {
                        var x = [|i|];
                    }
                }
                """,
                """
                class Class
                {
                    int i { get; set; }

                    void M()
                    {
                        var x = this.i;
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyPropertyAccess_PropertySubpattern1()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i { get; set; }

                    void M(Class c)
                    {
                        if (c is { [|i|]: 1 })
                        {
                        }
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyPropertyAccess_PropertySubpattern2()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i { get; set; }

                    void M(Class c)
                    {
                        switch (t)
                        {
                            case Class { [|i|]: 1 }:
                                return;
                        }
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyPropertyAccess_PropertySubpattern3()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i { get; set; }

                    void M(Class c)
                    {
                        var a = c switch
                        {
                            { [|i|]: 0 } => 1,
                            _ => 0
                        };
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyPropertyAccess_PropertySubpattern4()
        {
            //  it's ok that we qualify here because it's not a legal pattern (because it is not const).
            await TestAsyncWithOption(
                """
                class Class
                {
                    int i { get; set; }

                    void M(Class c)
                    {
                        var a = c switch
                        {
                            { i: [|i|] } => 1,
                            _ => 0
                        };
                    }
                }
                """,
                """
                class Class
                {
                    int i { get; set; }

                    void M(Class c)
                    {
                        var a = c switch
                        {
                            { i: this.i } => 1,
                            _ => 0
                        };
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyPropertyAccess_FieldSubpattern1()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M(Class c)
                    {
                        if (c is { [|i|]: 1 })
                        {
                        }
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyPropertyAccess_FieldSubpattern2()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M(Class c)
                    {
                        switch (t)
                        {
                            case Class { [|i|]: 1 }:
                                return;
                        }
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyPropertyAccess_FieldSubpattern3()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M(Class c)
                    {
                        var a = c switch
                        {
                            { [|i|]: 0 } => 1,
                            _ => 0
                        };
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40242")]
        public async Task QualifyPropertyAccess_FieldSubpattern4()
        {
            //  it's ok that we qualify here because it's not a legal pattern (because it is not const).
            await TestAsyncWithOption(
                """
                class Class
                {
                    int i;

                    void M(Class c)
                    {
                        var a = c switch
                        {
                            { i: [|i|] } => 1,
                            _ => 0
                        };
                    }
                }
                """,
                """
                class Class
                {
                    int i;

                    void M(Class c)
                    {
                        var a = c switch
                        {
                            { i: this.i } => 1,
                            _ => 0
                        };
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyPropertyAccess_MethodArgument()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    int i { get; set; }

                    void M(int ii)
                    {
                        M([|i|]);
                    }
                }
                """,
                """
                class Class
                {
                    int i { get; set; }

                    void M(int ii)
                    {
                        M(this.i);
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyPropertyAccess_ChainedAccess()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    int i { get; set; }

                    void M()
                    {
                        var s = [|i|].ToString();
                    }
                }
                """,
                """
                class Class
                {
                    int i { get; set; }

                    void M()
                    {
                        var s = this.i.ToString();
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyPropertyAccess_ConditionalAccess()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    string s { get; set; }

                    void M()
                    {
                        var x = [|s|]?.ToString();
                    }
                }
                """,
                """
                class Class
                {
                    string s { get; set; }

                    void M()
                    {
                        var x = this.s?.ToString();
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyPropertyAccess_OnBase()
        {
            await TestAsyncWithOption(
                """
                class Base
                {
                    protected int i { get; set; }
                }

                class Derived : Base
                {
                    void M()
                    {
                        [|i|] = 1;
                    }
                }
                """,
                """
                class Base
                {
                    protected int i { get; set; }
                }

                class Derived : Base
                {
                    void M()
                    {
                        this.i = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyPropertyAccess_NotSuggestedOnInstance()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    int i { get; set; }

                    void M(Class c)
                    {
                        c.[|i|] = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyPropertyAccess_NotSuggestedOnStatic()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    static int i { get; set; }

                    void M()
                    {
                        [|i|] = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyMethodAccess_VoidCallWithArguments()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    void M(int i)
                    {
                        [|M|](0);
                    }
                }
                """,
                """
                class Class
                {
                    void M(int i)
                    {
                        this.M(0);
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyMethodAccess_AsReturn()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    int M()
                    {
                        return [|M|]();
                    }
                """,
                """
                class Class
                {
                    int M()
                    {
                        return this.M();
                    }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyMethodAccess_ChainedAccess()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    string M()
                    {
                        var s = [|M|]().ToString();
                    }
                """,
                """
                class Class
                {
                    string M()
                    {
                        var s = this.M().ToString();
                    }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyMethodAccess_ConditionalAccess()
        {
            await TestAsyncWithOption(
                """
                class Class
                {
                    string M()
                    {
                        return [|M|]()?.ToString();
                    }
                """,
                """
                class Class
                {
                    string M()
                    {
                        return this.M()?.ToString();
                    }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyMethodAccess_EventSubscription1()
        {
            await TestAsyncWithOption(
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void Handler(object sender, EventArgs args)
                    {
                        e += [|Handler|];
                    }
                }
                """,
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void Handler(object sender, EventArgs args)
                    {
                        e += this.Handler;
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyMethodAccess_EventSubscription2()
        {
            await TestAsyncWithOption(
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void Handler(object sender, EventArgs args)
                    {
                        e += new EventHandler([|Handler|]);
                    }
                }
                """,
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void Handler(object sender, EventArgs args)
                    {
                        e += new EventHandler(this.Handler);
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyMethodAccess_OnBase()
        {
            await TestAsyncWithOption(
                """
                class Base
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
                }
                """,
                """
                class Base
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
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyMethodAccess_NotSuggestedOnInstance()
        {
            await TestMissingAsyncWithOption(
                """
                class Class
                {
                    void M(Class c)
                    {
                        c.[|M|]();
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyMethodAccess_NotSuggestedOnStatic()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    static void Method()
                    {
                    }

                    void M()
                    {
                        [|Method|]();
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyMethodAccess_NotSuggestedOnObjectInitializer()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    void M()
                    {
                         var foo = 1;
                         var test = new System.Collections.Generic.List<int> { [|foo|] };
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyLocalMethodAccess_NotSuggestedOnObjectInitializer()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    void M()
                    {
                        int Local() => 1;
                        var test = new System.Collections.Generic.List<int> { [|Local()|] };
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyMethodAccess_NotSuggestedOnCollectionInitializer()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    void M()
                    {
                         var foo = 1;
                         var test = new System.Collections.Generic.List<int> { [|foo|] };
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyLocalMethodAccess_NotSuggestedOnCollectionInitializer()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    void M()
                    {
                        int Local() => 1;
                        var test = new System.Collections.Generic.List<int> { [|Local()|] };
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyLocalMethodAccess_NotSuggestedInMethodCall()
        {
            await TestMissingAsyncWithOption(
                """
                class C
                {
                    void M()
                    {
                        int Local() => 1;
                        [|Local|]();
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38043")]
        public async Task QualifyLocalMethodAccess_NotSuggestedInNestedMethodCall()
        {
            await TestMissingAsyncWithOption(
                """
                using System;

                class C
                {
                    void Method()
                    {
                        object LocalFunction() => new object();
                        this.Method2([|LocalFunction|]);
                    }

                    void Method2(Func<object> LocalFunction)
                    {
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38043")]
        public async Task QualifyLocalMethodAccess_NotSuggestedInCollectionInitializer()
        {
            await TestMissingAsyncWithOption(
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    void Method()
                    {
                        object LocalFunction() => new object();
                        var dict = new Dictionary<Func<object>, int>() { { [|LocalFunction|], 1 } };
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38043")]
        public async Task QualifyLocalMethodAccess_NotSuggestedInObjectMethodInvocation()
        {
            await TestMissingAsyncWithOption(
                """
                using System;

                class C
                {
                    void Method()
                    {
                        object LocalFunction() => new object();
                        [|LocalFunction|]();
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyEventAccess_EventSubscription()
        {
            await TestAsyncWithOption(
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void Handler(object sender, EventArgs args)
                    {
                        [|e|] += Handler;
                    }
                }
                """,
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void Handler(object sender, EventArgs args)
                    {
                        this.e += Handler;
                    }
                }
                """,
CodeStyleOptions2.QualifyEventAccess);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyEventAccessAsProperty_EventSubscription()
        {
            await TestAsyncWithOption(
                """
                using System;

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
                }
                """,
                """
                using System;

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
                }
                """,
CodeStyleOptions2.QualifyEventAccess);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyEventAccess_InvokeEvent1()
        {
            await TestAsyncWithOption(
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void OnSomeEvent()
                    {
                        [|e|](this, new EventArgs());
                    }
                }
                """,
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void OnSomeEvent()
                    {
                        this.e(this, new EventArgs());
                    }
                }
                """,
CodeStyleOptions2.QualifyEventAccess);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyEventAccess_InvokeEvent2()
        {
            await TestAsyncWithOption(
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void OnSomeEvent()
                    {
                        [|e|].Invoke(this, new EventArgs());
                    }
                }
                """,
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void OnSomeEvent()
                    {
                        this.e.Invoke(this, new EventArgs());
                    }
                }
                """,
CodeStyleOptions2.QualifyEventAccess);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyEventAccess_InvokeEvent3()
        {
            await TestAsyncWithOption(
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void OnSomeEvent()
                    {
                        [|e|]?.Invoke(this, new EventArgs());
                    }
                }
                """,
                """
                using System;

                class C
                {
                    event EventHandler e;

                    void OnSomeEvent()
                    {
                        this.e?.Invoke(this, new EventArgs());
                    }
                }
                """,
CodeStyleOptions2.QualifyEventAccess);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/7587")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyEventAccess_OnBase()
        {
            await TestAsyncWithOption(
                """
                using System;

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
                }
                """,
                """
                using System;

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
                }
                """,
CodeStyleOptions2.QualifyEventAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyEventAccess_NotSuggestedOnInstance()
        {
            await TestMissingAsyncWithOption(
                """
                using System;

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
                }
                """,
CodeStyleOptions2.QualifyEventAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")]
        public async Task QualifyEventAccess_NotSuggestedOnStatic()
        {
            await TestMissingAsyncWithOption(
                """
                using System;

                class C
                {
                    static event EventHandler e;
                }

                void Handler(object sender, EventArgs args)
                {
                    [|e|] += Handler;
                } }
                """,
CodeStyleOptions2.QualifyEventAccess);
        }

        [Fact]
        public async Task QualifyMemberAccessOnNotificationOptionSilent()
        {
            await TestAsyncWithOptionAndNotificationOption(
                """
                class Class
                {
                    int Property { get; set; };

                    void M()
                    {
                        [|Property|] = 1;
                    }
                }
                """,
                """
                class Class
                {
                    int Property { get; set; };

                    void M()
                    {
                        this.Property = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess, NotificationOption2.Silent);
        }

        [Fact]
        public async Task QualifyMemberAccessOnNotificationOptionInfo()
        {
            await TestAsyncWithOptionAndNotificationOption(
                """
                class Class
                {
                    int Property { get; set; };

                    void M()
                    {
                        [|Property|] = 1;
                    }
                }
                """,
                """
                class Class
                {
                    int Property { get; set; };

                    void M()
                    {
                        this.Property = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess, NotificationOption2.Suggestion);
        }

        [Fact]
        public async Task QualifyMemberAccessOnNotificationOptionWarning()
        {
            await TestAsyncWithOptionAndNotificationOption(
                """
                class Class
                {
                    int Property { get; set; };

                    void M()
                    {
                        [|Property|] = 1;
                    }
                }
                """,
                """
                class Class
                {
                    int Property { get; set; };

                    void M()
                    {
                        this.Property = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess, NotificationOption2.Warning);
        }

        [Fact]
        public async Task QualifyMemberAccessOnNotificationOptionError()
        {
            await TestAsyncWithOptionAndNotificationOption(
                """
                class Class
                {
                    int Property { get; set; };

                    void M()
                    {
                        [|Property|] = 1;
                    }
                }
                """,
                """
                class Class
                {
                    int Property { get; set; };

                    void M()
                    {
                        this.Property = 1;
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess, NotificationOption2.Error);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18839")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/15325")]
        public async Task QualifyInstanceMethodInDelegateCreation()
        {
            await TestAsyncWithOption(
                """
                using System;

                class A
                {
                    int Function(int x) => x + x;

                    void Error()
                    { 
                        var func = new Func<int, int>([|Function|]);
                        func(1);
                    }
                }
                """,
                """
                using System;

                class A
                {
                    int Function(int x) => x + x;

                    void Error()
                    { 
                        var func = new Func<int, int>(this.Function);
                        func(1);
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15325")]
        public async Task DoNotQualifyStaticMethodInDelegateCreation()
        {
            await TestMissingAsyncWithOption(
                """
                using System;

                class A
                {
                    static int Function(int x) => x + x;

                    void Error()
                    { 
                        var func = new Func<int, int>([|Function|]);
                        func(1);
                    }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17711")]
        public async Task DoNotReportToQualify_IfBaseQualificationOnField()
        {
            await TestMissingAsyncWithOption(
                """
                class Base
                {
                    protected int field;
                }
                class Derived : Base
                {
                    void M() { [|base.field|] = 0; }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17711")]
        public async Task DoNotReportToQualify_IfBaseQualificationOnProperty()
        {
            await TestMissingAsyncWithOption(
                """
                class Base
                {
                    protected virtual int Property { get; }
                }
                class Derived : Base
                {
                    protected override int Property { get { return [|base.Property|]; } }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17711")]
        public async Task DoNotReportToQualify_IfBaseQualificationOnMethod()
        {
            await TestMissingAsyncWithOption(
                """
                class Base
                {
                    protected virtual void M() { }
                }
                class Derived : Base
                {
                    protected override void M() { [|base.M()|]; }
                }
                """,
CodeStyleOptions2.QualifyMethodAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17711")]
        public async Task DoNotReportToQualify_IfBaseQualificationOnEvent()
        {
            await TestMissingAsyncWithOption(
                """
                class Base
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
                }
                """,
CodeStyleOptions2.QualifyEventAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")]
        public async Task DoNotReportToQualify_IfInStaticContext1()
        {
            await TestMissingAsyncWithOption(
                """
                class Program
                {
                    public int Foo { get; set; }
                    public static string Bar = nameof([|Foo|]);
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")]
        public async Task DoNotReportToQualify_IfInStaticContext2()
        {
            await TestMissingAsyncWithOption(
                """
                class Program
                {
                    public int Foo { get; set; }
                    public string Bar = nameof([|Foo|]);
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")]
        public async Task DoNotReportToQualify_IfInStaticContext3()
        {
            await TestMissingAsyncWithOption(
                """
                class Program
                {
                    public int Foo { get; set; }
                    static void Main(string[] args)
                    {
                        System.Console.WriteLine(nameof([|Foo|]));
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")]
        public async Task DoNotReportToQualify_IfInStaticContext4()
        {
            await TestMissingAsyncWithOption(
                """
                class Program
                {
                    public int Foo;
                    static void Main(string[] args)
                    {
                        System.Console.WriteLine(nameof([|Foo|]));
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")]
        public async Task DoNotReportToQualify_IfInStaticContext5()
        {
            await TestMissingAsyncWithOption(
                """
                class Program
                {
                    public int Foo { get; set; }
                    static string Bar { get; set; }

                    static Program()
                    {
                        Bar = nameof([|Foo|]);
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")]
        public async Task DoNotReportToQualify_IfInStaticContext6()
        {
            await TestMissingAsyncWithOption(
                """
                public class Foo
                {
                    public event EventHandler Bar;

                    private string Field = nameof([|Bar|]);
                }
                """,
CodeStyleOptions2.QualifyEventAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32093")]
        public async Task DoNotReportToQualify_IfInBaseConstructor()
        {
            await TestMissingAsyncWithOption(
                """
                public class Base
                {
                    public string Foo { get; }
                    public Base(string foo){}
                }
                public class Derived : Base
                {
                    public Derived()
                        : base(nameof([|Foo|]))
                    {}
                }
                """,
                CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")]
        public async Task QualifyPropertyAccess_InAccessorExpressionBody()
        {
            await TestAsyncWithOption(
                """
                public class C
                {
                    public string Foo { get; set; }
                    public string Bar { get => [|Foo|]; }
                }
                """,
                """
                public class C
                {
                    public string Foo { get; set; }
                    public string Bar { get => this.Foo; }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")]
        public async Task QualifyPropertyAccess_InAccessorWithBodyAndExpressionBody1()
        {
            await TestAsyncWithOption(
                """
                public class C
                {
                    public string Foo { get; set; }
                    public string Bar { get { return [|Foo|]; } => Foo; }
                }
                """,
                """
                public class C
                {
                    public string Foo { get; set; }
                    public string Bar { get { return this.Foo; } => Foo; }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")]
        public async Task QualifyPropertyAccess_InAccessorWithBodyAndExpressionBody2()
        {
            await TestAsyncWithOption(
                """
                public class C
                {
                    public string Foo { get; set; }
                    public string Bar { get { return Foo; } => [|Foo|]; }
                }
                """,
                """
                public class C
                {
                    public string Foo { get; set; }
                    public string Bar { get { return Foo; } => this.Foo; }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyPropertyAccess_InObjectInitializer()
        {
            await TestAsyncWithOption(
                """
                class C
                {
                    public int Foo { get; set }
                    void M()
                    {
                        var test = new System.Collections.Generic.List<int> { [|Foo|] };
                    }
                }
                """,
                """
                class C
                {
                    public int Foo { get; set }
                    void M()
                    {
                        var test = new System.Collections.Generic.List<int> { this.Foo };
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")]
        public async Task QualifyPropertyAccess_InCollectionInitializer()
        {
            await TestAsyncWithOption(
                """
                class C
                {
                    public int Foo { get; set }
                    void M()
                    {
                        var test = new System.Collections.Generic.List<int> { [|Foo|] };
                    }
                }
                """,
                """
                class C
                {
                    public int Foo { get; set }
                    void M()
                    {
                        var test = new System.Collections.Generic.List<int> { this.Foo };
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22776")]
        public async Task DoNotReportToQualify_InObjectInitializer1()
        {
            await TestMissingAsyncWithOption(
                """
                public class C
                {
                    public string Foo { get; set; }
                    public void Bar()
                    {
                        var c = new C
                        {
                            [|Foo|] = string.Empty
                        };
                    }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22776")]
        public async Task DoNotReportToQualify_InObjectInitializer2()
        {
            await TestMissingAsyncWithOption(
                """
                public class C
                {
                    public string Foo;
                    public void Bar()
                    {
                        var c = new C
                        {
                            [|Foo|] = string.Empty
                        };
                    }
                }
                """,
CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22776")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/64374")]
        public async Task DoReportToQualify_InObjectInitializer2()
        {
            await TestAsyncWithOption(
                """
                public class C
                {
                    public string Foo;
                    public void Bar()
                    {
                        var c = new C
                        {
                            Foo = [|Foo|]
                        };
                    }
                }
                """,
                """
                public class C
                {
                    public string Foo;
                    public void Bar()
                    {
                        var c = new C
                        {
                            Foo = this.Foo
                        };
                    }
                }
                """,
                CodeStyleOptions2.QualifyFieldAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26893")]
        public async Task DoNotReportToQualify_IfInAttribute1()
        {
            await TestMissingAsyncWithOption(
                """
                using System;

                class MyAttribute : Attribute 
                {
                    public MyAttribute(string name) { }
                }

                [My(nameof([|Goo|]))]
                class Program
                {
                    int Goo { get; set; }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26893")]
        public async Task DoNotReportToQualify_IfInAttribute2()
        {
            await TestMissingAsyncWithOption(
                """
                using System;

                class MyAttribute : Attribute 
                {
                    public MyAttribute(string name) { }
                }

                class Program
                {
                    [My(nameof([|Goo|]))]
                    int Goo { get; set; }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26893")]
        public async Task DoNotReportToQualify_IfInAttribute3()
        {
            await TestMissingAsyncWithOption(
                """
                using System;

                class MyAttribute : Attribute 
                {
                    public MyAttribute(string name) { }
                }

                class Program
                {
                    [My(nameof([|Goo|]))]
                    public int Bar = 0 ;
                    public int Goo { get; set; }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26893")]
        public async Task DoNotReportToQualify_IfInAttribute4()
        {
            await TestMissingAsyncWithOption(
                """
                using System;

                class MyAttribute : Attribute 
                {
                    public MyAttribute(string name) { }
                }

                class Program
                {
                    int Goo { [My(nameof([|Goo|]))]get; set; }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26893")]
        public async Task DoNotReportToQualify_IfInAttribute5()
        {
            await TestMissingAsyncWithOption(
                """
                using System;

                class MyAttribute : Attribute 
                {
                    public MyAttribute(string name) { }
                }

                class Program
                {
                    int Goo { get; set; }
                    void M([My(nameof([|Goo|]))]int i) { }
                }
                """,
CodeStyleOptions2.QualifyPropertyAccess);
        }
    }
}
