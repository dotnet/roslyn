// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.BraceCompletion.AbstractBraceCompletionService;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticLiteralCompletionTests : AbstractAutomaticBraceCompletionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Creation()
        {
            using var session = CreateSessionSingleQuote("$$");
            Assert.NotNull(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public void String_TopLevel()
        {
            using var session = CreateSessionDoubleQuote("$$");
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public void VerbatimString_TopLevel()
        {
            using var session = CreateSessionDoubleQuote("@$$");
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public void Char_TopLevel()
        {
            using var session = CreateSessionSingleQuote("$$");
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public void String_TopLevel2()
        {
            using var session = CreateSessionDoubleQuote("using System;$$");
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public void VerbatimString_TopLevel2()
        {
            using var session = CreateSessionDoubleQuote("using System;@$$");
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void String_String()
        {
            var code = @"class C
{
    void Method()
    {
        var s = """"$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session, expectValidSession: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void String_VerbatimString()
        {
            var code = @"class C
{
    void Method()
    {
        var s = """"@$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void String_Char()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @""""$$
    }
}";
            using var session = CreateSessionSingleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method_String()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method_String_Delete()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method_String_Tab()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method_String_Quotation()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimMethod_String()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimMethod_String_Delete()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimMethod_String_Tab()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimMethod_String_Quotation()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method_InterpolatedString()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $[||]$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method_InterpolatedString_Delete()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $[||]$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method_InterpolatedString_Tab()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $[||]$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method_InterpolatedString_Quotation()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $[||]$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimMethod_InterpolatedString()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $@$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimMethod_InterpolatedString_Delete()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $@$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimMethod_InterpolatedString_Tab()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $@$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimMethod_InterpolatedString_Quotation()
        {
            var code = @"class C
{
    void Method()
    {
        var s = $@$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Preprocessor1()
        {
            var code = @"class C
{
    void Method()
    {
#line $$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Preprocessor2()
        {
            var code = @"class C
{
    void Method()
    {
#line $$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Preprocessor3()
        {
            var code = @"class C
{
    void Method()
    {
#line $$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WorkItem(546047, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546047")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimStringDoubleQuote()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @""""$$
    }
}";
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session, expectValidSession: false);
        }

        internal static Holder CreateSessionSingleQuote(string code)
        {
            return CreateSession(
                TestWorkspace.CreateCSharp(code),
                SingleQuote.OpenCharacter, SingleQuote.CloseCharacter);
        }

        internal static Holder CreateSessionDoubleQuote(string code)
        {
            return CreateSession(
                TestWorkspace.CreateCSharp(code),
                DoubleQuote.OpenCharacter, DoubleQuote.CloseCharacter);
        }
    }
}
