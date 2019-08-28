// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources
{
    public class SymbolCompletionProviderTests_NoInteractive : AbstractCSharpCompletionProviderTests
    {
        public SymbolCompletionProviderTests_NoInteractive(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new SymbolCompletionProvider();
        }

        private protected override Task VerifyWorkerAsync(
            string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription, List<CompletionItemFilter> matchingFilters)
        {
            return base.VerifyWorkerAsync(code, position,
                expectedItemOrNull, expectedDescriptionOrNull,
                SourceCodeKind.Regular, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                inlineDescription, matchingFilters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IsCommitCharacterTest()
        {
            await VerifyCommonCommitCharactersAsync("class C { void M() { System.Console.$$", textTypedSoFar: "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IsTextualTriggerCharacterTest()
        {
            TestCommonIsTextualTriggerCharacter();

            VerifyTextualTriggerCharacter("Abc $$X", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc$$ ", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SendEnterThroughToEditorTest()
        {
            await VerifySendEnterThroughToEnterAsync("class C { void M() { System.Console.$$", "Beep", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
            await VerifySendEnterThroughToEnterAsync("class C { void M() { System.Console.$$", "Beep", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: true);
            await VerifySendEnterThroughToEnterAsync("class C { void M() { System.Console.$$", "Beep", sendThroughEnterOption: EnterKeyRule.Always, expected: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation1()
        {
            await VerifyItemIsAbsentAsync(@"System.Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation2()
        {
            await VerifyItemIsAbsentAsync(@"using System;
Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation3()
        {
            await VerifyItemIsAbsentAsync(@"using System.Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation4()
        {
            await VerifyItemIsAbsentAsync(@"class C {
#if false 
System.Console.$$
#endif", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation5()
        {
            await VerifyItemIsAbsentAsync(@"class C {
#if true 
System.Console.$$
#endif", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation6()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
// Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation7()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/*  Console.$$   */", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation8()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/// Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation9()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
    void Method()
    {
        /// Console.$$
    }
}", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation10()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
    void Method()
    {
        /**  Console.$$   */", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation11()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("string s = \"Console.$$")), @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation12()
        {
            await VerifyItemIsAbsentAsync(@"[assembly: System.Console.$$]", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation13()
        {
            var content = @"[Console.$$]
class CL {}";

            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation14()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<[Console.$$]T> {}"), @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation15()
        {
            var content = @"class CL {
    [Console.$$]
    void Method() {}
}";
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation16()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<Console.$$"), @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation17()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class Program {
    static void Main(string[] args)
    {
        string a = ""a$$
    }
}", @"Main");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation18()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class Program {
    static void Main(string[] args)
    {
        #region
        #endregion // a$$
    }
}", @"Main");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvalidLocation19()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class Program {
    static void Main(string[] args)
    {
        //s$$
    }
}", @"SByte");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsideMethodBody()
        {
            await VerifyItemExistsAsync(@"using System;

class C {
    void Method()
    {
        Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingDirectiveGlobal()
        {
            await VerifyItemExistsAsync(@"using global::$$;", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsideAccessor()
        {
            await VerifyItemExistsAsync(@"using System;

class C {
    string Property
    {
        get 
        {
            Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FieldInitializer()
        {
            await VerifyItemExistsAsync(@"using System;

class C {
    int i = Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FieldInitializer2()
        {
            await VerifyItemExistsAsync(@"
class C {
    object i = $$", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ImportedProperty()
        {
            await VerifyItemExistsAsync(@"using System.Collections.Generic;

class C {
    void Method()
    {
       new List<string>().$$", @"Capacity");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FieldInitializerWithProperty()
        {
            await VerifyItemExistsAsync(@"using System.Collections.Generic;
class C {
    int i =  new List<string>().$$", @"Count");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticMethods()
        {
            await VerifyItemExistsAsync(@"using System;

class C {
    private static int Method() {}

    int i = $$
", @"Method");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EndOfFile()
        {
            await VerifyItemExistsAsync(@"static class E { public static void Method() { E.$$", @"Method");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InheritedStaticFields()
        {
            var code = @"class A { public static int X; }
class B : A { public static int Y; }
class C { void M() { B.$$ } }
";
            await VerifyItemExistsAsync(code, "X");
            await VerifyItemExistsAsync(code, "Y");
        }

        [WorkItem(209299, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=209299")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestDescriptionWhenDocumentLengthChanges()
        {
            var code = @"using System;

class C 
{
    string Property
    {
        get 
        {
            Console.$$";//, @"Beep"

            using var workspace = TestWorkspace.CreateCSharp(code);
            var testDocument = workspace.Documents.Single();
            var position = testDocument.CursorPosition.Value;

            var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
            var service = CompletionService.GetService(document);
            var completions = await service.GetCompletionsAsync(document, position);

            var item = completions.Items.First(i => i.DisplayText == "Beep");
            var edit = testDocument.GetTextBuffer().CreateEdit();
            edit.Delete(Span.FromBounds(position - 10, position));
            edit.Apply();

            var currentDocument = workspace.CurrentSolution.GetDocument(testDocument.Id);

            Assert.NotEqual(currentDocument, document);
            var description = service.GetDescriptionAsync(document, item);
        }
    }
}
