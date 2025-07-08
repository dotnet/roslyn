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
    public async Task TestNoSpellcheckForIfOnly2Characters()
    {
        await TestMissingInRegularAndScriptAsync(@"class Goo
{
    void Bar()
    {
        var a = new [|Fo|]
    }
}");
    }

    [Fact]
    public async Task TestAfterNewExpression()
    {
        await TestExactActionSetOfferedAsync(@"class Goo
{
    void Bar()
    {
        void a = new [|Gooa|].ToString();
    }
}", [String.Format(FeaturesResources.Change_0_to_1, "Gooa", "Goo")]);
    }

    [Fact]
    public async Task TestInLocalType()
    {
        await TestExactActionSetOfferedAsync(@"class Foo
{
    void Bar()
    {
        [|Foa|] a;
    }
}",
        [
            String.Format(FeaturesResources.Change_0_to_1, "Foa", "Foo"),
            String.Format(FeaturesResources.Change_0_to_1, "Foa", "for")
        ]);
    }

    [Fact]
    public async Task TestInFunc()
    {
        await TestExactActionSetOfferedAsync(@"
using System;

class Goo
{
    void Bar(Func<[|Goa|]> f)
    {
    }
}",
            [String.Format(FeaturesResources.Change_0_to_1, "Goa", "Goo")]);
    }

    [Fact]
    public async Task TestInExpression()
    {
        await TestExactActionSetOfferedAsync(@"class Program
{
    void Main(string[] args)
    {
        var zzz = 2;
        var  y = 2 + [|zza|];
    }
}", [String.Format(FeaturesResources.Change_0_to_1, "zza", "zzz")]);
    }

    [Fact]
    public async Task TestInTypeOfIsExpression()
    {
        await TestExactActionSetOfferedAsync(@"using System;
public class Class1
{
    void F()
    {
        if (x is [|Boolea|]) {}
    }
}",
        [
            String.Format(FeaturesResources.Change_0_to_1, "Boolea", "Boolean"),
            String.Format(FeaturesResources.Change_0_to_1, "Boolea", "bool")
        ]);
    }

    [Fact]
    public async Task TestInvokeCorrectIdentifier()
    {
        await TestInRegularAndScriptAsync(@"class Program
{
    void Main(string[] args)
    {
        var zzz = 2;
        var y = 2 + [|zza|];
    }
}", @"class Program
{
    void Main(string[] args)
    {
        var zzz = 2;
        var y = 2 + zzz;
    }
}");
    }

    [Fact]
    public async Task TestAfterDot()
    {
        await TestInRegularAndScriptAsync(@"class Program
{
    static void Main(string[] args)
    {
        Program.[|Mair|]
    }
}", @"class Program
{
    static void Main(string[] args)
    {
        Program.Main
    }
}");
    }

    [Fact]
    public async Task TestNotInaccessibleProperty()
    {
        await TestMissingInRegularAndScriptAsync(@"class Program
{
    void Main(string[] args)
    {
        var z = new c().[|membr|]
    }
}

class c
{
    protected int member { get; }
}");
    }

    [Fact]
    public async Task TestGenericName1()
    {
        await TestInRegularAndScriptAsync(@"class Goo<T>
{
    private [|Goo2|]<T> x;
}", @"class Goo<T>
{
    private Goo<T> x;
}");
    }

    [Fact]
    public async Task TestGenericName2()
    {
        await TestInRegularAndScriptAsync(@"class Goo<T>
{
    private [|Goo2|] x;
}", @"class Goo<T>
{
    private Goo x;
}");
    }

    [Fact]
    public async Task TestQualifiedName1()
    {
        await TestInRegularAndScriptAsync(@"class Program
{
   private object x = new [|Goo2|].Bar
}

class Goo
{
    class Bar
    {
    }
}", @"class Program
{
   private object x = new Goo.Bar
}

class Goo
{
    class Bar
    {
    }
}");
    }

    [Fact]
    public async Task TestQualifiedName2()
    {
        await TestInRegularAndScriptAsync(@"class Program
{
    private object x = new Goo.[|Ba2|]
}

class Goo
{
    public class Bar
    {
    }
}", @"class Program
{
    private object x = new Goo.Bar
}

class Goo
{
    public class Bar
    {
    }
}");
    }

    [Fact]
    public async Task TestMiddleOfDottedExpression()
    {
        await TestInRegularAndScriptAsync(@"class Program
{
    void Main(string[] args)
    {
        var z = new c().[|membr|].ToString();
    }
}

class c
{
    public int member { get; }
}", @"class Program
{
    void Main(string[] args)
    {
        var z = new c().member.ToString();
    }
}

class c
{
    public int member { get; }
}");
    }

    [Fact]
    public async Task TestNotForOverloadResolutionFailure()
    {
        await TestMissingInRegularAndScriptAsync(@"class Program
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
}");
    }

    [Fact]
    public async Task TestHandlePredefinedTypeKeywordCorrectly()
    {
        await TestInRegularAndScriptAsync(@"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Main(string[] args)
    {
        [|Int3|] i;
    }
}", @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Main(string[] args)
    {
        int i;
    }
}");
    }

    [Fact]
    public async Task TestHandlePredefinedTypeKeywordCorrectly1()
    {
        await TestInRegularAndScriptAsync(@"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Main(string[] args)
    {
        [|Int3|] i;
    }
}", @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Main(string[] args)
    {
        Int32 i;
    }
}", index: 1);
    }

    [Fact]
    public async Task TestOnGeneric()
    {
        await TestInRegularAndScriptAsync(@"
interface Enumerable<T>
{
}

class C
{
    void Main(string[] args)
    {
        [|IEnumerable|]<int> x;
    }
}", @"
interface Enumerable<T>
{
}

class C
{
    void Main(string[] args)
    {
        Enumerable<int> x;
    }
}");
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
        await TestInRegularAndScriptAsync(@"
using System.Text;
class C
{
  void M()
  {
    /*leading*/ [|stringbuilder|] /*trailing*/ sb = null;
  }
}", @"
using System.Text;
class C
{
  void M()
  {
    /*leading*/ StringBuilder /*trailing*/ sb = null;
  }
}");
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
        await TestMissingInRegularAndScriptAsync(@"
using System;

class Program : IDisposable
{
    void IDisposable.[|Dspose|]
}");
    }

    [Fact]
    public async Task TestInExplicitInterfaceImplementation2()
    {
        await TestMissingInRegularAndScriptAsync(@"
using System;

interface IInterface
{
    void Generic<K, V>();
}

class Program : IInterface
{
    void IInterface.[|Generi|]
}");
    }

    [Fact]
    public async Task TestInExplicitInterfaceImplementation3()
    {
        await TestMissingInRegularAndScriptAsync(@"
using System;

interface IInterface
{
    int this[int i] { get; }
}

class Program : IInterface
{
    void IInterface.[|thi|]
}");
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
