// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddUsing
{
    public partial class AddUsingTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestWhereExtension()
        {
            Test(
@"using System ; using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var q = args . [|Where|] } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { var q = args . Where } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSelectExtension()
        {
            Test(
@"using System ; using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var q = args . [|Select|] } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { var q = args . Select } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGroupByExtension()
        {
            Test(
@"using System ; using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var q = args . [|GroupBy|] } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { var q = args . GroupBy } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestJoinExtension()
        {
            Test(
@"using System ; using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var q = args . [|Join|] } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { var q = args . Join } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void RegressionFor8455()
        {
            TestMissing(
@"class C { void M ( ) { int dim = ( int ) Math . [|Min|] ( ) ; } } ");
        }

        [WorkItem(772321)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestExtensionWithThePresenceOfTheSameNameNonExtensionMethod()
        {
            Test(
@"namespace NS1 { class Program { void Main() { [|new C().Foo(4);|] } } class C { public void Foo(string y) { } } } namespace NS2 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ",
@"using NS2; namespace NS1 { class Program { void Main() { new C().Foo(4); } } class C { public void Foo(string y) { } } } namespace NS2 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ");
        }

        [WorkItem(772321)]
        [WorkItem(920398)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestExtensionWithThePresenceOfTheSameNameNonExtensionPrivateMethod()
        {
            Test(
@"namespace NS1 { class Program { void Main() { [|new C().Foo(4);|] } } class C { private void Foo(int x) { } } } namespace NS2 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ",
@"using NS2; namespace NS1 { class Program { void Main() { new C().Foo(4); } } class C { private void Foo(int x) { } } } namespace NS2 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ");
        }

        [WorkItem(772321)]
        [WorkItem(920398)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestExtensionWithThePresenceOfTheSameNameExtensionPrivateMethod()
        {
            Test(
@"using NS2; namespace NS1 { class Program { void Main() { [|new C().Foo(4);|] } } class C { } } namespace NS2 { static class CExt { private static void Foo(this NS1.C c, int x) { } } } namespace NS3 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ",
@"using NS2; using NS3; namespace NS1 { class Program { void Main() { new C().Foo(4); } } class C { } } namespace NS2 { static class CExt { private static void Foo(this NS1.C c, int x) { } } } namespace NS3 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ");
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { [|1|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { 1 } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
parseOptions: null);
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod2()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { 1 , 2 , [|3|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { 1 , 2 , 3 } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
parseOptions: null);
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod3()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { 1 , [|2|] , 3 } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { 1 , 2 , 3 } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
parseOptions: null);
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod4()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , [|{ 4 , 5 , 6 }|] , { 7 , 8 , 9 } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { 4 , 5 , 6 } , { 7 , 8 , 9 } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
parseOptions: null);
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod5()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { 4 , 5 , 6 } , [|{ 7 , 8 , 9 }|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { 4 , 5 , 6 } , { 7 , 8 , 9 } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
parseOptions: null);
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod6()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { ""Four"" , ""Five"" , ""Six"" } , [|{ '7' , '8' , '9' }|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
parseOptions: null);
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod7()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , [|{ ""Four"" , ""Five"" , ""Six"" }|] , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
parseOptions: null);
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod8()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { [|{ 1 , 2 , 3 }|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
parseOptions: null);
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod9()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { [|""This""|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { ""This"" } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
parseOptions: null);
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod10()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { [|{ 1 , 2 , 3 }|] , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } namespace Ext2 { static class Extensions { public static void Add ( this X x , object [ ] i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } namespace Ext2 { static class Extensions { public static void Add ( this X x , object [ ] i ) { } } } ",
parseOptions: null);
        }

        [WorkItem(269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExtentionMethod11()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { [|{ 1 , 2 , 3 }|] , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } namespace Ext2 { static class Extensions { public static void Add ( this X x , object [ ] i ) { } } } ",
@"using System ; using System . Collections ; using Ext2 ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } namespace Ext2 { static class Extensions { public static void Add ( this X x , object [ ] i ) { } } } ",
index: 1,
parseOptions: null);
        }

        [WorkItem(3818, "https://github.com/dotnet/roslyn/issues/3818")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void InExtensionMethodUnderConditionalAccessExpression()
        {
            var initialText =
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <Document FilePath = ""Program"">
namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            string myString = ""Sample"";
            var other = myString?[|.StringExtension()|].Substring(0);
        }
    }
}
       </Document>
       <Document FilePath = ""Extensions"">
namespace Sample.Extensions
{
    public static class StringExtensions
    {
        public static string StringExtension(this string s)
        {
            return ""Ok"";
        }
    }
}
        </Document>
    </Project>
</Workspace>";

            var expectedText =
@"using Sample.Extensions;
namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            string myString = ""Sample"";
            var other = myString?.StringExtension().Substring(0);
        }
    }
}";
            Test(initialText, expectedText);
        }

        [WorkItem(3818, "https://github.com/dotnet/roslyn/issues/3818")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void InExtensionMethodUnderMultipleConditionalAccessExpressions()
        {
            var initialText =
  @"<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <Document FilePath = ""Program"">
public class C
{
    public T F&lt;T&gt;(T x)
    {
        return F(new C())?.F(new C())?[|.Extn()|];
    }
}
       </Document>
       <Document FilePath = ""Extensions"">
namespace Sample.Extensions
{
    public static class Extensions
    {
        public static C Extn(this C obj)
        {
            return obj.F(new C());
        }
    }
}
        </Document>
    </Project>
</Workspace>";

            var expectedText =
@"using Sample.Extensions;
public class C
{
    public T F<T>(T x)
    {
        return F(new C())?.F(new C())?.Extn();
    }
}";
            Test(initialText, expectedText);
        }

        [WorkItem(3818, "https://github.com/dotnet/roslyn/issues/3818")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void InExtensionMethodUnderMultipleConditionalAccessExpressions2()
        {
            var initialText =
  @"<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <Document FilePath = ""Program"">
public class C
{
    public T F&lt;T&gt;(T x)
    {
        return F(new C())?.F(new C())[|.Extn()|]?.F(newC());
    }
}
       </Document>
       <Document FilePath = ""Extensions"">
namespace Sample.Extensions
{
    public static class Extensions
    {
        public static C Extn(this C obj)
        {
            return obj.F(new C());
        }
    }
}
        </Document>
    </Project>
</Workspace>";

            var expectedText =
@"using Sample.Extensions;
public class C
{
    public T F<T>(T x)
    {
        return F(new C())?.F(new C()).Extn()?.F(newC());
    }
}";
            Test(initialText, expectedText);
        }
    }
}
