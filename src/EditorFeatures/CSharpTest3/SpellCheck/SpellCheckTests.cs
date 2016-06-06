// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Spellcheck;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.SpellCheck
{
    public class SpellCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(null, new CSharpSpellCheckCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestNoSpellcheckForIfOnly2Characters()
        {
            var text =
@"class Foo
{
    void Bar()
    {
        var a = new [|Fo|]
    }
}";
            await TestMissingAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestAfterNewExpression()
        {
            var text =
@"class Foo
{
    void Bar()
    {
        void a = new [|Fooa|].ToString();
    }
}";

            await TestExactActionSetOfferedAsync(text, new[] { String.Format(FeaturesResources.ChangeTo, "Fooa", "Foo") });
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
                String.Format(FeaturesResources.ChangeTo, "Foa", "Foo"),
                String.Format(FeaturesResources.ChangeTo, "Foa", "for")
            });
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestInFunc()
        {
            var text = @"
using System;

class Foo
{
    void Bar(Func<[|Foa|]> f)
    {
    }
}";
            await TestExactActionSetOfferedAsync(text,
                new[] { String.Format(FeaturesResources.ChangeTo, "Foa", "Foo") });
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
            await TestExactActionSetOfferedAsync(text, new[] { String.Format(FeaturesResources.ChangeTo, "zza", "zzz") });
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
                String.Format(FeaturesResources.ChangeTo, "Boolea", "Boolean"),
                String.Format(FeaturesResources.ChangeTo, "Boolea", "bool")
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

            await TestAsync(text, expected);
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

            await TestAsync(text, expected);
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

            await TestMissingAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestGenericName1()
        {
            var text = @"class Foo<T>
{
    private [|Foo2|]<T> x;
}";

            var expected = @"class Foo<T>
{
    private Foo<T> x;
}";

            await TestAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestGenericName2()
        {
            var text = @"class Foo<T>
{
    private [|Foo2|] x;
}";

            var expected = @"class Foo<T>
{
    private Foo x;
}";

            await TestAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestQualifiedName1()
        {
            var text = @"class Program
{
   private object x = new [|Foo2|].Bar
}

class Foo
{
    class Bar
    {
    }
}";

            var expected = @"class Program
{
   private object x = new Foo.Bar
}

class Foo
{
    class Bar
    {
    }
}";

            await TestAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestQualifiedName2()
        {
            var text = @"class Program
{
    private object x = new Foo.[|Ba2|]
}

class Foo
{
    public class Bar
    {
    }
}";

            var expected = @"class Program
{
    private object x = new Foo.Bar
}

class Foo
{
    public class Bar
    {
    }
}";

            await TestAsync(text, expected);
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

            await TestAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestNotForOverloadResolutionFailure()
        {
            var text = @"class Program
{
    void Main(string[] args)
    {
    }

    void Foo()
    {
        [|Method|]();
    }

    int Method(int argument)
    {
    }
}";

            await TestMissingAsync(text);
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

            await TestAsync(text, expected, index: 0);
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

            await TestAsync(text, expected, index: 1);
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

            await TestAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestTestObjectConstruction()
        {
            await TestAsync(
"class AwesomeClass { void M() { var foo = new [|AwesomeClas()|]; } }",
"class AwesomeClass { void M() { var foo = new AwesomeClass(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)]
        public async Task TestTestMissingName()
        {
            await TestMissingAsync(
"[assembly: Microsoft.CodeAnalysis.[||]]");
        }

    }
}