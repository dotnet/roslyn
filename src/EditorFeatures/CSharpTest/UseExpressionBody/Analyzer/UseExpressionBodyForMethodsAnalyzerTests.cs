// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForMethodsAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExpressionBodyDiagnosticAnalyzer(), new UseExpressionBodyCodeFixProvider());

        private IDictionary<OptionKey, object> UseExpressionBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement);

        private IDictionary<OptionKey, object> UseBlockBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithNoneEnforcement);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public void TestOptionSerialization1()
        {
            // Verify that bool-options can migrate to ExpressionBodyPreference-options.
            var option = new CodeStyleOption<bool>(false, NotificationOption.None);
            var serialized = option.ToXElement();
            var deserialized = CodeStyleOption<ExpressionBodyPreference>.FromXElement(serialized);

            Assert.Equal(ExpressionBodyPreference.Never, deserialized.Value);

            option = new CodeStyleOption<bool>(true, NotificationOption.None);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption<ExpressionBodyPreference>.FromXElement(serialized);

            Assert.Equal(ExpressionBodyPreference.WhenPossible, deserialized.Value);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public void TestOptionSerialization2()
        {
            // Verify that ExpressionBodyPreference-options can migrate to bool-options.
            var option = new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption.None);
            var serialized = option.ToXElement();
            var deserialized = CodeStyleOption<bool>.FromXElement(serialized);

            Assert.Equal(false, deserialized.Value);

            option = new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption.None);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption<bool>.FromXElement(serialized);

            Assert.Equal(true, deserialized.Value);

            // This new values can't actually translate back to a bool.  So we'll just get the default
            // value for this option.
            option = new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, NotificationOption.None);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption<bool>.FromXElement(serialized);

            Assert.Equal(default(bool), deserialized.Value);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public void TestOptionEditorConfig1()
        {
            Assert.Null(CSharpCodeStyleOptions.ParseExpressionBodyPreference("true", null));
            Assert.Null(CSharpCodeStyleOptions.ParseExpressionBodyPreference("false", null));
            Assert.Null(CSharpCodeStyleOptions.ParseExpressionBodyPreference("when_on_single_line", null));
            Assert.Null(CSharpCodeStyleOptions.ParseExpressionBodyPreference("true:blah", null));
            Assert.Null(CSharpCodeStyleOptions.ParseExpressionBodyPreference("when_blah:error", null));

            var option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("false:error", null);
            Assert.Equal(ExpressionBodyPreference.Never, option.Value);
            Assert.Equal(NotificationOption.Error, option.Notification);

            option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("true:warning", null);
            Assert.Equal(ExpressionBodyPreference.WhenPossible, option.Value);
            Assert.Equal(NotificationOption.Warning, option.Notification);

            option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("when_on_single_line:suggestion", null);
            Assert.Equal(ExpressionBodyPreference.WhenOnSingleLine, option.Value);
            Assert.Equal(NotificationOption.Suggestion, option.Notification);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Foo()
    {
        [|Bar|]();
    }
}",
@"class C
{
    void Foo() => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo()
    {
        return [|Bar|]();
    }
}",
@"class C
{
    int Foo() => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo()
    {
        [|throw|] new NotImplementedException();
    }
}",
@"class C
{
    int Foo() => throw new NotImplementedException();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo()
    {
        [|throw|] new NotImplementedException(); // comment
    }
}",
@"class C
{
    int Foo() => throw new NotImplementedException(); // comment
}", ignoreTrivia: false, options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Foo() [|=>|] Bar();
}",
@"class C
{
    void Foo()
    {
        Bar();
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo() [|=>|] Bar();
}",
@"class C
{
    int Foo()
    {
        return Bar();
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo() [|=>|] throw new NotImplementedException();
}",
@"class C
{
    int Foo()
    {
        throw new NotImplementedException();
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo() [|=>|] throw new NotImplementedException(); // comment
}",
@"class C
{
    int Foo()
    {
        throw new NotImplementedException(); // comment
    }
}", ignoreTrivia: false, options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Foo()
    {
        // Comment
        [|Bar|]();
    }
}",
@"class C
{
    void Foo() =>
        // Comment
        Bar();
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Foo()
    {
        // Comment
        return [|Bar|]();
    }
}",
@"class C
{
    void Foo() =>
        // Comment
        Bar();
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Foo()
    {
        // Comment
        throw [|Bar|]();
    }
}",
@"class C
{
    void Foo() =>
        // Comment
        throw Bar();
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Foo()
    {
        [|Bar|](); // Comment
    }
}",
@"class C
{
    void Foo() => Bar(); // Comment
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Foo()
    {
        return [|Bar|](); // Comment
    }
}",
@"class C
{
    void Foo() => Bar(); // Comment
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Foo()
    {
        throw [|Bar|](); // Comment
    }
}",
@"class C
{
    void Foo() => throw Bar(); // Comment
}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [WorkItem(17120, "https://github.com/dotnet/roslyn/issues/17120")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDirectives1()
        {
            await TestInRegularAndScriptAsync(
@"
#define DEBUG
using System;

class Program
{
    void Method()
    {
#if DEBUG
        [|Console|].WriteLine();
#endif
    }
}",
@"
#define DEBUG
using System;

class Program
{
    void Method() =>
#if DEBUG
        Console.WriteLine();
#endif

}", options: UseExpressionBody, ignoreTrivia: false);
        }

        [WorkItem(17120, "https://github.com/dotnet/roslyn/issues/17120")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDirectives2()
        {
            await TestInRegularAndScriptAsync(
@"
#define DEBUG
using System;

class Program
{
    void Method()
    {
#if DEBUG
        [|Console|].WriteLine(a);
#else
        Console.WriteLine(b);
#endif
    }
}",
@"
#define DEBUG
using System;

class Program
{
    void Method() =>
#if DEBUG
        Console.WriteLine(a);
#else
        Console.WriteLine(b);
#endif

}", options: UseExpressionBody, ignoreTrivia: false);
        }
    }
}