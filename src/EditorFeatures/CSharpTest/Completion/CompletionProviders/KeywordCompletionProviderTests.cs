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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources;

public sealed class KeywordCompletionProviderTests : AbstractCSharpCompletionProviderTests
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
    public Task InEmptyFile()
        => VerifyAnyItemExistsAsync("$$");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInInactiveCode()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
            #if false
            $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInCharLiteral()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    var c = '$$';
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInUnterminatedCharLiteral()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    var c = '$$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInUnterminatedCharLiteralAtEndOfFile()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    var c = '$$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInString()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    var s = "$$";
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInStringInDirective()
        => VerifyNoItemsExistAsync("""
            #r "$$"
            """, SourceCodeKind.Script);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInUnterminatedString()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    var s = "$$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInUnterminatedStringInDirective()
        => VerifyNoItemsExistAsync("""
            #r "$$"
            """, SourceCodeKind.Script);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInUnterminatedStringAtEndOfFile()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    var s = "$$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInVerbatimString()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    var s = @"
            $$
            ";
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInUnterminatedVerbatimString()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    var s = @"
            $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInUnterminatedVerbatimStringAtEndOfFile()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    var s = @"$$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInSingleLineComment()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    // $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInSingleLineCommentAtEndOfFile()
        => VerifyNoItemsExistAsync("""
            namespace A
            {
            }// $$
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task NotInMutliLineComment()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
            /*
                $$
            */
            """);

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
    public Task FormattingAfterCompletionCommit_AfterGetAccessorInSingleLineIncompleteProperty()
        => VerifyProviderCommitAsync("""
            class Program
            {
                int P {g$$
                void Main() { }
            }
            """, "get", """
            class Program
            {
                int P {get;
                void Main() { }
            }
            """, commitChar: ';');

    [WorkItem("https://github.com/dotnet/roslyn/issues/7768")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/8228")]
    [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task FormattingAfterCompletionCommit_AfterBothAccessorsInSingleLineIncompleteProperty()
        => VerifyProviderCommitAsync("""
            class Program
            {
                int P {get;set$$
                void Main() { }
            }
            """, "set", """
            class Program
            {
                int P {get;set;
                void Main() { }
            }
            """, commitChar: ';');

    [WorkItem("https://github.com/dotnet/roslyn/issues/7768")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/8228")]
    [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task FormattingAfterCompletionCommit_InSingleLineMethod()
        => VerifyProviderCommitAsync("""
            class Program
            {
                public static void Test() { return$$
                void Main() { }
            }
            """, "return", """
            class Program
            {
                public static void Test() { return;
                void Main() { }
            }
            """, commitChar: ';');

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
    public Task PrivateProtectedModifier()
        => VerifyItemExistsAsync("""
            class C
            {
                private $$
            }
            """, "protected");

    [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task ProtectedPrivateModifier()
        => VerifyItemExistsAsync("""
            class C
            {
                protected $$
            }
            """, "private");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34774")]
    public Task DoNotSuggestEventAfterReadonlyInClass()
        => VerifyItemIsAbsentAsync("""
            class C {
                readonly $$
            }
            """, "event");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34774")]
    public Task DoNotSuggestEventAfterReadonlyInInterface()
        => VerifyItemIsAbsentAsync("""
            interface C {
                readonly $$
            }
            """, "event");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34774")]
    public Task SuggestEventAfterReadonlyInStruct()
        => VerifyItemExistsAsync("""
            struct C {
                readonly $$
            }
            """, "event");

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
            $$"""
            {{declarationType}} C {
                int X {
                    $$
                }
            }
            """;
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
            $$"""
            {{declarationType}} C {
                int X {
                    $$ get;
                }
            }
            """;
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
            $$"""
            {{declarationType}} C {
                int this[int i] {
                    $$
                }
            }
            """;
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
            $$"""
            {{declarationType}} C {
                event System.Action E {
                    $$
                }
            }
            """;
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
    public Task SuggestReadonlyMethodInStruct()
        => VerifyItemExistsAsync("""
            struct C {
                public $$ void M() {}
            }
            """, "readonly");

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/58921"), CombinatorialData, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public async Task TestInCastExpressionThatMightBeParenthesizedExpression1(bool hasNewline)
    {

        var markup =
            $$"""
            class C
            {
                void M()
                {
                    var data = (n$$) {{(hasNewline ? Environment.NewLine : string.Empty)}} M();
                }
            }
            """;

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
            $$"""
            class C
            {
                bool Prop => (t$$)  {{(hasExpression ? "n" : string.Empty)}}
                private int n;
            }
            """;
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
    public Task SuggestRequiredInClassOrStructOrRecord(string type)
        => VerifyItemExistsAsync($$"""
            {{type}} C
            {
                $$
            """, "required");

    [Fact]
    public Task DoNotSuggestRequiredInInterface()
        => VerifyItemIsAbsentAsync($$"""
            interface I
            {
                public $$
            """, "required");

    [Theory]
    [InlineData("static")]
    [InlineData("const")]
    [InlineData("readonly")]
    public Task DoNotSuggestRequiredOnFilteredKeywordMembers(string keyword)
        => VerifyItemIsAbsentAsync($$"""
            class C 
            {
                {{keyword}} $$
            """, "required");

    [Theory]
    [InlineData("static")]
    [InlineData("const")]
    [InlineData("readonly")]
    public Task DoNotSuggestFilteredKeywordsOnRequiredMembers(string keyword)
        => VerifyItemIsAbsentAsync($$"""
            class C 
            {
                required $$
            """, keyword);

    [Fact]
    public Task DoNotSuggestRequiredOnRequiredMembers()
        => VerifyItemIsAbsentAsync($$"""
            class C 
            {
                required $$
            """, "required");

    [Fact]
    public Task SuggestFileOnTypes()
        => VerifyItemExistsAsync($$"""
            $$ class C { }
            """, "file");

    [Fact]
    public Task DoNotSuggestFileAfterFile()
        => VerifyItemIsAbsentAsync($$"""
            file $$
            """, "file");

    [Fact]
    public Task SuggestFileAfterReadonly()
        => VerifyItemExistsAsync($$"""
            readonly $$
            """, "file");

    [Fact]
    public Task SuggestFileBeforeFileType()
        => VerifyItemExistsAsync($$"""
            $$

            file class C { }
            """, "file");

    [Fact]
    public Task SuggestFileBeforeDelegate()
        => VerifyItemExistsAsync($$"""
            $$ delegate
            """, "file");

    [Fact]
    public Task DoNotSuggestFileOnNestedTypes()
        => VerifyItemIsAbsentAsync($$"""
            class Outer
            {
                $$ class C { }
            }
            """, "file");

    [Fact]
    public Task DoNotSuggestFileOnNonTypeMembers()
        => VerifyItemIsAbsentAsync($$"""
            class C
            {
                $$
            }
            """, "file");

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("private")]
    public Task DoNotSuggestFileAfterFilteredKeywords(string keyword)
        => VerifyItemIsAbsentAsync($$"""
            {{keyword}} $$
            """, "file");

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("private")]
    public Task DoNotSuggestFilteredKeywordsAfterFile(string keyword)
        => VerifyItemIsAbsentAsync($$"""
            file $$
            """, keyword);

    [Fact]
    public Task SuggestFileInFileScopedNamespace()
        => VerifyItemExistsAsync($$"""
            namespace NS;

            $$
            """, "file");

    [Fact]
    public Task SuggestFileInNamespace()
        => VerifyItemExistsAsync($$"""
            namespace NS
            {
                $$
            }
            """, "file");

    [Fact]
    public Task SuggestFileAfterClass()
        => VerifyItemExistsAsync($$"""
            file class C { }

            $$
            """, "file");

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
    [MemberData(nameof(TypeDeclarationKeywords))]
    public Task TestTypeDeclarationKeywordsNotAfterUsingUnsafe(string keyword)
        => VerifyItemIsAbsentAsync("using unsafe $$", keyword);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
    [MemberData(nameof(TypeDeclarationKeywords))]
    public Task TestTypeDeclarationKeywordsNotAfterUsingStaticUnsafe(string keyword)
        => VerifyItemIsAbsentAsync("using static unsafe $$", keyword);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
    [MemberData(nameof(TypeDeclarationKeywords))]
    public Task TestTypeDeclarationKeywordsNotAfterGlobalUsingUnsafe(string keyword)
        => VerifyItemIsAbsentAsync("global using unsafe $$", keyword);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
    [MemberData(nameof(TypeDeclarationKeywords))]
    public Task TestTypeDeclarationKeywordsNotAfterGlobalUsingStaticUnsafe(string keyword)
        => VerifyItemIsAbsentAsync("global using static unsafe $$", keyword);

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
