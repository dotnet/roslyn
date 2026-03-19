// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddExplicitCast;

public sealed partial class AddExplicitCastTests
{
    #region "Fix all occurrences tests"

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task CS0266TestFixAllInDocument()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                Base ReturnBase(Derived d)
                {
                    Base b = new Base();
                    return b;
                }

                Derived ReturnDerived(Base b)
                {
                    return b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
                {
                    Base b;
                    yield return b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived()
                {
                    Base b;
                    return b;
                } ]]>

                <![CDATA[ async Task<Derived> M() ]]>
                {
                    Base b;
                    return b;
                }

                Derived ReturnDerived2(Base b)
                {
                    return ReturnBase();
                }

                Derived Foo()
                {
                    <![CDATA[ Func<Base, Base> func = d => d; ]]>
                    Base b;
                    return func(b);
                }
                public Program1()
                {
                    Base b;
                    Derived d = {|FixAllInDocument:b|};
                    d = new Base() { };

                    Derived d2 = ReturnBase();
                    Derived d2 = ReturnBase(b);

                    Test t = new Test();
                    t.D = b;
                    t.d = b;
                    d = t.B;

                    <![CDATA[ Func<Base, Derived> foo = d => d; ]]>

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    d2 = foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                class Test
                {
                    static public explicit operator Derived2(Test t) { return new Derived2();  }
                }

                 Derived2 returnDerived2_1() {
                    return new Derived1();
                }

                 Derived2 returnDerived2_2() {
                    return new Test();
                }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Derived2 derived2 = b1;
                    derived2 = b3;
                    Base2 base2 = b1;
                    derived2 = d1;
                    Derived2 d2 = new Test();
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            public class Program3
            {
                class Base { }
                class Derived : Base { }
                class Derived2 : Derived { }

                Derived2 returnD2(Base b)
                {
                    Derived d;
                    return d = b;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                Base ReturnBase(Derived d)
                {
                    Base b = new Base();
                    return b;
                }

                Derived ReturnDerived(Base b)
                {
                    return (Derived)b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
                {
                    Base b;
                    yield return (Derived)b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived()
                {
                    Base b;
                    return (IEnumerable<Derived>)b;
                } ]]>

                <![CDATA[ async Task<Derived> M() ]]>
                {
                    Base b;
                    return (Derived)b;
                }

                Derived ReturnDerived2(Base b)
                {
                    return (Derived)ReturnBase();
                }

                Derived Foo()
                {
                    <![CDATA[ Func<Base, Base> func = d => d; ]]>
                    Base b;
                    return (Derived)func(b);
                }
                public Program1()
                {
                    Base b;
                    Derived d = (Derived)b;
                    d = new Base() { };

                    Derived d2 = (Derived)ReturnBase();
                    Derived d2 = ReturnBase(b);

                    Test t = new Test();
                    t.D = (Derived)b;
                    t.d = b;
                    d = (Derived)t.B;

                    <![CDATA[ Func<Base, Derived> foo = d => (Derived)d; ]]>

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    d2 = (Derived)foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                class Test
                {
                    static public explicit operator Derived2(Test t) { return new Derived2();  }
                }

                 Derived2 returnDerived2_1() {
                    return new Derived1();
                }

                 Derived2 returnDerived2_2() {
                    return new Test();
                }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Derived2 derived2 = b1;
                    derived2 = b3;
                    Base2 base2 = b1;
                    derived2 = d1;
                    Derived2 d2 = new Test();
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            public class Program3
            {
                class Base { }
                class Derived : Base { }
                class Derived2 : Derived { }

                Derived2 returnD2(Base b)
                {
                    Derived d;
                    return d = b;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task CS0266TestFixAllInProject()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                Base ReturnBase(Derived d)
                {
                    Base b = new Base();
                    return b;
                }

                Derived ReturnDerived(Base b)
                {
                    return b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
                {
                    Base b;
                    yield return b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived()
                {
                    Base b;
                    return b;
                } ]]>

                <![CDATA[ async Task<Derived> M() ]]>
                {
                    Base b;
                    return b;
                }

                Derived ReturnDerived2(Base b)
                {
                    return ReturnBase();
                }

                Derived Foo()
                {
                    <![CDATA[ Func<Base, Base> func = d => d; ]]>
                    Base b;
                    return func(b);
                }
                public Program1()
                {
                    Base b;
                    Derived d = {|FixAllInProject:b|};
                    d = new Base() { };

                    Derived d2 = ReturnBase();
                    Derived d2 = ReturnBase(b);

                    Test t = new Test();
                    t.D = b;
                    t.d = b;
                    d = t.B;

                    <![CDATA[ Func<Base, Derived> foo = d => d; ]]>

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    d2 = foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                class Test
                {
                    static public explicit operator Derived2(Test t) { return new Derived2();  }
                }

                 Derived2 returnDerived2_1() {
                    return new Derived1();
                }

                 Derived2 returnDerived2_2() {
                    return new Test();
                }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Derived2 derived2 = b1;
                    derived2 = b3;
                    Base2 base2 = b1;
                    derived2 = d1;
                    Derived2 d2 = new Test();
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            public class Program3
            {
                class Base { }
                class Derived : Base { }
                class Derived2 : Derived { }

                Derived2 returnD2(Base b)
                {
                    Derived d;
                    return d = b;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                Base ReturnBase(Derived d)
                {
                    Base b = new Base();
                    return b;
                }

                Derived ReturnDerived(Base b)
                {
                    return (Derived)b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
                {
                    Base b;
                    yield return (Derived)b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived()
                {
                    Base b;
                    return (IEnumerable<Derived>)b;
                } ]]>

                <![CDATA[ async Task<Derived> M() ]]>
                {
                    Base b;
                    return (Derived)b;
                }

                Derived ReturnDerived2(Base b)
                {
                    return (Derived)ReturnBase();
                }

                Derived Foo()
                {
                    <![CDATA[ Func<Base, Base> func = d => d; ]]>
                    Base b;
                    return (Derived)func(b);
                }
                public Program1()
                {
                    Base b;
                    Derived d = (Derived)b;
                    d = new Base() { };

                    Derived d2 = (Derived)ReturnBase();
                    Derived d2 = ReturnBase(b);

                    Test t = new Test();
                    t.D = (Derived)b;
                    t.d = b;
                    d = (Derived)t.B;

                    <![CDATA[ Func<Base, Derived> foo = d => (Derived)d; ]]>

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    d2 = (Derived)foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                class Test
                {
                    static public explicit operator Derived2(Test t) { return new Derived2();  }
                }

                 Derived2 returnDerived2_1() {
                    return new Derived1();
                }

                 Derived2 returnDerived2_2() {
                    return (Derived2)new Test();
                }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Derived2 derived2 = (Derived2)b1;
                    derived2 = (Derived2)b3;
                    Base2 base2 = (Base2)b1;
                    derived2 = (Derived2)d1;
                    Derived2 d2 = (Derived2)new Test();
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            public class Program3
            {
                class Base { }
                class Derived : Base { }
                class Derived2 : Derived { }

                Derived2 returnD2(Base b)
                {
                    Derived d;
                    return d = b;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task CS0266TestFixAllInSolution()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                Base ReturnBase(Derived d)
                {
                    Base b = new Base();
                    return b;
                }

                Derived ReturnDerived(Base b)
                {
                    return b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
                {
                    Base b;
                    yield return b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived()
                {
                    Base b;
                    return b;
                } ]]>

                <![CDATA[ async Task<Derived> M() ]]>
                {
                    Base b;
                    return b;
                }

                Derived ReturnDerived2(Base b)
                {
                    return ReturnBase();
                }

                Derived Foo()
                {
                    <![CDATA[ Func<Base, Base> func = d => d; ]]>
                    Base b;
                    return func(b);
                }
                public Program1()
                {
                    Base b;
                    Derived d = {|FixAllInSolution:b|};
                    d = new Base() { };

                    Derived d2 = ReturnBase();
                    Derived d2 = ReturnBase(b);

                    Test t = new Test();
                    t.D = b;
                    t.d = b;
                    d = t.B;

                    <![CDATA[ Func<Base, Derived> foo = d => d; ]]>

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    d2 = foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                class Test
                {
                    static public explicit operator Derived2(Test t) { return new Derived2();  }
                }

                 Derived2 returnDerived2_1() {
                    return new Derived1();
                }

                 Derived2 returnDerived2_2() {
                    return new Test();
                }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Derived2 derived2 = b1;
                    derived2 = b3;
                    Base2 base2 = b1;
                    derived2 = d1;
                    Derived2 d2 = new Test();
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            public class Program3
            {
                class Base { }
                class Derived : Base { }
                class Derived2 : Derived { }

                Derived2 returnD2(Base b)
                {
                    Derived d;
                    return d = b;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                Base ReturnBase(Derived d)
                {
                    Base b = new Base();
                    return b;
                }

                Derived ReturnDerived(Base b)
                {
                    return (Derived)b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
                {
                    Base b;
                    yield return (Derived)b;
                }

                <![CDATA[ IEnumerable<Derived> ReturnDerived()
                {
                    Base b;
                    return (IEnumerable<Derived>)b;
                } ]]>

                <![CDATA[ async Task<Derived> M() ]]>
                {
                    Base b;
                    return (Derived)b;
                }

                Derived ReturnDerived2(Base b)
                {
                    return (Derived)ReturnBase();
                }

                Derived Foo()
                {
                    <![CDATA[ Func<Base, Base> func = d => d; ]]>
                    Base b;
                    return (Derived)func(b);
                }
                public Program1()
                {
                    Base b;
                    Derived d = (Derived)b;
                    d = new Base() { };

                    Derived d2 = (Derived)ReturnBase();
                    Derived d2 = ReturnBase(b);

                    Test t = new Test();
                    t.D = (Derived)b;
                    t.d = b;
                    d = (Derived)t.B;

                    <![CDATA[ Func<Base, Derived> foo = d => (Derived)d; ]]>

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    d2 = (Derived)foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                class Test
                {
                    static public explicit operator Derived2(Test t) { return new Derived2();  }
                }

                 Derived2 returnDerived2_1() {
                    return new Derived1();
                }

                 Derived2 returnDerived2_2() {
                    return (Derived2)new Test();
                }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Derived2 derived2 = (Derived2)b1;
                    derived2 = (Derived2)b3;
                    Base2 base2 = (Base2)b1;
                    derived2 = (Derived2)d1;
                    Derived2 d2 = (Derived2)new Test();
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            public class Program3
            {
                class Base { }
                class Derived : Base { }
                class Derived2 : Derived { }

                Derived2 returnD2(Base b)
                {
                    Derived d;
                    return (Derived2)(d = (Derived)b);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task CS1503TestFixAllInDocument()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Test(Derived derived)
                    {
                        d = derived;
                        B = derived;
                    }

                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }

                    public void testing(Derived d) { }
                    private void testing(Base b) { }
                }

                class Test2 : Test
                {
                    public Test2(Base b) : base(b) { }
                }

                class Test3
                {
                    public Test3(Derived b) { }
                    public Test3(int i, Base b) : this(b) { }

                    public void testing(int i, Derived d) { }
                    private void testing(int i, Base d) { }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                void PassDerived(Derived d) { }
                void PassDerived(int i, Derived d) { }

                public Program1()
                {
                    Base b;
                    Derived d = b;

                    PassDerived({|FixAllInDocument:b|});
                    PassDerived(ReturnBase());
                    PassDerived(1, b);
                    PassDerived(1, ReturnBase());

                    <![CDATA[ List<Derived> list = new List<Derived>(); ]]>
                    list.Add(b);

                    Test t = new Test();
                    t.testing(b);

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    Derived d2 = foo2(b);
                    d2 = foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                void Foo1(Derived2 b) { }
                void Foo2(Base2 b) { }

                void Foo3(Derived2 b1) { }
                void Foo3(int i) { }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Foo1(b1);
                    Foo1(d1);
                    Foo2(b1);
                    Foo3(b1);
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                void Foo1(Derived2 b) { }
                void Foo2(Base2 b) { }

                void Foo3(Derived2 b1) { }
                void Foo3(int i) { }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Foo1(b1);
                    Foo1(d1);
                    Foo2(b1);
                    Foo3(b1);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Test(Derived derived)
                    {
                        d = derived;
                        B = derived;
                    }

                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }

                    public void testing(Derived d) { }
                    private void testing(Base b) { }
                }

                class Test2 : Test
                {
                    public Test2(Base b) : base((Derived)b) { }
                }

                class Test3
                {
                    public Test3(Derived b) { }
                    public Test3(int i, Base b) : this((Derived)b) { }

                    public void testing(int i, Derived d) { }
                    private void testing(int i, Base d) { }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                void PassDerived(Derived d) { }
                void PassDerived(int i, Derived d) { }

                public Program1()
                {
                    Base b;
                    Derived d = b;

                    PassDerived((Derived)b);
                    PassDerived((Derived)ReturnBase());
                    PassDerived(1, (Derived)b);
                    PassDerived(1, (Derived)ReturnBase());

                    <![CDATA[ List<Derived> list = new List<Derived>(); ]]>
                    list.Add((Derived)b);

                    Test t = new Test();
                    t.testing((Derived)b);

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    Derived d2 = foo2((Derived)b);
                    d2 = foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                void Foo1(Derived2 b) { }
                void Foo2(Base2 b) { }

                void Foo3(Derived2 b1) { }
                void Foo3(int i) { }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Foo1(b1);
                    Foo1(d1);
                    Foo2(b1);
                    Foo3(b1);
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                void Foo1(Derived2 b) { }
                void Foo2(Base2 b) { }

                void Foo3(Derived2 b1) { }
                void Foo3(int i) { }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Foo1(b1);
                    Foo1(d1);
                    Foo2(b1);
                    Foo3(b1);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task CS1503TestFixAllInProject()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Test(Derived derived)
                    {
                        d = derived;
                        B = derived;
                    }

                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }

                    public void testing(Derived d) { }
                    private void testing(Base b) { }
                }

                class Test2 : Test
                {
                    public Test2(Base b) : base(b) { }
                }

                class Test3
                {
                    public Test3(Derived b) { }
                    public Test3(int i, Base b) : this(b) { }

                    public void testing(int i, Derived d) { }
                    private void testing(int i, Base d) { }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                void PassDerived(Derived d) { }
                void PassDerived(int i, Derived d) { }

                public Program1()
                {
                    Base b;
                    Derived d = b;

                    PassDerived({|FixAllInProject:b|});
                    PassDerived(ReturnBase());
                    PassDerived(1, b);
                    PassDerived(1, ReturnBase());

                    <![CDATA[ List<Derived> list = new List<Derived>(); ]]>
                    list.Add(b);

                    Test t = new Test();
                    t.testing(b);

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    Derived d2 = foo2(b);
                    d2 = foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                class Test
                {
                    static public explicit operator Derived1(Test t) { return new Derived1(); }
                    static public explicit operator Derived2(Test t) { return new Derived2(); }
                }

                void Foo1(Derived2 b) { }
                void Foo2(Base2 b) { }

                void Foo3(Derived2 b1) { }
                void Foo3(int i) { }

                void Foo4(int i, string j, Derived1 d) { }
                void Foo4(string j, int i, Derived1 d) { }

                void Foo5(string j, int i, Derived2 d, int x = 1) { }

                void Foo5(string j, int i, Derived1 d, params Derived2[] d2list) { }

                void Foo6(Derived1 d, params Derived2[] d2list) { }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Foo1(b1);
                    Foo1(d1);

                    Foo2(b1);

                    Foo3(b1);

                    Foo4(1, "", b1);
                    Foo4(i: 1, j: "", b1); // one operation, fix

                    Foo5("", 1, b1); // multiple operations, no fix-all

                    Foo5(d: b1, i: 1, j: "", x: 1); // all arguments out of order - match
                    Foo5(1, "", x: 1, d: b1); // part of arguments out of order - mismatch

                    Foo5(1, "", d: b1, b2, b3, d1); // part of arguments out of order - mismatch
                    Foo5("", 1, d: b1, b2, b3, d1); // part of arguments out of order - match

                    var d2list = new Derived2[] { };
                    Foo5(d2list: d2list, j: "", i: 1, d: b2);
                    var d1list = new Derived1[] { };
                    Foo5(d2list: d1list, j: "", i: 1, d: b2); 

                    Foo6(b1);

                    Foo6(new Test()); // params is optional,  object creation can be cast with explicit cast operator
                    Foo6(new Test(), new Derived1()); // object creation cannot be cast without explicit cast operator
                    Foo6(new Derived1(), new Test());
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                void Foo1(Derived2 b) { }
                void Foo2(Base2 b) { }

                void Foo3(Derived2 b1) { }
                void Foo3(int i) { }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Foo1(b1);
                    Foo1(d1);
                    Foo2(b1);
                    Foo3(b1);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Test(Derived derived)
                    {
                        d = derived;
                        B = derived;
                    }

                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }

                    public void testing(Derived d) { }
                    private void testing(Base b) { }
                }

                class Test2 : Test
                {
                    public Test2(Base b) : base((Derived)b) { }
                }

                class Test3
                {
                    public Test3(Derived b) { }
                    public Test3(int i, Base b) : this((Derived)b) { }

                    public void testing(int i, Derived d) { }
                    private void testing(int i, Base d) { }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                void PassDerived(Derived d) { }
                void PassDerived(int i, Derived d) { }

                public Program1()
                {
                    Base b;
                    Derived d = b;

                    PassDerived((Derived)b);
                    PassDerived((Derived)ReturnBase());
                    PassDerived(1, (Derived)b);
                    PassDerived(1, (Derived)ReturnBase());

                    <![CDATA[ List<Derived> list = new List<Derived>(); ]]>
                    list.Add((Derived)b);

                    Test t = new Test();
                    t.testing((Derived)b);

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    Derived d2 = foo2((Derived)b);
                    d2 = foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                class Test
                {
                    static public explicit operator Derived1(Test t) { return new Derived1(); }
                    static public explicit operator Derived2(Test t) { return new Derived2(); }
                }

                void Foo1(Derived2 b) { }
                void Foo2(Base2 b) { }

                void Foo3(Derived2 b1) { }
                void Foo3(int i) { }

                void Foo4(int i, string j, Derived1 d) { }
                void Foo4(string j, int i, Derived1 d) { }

                void Foo5(string j, int i, Derived2 d, int x = 1) { }

                void Foo5(string j, int i, Derived1 d, params Derived2[] d2list) { }

                void Foo6(Derived1 d, params Derived2[] d2list) { }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Foo1((Derived2)b1);
                    Foo1((Derived2)d1);

                    Foo2((Base2)b1);

                    Foo3((Derived2)b1);

                    Foo4(1, "", (Derived1)b1);
                    Foo4(i: 1, j: "", (Derived1)b1); // one operation, fix

                    Foo5("", 1, b1); // multiple operations, no fix-all

                    Foo5(d: (Derived2)b1, i: 1, j: "", x: 1); // all arguments out of order - match
                    Foo5(1, "", x: 1, d: b1); // part of arguments out of order - mismatch

                    Foo5(1, "", d: b1, b2, b3, d1); // part of arguments out of order - mismatch
                    Foo5("", 1, d: (Derived1)b1, (Derived2)b2, (Derived2)b3, (Derived2)d1); // part of arguments out of order - match

                    var d2list = new Derived2[] { };
                    Foo5(d2list: d2list, j: "", i: 1, d: (Derived1)b2);
                    var d1list = new Derived1[] { };
                    Foo5(d2list: (Derived2[])d1list, j: "", i: 1, d: (Derived1)b2); 

                    Foo6((Derived1)b1);

                    Foo6((Derived1)new Test()); // params is optional,  object creation can be cast with explicit cast operator
                    Foo6((Derived1)new Test(), new Derived1()); // object creation cannot be cast without explicit cast operator
                    Foo6(new Derived1(), (Derived2)new Test());
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                void Foo1(Derived2 b) { }
                void Foo2(Base2 b) { }

                void Foo3(Derived2 b1) { }
                void Foo3(int i) { }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Foo1(b1);
                    Foo1(d1);
                    Foo2(b1);
                    Foo3(b1);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task CS1503TestFixAllInSolution()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Test(Derived derived)
                    {
                        d = derived;
                        B = derived;
                    }

                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }

                    public void testing(Derived d) { }
                    private void testing(Base b) { }
                }

                class Test2 : Test
                {
                    public Test2(Base b) : base(b) { }
                }

                class Test3
                {
                    public Test3(Derived b) { }
                    public Test3(int i, Base b) : this(b) { }

                    public void testing(int i, Derived d) { }
                    private void testing(int i, Base d) { }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                void PassDerived(Derived d) { }
                void PassDerived(int i, Derived d) { }

                public Program1()
                {
                    Base b;
                    Derived d = b;

                    PassDerived({|FixAllInSolution:b|});
                    PassDerived(ReturnBase());
                    PassDerived(1, b);
                    PassDerived(1, ReturnBase());

                    <![CDATA[ List<Derived> list = new List<Derived>(); ]]>
                    list.Add(b);

                    Test t = new Test();
                    t.testing(b);

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    Derived d2 = foo2(b);
                    d2 = foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                void Foo1(Derived2 b) { }
                void Foo2(Base2 b) { }

                void Foo3(Derived2 b1) { }
                void Foo3(int i) { }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Foo1(b1);
                    Foo1(d1);
                    Foo2(b1);
                    Foo3(b1);
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                interface Base { }
                class Derived1 : Base { }
                class Derived2 : Derived1 { }

                class Test
                {
                    public Test(string s, Base b, int i, params object[] list) : this(d: b, s: s, i: i) { } // 2 operations, no fix in fix-all
                    Test(string s, Derived1 d, int i) { }
                    Test(string s, Derived2 d, int i) { }
                }

                void Foo(Derived1 d, int a, int b, params int[] list) { }
                void Foo(Derived2 d, params int[] list) { }


                private void M2(Base b, Derived1 d1, Derived2 d2)
                {
                    Foo(b, 1, 2); // 2 operations, no fix in fix-all
                    var intlist = new int[] { };
                    Foo(b, 1, 2, list: intlist); // 2 operations
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Program1
            {
                class Base { }
                class Derived : Base { }

                class Test
                {
                    private Derived d;
                    private Base b;
                    public Test(Derived derived)
                    {
                        d = derived;
                        B = derived;
                    }

                    public Derived D { get => d; set => d = value; }
                    public Base B { get => b; set => b = value; }

                    public void testing(Derived d) { }
                    private void testing(Base b) { }
                }

                class Test2 : Test
                {
                    public Test2(Base b) : base((Derived)b) { }
                }

                class Test3
                {
                    public Test3(Derived b) { }
                    public Test3(int i, Base b) : this((Derived)b) { }

                    public void testing(int i, Derived d) { }
                    private void testing(int i, Base d) { }
                }

                Base ReturnBase()
                {
                    Base b = new Base();
                    return b;
                }

                void PassDerived(Derived d) { }
                void PassDerived(int i, Derived d) { }

                public Program1()
                {
                    Base b;
                    Derived d = b;

                    PassDerived((Derived)b);
                    PassDerived((Derived)ReturnBase());
                    PassDerived(1, (Derived)b);
                    PassDerived(1, (Derived)ReturnBase());

                    <![CDATA[ List<Derived> list = new List<Derived>(); ]]>
                    list.Add((Derived)b);

                    Test t = new Test();
                    t.testing((Derived)b);

                    <![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
                    Derived d2 = foo2((Derived)b);
                    d2 = foo2(d);
                }
            }
                    </Document>
                    <Document>
            public class Program2
            {
                interface Base1 { }
                interface Base2 : Base1 { }
                interface Base3 { }
                class Derived1 : Base2, Base3 { }
                class Derived2 : Derived1 { }

                void Foo1(Derived2 b) { }
                void Foo2(Base2 b) { }

                void Foo3(Derived2 b1) { }
                void Foo3(int i) { }

                private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
                {
                    Foo1((Derived2)b1);
                    Foo1((Derived2)d1);
                    Foo2((Base2)b1);
                    Foo3((Derived2)b1);
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                interface Base { }
                class Derived1 : Base { }
                class Derived2 : Derived1 { }

                class Test
                {
                    public Test(string s, Base b, int i, params object[] list) : this(d: b, s: s, i: i) { } // 2 operations, no fix in fix-all
                    Test(string s, Derived1 d, int i) { }
                    Test(string s, Derived2 d, int i) { }
                }

                void Foo(Derived1 d, int a, int b, params int[] list) { }
                void Foo(Derived2 d, params int[] list) { }


                private void M2(Base b, Derived1 d1, Derived2 d2)
                {
                    Foo(b, 1, 2); // 2 operations, no fix in fix-all
                    var intlist = new int[] { };
                    Foo(b, 1, 2, list: intlist); // 2 operations
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);
    #endregion
}
