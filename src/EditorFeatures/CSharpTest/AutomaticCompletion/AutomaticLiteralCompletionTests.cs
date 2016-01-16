// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticLiteralCompletionTests : AbstractAutomaticBraceCompletionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Creation()
        {
            using (var session = await CreateSessionSingleQuoteAsync("$$"))
            {
                Assert.NotNull(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task String_TopLevel()
        {
            using (var session = await CreateSessionDoubleQuoteAsync("$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimString_TopLevel()
        {
            using (var session = await CreateSessionDoubleQuoteAsync("@$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Char_TopLevel()
        {
            using (var session = await CreateSessionSingleQuoteAsync("$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task String_TopLevel2()
        {
            using (var session = await CreateSessionDoubleQuoteAsync("using System;$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimString_TopLevel2()
        {
            using (var session = await CreateSessionDoubleQuoteAsync("using System;@$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task String_String()
        {
            var code = @"class C
{
    void Method()
    {
        var s = """"$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task String_VerbatimString()
        {
            var code = @"class C
{
    void Method()
    {
        var s = """"@$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task String_Char()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @""""$$
    }
}";
            using (var session = await CreateSessionSingleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method_String()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method_String_Delete()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method_String_Tab()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method_String_Quotation()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimMethod_String()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimMethod_String_Delete()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimMethod_String_Tab()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimMethod_String_Quotation()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method_InterpolatedString()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $[||]$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method_InterpolatedString_Delete()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $[||]$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method_InterpolatedString_Tab()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $[||]$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method_InterpolatedString_Quotation()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $[||]$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimMethod_InterpolatedString()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $@$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimMethod_InterpolatedString_Delete()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $@$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimMethod_InterpolatedString_Tab()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $@$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimMethod_InterpolatedString_Quotation()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $@$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Preprocessor1()
        {
            var code = @"class C
{
    void Method()
    {
#line $$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        public async Task Preprocessor2()
        {
            var code = @"class C
{
    void Method()
    {
#line $$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        public async Task Preprocessor3()
        {
            var code = @"class C
{
    void Method()
    {
#line $$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [WorkItem(546047)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VerbatimStringDoubleQuote()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @""""$$
    }
}";
            using (var session = await CreateSessionDoubleQuoteAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        internal async Task<Holder> CreateSessionSingleQuoteAsync(string code)
        {
            return CreateSession(
                await TestWorkspace.CreateCSharpAsync(code),
                BraceCompletionSessionProvider.SingleQuote.OpenCharacter, BraceCompletionSessionProvider.SingleQuote.CloseCharacter);
        }

        internal async Task<Holder> CreateSessionDoubleQuoteAsync(string code)
        {
            return CreateSession(
                await TestWorkspace.CreateCSharpAsync(code),
                BraceCompletionSessionProvider.DoubleQuote.OpenCharacter, BraceCompletionSessionProvider.DoubleQuote.CloseCharacter);
        }
    }
}
