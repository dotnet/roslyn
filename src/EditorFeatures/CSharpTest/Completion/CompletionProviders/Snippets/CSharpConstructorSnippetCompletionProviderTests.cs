// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class CSharpConstructorSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
{
    protected override string ItemToCommit => "ctor";

    [WpfFact]
    public async Task ConstructorSnippetMissingInNamespace()
    {
        var markupBeforeCommit =
            """
            namespace Namespace
            {
                $$
            }
            """;

        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }

    [WpfFact]
    public async Task ConstructorSnippetMissingInFileScopedNamespace()
    {
        var markupBeforeCommit =
            """
            namespace Namespace;

            $$
            """;

        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }

    [WpfFact]
    public async Task ConstructorSnippetMissingInTopLevelContext()
    {
        var markupBeforeCommit =
            """
            System.Console.WriteLine();
            $$
            """;

        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }

    [WpfFact]
    public async Task InsertConstructorSnippetInClassTest()
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
                public MyClass()
                {
                    $$
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertConstructorSnippetInAbstractClassTest()
    {
        var markupBeforeCommit =
            """
            abstract class MyClass
            {
                $$
            }
            """;

        var expectedCodeAfterCommit =
            """
            abstract class MyClass
            {
                protected MyClass()
                {
                    $$
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertConstructorSnippetInAbstractClassTest_AbstractModifierInOtherPartialDeclaration()
    {
        var markupBeforeCommit =
            """
            partial class MyClass
            {
                $$
            }

            abstract partial class MyClass
            {
            }
            """;

        var expectedCodeAfterCommit =
            """
            partial class MyClass
            {
                protected MyClass()
                {
                    $$
                }
            }
            
            abstract partial class MyClass
            {
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertConstructorSnippetInNestedAbstractClassTest()
    {
        var markupBeforeCommit =
            """
            class MyClass
            {
                abstract class NestedClass
                {
                    $$
                }
            }
            """;

        var expectedCodeAfterCommit =
            """
            class MyClass
            {
                abstract class NestedClass
                {
                    protected NestedClass()
                    {
                        $$
                    }
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertConstructorSnippetInStructTest()
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
                public MyStruct()
                {
                    $$
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertConstructorSnippetInRecordTest()
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
                public MyRecord()
                {
                    $$
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task ConstructorSnippetMissingInInterface()
    {
        var markupBeforeCommit =
            """
            interface MyInterface
            {
                $$
            }
            """;

        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }

    [WpfFact]
    public async Task InsertConstructorSnippetInNestedClassTest()
    {
        var markupBeforeCommit =
            """
            class MyClass
            {
                class MyClass1
                {
                    $$
                }
            }
            """;

        var expectedCodeAfterCommit =
            """
            class MyClass
            {
                class MyClass1
                {
                    public MyClass1()
                    {
                        $$
                    }
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfTheory]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("private protected")]
    [InlineData("protected internal")]
    [InlineData("static")]
    public async Task InsertConstructorSnippetAfterValidModifiersTest(string modifiers)
    {
        var markupBeforeCommit = $$"""
            class MyClass
            {
                {{modifiers}} $$
            }
            """;

        var expectedCodeAfterCommit = $$"""
            class MyClass
            {
                {{modifiers}} MyClass()
                {
                    $$
                }
            }
            """;

        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfTheory]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("virtual")]
    [InlineData("override")]
    [InlineData("readonly")]
    [InlineData("new")]
    [InlineData("file")]
    public async Task ConstructorSnippetMissingAfterInvalidModifierTest(string modifier)
    {
        var markupBeforeCommit = $$"""
            class MyClass
            {
                {{modifier}} $$
            }
            """;

        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }

    [WpfTheory]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("private protected")]
    [InlineData("protected internal")]
    public async Task ConstructorSnippetMissingAfterBothAccessibilityModifierAndStaticKeywordTest(string accessibilityModifier)
    {
        var markupBeforeCommit = $$"""
            class MyClass
            {
                {{accessibilityModifier}} static $$
            }
            """;

        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }

    [WpfFact]
    public async Task InsertConstructorSnippetAfterAccessibilityModifierBeforeOtherMemberTest()
    {
        var markupBeforeCommit = """
            class C
            {
                private $$
                readonly int Value = 3;
            }
            """;

        var expectedCodeAfterCommit = """
            class C
            {
                private C()
                {
                    $$
                }
                readonly int Value = 3;
            }
            """;

        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertConstructorSnippetBetweenAccessibilityModifiersBeforeOtherMemberTest()
    {
        var markupBeforeCommit = """
            class C
            {
                protected $$
                internal int Value = 3;
            }
            """;

        var expectedCodeAfterCommit = """
            class C
            {
                protected C()
                {
                    $$
                }
                internal int Value = 3;
            }
            """;

        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertConstructorSnippetAfterAccessibilityModifierBeforeOtherStaticMemberTest()
    {
        var markupBeforeCommit = """
            class C
            {
                internal $$
                static int Value = 3;
            }
            """;

        var expectedCodeAfterCommit = """
            class C
            {
                internal C()
                {
                    $$
                }
                static int Value = 3;
            }
            """;

        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/68176")]
    public async Task InsertCorrectConstructorSnippetInNestedTypeTest_CtorBeforeNestedType()
    {
        var markupBeforeCommit = """
            class Outer
            {
                $$
                class Inner
                {
                }
            }
            """;

        var expectedCodeAfterCommit = """
            class Outer
            {
                public Outer()
                {
                    $$
                }
                class Inner
                {
                }
            }
            """;

        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/68176")]
    public async Task InsertCorrectConstructorSnippetInNestedTypeTest_CtorAfterNestedType()
    {
        var markupBeforeCommit = """
            class Outer
            {
                class Inner
                {
                }
                $$
            }
            """;

        var expectedCodeAfterCommit = """
            class Outer
            {
                class Inner
                {
                }
                public Outer()
                {
                    $$
                }
            }
            """;

        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }
}
