// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementAbstractClass
{
    public partial class ImplementAbstractClassTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public ImplementAbstractClassTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpImplementAbstractClassCodeFixProvider());

        private OptionsCollection AllOptionsOff
            => new OptionsCollection(GetLanguage())
            {
                 { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            };

        internal Task TestAllOptionsOffAsync(
            string initialMarkup,
            string expectedMarkup,
            int index = 0,
            OptionsCollection options = null,
            ParseOptions parseOptions = null)
        {
            options = options ?? new OptionsCollection(GetLanguage());
            options.AddRange(AllOptionsOff);

            return TestInRegularAndScriptAsync(
                initialMarkup,
                expectedMarkup,
                index: index,
                options: options,
                parseOptions: parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestSimpleMethods()
        {
            await TestAllOptionsOffAsync(
@"abstract class Goo
{
    protected abstract string GooMethod();
    public abstract void Blah();
}

abstract class Bar : Goo
{
    public abstract bool BarMethod();

    public override void Blah()
    {
    }
}

class [|Program|] : Goo
{
    static void Main(string[] args)
    {
    }
}",
@"abstract class Goo
{
    protected abstract string GooMethod();
    public abstract void Blah();
}

abstract class Bar : Goo
{
    public abstract bool BarMethod();

    public override void Blah()
    {
    }
}

class Program : Goo
{
    static void Main(string[] args)
    {
    }

    public override void Blah()
    {
        throw new System.NotImplementedException();
    }

    protected override string GooMethod()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        [WorkItem(16434, "https://github.com/dotnet/roslyn/issues/16434")]
        public async Task TestMethodWithTupleNames()
        {
            await TestAllOptionsOffAsync(
@"abstract class Base
{
    protected abstract (int a, int b) Method((string, string d) x);
}

class [|Program|] : Base
{
}",
@"abstract class Base
{
    protected abstract (int a, int b) Method((string, string d) x);
}

class Program : Base
{
    protected override (int a, int b) Method((string, string d) x)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(543234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543234")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestNotAvailableForStruct()
        {
            await TestMissingInRegularAndScriptAsync(
@"abstract class Goo
{
    public abstract void Bar();
}

struct [|Program|] : Goo
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalIntParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(int x = 3);
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(int x = 3);
}

class b : d
{
    public override void goo(int x = 3)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalCharParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(char x = 'a');
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(char x = 'a');
}

class b : d
{
    public override void goo(char x = 'a')
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalStringParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(string x = ""x"");
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(string x = ""x"");
}

class b : d
{
    public override void goo(string x = ""x"")
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalShortParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(short x = 3);
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(short x = 3);
}

class b : d
{
    public override void goo(short x = 3)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalDecimalParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(decimal x = 3);
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(decimal x = 3);
}

class b : d
{
    public override void goo(decimal x = 3)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalDoubleParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(double x = 3);
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(double x = 3);
}

class b : d
{
    public override void goo(double x = 3)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalLongParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(long x = 3);
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(long x = 3);
}

class b : d
{
    public override void goo(long x = 3)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalFloatParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(float x = 3);
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(float x = 3);
}

class b : d
{
    public override void goo(float x = 3)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalUshortParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(ushort x = 3);
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(ushort x = 3);
}

class b : d
{
    public override void goo(ushort x = 3)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalUintParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(uint x = 3);
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(uint x = 3);
}

class b : d
{
    public override void goo(uint x = 3)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalUlongParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void goo(ulong x = 3);
}

class [|b|] : d
{
}",
@"abstract class d
{
    public abstract void goo(ulong x = 3);
}

class b : d
{
    public override void goo(ulong x = 3)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalStructParameter_CSharp7()
        {
            await TestAllOptionsOffAsync(
@"struct b
{
}

abstract class d
{
    public abstract void goo(b x = new b());
}

class [|c|] : d
{
}",
@"struct b
{
}

abstract class d
{
    public abstract void goo(b x = new b());
}

class c : d
{
    public override void goo(b x = default(b))
    {
        throw new System.NotImplementedException();
    }
}",
                parseOptions: TestOptions.Regular7);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalStructParameter()
        {
            await TestAllOptionsOffAsync(
@"struct b
{
}

abstract class d
{
    public abstract void goo(b x = new b());
}

class [|c|] : d
{
}",
@"struct b
{
}

abstract class d
{
    public abstract void goo(b x = new b());
}

class c : d
{
    public override void goo(b x = default)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalNullableStructParameter()
        {
            await TestAllOptionsOffAsync(
@"struct b
{
}

abstract class d
{
    public abstract void m(b? x = null, b? y = default(b?));
}

class [|c|] : d
{
}",
@"struct b
{
}

abstract class d
{
    public abstract void m(b? x = null, b? y = default(b?));
}

class c : d
{
    public override void m(b? x = null, b? y = null)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalNullableIntParameter()
        {
            await TestAllOptionsOffAsync(
@"abstract class d
{
    public abstract void m(int? x = 5, int? y = default(int?));
}

class [|c|] : d
{
}",
@"abstract class d
{
    public abstract void m(int? x = 5, int? y = default(int?));
}

class c : d
{
    public override void m(int? x = 5, int? y = null)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOptionalObjectParameter()
        {
            await TestAllOptionsOffAsync(
@"class b
{
}

abstract class d
{
    public abstract void goo(b x = null);
}

class [|c|] : d
{
}",
@"class b
{
}

abstract class d
{
    public abstract void goo(b x = null);
}

class c : d
{
    public override void goo(b x = null)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(543883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543883")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestDifferentAccessorAccessibility()
        {
            await TestAllOptionsOffAsync(
@"abstract class c1
{
    public abstract c1 this[c1 x] { get; internal set; }
}

class [|c2|] : c1
{
}",
@"abstract class c1
{
    public abstract c1 this[c1 x] { get; internal set; }
}

class c2 : c1
{
    public override c1 this[c1 x]
    {
        get
        {
            throw new System.NotImplementedException();
        }

        internal set
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestEvent1()
        {
            await TestAllOptionsOffAsync(
@"using System;

abstract class C
{
    public abstract event Action E;
}

class [|D|] : C
{
}",
@"using System;

abstract class C
{
    public abstract event Action E;
}

class D : C
{
    public override event Action E;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestIndexer1()
        {
            await TestAllOptionsOffAsync(
@"using System;

abstract class C
{
    public abstract int this[string s]
    {
        get
        {
        }

        internal set
        {
        }
    }
}

class [|D|] : C
{
}",
@"using System;

abstract class C
{
    public abstract int this[string s]
    {
        get
        {
        }

        internal set
        {
        }
    }
}

class D : C
{
    public override int this[string s]
    {
        get
        {
            throw new NotImplementedException();
        }

        internal set
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestMissingInHiddenType()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

abstract class Goo
{
    public abstract void F();
}

class [|Program|] : Goo
{
#line hidden
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestGenerateIfLocationAvailable()
        {
            await TestAllOptionsOffAsync(
@"#line default
using System;

abstract class Goo { public abstract void F(); }

partial class [|Program|] : Goo
{
    void Bar()
    {
    }

#line hidden
}
#line default",
@"#line default
using System;

abstract class Goo { public abstract void F(); }

partial class Program : Goo
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
#line default");
        }

        [WorkItem(545585, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545585")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestOnlyGenerateUnimplementedAccessors()
        {
            await TestAllOptionsOffAsync(
@"using System;

abstract class A
{
    public abstract int X { get; set; }
}

abstract class B : A
{
    public override int X
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}

class [|C|] : B
{
}",
@"using System;

abstract class A
{
    public abstract int X { get; set; }
}

abstract class B : A
{
    public override int X
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}

class C : B
{
    public override int X
    {
        set
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [WorkItem(545615, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545615")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestParamsArray()
        {
            await TestAllOptionsOffAsync(
@"class A
{
    public virtual void Goo(int x, params int[] y)
    {
    }
}

abstract class B : A
{
    public abstract override void Goo(int x, int[] y = null);
}

class [|C|] : B
{
}",
@"class A
{
    public virtual void Goo(int x, params int[] y)
    {
    }
}

abstract class B : A
{
    public abstract override void Goo(int x, int[] y = null);
}

class C : B
{
    public override void Goo(int x, params int[] y)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545636")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestNullPointerType()
        {
            await TestAllOptionsOffAsync(
@"abstract class C
{
    unsafe public abstract void Goo(int* x = null);
}

class [|D|] : C
{
}",
@"abstract class C
{
    unsafe public abstract void Goo(int* x = null);
}

class D : C
{
    public override unsafe void Goo(int* x = null)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545637")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestErrorTypeCalledVar()
        {
            await TestAllOptionsOffAsync(
@"extern alias var;

abstract class C
{
    public abstract void Goo(var::X x);
}

class [|D|] : C
{
}",
@"extern alias var;

abstract class C
{
    public abstract void Goo(var::X x);
}

class D : C
{
    public override void Goo(X x)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task Bugfix_581500()
        {
            await TestAllOptionsOffAsync(
@"abstract class A<T>
{
    public abstract void M(T x);

    abstract class B : A<B>
    {
        class [|T|] : A<T>
        {
        }
    }
}",
@"abstract class A<T>
{
    public abstract void M(T x);

    abstract class B : A<B>
    {
        class T : A<T>
        {
            public override void M(B.T x)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}");
        }

        [WorkItem(625442, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/625442")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task Bugfix_625442()
        {
            await TestAllOptionsOffAsync(
@"abstract class A<T>
{
    public abstract void M(T x);
    abstract class B : A<B>
    {
        class [|T|] : A<B.T> { }
    }
}
",
@"abstract class A<T>
{
    public abstract void M(T x);
    abstract class B : A<B>
    {
        class T : A<B.T>
        {
            public override void M(A<A<T>.B>.B.T x)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
");
        }

        [WorkItem(2407, "https://github.com/dotnet/roslyn/issues/2407")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task ImplementClassWithInaccessibleMembers()
        {
            await TestAllOptionsOffAsync(
@"using System;
using System.Globalization;

public class [|x|] : EastAsianLunisolarCalendar
{
}",
@"using System;
using System.Globalization;

public class x : EastAsianLunisolarCalendar
{
    public override int[] Eras
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    internal override int MinCalendarYear
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    internal override int MaxCalendarYear
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    internal override EraInfo[] CalEraInfo
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    internal override DateTime MinDate
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    internal override DateTime MaxDate
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override int GetEra(DateTime time)
    {
        throw new NotImplementedException();
    }

    internal override int GetGregorianYear(int year, int era)
    {
        throw new NotImplementedException();
    }

    internal override int GetYear(int year, DateTime time)
    {
        throw new NotImplementedException();
    }

    internal override int GetYearInfo(int LunarYear, int Index)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(13149, "https://github.com/dotnet/roslyn/issues/13149")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestPartialClass1()
        {
            await TestAllOptionsOffAsync(
@"using System;

public abstract class Base
{
    public abstract void Dispose();
}

partial class [|A|] : Base
{
}

partial class A
{
}",
@"using System;

public abstract class Base
{
    public abstract void Dispose();
}

partial class A : Base
{
    public override void Dispose()
    {
        throw new NotImplementedException();
    }
}

partial class A
{
}");
        }

        [WorkItem(13149, "https://github.com/dotnet/roslyn/issues/13149")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestPartialClass2()
        {
            await TestAllOptionsOffAsync(
@"using System;

public abstract class Base
{
    public abstract void Dispose();
}

partial class [|A|]
{
}

partial class A : Base
{
}",
@"using System;

public abstract class Base
{
    public abstract void Dispose();
}

partial class A
{
    public override void Dispose()
    {
        throw new NotImplementedException();
    }
}

partial class A : Base
{
}");
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestCodeStyle_Method1()
        {
            await TestInRegularAndScriptAsync(
@"abstract class A
{
    public abstract void M(int x);
}

class [|T|] : A
{
}",
@"abstract class A
{
    public abstract void M(int x);
}

class T : A
{
    public override void M(int x) => throw new System.NotImplementedException();
}", options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement));
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestCodeStyle_Property1()
        {
            await TestInRegularAndScriptAsync(
@"abstract class A
{
    public abstract int M { get; }
}

class [|T|] : A
{
}",
@"abstract class A
{
    public abstract int M { get; }
}

class T : A
{
    public override int M => throw new System.NotImplementedException();
}", options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement));
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestCodeStyle_Property3()
        {
            await TestInRegularAndScriptAsync(
@"abstract class A
{
    public abstract int M { set; }
}

class [|T|] : A
{
}",
@"abstract class A
{
    public abstract int M { set; }
}

class T : A
{
    public override int M
    {
        set
        {
            throw new System.NotImplementedException();
        }
    }
}", options: new OptionsCollection(GetLanguage())
    {
        { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
        { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.Silent },
    });
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestCodeStyle_Property4()
        {
            await TestInRegularAndScriptAsync(
@"abstract class A
{
    public abstract int M { get; set; }
}

class [|T|] : A
{
}",
@"abstract class A
{
    public abstract int M { get; set; }
}

class T : A
{
    public override int M
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}", options: new OptionsCollection(GetLanguage())
    {
        { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
        { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.Silent },
    });
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestCodeStyle_Indexers1()
        {
            await TestInRegularAndScriptAsync(
@"abstract class A
{
    public abstract int this[int i] { get; }
}

class [|T|] : A
{
}",
@"abstract class A
{
    public abstract int this[int i] { get; }
}

class T : A
{
    public override int this[int i] => throw new System.NotImplementedException();
}", options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement));
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestCodeStyle_Indexer3()
        {
            await TestInRegularAndScriptAsync(
@"abstract class A
{
    public abstract int this[int i] { set; }
}

class [|T|] : A
{
}",
@"abstract class A
{
    public abstract int this[int i] { set; }
}

class T : A
{
    public override int this[int i]
    {
        set
        {
            throw new System.NotImplementedException();
        }
    }
}", options: new OptionsCollection(GetLanguage())
    {
        { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
        { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.Silent },
    });
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestCodeStyle_Indexer4()
        {
            await TestInRegularAndScriptAsync(
@"abstract class A
{
    public abstract int this[int i] { get; set; }
}

class [|T|] : A
{
}",
@"abstract class A
{
    public abstract int this[int i] { get; set; }
}

class T : A
{
    public override int this[int i]
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}", options: new OptionsCollection(GetLanguage())
    {
        { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
        { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.Silent },
    });
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestCodeStyle_Accessor1()
        {
            await TestInRegularAndScriptAsync(
@"abstract class A
{
    public abstract int M { get; }
}

class [|T|] : A
{
}",
@"abstract class A
{
    public abstract int M { get; }
}

class T : A
{
    public override int M { get => throw new System.NotImplementedException(); }
}", options: new OptionsCollection(GetLanguage())
    {
        { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never, NotificationOption2.Silent },
        { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent },
    });
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestCodeStyle_Accessor3()
        {
            await TestInRegularAndScriptAsync(
@"abstract class A
{
    public abstract int M { set; }
}

class [|T|] : A
{
}",
@"abstract class A
{
    public abstract int M { set; }
}

class T : A
{
    public override int M { set => throw new System.NotImplementedException(); }
}", options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement));
        }

        [WorkItem(581500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581500")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestCodeStyle_Accessor4()
        {
            await TestInRegularAndScriptAsync(
@"abstract class A
{
    public abstract int M { get; set; }
}

class [|T|] : A
{
}",
@"abstract class A
{
    public abstract int M { get; set; }
}

class T : A
{
    public override int M { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}", options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement));
        }

        [WorkItem(15387, "https://github.com/dotnet/roslyn/issues/15387")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestWithGroupingOff1()
        {
            var options = CodeActionOptions.Default with
            {
                ImplementTypeOptions = new ImplementTypeOptions(InsertionBehavior: ImplementTypeInsertionBehavior.AtTheEnd)
            };

            await TestInRegularAndScriptAsync(
@"abstract class Base
{
    public abstract int Prop { get; }
}

class [|Derived|] : Base
{
    void Goo() { }
}",
@"abstract class Base
{
    public abstract int Prop { get; }
}

class Derived : Base
{
    void Goo() { }

    public override int Prop => throw new System.NotImplementedException();
}", codeActionOptions: options);
        }

        [WorkItem(17274, "https://github.com/dotnet/roslyn/issues/17274")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestAddedUsingWithBanner1()
        {
            await TestInRegularAndScriptAsync(
@"// Copyright ...

using Microsoft.Win32;

namespace My
{
    public abstract class Goo
    {
        public abstract void Bar(System.Collections.Generic.List<object> values);
    }

    public class [|Goo2|] : Goo // Implement Abstract Class
    {
    }
}",
@"// Copyright ...

using System.Collections.Generic;
using Microsoft.Win32;

namespace My
{
    public abstract class Goo
    {
        public abstract void Bar(System.Collections.Generic.List<object> values);
    }

    public class Goo2 : Goo // Implement Abstract Class
    {
        public override void Bar(List<object> values)
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [WorkItem(17562, "https://github.com/dotnet/roslyn/issues/17562")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestNullableOptionalParameters_CSharp7()
        {
            await TestInRegularAndScriptAsync(
@"struct V { }
abstract class B
{
    public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
    public abstract void M2<T>(T? i = null) where T : struct;
}
sealed class [|D|] : B
{
}",
@"struct V { }
abstract class B
{
    public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
    public abstract void M2<T>(T? i = null) where T : struct;
}
sealed class D : B
{
    public override void M1(int i = 0, string s = null, int? j = null, V v = default(V))
    {
        throw new System.NotImplementedException();
    }

    public override void M2<T>(T? i = null)
    {
        throw new System.NotImplementedException();
    }
}",
                parseOptions: TestOptions.Regular7);
        }

        [WorkItem(17562, "https://github.com/dotnet/roslyn/issues/17562")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestNullableOptionalParametersCSharp7()
        {
            await TestAsync(
@"struct V { }
abstract class B
{
    public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
    public abstract void M2<T>(T? i = null) where T : struct;
}
sealed class [|D|] : B
{
}",
@"struct V { }
abstract class B
{
    public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
    public abstract void M2<T>(T? i = null) where T : struct;
}
sealed class D : B
{
    public override void M1(int i = 0, string s = null, int? j = null, V v = default(V))
    {
        throw new System.NotImplementedException();
    }

    public override void M2<T>(T? i = null)
    {
        throw new System.NotImplementedException();
    }
}", parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [WorkItem(17562, "https://github.com/dotnet/roslyn/issues/17562")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestNullableOptionalParameters()
        {
            await TestInRegularAndScriptAsync(
@"struct V { }
abstract class B
{
    public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
    public abstract void M2<T>(T? i = null) where T : struct;
}
sealed class [|D|] : B
{
}",
@"struct V { }
abstract class B
{
    public abstract void M1(int i = 0, string s = null, int? j = null, V v = default(V));
    public abstract void M2<T>(T? i = null) where T : struct;
}
sealed class D : B
{
    public override void M1(int i = 0, string s = null, int? j = null, V v = default)
    {
        throw new System.NotImplementedException();
    }

    public override void M2<T>(T? i = null)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(13932, "https://github.com/dotnet/roslyn/issues/13932")]
        [WorkItem(5898, "https://github.com/dotnet/roslyn/issues/5898")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestAutoProperties()
        {
            var options = CodeActionOptions.Default with
            {
                ImplementTypeOptions = new ImplementTypeOptions(PropertyGenerationBehavior: ImplementTypePropertyGenerationBehavior.PreferAutoProperties)
            };

            await TestInRegularAndScript1Async(
@"abstract class AbstractClass
{
    public abstract int ReadOnlyProp { get; }
    public abstract int ReadWriteProp { get; set; }
    public abstract int WriteOnlyProp { set; }
}

class [|C|] : AbstractClass
{
}",
@"abstract class AbstractClass
{
    public abstract int ReadOnlyProp { get; }
    public abstract int ReadWriteProp { get; set; }
    public abstract int WriteOnlyProp { set; }
}

class C : AbstractClass
{
    public override int ReadOnlyProp { get; }
    public override int ReadWriteProp { get; set; }
    public override int WriteOnlyProp { set => throw new System.NotImplementedException(); }
}", parameters: new TestParameters(codeActionOptions: options));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestInWithMethod_Parameters()
        {
            await TestInRegularAndScriptAsync(
@"abstract class TestParent
{
    public abstract void Method(in int p);
}
public class [|Test|] : TestParent
{
}",
@"abstract class TestParent
{
    public abstract void Method(in int p);
}
public class Test : TestParent
{
    public override void Method(in int p)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestRefReadOnlyWithMethod_ReturnType()
        {
            await TestInRegularAndScriptAsync(
@"abstract class TestParent
{
    public abstract ref readonly int Method();
}
public class [|Test|] : TestParent
{
}",
@"abstract class TestParent
{
    public abstract ref readonly int Method();
}
public class Test : TestParent
{
    public override ref readonly int Method()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestRefReadOnlyWithProperty()
        {
            await TestInRegularAndScriptAsync(
@"abstract class TestParent
{
    public abstract ref readonly int Property { get; }
}
public class [|Test|] : TestParent
{
}",
@"abstract class TestParent
{
    public abstract ref readonly int Property { get; }
}
public class Test : TestParent
{
    public override ref readonly int Property => throw new System.NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestInWithIndexer_Parameters()
        {
            await TestInRegularAndScriptAsync(
@"abstract class TestParent
{
    public abstract int this[in int p] { set; }
}
public class [|Test|] : TestParent
{
}",
@"abstract class TestParent
{
    public abstract int this[in int p] { set; }
}
public class Test : TestParent
{
    public override int this[in int p] { set => throw new System.NotImplementedException(); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestRefReadOnlyWithIndexer_ReturnType()
        {
            await TestInRegularAndScriptAsync(
@"abstract class TestParent
{
    public abstract ref readonly int this[int p] { get; }
}
public class [|Test|] : TestParent
{
}",
@"abstract class TestParent
{
    public abstract ref readonly int this[int p] { get; }
}
public class Test : TestParent
{
    public override ref readonly int this[int p] => throw new System.NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestUnmanagedConstraint()
        {
            await TestInRegularAndScriptAsync(
@"public abstract class ParentTest
{
    public abstract void M<T>() where T : unmanaged;
}
public class [|Test|] : ParentTest
{
}",
@"public abstract class ParentTest
{
    public abstract void M<T>() where T : unmanaged;
}
public class Test : ParentTest
{
    public override void M<T>()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact]
        public async Task NothingOfferedWhenInheritanceIsPreventedByInternalAbstractMember()
        {
            await TestMissingAsync(
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
public abstract class Base
{
    internal abstract void Method();
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class [|Derived|] : Base
{
    Base inner;
}
        </Document>
    </Project>
</Workspace>");
        }

        [WorkItem(30102, "https://github.com/dotnet/roslyn/issues/30102")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestWithIncompleteGenericInBaseList()
        {
            await TestAllOptionsOffAsync(
@"abstract class A<T>
{
    public abstract void AbstractMethod();
}

class [|B|] : A<int
{

}",
@"abstract class A<T>
{
    public abstract void AbstractMethod();
}

class B : A<int
{
    public override void AbstractMethod()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(44907, "https://github.com/dotnet/roslyn/issues/44907")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestWithRecords()
        {
            await TestAllOptionsOffAsync(
@"abstract record A
{
    public abstract void AbstractMethod();
}

record [|B|] : A
{

}",
@"abstract record A
{
    public abstract void AbstractMethod();
}

record B : A
{
    public override void AbstractMethod()
    {
        throw new System.NotImplementedException();
    }
}", parseOptions: TestOptions.RegularPreview);
        }

        [WorkItem(44907, "https://github.com/dotnet/roslyn/issues/44907")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestWithRecordsWithPositionalMembers()
        {
            await TestAllOptionsOffAsync(
@"abstract record A
{
    public abstract void AbstractMethod();
}

record [|B|](int i) : A
{

}",
@"abstract record A
{
    public abstract void AbstractMethod();
}

record B(int i) : A
{
    public override void AbstractMethod()
    {
        throw new System.NotImplementedException();
    }
}", parseOptions: TestOptions.RegularPreview);
        }

        [WorkItem(48742, "https://github.com/dotnet/roslyn/issues/48742")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestUnconstrainedGenericNullable()
        {
            await TestAllOptionsOffAsync(
@"#nullable enable

abstract class B<T>
{
    public abstract T? M();
}

class [|D|] : B<int>
{
}",
@"#nullable enable

abstract class B<T>
{
    public abstract T? M();
}

class D : B<int>
{
    public override int M()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(48742, "https://github.com/dotnet/roslyn/issues/48742")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestUnconstrainedGenericNullable2()
        {
            await TestAllOptionsOffAsync(
@"#nullable enable

abstract class B<T>
{
    public abstract T? M();
}

class [|D<T>|] : B<T> where T : struct
{
}",
@"#nullable enable

abstract class B<T>
{
    public abstract T? M();
}

class D<T> : B<T> where T : struct
{
    public override T M()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(48742, "https://github.com/dotnet/roslyn/issues/48742")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestUnconstrainedGenericNullable_Tuple()
        {
            await TestAllOptionsOffAsync(
@"#nullable enable

abstract class B<T>
{
    public abstract T? M();
}

class [|D<T>|] : B<(T, T)>
{
}",
@"#nullable enable

abstract class B<T>
{
    public abstract T? M();
}

class D<T> : B<(T, T)>
{
    public override (T, T) M()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(48742, "https://github.com/dotnet/roslyn/issues/48742")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        [InlineData("", "T")]
        [InlineData(" where T : class", "T")]
        [InlineData("", "T?")]
        [InlineData(" where T : class", "T?")]
        [InlineData(" where T : struct", "T?")]
        public async Task TestUnconstrainedGenericNullable_NoRegression(string constraint, string passToBase)
        {
            await TestAllOptionsOffAsync(
$@"#nullable enable

abstract class B<T>
{{
    public abstract T? M();
}}

class [|D<T>|] : B<{passToBase}>{constraint}
{{
}}",
$@"#nullable enable

abstract class B<T>
{{
    public abstract T? M();
}}

class D<T> : B<{passToBase}>{constraint}
{{
    public override T? M()
    {{
        throw new System.NotImplementedException();
    }}
}}");
        }

        [WorkItem(53012, "https://github.com/dotnet/roslyn/issues/53012")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        public async Task TestNullableGenericType()
        {
            await TestAllOptionsOffAsync(
@"abstract class C
{
    public abstract void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d);
}
class [|D|] : C
{
}",
@"abstract class C
{
    public abstract void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d);
}
class D : C
{
    public override void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d)
        where T1 : default
        where T3 : default
    {
        throw new System.NotImplementedException();
    }
}");
        }
    }
}
