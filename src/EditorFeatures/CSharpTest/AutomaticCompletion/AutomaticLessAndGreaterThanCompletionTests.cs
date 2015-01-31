// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition.Hosting;
using Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticLessAndGreaterThanCompletionTests : AbstractAutomaticBraceCompletionTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Creation()
        {
            using (var session = CreateSession("$$"))
            {
                Assert.NotNull(session);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_TopLevel()
        {
            using (var session = CreateSession("$$"))
            {
                Assert.NotNull(session);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_TopLevel2()
        {
            using (var session = CreateSession("using System;$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class_TypeParameter()
        {
            var code = @"class C$$";

            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method_TypeParameter()
        {
            var code = @"class C
{
    void Method$$
}";

            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class_TypeParameter_Delete()
        {
            var code = @"class C$$";

            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class_TypeParameter_Tab()
        {
            var code = @"class C$$";

            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class_TypeParameter_GreatherThan()
        {
            var code = @"class C$$";

            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Multiple_Invalid()
        {
            var code = @"class C<$$>";

            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Multiple_Nested()
        {
            var code = @"class C<T>
{
    C<C$$>
}";

            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void TypeArgument_Invalid()
        {
            var code = @"class C
{
    void Method()
    {
        List$$
    }
}";
            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void TypeArgument_Invalid2()
        {
            var code = @"class C
{
    void Method()
    {
        var i = 1;
        var b = i $$
    }
}";
            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void TypeArgument2()
        {
            var code = @"class C
{
    void Method()
    {
        var a = new List$$
    }
}";
            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void TypeArgument3()
        {
            var code = @"class C
{
    void Method()
    {
        var a = typeof(List$$
    }
}";
            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WorkItem(531637)]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void TypeParameterReturnType()
        {
            var code = @"class C
{
    List$$
}";
            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                Type(session.Session, "int");
                CheckOverType(session.Session);
            }
        }

        [WorkItem(531637)]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void TypeParameterInDecl()
        {
            var code = @"class C
{
    void List$$
}";
            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                Type(session.Session, "T");
                CheckOverType(session.Session);
            }
        }

        [WorkItem(531637)]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void TypeParameterInDeclWithAsync()
        {
            var code = @"class C
{
    async Task$$
}";
            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                Type(session.Session, "int");
                CheckOverType(session.Session);
            }
        }

        [WorkItem(530864)]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void TypeArgumentWithUsing()
        {
            var code = @"using System.Collections.Generic;

class C
{
    void Test()
    {
        List$$
    }
}";
            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                Type(session.Session, "int");
                CheckOverType(session.Session);
            }
        }

        [WorkItem(530864)]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void TypeArgumentNoUsing()
        {
            var code = @"class C
{
    void Test()
    {
        List$$
    }
}";
            using (var session = CreateSession(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        internal Holder CreateSession(string code)
        {
            return CreateSession(
                CSharpWorkspaceFactory.CreateWorkspaceFromFile(code),
                BraceCompletionSessionProvider.LessAndGreaterThan.OpenCharacter, BraceCompletionSessionProvider.LessAndGreaterThan.CloseCharacter);
        }
    }
}
