// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    public class CSharpSimSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "sim";

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestMissingInNamespace()
        {
            await VerifyItemIsAbsentAsync("""
                namespace Test
                {
                    $$
                }
                """, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestMissingInFileScopedNamespace()
        {
            await VerifyItemIsAbsentAsync("""
                namespace Test;
                
                $$
                """, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestMissingInTopLevelContext()
        {
            await VerifyItemIsAbsentAsync("""
                System.Console.WriteLine();
                $$
                """, ItemToCommit);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        [InlineData("record")]
        [InlineData("record class")]
        [InlineData("record struct")]
        public async Task TestInsertSnippetInType(string type)
        {
            await VerifyCustomCommitProviderAsync($$"""
                {{type}} Program
                {
                    $$
                }
                """, ItemToCommit, $$"""
                {{type}} Program
                {
                    static int Main(string[] args)
                    {
                        $$
                        return 0;
                    }
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestMissingInEnum()
        {
            await VerifyItemIsAbsentAsync("""
                enum MyEnum
                {
                    $$
                }
                """, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestMissingInMethod()
        {
            await VerifyItemIsAbsentAsync("""
                class Program
                {
                    void M()
                    {
                        $$
                    }
                }
                """, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestMissingInConstructor()
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

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("private protected")]
        [InlineData("protected internal")]
        public async Task TestInsertSnippetAfterAccessibilityModifier(string modifier)
        {
            await VerifyCustomCommitProviderAsync($$"""
                class Program
                {
                    {{modifier}} $$
                }
                """, ItemToCommit, $$"""
                class Program
                {
                    {{modifier}} static int Main(string[] args)
                    {
                        $$
                        return 0;
                    }
                }
                """);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("static")]
        [InlineData("virtual")]
        [InlineData("abstract")]
        [InlineData("override")]
        [InlineData("file")]
        public async Task TestMissingAfterIncorrectModifiers(string modifier)
        {
            await VerifyItemIsAbsentAsync($$"""
                class Program
                {
                    {{modifier}} $$
                }
                """, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestMissingIfAnotherMemberWithNameMainExists()
        {
            await VerifyItemIsAbsentAsync("""
                class Program
                {
                    public int Main => 0;

                    $$
                }
                """, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestMissingIfTopLevelStatementsArePresent()
        {
            await VerifyItemIsAbsentAsync("""
                System.Console.WriteLine();
                
                class Program
                {
                    $$
                }
                """, ItemToCommit);
        }
    }
}
