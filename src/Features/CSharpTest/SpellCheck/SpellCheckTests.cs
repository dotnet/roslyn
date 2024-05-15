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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.SpellCheck
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
    public class SpellCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
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
        public async Task TestNoSpellcheckForIfOnly2Characters()
        {
            var text =
@"class Goo
{
    void Bar()
    {
        var a = new [|Fo|]
    }
}";
            await TestMissingInRegularAndScriptAsync(text);
        }

        [Fact]
        public async Task TestAfterNewExpression()
        {
            var text =
@"class Goo
{
    void Bar()
    {
        void a = new [|Gooa|].ToString();
    }
}";

            await TestExactActionSetOfferedAsync(text, [String.Format(FeaturesResources.Change_0_to_1, "Gooa", "Goo")]);
        }

        [Fact]
        public async Task TestInLocalType()
        {
            var text = @"class Foo
{
    void Bar()
    {
        [|Foa|] a;
    }
}";

            await TestExactActionSetOfferedAsync(text,
            [
                String.Format(FeaturesResources.Change_0_to_1, "Foa", "Foo"),
                String.Format(FeaturesResources.Change_0_to_1, "Foa", "for")
            ]);
        }

        [Fact]
        public async Task TestInFunc()
        {
            var text = @"
using System;

class Goo
{
    void Bar(Func<[|Goa|]> f)
    {
    }
}";
            await TestExactActionSetOfferedAsync(text,
                [String.Format(FeaturesResources.Change_0_to_1, "Goa", "Goo")]);
        }

        [Fact]
        public async Task TestInExpression()
        {
            var text = @"class Program
{
    void Main(string[] args)
    {
        var zzz = 2;
        var  y = 2 + [|zza|];
    }
}";
            await TestExactActionSetOfferedAsync(text, [String.Format(FeaturesResources.Change_0_to_1, "zza", "zzz")]);
        }

        [Fact]
        public async Task TestInTypeOfIsExpression()
        {
            var text = @"using System;
public class Class1
{
    void F()
    {
        if (x is [|Boolea|]) {}
    }
}";
            await TestExactActionSetOfferedAsync(text,
            [
                String.Format(FeaturesResources.Change_0_to_1, "Boolea", "Boolean"),
                String.Format(FeaturesResources.Change_0_to_1, "Boolea", "bool")
            ]);
        }

        [Fact]
        public async Task TestInvokeCorrectIdentifier()
        {
            var text = @"class Program
{
    void Main(string[] args)
    {
        var zzz = 2;
        var y = 2 + [|zza|];
    }
}";

            var expected = @"class Program
{
    void Main(string[] args)
    {
        var zzz = 2;
        var y = 2 + zzz;
    }
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestAfterDot()
        {
            var text = @"class Program
{
    static void Main(string[] args)
    {
        Program.[|Mair|]
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        Program.Main
    }
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestNotInaccessibleProperty()
        {
            var text = @"class Program
{
    void Main(string[] args)
    {
        var z = new c().[|membr|]
    }
}

class c
{
    protected int member { get; }
}";

            await TestMissingInRegularAndScriptAsync(text);
        }

        [Fact]
        public async Task TestGenericName1()
        {
            var text = @"class Goo<T>
{
    private [|Goo2|]<T> x;
}";

            var expected = @"class Goo<T>
{
    private Goo<T> x;
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestGenericName2()
        {
            var text = @"class Goo<T>
{
    private [|Goo2|] x;
}";

            var expected = @"class Goo<T>
{
    private Goo x;
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestQualifiedName1()
        {
            var text = @"class Program
{
   private object x = new [|Goo2|].Bar
}

class Goo
{
    class Bar
    {
    }
}";

            var expected = @"class Program
{
   private object x = new Goo.Bar
}

class Goo
{
    class Bar
    {
    }
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestQualifiedName2()
        {
            var text = @"class Program
{
    private object x = new Goo.[|Ba2|]
}

class Goo
{
    public class Bar
    {
    }
}";

            var expected = @"class Program
{
    private object x = new Goo.Bar
}

class Goo
{
    public class Bar
    {
    }
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestMiddleOfDottedExpression()
        {
            var text = @"class Program
{
    void Main(string[] args)
    {
        var z = new c().[|membr|].ToString();
    }
}

class c
{
    public int member { get; }
}";

            var expected = @"class Program
{
    void Main(string[] args)
    {
        var z = new c().member.ToString();
    }
}

class c
{
    public int member { get; }
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestNotForOverloadResolutionFailure()
        {
            var text = @"class Program
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
}";

            await TestMissingInRegularAndScriptAsync(text);
        }

        [Fact]
        public async Task TestHandlePredefinedTypeKeywordCorrectly()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Main(string[] args)
    {
        [|Int3|] i;
    }
}";

            var expected = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Main(string[] args)
    {
        int i;
    }
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestHandlePredefinedTypeKeywordCorrectly1()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Main(string[] args)
    {
        [|Int3|] i;
    }
}";

            var expected = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Main(string[] args)
    {
        Int32 i;
    }
}";

            await TestInRegularAndScriptAsync(text, expected, index: 1);
        }

        [Fact]
        public async Task TestOnGeneric()
        {
            var text = @"
interface Enumerable<T>
{
}

class C
{
    void Main(string[] args)
    {
        [|IEnumerable|]<int> x;
    }
}";

            var expected = @"
interface Enumerable<T>
{
}

class C
{
    void Main(string[] args)
    {
        Enumerable<int> x;
    }
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestTestObjectConstruction()
        {
            await TestInRegularAndScriptAsync(
@"class AwesomeClass
{
    void M()
    {
        var goo = new [|AwesomeClas()|];
    }
}",
@"class AwesomeClass
{
    void M()
    {
        var goo = new AwesomeClass();
    }
}");
        }

        [Fact]
        public async Task TestTestMissingName()
        {
            await TestMissingInRegularAndScriptAsync(
@"[assembly: Microsoft.CodeAnalysis.[||]]");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12990")]
        public async Task TestTrivia1()
        {
            var text = @"
using System.Text;
class C
{
  void M()
  {
    /*leading*/ [|stringbuilder|] /*trailing*/ sb = null;
  }
}";

            var expected = @"
using System.Text;
class C
{
  void M()
  {
    /*leading*/ StringBuilder /*trailing*/ sb = null;
  }
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13345")]
        public async Task TestNotMissingOnKeywordWhichIsAlsoASnippet()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        // here 'for' is a keyword and snippet, so we should offer to spell check to it.
        [|foo|];
    }
}",
@"class C
{
    void M()
    {
        // here 'for' is a keyword and snippet, so we should offer to spell check to it.
        for;
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18626")]
        public async Task TestForExplicitInterfaceTypeName()
        {
            await TestInRegularAndScriptAsync(
@"interface IProjectConfigurationsService
{
    void Method();
}

class Program : IProjectConfigurationsService
{
    void [|IProjectConfigurationService|].Method()
    {

    }
}",
@"interface IProjectConfigurationsService
{
    void Method();
}

class Program : IProjectConfigurationsService
{
    void IProjectConfigurationsService.Method()
    {

    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13345")]
        public async Task TestMissingOnKeywordWhichIsOnlyASnippet()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        // here 'for' is *only* a snippet, and we should not offer to spell check to it.
        var v = [|goo|];
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15733")]
        public async Task TestMissingOnVar()
        {
            await TestMissingInRegularAndScriptAsync(
@"
namespace bar { }

class C
{
    void M()
    {
        var y =
        [|var|]
    }
}");
        }

        [Fact]
        public async Task TestUnmanagedConstraint()
        {
            await TestInRegularAndScriptAsync(
@"class C<T> where T : [|umanaged|]
{
}",
@"class C<T> where T : unmanaged
{
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28244")]
        public async Task TestMisspelledConstructor()
        {
            await TestInRegularAndScriptAsync(
@"public class SomeClass
{
    public [|SomeClss|]() { }
}",
@"public class SomeClass
{
    public SomeClass() { }
}");
        }

        [Fact]
        public async Task TestInExplicitInterfaceImplementation1()
        {
            var text = @"
using System;

class Program : IDisposable
{
    void IDisposable.[|Dspose|]
}";

            var expected = @"
using System;

class Program : IDisposable
{
    void IDisposable.Dispose
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestInExplicitInterfaceImplementation2()
        {
            var text = @"
using System;

interface IInterface
{
    void Generic<K, V>();
}

class Program : IInterface
{
    void IInterface.[|Generi|]
}";

            var expected = @"
using System;

interface IInterface
{
    void Generic<K, V>();
}

class Program : IInterface
{
    void IInterface.Generic
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact]
        public async Task TestInExplicitInterfaceImplementation3()
        {
            var text = @"
using System;

interface IInterface
{
    int this[int i] { get; }
}

class Program : IInterface
{
    void IInterface.[|thi|]
}";

            var expected = @"
using System;

interface IInterface
{
    int this[int i] { get; }
}

class Program : IInterface
{
    void IInterface.this
}";

            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1640728")]
        public async Task TestMisspelledWordThatIsAlsoSnippetName()
        {
            await TestInRegularAndScriptAsync(
@"public [|interfacce|] IWhatever
{
}",
@"public interface IWhatever
{
}");
        }
    }
}
