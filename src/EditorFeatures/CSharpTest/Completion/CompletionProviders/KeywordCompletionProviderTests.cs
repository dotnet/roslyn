// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources
{
    public class KeywordCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(KeywordCompletionProvider);

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task IsCommitCharacterTest()
            => await VerifyCommonCommitCharactersAsync("$$", textTypedSoFar: "");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void IsTextualTriggerCharacterTest()
            => TestCommonIsTextualTriggerCharacter();

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
            var markup = """
                class C
                {
                    void M()
                    {
                #if false
                $$
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInCharLiteral()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var c = '$$';
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedCharLiteral()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var c = '$$
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedCharLiteralAtEndOfFile()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var c = '$$
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInString()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var s = "$$";
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInStringInDirective()
        {
            var markup = """
                #r "$$"
                """;

            await VerifyNoItemsExistAsync(markup, SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedString()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var s = "$$
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedStringInDirective()
        {
            var markup = """
                #r "$$"
                """;

            await VerifyNoItemsExistAsync(markup, SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedStringAtEndOfFile()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var s = "$$
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInVerbatimString()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var s = @"
                $$
                ";
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedVerbatimString()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var s = @"
                $$
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInUnterminatedVerbatimStringAtEndOfFile()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var s = @"$$
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInSingleLineComment()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        // $$
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInSingleLineCommentAtEndOfFile()
        {
            var markup = """
                namespace A
                {
                }// $$
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInMutliLineComment()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                /*
                    $$
                */
                """;

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968256")]
        public async Task UnionOfItemsFromBothContexts()
        {
            var markup = """
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                        <Document FilePath="CurrentDocument.cs"><![CDATA[
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
                    <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                        <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                    </Project>
                </Workspace>
                """;
            await VerifyItemInLinkedFilesAsync(markup, "public", null);
            await VerifyItemInLinkedFilesAsync(markup, "for", null);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/7768")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/8228")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FormattingAfterCompletionCommit_AfterGetAccessorInSingleLineIncompleteProperty()
        {
            var markupBeforeCommit = """
                class Program
                {
                    int P {g$$
                    void Main() { }
                }
                """;

            var expectedCodeAfterCommit = """
                class Program
                {
                    int P {get;
                    void Main() { }
                }
                """;
            await VerifyProviderCommitAsync(markupBeforeCommit, "get", expectedCodeAfterCommit, commitChar: ';');
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/7768")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/8228")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FormattingAfterCompletionCommit_AfterBothAccessorsInSingleLineIncompleteProperty()
        {
            var markupBeforeCommit = """
                class Program
                {
                    int P {get;set$$
                    void Main() { }
                }
                """;

            var expectedCodeAfterCommit = """
                class Program
                {
                    int P {get;set;
                    void Main() { }
                }
                """;
            await VerifyProviderCommitAsync(markupBeforeCommit, "set", expectedCodeAfterCommit, commitChar: ';');
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/7768")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/8228")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FormattingAfterCompletionCommit_InSingleLineMethod()
        {
            var markupBeforeCommit = """
                class Program
                {
                    public static void Test() { return$$
                    void Main() { }
                }
                """;

            var expectedCodeAfterCommit = """
                class Program
                {
                    public static void Test() { return;
                    void Main() { }
                }
                """;
            await VerifyProviderCommitAsync(markupBeforeCommit, "return", expectedCodeAfterCommit, commitChar: ';');
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/14218")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PredefinedTypeKeywordsShouldBeRecommendedAfterCaseInASwitch()
        {
            var text = """
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
                }
                """;

            await VerifyItemExistsAsync(text, "int");
            await VerifyItemExistsAsync(text, "string");
            await VerifyItemExistsAsync(text, "byte");
            await VerifyItemExistsAsync(text, "char");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PrivateOrProtectedModifiers()
        {
            var text = """
                class C
                {
                $$
                }
                """;

            await VerifyItemExistsAsync(text, "private");
            await VerifyItemExistsAsync(text, "protected");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PrivateProtectedModifier()
        {
            var text = """
                class C
                {
                    private $$
                }
                """;

            await VerifyItemExistsAsync(text, "protected");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ProtectedPrivateModifier()
        {
            var text = """
                class C
                {
                    protected $$
                }
                """;

            await VerifyItemExistsAsync(text, "private");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/34774")]
        public async Task DoNotSuggestEventAfterReadonlyInClass()
        {
            var markup =
                """
                class C {
                    readonly $$
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "event");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/34774")]
        public async Task DoNotSuggestEventAfterReadonlyInInterface()
        {
            var markup =
                """
                interface C {
                    readonly $$
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "event");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/34774")]
        public async Task SuggestEventAfterReadonlyInStruct()
        {
            var markup =
                """
                struct C {
                    readonly $$
                }
                """;
            await VerifyItemExistsAsync(markup, "event");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/39265")]
        [InlineData("struct", true)]
        [InlineData("record struct", true)]
        [InlineData("class", false)]
        [InlineData("record", false)]
        [InlineData("record class", false)]
        [InlineData("interface", false)]
        public async Task SuggestReadonlyPropertyAccessor(string declarationType, bool present)
        {

            var markup =
$@"{declarationType} C {{
    int X {{
        $$
    }}
}}
";
            if (present)
            {
                await VerifyItemExistsAsync(markup, "readonly");
            }
            else
            {
                await VerifyItemIsAbsentAsync(markup, "readonly");
            }
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/39265")]
        [InlineData("struct", true)]
        [InlineData("class", false)]
        [InlineData("interface", false)]
        public async Task SuggestReadonlyBeforePropertyAccessor(string declarationType, bool present)
        {

            var markup =
$@"{declarationType} C {{
    int X {{
        $$ get;
    }}
}}
";
            if (present)
            {
                await VerifyItemExistsAsync(markup, "readonly");
            }
            else
            {
                await VerifyItemIsAbsentAsync(markup, "readonly");
            }
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/39265")]
        [InlineData("struct", true)]
        [InlineData("class", false)]
        [InlineData("interface", false)]
        public async Task SuggestReadonlyIndexerAccessor(string declarationType, bool present)
        {

            var markup =
$@"{declarationType} C {{
    int this[int i] {{
        $$
    }}
}}
";
            if (present)
            {
                await VerifyItemExistsAsync(markup, "readonly");
            }
            else
            {
                await VerifyItemIsAbsentAsync(markup, "readonly");
            }
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/39265")]
        [InlineData("struct", true)]
        [InlineData("class", false)]
        [InlineData("interface", false)]
        public async Task SuggestReadonlyEventAccessor(string declarationType, bool present)
        {

            var markup =
$@"{declarationType} C {{
    event System.Action E {{
        $$
    }}
}}
";
            if (present)
            {
                await VerifyItemExistsAsync(markup, "readonly");
            }
            else
            {
                await VerifyItemIsAbsentAsync(markup, "readonly");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/39265")]
        public async Task SuggestAccessorAfterReadonlyInStruct()
        {
            var markup =
                """
                struct C {
                    int X {
                        readonly $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "get");
            await VerifyItemExistsAsync(markup, "set");
            await VerifyItemIsAbsentAsync(markup, "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/39265")]
        public async Task SuggestReadonlyMethodInStruct()
        {

            var markup =
                """
                struct C {
                    public $$ void M() {}
                }
                """;
            await VerifyItemExistsAsync(markup, "readonly");
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/58921"), CombinatorialData, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCastExpressionThatMightBeParenthesizedExpression1(bool hasNewline)
        {

            var markup =
$@"
class C
{{
    void M()
    {{
        var data = (n$$) {(hasNewline ? Environment.NewLine : string.Empty)} M();
    }}
}}";

            if (hasNewline)
            {
                await VerifyItemExistsAsync(markup, "new");
                await VerifyItemExistsAsync(markup, "this");
                await VerifyItemExistsAsync(markup, "null");
                await VerifyItemExistsAsync(markup, "base");
                await VerifyItemExistsAsync(markup, "true");
                await VerifyItemExistsAsync(markup, "false");
                await VerifyItemExistsAsync(markup, "typeof");
                await VerifyItemExistsAsync(markup, "sizeof");
                await VerifyItemExistsAsync(markup, "nameof");
            }
            else
            {
                await VerifyItemIsAbsentAsync(markup, "new");
                await VerifyItemIsAbsentAsync(markup, "this");
                await VerifyItemIsAbsentAsync(markup, "null");
                await VerifyItemIsAbsentAsync(markup, "base");
                await VerifyItemIsAbsentAsync(markup, "true");
                await VerifyItemIsAbsentAsync(markup, "false");
                await VerifyItemIsAbsentAsync(markup, "typeof");
                await VerifyItemIsAbsentAsync(markup, "sizeof");
                await VerifyItemIsAbsentAsync(markup, "nameof");
            }
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/57886")]
        public async Task TestInCastExpressionThatMightBeParenthesizedExpression2(bool hasExpression)
        {

            var markup =
$@"class C
{{
    bool Prop => (t$$)  {(hasExpression ? "n" : string.Empty)}
    private int n;
}}";
            if (hasExpression)
            {
                await VerifyItemIsAbsentAsync(markup, "new");
                await VerifyItemIsAbsentAsync(markup, "this");
                await VerifyItemIsAbsentAsync(markup, "null");
                await VerifyItemIsAbsentAsync(markup, "base");
                await VerifyItemIsAbsentAsync(markup, "true");
                await VerifyItemIsAbsentAsync(markup, "false");
                await VerifyItemIsAbsentAsync(markup, "typeof");
                await VerifyItemIsAbsentAsync(markup, "sizeof");
                await VerifyItemIsAbsentAsync(markup, "nameof");
            }
            else
            {
                await VerifyItemExistsAsync(markup, "new");
                await VerifyItemExistsAsync(markup, "this");
                await VerifyItemExistsAsync(markup, "null");
                await VerifyItemExistsAsync(markup, "base");
                await VerifyItemExistsAsync(markup, "true");
                await VerifyItemExistsAsync(markup, "false");
                await VerifyItemExistsAsync(markup, "typeof");
                await VerifyItemExistsAsync(markup, "sizeof");
                await VerifyItemExistsAsync(markup, "nameof");
            }
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("record struct")]
        public async Task SuggestRequiredInClassOrStructOrRecord(string type)
        {
            var markup = $$"""
                {{type}} C
                {
                    $$
                """;

            await VerifyItemExistsAsync(markup, "required");
        }

        [Fact]
        public async Task DoNotSuggestRequiredInInterface()
        {
            var markup = $$"""
                interface I
                {
                    public $$
                """;

            await VerifyItemIsAbsentAsync(markup, "required");
        }

        [Theory]
        [InlineData("static")]
        [InlineData("const")]
        [InlineData("readonly")]
        public async Task DoNotSuggestRequiredOnFilteredKeywordMembers(string keyword)
        {
            var markup = $$"""
                class C 
                {
                    {{keyword}} $$
                """;

            await VerifyItemIsAbsentAsync(markup, "required");
        }

        [Theory]
        [InlineData("static")]
        [InlineData("const")]
        [InlineData("readonly")]
        public async Task DoNotSuggestFilteredKeywordsOnRequiredMembers(string keyword)
        {
            var markup = $$"""
                class C 
                {
                    required $$
                """;

            await VerifyItemIsAbsentAsync(markup, keyword);
        }

        [Fact]
        public async Task DoNotSuggestRequiredOnRequiredMembers()
        {
            var markup = $$"""
                class C 
                {
                    required $$
                """;

            await VerifyItemIsAbsentAsync(markup, "required");
        }

        [Fact]
        public async Task SuggestFileOnTypes()
        {
            var markup = $$"""
                $$ class C { }
                """;

            await VerifyItemExistsAsync(markup, "file");
        }

        [Fact]
        public async Task DoNotSuggestFileAfterFile()
        {
            var markup = $$"""
                file $$
                """;

            await VerifyItemIsAbsentAsync(markup, "file");
        }

        [Fact]
        public async Task SuggestFileAfterReadonly()
        {
            // e.g. 'readonly file struct X { }'
            var markup = $$"""
                readonly $$
                """;

            await VerifyItemExistsAsync(markup, "file");
        }

        [Fact]
        public async Task SuggestFileBeforeFileType()
        {
            var markup = $$"""
                $$

                file class C { }
                """;

            // it might seem like we want to prevent 'file file class',
            // but it's likely the user is declaring a file-local type above an existing file-local type here.
            await VerifyItemExistsAsync(markup, "file");
        }

        [Fact]
        public async Task SuggestFileBeforeDelegate()
        {
            var markup = $$"""
                $$ delegate
                """;

            await VerifyItemExistsAsync(markup, "file");
        }

        [Fact]
        public async Task DoNotSuggestFileOnNestedTypes()
        {
            var markup = $$"""
                class Outer
                {
                    $$ class C { }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, "file");
        }

        [Fact]
        public async Task DoNotSuggestFileOnNonTypeMembers()
        {
            var markup = $$"""
                class C
                {
                    $$
                }
                """;

            await VerifyItemIsAbsentAsync(markup, "file");
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("private")]
        public async Task DoNotSuggestFileAfterFilteredKeywords(string keyword)
        {
            var markup = $$"""
                {{keyword}} $$
                """;

            await VerifyItemIsAbsentAsync(markup, "file");
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("private")]
        public async Task DoNotSuggestFilteredKeywordsAfterFile(string keyword)
        {
            var markup = $$"""
                file $$
                """;

            await VerifyItemIsAbsentAsync(markup, keyword);
        }

        [Fact]
        public async Task SuggestFileInFileScopedNamespace()
        {
            var markup = $$"""
                namespace NS;

                $$
                """;

            await VerifyItemExistsAsync(markup, "file");
        }

        [Fact]
        public async Task SuggestFileInNamespace()
        {
            var markup = $$"""
                namespace NS
                {
                    $$
                }
                """;

            await VerifyItemExistsAsync(markup, "file");
        }

        [Fact]
        public async Task SuggestFileAfterClass()
        {
            var markup = $$"""
                file class C { }

                $$
                """;

            await VerifyItemExistsAsync(markup, "file");
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
        [MemberData(nameof(TypeDeclarationKeywords))]
        public async Task TestTypeDeclarationKeywordsNotAfterUsingUnsafe(string keyword)
        {
            await VerifyItemIsAbsentAsync("using unsafe $$", keyword);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
        [MemberData(nameof(TypeDeclarationKeywords))]
        public async Task TestTypeDeclarationKeywordsNotAfterUsingStaticUnsafe(string keyword)
        {
            await VerifyItemIsAbsentAsync("using static unsafe $$", keyword);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
        [MemberData(nameof(TypeDeclarationKeywords))]
        public async Task TestTypeDeclarationKeywordsNotAfterGlobalUsingUnsafe(string keyword)
        {
            await VerifyItemIsAbsentAsync("global using unsafe $$", keyword);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
        [MemberData(nameof(TypeDeclarationKeywords))]
        public async Task TestTypeDeclarationKeywordsNotAfterGlobalUsingStaticUnsafe(string keyword)
        {
            await VerifyItemIsAbsentAsync("global using static unsafe $$", keyword);
        }

        public static IEnumerable<object[]> TypeDeclarationKeywords()
        {
            yield return new[] { "abstract" };
            yield return new[] { "class" };
            yield return new[] { "delegate" };
            yield return new[] { "file" };
            yield return new[] { "interface" };
            yield return new[] { "internal" };
            yield return new[] { "partial" };
            yield return new[] { "public" };
            yield return new[] { "readonly" };
            yield return new[] { "record" };
            yield return new[] { "ref" };
            yield return new[] { "sealed" };
            yield return new[] { "static" };
            yield return new[] { "struct" };
        }
    }
}
