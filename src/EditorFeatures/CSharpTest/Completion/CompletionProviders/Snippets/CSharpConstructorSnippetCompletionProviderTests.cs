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
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class CSharpConstructorSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "ctor";

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstructorSnippetMissingInNamespace()
        {
            var markupBeforeCommit =
@"namespace Namespace
{
    $$
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstructorSnippetMissingInFilescopedNamespace()
        {
            var markupBeforeCommit =
@"namespace Namespace;

$$";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstructorSnippetMissingInTopLevelContext()
        {
            var markupBeforeCommit =
@"System.Console.WriteLine();
$$";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetInClassTest()
        {
            var markupBeforeCommit =
@"class MyClass
{
    $$
}";

            var expectedCodeAfterCommit =
@"class MyClass
{
    public MyClass()
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetInAbstractClassTest()
        {
            var markupBeforeCommit =
@"abstract class MyClass
{
    $$
}";

            var expectedCodeAfterCommit =
@"abstract class MyClass
{
    public MyClass()
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetInStructTest()
        {
            var markupBeforeCommit =
@"struct MyStruct
{
    $$
}";

            var expectedCodeAfterCommit =
@"struct MyStruct
{
    public MyStruct()
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetInRecordTest()
        {
            var markupBeforeCommit =
@"record MyRecord
{
    $$
}";

            var expectedCodeAfterCommit =
@"record MyRecord
{
    public MyRecord()
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstructorSnippetMissingInInterface()
        {
            var markupBeforeCommit =
@"interface MyInterface
{
    $$
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetInNestedClassTest()
        {
            var markupBeforeCommit =
@"class MyClass
{
    class MyClass1
    {
        $$
    }
}";

            var expectedCodeAfterCommit =
@"class MyClass
{
    class MyClass1
    {
        public MyClass1()
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("class")]
        [InlineData("enum")]
        [InlineData("interface")]
        [InlineData("record")]
        [InlineData("struct")]
        [InlineData("record class")]
        [InlineData("record struct")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetHasNestedTypeTest(string keyword)
        {
            var markupBeforeCommit =
$$"""
class Outer
{
    $$
    {{keyword}} Inner
    {
    }
}
""";

            var expectedCodeAfterCommit =
$$"""
class Outer
{
    public Outer()
    {
        $$
    }
    {{keyword}} Inner
    {
    }
}
""";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetHasNestedClassGluedOuterTest()
        {
            var markupBeforeCommit_GluedToOuter =
@"class Outer
{$$
    class Inner
    {
    }
}";
            // The position ($$) is in a strange position because code does not format after insertion.
            var expectedCodeAfterCommit_GluedToOuter =
@"class Outer
{    public Outer()
    {
        
$$    }
    class Inner
    {
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit_GluedToOuter, ItemToCommit, expectedCodeAfterCommit_GluedToOuter);
        }

        [WpfTheory]
        [InlineData("class")]
        [InlineData("record")]
        [InlineData("struct")]
        [InlineData("record class")]
        [InlineData("record struct")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetInConstructableTypeTest(string keyword)
        {
            var markupBeforeCommit =
$$"""
{{keyword}} MyName
{
    $$
}
""";

            var expectedCodeAfterCommit =
$$"""
{{keyword}} MyName
{
    public MyName()
    {
        $$
    }
}
""";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetInStaticClassTest()
        {
            var markupBeforeCommit =
$$"""
static class MyClass
{
    $$
}
""";
            var expectedCodeAfterCommit =
$$"""
static class MyClass
{
    public MyClass()
    {
        $$
    }
}
""";
            // Current CSharpConstructorSnippetProvider implementation inserts a constructor even for static classes.
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("enum")]
        [InlineData("interface")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetInNotConstructableTypeTest(string keyword)
        {
            var markupBeforeCommit =
$$"""
{{keyword}} MyName
{
    $$
}
""";
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("class")]
        [InlineData("record")]
        [InlineData("struct")]
        [InlineData("record class")]
        [InlineData("record struct")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetBraceNotClosed(string keyword)
        {
            var markupBeforeCommit =
$$"""
namespace N
{
    {{keyword}} Outer
    {
        $$
        int i;
""";

            var expectedCodeAfterCommit =
$$"""
namespace N
{
    {{keyword}} Outer
    {
        public Outer()
        {
            $$
        }
        int i;
""";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("class")]
        [InlineData("record")]
        [InlineData("struct")]
        [InlineData("record class")]
        [InlineData("record struct")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetBraceNotClosedAndEndOfFile(string keyword)
        {
            var markupBeforeCommit =
$$"""
namespace N
{
    {{keyword}} Outer
    {
        $$
""";

            var expectedCodeAfterCommit =
$$"""
namespace N
{
    {{keyword}} Outer
    {
        public Outer()
        {
            $$
        }
""";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("class")]
        [InlineData("record")]
        [InlineData("struct")]
        [InlineData("record class")]
        [InlineData("record struct")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetBraceNotClosedAndEndOfFileAndNested(string keyword)
        {
            var markupBeforeCommit =
$$"""
namespace N
{
    {{keyword}} Outer
    {
        class Inner{}
        $$
""";

            var expectedCodeAfterCommit =
$$"""
namespace N
{
    {{keyword}} Outer
    {
        class Inner{}
        public Outer()
        {
            $$
        }
""";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConstructorSnippetInGenericClassTest()
        {
            var markupBeforeCommit =
@"class MyClass<T> : BaseClass
{
    $$
}";

            var expectedCodeAfterCommit =
@"class MyClass<T> : BaseClass
{
    public MyClass()
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
