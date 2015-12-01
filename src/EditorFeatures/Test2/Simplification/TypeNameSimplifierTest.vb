' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class TypeNameSimplifierTest
        Inherits AbstractSimplificationTests

#Region "Normal CSharp Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyTypeName() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using System;
                    namespace Root 
                    {
                        class A 
                        {
                            {|SimplifyParent:System.Exception|} c;
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using System;
                    namespace Root 
                    {
                        class A 
                        {
                            Exception c;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyReceiver1() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                class C
                {
                    void M(C other)
                    {
                        {|SimplifyParent:other.M|}(null);
                    }
                }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                class C
                {
                    void M(C other)
                    {
                        other.M(null);
                    }
                }
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyReceiver2() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                class C
                {
                    void M(C other)
                    {
                        {|SimplifyParent:this.M|}(null);
                    }
                }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                class C
                {
                    void M(C other)
                    {
                        M(null);
                    }
                }
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyNestedType() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;

class Preserve
{
	public class X
	{
		public static int Y;
	}
}

class Z<T> : Preserve
{
}

static class M
{
	public static void Main()
	{
		int k = {|SimplifyParent:Z<float>.X.Y|};
	}
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;

class Preserve
{
	public class X
	{
		public static int Y;
	}
}

class Z<T> : Preserve
{
}

static class M
{
	public static void Main()
	{
		int k = Preserve.X.Y;
	}
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyNestedType2() As Task
            ' Simplified type is in a different namespace.

            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
using System;

namespace N1
{
    class Preserve
    {
        public class X
        {
            public static int Y;
        }
    }
}

namespace P
{
    class NonGeneric : N1.Preserve
    {
    }
}

static class M
{
    public static void Main()
    {
        int k = P.NonGeneric.{|SimplifyParent:X|}.Y;
    }
}               </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
using System;

namespace N1
{
    class Preserve
    {
        public class X
        {
            public static int Y;
        }
    }
}

namespace P
{
    class NonGeneric : N1.Preserve
    {
    }
}

static class M
{
    public static void Main()
    {
        int k = N1.Preserve.X.Y;
    }
}</text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyNestedType3() As Task
            ' Simplified type is in a different namespace, whose names have been imported with a usings statement.

            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
using System;

namespace N1
{
    class Preserve
    {
        public class X
        {
            public static int Y;
        }
    }
}

namespace P
{
    class NonGeneric : N1.Preserve
    {
    }
}

namespace R
{
    using N1;

    static class M
    {
        public static void Main()
        {
            int k = P.NonGeneric.{|SimplifyParent:X|}.Y;
        }
    }
}               </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
using System;

namespace N1
{
    class Preserve
    {
        public class X
        {
            public static int Y;
        }
    }
}

namespace P
{
    class NonGeneric : N1.Preserve
    {
    }
}

namespace R
{
    using N1;

    static class M
    {
        public static void Main()
        {
            int k = Preserve.X.Y;
        }
    }
}</text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyNestedType4() As Task
            ' Highly nested type simplified to another highly nested type.

            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;

namespace N1
{
    namespace N2
    {
        public class Outer
        {
            public class Preserve
            {
                public class X
                {
                    public static int Y;
                }
            }
        }
    }

}

namespace P1
{
    namespace P2
    {
        public class NonGeneric : N1.N2.Outer.Preserve
        {
        }
    }
}

namespace Q1
{
    using P1.P2;

    namespace Q2
    {
        class Generic<T> : NonGeneric
        {
        }
    }
}

namespace R
{
    using Q1.Q2;

    static class M
    {
        public static void Main()
        {
            int k = Generic<int>.{|SimplifyParent:X|}.Y;
        }
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;

namespace N1
{
    namespace N2
    {
        public class Outer
        {
            public class Preserve
            {
                public class X
                {
                    public static int Y;
                }
            }
        }
    }

}

namespace P1
{
    namespace P2
    {
        public class NonGeneric : N1.N2.Outer.Preserve
        {
        }
    }
}

namespace Q1
{
    using P1.P2;

    namespace Q2
    {
        class Generic<T> : NonGeneric
        {
        }
    }
}

namespace R
{
    using Q1.Q2;

    static class M
    {
        public static void Main()
        {
            int k = N1.N2.Outer.Preserve.X.Y;
        }
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyNestedType5() As Task
            ' Name requiring multiple iterations of nested type simplification.

            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;

namespace N1
{
    namespace N2
    {
        public class Outer
        {
            public class Preserve
            {
                public class X
                {
                    public static int Y;
                }
            }
        }
    }
    
}

namespace P1
{
    using N1.N2;
    namespace P2
    {
        public class NonGeneric : Outer
        {
            public class NonGenericInner : Outer.Preserve
            {
            }
        }
    }
}

namespace Q1
{
    using P1.P2;

    namespace Q2
    {
        class Generic<T> : NonGeneric
        {
        }
    }
}

namespace R
{
    using N1.N2;
    using Q1.Q2;

    static class M
    {
        public static void Main()
        {
            int k = Generic<int>.NonGenericInner.{|SimplifyParent:X|}.Y;
        }
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;

namespace N1
{
    namespace N2
    {
        public class Outer
        {
            public class Preserve
            {
                public class X
                {
                    public static int Y;
                }
            }
        }
    }
    
}

namespace P1
{
    using N1.N2;
    namespace P2
    {
        public class NonGeneric : Outer
        {
            public class NonGenericInner : Outer.Preserve
            {
            }
        }
    }
}

namespace Q1
{
    using P1.P2;

    namespace Q2
    {
        class Generic<T> : NonGeneric
        {
        }
    }
}

namespace R
{
    using N1.N2;
    using Q1.Q2;

    static class M
    {
        public static void Main()
        {
            int k = Outer.Preserve.X.Y;
        }
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyStaticMemberAccess() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;

class Preserve
{
	public static int Y;
}

class Z<T> : Preserve
{
}

static class M
{
	public static void Main()
	{
		int k = {|SimplifyParent:Z<float>.Y|};
	}
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;

class Preserve
{
	public static int Y;
}

class Z<T> : Preserve
{
}

static class M
{
	public static void Main()
	{
		int k = Preserve.Y;
	}
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyQualifiedName() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;

class A
{
    public static class B { }
}

class C : A
{
}

namespace N1
{
    static class M
    {
        public static {|SimplifyParent:C.B|} F()
        {
            return null;
        }
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;

class A
{
    public static class B { }
}

class C : A
{
}

namespace N1
{
    static class M
    {
        public static A.B F()
        {
            return null;
        }
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyAllNodes_SimplifyAliasStaticMemberAccess() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;

class Preserve
{
    public static int Y;
}

class NonGeneric : Preserve
{
}

namespace N1
{
    using X = NonGeneric;

    static class M
    {
        public static void Main()
        {
            int k = {|SimplifyParent:X|}.Y;
        }
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;

class Preserve
{
    public static int Y;
}

class NonGeneric : Preserve
{
}

namespace N1
{
    using X = NonGeneric;

    static class M
    {
        public static void Main()
        {
            int k = Preserve.Y;
        }
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyNot_Delegate1() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                using System;
                class A
                {
                    static void Del() { }
                    class B
                    {
                        delegate void Del();
                        void Boo()
                        {
                            Del d = new Del(A.{|SimplifyParent:Del|}); 
                            A.{|SimplifyParent:Del|}(); 
                        }
                    }
                }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                using System;
                class A
                {
                    static void Del() { }
                    class B
                    {
                        delegate void Del();
                        void Boo()
                        {
                            Del d = new Del(A.Del); 
                            Del(); 
                        }
                    }
                }                
                </text>
            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyNot_Delegate2() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                using System;
                class A
                {
                    static void Bar() { }
                    class B
                    {
                        delegate void Del();
                        void Bar() { }
                        void Boo()
                        {
                            Del d = new Del(A.{|SimplifyParent:Bar|}); 
                            A.{|SimplifyParent:Bar|}();
                        }
                    }
                }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                using System;
                class A
                {
                    static void Bar() { }
                    class B
                    {
                        delegate void Del();
                        void Bar() { }
                        void Boo()
                        {
                            Del d = new Del(A.Bar); 
                            A.Bar();
                        }
                    }
                }
                </text>
            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyNot_Delegate3() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using System;
                    class A
                    {
                        delegate void Del(Del a);
                        static void Boo(Del a) { }
                        class B
                        {
                            Del Boo = new Del(A.Boo);
                            void Foo()
                            {
                                Boo(A.{|SimplifyParent:Boo|}); 
                                A.Boo(Boo);
                            }
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using System;
                    class A
                    {
                        delegate void Del(Del a);
                        static void Boo(Del a) { }
                        class B
                        {
                            Del Boo = new Del(A.Boo);
                            void Foo()
                            {
                                Boo(A.Boo); 
                                A.Boo(Boo);
                            }
                        }
                    }
                </text>
            Await TestAsync(input, expected)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(552722)>
        Public Async Function TestSimplifyNot_Action() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document><![CDATA[
                using System;
                class A
                {
                    static Action<int> Bar = (int x) => { };    
                    class B
                    {
                        Action<int> Bar = (int x) => { };
                        void Foo()
                        {
                            A.{|SimplifyParent:Bar|}(3);            
                        }
                    }
                }]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
                using System;
                class A
                {
                    static Action<int> Bar = (int x) => { };    
                    class B
                    {
                        Action<int> Bar = (int x) => { };
                        void Foo()
                        {
                            A.Bar(3);            
                        }
                    }
                }]]>
              </text>
            Await TestAsync(input, expected)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(552722)>
        Public Async Function TestSimplifyNot_Func() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document><![CDATA[
                using System;
                class A
                {
                    static Func<int,int> Bar = (int x) => { return x; };    
                    class B
                    {
                        Func<int,int> Bar = (int x) => { return x; };
                        void Foo()
                        {
                            A.{|SimplifyParent:Bar|}(3);            
                        }
                    }
                }]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
                using System;
                class A
                {
                    static Func<int,int> Bar = (int x) => { return x; };    
                    class B
                    {
                        Func<int,int> Bar = (int x) => { return x; };
                        void Foo()
                        {
                            A.Bar(3);            
                        }
                    }
                }]]>
              </text>
            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyNot_Inheritance() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
using System;
class A
{
    public virtual void f() { }
}
class B : A
{
    public override void f()
    {
        base.{|SimplifyParent:f|}();
    }
}
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
using System;
class A
{
    public virtual void f() { }
}
class B : A
{
    public override void f()
    {
        base.f();
    }
}        
              </text>
            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyDoNothingWithFailedOverloadResolution() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
using System;
using System.Console;
class A
{
    public void f()
    {
        Console.{|SimplifyParent:ReadLine|}("Boo!");
    }
}
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
using System;
using System.Console;
class A
{
    public void f()
    {
        Console.ReadLine("Boo!");
    }
}
              </text>
            Await TestAsync(input, expected)
        End Function

        <WorkItem(609496)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharpDoNotSimplifyNameInNamespaceDeclaration() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
using System;
namespace System.{|SimplifyParent:Foo|}
{}
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
using System;
namespace System.Foo
{}
              </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608197)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCS_EscapeAliasReplacementIfNeeded() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            using @if = System.Runtime.InteropServices.InAttribute;
            class C
            {
                void foo()
                {
                    var x = new System.Runtime.InteropServices.{|SimplifyParent:InAttribute|}() // Simplify Type Name
                }
            }
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Code>
            using @if = System.Runtime.InteropServices.InAttribute;
            class C
            {
                void foo()
                {
                    var x = new @if() // Simplify Type Name
                }
            }
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(529989)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCS_AliasReplacementKeepsUnicodeEscaping() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using B\u0061r = System.Console;

        class Program
        {
            static void Main(string[] args)
            {
                System.Console.{|SimplifyParent:WriteLine|}("");
            }
        }
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Code>
        using B\u0061r = System.Console;

        class Program
        {
            static void Main(string[] args)
            {
                B\u0061r.WriteLine("");
            }
        }
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Simplify_Cast_Type_Name() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        var a = 1;
        Console.WriteLine((System.Collections.Generic.{|SimplifyParent:IEnumerable&lt;int&gt;|})a);
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        var a = 1;
        Console.WriteLine((IEnumerable&lt;int&gt;)a);
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestAliasedNameWithMethod() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Collections.Generic;

using foo = System.Console;
class Program
{
    static void Main(string[] args)
    {
             {|SimplifyExtension:System.Console.WriteLine|}("test");
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
using System.Collections.Generic;

using foo = System.Console;
class Program
{
    static void Main(string[] args)
    {
             foo.WriteLine("test");
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(554010)>
        Public Async Function TestSimplificationForDelegateCreation() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
class Test
{
    static void Main(string[] args)
    {
        Action b = (Action)Console.WriteLine + {|SimplifyParent:System.Console.WriteLine|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
class Test
{
    static void Main(string[] args)
    {
        Action b = (Action)Console.WriteLine + Console.WriteLine;
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(554010)>
        Public Async Function TestSimplificationForDelegateCreation2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
class Test
{
    Action b = (Action)Console.WriteLine + {|SimplifyParent:System.Console.WriteLine|};
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
class Test
{
    Action b = (Action)Console.WriteLine + Console.WriteLine;
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(576970)>
        Public Async Function TestCSRemoveThisWouldBeConsideredACast_1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class C
{
    Action A { get; set; }
 
    void Foo()
    {
        (this.{|SimplifyParent:A|})(); // Simplify type name
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
 
class C
{
    Action A { get; set; }
 
    void Foo()
    {
        (this.A)(); // Simplify type name
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(576970)>
        Public Async Function TestCSRemoveThisWouldBeConsideredACast_2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class C
{
    Action A { get; set; }
 
    void Foo()
    {
        ((this.{|SimplifyParent:A|}))(); // Simplify type name
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
 
class C
{
    Action A { get; set; }
 
    void Foo()
    {
        ((A))(); // Simplify type name
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(576970)>
        Public Async Function TestCSRemoveThisWouldBeConsideredACast_3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

public class C
{
    public class D
    {
        public Action A { get; set; }
    }

    public D d = new D();

    void Foo()
    {
        (this.{|SimplifyParent:d|}.A)(); // Simplify type name
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;

public class C
{
    public class D
    {
        public Action A { get; set; }
    }

    public D d = new D();

    void Foo()
    {
        (this.d.A)(); // Simplify type name
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(50, "https://github.com/dotnet/roslyn/issues/50")>
        Public Async Function TestCSRemoveThisPreservesTrivia() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C1
{
    int _field;

    void M()
    {
        this /*comment 1*/ . /* comment 2 */ {|SimplifyParent:_field|} /* comment 3 */ = 0;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C1
{
    int _field;

    void M()
    {
         /*comment 1*/  /* comment 2 */ _field /* comment 3 */ = 0;
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(649385)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharpSimplifyToVarCorrect() As Task

            Dim simplificationOption = New Dictionary(Of OptionKey, Object) From {{SimplificationOptions.PreferImplicitTypeInLocalDeclaration, True}}

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System.IO;
using I = N.C;
namespace N { class C {} }
class Program
{
    class D { }
    static void Main(string[] args)
    {
        {|Simplify:int|} i = 0;

        for ({|Simplify:int|} j = 0; ;) { }

        {|Simplify:D|} d = new D();

        foreach ({|Simplify:int|} item in new List&lt;int&gt;()) { }

        using ({|Simplify:StreamReader|} file = new StreamReader("C:\\myfile.txt")) {}

        {|Simplify:int|} x = Foo();
    }
    static int Foo() { return 1; }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System.IO;
using I = N.C;
namespace N { class C {} }
class Program
{
    class D { }
    static void Main(string[] args)
    {
        var i = 0;

        for (var j = 0; ;) { }

        var d = new D();

        foreach (var item in new List&lt;int&gt;()) { }

        using (var file = new StreamReader("C:\\myfile.txt")) {}

        var x = Foo();
    }
    static int Foo() { return 1; }
}
 
</code>

            Await TestAsync(input, expected, simplificationOption)
        End Function

        <WorkItem(734445)>
        <WorkItem(649385)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharpSimplifyToVarCorrect_QualifiedTypeNames() As Task

            Dim simplificationOption = New Dictionary(Of OptionKey, Object) From {{SimplificationOptions.PreferImplicitTypeInLocalDeclaration, True}}

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System.IO;
namespace N { class C {} }
class Program
{
    class D { }
    static void Main(string[] args)
    {
        {|SimplifyParent:N.C|} z = new N.C();

        {|SimplifyParent:System.Int32|} i = 1;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System.IO;
namespace N { class C {} }
class Program
{
    class D { }
    static void Main(string[] args)
    {
        var z = new N.C();

        var i = 1;
    }
}
 
</code>

            Await TestAsync(input, expected, simplificationOption)
        End Function

        <WorkItem(649385)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharpSimplifyToVarDontSimplify() As Task

            Dim simplificationOption = New Dictionary(Of OptionKey, Object) From {{SimplificationOptions.PreferImplicitTypeInLocalDeclaration, True}}

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    {|Simplify:int|} x;

    static void Main(string[] args)
    {
        {|Simplify:int|} i = (i = 20);

        {|Simplify:object|} o = null;

        {|Simplify:Action&lt;string[]&gt;|} m = Main;

        {|Simplify:int|} ij = 0, k = 0;

        {|Simplify:int|} j;

        {|Simplify:dynamic|} d = 1;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;

class Program
{
    int x;

    static void Main(string[] args)
    {
        int i = (i = 20);

        object o = null;

        Action&lt;string[]&gt; m = Main;

        int ij = 0, k = 0;

        int j;

        dynamic d = 1;
    }
}
</code>

            Await TestAsync(input, expected, simplificationOption)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyTypeNameWhenParentHasSimplifyAnnotation() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using System;
                    namespace Root 
                    {
                        {|SimplifyParent:class A 
                        {
                            System.Exception c;
                        }|}
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using System;
                    namespace Root 
                    {
                        class A 
                        {
                            Exception c;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyTypeNameWithExplicitSimplifySpan_MutuallyExclusive() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    {|SpanToSimplify:using System;|}
                    namespace Root 
                    {
                        {|SimplifyParent:class A 
                        {
                            System.Exception c;
                        }|}
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using System;
                    namespace Root 
                    {
                        class A 
                        {
                            System.Exception c;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyTypeNameWithExplicitSimplifySpan_Inclusive() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    {|SpanToSimplify:using System;
                    namespace Root 
                    {
                        {|SimplifyParent:class A 
                        {
                            System.Exception c;
                        }|}
                    }
                    |}
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using System;
                    namespace Root 
                    {
                        class A 
                        {
                            Exception c;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyTypeNameWithExplicitSimplifySpan_OverlappingPositive() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using System;
                    namespace Root 
                    {
                        {|SimplifyParent:class A 
                        {
                            {|SpanToSimplify:System.Exception|} c;
                        }|}
                    }                    
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using System;
                    namespace Root 
                    {
                        class A 
                        {
                            Exception c;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyTypeNameWithExplicitSimplifySpan_OverlappingNegative() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using System;
                    namespace Root 
                    {
                        {|SimplifyParent:class A 
                        {
                            System.Exception {|SpanToSimplify:c;|}
                        }|}
                    }                    
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using System;
                    namespace Root 
                    {
                        class A 
                        {
                            System.Exception c;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification), WorkItem(864735)>
        Public Async Function TestBugFix864735_CSharp_SimplifyNameInIncompleteIsExpression() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                class C
                {
                    static int F;

                    void M(C other)
                    {
                        Console.WriteLine({|SimplifyParent:C.F|} is
                    }
                }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                class C
                {
                    static int F;

                    void M(C other)
                    {
                        Console.WriteLine(F is
                    }
                }
                </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(813566)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyQualifiedCref() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document><![CDATA[
                    using System;

                    /// <summary>
                    /// <see cref="{|Simplify:System.Object|}"/>
                    /// </summary>
                    class Program
                    {
                    }]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text><![CDATA[
                    using System;

                    /// <summary>
                    /// <see cref="object"/>
                    /// </summary>
                    class Program
                    {
                    }]]>
              </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(838109)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestDontSimplifyToGenericNameCSharp() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document><![CDATA[
public class C<T>
{
    public class D
    {
        public static void F()
        {
        }
    }
}
 
public class C : C<int>
{
}
 
class E
{
    public static void Main()
    {
        {|SimplifyParent:C.D|}.F();
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text><![CDATA[
public class C<T>
{
    public class D
    {
        public static void F()
        {
        }
    }
}
 
public class C : C<int>
{
}
 
class E
{
    public static void Main()
    {
        C.D.F();
    }
}]]>
              </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(838109)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestDoSimplifyToGenericName() As Task
            Dim simplificationOption = New Dictionary(Of OptionKey, Object) From {{SimplificationOptions.AllowSimplificationToGenericType, True}}

            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document><![CDATA[
public class C<T>
{
    public class D
    {
        public static void F()
        {
        }
    }
}
 
public class C : C<int>
{
}
 
class E
{
    public static void Main()
    {
        {|SimplifyParent:C.D|}.F();
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text><![CDATA[
public class C<T>
{
    public class D
    {
        public static void F()
        {
        }
    }
}
 
public class C : C<int>
{
}
 
class E
{
    public static void Main()
    {
        C<int>.D.F();
    }
}]]>
              </text>

            Await TestAsync(input, expected, simplificationOption)
        End Function

        <WpfFact, WorkItem(838109), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestDontSimplifyAllNodes_SimplifyNestedType() As Task
            Dim simplificationOption = New Dictionary(Of OptionKey, Object) From {{SimplificationOptions.AllowSimplificationToBaseType, False}}

            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;

class Preserve
{
	public class X
	{
		public static int Y;
	}
}

class Z<T> : Preserve
{
}

static class M
{
	public static void Main()
	{
		int k = {|SimplifyParent:Z<float>.X.Y|};
	}
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;

class Preserve
{
	public class X
	{
		public static int Y;
	}
}

class Z<T> : Preserve
{
}

static class M
{
	public static void Main()
	{
		int k = Z<float>.X.Y;
	}
}]]></text>

            Await TestAsync(input, expected, simplificationOption)
        End Function

        <WpfFact, WorkItem(838109), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestDontSimplifyAwayThisExpression() As Task
            Dim simplificationOption = New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, LanguageNames.CSharp), True}}

            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;

class Program
{
    private int s;
    static void Main(string[] args)
    {
        
    }

    public int give()
    {
        return {|SimplifyParent:this.s|};
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;

class Program
{
    private int s;
    static void Main(string[] args)
    {
        
    }

    public int give()
    {
        return this.s;
    }
}]]></text>

            Await TestAsync(input, expected, simplificationOption)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyTypeNameInCodeWithSyntaxErrors() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
class C
{
    private int x;

    void F(int y) {}

    void M()
    {
        C
        // some comment
        F({|SimplifyParent:this.x|});
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
class C
{
    private int x;

    void F(int y) {}

    void M()
    {
        C
        // some comment
        F(x);
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function
        <WpfFact, WorkItem(653601), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCrefSimplification_1() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
namespace A
{
    /// <summary>
    /// <see cref="{|Simplify:A.Program|}"/>
    /// </summary>
    class Program
    {
        void B()
        {

        }
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
namespace A
{
    /// <summary>
    /// <see cref="Program"/>
    /// </summary>
    class Program
    {
        void B()
        {

        }
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(653601), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCrefSimplification_2() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
namespace A
{
    /// <summary>
    /// <see cref="{|Simplify:A.Program.B|}"/>
    /// </summary>
    class Program
    {
        void B()
        {

        }
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
namespace A
{
    /// <summary>
    /// <see cref="B"/>
    /// </summary>
    class Program
    {
        void B()
        {

        }
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(966633), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DontSimplifyNullableQualifiedName() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;
class C
{
       {|SimplifyParent:Nullable<long>|}.Value x;
       void M({|SimplifyParent:Nullable<int>|} a, ref {|SimplifyParent:System.Nullable<int>|} b, ref {|SimplifyParent:Nullable<long>|}.Something c) { }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;
class C
{
       Nullable<long>.Value x;
       void M(int? a, ref int? b, ref Nullable<long>.Something c) { }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(965240), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DontSimplifyOpenGenericNullable() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;
class C
{
    void M()
    {
        var x = typeof({|SimplifyParent:System.Nullable<>|});
        var y = (typeof({|SimplifyParent:System.Nullable<long>|}));
        var z = (typeof({|SimplifyParent:System.Nullable|}));
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;
class C
{
    void M()
    {
        var x = typeof(Nullable<>);
        var y = (typeof(long?));
        var z = (typeof(Nullable));
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(1067214), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyTypeNameInExpressionBody_Property() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
namespace N
{
    class Program
    {
        private object x;
        public Program X => ({|SimplifyParent:N.Program|})x;
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
namespace N
{
    class Program
    {
        private object x;
        public Program X => (Program)x;
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(1067214), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyTypeNameInExpressionBody_Method() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
namespace N
{
    class Program
    {
        private object x;
        public Program X() => ({|SimplifyParent:N.Program|})x;
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
namespace N
{
    class Program
    {
        private object x;
        public Program X() => (Program)x;
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(2232, "https://github.com/dotnet/roslyn/issues/2232"), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DontSimplifyToPredefinedTypeNameInQualifiedName() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;
namespace N
{
    class Program
    {
        void Main()
        {
            var x = new {|SimplifyParent:System.Int32|}.Blah;
        }
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;
namespace N
{
    class Program
    {
        void Main()
        {
            var x = new Int32.Blah;
        }
    }
}]]></text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4859, "https://github.com/dotnet/roslyn/issues/4859")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DontSimplifyNullableInNameOfExpression() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    <![CDATA[
using System;
class C
{
    void M()
    {
        var s = nameof({|Simplify:Nullable<int>|});
    }
}
]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
using System;
class C
{
    void M()
    {
        var s = nameof(Nullable<int>);
    }
}
]]></text>

            Await TestAsync(input, expected)
        End Function
#End Region

#Region "Normal Visual Basic Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyAllNodes_SimplifyTypeName() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports System
                      Namespace Root
                        Class A
                            Private e As {|SimplifyParent:System.Exception|}
                        End Class
                    End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                      Imports System
                      Namespace Root
                        Class A
                            Private e As Exception
                        End Class
                    End Namespace
                </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(547117)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestGetChanges_SimplifyTypeName_Array_1() As Task
            Dim input =
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                    Module Program
                    Dim Foo() As Integer

                    Sub Main(args As String())
                        {|SimplifyParent:Program|}.Foo(23) = 23
                    End Sub
                    End Module
                </Document>
                        </Project>
                    </Workspace>

            Dim expected =
              <text>
                    Module Program
                    Dim Foo() As Integer

                    Sub Main(args As String())
                        Foo(23) = 23
                    End Sub
                    End Module
                </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(547117)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestGetChanges_SimplifyTypeName_Array_2() As Task
            Dim input =
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module Program
    Dim Bar() As Action(Of Integer)

    Sub Main(args As String())
        {|SimplifyParent:Program.Bar|}(2)(2)
    End Sub
End Module
                </Document>
                        </Project>
                    </Workspace>

            Dim expected =
              <text>
Module Program
    Dim Bar() As Action(Of Integer)

    Sub Main(args As String())
        Bar(2)(2)
    End Sub
End Module
                </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(547117)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestGetChanges_SimplifyTypeName_Receiver1() As Task
            Dim input =
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Sub M(other As C)
        {|SimplifyParent:other.M|}(Nothing)
    End Sub
End Class
                </Document>
                        </Project>
                    </Workspace>

            Dim expected =
              <text>
Class C
    Sub M(other As C)
        other.M(Nothing)
    End Sub
End Class

                </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(547117)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestGetChanges_SimplifyTypeName_Receiver2() As Task
            Dim input =
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Sub M(other As C)
        {|SimplifyParent:Me.M|}(Nothing)
    End Sub
End Class
                </Document>
                        </Project>
                    </Workspace>

            Dim expected =
              <text>
Class C
    Sub M(other As C)
        M(Nothing)
    End Sub
End Class
                </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(547117)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestGetChanges_SimplifyTypeName_Receiver3() As Task
            Dim input =
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class A
    Public Shared B As Action

    Public Sub M(ab As A)
        {|SimplifyParent:ab.B|}()
    End Sub
End Class
                </Document>
                        </Project>
                    </Workspace>

            Dim expected =
              <text>
Class A
    Public Shared B As Action

    Public Sub M(ab As A)
        ab.B()
    End Sub
End Class
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyAllNodes_SimplifyNestedType() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    <![CDATA[
Class Preserve
	Public Class X
		Public Shared Y
	End Class
End Class

Class Z(Of T)
	Inherits Preserve
End Class

NotInheritable Class M
	Public Shared Sub Main()
        ReDim {|SimplifyParent:Z(Of Integer).X.Y(1)|}
	End Sub
End Class]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
Class Preserve
	Public Class X
		Public Shared Y
	End Class
End Class

Class Z(Of T)
	Inherits Preserve
End Class

NotInheritable Class M
	Public Shared Sub Main()
        ReDim [Preserve].X.Y(1)
	End Sub
End Class]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyAllNodes_SimplifyNestedType2() As Task
            ' Simplified type is in a different namespace.

            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Namespace N1
	Class Preserve
		Public Class X
			Public Shared Y
		End Class
	End Class
End Namespace

Namespace P
	Class NonGeneric
		Inherits N1.Preserve
	End Class
End Namespace

NotInheritable Class M
	Public Shared Sub Main()
		ReDim P.NonGeneric.{|SimplifyParent:X|}.Y(1)
	End Sub
End Class
               </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Namespace N1
	Class Preserve
		Public Class X
			Public Shared Y
		End Class
	End Class
End Namespace

Namespace P
	Class NonGeneric
		Inherits N1.Preserve
	End Class
End Namespace

NotInheritable Class M
	Public Shared Sub Main()
		ReDim N1.Preserve.X.Y(1)
	End Sub
End Class</text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyAllNodes_SimplifyNestedType3() As Task
            ' Simplified type is in a different namespace, whose names have been imported with an Imports statement.

            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports N1

Namespace N1
	Class Preserve
		Public Class X
			Public Shared Y
		End Class
	End Class
End Namespace

Namespace P
	Class NonGeneric
		Inherits N1.Preserve
	End Class
End Namespace

Namespace R
	NotInheritable Class M
		Public Shared Sub Main()
			Redim P.NonGeneric.{|SimplifyParent:X|}.Y(1)
		End Sub
	End Class
End Namespace
              </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports N1

Namespace N1
	Class Preserve
		Public Class X
			Public Shared Y
		End Class
	End Class
End Namespace

Namespace P
	Class NonGeneric
		Inherits N1.Preserve
	End Class
End Namespace

Namespace R
	NotInheritable Class M
		Public Shared Sub Main()
			Redim [Preserve].X.Y(1)
		End Sub
	End Class
End Namespace</text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyAllNodes_SimplifyNestedType4() As Task
            ' Highly nested type simplified to another highly nested type.

            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports P1.P2
Imports Q1.Q2

Namespace N1
	Namespace N2
		Public Class Outer
			Public Class Preserve
				Public Class X
					Public Shared Y As Integer
				End Class
			End Class
		End Class
	End Namespace
End Namespace

Namespace P1
	Namespace P2
		Public Class NonGeneric
			Inherits N1.N2.Outer.Preserve
		End Class
	End Namespace
End Namespace

Namespace Q1
	Namespace Q2
		Class Generic(Of T)
			Inherits NonGeneric
		End Class
	End Namespace
End Namespace

Namespace R
	NotInheritable Class M
		Public Shared Sub Main()
			Dim k As Integer = Generic(Of Integer).{|SimplifyParent:X|}.Y
		End Sub
	End Class
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  Imports P1.P2
Imports Q1.Q2

Namespace N1
	Namespace N2
		Public Class Outer
			Public Class Preserve
				Public Class X
					Public Shared Y As Integer
				End Class
			End Class
		End Class
	End Namespace
End Namespace

Namespace P1
	Namespace P2
		Public Class NonGeneric
			Inherits N1.N2.Outer.Preserve
		End Class
	End Namespace
End Namespace

Namespace Q1
	Namespace Q2
		Class Generic(Of T)
			Inherits NonGeneric
		End Class
	End Namespace
End Namespace

Namespace R
	NotInheritable Class M
		Public Shared Sub Main()
			Dim k As Integer = N1.N2.Outer.Preserve.X.Y
		End Sub
	End Class
End Namespace</text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyAllNodes_SimplifyNestedType5() As Task
            ' Name requiring multiple iterations of nested type simplification.

            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports N1.N2
Imports P1.P2
Imports Q1.Q2

Namespace N1
	Namespace N2
		Public Class Outer
			Public Class Preserve
				Public Class X
					Public Shared Y As Integer
				End Class
			End Class
		End Class
	End Namespace
End Namespace

Namespace P1
	Namespace P2
		Public Class NonGeneric
			Inherits Outer
			Public Class NonGenericInner
				Inherits Outer.Preserve
			End Class
		End Class
	End Namespace
End Namespace

Namespace Q1
	Namespace Q2
		Class Generic(Of T)
			Inherits NonGeneric
		End Class
	End Namespace
End Namespace

Namespace R
	NotInheritable Class M
		Public Shared Sub Main()
			Dim k As Integer = Generic(Of Integer).NonGenericInner.{|SimplifyParent:X|}.Y
		End Sub
	End Class
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports N1.N2
Imports P1.P2
Imports Q1.Q2

Namespace N1
	Namespace N2
		Public Class Outer
			Public Class Preserve
				Public Class X
					Public Shared Y As Integer
				End Class
			End Class
		End Class
	End Namespace
End Namespace

Namespace P1
	Namespace P2
		Public Class NonGeneric
			Inherits Outer
			Public Class NonGenericInner
				Inherits Outer.Preserve
			End Class
		End Class
	End Namespace
End Namespace

Namespace Q1
	Namespace Q2
		Class Generic(Of T)
			Inherits NonGeneric
		End Class
	End Namespace
End Namespace

Namespace R
	NotInheritable Class M
		Public Shared Sub Main()
			Dim k As Integer = Outer.Preserve.X.Y
		End Sub
	End Class
End Namespace</text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyAllNodes_SimplifyStaticMemberAccess() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Class Preserve
	Public Shared Y
End Class

Class Z(Of T)
	Inherits Preserve
End Class

NotInheritable Class M
	Public Shared Sub Main()
		Redim {|SimplifyParent:Z(Of Single).Y(1)|}
	End Sub
End Class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Class Preserve
	Public Shared Y
End Class

Class Z(Of T)
	Inherits Preserve
End Class

NotInheritable Class M
	Public Shared Sub Main()
		Redim [Preserve].Y(1)
	End Sub
End Class</text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyAllNodes_SimplifyQualifiedName() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Class A
	Public NotInheritable Class B
	End Class
End Class

Class C
	Inherits A
End Class

Namespace N1
	NotInheritable Class M
		Public Shared Function F() As {|SimplifyParent:C.B|}
			Return Nothing
		End Function
	End Class
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Class A
	Public NotInheritable Class B
	End Class
End Class

Class C
	Inherits A
End Class

Namespace N1
	NotInheritable Class M
		Public Shared Function F() As A.B
			Return Nothing
		End Function
	End Class
End Namespace</text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(551040), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyAllNodes_SimplifyAliasStaticMemberAccess() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports X = NonGeneric

Class Preserve
	Public Shared Y
End Class

Class NonGeneric
	Inherits Preserve
End Class

Namespace N1
	NotInheritable Class M
		Public Shared Sub Main()
			Redim {|SimplifyParent:X.Y(1)|}
		End Sub
	End Class
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports X = NonGeneric

Class Preserve
	Public Shared Y
End Class

Class NonGeneric
	Inherits Preserve
End Class

Namespace N1
	NotInheritable Class M
		Public Shared Sub Main()
			Redim [Preserve].Y(1)
		End Sub
	End Class
End Namespace</text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyNot_Delegate1_VB() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                Class A
                    Shared Sub Del()
                    End Sub
                    Class B
                        Delegate Sub Del()
                        Sub Boo()
                            Dim d As Del = New Del(AddressOf A.{|SimplifyParent:Del|})
                            A.{|SimplifyParent:Del|}()
                        End Sub
                    End Class
                End Class      
              </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                Class A
                    Shared Sub Del()
                    End Sub
                    Class B
                        Delegate Sub Del()
                        Sub Boo()
                            Dim d As Del = New Del(AddressOf A.Del)
                            A.Del()
                        End Sub
                    End Class
                End Class           
                </text>
            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyNot_Delegate2_VB() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                Class A
                    Shared Sub Bar()
                    End Sub
                    Class B
                        Delegate Sub Del()
                        Sub Bar()
                        End Sub
                        Sub Boo()
                            Dim d As Del = New Del(AddressOf A.{|SimplifyParent:Bar|})
                        End Sub
                    End Class
                End Class
              </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                Class A
                    Shared Sub Bar()
                    End Sub
                    Class B
                        Delegate Sub Del()
                        Sub Bar()
                        End Sub
                        Sub Boo()
                            Dim d As Del = New Del(AddressOf A.Bar)
                        End Sub
                    End Class
                End Class
                </text>
            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(570986)>
        <WorkItem(552722)>
        Public Async Function TestSimplifyNot_Action_VB() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                Imports System

                Class A
                    Shared Bar As Action(Of Integer) = New Action(Of Integer)(Function(x) x + 1)
                    Class B
                        Shared Bar As Action(Of Integer) = New Action(Of Integer)(Function(x) x + 1)
                        Sub Foo()
                            A.{|SimplifyParent:Bar|}(3)
                        End Sub
                    End Class
                End Class
              </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                Imports System

                Class A
                    Shared Bar As Action(Of Integer) = New Action(Of Integer)(Function(x) x + 1)
                    Class B
                        Shared Bar As Action(Of Integer) = New Action(Of Integer)(Function(x) x + 1)
                        Sub Foo()
                            A.Bar(3)
                        End Sub
                    End Class
                End Class
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyBaseInheritanceVB() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                MustInherit Class A
                    Public MustOverride Sub Foo()
                    Public Sub Boo()
                    End Sub
                End Class
                Class B
                    Inherits A
                    Public Overrides Sub Foo()
                        MyBase.{|SimplifyParent:Boo|}()
                    End Sub
                End Class    
              </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                MustInherit Class A
                    Public MustOverride Sub Foo()
                    Public Sub Boo()
                    End Sub
                End Class
                Class B
                    Inherits A
                    Public Overrides Sub Foo()
                        Boo()
                    End Sub
                End Class    
                </text>
            Await TestAsync(input, expected)
        End Function

        <WorkItem(588099)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_EscapeReservedNamesInAttributes() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System

&lt;Global.Assembly.{|SimplifyParent:Foo|}&gt;
Module Assembly
    Class FooAttribute
        Inherits Attribute
    End Class
End Module

Module M
    Class FooAttribute
        Inherits Attribute
    End Class
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System

&lt;[Assembly].Foo&gt;
Module Assembly
    Class FooAttribute
        Inherits Attribute
    End Class
End Module

Module M
    Class FooAttribute
        Inherits Attribute
    End Class
End Module
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_OmitModuleNameInMemberAccess() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System

Namespace foo
    Module Program
        Sub Main(args As String())

        End Sub
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
            foo.Program.{|SimplifyParent:Main|}(Nothing)
        End Sub
    End Module
End Namespace
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System

Namespace foo
    Module Program
        Sub Main(args As String())

        End Sub
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
            foo.Main(Nothing)
        End Sub
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_OmitModuleNameInQualifiedName() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System

Namespace foo
    Module Program
        Sub Main(args As String())
        End Sub
        Class C1
        End Class
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
            Dim x as foo.Program.{|SimplifyParent:C1|}
        End Sub
    End Module
End Namespace
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System

Namespace foo
    Module Program
        Sub Main(args As String())
        End Sub
        Class C1
        End Class
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
            Dim x as foo.C1
        End Sub
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(601160)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestExpandMultilineLambdaWithImports() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module Program
    {|Expand:Sub Main(args As String())
        Task.Run(Sub() 
Imports System 
End Sub)
    End Sub|}
End Module
        </Document>
    </Project>
</Workspace>
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(input)
                Dim hostDocument = workspace.Documents.Single()
                Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                Dim root = Await document.GetSyntaxRootAsync()

                For Each span In hostDocument.AnnotatedSpans("Expand")
                    Dim node = root.FindToken(span.Start).Parent.Parent
                    If TypeOf node Is MethodBlockBaseSyntax Then
                        node = DirectCast(node, MethodBlockBaseSyntax).Statements.Single()
                    End If

                    Assert.True(TypeOf node Is ExpressionStatementSyntax)

                    Dim result = Await Simplifier.ExpandAsync(node, document)

                    Assert.NotEqual(0, result.ToString().Count)
                Next
            End Using
        End Function

        <WorkItem(609496)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVB_DoNotReduceNamesInNamespaceDeclarations() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Imports System
            Namespace System.{|SimplifyParent:Foo|}
            End Namespace
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Code>
            Imports System
            Namespace System.Foo
            End Namespace
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608197)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVB_EscapeAliasReplacementIfNeeded() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Imports [In] = System.Runtime.InteropServices.InAttribute
            Module M
                Dim x = New System.Runtime.InteropServices.{|SimplifyParent:InAttribute|}() ' Simplify Type Name
            End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Code>
            Imports [In] = System.Runtime.InteropServices.InAttribute
            Module M
                Dim x = New [In]() ' Simplify Type Name
            End Module
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608197)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVB_NoNREForOmittedReceiverInWithBlock() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Public Class A
                Public Sub M()
                    With New B()
                        Dim x = .P.{|SimplifyParent:MaxValue|}
                    End With
                End Sub
            End Class

            Public Class B
                Public Property P As Integer
            End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Code>
            Public Class A
                Public Sub M()
                    With New B()
                        Dim x = .P.MaxValue
                    End With
                End Sub
            End Class

            Public Class B
                Public Property P As Integer
            End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(639971)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestBugFix639971_VisualBasic_FalseUnnecessaryBaseQualifier() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Z
    Public Function Bar() As String
        Return MyBase.{|SimplifyParent:ToString|}()
    End Function
End Class

Class Y
    Inherits Z
    Public Overrides Function ToString() As String
        Return ""
    End Function

    Public Sub Baz()
        Console.WriteLine(New Y().Bar())
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Code>
Class Z
    Public Function Bar() As String
        Return MyBase.ToString()
    End Function
End Class

Class Y
    Inherits Z
    Public Overrides Function ToString() As String
        Return ""
    End Function

    Public Sub Baz()
        Console.WriteLine(New Y().Bar())
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(639971)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestBugFix639971_CSharp_FalseUnnecessaryBaseQualifier() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C
{
    public string InvokeBaseToString()
    {
        return base.{|SimplifyParent:ToString|}();
    }
}

class D : C
{
    public override string ToString()
    {
        return "";
    }

    static void Main()
    {
        Console.WriteLine(new D().InvokeBaseToString());
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Code>
using System;

class C
{
    public string InvokeBaseToString()
    {
        return base.ToString();
    }
}

class D : C
{
    public override string ToString()
    {
        return "";
    }

    static void Main()
    {
        Console.WriteLine(new D().InvokeBaseToString());
    }
}
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyTypeNameWhenParentHasSimplifyAnnotation() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports System
Namespace Root
    {|SimplifyParent:Class A
        Dim c As System.Exception
    End Class|}
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports System
Namespace Root
    Class A
        Dim c As Exception
    End Class
End Namespace
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyTypeNameWithExplicitSimplifySpan_MutuallyExclusive() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
{|SpanToSimplify:Imports System|}
Namespace Root
    {|SimplifyParent:Class A
        Dim c As System.Exception
    End Class|}
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports System
Namespace Root
    Class A
        Dim c As System.Exception
    End Class
End Namespace
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyTypeNameWithExplicitSimplifySpan_Inclusive() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
{|SpanToSimplify:Imports System
Namespace Root
    {|SimplifyParent:Class A
        Dim c As System.Exception
    End Class|}
End Namespace
|}
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports System
Namespace Root
    Class A
        Dim c As Exception
    End Class
End Namespace
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyTypeNameWithExplicitSimplifySpan_OverlappingPositive() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports System
Namespace Root
    {|SimplifyParent:Class A
        Dim c As {|SpanToSimplify:System.Exception|}
    End Class|}
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports System
Namespace Root
    Class A
        Dim c As Exception
    End Class
End Namespace
                </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyTypeNameWithExplicitSimplifySpan_OverlappingNegative() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports System
Namespace Root
    {|SimplifyParent:Class A
        {|SpanToSimplify:Dim c|} As System.Exception
    End Class|}
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports System
Namespace Root
    Class A
        Dim c As System.Exception
    End Class
End Namespace
                </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(769354)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyTypeNameInCrefCausesConflict() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document><![CDATA[
Imports System

Class Base
    Public Sub New(x As Integer)
    End Sub
End Class
Class Derived : Inherits Base
    ''' <summary>
    ''' <see cref="{|SimplifyParent:Global.Base|}.New(Integer)"/>
    ''' </summary>
    ''' <param name="x"></param>
    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
    Public Sub Base(x As Integer)
    End Sub
End Class]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text><![CDATA[
Imports System

Class Base
    Public Sub New(x As Integer)
    End Sub
End Class
Class Derived : Inherits Base
    ''' <summary>
    ''' <see cref="Base.New(Integer)"/>
    ''' </summary>
    ''' <param name="x"></param>
    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
    Public Sub Base(x As Integer)
    End Sub
End Class]]>
              </text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification), WorkItem(864735)>
        Public Async Function TestBugFix864735_VisualBasic_SimplifyNameInIncompleteIsExpression() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports System
Class C
    Shared Public F As Integer
    Shared Sub Main()
        Console.WriteLine(TypeOf {|SimplifyParent:C.F|} Is
    End Sub
End Class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports System
Class C
    Shared Public F As Integer
    Shared Sub Main()
        Console.WriteLine(TypeOf F Is
    End Sub
End Class
                </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(813566)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestSimplifyQualifiedCref() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document><![CDATA[
Imports System

''' <summary>
''' <see cref="{|Simplify:System.Object|}"/>
''' </summary>
Class Program
End Class]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text><![CDATA[
Imports System

''' <summary>
''' <see cref="Object"/>
''' </summary>
Class Program
End Class]]>
              </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(838109)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontSimplifyToGenericName() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document><![CDATA[
Imports System

Class Program
    Sub Main(args As String())
        {|SimplifyParent:C.D|}.F()
    End Sub
End Class
Public Class C(Of T)
    Public Class D
        Public Shared Sub F()
        End Sub
    End Class
End Class
Public Class C
    Inherits C(Of Integer)
End Class
]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text><![CDATA[
Imports System

Class Program
    Sub Main(args As String())
        C.D.F()
    End Sub
End Class
Public Class C(Of T)
    Public Class D
        Public Shared Sub F()
        End Sub
    End Class
End Class
Public Class C
    Inherits C(Of Integer)
End Class
]]>
              </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(838109)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoSimplifyToGenericName() As Task
            Dim simplificationOption = New Dictionary(Of OptionKey, Object) From {{SimplificationOptions.AllowSimplificationToGenericType, True}}

            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document><![CDATA[
Imports System

Class Program
    Sub Main(args As String())
        {|SimplifyParent:C.D|}.F()
    End Sub
End Class
Public Class C(Of T)
    Public Class D
        Public Shared Sub F()
        End Sub
    End Class
End Class
Public Class C
    Inherits C(Of Integer)
End Class
]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text><![CDATA[
Imports System

Class Program
    Sub Main(args As String())
        C(OfInteger).D.F()
    End Sub
End Class
Public Class C(Of T)
    Public Class D
        Public Shared Sub F()
        End Sub
    End Class
End Class
Public Class C
    Inherits C(Of Integer)
End Class
]]>
              </text>

            Await TestAsync(input, expected, simplificationOption)
        End Function

        <WpfFact, WorkItem(838109), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestDontSimplifyAllNodes_SimplifyNestedType() As Task
            Dim simplificationOption = New Dictionary(Of OptionKey, Object) From {{SimplificationOptions.AllowSimplificationToBaseType, False}}

            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    <![CDATA[
Class Preserve
	Public Class X
		Public Shared Y
	End Class
End Class

Class Z(Of T)
	Inherits Preserve
End Class

NotInheritable Class M
	Public Shared Sub Main()
        ReDim {|SimplifyParent:Z(Of Integer).X.Y(1)|}
	End Sub
End Class]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
Class Preserve
	Public Class X
		Public Shared Y
	End Class
End Class

Class Z(Of T)
	Inherits Preserve
End Class

NotInheritable Class M
	Public Shared Sub Main()
        ReDim Z(Of Integer).X.Y(1)
	End Sub
End Class]]></text>

            Await TestAsync(input, expected, simplificationOption)
        End Function

        <WpfFact, WorkItem(838109), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_TestDontSimplifyAwayMeExpression() As Task
            Dim simplificationOption = New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, LanguageNames.VisualBasic), True}}

            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    <![CDATA[
Imports System
Class Program
    Private s As Integer
    Shared Sub Main(args As String())
    End Sub
    Public Function give() As Integer
        Return {|SimplifyParent:Me.s|}
    End Function
End Class]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
Imports System
Class Program
    Private s As Integer
    Shared Sub Main(args As String())
    End Sub
    Public Function give() As Integer
        Return Me.s
    End Function
End Class]]></text>

            Await TestAsync(input, expected, simplificationOption)
        End Function

        <WpfFact, WorkItem(881746), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_SimplyToAlias() As Task

            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    <![CDATA[
Imports System
Imports AttributeAttributeAttribute = AttributeAttribute
Class AttributeAttribute
    Inherits Attribute
End Class
 
<{|SimplifyParent:Attribute|}>
Class A
End Class]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
Imports System
Imports AttributeAttributeAttribute = AttributeAttribute
Class AttributeAttribute
    Inherits Attribute
End Class
 
<AttributeAttributeAttribute>
Class A
End Class]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(881746), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontSimplifyAlias() As Task

            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    <![CDATA[
Imports System
Imports AttributeAttributeAttribute = AttributeAttribute
Class AttributeAttribute
    Inherits Attribute
End Class
 
<{|SimplifyParent:AttributeAttributeAttribute|}>
Class A
End Class]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
Imports System
Imports AttributeAttributeAttribute = AttributeAttribute
Class AttributeAttribute
    Inherits Attribute
End Class
 
<AttributeAttributeAttribute>
Class A
End Class]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(966633), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontSimplifyNullableQualifiedName() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    <![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim x as {|SimplifyParent:Nullable(Of Integer)|}.Value
    End Sub
    Sub M(a as {|SimplifyParent:Nullable(Of Integer)|}, byref b as {|SimplifyParent:System.Nullable(Of Integer)|}, byref c as {|SimplifyParent:Nullable(Of Integer)|}.Value)
    End Sub
End Module]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim x as Nullable(Of Integer).Value
    End Sub
    Sub M(a as Integer?, byref b as Integer?, byref c as Nullable(Of Integer).Value)
    End Sub
End Module]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(965240), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontSimplifyOpenGenericNullable() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    <![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim x = GetType({|SimplifyParent:System.Nullable(Of )|})
        Dim y = (GetType({|SimplifyParent:System.Nullable(Of Long)|}))
        Dim z = (GetType({|SimplifyParent:System.Nullable|}))
    End Sub
End Module]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim x = GetType(Nullable(Of ))
        Dim y = (GetType(Long?))
        Dim z = (GetType(Nullable))
    End Sub
End Module]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact(Skip:="1019361"), WorkItem(1019361), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Bug1019361() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    <![CDATA[
Imports N
Namespace N
    Class A
        Public Const X As Integer = 1
    End Class
End Namespace

Module Program
    Sub Main()
        Dim x = {|SimplifyParent:N.A|}.X ' Simplify type name 'N.A' 
        Dim a As A = Nothing
    End Sub
End Module]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
Imports N
Namespace N
    Class A
        Public Const X As Integer = 1
    End Class
End Namespace

Module Program
    Sub Main()
        Dim x = N.A.X ' Simplify type name 'N.A' 
        Dim a As A = Nothing
    End Sub
End Module]]></text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, WorkItem(2232, "https://github.com/dotnet/roslyn/issues/2232"), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontSimplifyToPredefinedTypeNameInQualifiedName() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    <![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim x = New {|SimplifyParent:System.Int32|}.Blah
    End Sub
End Module]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim x = New System.Int32.Blah
    End Sub
End Module]]></text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4859, "https://github.com/dotnet/roslyn/issues/4859")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontSimplifyNullableInNameOfExpression() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    <![CDATA[
Imports System
Class C
    Sub M()
        Dim s = NameOf({|Simplify:Nullable(Of Integer)|})
    End Sum
End Class
]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  <![CDATA[
Imports System
Class C
    Sub M()
        Dim s = NameOf(Nullable(Of Integer))
    End Sum
End Class
]]></text>

            Await TestAsync(input, expected)
        End Function
#End Region

    End Class
End Namespace
