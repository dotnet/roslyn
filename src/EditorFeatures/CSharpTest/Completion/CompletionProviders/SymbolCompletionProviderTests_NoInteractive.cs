// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources
{
    public class SymbolCompletionProviderTests_NoInteractive : AbstractCSharpCompletionProviderTests
    {
        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new SymbolCompletionProvider();
        }

        protected override void VerifyWorker(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, bool experimental, int? glyph)
        {
            base.VerifyWorker(code, position, expectedItemOrNull, expectedDescriptionOrNull, SourceCodeKind.Regular, usePreviousCharAsTrigger, checkForAbsence, experimental, glyph);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IsCommitCharacterTest()
        {
            VerifyCommonCommitCharacters("class C { void M() { System.Console.$$", textTypedSoFar: "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IsTextualTriggerCharacterTest()
        {
            TestCommonIsTextualTriggerCharacter();

            VerifyTextualTriggerCharacter("Abc $$X", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc$$ ", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SendEnterThroughToEditorTest()
        {
            VerifySendEnterThroughToEnter("class C { void M() { System.Console.$$", "Beep", sendThroughEnterEnabled: false, expected: false);
            VerifySendEnterThroughToEnter("class C { void M() { System.Console.$$", "Beep", sendThroughEnterEnabled: true, expected: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation1()
        {
            VerifyItemIsAbsent(@"System.Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation2()
        {
            VerifyItemIsAbsent(@"using System;
Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation3()
        {
            VerifyItemIsAbsent(@"using System.Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation4()
        {
            VerifyItemIsAbsent(@"class C {
#if false 
System.Console.$$
#endif", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation5()
        {
            VerifyItemIsAbsent(@"class C {
#if true 
System.Console.$$
#endif", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation6()
        {
            VerifyItemIsAbsent(@"using System;

class C {
// Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation7()
        {
            VerifyItemIsAbsent(@"using System;

class C {
/*  Console.$$   */", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation8()
        {
            VerifyItemIsAbsent(@"using System;

class C {
/// Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation9()
        {
            VerifyItemIsAbsent(@"using System;

class C {
    void Method()
    {
        /// Console.$$
    }
}", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation10()
        {
            VerifyItemIsAbsent(@"using System;

class C {
    void Method()
    {
        /**  Console.$$   */", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation11()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", AddInsideMethod("string s = \"Console.$$")), @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation12()
        {
            VerifyItemIsAbsent(@"[assembly: System.Console.$$]", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation13()
        {
            var content = @"[Console.$$]
class CL {}";

            VerifyItemIsAbsent(AddUsingDirectives("using System;", content), @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation14()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<[Console.$$]T> {}"), @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation15()
        {
            var content = @"class CL {
    [Console.$$]
    void Method() {}
}";
            VerifyItemIsAbsent(AddUsingDirectives("using System;", content), @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation16()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<Console.$$"), @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation17()
        {
            VerifyItemIsAbsent(@"using System;

class Program {
    static void Main(string[] args)
    {
        string a = ""a$$
    }
}", @"Main");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation18()
        {
            VerifyItemIsAbsent(@"using System;

class Program {
    static void Main(string[] args)
    {
        #region
        #endregion // a$$
    }
}", @"Main");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidLocation19()
        {
            VerifyItemIsAbsent(@"using System;

class Program {
    static void Main(string[] args)
    {
        //s$$
    }
}", @"SByte");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InsideMethodBody()
        {
            VerifyItemExists(@"using System;

class C {
    void Method()
    {
        Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingDirectiveGlobal()
        {
            VerifyItemExists(@"using global::$$;", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InsideAccessor()
        {
            VerifyItemExists(@"using System;

class C {
    string Property
    {
        get 
        {
            Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FieldInitializer()
        {
            VerifyItemExists(@"using System;

class C {
    int i = Console.$$", @"Beep");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FieldInitializer2()
        {
            VerifyItemExists(@"
class C {
    object i = $$", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ImportedProperty()
        {
            VerifyItemExists(@"using System.Collections.Generic;

class C {
    void Method()
    {
       new List<string>().$$", @"Capacity");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FieldInitializerWithProperty()
        {
            VerifyItemExists(@"using System.Collections.Generic;
class C {
    int i =  new List<string>().$$", @"Count");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticMethods()
        {
            VerifyItemExists(@"using System;

class C {
    private static int Method() {}

    int i = $$
", @"Method");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EndOfFile()
        {
            VerifyItemExists(@"static class E { public static void Method() { E.$$", @"Method");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedStaticFields()
        {
            var code = @"class A { public static int X; }
class B : A { public static int Y; }
class C { void M() { B.$$ } }
";
            VerifyItemExists(code, "X");
            VerifyItemExists(code, "Y");
        }
    }
}
