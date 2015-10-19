// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddImport;
using Microsoft.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddUsing
{
    public partial class AddUsingTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(
                    null,
                    new CSharpAddImportCodeFixProvider());
        }

        private void Test(
             string initialMarkup,
             string expected,
             bool systemSpecialCase,
             int index = 0)
        {
            Test(initialMarkup, expected, index, options: new Dictionary<OptionKey, object>
            {
                { new OptionKey(OrganizerOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp), systemSpecialCase }
            });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestTypeFromMultipleNamespaces1()
        {
            Test(
@"class Class { [|IDictionary|] Method() { Foo(); } }",
@"using System.Collections; class Class { IDictionary Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestTypeFromMultipleNamespaces2()
        {
            Test(
@"class Class { [|IDictionary|] Method() { Foo(); } }",
@"using System.Collections.Generic; class Class { IDictionary Method() { Foo(); } }",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGenericWithNoArgs()
        {
            Test(
@"class Class { [|List|] Method() { Foo(); } }",
@"using System.Collections.Generic; class Class { List Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGenericWithCorrectArgs()
        {
            Test(
@"class Class { [|List<int>|] Method() { Foo(); } }",
@"using System.Collections.Generic; class Class { List<int> Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGenericWithWrongArgs()
        {
            TestMissing(
@"class Class { [|List<int,string>|] Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGenericInLocalDeclaration()
        {
            Test(
@"class Class { void Foo() { [|List<int>|] a = new List<int>(); } }",
@"using System.Collections.Generic; class Class { void Foo() { List<int> a = new List<int>(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGenericItemType()
        {
            Test(
@"using System.Collections.Generic; class Class { List<[|Int32|]> l; }",
@"using System; using System.Collections.Generic; class Class { List<Int32> l; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGenerateWithExistingUsings()
        {
            Test(
@"using System; class Class { [|List<int>|] Method() { Foo(); } }",
@"using System; using System.Collections.Generic; class Class { List<int> Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGenerateInNamespace()
        {
            Test(
@"namespace N { class Class { [|List<int>|] Method() { Foo(); } } }",
@"using System.Collections.Generic; namespace N { class Class { List<int> Method() { Foo(); } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGenerateInNamespaceWithUsings()
        {
            Test(
@"namespace N { using System; class Class { [|List<int>|] Method() { Foo(); } } }",
@"namespace N { using System; using System.Collections.Generic; class Class { List<int> Method() { Foo(); } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestExistingUsing()
        {
            TestActionCount(
@"using System.Collections.Generic; class Class { [|IDictionary|] Method() { Foo(); } }",
count: 1);

            Test(
@"using System.Collections.Generic; class Class { [|IDictionary|] Method() { Foo(); } }",
@"using System.Collections; using System.Collections.Generic; class Class { IDictionary Method() { Foo(); } }");
        }

        [WorkItem(541730)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForGenericExtensionMethod()
        {
            Test(
@"using System.Collections.Generic; class Class { void Method(IList<int> args) { args.[|Where|]() } }",
@"using System.Collections.Generic; using System.Linq; class Class { void Method(IList<int> args) { args.Where() } }");
        }

        [WorkItem(541730)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForNormalExtensionMethod()
        {
            Test(
@"class Class { void Method(Class args) { args.[|Where|]() } } namespace N { static class E { public static void Where(this Class c) { } } }",
@"using N; class Class { void Method(Class args) { args.Where() } } namespace N { static class E { public static void Where(this Class c) { } } }",
parseOptions: Options.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestOnEnum()
        {
            Test(
@"class Class { void Foo() { var a = [|Colors|].Red; } } namespace A { enum Colors {Red, Green, Blue} }",
@"using A; class Class { void Foo() { var a = Colors.Red; } } namespace A { enum Colors {Red, Green, Blue} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestOnClassInheritance()
        {
            Test(
@"class Class : [|Class2|] { } namespace A { class Class2 { } }",
@"using A; class Class : Class2 { } namespace A { class Class2 { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestOnImplementedInterface()
        {
            Test(
@"class Class : [|IFoo|] { } namespace A { interface IFoo { } }",
@"using A; class Class : IFoo { } namespace A { interface IFoo { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAllInBaseList()
        {
            Test(
@"class Class : [|IFoo|], Class2 { } namespace A { class Class2 { } } namespace B { interface IFoo { } } ",
@"using B; class Class : IFoo, Class2 { } namespace A { class Class2 { } } namespace B { interface IFoo { } }");

            Test(
@"using B; class Class : IFoo, [|Class2|] { } namespace A { class Class2 { } } namespace B { interface IFoo { } } ",
@"using A; using B; class Class : IFoo, Class2 { } namespace A { class Class2 { } } namespace B { interface IFoo { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAttributeUnexpanded()
        {
            Test(
@"[[|Obsolete|]]class Class { }",
@"using System; [Obsolete]class Class { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAttributeExpanded()
        {
            Test(
@"[[|ObsoleteAttribute|]]class Class { }",
@"using System; [ObsoleteAttribute]class Class { }");
        }

        [WorkItem(538018)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAfterNew()
        {
            Test(
@"class Class { void Foo() { List<int> l; l = new [|List<int>|](); } }",
@"using System.Collections.Generic; class Class { void Foo() { List<int> l; l = new List<int>(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestArgumentsInMethodCall()
        {
            Test(
@"class Class { void Test() { Console.WriteLine([|DateTime|].Today); } }",
@"using System; class Class { void Test() { Console.WriteLine(DateTime.Today); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestCallSiteArgs()
        {
            Test(
@"class Class { void Test([|DateTime|] dt) { } }",
@"using System; class Class { void Test(DateTime dt) { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestUsePartialClass()
        {
            Test(
@"namespace A { public class Class { [|PClass|] c; } } namespace B{ public partial class PClass { } }",
@"using B; namespace A { public class Class { PClass c; } } namespace B{ public partial class PClass { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGenericClassInNestedNamespace()
        {
            Test(
@"namespace A { namespace B { class GenericClass<T> { } } } namespace C { class Class { [|GenericClass<int>|] c; } }",
@"using A.B; namespace A { namespace B { class GenericClass<T> { } } } namespace C { class Class { GenericClass<int> c; } }");
        }

        [WorkItem(541730)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestExtensionMethods()
        {
            Test(
@"using System . Collections . Generic ; class Foo { void Bar ( ) { var values = new List < int > ( ) ; values . [|Where|] ( i => i > 1 ) ; } } ",
@"using System . Collections . Generic ; using System . Linq ; class Foo { void Bar ( ) { var values = new List < int > ( ) ; values . Where ( i => i > 1 ) ; } } ");
        }

        [WorkItem(541730)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestQueryPatterns()
        {
            Test(
@"using System . Collections . Generic ; class Foo { void Bar ( ) { var values = new List < int > ( ) ; var q = [|from v in values where v > 1 select v + 10|] ; } } ",
@"using System . Collections . Generic ; using System . Linq ; class Foo { void Bar ( ) { var values = new List < int > ( ) ; var q = from v in values where v > 1 select v + 10 ; } } ");
        }

        // Tests for Insertion Order
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimplePresortedUsings1()
        {
            Test(
@"using B; using C; class Class { void Method() { [|Foo|].Bar(); } } namespace D { class Foo { public static void Bar() { } } }",
@"using B; using C; using D; class Class { void Method() { Foo.Bar(); } } namespace D { class Foo { public static void Bar() { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimplePresortedUsings2()
        {
            Test(
@"using B; using C; class Class { void Method() { [|Foo|].Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
@"using A; using B; using C; class Class { void Method() { Foo.Bar(); } } namespace A { class Foo { public static void Bar() { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleUnsortedUsings1()
        {
            Test(
@"using C; using B; class Class { void Method() { [|Foo|].Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
@"using C; using B; using A; class Class { void Method() { Foo.Bar(); } } namespace A { class Foo { public static void Bar() { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleUnsortedUsings2()
        {
            Test(
@"using D; using B; class Class { void Method() { [|Foo|].Bar(); } } namespace C { class Foo { public static void Bar() { } } }",
@"using D; using B; using C; class Class { void Method() { Foo.Bar(); } } namespace C { class Foo { public static void Bar() { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestMultiplePresortedUsings1()
        {
            Test(
@"using B.X; using B.Y; class Class { void Method() { [|Foo|].Bar(); } } namespace B { class Foo { public static void Bar() { } } }",
@"using B; using B.X; using B.Y; class Class { void Method() { Foo.Bar(); } } namespace B { class Foo { public static void Bar() { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestMultiplePresortedUsings2()
        {
            Test(
@"using B.X; using B.Y; class Class { void Method() { [|Foo|].Bar(); } } namespace B.A { class Foo { public static void Bar() { } } }",
@"using B.A; using B.X; using B.Y; class Class { void Method() { Foo.Bar(); } } namespace B.A { class Foo { public static void Bar() { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestMultiplePresortedUsings3()
        {
            Test(
@"using B.X; using B.Y; class Class { void Method() { [|Foo|].Bar(); } } namespace B { namespace A { class Foo { public static void Bar() { } } } }",
@"using B.A; using B.X; using B.Y; class Class { void Method() { Foo.Bar(); } } namespace B { namespace A { class Foo { public static void Bar() { } } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestMultipleUnsortedUsings1()
        {
            Test(
@"using B.Y; using B.X; class Class { void Method() { [|Foo|].Bar(); } } namespace B { namespace A { class Foo { public static void Bar() { } } } }",
@"using B.Y; using B.X; using B.A; class Class { void Method() { Foo.Bar(); } } namespace B { namespace A { class Foo { public static void Bar() { } } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestMultipleUnsortedUsings2()
        {
            Test(
@"using B.Y; using B.X; class Class { void Method() { [|Foo|].Bar(); } } namespace B { class Foo { public static void Bar() { } } }",
@"using B.Y; using B.X; using B; class Class { void Method() { Foo.Bar(); } } namespace B { class Foo { public static void Bar() { } } }");
        }

        // System on top cases
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleSystemSortedUsings1()
        {
            Test(
@"using System; using B; class Class { void Method() { [|Foo|].Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
@"using System; using A; using B; class Class { void Method() { Foo.Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
systemSpecialCase: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleSystemSortedUsings2()
        {
            Test(
@"using System; using System.Collections.Generic; using B; class Class { void Method() { [|Foo|].Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
@"using System; using System.Collections.Generic; using A; using B; class Class { void Method() { Foo.Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
systemSpecialCase: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleSystemSortedUsings3()
        {
            Test(
@"using A; using B; class Class { void Method() { [|Console|].Write(1); } }",
@"using System; using A; using B; class Class { void Method() { Console.Write(1); } }",
systemSpecialCase: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleSystemUnsortedUsings1()
        {
            Test(
@"using B; using System; class Class { void Method() { [|Foo|].Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
@"using B; using System; using A; class Class { void Method() { Foo.Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
systemSpecialCase: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleSystemUnsortedUsings2()
        {
            Test(
@"using System.Collections.Generic; using System; using B; class Class { void Method() { [|Foo|].Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
@"using System.Collections.Generic; using System; using B; using A; class Class { void Method() { Foo.Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
systemSpecialCase: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleSystemUnsortedUsings3()
        {
            Test(
@"using B; using A; class Class { void Method() { [|Console|].Write(1); } }",
@"using B; using A; using System; class Class { void Method() { Console.Write(1); } }",
systemSpecialCase: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleBogusSystemUsings1()
        {
            Test(
@"using A.System; class Class { void Method() { [|Console|].Write(1); } }",
@"using System; using A.System; class Class { void Method() { Console.Write(1); } }",
systemSpecialCase: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleBogusSystemUsings2()
        {
            Test(
@"using System.System; class Class { void Method() { [|Console|].Write(1); } }",
@"using System; using System.System; class Class { void Method() { Console.Write(1); } }",
systemSpecialCase: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestUsingsWithComments()
        {
            Test(
@"using System./*...*/.Collections.Generic; class Class { void Method() { [|Console|].Write(1); } }",
@"using System; using System./*...*/.Collections.Generic; class Class { void Method() { Console.Write(1); } }",
systemSpecialCase: true);
        }

        // System Not on top cases
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleSystemUnsortedUsings4()
        {
            Test(
@"using System; using B; class Class { void Method() { [|Foo|].Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
@"using System; using B; using A; class Class { void Method() { Foo.Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
systemSpecialCase: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleSystemSortedUsings5()
        {
            Test(
@"using B; using System; class Class { void Method() { [|Foo|].Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
@"using A; using B; using System; class Class { void Method() { Foo.Bar(); } } namespace A { class Foo { public static void Bar() { } } }",
systemSpecialCase: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSimpleSystemSortedUsings4()
        {
            Test(
@"using A; using B; class Class { void Method() { [|Console|].Write(1); } }",
@"using A; using B; using System; class Class { void Method() { Console.Write(1); } }",
systemSpecialCase: false);
        }

        [WorkItem(538136)]
        [WorkItem(538763)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForNamespace()
        {
            TestMissing(
@"namespace A { class Class { [|C|].Test t; } } namespace B { namespace C { class Test { } } }");
        }

        [WorkItem(538220)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForFieldWithFormatting()
        {
            Test(
@"class C { [|DateTime|] t; }",
@"using System;

class C { DateTime t; }",
compareTokens: false);
        }

        [WorkItem(539657)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void BugFix5688()
        {
            Test(
@"class Program { static void Main ( string [ ] args ) { [|Console|] . Out . NewLine = ""\r\n\r\n"" ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Console . Out . NewLine = ""\r\n\r\n"" ; } } ");
        }

        [WorkItem(539853)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void BugFix5950()
        {
            Test(
@"using System.Console; WriteLine([|Expression|].Constant(123));",
@"using System.Console; using System.Linq.Expressions; WriteLine(Expression.Constant(123));",
parseOptions: GetScriptOptions());
        }

        [WorkItem(540339)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddAfterDefineDirective1()
        {
            Test(
@"#define foo

using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        [|Console|].WriteLine();
    }
}",
@"#define foo

using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}",
compareTokens: false);
        }

        [WorkItem(540339)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddAfterDefineDirective2()
        {
            Test(
@"#define foo

class Program
{
    static void Main(string[] args)
    {
        [|Console|].WriteLine();
    }
}",
@"#define foo

using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddAfterDefineDirective3()
        {
            Test(
@"#define foo

/// Foo
class Program
{
    static void Main(string[] args)
    {
        [|Console|].WriteLine();
    }
}",
@"#define foo

using System;
/// Foo
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddAfterDefineDirective4()
        {
            Test(
@"#define foo

// Foo
class Program
{
    static void Main(string[] args)
    {
        [|Console|].WriteLine();
    }
}",
@"#define foo

// Foo
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddAfterExistingBanner()
        {
            Test(
@"// Banner
// Banner

class Program
{
    static void Main(string[] args)
    {
        [|Console|].WriteLine();
    }
}",
@"// Banner
// Banner

using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddAfterExternAlias1()
        {
            Test(
@"#define foo

extern alias Foo;

class Program
{
    static void Main(string[] args)
    {
        [|Console|].WriteLine();
    }
}",
@"#define foo

extern alias Foo;

using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddAfterExternAlias2()
        {
            Test(
@"#define foo

extern alias Foo;

using System.Collections;

class Program
{
    static void Main(string[] args)
    {
        [|Console|].WriteLine();
    }
}",
@"#define foo

extern alias Foo;

using System;
using System.Collections;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestWithReferenceDirective()
        {
            var resolver = new TestMetadataReferenceResolver(assemblyNames: new Dictionary<string, PortableExecutableReference>()
            {
                { "exprs", AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Core).GetReference() }
            });

            Test(
@"#r ""exprs""
[|Expression|]",
@"#r ""exprs""
using System.Linq.Expressions;

Expression",
parseOptions: GetScriptOptions(),
compilationOptions: TestOptions.ReleaseDll.WithMetadataReferenceResolver(resolver),
compareTokens: false);
        }

        [WorkItem(542643)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAssemblyAttribute()
        {
            Test(
@"[ assembly : [|InternalsVisibleTo|] ( ""Project"" ) ] ",
@"using System . Runtime . CompilerServices ; [ assembly : InternalsVisibleTo ( ""Project"" ) ] ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestDoNotAddIntoHiddenRegion()
        {
            TestMissing(
@"#line hidden
using System.Collections.Generic;
#line default

class Program
{
    void Main()
    {
        [|DateTime|] d;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddToVisibleRegion()
        {
            Test(
@"#line default
using System.Collections.Generic;

#line hidden
class Program
{
    void Main()
    {
#line default
        [|DateTime|] d;
#line hidden
    }
}
#line default",
@"#line default
using System;
using System.Collections.Generic;

#line hidden
class Program
{
    void Main()
    {
#line default
        DateTime d;
#line hidden
    }
}
#line default",
compareTokens: false);
        }

        [WorkItem(545248)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestVenusGeneration1()
        {
            TestMissing(
@"
class C
{
    void Foo()
    {
#line 1 ""Default.aspx""
        using (new [|StreamReader|]()) {
#line default
#line hidden
    }
}");
        }

        [WorkItem(545774)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAttribute()
        {
            var input = @"[ assembly : [|Guid|] ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ";
            TestActionCount(input, 1);

            Test(
input,
@"using System . Runtime . InteropServices ; [ assembly : Guid ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ");
        }

        [WorkItem(546833)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestNotOnOverloadResolutionError()
        {
            TestMissing(
@"namespace ConsoleApplication1 { class Program { void Main ( ) { var test = new [|Test|] ( """" ) ; } } class Test { } } ");
        }

        [WorkItem(17020, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForGenericArgument()
        {
            Test(
@"namespace ConsoleApplication10 { class Program { static void Main ( string [ ] args ) { var inArgument = new InArgument < [|IEnumerable < int >|] > ( new int [ ] { 1 , 2 , 3 } ) ; } } public class InArgument < T > { public InArgument ( T constValue ) { } } } ",
@"using System . Collections . Generic ; namespace ConsoleApplication10 { class Program { static void Main ( string [ ] args ) { var inArgument = new InArgument < IEnumerable < int > > ( new int [ ] { 1 , 2 , 3 } ) ; } } public class InArgument < T > { public InArgument ( T constValue ) { } } } ");
        }

        [WorkItem(775448)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void ShouldTriggerOnCS0308()
        {
            // CS0308: The non-generic type 'A' cannot be used with type arguments
            Test(
@"using System.Collections;

class Test
{
    static void Main(string[] args)
    {
        [|IEnumerable<int>|] f;
    }
}",
@"using System.Collections;
using System.Collections.Generic;

class Test
{
    static void Main(string[] args)
    {
        IEnumerable<int> f;
    }
}");
        }

        [WorkItem(838253)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestConflictedInaccessibleType()
        {
            Test(
@"using System.Diagnostics; namespace N { public class Log { } } class C { static void Main(string[] args) { [|Log|] } }",
@"using System.Diagnostics; using N; namespace N { public class Log { } } class C { static void Main(string[] args) { Log } }",
systemSpecialCase: true);
        }

        [WorkItem(858085)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestConflictedAttributeName()
        {
            Test(
@"[[|Description|]]class Description { }",
@"using System.ComponentModel; [Description]class Description { }");
        }

        [WorkItem(872908)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestConflictedGenericName()
        {
            Test(
@"using Task = System.AccessViolationException; class X { [|Task<X> x;|] }",
@"using System.Threading.Tasks; using Task = System.AccessViolationException; class X { Task<X> x; }");
        }

        [WorkItem(860648)]
        [WorkItem(902014)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestIncompleteSimpleLambdaExpression()
        {
            Test(
@"using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        [|args[0].Any(x => IBindCtx|]
        string a;
    }
}",
@"using System.Linq;
using System.Runtime.InteropServices.ComTypes;

class Program
{
    static void Main(string[] args)
    {
        args[0].Any(x => IBindCtx
        string a;
    }
}");
        }

        [WorkItem(860648)]
        [WorkItem(902014)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestIncompleteParenthesizedLambdaExpression()
        {
            Test(
@"using System;

class Test
{
    void Foo()
    {
        Action a = () => [|{ IBindCtx };|]
        string a;        
    }
}",
@"using System;
using System.Runtime.InteropServices.ComTypes;

class Test
{
    void Foo()
    {
        Action a = () => { IBindCtx };
        string a;        
    }
}");
        }

        [WorkItem(913300)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestNoDuplicateReport()
        {
            TestActionCountInAllFixes(
@"class C
{
    void M(P p)
    {
        [| Console |]
    }

    static void Main(string[] args)
    {
    }
}", count: 1);

            Test(
@"class C { void M(P p) { [|Console|] } static void Main(string[] args) { } }",
@"using System; class C { void M(P p) { Console } static void Main(string[] args) { } }");
        }

        [WorkItem(938296)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestNullParentInNode()
        {
            TestMissing(
@"using System.Collections.Generic;
 
class MultiDictionary<K, V> : Dictionary<K, HashSet<V>>
{
    void M()
    {
        new HashSet<V>([|Comparer|]);
    }
}");
        }

        [WorkItem(968303)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestMalformedUsingSection()
        {
            TestMissing("[ class Class { [|List<|] }");
        }

        [WorkItem(875899)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingsWithExternAlias()
        {
            const string InitialWorkspace = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""lib"" CommonReferences=""true"">
        <Document FilePath=""lib.cs"">
namespace ProjectLib
{
    public class Project
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Console"" CommonReferences=""true"">
        <ProjectReference Alias=""P"">lib</ProjectReference>
        <Document FilePath=""Program.cs"">
namespace ExternAliases
{
    class Program
    {
        static void Main(string[] args)
        {
            Project p = new [|Project()|];
        }
    }
} 
</Document>
    </Project>
</Workspace>";

            const string ExpectedDocumentText = @"extern alias P;

using P::ProjectLib;
namespace ExternAliases
{
    class Program
    {
        static void Main(string[] args)
        {
            Project p = new Project();
        }
    }
} 
";
            Test(InitialWorkspace, ExpectedDocumentText);
        }

        [WorkItem(875899)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingsWithPreExistingExternAlias()
        {
            const string InitialWorkspace = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""lib"" CommonReferences=""true"">
        <Document FilePath=""lib.cs"">
namespace ProjectLib
{
    public class Project
    {
    }
}

namespace AnotherNS
{
    public class AnotherClass
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Console"" CommonReferences=""true"">
        <ProjectReference Alias=""P"">lib</ProjectReference>
        <Document FilePath=""Program.cs"">
extern alias P;
using P::ProjectLib;
namespace ExternAliases
{
    class Program
    {
        static void Main(string[] args)
        {
            Project p = new Project();
            var x = new [|AnotherClass()|];
        }
    }
} 
</Document>
    </Project>
</Workspace>";

            const string ExpectedDocumentText = @"extern alias P;

using P::AnotherNS;
using P::ProjectLib;
namespace ExternAliases
{
    class Program
    {
        static void Main(string[] args)
        {
            Project p = new Project();
            var x = new [|AnotherClass()|];
        }
    }
} 
";
            Test(InitialWorkspace, ExpectedDocumentText);
        }

        [WorkItem(875899)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingsNoExtern()
        {
            const string InitialWorkspace = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""lib"" CommonReferences=""true"">
        <Document FilePath=""lib.cs"">
namespace AnotherNS
{
    public class AnotherClass
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Console"" CommonReferences=""true"">
        <ProjectReference Alias=""P"">lib</ProjectReference>
        <Document FilePath=""Program.cs"">
using P::AnotherNS;
namespace ExternAliases
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = new [|AnotherClass()|];
        }
    }
} 
</Document>
    </Project>
</Workspace>";

            const string ExpectedDocumentText = @"extern alias P;
using P::AnotherNS;
namespace ExternAliases
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = new AnotherClass();
        }
    }
} 
";
            Test(InitialWorkspace, ExpectedDocumentText);
        }

        [WorkItem(875899)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingsNoExternFilterGlobalAlias()
        {
            Test(
@"class Program
{
    static void Main(string[] args)
    {
        [|INotifyPropertyChanged.PropertyChanged|]
    }
}",
@"using System.ComponentModel;

class Program
{
    static void Main(string[] args)
    {
        INotifyPropertyChanged.PropertyChanged
    }
}");
        }

        [WorkItem(916368)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForCref()
        {
            var initialText =
@"/// <summary>
/// This is just like <see cref='[|INotifyPropertyChanged|]'/>, but this one is mine.
/// </summary>
interface MyNotifyPropertyChanged { }";

            var expectedText =
@"using System.ComponentModel;
/// <summary>
/// This is just like <see cref='INotifyPropertyChanged'/>, but this one is mine.
/// </summary>
interface MyNotifyPropertyChanged { }";

            var options = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);

            Test(initialText, expectedText, parseOptions: options);
        }

        [WorkItem(916368)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForCref2()
        {
            var initialText =
@"/// <summary>
/// This is just like <see cref='[|INotifyPropertyChanged.PropertyChanged|]'/>, but this one is mine.
/// </summary>
interface MyNotifyPropertyChanged { }";

            var expectedText =
@"using System.ComponentModel;
/// <summary>
/// This is just like <see cref='INotifyPropertyChanged.PropertyChanged'/>, but this one is mine.
/// </summary>
interface MyNotifyPropertyChanged { }";

            var options = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);

            Test(initialText, expectedText, parseOptions: options);
        }

        [WorkItem(916368)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForCref3()
        {
            var initialText =
@"namespace N1
{
    public class D { }
}

public class MyClass
{
    public static explicit operator N1.D (MyClass f)
    {
        return default(N1.D);
    }
}

/// <seealso cref='MyClass.explicit operator [|D(MyClass)|]'/>
public class MyClass2
{
}";

            var expectedText =
@"using N1;

namespace N1
{
    public class D { }
}

public class MyClass
{
    public static explicit operator N1.D(MyClass f)
    {
        return default(N1.D);
    }
}

/// <seealso cref='MyClass.explicit operator D(MyClass)'/>
public class MyClass2
{
}";

            var options = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);

            Test(initialText, expectedText, parseOptions: options);
        }

        [WorkItem(916368)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForCref4()
        {
            var initialText =
@"namespace N1
{
    public class D { }
}

/// <seealso cref='[|Test(D)|]'/>
public class MyClass
{
    public void Test(N1.D i)
    {
    }
}";

            var expectedText =
@"using N1;

namespace N1
{
    public class D { }
}

/// <seealso cref='Test(D)'/>
public class MyClass
{
    public void Test(N1.D i)
    {
    }
}";

            var options = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);

            Test(initialText, expectedText, parseOptions: options);
        }

        [WorkItem(773614)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddStaticType()
        {
            var initialText =
@"using System;

public static class Outer
{
    [AttributeUsage(AttributeTargets.All)]
    public class MyAttribute : Attribute
    {

    }
}

[[|My|]]
class Test
{}";

            var expectedText =
@"using System;
using static Outer;

public static class Outer
{
    [AttributeUsage(AttributeTargets.All)]
    public class MyAttribute : Attribute
    {

    }
}

[My]
class Test
{}";

            Test(initialText, expectedText);
        }

        [WorkItem(773614)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddStaticType2()
        {
            var initialText =
@"using System;

public static class Outer
{
    public static class Inner
    {
        [AttributeUsage(AttributeTargets.All)]
        public class MyAttribute : Attribute
        {
        }
    }
}

[[|My|]]
class Test
{}";

            var expectedText =
@"using System;
using static Outer.Inner;

public static class Outer
{
    public static class Inner
    {
        [AttributeUsage(AttributeTargets.All)]
        public class MyAttribute : Attribute
        {
        }
    }
}

[My]
class Test
{}";

            Test(initialText, expectedText);
        }

        [WorkItem(773614)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddStaticType3()
        {
            var initialText =
@"using System;

public static class Outer
{
    public class Inner
    {
        [AttributeUsage(AttributeTargets.All)]
        public class MyAttribute : Attribute
        {
        }
    }
}

[[|My|]]
class Test
{}";
            TestMissing(initialText);
        }

        [WorkItem(773614)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddStaticType4()
        {
            var initialText =
@"using System;
using Outer;

public static class Outer
{
    public static class Inner
    {
        [AttributeUsage(AttributeTargets.All)]
        public class MyAttribute : Attribute
        {
        }
    }
}

[[|My|]]
class Test
{}";

            var expectedText =
@"using System;
using Outer;
using static Outer.Inner;

public static class Outer
{
    public static class Inner
    {
        [AttributeUsage(AttributeTargets.All)]
        public class MyAttribute : Attribute
        {
        }
    }
}

[My]
class Test
{}";

            Test(initialText, expectedText);
        }

        [WorkItem(991463)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddInsideUsingDirective1()
        {
            Test(
@"namespace ns { using B = [|Byte|]; }",
@"using System; namespace ns { using B = Byte; }");
        }

        [WorkItem(991463)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddInsideUsingDirective2()
        {
            Test(
@"using System.Collections; namespace ns { using B = [|Byte|]; }",
@"using System; using System.Collections; namespace ns { using B = Byte; }");
        }

        [WorkItem(991463)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddInsideUsingDirective3()
        {
            Test(
@"namespace ns2 { namespace ns3 { namespace ns { using B = [|Byte|]; namespace ns4 { } } } }",
@"using System; namespace ns2 { namespace ns3 { namespace ns { using B = Byte; namespace ns4 { } } } }");
        }

        [WorkItem(991463)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddInsideUsingDirective4()
        {
            Test(
@"namespace ns2 { using System.Collections; namespace ns3 { namespace ns { using System.IO; using B = [|Byte|]; } } }",
@"namespace ns2 { using System; using System.Collections; namespace ns3 { namespace ns { using System.IO; using B = Byte; } } }");
        }

        [WorkItem(991463)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddInsideUsingDirective5()
        {
            Test(
@"using System.IO; namespace ns2 { using System.Diagnostics; namespace ns3 { using System.Collections; namespace ns { using B = [|Byte|]; } } }",
@"using System.IO; namespace ns2 { using System.Diagnostics; namespace ns3 { using System; using System.Collections; namespace ns { using B = Byte; } } }");
        }

        [WorkItem(991463)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddInsideUsingDirective6()
        {
            TestMissing(
@"using B = [|Byte|];");
        }

        [WorkItem(1033612)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddInsideLambda()
        {
            var initialText =
@"using System;

static void Main(string[] args)
{
    Func<int> f = () => { List<int>[|.|]}
}";

            var expectedText =
@"using System;
using System.Collections.Generic;

static void Main(string[] args)
{
    Func<int> f = () => { List<int>.}
}";
            Test(initialText, expectedText);
        }

        [WorkItem(1033612)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddInsideLambda2()
        {
            var initialText =
@"using System;

static void Main(string[] args)
{
    Func<int> f = () => { [|List<int>|]}
}";

            var expectedText =
@"using System;
using System.Collections.Generic;

static void Main(string[] args)
{
    Func<int> f = () => { List<int>}
}";
            Test(initialText, expectedText);
        }

        [WorkItem(1033612)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddInsideLambda3()
        {
            var initialText =
@"using System;

static void Main(string[] args)
{
    Func<int> f = () => { 
        var a = 3;
        List<int>[|.|]
        return a;
        };
}";

            var expectedText =
@"using System;
using System.Collections.Generic;

static void Main(string[] args)
{
    Func<int> f = () => { 
        var a = 3;
        List<int>.
        return a;
        };
}";
            Test(initialText, expectedText);
        }

        [WorkItem(1033612)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddInsideLambda4()
        {
            var initialText =
@"using System;

static void Main(string[] args)
{
    Func<int> f = () => { 
        var a = 3;
        [|List<int>|]
        return a;
        };
}";

            var expectedText =
@"using System;
using System.Collections.Generic;

static void Main(string[] args)
{
    Func<int> f = () => { 
        var a = 3;
        List<int>
        return a;
        };
}";
            Test(initialText, expectedText);
        }

        [WorkItem(1064748)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddConditionalAccessExpression()
        {
            var initialText =
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <Document FilePath = ""Program"">
public class C
{
    void Main(C a)
    {
        C x = a?[|.B()|];
    }
}
       </Document>
       <Document FilePath = ""Extensions"">
namespace Extensions
{
    public static class E
    {
        public static C B(this C c) { return c; }
    }
}
        </Document>
    </Project>
</Workspace> ";

            var expectedText =
@"using Extensions;
public class C
{
    void Main(C a)
    {
        C x = a?.B();
    }
}";
            Test(initialText, expectedText);
        }

        [WorkItem(1064748)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddConditionalAccessExpression2()
        {
            var initialText =
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <Document FilePath = ""Program"">
public class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.[|C()|];
    }

    public class E
    {
    }
}
       </Document>
       <Document FilePath = ""Extensions"">
namespace Extensions
{
    public static class D
    {
        public static C.E C(this C.E c) { return c; }
    }
}
        </Document>
    </Project>
</Workspace> ";

            var expectedText =
@"using Extensions;
public class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.C();
    }

    public class E
    {
    }
}";
            Test(initialText, expectedText);
        }

        [WorkItem(1089138)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAmbiguousUsingName()
        {
            Test(
@"namespace ClassLibrary1 { using System ; public class SomeTypeUser { [|SomeType|] field ; } } namespace SubNamespaceName { using System ; class SomeType { } } namespace ClassLibrary1 . SubNamespaceName { using System ; class SomeOtherFile { } } ",
@"namespace ClassLibrary1 { using System ; using global::SubNamespaceName ; public class SomeTypeUser { SomeType field ; } } namespace SubNamespaceName { using System ; class SomeType { } } namespace ClassLibrary1 . SubNamespaceName { using System ; class SomeOtherFile { } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingInDirective()
        {
            Test(
@"#define DEBUG
#if DEBUG 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
#endif
class Program { static void Main ( string [ ] args ) { var a = [|File|] . OpenRead ( """" ) ; } } ",
@"#define DEBUG
#if DEBUG 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
#endif
class Program { static void Main ( string [ ] args ) { var a = File . OpenRead ( """" ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingInDirective2()
        {
            Test(
@"#define DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if DEBUG
using System.Text;
#endif
class Program { static void Main ( string [ ] args ) { var a = [|File|] . OpenRead ( """" ) ; } } ",
@"#define DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
#if DEBUG
using System.Text;
#endif
class Program { static void Main ( string [ ] args ) { var a = File . OpenRead ( """" ) ; } } ", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingInDirective3()
        {
            Test(
@"#define DEBUG
using System;
using System.Collections.Generic;
#if DEBUG
using System.Text;
#endif
using System.Linq;
using System.Threading.Tasks;
class Program { static void Main ( string [ ] args ) { var a = [|File|] . OpenRead ( """" ) ; } } ",
@"#define DEBUG
using System;
using System.Collections.Generic;
#if DEBUG
using System.Text;
#endif
using System.Linq;
using System.Threading.Tasks;
using System.IO;

class Program { static void Main ( string [ ] args ) { var a = File . OpenRead ( """" ) ; } } ", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingInDirective4()
        {
            Test(
@"#define DEBUG
#if DEBUG
using System;
#endif
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
class Program { static void Main ( string [ ] args ) { var a = [|File|] . OpenRead ( """" ) ; } } ",
@"#define DEBUG
#if DEBUG
using System;
#endif
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

class Program { static void Main ( string [ ] args ) { var a = File . OpenRead ( """" ) ; } } ", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestInaccessibleExtensionMethod()
        {
            const string initial = @"
namespace N1
{
    public static class C
    {
        private static bool ExtMethod1(this string arg1)
        {
            return true;
        }
    }
}

namespace N2
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = ""str1"".[|ExtMethod1()|];
        }
    }
}";
            TestMissing(initial);
        }

        [WorkItem(1116011)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForProperty()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; class Program { public BindingFlags BindingFlags { get { return BindingFlags . [|Instance|] ; } } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Reflection ; using System . Threading . Tasks ; class Program { public BindingFlags BindingFlags { get { return BindingFlags . Instance ; } } } ");
        }

        [WorkItem(1116011)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForField()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; class Program { public B B { get { return B . [|Instance|] ; } } } namespace A { public class B { public static readonly B Instance ; } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; using A ; class Program { public B B { get { return B . Instance ; } } } namespace A { public class B { public static readonly B Instance ; } } ");
        }

        [WorkItem(1893, "https://github.com/dotnet/roslyn/issues/1893")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestNameSimplification()
        {
            // Generated using directive must be simplified from "using A.B;" to "using B;" below.
            Test(
@"
namespace A.B
{
    class T1 { }
}
namespace A.C
{
    using System;
    class T2
    {
        void Test()
        {
            Console.WriteLine();
            [|T1|] t1;
        }
    }
}",
@"
namespace A.B
{
    class T1 { }
}
namespace A.C
{
    using System;
    using B;
    class T2
    {
        void Test()
        {
            Console.WriteLine();
            T1 t1;
        }
    }
}");
        }

        [WorkItem(935, "https://github.com/dotnet/roslyn/issues/935")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingWithOtherExtensionsInScope()
        {
            Test(
@"using System . Linq ; using System . Collections ; using X ; namespace X { public static class Ext { public static void ExtMethod ( this int a ) { } } } namespace Y { public static class Ext { public static void ExtMethod ( this int a , int v ) { } } } public class B { static void Main ( ) { var b = 0 ; b . [|ExtMethod|] ( 0 ) ; } } ",
@"using System . Linq ; using System . Collections ; using X ; using Y ; namespace X { public static class Ext { public static void ExtMethod ( this int a ) { } } } namespace Y { public static class Ext { public static void ExtMethod ( this int a , int v ) { } } } public class B { static void Main ( ) { var b = 0 ; b . ExtMethod ( 0 ) ; } } ");
        }

        [WorkItem(935, "https://github.com/dotnet/roslyn/issues/935")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingWithOtherExtensionsInScope2()
        {
            Test(
@"using System . Linq ; using System . Collections ; using X ; namespace X { public static class Ext { public static void ExtMethod ( this int ? a ) { } } } namespace Y { public static class Ext { public static void ExtMethod ( this int ? a , int v ) { } } } public class B { static void Main ( ) { var b = new int ? ( ) ; b ? [|. ExtMethod|] ( 0 ) ; } } ",
@"using System . Linq ; using System . Collections ; using X ; using Y ; namespace X { public static class Ext { public static void ExtMethod ( this int ? a ) { } } } namespace Y { public static class Ext { public static void ExtMethod ( this int ? a , int v ) { } } } public class B { static void Main ( ) { var b = new int ? ( ) ; b ? . ExtMethod ( 0 ) ; } } ");
        }

        [WorkItem(562, "https://github.com/dotnet/roslyn/issues/562")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingWithOtherExtensionsInScope3()
        {
            Test(
@"using System . Linq ; class C { int i = 0 . [|All|] ( ) ; } namespace X { static class E { public static int All ( this int o ) => 0 ; } } ",
@"using System . Linq ; using X ; class C { int i = 0 . All ( ) ; } namespace X { static class E { public static int All ( this int o ) => 0 ; } } ");
        }

        [WorkItem(562, "https://github.com/dotnet/roslyn/issues/562")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingWithOtherExtensionsInScope4()
        {
            Test(
@"using System . Linq ; class C { static void Main ( string [ ] args ) { var a = new int ? ( ) ; int ? i = a ? [|. All|] ( ) ; } } namespace X { static class E { public static int ? All ( this int ? o ) => 0 ; } } ",
@"using System . Linq ; using X ; class C { static void Main ( string [ ] args ) { var a = new int ? ( ) ; int ? i = a ? . All ( ) ; } } namespace X { static class E { public static int ? All ( this int ? o ) => 0 ; } } ");
        }

        [WorkItem(3080, "https://github.com/dotnet/roslyn/issues/3080")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestNestedNamespaceSimplified()
        {
            Test(
@"namespace Microsoft . MyApp { using Win32 ; class Program { static void Main ( string [ ] args ) { [|SafeRegistryHandle|] h ; } } } ",
@"namespace Microsoft . MyApp { using Win32 ; using Win32 . SafeHandles ; class Program { static void Main ( string [ ] args ) { SafeRegistryHandle h ; } } } ");
        }

        [WorkItem(3080, "https://github.com/dotnet/roslyn/issues/3080")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestNestedNamespaceSimplified2()
        {
            Test(
@"namespace Microsoft . MyApp { using Zin32 ; class Program { static void Main ( string [ ] args ) { [|SafeRegistryHandle|] h ; } } } ",
@"namespace Microsoft . MyApp { using Win32 . SafeHandles ; using Zin32 ; class Program { static void Main ( string [ ] args ) { SafeRegistryHandle h ; } } } ");
        }

        [WorkItem(3080, "https://github.com/dotnet/roslyn/issues/3080")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestNestedNamespaceSimplified3()
        {
            Test(
@"namespace Microsoft . MyApp { using System ; using Win32 ; class Program { static void Main ( string [ ] args ) { [|SafeRegistryHandle|] h ; } } } ",
@"namespace Microsoft . MyApp { using System ; using Win32 ; using Win32 . SafeHandles ; class Program { static void Main ( string [ ] args ) { SafeRegistryHandle h ; } } } ");
        }

        [WorkItem(3080, "https://github.com/dotnet/roslyn/issues/3080")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestNestedNamespaceSimplified4()
        {
            Test(
@"namespace Microsoft . MyApp { using System ; using Zin32 ; class Program { static void Main ( string [ ] args ) { [|SafeRegistryHandle|] h ; } } } ",
@"namespace Microsoft . MyApp { using System ; using Win32 . SafeHandles ; using Zin32 ; class Program { static void Main ( string [ ] args ) { SafeRegistryHandle h ; } } } ");
        }

        [WorkItem(3080, "https://github.com/dotnet/roslyn/issues/3080")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestNestedNamespaceSimplified5()
        {
            Test(
@"namespace Microsoft.MyApp
{
#if true
    using Win32;
#else
    using System;
#endif
    class Program
    {
        static void Main(string[] args)
        {
            [|SafeRegistryHandle|] h;
        }
    }
}",
@"namespace Microsoft.MyApp
{
#if true
    using Win32;
    using Win32.SafeHandles;
#else
    using System;
#endif
    class Program
    {
        static void Main(string[] args)
        {
            SafeRegistryHandle h;
        }
    }
}");
        }

        [WorkItem(3080, "https://github.com/dotnet/roslyn/issues/3080")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestNestedNamespaceSimplified6()
        {
            Test(
@"namespace Microsoft.MyApp
{
    using System;
#if false
    using Win32;
#endif
    using Win32;
    class Program
    {
        static void Main(string[] args)
        {
            [|SafeRegistryHandle|] h;
        }
    }
}",
@"namespace Microsoft.MyApp
{
    using System;
#if false
    using Win32;
#endif
    using Win32;
    using Win32.SafeHandles;
    class Program
    {
        static void Main(string[] args)
        {
            SafeRegistryHandle h;
        }
    }
}");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingOrdinalUppercase()
        {
            Test(
@"namespace A { class A { static void Main ( string [ ] args ) { var b = new [|B|] ( ) ; } } } namespace lowercase { class b { } } namespace Uppercase { class B { } } ",
@"using Uppercase ; namespace A { class A { static void Main ( string [ ] args ) { var b = new B ( ) ; } } } namespace lowercase { class b { } } namespace Uppercase { class B { } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingOrdinalLowercase()
        {
            Test(
@"namespace A { class A { static void Main ( string [ ] args ) { var a = new [|b|] ( ) ; } } } namespace lowercase { class b { } } namespace Uppercase { class B { } } ",
@"using lowercase ; namespace A { class A { static void Main ( string [ ] args ) { var a = new b ( ) ; } } } namespace lowercase { class b { } } namespace Uppercase { class B { } } ");
        }

        public partial class AddUsingTestsWithAddImportDiagnosticProvider : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
        {
            internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            {
                return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(
                        new CSharpUnboundIdentifiersDiagnosticAnalyzer(),
                        new CSharpAddImportCodeFixProvider());
            }

            private void Test(
                 string initialMarkup,
                 string expected,
                 bool systemSpecialCase,
                 int index = 0)
            {
                Test(initialMarkup, expected, index: index, options: new Dictionary<OptionKey, object>
                {
                    { new OptionKey(OrganizerOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp), systemSpecialCase }
                });
            }

            [WorkItem(752640)]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public void TestUnknownIdentifierWithSyntaxError()
            {
                Test(
    @"class C { [|Directory|] private int i ; } ",
    @"using System . IO ; class C { Directory private int i ; } ");
            }

            [WorkItem(829970)]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public void TestUnknownIdentifierInAttributeSyntaxWithoutTarget()
            {
                Test(
    @"class C { [[|Extension|]] } ",
    @"using System.Runtime.CompilerServices; class C { [Extension] } ");
            }

            [WorkItem(829970)]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public void TestUnknownIdentifierGenericName()
            {
                Test(
    @"class C { private [|List<int>|] } ",
    @"using System.Collections.Generic; class C { private List<int> } ");
            }

            [WorkItem(855748)]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public void TestGenericNameWithBrackets()
            {
                Test(
    @"class Class { [|List|] }",
    @"using System.Collections.Generic; class Class { List }");

                Test(
    @"class Class { [|List<>|] }",
    @"using System.Collections.Generic; class Class { List<> }");

                Test(
    @"class Class { List[|<>|] }",
    @"using System.Collections.Generic; class Class { List<> }");
            }

            [WorkItem(867496)]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public void TestMalformedGenericParameters()
            {
                Test(
    @"class Class { [|List<|] }",
    @"using System.Collections.Generic; class Class { List< }");

                Test(
    @"class Class { [|List<Y x;|] }",
    @"using System.Collections.Generic; class Class { List<Y x; }");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public void TestOutsideOfMethodWithMalformedGenericParameters()
            {
                Test(
    @"using System ; class Program { Func < [|FlowControl|] x } ",
    @"using System ; using System . Reflection . Emit ; class Program { Func < FlowControl x } ");
            }

            [WorkItem(1744, @"https://github.com/dotnet/roslyn/issues/1744")]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public void TestIncompleteCatchBlockInLambda()
            {
                Test(
    @"class A { System . Action a = ( ) => { try { } catch ( [|Exception|] ",
    @"using System ; class A { System . Action a = ( ) => { try { } catch ( Exception ");
            }

            [WorkItem(1239, @"https://github.com/dotnet/roslyn/issues/1239")]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public void TestIncompleteLambda1()
            {
                Test(
    @"using System . Linq ; class C { C ( ) { """" . Select ( ( ) => { new [|Byte|] ",
    @"using System ; using System . Linq ; class C { C ( ) { """" . Select ( ( ) => { new Byte ");
            }

            [WorkItem(1239, @"https://github.com/dotnet/roslyn/issues/1239")]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
            public void TestIncompleteLambda2()
            {
                Test(
    @"using System . Linq ; class C { C ( ) { """" . Select ( ( ) => { new [|Byte|] ( ) } ",
    @"using System ; using System . Linq ; class C { C ( ) { """" . Select ( ( ) => { new Byte ( ) } ");
            }
        }
    }
}
