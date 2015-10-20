// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnusedUsings;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.RemoveUnnecessaryUsings
{
    public partial class RemoveUnnecessaryUsingsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer(), new RemoveUnnecessaryUsingsCodeFixProvider());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestNoReferences()
        {
            Test(
@"[|using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { } } |]",
@"class Program { static void Main ( string [ ] args ) { } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestIdentifierReferenceInTypeContext()
        {
            Test(
@"[|using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { DateTime d ; } } |]",
@"using System ; class Program { static void Main ( string [ ] args ) { DateTime d ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestGenericReferenceInTypeContext()
        {
            Test(
@"[|using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { List < int > list ; } } |]",
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { List < int > list ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestMultipleReferences()
        {
            Test(
@"[|using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { List < int > list ; DateTime d ; } } |]",
@"using System ; using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { List < int > list ; DateTime d ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestExtensionMethodReference()
        {
            Test(
@"[|using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { args . Where ( a => a . Length > 10 ) ; } } |]",
@"using System . Linq ; class Program { static void Main ( string [ ] args ) { args . Where ( a => a . Length > 10 ) ; } } ");
        }

        [WorkItem(541827)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestExtensionMethodLinq()
        {
            // NOTE: Intentionally not running this test with Script options, because in Script,
            // NOTE: class "Foo" is placed inside the script class, and can't be seen by the extension
            // NOTE: method Select, which is not inside the script class.
            TestMissing(
@"[|using System;
using System.Collections;
using SomeNS;

class Program
{
    static void Main()
    {
        Foo qq = new Foo();
        IEnumerable x = from q in qq select q;
    }
}

public class Foo
{
    public Foo()
    {
    }
}

namespace SomeNS
{
    public static class SomeClass
    {
        public static IEnumerable Select(this Foo o, Func<object, object> f)
        {
            return null;
        }
    }
}|]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestAliasQualifiedAliasReference()
        {
            Test(
@"[|using System ; using G = System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { G :: List < int > list ; } } |]",
@"using G = System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { G :: List < int > list ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestQualifiedAliasReference()
        {
            Test(
@"[|using System ; using G = System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { G . List < int > list ; } } |]",
@"using G = System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { G . List < int > list ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestNestedUnusedUsings()
        {
            Test(
@"[|using System ; using System . Collections . Generic ; using System . Linq ; namespace N { using System ; class Program { static void Main ( string [ ] args ) { DateTime d ; } } } |]",
@"namespace N { using System ; class Program { static void Main ( string [ ] args ) { DateTime d ; } } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestNestedUsedUsings()
        {
            Test(
@"[|using System ; using System . Collections . Generic ; using System . Linq ; namespace N { using System ; class Program { static void Main ( string [ ] args ) { DateTime d ; } } } class F { DateTime d ; } |]",
@"using System ; namespace N { using System ; class Program { static void Main ( string [ ] args ) { DateTime d ; } } } class F { DateTime d ; } ");
        }

        [WorkItem(712656)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestNestedUsedUsings2()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; namespace N { [|using System ;|] using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { DateTime d ; } } } class F { DateTime d ; } ",
@"using System ; namespace N { using System ; class Program { static void Main ( string [ ] args ) { DateTime d ; } } } class F { DateTime d ; } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestAttribute()
        {
            TestMissing(
@"[|using SomeNamespace ; [ SomeAttr ] class Foo { } namespace SomeNamespace { public class SomeAttrAttribute : System . Attribute { } } |]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestAttributeArgument()
        {
            TestMissing(
@"[|using foo ; [ SomeAttribute ( typeof ( SomeClass ) ) ] class Program { static void Main ( ) { } } public class SomeAttribute : System . Attribute { public SomeAttribute ( object f ) { } } namespace foo { public class SomeClass { } } |]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestRemoveAllWithSurroundingPreprocessor()
        {
            Test(
@"#if true

[|using System;
using System.Collections.Generic;

#endif

class Program
{
    static void Main(string[] args)
    {
    }
}|]",
@"#if true


#endif

class Program
{
    static void Main(string[] args)
    {
    }
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestRemoveFirstWithSurroundingPreprocessor()
        {
            Test(
@"#if true

[|using System;
using System.Collections.Generic;

#endif

class Program
{
    static void Main(string[] args)
    {
        List<int> list;
    }
}|]",
@"#if true

using System.Collections.Generic;

#endif

class Program
{
    static void Main(string[] args)
    {
        List<int> list;
    }
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestRemoveAllWithSurroundingPreprocessor2()
        {
            Test(
@"[|namespace N
{
#if true

    using System;
    using System.Collections.Generic;

#endif

    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}|]",
@"namespace N
{
#if true


#endif

    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestRemoveOneWithSurroundingPreprocessor2()
        {
            Test(
@"[|namespace N
{
#if true

    using System;
    using System.Collections.Generic;

#endif

    class Program
    {
        static void Main(string[] args)
        {
            List<int> list;
        }
    }
}|]",
@"namespace N
{
#if true

    using System.Collections.Generic;

#endif

    class Program
    {
        static void Main(string[] args)
        {
            List<int> list;
        }
    }
}",
compareTokens: false);
        }

        [WorkItem(541817)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestComments8718()
        {
            Test(
@"[|using Foo; using System.Collections.Generic; /*comment*/ using Foo2;

class Program
{
    static void Main(string[] args)
    {
        Bar q;
        Bar2 qq;
    }
}

namespace Foo
{
    public class Bar
    {
    }
}

namespace Foo2
{
    public class Bar2
    {
    }
}|]",
@"using Foo;
using Foo2;

class Program
{
    static void Main(string[] args)
    {
        Bar q;
        Bar2 qq;
    }
}

namespace Foo
{
    public class Bar
    {
    }
}

namespace Foo2
{
    public class Bar2
    {
    }
}",
compareTokens: false);
        }

        [WorkItem(528609)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestComments()
        {
            Test(
@"//c1
/*c2*/
[|using/*c3*/ System/*c4*/; //c5
//c6

class Program
{
}
|]",
@"//c1
/*c2*/
//c6

class Program
{
}
",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestUnusedUsing()
        {
            Test(
@"[|using System.Collections.Generic;

class Program
{
    static void Main()
    {
    }
}|]",
@"class Program
{
    static void Main()
    {
    }
}",
compareTokens: false);
        }

        [WorkItem(541827)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestSimpleQuery()
        {
            Test(
@"[|using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { var q = from a in args where a . Length > 21 select a ; } } |]",
@"using System . Linq ; class Program { static void Main ( string [ ] args ) { var q = from a in args where a . Length > 21 select a ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestUsingStaticClassAccessField1()
        {
            Test(
@"[|using SomeNS . Foo ; class Program { static void Main ( ) { var q = x ; } } namespace SomeNS { static class Foo { public static int x ; } } |]",
@"class Program { static void Main ( ) { var q = x ; } } namespace SomeNS { static class Foo { public static int x ; } } ",
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestUsingStaticClassAccessField2()
        {
            TestMissing(
@"[|using static SomeNS . Foo ; class Program { static void Main ( ) { var q = x ; } } namespace SomeNS { static class Foo { public static int x ; } } |]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestUsingStaticClassAccessMethod1()
        {
            Test(
@"[|using SomeNS . Foo ; class Program { static void Main ( ) { var q = X ( ) ; } } namespace SomeNS { static class Foo { public static int X ( ) { return 42 ; } } } |]",
@"[|class Program { static void Main ( ) { var q = X ( ) ; } } namespace SomeNS { static class Foo { public static int X ( ) { return 42 ; } } } |]",
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestUsingStaticClassAccessMethod2()
        {
            TestMissing(
@"[|using static SomeNS . Foo ; class Program { static void Main ( ) { var q = X ( ) ; } } namespace SomeNS { static class Foo { public static int X ( ) { return 42 ; } } } |]");
        }

        [WorkItem(8846, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestUnusedTypeImportIsRemoved()
        {
            Test(
@"[|using SomeNS.Foo;

class Program
{
    static void Main()
    {
    }
}

namespace SomeNS
{
    static class Foo
    {
    }
}|]",
@"
class Program
{
    static void Main()
    {
    }
}

namespace SomeNS
{
    static class Foo
    {
    }
}");
        }

        [WorkItem(541817)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestRemoveTrailingComment()
        {
            Test(
@"[|using System.Collections.Generic; // comment

class Program
{
    static void Main(string[] args)
    {
    }
}

|]",
@"class Program
{
    static void Main(string[] args)
    {
    }
}

",
compareTokens: false);
        }

        [WorkItem(541914)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestRemovingUnbindableUsing()
        {
            Test(
@"[|using gibberish ; public static class Program { } |]",
@"public static class Program { } ");
        }

        [WorkItem(541937)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestAliasInUse()
        {
            TestMissing(
@"[|using GIBBERISH = Foo.Bar;

class Program
{
    static void Main(string[] args)
    {
        GIBBERISH x;
    }
}

namespace Foo
{
    public class Bar
    {
    }
}|]");
        }

        [WorkItem(541914)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestRemoveUnboundUsing()
        {
            Test(
@"[|using gibberish; public static class Program { }|]",
@"public static class Program { }");
        }

        [WorkItem(542016)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestLeadingNewlines1()
        {
            Test(
@"[|using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {

    }
}|]",
@"class Program
{
    static void Main(string[] args)
    {

    }
}",
compareTokens: false);
        }

        [WorkItem(542016)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestRemoveLeadingNewLines2()
        {
            Test(
@"[|namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {

        }
    }
}|]",
@"namespace N
{
    class Program
    {
        static void Main(string[] args)
        {

        }
    }
}",
compareTokens: false);
        }

        [WorkItem(542134)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestImportedTypeUsedAsGenericTypeArgument()
        {
            TestMissing(
@"[|using GenericThingie;

public class GenericType<T>
{
}

namespace GenericThingie
{
    public class Something
    { }
}

public class Program
{
    void foo()
    {
        GenericType<Something> type;
    }
}|]");
        }

        [WorkItem(542723)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestRemoveCorrectUsing1()
        {
            Test(
@"[|using System . Collections . Generic ; namespace Foo { using Bar = Dictionary < string , string > ; } |]",
@"using System . Collections . Generic ; namespace Foo { } ",
parseOptions: null);
        }

        [WorkItem(542723)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestRemoveCorrectUsing2()
        {
            TestMissing(
@"[|using System . Collections . Generic ; namespace Foo { using Bar = Dictionary < string , string > ; class C { Bar b; } } |]",
parseOptions: null);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestSpan()
        {
            TestSpans(
@"[|namespace N
{
    using System;
}|]",
@"namespace N
{
    [|using System;|]
}");
        }

        [WorkItem(543000)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestMissingWhenErrorsWouldBeGenerated()
        {
            TestMissing(
@"[|using System ; using X ; using Y ; class B { static void Main ( ) { Bar ( x => x . Foo ( ) ) ; } static void Bar ( Action < int > x ) { } static void Bar ( Action < string > x ) { } } namespace X { public static class A { public static void Foo ( this int x ) { } public static void Foo ( this string x ) { } } } namespace Y { public static class B { public static void Foo ( this int x ) { } } } |]");
        }

        [WorkItem(544976)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestMissingWhenMeaningWouldChangeInLambda()
        {
            TestMissing(
@"[|using System;
using X;
using Y;
 
class B
{
    static void Main()
    {
        Bar(x => x.Foo(), null); // Prints 1
    }
 
    static void Bar(Action<string> x, object y) { Console.WriteLine(1); }
    static void Bar(Action<int> x, string y) { Console.WriteLine(2); }
}
 
namespace X
{
    public static class A
    {
        public static void Foo(this int x) { }
        public static void Foo(this string x) { }
    }
 
}
 
namespace Y
{
    public static class B
    {
        public static void Foo(this int x) { }
    }
}|]");
        }

        [WorkItem(544976)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestCasesWithLambdas1()
        {
            // NOTE: Y is used when speculatively binding "x => x.Foo()".  As such, it is marked as
            // used even though it isn't in the final bind, and could be removed.  However, as we do
            // not know if it was necessary to eliminate a speculative lambda bind, we must leave
            // it.
            TestMissing(
@"[|using System;
using X;
using Y;

class B
{
    static void Main()
    {
        Bar(x => x.Foo(), null); // Prints 1
    }

    static void Bar(Action<string> x, object y) { }
}

namespace X
{
    public static class A
    {
        public static void Foo(this string x) { }
    }
}

namespace Y
{
    public static class B
    {
        public static void Foo(this int x) { }
    }
}|]");
        }

        [WorkItem(545646)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestCasesWithLambdas2()
        {
            TestMissing(
@"[|using System;
using N; // Falsely claimed as unnecessary
 
static class C
    {
    static void Ex(this string x) { }
 
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static void Main()
    {
        Outer(y => Inner(x => x.Ex(), y), null);
    }
}

namespace N
{
    static class E
    {
        public static void Ex(this int x) { }
    }
}|]");
        }

        [WorkItem(545741)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestMissingOnAliasedVar()
        {
            TestMissing(
@"[|using var = var ; class var { } class B { static void Main ( ) { var a = 1 ; } }|] ");
        }

        [WorkItem(546115)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestBrokenCode()
        {
            TestMissing(
@"[|using System.Linq;
public class QueryExpressionTest
{
  public static void Main()
  {
    var expr1 = new[] { };
    var expr2 = new[] { };
    var query8 = from int i in expr1 join int fixed in expr2 on i equals fixed select new { i, fixed };
    var query9 = from object i in expr1 join object fixed in expr2 on i equals fixed select new { i, fixed };
  }
}|]");
        }

        [WorkItem(530980)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestReferenceInCref()
        {
            // parsing doc comments as simple trivia; System is unnecessary
            Test(@"[|using System ;
/// <summary><see cref=""String"" /></summary>
 class C { }|] ",
 @"/// <summary><see cref=""String"" /></summary>
 class C { } ");

            // fully parsing doc comments; System is necessary
            TestMissing(
@"[|using System ;
/// <summary><see cref=""String"" /></summary>
 class C { }|] ", Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose));
        }

        [WorkItem(751283)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        public void TestUnusedUsingOverLinq()
        {
            Test(
@"using System ; [|using System . Linq ; using System . Threading . Tasks ;|] class Program { static void Main ( string [ ] args ) { Console . WriteLine ( ) ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Console . WriteLine ( ) ; } } ");
        }
    }
}
