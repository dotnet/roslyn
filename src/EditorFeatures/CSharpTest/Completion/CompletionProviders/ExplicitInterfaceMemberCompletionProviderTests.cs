﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class ExplicitInterfaceMemberCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ExplicitInterfaceMemberCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new ExplicitInterfaceMemberCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExplicitInterfaceMember()
        {
            var markup = @"
interface IFoo
{
    void Foo();
    void Foo(int x);
    int Prop { get; }
}

class Bar : IFoo
{
     void IFoo.$$
}";

            await VerifyItemExistsAsync(markup, "Foo()");
            await VerifyItemExistsAsync(markup, "Foo(int x)");
            await VerifyItemExistsAsync(markup, "Prop");
        }

        [WorkItem(709988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709988")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitOnNotParen()
        {
            var markup = @"
interface IFoo
{
    void Foo();
}

class Bar : IFoo
{
     void IFoo.$$
}";

            var expected = @"
interface IFoo
{
    void Foo();
}

class Bar : IFoo
{
     void IFoo.Foo()
}";

            await VerifyProviderCommitAsync(markup, "Foo()", expected, null, "");
        }

        [WorkItem(709988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709988")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitOnParen()
        {
            var markup = @"
interface IFoo
{
    void Foo();
}

class Bar : IFoo
{
     void IFoo.$$
}";

            var expected = @"
interface IFoo
{
    void Foo();
}

class Bar : IFoo
{
     void IFoo.Foo(
}";

            await VerifyProviderCommitAsync(markup, "Foo()", expected, '(', "");
        }
    }
}
