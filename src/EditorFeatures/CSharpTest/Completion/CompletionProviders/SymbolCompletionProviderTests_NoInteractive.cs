// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources
{
    public class SymbolCompletionProviderTests_NoInteractive : AbstractCSharpCompletionProviderTests
    {
        public SymbolCompletionProviderTests_NoInteractive(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new SymbolCompletionProvider();
        }

        protected override Task VerifyWorkerAsync(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, bool experimental, int? glyph)
        {
            return base.VerifyWorkerAsync(code, position, expectedItemOrNull, expectedDescriptionOrNull, SourceCodeKind.Regular, usePreviousCharAsTrigger, checkForAbsence, experimental, glyph);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IsCommitCharacterTest()
        {
            await VerifyCommonCommitCharactersAsync("class C { void M() { System.Console.$$", textTypedSoFar: "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IsTextualTriggerCharacterTest()
        {
            await TestCommonIsTextualTriggerCharacterAsync();

            await VerifyTextualTriggerCharacterAsync("Abc $$X", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
            await VerifyTextualTriggerCharacterAsync("Abc$$ ", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SendEnterThroughToEditorTest()
        {
            await VerifySendEnterThroughToEnterAsync("class C { void M() { System.Console.$$", "Beep", sendThroughEnterEnabled: false, expected: false);
            await VerifySendEnterThroughToEnterAsync("class C { void M() { System.Console.$$", "Beep", sendThroughEnterEnabled: true, expected: true);
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
    }
}
