// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class AttributeNamedParameterCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(AttributeNamedParameterCompletionProvider);

    [Fact]
    public async Task SendEnterThroughToEditorTest()
    {
        const string markup = """
            using System;
            class class1
            {
                [Test($$
                public void Goo()
                {
                }
            }

            public class TestAttribute : Attribute
            {
                public ConsoleColor Color { get; set; }
            }
            """;

        await VerifySendEnterThroughToEnterAsync(markup, "Color =", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
        await VerifySendEnterThroughToEnterAsync(markup, "Color =", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: true);
        await VerifySendEnterThroughToEnterAsync(markup, "Color =", sendThroughEnterOption: EnterKeyRule.Always, expected: true);
    }

    [Fact]
    public Task CommitCharacterTest()
        => VerifyCommonCommitCharactersAsync("""
            using System;
            class class1
            {
                [Test($$
                public void Goo()
                {
                }
            }

            public class TestAttribute : Attribute
            {
                public ConsoleColor Color { get; set; }
            }
            """, textTypedSoFar: "");

    [Fact]
    public Task SimpleAttributeUsage()
        => VerifyItemExistsAsync("""
            using System;
            class class1
            {
                [Test($$
                public void Goo()
                {
                }
            }

            public class TestAttribute : Attribute
            {
                public ConsoleColor Color { get; set; }
            }
            """, "Color", displayTextSuffix: " =");

    [Fact]
    public Task AfterComma()
        => VerifyItemExistsAsync("""
            using System;
            class class1
            {
                [Test(Color = ConsoleColor.Black, $$
                public void Goo()
                {
                }
            }

            public class TestAttribute : Attribute
            {
                public ConsoleColor Color { get; set; }
                public string Text { get; set; }
            }
            """, "Text", displayTextSuffix: " =");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544345")]
    public async Task ExistingItemsAreFiltered()
    {
        var markup = """
            using System;
            class class1
            {
                [Test(Color = ConsoleColor.Black, $$
                public void Goo()
                {
                }
            }

            public class TestAttribute : Attribute
            {
                public ConsoleColor Color { get; set; }
                public string Text { get; set; }
            }
            """;

        await VerifyItemExistsAsync(markup, "Text", displayTextSuffix: " =");
        await VerifyItemIsAbsentAsync(markup, "Color", displayTextSuffix: " =");
    }

    [Fact]
    public Task AttributeConstructor()
        => VerifyItemExistsAsync("""
            using System;
            class TestAttribute : Attribute
            {
                public TestAttribute(int a = 42)
                { }
            }

            [Test($$
            class Goo
            { }
            """, "a", displayTextSuffix: ":");

    [Fact]
    public Task AttributeConstructorAfterComma()
        => VerifyItemExistsAsync("""
            using System;
            class TestAttribute : Attribute
            {
                public TestAttribute(int a = 42, string s = "")
                { }
            }

            [Test(s:"", $$
            class Goo
            { }
            """, "a", displayTextSuffix: ":");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545426")]
    public Task TestPropertiesInScript()
        => VerifyItemExistsAsync("""
            using System;

            class TestAttribute : Attribute
            {
                public string Text { get; set; }
                public TestAttribute(int number = 42)
                {
                }
            }

            [Test($$
            class Goo
            {
            }
            """, "Text", displayTextSuffix: " =");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075278")]
    public Task NotInComment()
        => VerifyNoItemsExistAsync("""
            using System;
            class class1
            {
                [Test( //$$
                public void Goo()
                {
                }
            }

            public class TestAttribute : Attribute
            {
                public ConsoleColor Color { get; set; }
            }
            """);
}
