// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets;

public class CSharpEnumSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
{
    protected override string ItemToCommit => "enum";

    [WpfFact]
    public async Task InsertEnumSnippetInNamespaceTest()
    {
        await VerifyCustomCommitProviderAsync("""
            namespace Namespace
            {
                $$
            }
            """, ItemToCommit, """
            namespace Namespace
            {
                enum MyEnum
                {
                    $$
                }
            }
            """);
    }

    [WpfFact]
    public async Task InsertEnumSnippetInFileScopedNamespaceTest()
    {
        await VerifyCustomCommitProviderAsync("""
            namespace Namespace;
            
            $$
            """, ItemToCommit, """
            namespace Namespace;
            
            enum MyEnum
            {
                $$
            }
            """);
    }

    [WpfFact]
    public async Task InsertEnumSnippetTest()
    {
        await VerifyCustomCommitProviderAsync("""
            $$
            """, ItemToCommit, """
            enum MyEnum
            {
                $$
            }
            """);
    }

    [WpfFact]
    public async Task InsertEnumTopLevelSnippetTest()
    {
        await VerifyCustomCommitProviderAsync("""
            System.Console.WriteLine();
            $$
            """, ItemToCommit, """
            System.Console.WriteLine();
            enum MyEnum
            {
                $$
            }
            """);
    }

    [WpfFact]
    public async Task InsertEnumSnippetInClassTest()
    {
        await VerifyCustomCommitProviderAsync("""
            class MyClass
            {
                $$
            }
            """, ItemToCommit, """
            class MyClass
            {
                enum MyEnum
                {
                    $$
                }
            }
            """);
    }

    [WpfFact]
    public async Task InsertEnumSnippetInRecordTest()
    {
        await VerifyCustomCommitProviderAsync("""
            record MyRecord
            {
                $$
            }
            """, ItemToCommit, """
            record MyRecord
            {
                enum MyEnum
                {
                    $$
                }
            }
            """);
    }

    [WpfFact]
    public async Task InsertEnumSnippetInStructTest()
    {
        await VerifyCustomCommitProviderAsync("""
            struct MyStruct
            {
                $$
            }
            """, ItemToCommit, """
            struct MyStruct
            {
                enum MyEnum
                {
                    $$
                }
            }
            """);
    }

    [WpfFact]
    public async Task InsertEnumSnippetInInterfaceTest()
    {
        await VerifyCustomCommitProviderAsync("""
            interface MyInterface
            {
                $$
            }
            """, ItemToCommit, """
            interface MyInterface
            {
                enum MyEnum
                {
                    $$
                }
            }
            """);
    }

    [WpfFact]
    public async Task InsertEnumSnippetWithModifiersTest()
    {
        await VerifyCustomCommitProviderAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                <Document FilePath="/0/Test0.cs">$$</Document>
            <AnalyzerConfigDocument FilePath="/.editorconfig">
            root = true
            
            [*]
            # IDE0008: Use explicit type
            dotnet_style_require_accessibility_modifiers = always
                </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """, ItemToCommit, """
            public enum MyEnum
            {
                $$
            }
            """);
    }

    [WpfFact]
    public async Task NoEnumSnippetInEnumTest()
    {
        await VerifyItemIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """, ItemToCommit);
    }

    [WpfFact]
    public async Task NoEnumSnippetInMethodTest()
    {
        await VerifyItemIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """, ItemToCommit);
    }

    [WpfFact]
    public async Task NoEnumSnippetInConstructorTest()
    {
        await VerifyItemIsAbsentAsync("""
            class Program
            {
                public Program()
                {
                    $$
                }
            }
            """, ItemToCommit);
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
            {{modifier}} enum MyEnum
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
            {{modifier}} enum MyEnum
            {
                $$
            }
            """;

        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfTheory]
    [InlineData("abstract")]
    [InlineData("partial")]
    [InlineData("sealed")]
    [InlineData("static")]
    [InlineData("ref")]
    [InlineData("readonly")]
    [InlineData("unsafe")]
    public async Task NoEnumSnippetAfterInvalidModifiersTest(string modifier)
    {
        var markupBeforeCommit = $"{modifier} $$";

        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }
}
