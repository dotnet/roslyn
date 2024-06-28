// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class CSharpClassSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
{
    protected override string ItemToCommit => "class";

    [WpfFact]
    public async Task InsertClassSnippetInNamespaceTest()
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
                class MyClass
                {
                    $$
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertClassSnippetInFileScopedNamespaceTest()
    {
        var markupBeforeCommit =
            """
            namespace Namespace;

            $$
            """;

        var expectedCodeAfterCommit =
            """
            namespace Namespace;

            class MyClass
            {
                $$
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertClassSnippetTest()
    {
        var markupBeforeCommit =
@"$$";

        var expectedCodeAfterCommit =
            """
            class MyClass
            {
                $$
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertClassTopLevelSnippetTest()
    {
        var markupBeforeCommit =
            """
            System.Console.WriteLine();
            $$
            """;

        var expectedCodeAfterCommit =
            """
            System.Console.WriteLine();
            class MyClass
            {
                $$
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertClassSnippetInClassTest()
    {
        var markupBeforeCommit =
            """
            class MyClass
            {
                $$
            }
            """;

        var expectedCodeAfterCommit =
            """
            class MyClass
            {
                class MyClass1
                {
                    $$
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertClassSnippetInRecordTest()
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
                class MyClass
                {
                    $$
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertClassSnippetInStructTest()
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
                class MyClass
                {
                    $$
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertClassSnippetInInterfaceTest()
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
                class MyClass
                {
                    $$
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertClassSnippetWithModifiersTest()
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
public class MyClass
{{
    $$
}}
";
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task NoClassSnippetInEnumTest()
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
    public async Task NoClassSnippetInMethodTest()
    {
        var markupBeforeCommit =
            """
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """;
        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }

    [WpfFact]
    public async Task NoClassSnippetInConstructorTest()
    {
        var markupBeforeCommit =
            """
            class Program
            {
                public Program()
                {
                    $$
                }
            }
            """;
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
            {{modifier}} class MyClass
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
            {{modifier}} class MyClass
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
    [InlineData("unsafe")]
    public async Task InsertClassSnippetAfterValidModifiersTest(string modifier)
    {
        var markupBeforeCommit = $"{modifier} $$";

        var expectedCodeAfterCommit = $$"""
            {{modifier}} class MyClass
            {
                $$
            }
            """;

        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfTheory]
    [InlineData("ref")]
    [InlineData("readonly")]
    public async Task NoClassSnippetAfterInvalidModifiersTest(string modifier)
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
            {{modifier}} partial class MyClass
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
            public partial class MyClass
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
            public partial class MyClass
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
                <Document FilePath="/0/Test0.cs">sealed unsafe $$</Document>
            <AnalyzerConfigDocument FilePath="/.editorconfig">
            [*]
            dotnet_style_require_accessibility_modifiers = always

            csharp_preferred_modifier_order = public,sealed,unsafe
                </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """;

        var expectedCodeAfterCommit = """
            public sealed unsafe class MyClass
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
                <Document FilePath="/0/Test0.cs">sealed unsafe $$</Document>
            <AnalyzerConfigDocument FilePath="/.editorconfig">
            [*]
            dotnet_style_require_accessibility_modifiers = always

            csharp_preferred_modifier_order = public,sealed
                </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """;

        var expectedCodeAfterCommit = """
            public sealed unsafe class MyClass
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
                <Document FilePath="/0/Test0.cs">sealed unsafe $$</Document>
            <AnalyzerConfigDocument FilePath="/.editorconfig">
            [*]
            dotnet_style_require_accessibility_modifiers = always

            csharp_preferred_modifier_order = sealed,public,unsafe
                </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """;

        var expectedCodeAfterCommit = """
            sealed public unsafe class MyClass
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
                <Document FilePath="/0/Test0.cs">sealed unsafe $$</Document>
            <AnalyzerConfigDocument FilePath="/.editorconfig">
            [*]
            dotnet_style_require_accessibility_modifiers = always

            csharp_preferred_modifier_order = sealed,unsafe,public
                </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """;

        var expectedCodeAfterCommit = """
            sealed unsafe public class MyClass
            {
                $$
            }
            """;

        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }
}
