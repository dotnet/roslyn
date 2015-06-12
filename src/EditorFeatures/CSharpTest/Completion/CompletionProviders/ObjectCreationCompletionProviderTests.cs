// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class ObjectCreationCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override ICompletionProvider CreateCompletionProvider()
        {
            return new ObjectCreationCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InObjectCreation()
        {
            var markup = @"
class MyGeneric<T> { }

void foo()
{
   MyGeneric<string> foo = new $$
}";

            VerifyItemExists(markup, "MyGeneric<string>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInAnonymouTypeObjectCreation1()
        {
            var markup = @"
class C
{
    void M()
    {
        var x = new[] { new { Foo = ""asdf"", Bar = 1 }, new $$
    }
}";

            VerifyItemIsAbsent(markup, "<anonymous type: string Foo, int Bar>");
        }

        [WorkItem(854497)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotVoid()
        {
            var markup = @"
class C
{
    void M()
    {
        var x = new $$
    }
}";

            VerifyItemIsAbsent(markup, "void");
        }

        [WorkItem(827897)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InYieldReturn()
        {
            var markup =
@"using System;
using System.Collections.Generic;

class Program
{
    IEnumerable<FieldAccessException> M()
    {
        yield return new $$
    }
}";
            VerifyItemExists(markup, "FieldAccessException");
        }

        [WorkItem(827897)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InAsyncMethodReturnStatement()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

class Program
{
    async Task<FieldAccessException> M()
    {
        await Task.Delay(1);
        return new $$
    }
}";
            VerifyItemExists(markup, "FieldAccessException");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IsCommitCharacterTest()
        {
            var validCharacters = new[]
            {
                ' ', '(', '{', '['
            };

            var invalidCharacters = new[]
            {
                'x', ',', '#'
            };

            foreach (var ch in validCharacters)
            {
                Assert.True(CompletionProvider.IsCommitCharacter(null, ch, null), "Expected '" + ch + "' to be a commit character");
            }

            foreach (var ch in invalidCharacters)
            {
                Assert.False(CompletionProvider.IsCommitCharacter(null, ch, null), "Expected '" + ch + "' to NOT be a commit character");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IsTextualTriggerCharacterTest()
        {
            VerifyTextualTriggerCharacter("Abc$$ ", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true);
            VerifyTextualTriggerCharacter("Abc $$X", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc $$@", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc$$@", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc$$.", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SendEnterThroughToEditorTest()
        {
            VerifySendEnterThroughToEnter("Foo", "Foo", sendThroughEnterEnabled: false, expected: false);
            VerifySendEnterThroughToEnter("Foo", "Foo", sendThroughEnterEnabled: true, expected: true);
        }

        [WorkItem(828196)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestAlias()
        {
            var markup = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new $$
    }
}";
            VerifyItemExists(markup, "D");
        }

        [WorkItem(828196)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestAlias2()
        {
            var markup = @"
namespace N
{
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new $$
    }
}
}

";
            VerifyItemExists(markup, "D");
        }

        [WorkItem(1075275)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitAlias()
        {
            var markup = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new $$
    }
}";

            var expected = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  new D
    }
}";
            VerifyProviderCommit(markup, "D", expected, '(', "");
        }

        [WorkItem(1090377)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterNewFollowedByAssignment()
        {
            var markup = @"
class Location {}
enum EAB { A, B }
class Foo
{
    Location Loc {get; set;}
    EAB E {get; set;}

    void stuff()
    {
        var x = new Foo
            {
                Loc = new $$
                E = EAB.A
            };
    }
}

";
            VerifyItemExists(markup, "Location");
        }

        [WorkItem(1090377)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterNewFollowedByAssignment_GrandParentIsSimpleAssignment()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        Program p = new $$
        bool b = false;
    }
}";
            VerifyItemExists(markup, "Program");
        }

        [WorkItem(2836, "https://github.com/dotnet/roslyn/issues/2836")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterNewFollowedByAssignment_GrandParentIsEqualsValueClause()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        bool b;
        Program p = new $$
        b = false;
    }
}";
            VerifyItemExists(markup, "Program");
        }
    }
}
