// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    public abstract class AbstractCSharpAutoPropertyCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected abstract string GetDefaultPropertyText(string propertyName);

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MissingInNamespace()
        {
            await VerifyPropertyAbsenceAsync("""
                namespace Namespace
                {
                    $$
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MissingInFilescopedNamespace()
        {
            await VerifyPropertyAbsenceAsync("""
                namespace Namespace;

                $$
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MissingInTopLevelContext()
        {
            await VerifyPropertyAbsenceAsync("""
                System.Console.WriteLine();
                $$
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertSnippetInClass()
        {
            await VerifyDefaultPropertyAsync("""
                class MyClass
                {
                    $$
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertSnippetInRecord()
        {
            await VerifyDefaultPropertyAsync("""
                record MyRecord
                {
                    $$
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertSnippetInStruct()
        {
            await VerifyDefaultPropertyAsync("""
                struct MyStruct
                {
                    $$
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public virtual async Task InsertSnippetInInterface()
        {
            await VerifyDefaultPropertyAsync("""
                interface MyInterface
                {
                    $$
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertSnippetNaming()
        {
            await VerifyDefaultPropertyAsync("""
                class MyClass
                {
                    public int MyProperty { get; set; }
                    $$
                }
                """, "MyProperty1");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MissingInEnum()
        {
            await VerifyPropertyAbsenceAsync("""
                enum MyEnum
                {
                    $$
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MissingInMethod()
        {
            await VerifyPropertyAbsenceAsync("""
                class Program
                {
                    public void Method()
                    {
                        $$
                    }
                }
                """);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MissingInConstructor()
        {
            await VerifyPropertyAbsenceAsync("""
                class Program
                {
                    public Program()
                    {
                        $$
                    }
                }
                """);
        }

        private Task VerifyPropertyAbsenceAsync(string markup) => VerifyItemIsAbsentAsync(markup, ItemToCommit);

        protected async Task VerifyPropertyAsync(string markup, string propertyText)
        {
            TestFileMarkupParser.GetPosition(markup, out var code, out var position);
            var expectedCode = code.Insert(position, propertyText + "$$");
            await VerifyCustomCommitProviderAsync(markup, ItemToCommit, expectedCode);
        }

        private Task VerifyDefaultPropertyAsync(string markup, string propertyName = "MyProperty")
            => VerifyPropertyAsync(markup, GetDefaultPropertyText(propertyName));
    }
}
