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
                """
                namespace Namespace
                {
                    $$
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstructorSnippetMissingInFilescopedNamespace()
        {
            var markupBeforeCommit =
                """
                namespace Namespace;

                $$
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstructorSnippetMissingInTopLevelContext()
        {
            var markupBeforeCommit =
                """
                System.Console.WriteLine();
                $$
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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
                    public MyClass()
                    {
                        $$
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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
    }
}
