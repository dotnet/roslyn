// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    using VerifyCS = CSharpCodeFixVerifier<
        UseExpressionBodyDiagnosticAnalyzer,
        UseExpressionBodyCodeFixProvider>;

    public class UseExpressionBodyForMethodsAnalyzerTests
    {
        private static async Task TestWithUseExpressionBody(string code, string fixedCode, LanguageVersion version = LanguageVersion.CSharp8)
        {
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = version,
                Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, ExpressionBodyPreference.WhenPossible } }
            }.RunAsync();
        }

        private static async Task TestWithUseBlockBody(string code, string fixedCode, ReferenceAssemblies? referenceAssemblies = null)
        {
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, ExpressionBodyPreference.Never } },
                ReferenceAssemblies = referenceAssemblies ?? ReferenceAssemblies.Default,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public void TestOptionSerialization1()
        {
            // Verify that bool-options can migrate to ExpressionBodyPreference-options.
            var option = new CodeStyleOption2<bool>(false, NotificationOption2.Silent);
            var serialized = option.ToXElement();
            var deserialized = CodeStyleOption2<ExpressionBodyPreference>.FromXElement(serialized);

            Assert.Equal(ExpressionBodyPreference.Never, deserialized.Value);

            option = new CodeStyleOption2<bool>(true, NotificationOption2.Silent);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption2<ExpressionBodyPreference>.FromXElement(serialized);

            Assert.Equal(ExpressionBodyPreference.WhenPossible, deserialized.Value);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public void TestOptionSerialization2()
        {
            // Verify that ExpressionBodyPreference-options can migrate to bool-options.
            var option = new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption2.Silent);
            var serialized = option.ToXElement();
            var deserialized = CodeStyleOption2<bool>.FromXElement(serialized);

            Assert.False(deserialized.Value);

            option = new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption2<bool>.FromXElement(serialized);

            Assert.True(deserialized.Value);

            // This new values can't actually translate back to a bool.  So we'll just get the default
            // value for this option.
            option = new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.Silent);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption2<bool>.FromXElement(serialized);

            Assert.Equal(default, deserialized.Value);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public void TestOptionEditorConfig1()
        {
            var option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("true", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
            Assert.Equal(ExpressionBodyPreference.WhenPossible, option.Value);
            Assert.Equal(NotificationOption2.Silent, option.Notification);

            option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("false", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
            Assert.Equal(ExpressionBodyPreference.Never, option.Value);
            Assert.Equal(NotificationOption2.Silent, option.Notification);

            option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("when_on_single_line", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
            Assert.Equal(ExpressionBodyPreference.WhenOnSingleLine, option.Value);
            Assert.Equal(NotificationOption2.Silent, option.Notification);

            option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("true:blah", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
            Assert.Equal(ExpressionBodyPreference.Never, option.Value);
            Assert.Equal(NotificationOption2.Silent, option.Notification);

            option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("when_blah:error", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
            Assert.Equal(ExpressionBodyPreference.Never, option.Value);
            Assert.Equal(NotificationOption2.Silent, option.Notification);

            option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("false:error", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
            Assert.Equal(ExpressionBodyPreference.Never, option.Value);
            Assert.Equal(NotificationOption2.Error, option.Notification);

            option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("true:warning", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
            Assert.Equal(ExpressionBodyPreference.WhenPossible, option.Value);
            Assert.Equal(NotificationOption2.Warning, option.Notification);

            option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("when_on_single_line:suggestion", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
            Assert.Equal(ExpressionBodyPreference.WhenOnSingleLine, option.Value);
            Assert.Equal(NotificationOption2.Suggestion, option.Notification);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            var code = @"
class C
{
    void Bar() => Bar();

    {|IDE0022:void Goo()
    {
        Bar();
    }|}
}";
            var fixedCode = @"
class C
{
    void Bar() => Bar();

    void Goo() => Bar();
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody2()
        {
            var code = @"
class C
{
    int Bar() => 0;

    {|IDE0022:int Goo()
    {
        return Bar();
    }|}
}";
            var fixedCode = @"
class C
{
    int Bar() => 0;

    int Goo() => Bar();
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            var code = @"
using System;

class C
{
    {|IDE0022:int Goo()
    {
        throw new NotImplementedException();
    }|}
}";
            var fixedCode = @"
using System;

class C
{
    int Goo() => throw new NotImplementedException();
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            var code = @"
using System;

class C
{
    {|IDE0022:int Goo()
    {
        throw new NotImplementedException(); // comment
    }|}
}";
            var fixedCode = @"
using System;

class C
{
    int Goo() => throw new NotImplementedException(); // comment
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            var code = @"
class C
{
    void Bar() { }

    {|IDE0022:void Goo() => Bar();|}
}";
            var fixedCode = @"
class C
{
    void Bar() { }

    void Goo()
    {
        Bar();
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody2()
        {
            var code = @"
class C
{
    int Bar() { return 0; }

    {|IDE0022:int Goo() => Bar();|}
}";
            var fixedCode = @"
class C
{
    int Bar() { return 0; }

    int Goo()
    {
        return Bar();
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody3()
        {
            var code = @"
using System;

class C
{
    {|IDE0022:int Goo() => throw new NotImplementedException();|}
}";
            var fixedCode = @"
using System;

class C
{
    int Goo()
    {
        throw new NotImplementedException();
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody4()
        {
            var code = @"
using System;

class C
{
    {|IDE0022:int Goo() => throw new NotImplementedException();|} // comment
}";
            var fixedCode = @"
using System;

class C
{
    int Goo()
    {
        throw new NotImplementedException(); // comment
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments1()
        {
            var code = @"
class C
{
    void Bar() => Bar();

    {|IDE0022:void Goo()
    {
        // Comment
        Bar();
    }|}
}";
            var fixedCode = @"
class C
{
    void Bar() => Bar();

    void Goo() =>
        // Comment
        Bar();
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments2()
        {
            var code = @"
class C
{
    int Bar() => 0;

    {|IDE0022:int Goo()
    {
        // Comment
        return Bar();
    }|}
}";
            var fixedCode = @"
class C
{
    int Bar() => 0;

    int Goo() =>
        // Comment
        Bar();
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments3()
        {
            var code = @"
using System;

class C
{
    Exception Bar() => new Exception();

    {|IDE0022:void Goo()
    {
        // Comment
        throw Bar();
    }|}
}";
            var fixedCode = @"
using System;

class C
{
    Exception Bar() => new Exception();

    void Goo() =>
        // Comment
        throw Bar();
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments4()
        {
            var code = @"
class C
{
    void Bar() => Bar();

    {|IDE0022:void Goo()
    {
        Bar(); // Comment
    }|}
}";
            var fixedCode = @"
class C
{
    void Bar() => Bar();

    void Goo() => Bar(); // Comment
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments5()
        {
            var code = @"
class C
{
    int Bar() => 0;

    {|IDE0022:int Goo()
    {
        return Bar(); // Comment
    }|}
}";
            var fixedCode = @"
class C
{
    int Bar() => 0;

    int Goo() => Bar(); // Comment
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestComments6()
        {
            var code = @"
using System;

class C
{
    Exception Bar() => new Exception();

    {|IDE0022:void Goo()
    {
        throw Bar(); // Comment
    }|}
}";
            var fixedCode = @"
using System;

class C
{
    Exception Bar() => new Exception();

    void Goo() => throw Bar(); // Comment
}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [WorkItem(17120, "https://github.com/dotnet/roslyn/issues/17120")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDirectives1()
        {
            var code = @"
#define DEBUG
using System;

class Program
{
    {|IDE0022:void Method()
    {
#if DEBUG
        Console.WriteLine();
#endif
    }|}
}";
            var fixedCode = @"
#define DEBUG
using System;

class Program
{
    void Method() =>
#if DEBUG
        Console.WriteLine();
#endif

}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [WorkItem(17120, "https://github.com/dotnet/roslyn/issues/17120")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDirectives2()
        {
            var code = @"
#define DEBUG
using System;

class Program
{
    {|IDE0022:void Method()
    {
#if DEBUG
        Console.WriteLine(0);
#else
        Console.WriteLine(1);
#endif
    }|}
}";
            var fixedCode = @"
#define DEBUG
using System;

class Program
{
    void Method() =>
#if DEBUG
        Console.WriteLine(0);
#else
        Console.WriteLine(1);
#endif

}";
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [WorkItem(20362, "https://github.com/dotnet/roslyn/issues/20362")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp6()
        {
            var code = @"
using System;
class C
{
    {|IDE0022:void M() {|CS8026:=> {|CS8026:throw new NotImplementedException()|}|};|}
}";
            var fixedCode = @"
using System;
class C
{
    void M()
    {
        throw new NotImplementedException();
    }
}";
            await TestWithUseExpressionBody(code, fixedCode, LanguageVersion.CSharp5);
        }

        [WorkItem(20352, "https://github.com/dotnet/roslyn/issues/20352")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDoNotOfferToConvertToBlockIfExpressionBodyPreferredIfCSharp6()
        {
            var code = @"
using System;
class C
{
    int M() => 0;
}";
            await TestWithUseExpressionBody(code, code, LanguageVersion.CSharp6);
        }

        [WorkItem(20352, "https://github.com/dotnet/roslyn/issues/20352")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferToConvertToExpressionIfCSharp6()
        {
            var code = @"
using System;
class C
{
    {|IDE0022:int M() { return 0; }|}
}";
            var fixedCode = @"
using System;
class C
{
    int M() => 0;
}";
            await TestWithUseExpressionBody(code, fixedCode, LanguageVersion.CSharp6);
        }

        [WorkItem(20352, "https://github.com/dotnet/roslyn/issues/20352")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDoNotOfferToConvertToExpressionInCSharp6IfThrowExpression()
        {
            var code = @"
using System;
class C
{
    // throw expressions not supported in C# 6.
    void M() { throw new Exception(); }
}";
            await TestWithUseExpressionBody(code, code, LanguageVersion.CSharp6);
        }

        [WorkItem(20362, "https://github.com/dotnet/roslyn/issues/20362")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp6_FixAll()
        {
            var code = @"
using System;
class C
{
    {|IDE0022:void M() => {|CS8059:throw new NotImplementedException()|};|}
    {|IDE0022:void M(int i) => {|CS8059:throw new NotImplementedException()|};|}
    int M(bool b) => 0;
}";
            var fixedCode = @"
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
}";
            await TestWithUseExpressionBody(code, fixedCode, LanguageVersion.CSharp6);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync1()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    {|IDE0022:async Task Goo() => await Bar();|}

    Task Bar() { return Task.CompletedTask; }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    async Task Goo()
    {
        await Bar();
    }

    Task Bar() { return Task.CompletedTask; }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync2()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    {|IDE0022:async void Goo() => await Bar();|}

    Task Bar() { return Task.CompletedTask; }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    async void Goo()
    {
        await Bar();
    }

    Task Bar() { return Task.CompletedTask; }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync3()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    {|IDE0022:async void Goo() => await Bar();|}

    Task Bar() { return Task.CompletedTask; }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    async void Goo()
    {
        await Bar();
    }

    Task Bar() { return Task.CompletedTask; }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync4()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    {|IDE0022:async ValueTask Goo() => await Bar();|}

    Task Bar() { return Task.CompletedTask; }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    async ValueTask Goo()
    {
        await Bar();
    }

    Task Bar() { return Task.CompletedTask; }
}";
            await TestWithUseBlockBody(code, fixedCode, ReferenceAssemblies.NetStandard.NetStandard21);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync5()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    {|IDE0022:async Task<int> Goo() => await Bar();|}

    Task<int> Bar() { return Task.FromResult(0); }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    async Task<int> Goo()
    {
        return await Bar();
    }

    Task<int> Bar() { return Task.FromResult(0); }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [WorkItem(25202, "https://github.com/dotnet/roslyn/issues/25202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyAsync6()
        {
            var code = @"
using System.Threading.Tasks;

class C
{
    {|IDE0022:Task Goo() => Bar();|}

    Task Bar() { return Task.CompletedTask; }
}";
            var fixedCode = @"
using System.Threading.Tasks;

class C
{
    Task Goo()
    {
        return Bar();
    }

    Task Bar() { return Task.CompletedTask; }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        [WorkItem(53532, "https://github.com/dotnet/roslyn/issues/53532")]
        public async Task TestUseBlockBodyTrivia1()
        {
            var code = @"
using System;
class C
{
    {|IDE0022:void M()
        // Test
        => Console.WriteLine();|}
}";
            var fixedCode = @"
using System;
class C
{
    void M()
    {
        // Test
        Console.WriteLine();
    }
}";
            await TestWithUseBlockBody(code, fixedCode);
        }
    }
}
