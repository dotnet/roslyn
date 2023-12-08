// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                """
                namespace Namespace
                {
                    $$
                }
                """;

            var expectedCodeAfterCommit =
                """
                namespace Namespace
                {
                    struct MyStruct
                    {
                        $$
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetInFileScopedNamespaceTest()
        {
            var markupBeforeCommit =
                """
                namespace Namespace;

                $$
                """;

            var expectedCodeAfterCommit =
                """
                namespace Namespace;

                struct MyStruct
                {
                    $$
                }
                """
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetTest()
        {
            var markupBeforeCommit =
@"$$";

            var expectedCodeAfterCommit =
                """
                struct MyStruct
                {
                    $$
                }
                """
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructTopLevelSnippetTest()
        {
            var markupBeforeCommit =
                """
                System.Console.WriteLine();
                $$
                """;

            var expectedCodeAfterCommit =
                """
                System.Console.WriteLine();
                struct MyStruct
                {
                    $$
                }
                """
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetInClassTest()
        {
            var markupBeforeCommit =
                """
                struct MyClass
                {
                    $$
                }
                """
;

            var expectedCodeAfterCommit =
                """
                struct MyClass
                {
                    struct MyStruct
                    {
                        $$
                    }
                }
                """
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetInRecordTest()
        {
            var markupBeforeCommit =
                """
                record MyRecord
                {
                    $$
                }
                """;

            var expectedCodeAfterCommit =
                """
                record MyRecord
                {
                    struct MyStruct
                    {
                        $$
                    }
                }
                """
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetInStructTest()
        {
            var markupBeforeCommit =
                """
                struct MyStruct
                {
                    $$
                }
                """;

            var expectedCodeAfterCommit =
                """
                struct MyStruct
                {
                    struct MyStruct1
                    {
                        $$
                    }
                }
                """
;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertStructSnippetInInterfaceTest()
        {
            var markupBeforeCommit =
                """
                interface MyInterface
                {
                    $$
                }
                """;

            var expectedCodeAfterCommit =
                """
                interface MyInterface
                {
                    struct MyStruct
                    {
                        $$
                    }
                }
                """
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
                """
                enum MyEnum
                {
                    $$
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoStructSnippetInMethodTest()
        {
            var markupBeforeCommit =
                """
                struct Program
                {
                    public void Method()
                    {
                        $$
                    }
                }
                """
;
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoStructSnippetInConstructorTest()
        {
            var markupBeforeCommit =
                """
                struct Program
                {
                    public Program()
                    {
                        $$
                    }
                }
                """
;
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("private protected")]
        [InlineData("protected internal")]
        public async Task AfterAccessibilityModifier(string modifier)
        {
            var markupBeforeCommit = $"{modifier} $$";

            var expectedCodeAfterCommit = $$"""
                {{modifier}} struct MyStruct
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("private protected")]
        [InlineData("protected internal")]
        public async Task AfterAccessibilityModifier_RequireAccessibilityModifiers(string modifier)
        {
            var markupBeforeCommit = $$"""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">{{modifier}} $$</Document>
                <AnalyzerConfigDocument FilePath="/.editorconfig">
                root = true

                [*]
                # IDE0008: Use explicit type
                dotnet_style_require_accessibility_modifiers = always
                    </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = $$"""
                {{modifier}} struct MyStruct
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("ref")]
        [InlineData("readonly")]
        [InlineData("unsafe")]
        public async Task InsertStructSnippetAfterValidModifiersTest(string modifier)
        {
            var markupBeforeCommit = $"{modifier} $$";

            var expectedCodeAfterCommit = $$"""
                {{modifier}} struct MyStruct
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("abstract")]
        [InlineData("sealed")]
        [InlineData("static")]
        public async Task NoStructSnippetAfterInvalidModifiersTest(string modifier)
        {
            var markupBeforeCommit = $"{modifier} $$";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("private protected")]
        [InlineData("protected internal")]
        public async Task NoAdditionalAccessibilityModifiersIfAfterPartialKeywordTest(string modifier)
        {
            var markupBeforeCommit = $$"""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">{{modifier}} partial $$</Document>
                <AnalyzerConfigDocument FilePath="/.editorconfig">
                root = true

                [*]
                # IDE0008: Use explicit type
                dotnet_style_require_accessibility_modifiers = always
                    </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = $$"""
                {{modifier}} partial struct MyStruct
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
        public async Task EnsureCorrectModifierOrderAfterPartialKeywordTest()
        {
            var markupBeforeCommit = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">partial $$</Document>
                <AnalyzerConfigDocument FilePath="/.editorconfig">
                root = true

                [*]
                # IDE0008: Use explicit type
                dotnet_style_require_accessibility_modifiers = always
                    </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = """
                public partial struct MyStruct
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
        public async Task EnsureCorrectModifierOrderAfterPartialKeywordTest_InvalidPreferredModifiersList()
        {
            var markupBeforeCommit = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">partial $$</Document>
                <AnalyzerConfigDocument FilePath="/.editorconfig">
                [*]
                dotnet_style_require_accessibility_modifiers = always

                csharp_preferred_modifier_order = invalid!
                    </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = """
                public partial struct MyStruct
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBeforeAllOthers()
        {
            var markupBeforeCommit = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">readonly ref $$</Document>
                <AnalyzerConfigDocument FilePath="/.editorconfig">
                [*]
                dotnet_style_require_accessibility_modifiers = always

                csharp_preferred_modifier_order = public,readonly,ref
                    </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = """
                public readonly ref struct MyStruct
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBeforeAllOthers_NotAllModifiersInTheList()
        {
            var markupBeforeCommit = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">readonly ref $$</Document>
                <AnalyzerConfigDocument FilePath="/.editorconfig">
                [*]
                dotnet_style_require_accessibility_modifiers = always

                csharp_preferred_modifier_order = public,readonly
                    </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = """
                public readonly ref struct MyStruct
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBetweenOthers()
        {
            var markupBeforeCommit = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">readonly ref $$</Document>
                <AnalyzerConfigDocument FilePath="/.editorconfig">
                [*]
                dotnet_style_require_accessibility_modifiers = always

                csharp_preferred_modifier_order = readonly,public,ref
                    </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = """
                readonly public ref struct MyStruct
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierAfterAllOthers()
        {
            var markupBeforeCommit = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">readonly ref $$</Document>
                <AnalyzerConfigDocument FilePath="/.editorconfig">
                [*]
                dotnet_style_require_accessibility_modifiers = always

                csharp_preferred_modifier_order = readonly,ref,public
                    </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = """
                readonly ref public struct MyStruct
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
