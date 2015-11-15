// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class ObjectCreationCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ObjectCreationCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new ObjectCreationCompletionProvider();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInAnonymousTypeObjectCreation1()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IsCommitCharacterTest()
        {
            const string markup = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d = new $$
    }
}";

            VerifyCommitCharacters(markup, textTypedSoFar: "",
                validChars: new[] { ' ', '(', '{', '[' },
                invalidChars: new[] { 'x', ',', '#' });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IsTextualTriggerCharacterTest()
        {
            VerifyTextualTriggerCharacter("Abc$$ ", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true);
            VerifyTextualTriggerCharacter("Abc $$X", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc $$@", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc$$@", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc$$.", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SendEnterThroughToEditorTest()
        {
            const string markup = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d = new $$
    }
}";

            VerifySendEnterThroughToEnter(markup, "D", sendThroughEnterEnabled: false, expected: false);
            VerifySendEnterThroughToEnter(markup, "D", sendThroughEnterEnabled: true, expected: true);
        }

        [WorkItem(828196)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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
        D d=  new D(
    }
}";
            VerifyProviderCommit(markup, "D", expected, '(', "");
        }

        [WorkItem(1090377)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterNewFollowedBySimpleAssignment_GrandParentIsEqualsValueClause()
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

        [WorkItem(2836, "https://github.com/dotnet/roslyn/issues/2836")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterNewFollowedByCompoundAssignment_GrandParentIsEqualsValueClause()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        int i;
        Program p = new $$
        i += 5;
    }
}";
            VerifyItemExists(markup, "Program");
        }

        [WorkItem(2836, "https://github.com/dotnet/roslyn/issues/2836")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterNewFollowedByCompoundAssignment_GrandParentIsEqualsValueClause2()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 1000;
        Program p = new $$
        i <<= 4;
    }
}";
            VerifyItemExists(markup, "Program");
        }

        [WorkItem(4115, "https://github.com/dotnet/roslyn/issues/4115")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitObjectWithParenthesis1()
        {
            var markup = @"
class C
{
    void M1()
    {
        object o = new $$
    }
}";

            var expected = @"
class C
{
    void M1()
    {
        object o = new object(
    }
}";

            VerifyProviderCommit(markup, "object", expected, '(', "");
        }

        [WorkItem(4115, "https://github.com/dotnet/roslyn/issues/4115")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitObjectWithParenthesis2()
        {
            var markup = @"
class C
{
    void M1()
    {
        M2(new $$
    }

    void M2(object o) { }
}";

            var expected = @"
class C
{
    void M1()
    {
        M2(new object(
    }

    void M2(object o) { }
}";

            VerifyProviderCommit(markup, "object", expected, '(', "");
        }

        [WorkItem(4115, "https://github.com/dotnet/roslyn/issues/4115")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DontCommitObjectWithOpenBrace1()
        {
            var markup = @"
class C
{
    void M1()
    {
        object o = new $$
    }
}";

            var expected = @"
class C
{
    void M1()
    {
        object o = new {
    }
}";

            VerifyProviderCommit(markup, "object", expected, '{', "");
        }

        [WorkItem(4115, "https://github.com/dotnet/roslyn/issues/4115")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DontCommitObjectWithOpenBrace2()
        {
            var markup = @"
class C
{
    void M1()
    {
        M2(new $$
    }

    void M2(object o) { }
}";

            var expected = @"
class C
{
    void M1()
    {
        M2(new {
    }

    void M2(object o) { }
}";

            VerifyProviderCommit(markup, "object", expected, '{', "");
        }

        [WorkItem(4310, "https://github.com/dotnet/roslyn/issues/4310")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InExpressionBodiedProperty()
        {
            var markup =
@"class C
{
    object Object => new $$
}
";
            VerifyItemExists(markup, "object");
        }

        [WorkItem(4310, "https://github.com/dotnet/roslyn/issues/4310")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InExpressionBodiedMethod()
        {
            var markup =
@"class C
{
    object GetObject() => new $$
}
";
            VerifyItemExists(markup, "object");
        }
    }
}
