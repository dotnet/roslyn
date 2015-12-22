// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
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

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InObjectCreation()
        {
            var markup = @"
class MyGeneric<T> { }

void foo()
{
   MyGeneric<string> foo = new $$
}";

            await VerifyItemExistsAsync(markup, "MyGeneric<string>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInAnonymousTypeObjectCreation1()
        {
            var markup = @"
class C
{
    void M()
    {
        var x = new[] { new { Foo = ""asdf"", Bar = 1 }, new $$
    }
}";

            await VerifyItemIsAbsentAsync(markup, "<anonymous type: string Foo, int Bar>");
        }

        [WorkItem(854497)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotVoid()
        {
            var markup = @"
class C
{
    void M()
    {
        var x = new $$
    }
}";

            await VerifyItemIsAbsentAsync(markup, "void");
        }

        [WorkItem(827897)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InYieldReturn()
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
            await VerifyItemExistsAsync(markup, "FieldAccessException");
        }

        [WorkItem(827897)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InAsyncMethodReturnStatement()
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
            await VerifyItemExistsAsync(markup, "FieldAccessException");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IsCommitCharacterTest()
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

            await VerifyCommitCharactersAsync(markup, textTypedSoFar: "",
                validChars: new[] { ' ', '(', '{', '[' },
                invalidChars: new[] { 'x', ',', '#' });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IsTextualTriggerCharacterTest()
        {
            await VerifyTextualTriggerCharacterAsync("Abc$$ ", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true);
            await VerifyTextualTriggerCharacterAsync("Abc $$X", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
            await VerifyTextualTriggerCharacterAsync("Abc $$@", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
            await VerifyTextualTriggerCharacterAsync("Abc$$@", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
            await VerifyTextualTriggerCharacterAsync("Abc$$.", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SendEnterThroughToEditorTest()
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

            await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterEnabled: false, expected: false);
            await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterEnabled: true, expected: true);
        }

        [WorkItem(828196)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SuggestAlias()
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
            await VerifyItemExistsAsync(markup, "D");
        }

        [WorkItem(828196)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SuggestAlias2()
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
            await VerifyItemExistsAsync(markup, "D");
        }

        [WorkItem(1075275)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitAlias()
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
            await VerifyProviderCommitAsync(markup, "D", expected, '(', "");
        }

        [WorkItem(1090377)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterNewFollowedByAssignment()
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
            await VerifyItemExistsAsync(markup, "Location");
        }

        [WorkItem(1090377)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterNewFollowedByAssignment_GrandParentIsSimpleAssignment()
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
            await VerifyItemExistsAsync(markup, "Program");
        }

        [WorkItem(2836, "https://github.com/dotnet/roslyn/issues/2836")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterNewFollowedBySimpleAssignment_GrandParentIsEqualsValueClause()
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
            await VerifyItemExistsAsync(markup, "Program");
        }

        [WorkItem(2836, "https://github.com/dotnet/roslyn/issues/2836")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterNewFollowedByCompoundAssignment_GrandParentIsEqualsValueClause()
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
            await VerifyItemExistsAsync(markup, "Program");
        }

        [WorkItem(2836, "https://github.com/dotnet/roslyn/issues/2836")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterNewFollowedByCompoundAssignment_GrandParentIsEqualsValueClause2()
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
            await VerifyItemExistsAsync(markup, "Program");
        }

        [WorkItem(4115, "https://github.com/dotnet/roslyn/issues/4115")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitObjectWithParenthesis1()
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

            await VerifyProviderCommitAsync(markup, "object", expected, '(', "");
        }

        [WorkItem(4115, "https://github.com/dotnet/roslyn/issues/4115")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitObjectWithParenthesis2()
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

            await VerifyProviderCommitAsync(markup, "object", expected, '(', "");
        }

        [WorkItem(4115, "https://github.com/dotnet/roslyn/issues/4115")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DontCommitObjectWithOpenBrace1()
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

            await VerifyProviderCommitAsync(markup, "object", expected, '{', "");
        }

        [WorkItem(4115, "https://github.com/dotnet/roslyn/issues/4115")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DontCommitObjectWithOpenBrace2()
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

            await VerifyProviderCommitAsync(markup, "object", expected, '{', "");
        }

        [WorkItem(4310, "https://github.com/dotnet/roslyn/issues/4310")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InExpressionBodiedProperty()
        {
            var markup =
@"class C
{
    object Object => new $$
}
";
            await VerifyItemExistsAsync(markup, "object");
        }

        [WorkItem(4310, "https://github.com/dotnet/roslyn/issues/4310")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InExpressionBodiedMethod()
        {
            var markup =
@"class C
{
    object GetObject() => new $$
}
";
            await VerifyItemExistsAsync(markup, "object");
        }
    }
}
