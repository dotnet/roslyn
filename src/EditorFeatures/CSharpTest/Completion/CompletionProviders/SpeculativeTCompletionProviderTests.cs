// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class SpeculativeTCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public SpeculativeTCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new SpeculativeTCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IsCommitCharacterTest()
        {
            const string markup = @"
class C
{
    $$
}";

            await VerifyCommonCommitCharactersAsync(markup, textTypedSoFar: "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IsTextualTriggerCharacterTest()
        {
            await TestCommonIsTextualTriggerCharacterAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SendEnterThroughToEditorTest()
        {
            const string markup = @"
class C
{
    $$
}";

            await VerifySendEnterThroughToEnterAsync(markup, "T", sendThroughEnterEnabled: false, expected: false);
            await VerifySendEnterThroughToEnterAsync(markup, "T", sendThroughEnterEnabled: true, expected: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InClass()
        {
            var markup = @"
class C
{
    $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InInterface()
        {
            var markup = @"
interface I
{
    $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InStruct()
        {
            var markup = @"
struct S
{
    $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInNamespace()
        {
            var markup = @"
namespace N
{
    $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInEnum()
        {
            var markup = @"
enum E
{
    $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterDelegate()
        {
            var markup = @"
class C
{
    delegate $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterVoid()
        {
            var markup = @"
class C
{
    void $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterInt()
        {
            var markup = @"
class C
{
    int $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InGeneric()
        {
            var markup = @"
using System;
class C
{
    Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InNestedGeneric1()
        {
            var markup = @"
using System;
class C
{
    Func<Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InNestedGeneric2()
        {
            var markup = @"
using System;
class C
{
    Func<Func<int,$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InScript()
        {
            var markup = @"$$";

            await VerifyItemExistsAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterVoidInScript()
        {
            var markup = @"void $$";

            await VerifyItemIsAbsentAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterIntInScript()
        {
            var markup = @"int $$";

            await VerifyItemIsAbsentAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InGenericInScript()
        {
            var markup = @"
using System;
Func<$$
";

            await VerifyItemExistsAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InNestedGenericInScript1()
        {
            var markup = @"
using System;
Func<Func<$$
";

            await VerifyItemExistsAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InNestedGenericInScript2()
        {
            var markup = @"
using System;
Func<Func<int,$$
";

            await VerifyItemExistsAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInComment()
        {
            var markup = @"
class C
{
    // $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInXmlDocComment()
        {
            var markup = @"
class C
{
    /// <summary>
    /// $$
    /// </summary>
    void Foo() { }
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterAsyncTask()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async Task<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterAsync()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [WorkItem(968256)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task UnionOfItemsFromBothContexts()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if FOO
    void foo() {
#endif

$$

#if FOO
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            await VerifyItemInLinkedFilesAsync(markup, "T", null);
        }

        [WorkItem(1020654)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterAsyncTaskWithBraceCompletion()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async Task<$$>
}";

            await VerifyItemExistsAsync(markup, "T");
        }
    }
}
