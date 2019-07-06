// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForMethodsAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExpressionBodyDiagnosticAnalyzer(), new UseExpressionBodyCodeFixProvider());

        private IDictionary<OptionKey, object> UseExpressionBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement);

        private IDictionary<OptionKey, object> UseBlockBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public void TestOptionSerialization1()
        {
            // Verify that bool-options can migrate to ExpressionBodyPreference-options.
            var option = new CodeStyleOption<bool>(false, NotificationOption.Silent);
            var serialized = option.ToXElement();
            var deserialized = CodeStyleOption<ExpressionBodyPreference>.FromXElement(serialized);

            Assert.Equal(ExpressionBodyPreference.Never, deserialized.Value);

            option = new CodeStyleOption<bool>(true, NotificationOption.Silent);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption<ExpressionBodyPreference>.FromXElement(serialized);

            Assert.Equal(ExpressionBodyPreference.WhenPossible, deserialized.Value);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public void TestOptionSerialization2()
        {
            // Verify that ExpressionBodyPreference-options can migrate to bool-options.
            var option = new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption.Silent);
            var serialized = option.ToXElement();
            var deserialized = CodeStyleOption<bool>.FromXElement(serialized);

            Assert.Equal(false, deserialized.Value);

            option = new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption.Silent);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption<bool>.FromXElement(serialized);

            Assert.Equal(true, deserialized.Value);

            // This new values can't actually translate back to a bool.  So we'll just get the default
            // value for this option.
            option = new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, NotificationOption.Silent);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption<bool>.FromXElement(serialized);

            Assert.Equal(default, deserialized.Value);
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
    void Goo()
    {
        [|Bar|]();
    }
}",
@"class C
{
    void Goo() => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Goo()
    {
        return [|Bar|]();
    }
}",
@"class C
{
    int Goo() => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Goo()
    {
        [|throw|] new NotImplementedException();
    }
}",
@"class C
{
    int Goo() => throw new NotImplementedException();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Goo()
    {
        [|throw|] new NotImplementedException(); // comment
    }
}",
@"class C
{
    int Goo() => throw new NotImplementedException(); // comment
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo() [|=>|] Bar();
}",
@"class C
{
    void Goo()
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
    int Goo() [|=>|] Bar();
}",
@"class C
{
    int Goo()
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
    int Goo() [|=>|] throw new NotImplementedException();
}",
@"class C
{
    int Goo()
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
    int Goo() [|=>|] throw new NotImplementedException(); // comment
}",
@"class C
{
    int Goo()
    {
        throw new NotImplementedException(); // comment
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        // Comment
        [|Bar|]();
    }
}",
@"class C
{
    void Goo() =>
        // Comment
        Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        // Comment
        return [|Bar|]();
    }
}",
@"class C
{
    void Goo() =>
        // Comment
        Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        // Comment
        throw [|Bar|]();
    }
}",
@"class C
{
    void Goo() =>
        // Comment
        throw Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        [|Bar|](); // Comment
    }
}",
@"class C
{
    void Goo() => Bar(); // Comment
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        return [|Bar|](); // Comment
    }
}",
@"class C
{
    void Goo() => Bar(); // Comment
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        throw [|Bar|](); // Comment
    }
}",
@"class C
{
    void Goo() => throw Bar(); // Comment
}", options: UseExpressionBody);
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

}", options: UseExpressionBody);
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

}", options: UseExpressionBody);
        }

        [WorkItem(20362, "https://github.com/dotnet/roslyn/issues/20362")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp6()
        {
            await TestAsync(
@"
using System;
class C
{
    void M() [|=>|] throw new NotImplementedException();
}",
@"
using System;
class C
{
    void M()
    {
        throw new NotImplementedException();
    }
}", options: UseExpressionBody, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [WorkItem(20352, "https://github.com/dotnet/roslyn/issues/20352")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDoNotOfferToConvertToBlockIfExpressionBodyPreferredIfCSharp6()
        {
            await TestMissingAsync(
@"
using System;
class C
{
    void M() [|=>|] 0;
}", new TestParameters(options: UseExpressionBody, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [WorkItem(20352, "https://github.com/dotnet/roslyn/issues/20352")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferToConvertToExpressionIfCSharp6()
        {
            await TestAsync(
@"
using System;
class C
{
    void M() { [|return|] 0; }
}",
@"
using System;
class C
{
    void M() => 0;
}", options: UseExpressionBody, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [WorkItem(20352, "https://github.com/dotnet/roslyn/issues/20352")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDoNotOfferToConvertToExpressionInCSharp6IfThrowExpression()
        {
            await TestMissingAsync(
@"
using System;
class C
{
    // throw expressions not supported in C# 6.
    void M() { [|throw|] new Exception(); }
}", new TestParameters(options: UseExpressionBody, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [WorkItem(20362, "https://github.com/dotnet/roslyn/issues/20362")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp6_FixAll()
        {
            await TestAsync(
@"
using System;
class C
{
    void M() {|FixAllInDocument:=>|} throw new NotImplementedException();
    void M(int i) => throw new NotImplementedException();
    int M(bool b) => 0;
}",
@"
using System;
class C
{
    void M()
    {
        throw new NotImplementedException();
    }

    void M(int i)
    {
        throw new NotImplementedException();
    }

    int M(bool b) => 0;
}", options: UseExpressionBody, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    async Task Goo() [|=>|] await Bar();

    Task Bar() { }
}",
@"using System.Threading.Tasks;

class C
{
    async Task Goo()
    {
        await Bar();
    }

    Task Bar() { }
}", options: UseBlockBody);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    async void Goo() [|=>|] await Bar();

    Task Bar() { }
}",
@"using System.Threading.Tasks;

class C
{
    async void Goo()
    {
        await Bar();
    }

    Task Bar() { }
}", options: UseBlockBody);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    async void Goo() [|=>|] await Bar();

    Task Bar() { }
}",
@"using System.Threading.Tasks;

class C
{
    async void Goo()
    {
        await Bar();
    }

    Task Bar() { }
}", options: UseBlockBody);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync4()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    async ValueTask Goo() [|=>|] await Bar();

    Task Bar() { }
}",
@"using System.Threading.Tasks;

class C
{
    async ValueTask Goo()
    {
        await Bar();
    }

    Task Bar() { }
}", options: UseBlockBody);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync5()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    async Task<int> Goo() [|=>|] await Bar();

    Task<int> Bar() { }
}",
@"using System.Threading.Tasks;

class C
{
    async Task<int> Goo()
    {
        return await Bar();
    }

    Task<int> Bar() { }
}", options: UseBlockBody);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync6()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    Task Goo() [|=>|] Bar();

    Task Bar() { }
}",
@"using System.Threading.Tasks;

class C
{
    Task Goo()
    {
        return Bar();
    }

    Task Bar() { }
}", options: UseBlockBody);
        }
    }
}
