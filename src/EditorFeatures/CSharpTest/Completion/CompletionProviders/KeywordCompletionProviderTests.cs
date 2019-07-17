// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources
{
    public class KeywordCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public KeywordCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new KeywordCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task IsCommitCharacterTest()
        {
            await VerifyCommonCommitCharactersAsync("$$", textTypedSoFar: "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void IsTextualTriggerCharacterTest()
        {
            TestCommonIsTextualTriggerCharacter();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task SendEnterThroughToEditorTest()
        {
            await VerifySendEnterThroughToEnterAsync("$$", "class", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
            await VerifySendEnterThroughToEnterAsync("$$", "class", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: true);
            await VerifySendEnterThroughToEnterAsync("$$", "class", sendThroughEnterOption: EnterKeyRule.Always, expected: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InEmptyFile()
        {
            var markup = "$$";

            await VerifyAnyItemExistsAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInInactiveCode()
        {
            var markup = @"class C
{
    void M()
    {
#if false
$$
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInCharLiteral()
        {
            var markup = @"class C
{
    void M()
    {
        var c = '$$';
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedCharLiteral()
        {
            var markup = @"class C
{
    void M()
    {
        var c = '$$   ";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedCharLiteralAtEndOfFile()
        {
            var markup = @"class C
{
    void M()
    {
        var c = '$$";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInString()
        {
            var markup = @"class C
{
    void M()
    {
        var s = ""$$"";
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInStringInDirective()
        {
            var markup = "#r \"$$\"";

            await VerifyNoItemsExistAsync(markup, SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedString()
        {
            var markup = @"class C
{
    void M()
    {
        var s = ""$$   ";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedStringInDirective()
        {
            var markup = "#r \"$$\"";

            await VerifyNoItemsExistAsync(markup, SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedStringAtEndOfFile()
        {
            var markup = @"class C
{
    void M()
    {
        var s = ""$$";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInVerbatimString()
        {
            var markup = @"class C
{
    void M()
    {
        var s = @""
$$
"";
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedVerbatimString()
        {
            var markup = @"class C
{
    void M()
    {
        var s = @""
$$
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedVerbatimStringAtEndOfFile()
        {
            var markup = @"class C
{
    void M()
    {
        var s = @""$$";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInSingleLineComment()
        {
            var markup = @"class C
{
    void M()
    {
        // $$
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInSingleLineCommentAtEndOfFile()
        {
            var markup = @"namespace A
{
}// $$";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInMutliLineComment()
        {
            var markup = @"class C
{
    void M()
    {
/*
    $$
*/
";

            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(968256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968256")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task UnionOfItemsFromBothContexts()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""GOO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if GOO
    void goo() {
#endif

$$

#if GOO
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
            await VerifyItemInLinkedFilesAsync(markup, "public", null);
            await VerifyItemInLinkedFilesAsync(markup, "for", null);
        }

        [WorkItem(7768, "https://github.com/dotnet/roslyn/issues/7768")]
        [WorkItem(8228, "https://github.com/dotnet/roslyn/issues/8228")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FormattingAfterCompletionCommit_AfterGetAccessorInSingleLineIncompleteProperty()
        {
            var markupBeforeCommit = @"class Program
{
    int P {g$$
    void Main() { }
}";

            var expectedCodeAfterCommit = @"class Program
{
    int P {get;
    void Main() { }
}";
            await VerifyProviderCommitAsync(markupBeforeCommit, "get", expectedCodeAfterCommit, commitChar: ';', textTypedSoFar: "g");
        }

        [WorkItem(7768, "https://github.com/dotnet/roslyn/issues/7768")]
        [WorkItem(8228, "https://github.com/dotnet/roslyn/issues/8228")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FormattingAfterCompletionCommit_AfterBothAccessorsInSingleLineIncompleteProperty()
        {
            var markupBeforeCommit = @"class Program
{
    int P {get;set$$
    void Main() { }
}";

            var expectedCodeAfterCommit = @"class Program
{
    int P {get;set;
    void Main() { }
}";
            await VerifyProviderCommitAsync(markupBeforeCommit, "set", expectedCodeAfterCommit, commitChar: ';', textTypedSoFar: "set");
        }

        [WorkItem(7768, "https://github.com/dotnet/roslyn/issues/7768")]
        [WorkItem(8228, "https://github.com/dotnet/roslyn/issues/8228")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FormattingAfterCompletionCommit_InSingleLineMethod()
        {
            var markupBeforeCommit = @"class Program
{
    public static void Test() { return$$
    void Main() { }
}";

            var expectedCodeAfterCommit = @"class Program
{
    public static void Test() { return;
    void Main() { }
}";
            await VerifyProviderCommitAsync(markupBeforeCommit, "return", expectedCodeAfterCommit, commitChar: ';', textTypedSoFar: "return");
        }

        [WorkItem(14218, "https://github.com/dotnet/roslyn/issues/14218")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PredefinedTypeKeywordsShouldBeRecommendedAfterCaseInASwitch()
        {
            var text = @"
class Program
{
    public static void Test()
    {
        object o = null;
        switch (o)
        {
            case $$
        }
    }
}";

            await VerifyItemExistsAsync(text, "int");
            await VerifyItemExistsAsync(text, "string");
            await VerifyItemExistsAsync(text, "byte");
            await VerifyItemExistsAsync(text, "char");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PrivateOrProtectedModifiers()
        {
            var text = @"
class C
{
$$
}";

            await VerifyItemExistsAsync(text, "private");
            await VerifyItemExistsAsync(text, "protected");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PrivateProtectedModifier()
        {
            var text = @"
class C
{
    private $$
}";

            await VerifyItemExistsAsync(text, "protected");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ProtectedPrivateModifier()
        {
            var text = @"
class C
{
    protected $$
}";

            await VerifyItemExistsAsync(text, "private");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(34774, "https://github.com/dotnet/roslyn/issues/34774")]
        public async Task DontSuggestEventAfterReadonlyInClass()
        {
            var markup =
@"class C {
    readonly $$
}
";
            await VerifyItemIsAbsentAsync(markup, "event");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(34774, "https://github.com/dotnet/roslyn/issues/34774")]
        public async Task DontSuggestEventAfterReadonlyInInterface()
        {
            var markup =
@"interface C {
    readonly $$
}
";
            await VerifyItemIsAbsentAsync(markup, "event");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(34774, "https://github.com/dotnet/roslyn/issues/34774")]
        public async Task SuggestEventAfterReadonlyInStruct()
        {
            var markup =
@"struct C {
    readonly $$
}
";
            await VerifyItemExistsAsync(markup, "event");
        }
    }
}
