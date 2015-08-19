// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SendEnterThroughToEditorTest()
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

            VerifySendEnterThroughToEnter(markup, "a:", sendThroughEnterEnabled: false, expected: false);
            VerifySendEnterThroughToEnter(markup, "a:", sendThroughEnterEnabled: true, expected: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitCharacterTest()
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

            VerifyCommonCommitCharacters(markup, textTypedSoFar: "");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InObjectCreation()
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

            VerifyItemExists(markup, "a:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InBaseConstructor()
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

            VerifyItemExists(markup, "a:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvocationExpression()
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

            VerifyItemExists(markup, "a:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvocationExpressionAfterComma()
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

            VerifyItemExists(markup, "a:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ElementAccessExpression()
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

            VerifyItemExists(markup, "i:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PartialMethods()
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

            VerifyItemExists(markup, "declaring:");
            VerifyItemIsAbsent(markup, "implementing:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterColon()
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

            VerifyNoItemsExist(markup);
        }

        [WorkItem(544292)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInCollectionInitializers()
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

            VerifyNoItemsExist(markup);
        }

        [WorkItem(544191)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FilteringOverloadsByCallSite()
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

            VerifyItemExists(markup, "str:");
            VerifyItemIsAbsent(markup, "character:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DontFilterYet()
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

            VerifyItemExists(markup, "boolean:");
            VerifyItemExists(markup, "character:");
        }

        [WorkItem(544191)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FilteringOverloadsByCallSiteComplex()
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
            VerifyItemExists(markup, "str:");
            VerifyItemExists(markup, "num:");
            VerifyItemExists(markup, "b:");
            VerifyItemIsAbsent(markup, "dbl:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodOverloads()
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
            VerifyItemExists(markup, "str:");
            VerifyItemExists(markup, "num:");
            VerifyItemExists(markup, "b:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExistingNamedParamsAreFilteredOut()
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
            VerifyItemExists(markup, "num:");
            VerifyItemExists(markup, "b:");
            VerifyItemIsAbsent(markup, "obj:");
            VerifyItemIsAbsent(markup, "str:");
        }

        [WorkItem(529369)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void VerbatimIdentifierNotAKeyword()
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
            VerifyItemExists(markup, "integer:");
        }

        [WorkItem(544209)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DescriptionStringInMethodOverloads()
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
            VerifyItemExists(markup, "obj:",
                expectedDescriptionOrNull: $"({FeaturesResources.Parameter}) Class1 obj = default(Class1)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InDelegates()
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
            VerifyItemExists(markup, "message:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InDelegateInvokeSyntax()
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
            VerifyItemExists(markup, "message:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInComment()
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
            VerifyNoItemsExist(markup);
        }
    }
}
