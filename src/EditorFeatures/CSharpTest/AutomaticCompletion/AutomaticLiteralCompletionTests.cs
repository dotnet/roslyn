// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public void Creation()
        {
            using (var session = CreateSessionSingleQuote("$$"))
            {
                Assert.NotNull(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void String_TopLevel()
        {
            using (var session = CreateSessionDoubleQuote("$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimString_TopLevel()
        {
            using (var session = CreateSessionDoubleQuote("@$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Char_TopLevel()
        {
            using (var session = CreateSessionSingleQuote("$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void String_TopLevel2()
        {
            using (var session = CreateSessionDoubleQuote("using System;$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VerbatimString_TopLevel2()
        {
            using (var session = CreateSessionDoubleQuote("using System;@$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
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
            using (var session = CreateSessionSingleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        public void Preprocessor2()
        {
            var code = @"class C
{
    void Method()
    {
#line $$
    }
}";
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        public void Preprocessor3()
        {
            var code = @"class C
{
    void Method()
    {
#line $$
    }
}";
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [WorkItem(546047)]
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
            using (var session = CreateSessionDoubleQuote(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        internal Holder CreateSessionSingleQuote(string code)
        {
            return CreateSession(
                CSharpWorkspaceFactory.CreateWorkspaceFromFile(code),
                BraceCompletionSessionProvider.SingleQuote.OpenCharacter, BraceCompletionSessionProvider.SingleQuote.CloseCharacter);
        }

        internal Holder CreateSessionDoubleQuote(string code)
        {
            return CreateSession(
                CSharpWorkspaceFactory.CreateWorkspaceFromFile(code),
                BraceCompletionSessionProvider.DoubleQuote.OpenCharacter, BraceCompletionSessionProvider.DoubleQuote.CloseCharacter);
        }
    }
}
