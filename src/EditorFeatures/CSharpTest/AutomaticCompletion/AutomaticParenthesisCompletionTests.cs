// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticParenthesisCompletionTests : AbstractAutomaticBraceCompletionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Creation()
        {
            using (var session = await CreateSessionAsync("$$"))
            {
                Assert.NotNull(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task String1()
        {
            var code = @"class C
{
    void Method()
    {
        var s = """"$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task String2()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @""""$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ParameterList_OpenParenthesis()
        {
            var code = @"class C
{
    void Method$$
}";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ParameterList_OpenParenthesis_Delete()
        {
            var code = @"class C
{
    void Method$$
}";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ParameterList_OpenParenthesis_Tab()
        {
            var code = @"class C
{
    void Method$$
}";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ParameterList_OpenParenthesis_CloseParenthesis()
        {
            var code = @"class C
{
    void Method$$
}";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Argument()
        {
            var code = @"class C 
{
    void Method()
    {
        Method$$
    }
}";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Argument_Invalid()
        {
            var code = @"class C 
{
    void Method()
    {
        Method($$)
    }
}";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Array_Nested()
        {
            var code = @"class C
{
    int Method(int i)
    {
        Method(Method$$)
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WorkItem(546337)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task OpenParenthesisWithExistingCloseParen()
        {
            var code = @"class A
{
    public A(int a, int b) { }

    public static A Create()
    {
        return new A$$
            0, 0);
    }
}
";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        internal async Task<Holder> CreateSessionAsync(string code)
        {
            return CreateSession(
                await CSharpWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync(code),
                BraceCompletionSessionProvider.Parenthesis.OpenCharacter, BraceCompletionSessionProvider.Parenthesis.CloseCharacter);
        }
    }
}
