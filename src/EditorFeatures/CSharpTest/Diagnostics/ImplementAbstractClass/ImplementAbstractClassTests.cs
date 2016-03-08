// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.ImplementAbstractClass;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.ImplementAbstractClass
{
    public partial class ImplementAbstractClassTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                null, new ImplementAbstractClassCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestSimpleMethods()
        {
            await TestAsync(
@"abstract class Foo { protected abstract string FooMethod ( ) ; public abstract void Blah ( ) ; } abstract class Bar : Foo { public abstract bool BarMethod ( ) ; public override void Blah ( ) { } } class [|Program|] : Foo { static void Main ( string [ ] args ) { } } ",
@"using System ; abstract class Foo { protected abstract string FooMethod ( ) ; public abstract void Blah ( ) ; } abstract class Bar : Foo { public abstract bool BarMethod ( ) ; public override void Blah ( ) { } } class Program : Foo { static void Main ( string [ ] args ) { } public override void Blah ( ) { throw new NotImplementedException ( ) ; } protected override string FooMethod ( ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(543234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543234")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestNotAvailableForStruct()
        {
            await TestMissingAsync(
@"abstract class Foo { public abstract void Bar ( ) ; } struct [|Program|] : Foo { } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalIntParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( int x = 3 ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( int x = 3 ) ; } class b : d { public override void foo ( int x = 3 ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalCharParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( char x = 'a' ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( char x = 'a' ) ; } class b : d { public override void foo ( char x = 'a' ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalStringParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( string x = ""x"" ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( string x = ""x"" ) ; } class b : d { public override void foo ( string x = ""x"" ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalShortParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( short x = 3 ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( short x = 3 ) ; } class b : d { public override void foo ( short x = 3 ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalDecimalParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( decimal x = 3 ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( decimal x = 3 ) ; } class b : d { public override void foo ( decimal x = 3 ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalDoubleParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( double x = 3 ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( double x = 3 ) ; } class b : d { public override void foo ( double x = 3 ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalLongParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( long x = 3 ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( long x = 3 ) ; } class b : d { public override void foo ( long x = 3 ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalFloatParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( float x = 3 ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( float x = 3 ) ; } class b : d { public override void foo ( float x = 3 ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalUshortParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( ushort x = 3 ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( ushort x = 3 ) ; } class b : d { public override void foo ( ushort x = 3 ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalUintParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( uint x = 3 ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( uint x = 3 ) ; } class b : d { public override void foo ( uint x = 3 ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalUlongParameter()
        {
            await TestAsync(
                @"abstract class d { public abstract void foo ( ulong x = 3 ) ; } class [|b|] : d { }",
                @"using System ; abstract class d { public abstract void foo ( ulong x = 3 ) ; } class b : d { public override void foo ( ulong x = 3 ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalStructParameter()
        {
            await TestAsync(
                @"struct b { } abstract class d { public abstract void foo ( b x = new b ( ) ) ; } class [|c|] : d { }",
                @"using System ; struct b { } abstract class d { public abstract void foo ( b x = new b ( ) ) ; } class c : d { public override void foo ( b x = default ( b ) ) { throw new NotImplementedException ( ) ; } }");
        }

        [WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalNullableStructParameter()
        {
            await TestAsync(
@"struct b { } abstract class d { public abstract void m ( b? x = null, b? y = default(b?) ) ; } class [|c|] : d { }",
@"using System ; struct b { } abstract class d { public abstract void m ( b? x = null, b? y = default(b?) ) ; } class c : d { public override void m ( b? x = default(b?), b? y = default(b?) ) { throw new NotImplementedException ( ) ; } }");
        }

        [WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalNullableIntParameter()
        {
            await TestAsync(
@"abstract class d { public abstract void m ( int? x = 5, int? y = default(int?) ) ; } class [|c|] : d { }",
@"using System ; abstract class d { public abstract void m ( int? x = 5, int? y = default(int?) ) ; } class c : d { public override void m ( int? x = 5, int? y = default(int?) ) { throw new NotImplementedException ( ) ; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalObjectParameter()
        {
            await TestAsync(
                @"class b { } abstract class d { public abstract void foo ( b x = null ) ; } class [|c|] : d { }",
                @"using System ; class b { } abstract class d { public abstract void foo ( b x = null ) ; } class c : d { public override void foo ( b x = null ) { throw new NotImplementedException ( ) ; } }");
        }

        [WorkItem(543883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543883")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestDifferentAccessorAccessibility()
        {
            await TestAsync(
@"abstract class c1 { public abstract c1 this [ c1 x ] { get ; internal set ; } } class [|c2|] : c1 { } ",
@"using System ; abstract class c1 { public abstract c1 this [ c1 x ] { get ; internal set ; } } class c2 : c1 { public override c1 this [ c1 x ] { get { throw new NotImplementedException ( ) ; } internal set { throw new NotImplementedException ( ) ; } } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestEvent1()
        {
            await TestAsync(
@"using System ; abstract class C { public abstract event Action E ; } class [|D|] : C { } ",
@"using System ; abstract class C { public abstract event Action E ; } class D : C { public override event Action E ; } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestIndexer1()
        {
            await TestAsync(
@"using System ; abstract class C { public abstract int this [ string s ] { get { } internal set { } } } class [|D|] : C { } ",
@"using System ; abstract class C { public abstract int this [ string s ] { get { } internal set { } } } class D : C { public override int this [ string s ] { get { throw new NotImplementedException ( ) ; } internal set { throw new NotImplementedException ( ) ; } } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestMissingInHiddenType()
        {
            await TestMissingAsync(
@"using System;

abstract class Foo { public abstract void F(); }

class [|Program|] : Foo
{
#line hidden
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestGenerateIntoNonHiddenPart()
        {
            await TestAsync(
@"using System;

abstract class Foo { public abstract void F(); }

partial class [|Program|] : Foo
{
#line hidden
}
#line default

partial class Program ",
@"using System;

abstract class Foo { public abstract void F(); }

partial class Program : Foo
{
#line hidden
}
#line default

partial class Program
{
    public override void F()
    {
        throw new NotImplementedException();
    }
}
",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestGenerateIfLocationAvailable()
        {
            await TestAsync(
@"#line default
using System;

abstract class Foo { public abstract void F(); }

partial class [|Program|] : Foo
{
    void Bar()
    {
    }

#line hidden
}
#line default",
@"#line default
using System;

abstract class Foo { public abstract void F(); }

partial class Program : Foo
{
    public override void F()
    {
        throw new NotImplementedException();
    }

    void Bar()
    {
    }

#line hidden
}
#line default",
compareTokens: false);
        }

        [WorkItem(545585, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545585")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOnlyGenerateUnimplementedAccessors()
        {
            await TestAsync(
@"using System ; abstract class A { public abstract int X { get ; set ; } } abstract class B : A { public override int X { get { throw new NotImplementedException ( ) ; } } } class [|C|] : B { } ",
@"using System ; abstract class A { public abstract int X { get ; set ; } } abstract class B : A { public override int X { get { throw new NotImplementedException ( ) ; } } } class C : B { public override int X { set { throw new NotImplementedException ( ) ; } } } ");
        }

        [WorkItem(545615, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545615")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestParamsArray()
        {
            await TestAsync(
@"class A { public virtual void Foo ( int x , params int [ ] y ) { } } abstract class B : A { public abstract override void Foo ( int x , int [ ] y = null ) ; } class [|C|] : B { } ",
@"using System ; class A { public virtual void Foo ( int x , params int [ ] y ) { } } abstract class B : A { public abstract override void Foo ( int x , int [ ] y = null ) ; } class C : B { public override void Foo ( int x , params int [ ] y ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545636")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestNullPointerType()
        {
            await TestAsync(
@"abstract class C { unsafe public abstract void Foo ( int * x = null ) ; } class [|D|] : C { } ",
@"using System ; abstract class C { unsafe public abstract void Foo ( int * x = null ) ; } class D : C { public override unsafe void Foo ( int * x = null ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(545637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545637")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestErrorTypeCalledVar()
        {
            await TestAsync(
@"extern alias var ; abstract class C { public abstract void Foo ( var :: X x ) ; } class [|D|] : C { } ",
@"extern alias var ; using System ; abstract class C { public abstract void Foo ( var :: X x ) ; } class D : C { public override void Foo ( X x ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task Bugfix_581500()
        {
            await TestAsync(
@"abstract class A<T>
{
    public abstract void M(T x);
    abstract class B : A<B>
    {
        class [|T|] : A<T> { }
    }
}
",
@"using System;

abstract class A<T>
{
    public abstract void M(T x);
    abstract class B : A<B>
    {
        class T : A<T>
        {
            public override void M(B.T x)
            {
                throw new NotImplementedException();
            }
        }
    }
}
");
        }

        [WorkItem(625442, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/625442")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task Bugfix_625442()
        {
            await TestAsync(
@"abstract class A<T>
{
    public abstract void M(T x);
    abstract class B : A<B>
    {
        class [|T|] : A<B.T> { }
    }
}
",
@"using System;

abstract class A<T>
{
    public abstract void M(T x);
    abstract class B : A<B>
    {
        class T : A<B.T>
        {
            public override void M(A<A<T>.B>.B.T x)
            {
                throw new NotImplementedException();
            }
        }
    }
}
", compareTokens: false);
        }

        [WorkItem(2407, "https://github.com/dotnet/roslyn/issues/2407")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task ImplementClassWithInaccessibleMembers()
        {
            await TestAsync(
@"using System ; using System . Globalization ; public class [|x|] : EastAsianLunisolarCalendar { } ",
@"using System ; using System . Globalization ; public class x : EastAsianLunisolarCalendar { public override int [ ] Eras { get { throw new NotImplementedException ( ) ; } } internal override EraInfo [ ] CalEraInfo { get { throw new NotImplementedException ( ) ; } } internal override int MaxCalendarYear { get { throw new NotImplementedException ( ) ; } } internal override DateTime MaxDate { get { throw new NotImplementedException ( ) ; } } internal override int MinCalendarYear { get { throw new NotImplementedException ( ) ; } } internal override DateTime MinDate { get { throw new NotImplementedException ( ) ; } } public override int GetEra ( DateTime time ) { throw new NotImplementedException ( ) ; } internal override int GetGregorianYear ( int year , int era ) { throw new NotImplementedException ( ) ; } internal override int GetYear ( int year , DateTime time ) { throw new NotImplementedException ( ) ; } internal override int GetYearInfo ( int LunarYear , int Index ) { throw new NotImplementedException ( ) ; } } ");
        }
    }
}
