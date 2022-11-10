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
    public class CSharpStructSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "struct";

        [WpfFact]
        public async Task InsertStructSnippetInNamespaceTest()
        {
            var markupBeforeCommit =
@"namespace Namespace
{
    $$
}";

            var expectedCodeAfterCommit =
@"namespace Namespace
{
    struct MyStruct
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetInFileScopedNamespaceTest()
        {
            var markupBeforeCommit =
@"namespace Namespace;

$$";

            var expectedCodeAfterCommit =
@"namespace Namespace;

struct MyStruct
{
    $$
}"
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetTest()
        {
            var markupBeforeCommit =
@"$$";

            var expectedCodeAfterCommit =
@"struct MyStruct
{
    $$
}"
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructTopLevelSnippetTest()
        {
            var markupBeforeCommit =
@"System.Console.WriteLine();
$$";

            var expectedCodeAfterCommit =
@"System.Console.WriteLine();
struct MyStruct
{
    $$
}"
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetInClassTest()
        {
            var markupBeforeCommit =
@"struct MyClass
{
    $$
}"
;

            var expectedCodeAfterCommit =
@"struct MyClass
{
    struct MyStruct
    {
        $$
    }
}"
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetInRecordTest()
        {
            var markupBeforeCommit =
@"record MyRecord
{
    $$
}";

            var expectedCodeAfterCommit =
@"record MyRecord
{
    struct MyStruct
    {
        $$
    }
}"
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetInStructTest()
        {
            var markupBeforeCommit =
@"struct MyStruct
{
    $$
}";

            var expectedCodeAfterCommit =
@"struct MyStruct
{
    struct MyStruct1
    {
        $$
    }
}"
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetInInterfaceTest()
        {
            var markupBeforeCommit =
@"interface MyInterface
{
    $$
}";

            var expectedCodeAfterCommit =
@"interface MyInterface
{
    struct MyStruct
    {
        $$
    }
}"
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetWithModifiersTest()
        {
            var markupBeforeCommit =
                $@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
    <Document FilePath=""/0/Test0.cs"">
$$
</Document>
<AnalyzerConfigDocument FilePath=""/.editorconfig"">
root = true
 
[*]
# IDE0008: Use explicit type
dotnet_style_require_accessibility_modifiers = always
    </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            var expectedCodeAfterCommit =
                $@"
public struct MyStruct
{{
    $$
}}
";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoStructSnippetInEnumTest()
        {
            var markupBeforeCommit =
@"enum MyEnum
{
    $$
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoStructSnippetInMethodTest()
        {
            var markupBeforeCommit =
@"struct Program
{
    public void Method()
    {
        $$
    }
}"
;
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoStructSnippetInConstructorTest()
        {
            var markupBeforeCommit =
@"struct Program
{
    public Program()
    {
        $$
    }
}"
;
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }
    }
}
