// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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
        public async Task IsTextualTriggerCharacterTest()
        {
            await TestCommonIsTextualTriggerCharacterAsync();
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

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyWillPopUpAfterRefInMethodParameterModifiers_Methods()
        {
            var code = @"
class Program
{
    public static void Test(ref $$ p) { }
}";

            await VerifyItemExistsAsync(code, "readonly");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyWillPopUpAfterRefInMethodParameterModifiers_SecondParameter()
        {
            var code = @"
class Program
{
    public static void Test(int p1, ref $$ p2) { }
}";

            await VerifyItemExistsAsync(code, "readonly");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyWillPopUpAfterRefInMethodParameterModifiers_Delegates()
        {
            var code = @"
public delegate int Delegate(ref $$ int p);";

            await VerifyItemExistsAsync(code, "readonly");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyWillPopUpAfterRefInMethodParameterModifiers_LocalFunctions()
        {
            var code = @"
class Program
{
    public static void Test()
    {
        void localFunc(ref $$ int p) { }
    }
}";

            await VerifyItemExistsAsync(code, "readonly");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyWillPopUpAfterRefInMethodParameterModifiers_LambdaExpressions()
        {
            var code = @"
public delegate int Delegate(ref readonly int p);

class Program
{
    public static void Test()
    {
        Delegate lambda = (ref $$ int p) => p;
    }
}";

            await VerifyItemExistsAsync(code, "readonly");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyWillPopUpAfterRefInMethodParameterModifiers_AnonymousMethods()
        {
            var code = @"
public delegate int Delegate(ref readonly int p);

class Program
{
    public static void Test()
    {
        Delegate anonymousDelegate = delegate (ref $$ int p) { return p; };
    }
}";

            await VerifyItemExistsAsync(code, "readonly");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InWillPopUpAsParameterModifier_Methods()
        {
            var code = @"
class Program
{
    public static void Test($$ p) { }
}";

            await VerifyItemExistsAsync(code, "in");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InWillPopUpAsParameterModifier_SecondParameter()
        {
            var code = @"
class Program
{
    public static void Test(int p1, $$ p2) { }
}";

            await VerifyItemExistsAsync(code, "in");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InWillPopUpAsParameterModifier_Delegates()
        {
            var code = @"
public delegate int Delegate($$ int p);";

            await VerifyItemExistsAsync(code, "in");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InWillPopUpAsParameterModifier_LocalFunctions()
        {
            var code = @"
class Program
{
    public static void Test()
    {
        void localFunc($$ int p) { }
    }
}";

            await VerifyItemExistsAsync(code, "in");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InWillPopUpAsParameterModifier_LambdaExpressions()
        {
            var code = @"
public delegate int Delegate(ref readonly int p);

class Program
{
    public static void Test()
    {
        Delegate lambda = ($$ int p) => p;
    }
}";

            await VerifyItemExistsAsync(code, "in");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadonlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InWillPopUpAsParameterModifier_AnonymousMethods()
        {
            var code = @"
public delegate int Delegate(ref readonly int p);

class Program
{
    public static void Test()
    {
        Delegate anonymousDelegate = delegate ($$ int p) { return p; };
    }
}";

            await VerifyItemExistsAsync(code, "in");
        }
    }
}
