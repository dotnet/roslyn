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
    public class CSharpPropSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "prop";

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropSnippetMissingInNamespace()
        {
            var markupBeforeCommit =
@"namespace Namespace
{
    $$
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropSnippetMissingInFilescopedNamespace()
        {
            var markupBeforeCommit =
@"namespace Namespace;

$$";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropSnippetMissingInTopLevelContext()
        {
            var markupBeforeCommit =
@"System.Console.WriteLine();
$$";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertPropSnippetInClassTest()
        {
            var markupBeforeCommit =
@"class MyClass
{
    $$
}";

            var expectedCodeAfterCommit =
@"class MyClass
{
    public int MyProperty { get; set; }$$
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertPropSnippetInRecordTest()
        {
            var markupBeforeCommit =
@"record MyRecord
{
    $$
}";

            var expectedCodeAfterCommit =
@"record MyRecord
{
    public int MyProperty { get; set; }$$
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertPropSnippetInStructTest()
        {
            var markupBeforeCommit =
@"struct MyStruct
{
    $$
}";

            var expectedCodeAfterCommit =
@"struct MyStruct
{
    public int MyProperty { get; set; }$$
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertPropSnippetInInterfaceTest()
        {
            var markupBeforeCommit =
@"interface MyInterface
{
    $$
}";

            var expectedCodeAfterCommit =
@"interface MyInterface
{
    public int MyProperty { get; set; }$$
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertPropSnippetNamingTest()
        {
            var markupBeforeCommit =
@"class MyClass
{
    public int MyProperty { get; set; }
    $$
}";

            var expectedCodeAfterCommit =
@"class MyClass
{
    public int MyProperty { get; set; }
    public int MyProperty1 { get; set; }$$
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoPropSnippetInEnumTest()
        {
            var markupBeforeCommit =
@"enum MyEnum
{
    $$
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoPropSnippetInMethodTest()
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
        public async Task NoPropSnippetInConstructorTest()
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
