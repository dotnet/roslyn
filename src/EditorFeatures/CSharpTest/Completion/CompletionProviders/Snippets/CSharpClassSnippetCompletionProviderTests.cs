// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    public class CSharpClassSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "class";

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertClassSnippetInNamespaceTest()
        {
            var markupBeforeCommit =
@"namespace Namespace
{
    $$
}";

            var expectedCodeAfterCommit =
@"namespace Namespace
{
    class MyClass
    {$$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertClassSnippetInFileScopedNamespaceTest()
        {
            var markupBeforeCommit =
@"namespace Namespace;

$$";

            var expectedCodeAfterCommit =
@"namespace Namespace;

class MyClass
{$$
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertClassSnippetTest()
        {
            var markupBeforeCommit =
@"$$";

            var expectedCodeAfterCommit =
@"class MyClass
{$$
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertClassSnippetInClassTest()
        {
            var markupBeforeCommit =
@"class MyClass
{
    $$
}";

            var expectedCodeAfterCommit =
@"class MyClass
{
    class MyClass1
    {$$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertClassSnippetInRecordTest()
        {
            var markupBeforeCommit =
@"record MyRecord
{
    $$
}";

            var expectedCodeAfterCommit =
@"record MyRecord
{
    class MyClass
    {$$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertClassSnippetInStructTest()
        {
            var markupBeforeCommit =
@"struct MyStruct
{
    $$
}";

            var expectedCodeAfterCommit =
@"struct MyStruct
{
    class MyClass
    {$$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertClassSnippetInInterfaceTest()
        {
            var markupBeforeCommit =
@"interface MyInterface
{
    $$
}";

            var expectedCodeAfterCommit =
@"interface MyInterface
{
    class MyClass
    {$$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoClassSnippetInEnumTest()
        {
            var markupBeforeCommit =
@"enum MyEnum
{
    $$
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoClassSnippetInMethodTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        $$
    }
}";
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoClassSnippetInConstructorTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public Program()
    {
        $$
    }
}";
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }
    }
}
