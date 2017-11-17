// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
interface IGoo
{
    void Goo();
    void Goo(int x);
    int Prop { get; }
}

class Bar : IGoo
{
     void IGoo.$$
}";

            await VerifyItemExistsAsync(markup, "Goo()");
            await VerifyItemExistsAsync(markup, "Goo(int x)");
            await VerifyItemExistsAsync(markup, "Prop");
        }

        [WorkItem(709988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709988")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitOnNotParen()
        {
            var markup = @"
interface IGoo
{
    void Goo();
}

class Bar : IGoo
{
     void IGoo.$$
}";

            var expected = @"
interface IGoo
{
    void Goo();
}

class Bar : IGoo
{
     void IGoo.Goo()
}";

            await VerifyProviderCommitAsync(markup, "Goo()", expected, null, "");
        }

        [WorkItem(709988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709988")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitOnParen()
        {
            var markup = @"
interface IGoo
{
    void Goo();
}

class Bar : IGoo
{
     void IGoo.$$
}";

            var expected = @"
interface IGoo
{
    void Goo();
}

class Bar : IGoo
{
     void IGoo.Goo(
}";

            await VerifyProviderCommitAsync(markup, "Goo()", expected, '(', "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(19947, "https://github.com/dotnet/roslyn/issues/19947")]
        public async Task ExplicitInterfaceMemberCompletionContainsOnlyValidValues()
        {
            var markup = @"
interface I1
{
    void Goo();
}

interface I2 : I1
{
    void Goo2();
    int Prop { get; }
}

class Bar : I2
{
     void I2.$$
}";

            await VerifyItemIsAbsentAsync(markup, "Equals(object obj)");
            await VerifyItemIsAbsentAsync(markup, "Goo()");
            await VerifyItemIsAbsentAsync(markup, "GetHashCode()");
            await VerifyItemIsAbsentAsync(markup, "GetType()");
            await VerifyItemIsAbsentAsync(markup, "ToString()");

            await VerifyItemExistsAsync(markup, "Goo2()");
            await VerifyItemExistsAsync(markup, "Prop");
        }
    }
}
