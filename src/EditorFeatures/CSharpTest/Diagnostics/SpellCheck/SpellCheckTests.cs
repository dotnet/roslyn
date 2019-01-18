// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.SpellCheck
{
    public class SpellCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpSpellCheckCodeFixProvider());

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

            await TestExactActionSetOfferedAsync(text, new[] { String.Format(FeaturesResources.Change_0_to_1, "Gooa", "Goo") });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestInLocalType()
        {
            var text = @"class Foo
{
    void Bar()
    {
        [|Foa|] a;
    }
}";

            await TestExactActionSetOfferedAsync(text, new[]
            {
                String.Format(FeaturesResources.Change_0_to_1, "Foa", "Foo"),
                String.Format(FeaturesResources.Change_0_to_1, "Foa", "for")
            });
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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
                new[] { String.Format(FeaturesResources.Change_0_to_1, "Goa", "Goo") });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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
            await TestExactActionSetOfferedAsync(text, new[] { String.Format(FeaturesResources.Change_0_to_1, "zza", "zzz") });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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
            await TestExactActionSetOfferedAsync(text, new[]
            {
                String.Format(FeaturesResources.Change_0_to_1, "Boolea", "Boolean"),
                String.Format(FeaturesResources.Change_0_to_1, "Boolea", "bool")
            });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestTestMissingName()
        {
            await TestMissingInRegularAndScriptAsync(
@"[assembly: Microsoft.CodeAnalysis.[||]]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        [WorkItem(12990, "https://github.com/dotnet/roslyn/issues/12990")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        [WorkItem(13345, "https://github.com/dotnet/roslyn/issues/13345")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        [WorkItem(18626, "https://github.com/dotnet/roslyn/issues/18626")]
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



        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        [WorkItem(13345, "https://github.com/dotnet/roslyn/issues/13345")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        [WorkItem(15733, "https://github.com/dotnet/roslyn/issues/15733")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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

        [WorkItem(28244, "https://github.com/dotnet/roslyn/issues/28244")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
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
    }
}
