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
    public class CSharpInterfaceSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "interface";

        [WpfFact]
        public async Task InsertInterfaceSnippetInNamespaceTest()
        {
            var markupBeforeCommit =
@"namespace Namespace
{
    $$
}";

            var expectedCodeAfterCommit =
@"namespace Namespace
{
    interface MyInterface
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInterfaceSnippetInFileScopedNamespaceTest()
        {
            var markupBeforeCommit =
@"namespace Namespace;

$$";

            var expectedCodeAfterCommit =
@"namespace Namespace;

interface MyInterface
{
    $$
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInterfaceSnippetTest()
        {
            var markupBeforeCommit =
@"$$";

            var expectedCodeAfterCommit =
@"interface MyInterface
{
    $$
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInterfaceTopLevelSnippetTest()
        {
            var markupBeforeCommit =
@"System.Console.WriteLine();
$$";

            var expectedCodeAfterCommit =
@"System.Console.WriteLine();
interface MyInterface
{
    $$
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInterfaceSnippetInClassTest()
        {
            var markupBeforeCommit =
@"class MyClass
{
    $$
}";

            var expectedCodeAfterCommit =
@"class MyClass
{
    interface MyInterface
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInterfaceSnippetInRecordTest()
        {
            var markupBeforeCommit =
@"record MyRecord
{
    $$
}";

            var expectedCodeAfterCommit =
@"record MyRecord
{
    interface MyInterface
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInterfaceSnippetInStructTest()
        {
            var markupBeforeCommit =
@"struct MyStruct
{
    $$
}";

            var expectedCodeAfterCommit =
@"struct MyStruct
{
    interface MyInterface
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInterfaceSnippetInInterfaceTest()
        {
            var markupBeforeCommit =
@"interface MyInterface
{
    $$
}";

            var expectedCodeAfterCommit =
@"interface MyInterface
{
    interface MyInterface1
    {
        $$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInterfaceSnippetWithModifiersTest()
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
public interface MyInterface
{{
    $$
}}
";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoInterfaceSnippetInEnumTest()
        {
            var markupBeforeCommit =
@"enum MyEnum
{
    $$
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoInteraceSnippetInMethodTest()
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

        [WpfFact]
        public async Task NoInterfaceSnippetInConstructorTest()
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
