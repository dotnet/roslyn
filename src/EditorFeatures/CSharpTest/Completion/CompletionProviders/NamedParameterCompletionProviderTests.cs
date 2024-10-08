// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class NamedParameterCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(NamedParameterCompletionProvider);

    [Fact]
    public async Task SendEnterThroughToEditorTest()
    {
        const string markup = """
            class Goo
            {
                public Goo(int a = 42)
                { }

                void Bar()
                {
                    var b = new Goo($$
                }
            }
            """;

        await VerifySendEnterThroughToEnterAsync(markup, "a:", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
        await VerifySendEnterThroughToEnterAsync(markup, "a:", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: true);
        await VerifySendEnterThroughToEnterAsync(markup, "a:", sendThroughEnterOption: EnterKeyRule.Always, expected: true);
    }

    [Fact]
    public async Task CommitCharacterTest()
    {
        const string markup = """
            class Goo
            {
                public Goo(int a = 42)
                { }

                void Bar()
                {
                    var b = new Goo($$
                }
            }
            """;

        await VerifyCommonCommitCharactersAsync(markup, textTypedSoFar: "");
    }

    [Fact]
    public async Task InObjectCreation()
    {
        var markup = """
            class Goo
            {
                public Goo(int a = 42)
                { }

                void Bar()
                {
                    var b = new Goo($$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "a", displayTextSuffix: ":");
    }

    [Fact]
    public async Task InBaseConstructor()
    {
        var markup = """
            class Goo
            {
                public Goo(int a = 42)
                { }
            }

            class DogBed : Goo
            {
                public DogBed(int b) : base($$
            }
            """;

        await VerifyItemExistsAsync(markup, "a", displayTextSuffix: ":");
    }

    [Fact]
    public async Task InvocationExpression()
    {
        var markup = """
            class Goo
            {
                void Bar(int a)
                {
                    Bar($$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "a", displayTextSuffix: ":");
    }

    [Fact]
    public async Task InvocationExpressionAfterComma()
    {
        var markup = """
            class Goo
            {
                void Bar(int a, string b)
                {
                    Bar(b:"", $$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "a", displayTextSuffix: ":");
    }

    [Fact]
    public async Task ElementAccessExpression()
    {
        var markup = """
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
            """;

        await VerifyItemExistsAsync(markup, "i", displayTextSuffix: ":");
    }

    [Fact]
    public async Task PartialMethods()
    {
        var markup = """
            partial class PartialClass
            {
                static partial void Goo(int declaring);
                static partial void Goo(int implementing)
                {
                }
                static void Caller()
                {
                    Goo($$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "declaring", displayTextSuffix: ":");
        await VerifyItemIsAbsentAsync(markup, "implementing", displayTextSuffix: ":");
    }

    [Fact]
    public async Task ExtendedPartialMethods()
    {
        var markup = """
            partial class PartialClass
            {
                public static partial void Goo(int declaring);
                public static partial void Goo(int implementing)
                {
                }
                static void Caller()
                {
                    Goo($$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "declaring", displayTextSuffix: ":");
        await VerifyItemIsAbsentAsync(markup, "implementing", displayTextSuffix: ":");
    }

    [Fact]
    public async Task NotAfterColon()
    {
        var markup = """
            class Goo
            {
                void Bar(int a, string b)
                {
                    Bar(a:$$ 
                }
            }
            """;

        await VerifyNoItemsExistAsync(markup);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544292")]
    public async Task NotInCollectionInitializers()
    {
        var markup = """
            using System.Collections.Generic;
            class Goo
            {
                void Bar(List<int> integers)
                {
                    Bar(integers: new List<int> { 10, 11,$$ 12 });
                }
            }
            """;

        await VerifyNoItemsExistAsync(markup);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544191")]
    public async Task FilteringOverloadsByCallSite()
    {
        var markup = """
            class Class1
            {
                void Test()
                {
                    Goo(boolean:true, $$)
                }

                void Goo(string str = "hello", char character = 'a')
                { }

                void Goo(string str = "hello", bool boolean = false)
                { }
            }
            """;

        await VerifyItemExistsAsync(markup, "str", displayTextSuffix: ":");
        await VerifyItemIsAbsentAsync(markup, "character", displayTextSuffix: ":");
    }

    [Fact]
    public async Task DoNotFilterYet()
    {
        var markup = """
            class Class1
            {
                void Test()
                {
                    Goo(str:"", $$)
                }

                void Goo(string str = "hello", char character = 'a')
                { }

                void Goo(string str = "hello", bool boolean = false)
                { }
            }
            """;

        await VerifyItemExistsAsync(markup, "boolean", displayTextSuffix: ":");
        await VerifyItemExistsAsync(markup, "character", displayTextSuffix: ":");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544191")]
    public async Task FilteringOverloadsByCallSiteComplex()
    {
        var markup = """
            class Goo
            {
                void Test()
                {
                    Bar m = new Bar();
                    Method(obj:m, $$
                }

                void Method(Bar obj, int num = 23, string str = "")
                {
                }
                void Method(double dbl = double.MinValue, string str = "")
                {
                }
                void Method(int num = 1, bool b = false, string str = "")
                {
                }
                void Method(Bar obj, bool b = false, string str = "")
                {
                }
            }
            class Bar { }
            """;
        await VerifyItemExistsAsync(markup, "str", displayTextSuffix: ":");
        await VerifyItemExistsAsync(markup, "num", displayTextSuffix: ":");
        await VerifyItemExistsAsync(markup, "b", displayTextSuffix: ":");
        await VerifyItemIsAbsentAsync(markup, "dbl", displayTextSuffix: ":");
    }

    [Fact]
    public async Task MethodOverloads()
    {
        var markup = """
            class Goo
            {
                void Test()
                {
                    object m = null;
                    Method(m, $$
                }

                void Method(object obj, int num = 23, string str = "")
                {
                }
                void Method(int num = 1, bool b = false, string str = "")
                {
                }
                void Method(object obj, bool b = false, string str = "")
                {
                }
            }
            """;
        await VerifyItemExistsAsync(markup, "str", displayTextSuffix: ":");
        await VerifyItemExistsAsync(markup, "num", displayTextSuffix: ":");
        await VerifyItemExistsAsync(markup, "b", displayTextSuffix: ":");
    }

    [Fact]
    public async Task ExistingNamedParamsAreFilteredOut()
    {
        var markup = """
            class Goo
            {
                void Test()
                {
                    object m = null;
                    Method(obj: m, str: "", $$);
                }

                void Method(object obj, int num = 23, string str = "")
                {
                }
                void Method(double dbl = double.MinValue, string str = "")
                {
                }
                void Method(int num = 1, bool b = false, string str = "")
                {
                }
                void Method(object obj, bool b = false, string str = "")
                {
                }
            }
            """;
        await VerifyItemExistsAsync(markup, "num", displayTextSuffix: ":");
        await VerifyItemExistsAsync(markup, "b", displayTextSuffix: ":");
        await VerifyItemIsAbsentAsync(markup, "obj", displayTextSuffix: ":");
        await VerifyItemIsAbsentAsync(markup, "str", displayTextSuffix: ":");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529369")]
    public async Task VerbatimIdentifierNotAKeyword()
    {
        var markup = """
            class Program
            {
                void Goo(int @integer)
                {
                    Goo(@i$$
                }
            }
            """;
        await VerifyItemExistsAsync(markup, "integer", displayTextSuffix: ":");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544209")]
    public async Task DescriptionStringInMethodOverloads()
    {
        var markup = """
            class Class1
            {
                void Test()
                {
                    Goo(boolean: true, $$)
                }

                void Goo(string obj = "hello")
                { }

                void Goo(bool boolean = false, Class1 obj = default(Class1))
                { }
            }
            """;
        await VerifyItemExistsAsync(markup, "obj", displayTextSuffix: ":",
            expectedDescriptionOrNull: $"({FeaturesResources.parameter}) Class1 obj = default(Class1)");
    }

    [Fact]
    public async Task InDelegates()
    {
        var markup = """
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
            }
            """;
        await VerifyItemExistsAsync(markup, "message", displayTextSuffix: ":");
    }

    [Fact]
    public async Task InDelegateInvokeSyntax()
    {
        var markup = """
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
            }
            """;
        await VerifyItemExistsAsync(markup, "message", displayTextSuffix: ":");
    }

    [Fact]
    public async Task NotInComment()
    {
        var markup = """
            public class Test
            {
            static void Main()
            {
            M(x: 0, //Hit ctrl-space at the end of this comment $$
            y: 1);
            }
            static void M(int x, int y) { }
            }
            """;
        await VerifyNoItemsExistAsync(markup);
    }

    [Fact]
    public async Task CommitWithColonWordFullyTyped()
    {
        var markup = """
            class Program
            {
                static void Main(string[] args)
                {
                    Main(args$$)
                }
            }
            """;

        var expected = """
            class Program
            {
                static void Main(string[] args)
                {
                    Main(args:)
                }
            }
            """;
        await VerifyProviderCommitAsync(markup, "args:", expected, ':');
    }

    [Fact]
    public async Task CommitWithColonWordPartiallyTyped()
    {
        var markup = """
            class Program
            {
                static void Main(string[] args)
                {
                    Main(arg$$)
                }
            }
            """;

        var expected = """
            class Program
            {
                static void Main(string[] args)
                {
                    Main(args:)
                }
            }
            """;
        await VerifyProviderCommitAsync(markup, "args:", expected, ':');
    }
}
