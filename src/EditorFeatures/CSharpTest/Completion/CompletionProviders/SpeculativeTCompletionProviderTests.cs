// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IsCommitCharacterTest()
        {
            const string markup = @"
class C
{
    $$
}";

            VerifyCommonCommitCharacters(markup, textTypedSoFar: "");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IsTextualTriggerCharacterTest()
        {
            TestCommonIsTextualTriggerCharacter();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SendEnterThroughToEditorTest()
        {
            const string markup = @"
class C
{
    $$
}";

            VerifySendEnterThroughToEnter(markup, "T", sendThroughEnterEnabled: false, expected: false);
            VerifySendEnterThroughToEnter(markup, "T", sendThroughEnterEnabled: true, expected: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InClass()
        {
            var markup = @"
class C
{
    $$
}";

            VerifyItemExists(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InInterface()
        {
            var markup = @"
interface I
{
    $$
}";

            VerifyItemExists(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InStruct()
        {
            var markup = @"
struct S
{
    $$
}";

            VerifyItemExists(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInNamespace()
        {
            var markup = @"
namespace N
{
    $$
}";

            VerifyItemIsAbsent(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInEnum()
        {
            var markup = @"
enum E
{
    $$
}";

            VerifyItemIsAbsent(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterDelegate()
        {
            var markup = @"
class C
{
    delegate $$
}";

            VerifyItemExists(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterVoid()
        {
            var markup = @"
class C
{
    void $$
}";

            VerifyItemIsAbsent(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterInt()
        {
            var markup = @"
class C
{
    int $$
}";

            VerifyItemIsAbsent(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InGeneric()
        {
            var markup = @"
using System;
class C
{
    Func<$$
}";

            VerifyItemExists(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InNestedGeneric1()
        {
            var markup = @"
using System;
class C
{
    Func<Func<$$
}";

            VerifyItemExists(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InNestedGeneric2()
        {
            var markup = @"
using System;
class C
{
    Func<Func<int,$$
}";

            VerifyItemExists(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InScript()
        {
            var markup = @"$$";

            VerifyItemExists(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterVoidInScript()
        {
            var markup = @"void $$";

            VerifyItemIsAbsent(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterIntInScript()
        {
            var markup = @"int $$";

            VerifyItemIsAbsent(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InGenericInScript()
        {
            var markup = @"
using System;
Func<$$
";

            VerifyItemExists(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InNestedGenericInScript1()
        {
            var markup = @"
using System;
Func<Func<$$
";

            VerifyItemExists(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InNestedGenericInScript2()
        {
            var markup = @"
using System;
Func<Func<int,$$
";

            VerifyItemExists(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInComment()
        {
            var markup = @"
class C
{
    // $$
}";

            VerifyItemIsAbsent(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInXmlDocComment()
        {
            var markup = @"
class C
{
    /// <summary>
    /// $$
    /// </summary>
    void Foo() { }
}";

            VerifyItemIsAbsent(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterAsyncTask()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async Task<$$
}";

            VerifyItemExists(markup, "T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterAsync()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async $$
}";

            VerifyItemIsAbsent(markup, "T");
        }

        [WorkItem(968256)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void UnionOfItemsFromBothContexts()
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
            VerifyItemInLinkedFiles(markup, "T", null);
        }

        [WorkItem(1020654)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterAsyncTaskWithBraceCompletion()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async Task<$$>
}";

            VerifyItemExists(markup, "T");
        }
    }
}
