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
    public class NamedParameterCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public NamedParameterCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new NamedParameterCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SendEnterThroughToEditorTest()
        {
            const string markup = @"
class Foo
{
    public Foo(int a = 42)
    { }

    void Bar()
    {
        var b = new Foo($$
    }
}";

            await VerifySendEnterThroughToEnterAsync(markup, "a:", sendThroughEnterEnabled: false, expected: false);
            await VerifySendEnterThroughToEnterAsync(markup, "a:", sendThroughEnterEnabled: true, expected: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitCharacterTest()
        {
            const string markup = @"
class Foo
{
    public Foo(int a = 42)
    { }

    void Bar()
    {
        var b = new Foo($$
    }
}";

            await VerifyCommonCommitCharactersAsync(markup, textTypedSoFar: "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InObjectCreation()
        {
            var markup = @"
class Foo
{
    public Foo(int a = 42)
    { }

    void Bar()
    {
        var b = new Foo($$
    }
}";

            await VerifyItemExistsAsync(markup, "a:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InBaseConstructor()
        {
            var markup = @"
class Foo
{
    public Foo(int a = 42)
    { }
}

class DogBed : Foo
{
    public DogBed(int b) : base($$
}
";

            await VerifyItemExistsAsync(markup, "a:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvocationExpression()
        {
            var markup = @"
class Foo
{
    void Bar(int a)
    {
        Bar($$
    }
}
";

            await VerifyItemExistsAsync(markup, "a:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvocationExpressionAfterComma()
        {
            var markup = @"
class Foo
{
    void Bar(int a, string b)
    {
        Bar(b:"""", $$
    }
}
";

            await VerifyItemExistsAsync(markup, "a:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ElementAccessExpression()
        {
            var markup = @"
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get
        {
            return arr[i];
        }
        set
        {
            arr[i] = value;
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[$$
    }
}
";

            await VerifyItemExistsAsync(markup, "i:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialMethods()
        {
            var markup = @"
partial class PartialClass
{
    static partial void Foo(int declaring);
    static partial void Foo(int implementing)
    {
    }
    static void Caller()
    {
        Foo($$
    }
}
";

            await VerifyItemExistsAsync(markup, "declaring:");
            await VerifyItemIsAbsentAsync(markup, "implementing:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterColon()
        {
            var markup = @"
class Foo
{
    void Bar(int a, string b)
    {
        Bar(a:$$ 
    }
}
";

            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(544292)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInCollectionInitializers()
        {
            var markup = @"
using System.Collections.Generic;
class Foo
{
    void Bar(List<int> integers)
    {
        Bar(integers: new List<int> { 10, 11,$$ 12 });
    }
}
";

            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(544191)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FilteringOverloadsByCallSite()
        {
            var markup = @"
class Class1
{
    void Test()
    {
        Foo(boolean:true, $$)
    }
 
    void Foo(string str = ""hello"", char character = 'a')
    { }
 
    void Foo(string str = ""hello"", bool boolean = false)
    { }
}
";

            await VerifyItemExistsAsync(markup, "str:");
            await VerifyItemIsAbsentAsync(markup, "character:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DontFilterYet()
        {
            var markup = @"
class Class1
{
    void Test()
    {
        Foo(str:"""", $$)
    }
 
    void Foo(string str = ""hello"", char character = 'a')
    { }
 
    void Foo(string str = ""hello"", bool boolean = false)
    { }
}
";

            await VerifyItemExistsAsync(markup, "boolean:");
            await VerifyItemExistsAsync(markup, "character:");
        }

        [WorkItem(544191)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FilteringOverloadsByCallSiteComplex()
        {
            var markup = @"
class Foo
{
    void Test()
    {
        Bar m = new Bar();
        Method(obj:m, $$
    }

    void Method(Bar obj, int num = 23, string str = """")
    {
    }
    void Method(double dbl = double.MinValue, string str = """")
    {
    }
    void Method(int num = 1, bool b = false, string str = """")
    {
    }
    void Method(Bar obj, bool b = false, string str = """")
    {
    }
}
class Bar { }
";
            await VerifyItemExistsAsync(markup, "str:");
            await VerifyItemExistsAsync(markup, "num:");
            await VerifyItemExistsAsync(markup, "b:");
            await VerifyItemIsAbsentAsync(markup, "dbl:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodOverloads()
        {
            var markup = @"
class Foo
{
    void Test()
    {
        object m = null;
        Method(m, $$
    }

    void Method(object obj, int num = 23, string str = """")
    {
    }
    void Method(int num = 1, bool b = false, string str = """")
    {
    }
    void Method(object obj, bool b = false, string str = """")
    {
    }
}
";
            await VerifyItemExistsAsync(markup, "str:");
            await VerifyItemExistsAsync(markup, "num:");
            await VerifyItemExistsAsync(markup, "b:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExistingNamedParamsAreFilteredOut()
        {
            var markup = @"
class Foo
{
    void Test()
    {
        object m = null;
        Method(obj: m, str: """", $$);
    }

    void Method(object obj, int num = 23, string str = """")
    {
    }
    void Method(double dbl = double.MinValue, string str = """")
    {
    }
    void Method(int num = 1, bool b = false, string str = """")
    {
    }
    void Method(object obj, bool b = false, string str = """")
    {
    }
}
";
            await VerifyItemExistsAsync(markup, "num:");
            await VerifyItemExistsAsync(markup, "b:");
            await VerifyItemIsAbsentAsync(markup, "obj:");
            await VerifyItemIsAbsentAsync(markup, "str:");
        }

        [WorkItem(529369)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VerbatimIdentifierNotAKeyword()
        {
            var markup = @"
class Program
{
    void Foo(int @integer)
    {
        Foo(@i$$
    }
}
";
            await VerifyItemExistsAsync(markup, "integer:");
        }

        [WorkItem(544209)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DescriptionStringInMethodOverloads()
        {
            var markup = @"
class Class1
{
    void Test()
    {
        Foo(boolean: true, $$)
    }
 
    void Foo(string obj = ""hello"")
    { }
 
    void Foo(bool boolean = false, Class1 obj = default(Class1))
    { }
}
";
            await VerifyItemExistsAsync(markup, "obj:",
                expectedDescriptionOrNull: $"({FeaturesResources.Parameter}) Class1 obj = default(Class1)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InDelegates()
        {
            var markup = @"
public delegate void Del(string message);

class Program
{
    public static void DelegateMethod(string message)
    {
        System.Console.WriteLine(message);
    }

    static void Main(string[] args)
    {
        Del handler = DelegateMethod;
        handler($$
    }
}";
            await VerifyItemExistsAsync(markup, "message:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InDelegateInvokeSyntax()
        {
            var markup = @"
public delegate void Del(string message);

class Program
{
    public static void DelegateMethod(string message)
    {
        System.Console.WriteLine(message);
    }

    static void Main(string[] args)
    {
        Del handler = DelegateMethod;
        handler.Invoke($$
    }
}";
            await VerifyItemExistsAsync(markup, "message:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInComment()
        {
            var markup = @"
public class Test
{
static void Main()
{
M(x: 0, //Hit ctrl-space at the end of this comment $$
y: 1);
}
static void M(int x, int y) { }
}
";
            await VerifyNoItemsExistAsync(markup);
        }
    }
}
