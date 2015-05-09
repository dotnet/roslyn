// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddUsing
{
    public partial class AddUsingTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestWhereExtension()
        {
            Test(
@"using System ; using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var q = args . [|Where|] } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { var q = args . Where } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestSelectExtension()
        {
            Test(
@"using System ; using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var q = args . [|Select|] } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { var q = args . Select } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestGroupByExtension()
        {
            Test(
@"using System ; using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var q = args . [|GroupBy|] } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { var q = args . GroupBy } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestJoinExtension()
        {
            Test(
@"using System ; using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var q = args . [|Join|] } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { var q = args . Join } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void RegressionFor8455()
        {
            TestMissing(
@"class C { void M ( ) { int dim = ( int ) Math . [|Min|] ( ) ; } } ");
        }

        [WorkItem(772321)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestExtensionWithThePresenceOfTheSameNameNonExtensionMethod()
        {
            Test(
@"namespace NS1 { class Program { void Main() { [|new C().Foo(4);|] } } class C { public void Foo(string y) { } } } namespace NS2 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ",
@"using NS2; namespace NS1 { class Program { void Main() { new C().Foo(4); } } class C { public void Foo(string y) { } } } namespace NS2 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ");
        }

        [WorkItem(772321)]
        [WorkItem(920398)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestExtensionWithThePresenceOfTheSameNameNonExtensionPrivateMethod()
        {
            Test(
@"namespace NS1 { class Program { void Main() { [|new C().Foo(4);|] } } class C { private void Foo(int x) { } } } namespace NS2 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ",
@"using NS2; namespace NS1 { class Program { void Main() { new C().Foo(4); } } class C { private void Foo(int x) { } } } namespace NS2 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ");
        }

        [WorkItem(772321)]
        [WorkItem(920398)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestExtensionWithThePresenceOfTheSameNameExtensionPrivateMethod()
        {
            Test(
@"using NS2; namespace NS1 { class Program { void Main() { [|new C().Foo(4);|] } } class C { } } namespace NS2 { static class CExt { private static void Foo(this NS1.C c, int x) { } } } namespace NS3 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ",
@"using NS2; using NS3; namespace NS1 { class Program { void Main() { new C().Foo(4); } } class C { } } namespace NS2 { static class CExt { private static void Foo(this NS1.C c, int x) { } } } namespace NS3 { static class CExt { public static void Foo(this NS1.C c, int x) { } } } ");
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { [|1|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { 1 } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
null, 0, true, true, null, false, null);
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod2()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { 1 , 2 , [|3|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { 1 , 2 , 3 } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
null, 0, true, true, null, false, null);
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod3()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { 1 , [|2|] , 3 } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { 1 , 2 , 3 } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
null, 0, true, true, null, false, null);
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod4()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , [|{ 4 , 5 , 6 }|] , { 7 , 8 , 9 } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { 4 , 5 , 6 } , { 7 , 8 , 9 } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
null, 0, true, true, null, false, null);
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod5()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { 4 , 5 , 6 } , [|{ 7 , 8 , 9 }|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { 4 , 5 , 6 } , { 7 , 8 , 9 } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
null, 0, true, true, null, false, null);
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod6()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { ""Four"" , ""Five"" , ""Six"" } , [|{ '7' , '8' , '9' }|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
null, 0, true, true, null, false, null);
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod7()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , [|{ ""Four"" , ""Five"" , ""Six"" }|] , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
null, 0, true, true, null, false, null);
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod8()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { [|{ 1 , 2 , 3 }|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
null, 0, true, true, null, false, null);
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod9()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { [|""This""|] } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { ""This"" } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } ",
null, 0, true, true, null, false, null);
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod10()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { [|{ 1 , 2 , 3 }|] , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } namespace Ext2 { static class Extensions { public static void Add ( this X x , object [ ] i ) { } } } ",
@"using System ; using System . Collections ; using Ext ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } namespace Ext2 { static class Extensions { public static void Add ( this X x , object [ ] i ) { } } } ",
null, 0, true, true, null, false, null);
        }

        [WorkItem(269)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddUsing)]
        public void TestAddUsingForAddExentionMethod11()
        {
            Test(
@"using System ; using System . Collections ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { [|{ 1 , 2 , 3 }|] , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } namespace Ext2 { static class Extensions { public static void Add ( this X x , object [ ] i ) { } } } ",
@"using System ; using System . Collections ; using Ext2 ; class X : IEnumerable { public IEnumerator GetEnumerator ( ) { new X { { 1 , 2 , 3 } , { ""Four"" , ""Five"" , ""Six"" } , { '7' , '8' , '9' } } ; return null ; } } namespace Ext { static class Extensions { public static void Add ( this X x , int i ) { } } } namespace Ext2 { static class Extensions { public static void Add ( this X x , object [ ] i ) { } } } ",
null, 1, true, true, null, false, null);
        }
    }
}
