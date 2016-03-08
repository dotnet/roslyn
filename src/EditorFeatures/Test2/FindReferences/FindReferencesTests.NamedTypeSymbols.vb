' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
#Region "FAR on reference types"

        <WorkItem(541155, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541155")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInaccessibleVar1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class A
{
    private class {|Definition:$$var|} { }
}
 
class B : A
{
    static void Main()
    {
        var x = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(541155, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541155")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInaccessibleVar2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class A
{
    private class var { }
}
 
class B : A
{
    static void Main()
    {
        $$var x = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(541151, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541151")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestGenericVar1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        var x = 1;
    }
}
 
class {|Definition:$$var|}<T> { }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(541151, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541151")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestGenericVar2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        $$var x = 1;
    }
}
 
class var<T> { }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Class() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:$$C|}
        {
            private [|C|] c1, c2;

            void Foo([|C|] c3)
            {
                [|C|] c4;
            }

            void Bar(System.C c5)
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_NestedClass() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            class {|Definition:Nested|} { }
            void Method()
            {
                [|$$Nested|] obj = new [|Nested|]();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_ExplicitCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:Celsius|}
        {
            private float degrees;
            public {|Definition:Celsius|}(float temp)
            {
                degrees = temp;
            }
        }

        class Fahrenheit
        {
            private float degrees;
            public Fahrenheit(float temp)
            {
                degrees = temp;
            }
            public static explicit operator [|Celsius|](Fahrenheit f)
            {
                return new [|Celsius|]((5.0f / 9.0f) * (f.degrees - 32));
            }
        }

        class MainClass
        {
            static void Main()
            {
                Fahrenheit f = new Fahrenheit(100.0f);
                [|Celsius|] c = ([|C$$elsius|])f;
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Events() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        public class CustomEventArgs : System.EventArgs
        {
            public CustomEventArgs(string s)
            {
                msg = s;
            }
            private string msg;
            public string Message
            {
                get { return msg; }
            }
        }

        public class Publisher
        {
            public delegate void SampleEventHandler(object sender, CustomEventArgs e);
            public event SampleEventHandler {|Definition:SampleEvent|};

            protected virtual void RaiseSampleEvent()
            {
                [|$$SampleEvent|](this, new CustomEventArgs("Hello"));
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_TypeOfOperator() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:Celsius|}
        {
            public {|Definition:Celsius|}()
            {
                System.Type t = typeof([|$$Celsius|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539799")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_InaccessibleType() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    private class {|Definition:$$D|}
    {
    }
}

class A
{
    void M()
    {
        C.[|D|] d = new C.[|D|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_OneDimensionalArray() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Method()
            {
                int[] {|Definition:a|} = {0, 2, 4, 6, 8};
                int b = [|$$a|][0];
                    b = [|a|][1];
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_BaseList() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:Base|} { }
        class Derived : [|$$Base|] { }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_StaticConstructor() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:S$$impleClass|}
        {
            static {|Definition:SimpleClass|}()
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_GenericClass() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class {|Definition:$$C|}<T>
        {
            private [|C|] c1;
            private [|C|]<T> c2;
            private [|C|]<int> c3;
            private [|C|]<[|C|]<T>> c4;
            private [|C|]<int,string> c5;
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_GenericClass1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class {|Definition:$$C|}
        {
            private [|C|] c1;
            private [|C|]<T> c2;
            private [|C|]<int> c3;
            private [|C|]<[|C|]<T>> c4;
            private [|C|]<int,string> c5;
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_GenericClass2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class {|Definition:$$C|}
        {
            private [|C|] c1;
            private C<T> c2;
            private C<int> c3;
            private C<C<T>> c4;
            private [|C|]<int,string> c5;
        }
        class C<T>
        {
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_GenericClass3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class {|Definition:$$C|}<T>
        {
            private C c1;
            private [|C|]<T> c2;
            private [|C|]<int> c3;
            private [|C|]<[|C|]<T>> c4;
            private [|C|]<int,string> c5;
        }
        class C
        {
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadedMembersFromConstructedInterfaces1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
class Program
{
    static void Main(string[] args)
    {
        I1<int> m = new Basic();
        Console.WriteLine(m.$$[|Add|](1, 1));
    }
}

public interface I1<T>
{
    T {|Definition:Add|}(T arg1, T arg2);
}

public class Basic : I1<int>
{
    public int {|Definition:Add|}(int arg1, int arg2)
    { return arg1 + arg2; }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadedMembersFromConstructedInterfaces2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
class Program
{
    static void Main(string[] args)
    {
        I1<int> m = new Basic();
        Console.WriteLine(m.[|Add|](1, 1));
    }
}

public interface I1<T>
{
    T {|Definition:$$Add|}(T arg1, T arg2);
}

public class Basic : I1<int>
{
    public int {|Definition:Add|}(int arg1, int arg2)
    { return arg1 + arg2; }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadedMembersFromConstructedInterfaces3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
class Program
{
    static void Main(string[] args)
    {
        I1<int> m = new Basic();
        Console.WriteLine(m.[|Add|](1, 1));
    }
}

public interface I1<T>
{
    T {|Definition:Add|}(T arg1, T arg2);
}

public class Basic : I1<int>
{
    public int {|Definition:$$Add|}(int arg1, int arg2)
    { return arg1 + arg2; }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadedMembersFromConstructedInterfaces4() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I<X>
{
    void {|Definition:$$Foo|}(X x);
}

class C : I<int>, I<string>
{
    public void {|Definition:Foo|}(int x) { }
    public void {|Definition:Foo|}(string x) { }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadedMembersFromConstructedInterfaces5() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I<X>
{
    void {|Definition:Foo|}(X x);
}

class C : I<int>, I<string>
{
    public void {|Definition:$$Foo|}(int x) { }
    public void {|Definition:Foo|}(string x) { }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadedMembersFromConstructedInterfaces6() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I<X>
{
    void {|Definition:Foo|}(X x);
}

class C : I<int>, I<string>
{
    public void {|Definition:Foo|}(int x) { }
    public void {|Definition:$$Foo|}(string x) { }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_MultipleFiles() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:$$C|}
        {
        }
        </Document>
        <Document>
        class D
        {
            private [|C|] c;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_MultipleFiles_InOneFileOnly() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:$$C|}
        {
            private [|C|] c;
        }
        </Document>
        <Document>
        class D
        {
            private C c;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input, searchSingleFileOnly:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpImplicitConstructor() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:$$C|}
        {
            void Foo()
            {
                new [|C|]();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpExplicitConstructor() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:$$C|}
        {
            public {|Definition:C|}() { }

            void Foo()
            {
                new [|C|]();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_VBImplicitConstructor() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class {|Definition:$$C|}
            Shared Sub Main()
                Dim c As [|C|] = New [|C|]()
            End Function
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpConstructorCallUsingNewOperator1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           bool b;
           C()
           {
              [|M|] m = new [|M|]() { b };
           }
        }
        class {|Definition:$$M|}
        {
           public {|Definition:M|}(bool i) { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpConstructorCallUsingNewOperator2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            C()
            {
                bool b;
                [|M|] m = new [|M|](b);
            }
        }
        class {|Definition:$$M|}
        {
            public {|Definition:M|}(bool i) { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_VBConstructorCallUsingNewOperator() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
            Sub New()
                Dim m As [|M|] = New [|M|] (2)
            End Function
        End Class 
        Class {|Definition:$$M|}
             Sub {|Definition:New|}(ByVal i As Integer)
             End Sub
        End Class   
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_VBModule() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Module {|Definition:Program|}
            Sub Main(args As String())

            End Function
        End Module

        Class Test
            Sub Test()
                [|$$Program|].Main("")
            End Function
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_PartialClass() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>            
        partial class {|Definition:$$C|}
        {
           public static bool operator ==([|C|] a, [|C|] b)
           {
              return a.ToString() == b.ToString();
           }
        }
        public class M
        {
           [|C|] pc;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Interface() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>            
        interface {|Definition:$$IMyInterface|} { }
        class InterfaceImplementer : [|IMyInterface|] { }
        </Document>
        <Document>
        class C
        {
           [|IMyInterface|] iMyInter;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_GenericInterface() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[          
        interface {|Definition:$$GenericInterface|}<T> { }
        class GenericClass<T> : [|GenericInterface|]<T> 
        {
        }]]>
        </Document>
        <Document><![CDATA[  
        class C
        {
           [|GenericInterface|]<C> MyGenInt;
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539065, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539065")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Delegate() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>            
        class C
        {
            delegate void {|Definition:$$MyDelegate|}();

            public void F()
            {
               [|MyDelegate|] MD1 = new [|MyDelegate|](this.F);
               [|MyDelegate|] MD2 = MD1 + MD1;
               [|MyDelegate|] MD3 = new [|MyDelegate|](MD1);
            }             
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539065, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539065")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Delegate1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>            
        class C
        {
            delegate void {|Definition:$$MyDelegate|}();

            public void F()
            {
               [|MyDelegate|] MD;
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539065, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539065")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Delegate2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>            
        class C
        {
            delegate void {|Definition:MyDelegate|}();

            public void F()
            {
               [|$$MyDelegate|] MD;
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539614")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Delegate3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    static void Main(string[] args)
    {
        myDel = new [|TestDelegate|](Foo);
    }

    private delegate void {|Definition:$$TestDelegate|}(string s);
    private static [|TestDelegate|] myDel;

    static void Foo(string arg) { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539614")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Delegate4() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    static void Main(string[] args)
    {
        myDel = new [|$$TestDelegate|](Foo);
    }

    private delegate void {|Definition:TestDelegate|}(string s);
    private static [|TestDelegate|] myDel;

    static void Foo(string arg) { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539614")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Delegate5() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    static void Main(string[] args)
    {
        myDel = new [|TestDelegate|](Foo);
    }

    private delegate void {|Definition:TestDelegate|}(string s);
    private static [|$$TestDelegate|] myDel;

    static void Foo(string arg) { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539646, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539646")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Delegate6() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
 using System;
class Program
{
    delegate R {|Definition:Func|}<T, R>(T t);
    static void Main(string[] args)
    {
        [|Func|]<int, int> f = (arg) =>
        {
            int s = 3;
            return s;
        };
        f.[|$$BeginInvoke|](2, null, null);
    }
}
            ]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(537966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537966")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CalledDynamic1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>            
        class {|Definition:dyn$$amic|}
        {
        }
        class @static : [|dynamic|]
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(537966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537966")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CalledDynamic2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:dyn$$amic|}
        {
        }
        </Document>
        <Document>
        class @static : [|dynamic|]
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538842")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CalledSystemString1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace System
        {
          public class {|Definition:St$$ring|}
          {
            void Foo(string s) { }
            void Foo([|String|] s) { }
          }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538842")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CalledSystemString2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace System
        {
          public class String
          {
            void Foo([|st$$ring|] s) { }
            void Foo(String s) { }
          }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538926")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CalledSystemString3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
namespace System
{
    public class {|Definition:String|}
    {
        void Foo([|Str$$ing|] s) { }
        void Foo(params [|String|][] s) { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539299, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539299")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_LeftSideOfMemberAccessExpression1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
public class C
{
    void Foo()
    {
        [|$$Console|].Write(0);
        [|Console|].Write(0);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539299, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539299")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_LeftSideOfMemberAccessExpression2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
public class C
{
    void Foo()
    {
        [|Console$$|].Write(0);
        [|Console|].Write(0);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539299, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539299")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_LeftSideOfMemberAccessExpression3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Public Class C
    Sub Foo()
        [|$$Console|].Write(0)
        [|Console|].Write(0)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539299, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539299")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_LeftSideOfMemberAccessExpression4() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Public Class C
    Sub Foo()
        [|Console$$|].Write(0)
        [|Console|].Write(0)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefNamedType() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class {|Definition:Program|}
{
    ///  <see cref="[|$$Program|]"/> to start the program.
    static void Main(string[] args)
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefNamedType2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class {|Definition:Progr$$am|}
{
    ///  <see cref="[|Program|]"/> to start the program.
    static void Main(string[] args)
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(775925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775925")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_LeftSideOfGreaterThanTokenInAttribute() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

<[|Obsolete$$|]>
Public Class Class1
    <[|Obsolete|]>
    Dim obsoleteField as Integer
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR on primitive types"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_PrimitiveTypeAsMethodParameter() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        namespace N
        {
            class D
            {
                void Foo([|str$$ing|] s)
                {
                }
            }
        }]]>
        </Document>
        <Document><![CDATA[
        using System;
        class C
        {
            [|String|] s;
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_PrimitiveTypeAsField() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        namespace N
        {
            class D
            {
                void Foo([|string|] s)
                {
                }
            }
        }]]>
        </Document>
        <Document><![CDATA[
        using System;
        class C
        {
            [|Str$$ing|] s;
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR on value types"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Struct() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>            
        public struct {|Definition:$$S|} { }
        public class M
        {
           [|S|] s;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_Enum() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>            
        public class EnumTest
        {
            enum {|Definition:Days|} { Sat = 1, Sun, Mon, Tue, Wed, Thu, Fri };

            static void Main()
            {
                int x = (int)[|$$Days|].Sun;
                int y = (int)[|Days|].Fri;
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_EnumMembers() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>            
        public class EnumTest
        {
            enum Days { Sat = 1, {|Definition:Sun|}, Mon, Tue, Wed, Thu, Fri };

            static void Main()
            {
                int x = (int)Days.[|$$Sun|];
                int y = (int)Days.Fri;
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR on across projects"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_NonDependentProjectCSharpRefsCSharp() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        public class {|Definition:$$C|}
        {
        }
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class D
        {
            private C c;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_NonDependentProjectVBRefsCSharp() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        public class {|Definition:$$C|}
        {
        }
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        class D
            private c as C = nothing
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_InDependentProjectCSharpRefsCSharp() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
        <Document>
        public class {|Definition:$$C|}
        {
        }
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly2" CommonReferences="true">
        <ProjectReference>CSharpAssembly1</ProjectReference>
        <Document>
        class D
        {
            private [|C|] c;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_InDependentProjectVBRefsCSharp() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
        <Document>
        public class {|Definition:$$C|}
        {
        }
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>CSharpAssembly1</ProjectReference>
        <Document>
        class D
            private c as [|C|] = nothing
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR in namespaces"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_InNamespace() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        namespace N
        {
            class {|Definition:$$C|}
            {
            }
        }

        class D : N.[|C|]
        {
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR with case sensitivity"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CaseSensitivity() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
        <Document>
        public class {|Definition:$$C|}
        {
            [|C|] c1;
        }
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly2" CommonReferences="true">
        <ProjectReference>CSharpAssembly1</ProjectReference>
        <Document>
        public class M
        {
            [|C|] c1;
            c c1;
        }
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>CSharpAssembly1</ProjectReference>
        <Document>
        class D
            private c1 as [|C|] = nothing
            private c2 as [|c|] = nothing
        end class
        </Document>
        <Document>
        class E
            ' Find, even in file without the original symbol name.
            private c1 as [|c|] = nothing
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR through alias"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_ThroughAlias() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N
        {
            class {|Definition:$$D|}
            {
            }
        }
        </Document>
        <Document>
        using M = N.[|D|];
        class C
        {
            [|M|] d;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_ThroughAliasNestedType() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N
        {
            public class {|Definition:$$D|}
            {
                public class Inner
                {
                }
            }
        }
        </Document>
        <Document>
        using M = N.[|D|].Inner;
        class C
        {
            M d;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_ThroughAliasGenericType() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        namespace N
        {
            class {|Definition:$$D|}<T>
            {
            }
        }]]>
        </Document>
        <Document><![CDATA[
        using M = N.[|D|]<int>;
        class C
        {
            [|M|] d;
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR on object initializers"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_ReferenceInObjectInitializers() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           bool {|Definition:b|};
           C()
           {
             new C() {[|$$b|]= false};
           }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_ReferenceInObjectInitializersConstructorTakesNoParms() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           bool b;
           C()
           {
             [|M|] m = new [|M|]() { b };
           }
        }
        class {|Definition:$$M|}
        {
           public {|Definition:M|}(bool i)
           {
           }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR on collection initializers"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocal_CSharpColInitWithMultipleExpressionContainSameIdentifier() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C
        {          
            void M()
            {                    
                var col = new List<string> {[|Foo|](1), {[|Foo|](2), {[|Foo|](3), {[|Foo|](4) };
            }
            string {|Definition:$$Foo|}(int i) { return "1";}
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocal_VBColInitWithMultipleExpressionContainSameIdentifier() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C                
           sub M()
               Dim col = New List(Of string) From {[|Foo|](1), [|$$Foo|](2), [|Foo|](3), [|Foo|](4) }
           End Sub
           Function {|Definition:Foo|}(ByVal i as Integer) as string
                return "1"
           End Function
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR on array initializers"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpArrayInitializerContainsALongExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {         
            int {|Definition:bravo|} = 35;
            int charlie = 47;
            int delta = 314;
            int echo = -5;

            void M()
            {
              var alpha = new[] { ([|bravo|] + [|bravo|] % charlie) | (/*Ren1*/[|bravo|] * delta) % charlie - 4, echo, delta >> [|$$bravo|] / 5 };
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_VBArrayInitializerContainsALongExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C                
           Dim {|Definition:bravo|} As Integer = 35
           Dim charlie As Integer = 47
           Dim delta As Integer = 314
           Dim echo As Integer = -5
           sub M()
               Dim alpha = {([|bravo|] + [|bravo|] Mod charlie) Or ([|bravo|] * delta) Mod charlie - 4, echo, delta >> [|$$bravo|] \ 5}
           End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpArrayInitializerContansANestedArrayInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {         
            var hotel = new[] { 1, 3, 5, 6 };
            var india = new[] { 7, 8, 9, 10 };
            var juliet = new[] { 1 };
            var kilo = new[] { hotel, india, juliet };
            int {|Definition:november|} = 5;   
            void M()
            {
               var lima = new[] { hotel, india, new[] { 3, [|$$november|],} };
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_VBArrayInitializerContainsANestedArrayInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C                
           Dim hotel = {1, 3, 5, 6}
           Dim {|Definition:$$india|} = {7, 8, 9, 10}
           Dim juliet = {1}
           Dim kilo = {hotel, [|india|], juliet}
           sub M()
               Dim lima = {hotel, [|india|]}
           End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpArrayInitializerDifferentTypesWithImplicitCasting() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {          
            void M()
            {
                var {|Definition:$$oscar|} = 350.0 / 72F;
                int papa = (int) [|oscar|];
                var quebec = new[] { [|oscar|], papa, -758, 2007, papa % 4 };
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_VBArrayInitializerDifferentTypesWithImplicitCasting() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C                
           sub M()
               Dim {|Definition:$$oscar|} = 350.0 / 72
               Dim papa as Integer = [|oscar|]
               Dim quebec = { [|oscar|], papa, -758, 2007, papa % 4 }
           End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpImplicitlyTypedArray() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {          
            void M()
            {
                var v1 = 1.0;
                var {|Definition:v2|} = -3;
                var array = new[] { v1, [|$$v2|] };
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_VBImplicitlyTypedArray() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C                
           sub M()
               Dim {|Definition:v1|} = 1.0
               Dim v2 =  -3
               Dim array = { [|$$v1|], v2 }
           End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR on query expressions"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpQueryExpressionInitializedViaColInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C
        {          
            void M()
            {
                 var {|Definition:$$col|} = new List<int> { 1, 2, 6, 24, 120 };
                 var query = from i in [|col|] select i * 2;
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_VBQueryExpressionInitializedViaColInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
           Private Sub M()
               Dim {|Definition:col|} = New List(Of Integer)() From {1, 2}
               Dim query = From i In [|$$col|] Where i * 2 > 1
           End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpQueryExpressionThatIncludeColInit() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C
        {          
            void M()
            {
                 int rolf = 732;
                 int {|Definition:$$roark|} = -9;
                 var replicator = from r in new List<int> { 1, 2, 9, rolf, [|roark|] } select r * 2;
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_VBQueryExpressionThatIncludeColInit() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
           Private Sub M()
               Dim {|Definition:rolf|} As Integer = 7;
               Dim roark As Integer = -9;
               Dim query = From i In New List(Of Integer)() From { 1, 2, 9, [|$$rolf|], roark } select r * 2;
           End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR in Venus Contexts"

        <WorkItem(545325, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545325")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestHiddenCodeIsNotVisibleFromUI() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class _Default
{
#line 1 "CSForm1.aspx"
   [|$$_Default|] a;
#line default
#line hidden
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input, uiVisibleOnly:=True)
        End Function

        <WorkItem(545325, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545325")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestHiddenCodeIsAccessibleViaApis() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class {|Definition:_Default|}
{
#line 1 "CSForm1.aspx"
   [|$$_Default|] a;
#line default
#line hidden
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input, uiVisibleOnly:=False)
        End Function

#End Region

        <WorkItem(542949, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542949")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_DoNotFindDestructor1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class {|Definition:$$A|} {    ~{|Definition:A|}()    {        Console.WriteLine("A");    }}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(546229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546229")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CrossLanguageModule() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
Public Module {|Definition:$$Foo|}
    Public Sub Bar()
    End Sub
End Module
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
class C
{
    void M()
    {
        [|Foo|].Bar();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingType_Basic() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        [|$$int|] x;
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
class Class2
{
    [|int|] x;
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingType_GenericType() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        [|$$Tuple|]<int> x;
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
using System;

class Class2
{
    [|Tuple|]<int> x;
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDuplicatePublicTypeWithDuplicateConstructors() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="ClassLibrary1" CommonReferences="true">
        <Document><![CDATA[
public class {|Definition:$$C|}
{
    public {|Definition:C|}(D d1, D d2) { new [|C|](d1, d2); }
}

public class D { }
]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="ClassLibrary2" CommonReferences="true">
        <ProjectReference>ClassLibrary1</ProjectReference>
        <Document><![CDATA[
public class C
{
    public C(D d1, D d2) { new C(d1, d2); }
}
public class D { }
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(1174256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174256")>
        Public Async Function TestFarWithInternalsVisibleToNull() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="ClassLibrary1" CommonReferences="true">
        <Document><![CDATA[
        [assembly: System.Runtime.CompilerServices.InternalsVisibleTo(null)]
        internal class {|Definition:$$A|}
        {
        }]]>
        </Document>
    </Project>

    <Project Language="C#" AssemblyName="ClassLibrary2" CommonReferences="true">
        <ProjectReference>ClassLibrary1</ProjectReference>
        <Document><![CDATA[
        public class B : A
        {
        }]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)

        End Function

    End Class
End Namespace
