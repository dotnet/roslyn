// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class ObjectCreationCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ObjectCreationCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new ObjectCreationCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InObjectCreation()
        {
            var markup = @"
class MyGeneric<T> { }

void goo()
{
   MyGeneric<string> goo = new $$
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
        var x = new[] { new { Goo = ""asdf"", Bar = 1 }, new $$
    }
}";

            await VerifyItemIsAbsentAsync(markup, "<anonymous type: string Goo, int Bar>");
        }

        [WorkItem(854497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854497")]
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

        [WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
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

        [WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
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
        public void IsTextualTriggerCharacterTest()
        {
            VerifyTextualTriggerCharacter("Abc$$ ", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true);
            VerifyTextualTriggerCharacter("Abc $$X", shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc $$@", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc$$@", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
            VerifyTextualTriggerCharacter("Abc$$.", shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
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

            await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
            await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: true);
            await VerifySendEnterThroughToEnterAsync(markup, "D", sendThroughEnterOption: EnterKeyRule.Always, expected: true);
        }

        [WorkItem(828196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
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

        [WorkItem(828196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
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

        [WorkItem(1075275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075275")]
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

        [WorkItem(1090377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090377")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterNewFollowedByAssignment()
        {
            var markup = @"
class Location {}
enum EAB { A, B }
class Goo
{
    Location Loc {get; set;}
    EAB E {get; set;}

    void stuff()
    {
        var x = new Goo
            {
                Loc = new $$
                E = EAB.A
            };
    }
}

";
            await VerifyItemExistsAsync(markup, "Location");
        }

        [WorkItem(1090377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090377")]
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

        [WorkItem(15804, "https://github.com/dotnet/roslyn/issues/15804")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BeforeAttributeParsedAsImplicitArray()
        {
            var markup =
@"class Program
{
    Program p = new $$ 

    [STAThread]
    static void Main() { }
}
";
            await VerifyItemExistsAsync(markup, "Program");
        }

        [WorkItem(14084, "https://github.com/dotnet/roslyn/issues/14084")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InMethodCallBeforeAssignment1()
        {
            var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            object o;
            string s;

            Test(new $$
            o = s;
        }
        static void Test(TimeSpan t, TimeSpan t2) { }
    }
}
";
            await VerifyItemExistsAsync(markup, "TimeSpan");
        }

        [WorkItem(14084, "https://github.com/dotnet/roslyn/issues/14084")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InMethodCallBeforeAssignment2()
        {
            var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            object o;
            string s;

            Test(new TimeSpan(), new $$
            o = s;
        }
        static void Test(TimeSpan t, TimeSpan t2) { }
    }
}
";
            await VerifyItemExistsAsync(markup, "TimeSpan");
        }

        [WorkItem(2644, "https://github.com/dotnet/roslyn/issues/2644")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InPropertyWithSameNameAsGenericTypeArgument1()
        {
            var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        public static List<Bar> Bar { get; set; }

        static void Main(string[] args)
        {
            Bar = new $$
        }
    }

    class Bar
    {
    }
}
";
            await VerifyItemExistsAsync(markup, "List<Bar>");
        }

        [WorkItem(2644, "https://github.com/dotnet/roslyn/issues/2644")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InPropertyWithSameNameAsGenericTypeArgument2()
        {
            var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        public static List<Bar> Bar { get; set; } = new $$
    }

    class Bar
    {
    }
}
";
            await VerifyItemExistsAsync(markup, "List<Bar>");
        }

        [WorkItem(2644, "https://github.com/dotnet/roslyn/issues/2644")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InPropertyWithSameNameAsGenericTypeArgument3()
        {
            var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        public static List<Bar> Bar { get; set; } => new $$
    }

    class Bar
    {
    }
}
";
            await VerifyItemExistsAsync(markup, "List<Bar>");
        }

        [WorkItem(2644, "https://github.com/dotnet/roslyn/issues/2644")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InPropertyWithSameNameAsGenericTypeArgument4()
        {
            var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        static C<A> B { get; set; }
        static C<B> A { get; set; }

        static void Main(string[] args)
        {
            B = new $$
        }
    }
    class A { }
    class B { }
    class C<T> { }
}
";
            await VerifyItemExistsAsync(markup, "C<A>");
        }

        [WorkItem(21674, "https://github.com/dotnet/roslyn/issues/21674")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropertyWithSameNameAsOtherType()
        {
            var markup =
@"namespace ConsoleApplication1
{
    class Program
    {
        static A B { get; set; }
        static B A { get; set; }

        static void Main()
        {
            B = new $$
        }
    }
    class A { }
    class B { }
}
";
            await VerifyItemExistsAsync(markup, "A");
        }
    }
}
