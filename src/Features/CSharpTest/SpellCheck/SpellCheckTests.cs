// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.SpellCheck;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.SpellCheck;

[Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
public sealed class SpellCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public SpellCheckTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpSpellCheckCodeFixProvider());

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    [Fact]
    public Task TestNoSpellcheckForIfOnly2Characters()
        => TestMissingInRegularAndScriptAsync("""
            class Goo
            {
                void Bar()
                {
                    var a = new [|Fo|]
                }
            }
            """);

    [Fact]
    public Task TestAfterNewExpression()
        => TestExactActionSetOfferedAsync("""
            class Goo
            {
                void Bar()
                {
                    void a = new [|Gooa|].ToString();
                }
            }
            """, [String.Format(FeaturesResources.Change_0_to_1, "Gooa", "Goo")]);

    [Fact]
    public Task TestInLocalType()
        => TestExactActionSetOfferedAsync("""
            class Foo
            {
                void Bar()
                {
                    [|Foa|] a;
                }
            }
            """,
        [
            String.Format(FeaturesResources.Change_0_to_1, "Foa", "Foo"),
            String.Format(FeaturesResources.Change_0_to_1, "Foa", "for")
        ]);

    [Fact]
    public Task TestInFunc()
        => TestExactActionSetOfferedAsync("""
            using System;

            class Goo
            {
                void Bar(Func<[|Goa|]> f)
                {
                }
            }
            """,
            [String.Format(FeaturesResources.Change_0_to_1, "Goa", "Goo")]);

    [Fact]
    public Task TestInExpression()
        => TestExactActionSetOfferedAsync("""
            class Program
            {
                void Main(string[] args)
                {
                    var zzz = 2;
                    var  y = 2 + [|zza|];
                }
            }
            """, [String.Format(FeaturesResources.Change_0_to_1, "zza", "zzz")]);

    [Fact]
    public Task TestInTypeOfIsExpression()
        => TestExactActionSetOfferedAsync("""
            using System;
            public class Class1
            {
                void F()
                {
                    if (x is [|Boolea|]) {}
                }
            }
            """,
        [
            String.Format(FeaturesResources.Change_0_to_1, "Boolea", "Boolean"),
            String.Format(FeaturesResources.Change_0_to_1, "Boolea", "bool")
        ]);

    [Fact]
    public Task TestInvokeCorrectIdentifier()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                void Main(string[] args)
                {
                    var zzz = 2;
                    var y = 2 + [|zza|];
                }
            }
            """, """
            class Program
            {
                void Main(string[] args)
                {
                    var zzz = 2;
                    var y = 2 + zzz;
                }
            }
            """);

    [Fact]
    public Task TestAfterDot()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    Program.[|Mair|]
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    Program.Main
                }
            }
            """);

    [Fact]
    public Task TestNotInaccessibleProperty()
        => TestMissingInRegularAndScriptAsync("""
            class Program
            {
                void Main(string[] args)
                {
                    var z = new c().[|membr|]
                }
            }

            class c
            {
                protected int member { get; }
            }
            """);

    [Fact]
    public Task TestGenericName1()
        => TestInRegularAndScriptAsync("""
            class Goo<T>
            {
                private [|Goo2|]<T> x;
            }
            """, """
            class Goo<T>
            {
                private Goo<T> x;
            }
            """);

    [Fact]
    public Task TestGenericName2()
        => TestInRegularAndScriptAsync("""
            class Goo<T>
            {
                private [|Goo2|] x;
            }
            """, """
            class Goo<T>
            {
                private Goo x;
            }
            """);

    [Fact]
    public Task TestQualifiedName1()
        => TestInRegularAndScriptAsync("""
            class Program
            {
               private object x = new [|Goo2|].Bar
            }

            class Goo
            {
                class Bar
                {
                }
            }
            """, """
            class Program
            {
               private object x = new Goo.Bar
            }

            class Goo
            {
                class Bar
                {
                }
            }
            """);

    [Fact]
    public Task TestQualifiedName2()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                private object x = new Goo.[|Ba2|]
            }

            class Goo
            {
                public class Bar
                {
                }
            }
            """, """
            class Program
            {
                private object x = new Goo.Bar
            }

            class Goo
            {
                public class Bar
                {
                }
            }
            """);

    [Fact]
    public Task TestMiddleOfDottedExpression()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                void Main(string[] args)
                {
                    var z = new c().[|membr|].ToString();
                }
            }

            class c
            {
                public int member { get; }
            }
            """, """
            class Program
            {
                void Main(string[] args)
                {
                    var z = new c().member.ToString();
                }
            }

            class c
            {
                public int member { get; }
            }
            """);

    [Fact]
    public Task TestNotForOverloadResolutionFailure()
        => TestMissingInRegularAndScriptAsync("""
            class Program
            {
                void Main(string[] args)
                {
                }

                void Goo()
                {
                    [|Method|]();
                }

                int Method(int argument)
                {
                }
            }
            """);

    [Fact]
    public Task TestHandlePredefinedTypeKeywordCorrectly()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Main(string[] args)
                {
                    [|Int3|] i;
                }
            }
            """, """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Main(string[] args)
                {
                    int i;
                }
            }
            """);

    [Fact]
    public Task TestHandlePredefinedTypeKeywordCorrectly1()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Main(string[] args)
                {
                    [|Int3|] i;
                }
            }
            """, """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Main(string[] args)
                {
                    Int32 i;
                }
            }
            """, index: 1);

    [Fact]
    public Task TestOnGeneric()
        => TestInRegularAndScriptAsync("""
            interface Enumerable<T>
            {
            }

            class C
            {
                void Main(string[] args)
                {
                    [|IEnumerable|]<int> x;
                }
            }
            """, """
            interface Enumerable<T>
            {
            }

            class C
            {
                void Main(string[] args)
                {
                    Enumerable<int> x;
                }
            }
            """);

    [Fact]
    public Task TestTestObjectConstruction()
        => TestInRegularAndScriptAsync(
            """
            class AwesomeClass
            {
                void M()
                {
                    var goo = new [|AwesomeClas()|];
                }
            }
            """,
            """
            class AwesomeClass
            {
                void M()
                {
                    var goo = new AwesomeClass();
                }
            }
            """);

    [Fact]
    public Task TestTestMissingName()
        => TestMissingInRegularAndScriptAsync(
@"[assembly: Microsoft.CodeAnalysis.[||]]");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12990")]
    public Task TestTrivia1()
        => TestInRegularAndScriptAsync("""
            using System.Text;
            class C
            {
              void M()
              {
                /*leading*/ [|stringbuilder|] /*trailing*/ sb = null;
              }
            }
            """, """
            using System.Text;
            class C
            {
              void M()
              {
                /*leading*/ StringBuilder /*trailing*/ sb = null;
              }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13345")]
    public Task TestNotMissingOnKeywordWhichIsAlsoASnippet()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    // here 'for' is a keyword and snippet, so we should offer to spell check to it.
                    [|foo|];
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    // here 'for' is a keyword and snippet, so we should offer to spell check to it.
                    for;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18626")]
    public Task TestForExplicitInterfaceTypeName()
        => TestInRegularAndScriptAsync(
            """
            interface IProjectConfigurationsService
            {
                void Method();
            }

            class Program : IProjectConfigurationsService
            {
                void [|IProjectConfigurationService|].Method()
                {

                }
            }
            """,
            """
            interface IProjectConfigurationsService
            {
                void Method();
            }

            class Program : IProjectConfigurationsService
            {
                void IProjectConfigurationsService.Method()
                {

                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13345")]
    public Task TestMissingOnKeywordWhichIsOnlyASnippet()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    // here 'for' is *only* a snippet, and we should not offer to spell check to it.
                    var v = [|goo|];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15733")]
    public Task TestMissingOnVar()
        => TestMissingInRegularAndScriptAsync(
            """
            namespace bar { }

            class C
            {
                void M()
                {
                    var y =
                    [|var|]
                }
            }
            """);

    [Fact]
    public Task TestUnmanagedConstraint()
        => TestInRegularAndScriptAsync(
            """
            class C<T> where T : [|umanaged|]
            {
            }
            """,
            """
            class C<T> where T : unmanaged
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28244")]
    public Task TestMisspelledConstructor()
        => TestInRegularAndScriptAsync(
            """
            public class SomeClass
            {
                public [|SomeClss|]() { }
            }
            """,
            """
            public class SomeClass
            {
                public SomeClass() { }
            }
            """);

    [Fact]
    public Task TestInExplicitInterfaceImplementation1()
        => TestMissingInRegularAndScriptAsync("""
            using System;

            class Program : IDisposable
            {
                void IDisposable.[|Dspose|]
            }
            """);

    [Fact]
    public Task TestInExplicitInterfaceImplementation2()
        => TestMissingInRegularAndScriptAsync("""
            using System;

            interface IInterface
            {
                void Generic<K, V>();
            }

            class Program : IInterface
            {
                void IInterface.[|Generi|]
            }
            """);

    [Fact]
    public Task TestInExplicitInterfaceImplementation3()
        => TestMissingInRegularAndScriptAsync("""
            using System;

            interface IInterface
            {
                int this[int i] { get; }
            }

            class Program : IInterface
            {
                void IInterface.[|thi|]
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1640728")]
    public Task TestMisspelledWordThatIsAlsoSnippetName()
        => TestInRegularAndScriptAsync(
            """
            public [|interfacce|] IWhatever
            {
            }
            """,
            """
            public interface IWhatever
            {
            }
            """);
}
