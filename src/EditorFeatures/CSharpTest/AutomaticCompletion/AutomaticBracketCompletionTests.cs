// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition.Hosting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticBracketCompletionTests : AbstractAutomaticBraceCompletionTests
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
        public async Task Attribute_TopLevel()
        {
            using (var session = await CreateSessionAsync("$$"))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Attribute_TopLevel2()
        {
            using (var session = await CreateSessionAsync("using System;$$"))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_String()
        {
            var code = @"class C
{
    string s = ""$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_String2()
        {
            var code = @"class C
{
    string s = @""
$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_Comment()
        {
            var code = @"class C
{
    //$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_Comment2()
        {
            var code = @"class C
{
    /* $$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_Comment3()
        {
            var code = @"class C
{
    /// $$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_Comment4()
        {
            var code = @"class C
{
    /** $$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task MultiLine_Comment()
        {
            var code = @"class C
{
    void Method()
    {
        /* */$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task MultiLine_DocComment()
        {
            var code = @"class C
{
    void Method()
    {
        /** */$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
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
        public async Task Attribute_OpenBracket()
        {
            var code = @"$$
class C { }";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Attribute_OpenBracket_Delete()
        {
            var code = @"$$
class C { }";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Attribute_OpenBracket_Tab()
        {
            var code = @"$$
class C { }";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Attribute_OpenBracket_CloseBracket()
        {
            var code = @"$$
class C { }";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Array_Multiple_Invalid()
        {
            var code = @"class C 
{
    int [$$]
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
    int [] i = new int [arr$$]
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
            }
        }

        internal async Task<Holder> CreateSessionAsync(string code)
        {
            return CreateSession(
                await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(code),
                BraceCompletionSessionProvider.Bracket.OpenCharacter, BraceCompletionSessionProvider.Bracket.CloseCharacter);
        }
    }
}
