' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
#Region "FAR on reference types"

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541155")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestInaccessibleVar1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541155")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestInaccessibleVar2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541151")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGenericVar1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541151")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGenericVar2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Class(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:$$C|}
        {
            private [|C|] c1, c2;

            void Goo([|C|] c3)
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_NestedClass(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_ExplicitCast(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Events(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_TypeOfOperator(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/79100")>
        Public Async Function TestNamedType_TypeOfOperator_Name(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:Celsius|}
        {
            public {|Definition:Celsius|}()
            {
                System.Type t = typeof({|ValueUsageInfo.Name:[|$$Celsius|]|});
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/79100")>
        Public Async Function TestNamedType_SizeOfOperator_Name(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:Celsius|}
        {
            public {|Definition:Celsius|}()
            {
                int t = sizeof({|ValueUsageInfo.Name:[|$$Celsius|]|});
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/79100")>
        Public Async Function TestNamedType_NameOfOperator_Name(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:Celsius|}
        {
            public {|Definition:Celsius|}()
            {
                string t = nameof({|ValueUsageInfo.Name:[|$$Celsius|]|});
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539799")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_InaccessibleType(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_OneDimensionalArray(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_BaseList(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:Base|} { }
        class Derived : [|$$Base|] { }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_StaticConstructor(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_GenericClass(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_GenericClass1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_GenericClass2(kind As TestKind, host As TestHost) As Task
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
            private C<int,string> c5;
        }
        class C<T>
        {
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_GenericClass3(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadedMembersFromConstructedInterfaces1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadedMembersFromConstructedInterfaces2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadedMembersFromConstructedInterfaces3(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadedMembersFromConstructedInterfaces4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I<X>
{
    void {|Definition:$$Goo|}(X x);
}

class C : I<int>, I<string>
{
    public void {|Definition:Goo|}(int x) { }
    public void {|Definition:Goo|}(string x) { }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadedMembersFromConstructedInterfaces5_Api(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I<X>
{
    void {|Definition:Goo|}(X x);
}

class C : I<int>, I<string>
{
    public void {|Definition:$$Goo|}(int x) { }
    public void {|Definition:Goo|}(string x) { }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadedMembersFromConstructedInterfaces5_FEature(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I<X>
{
    void {|Definition:Goo|}(X x);
}

class C : I<int>, I<string>
{
    public void {|Definition:$$Goo|}(int x) { }
    public void Goo(string x) { }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadedMembersFromConstructedInterfaces6_Api(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I<X>
{
    void {|Definition:Goo|}(X x);
}

class C : I<int>, I<string>
{
    public void {|Definition:Goo|}(int x) { }
    public void {|Definition:$$Goo|}(string x) { }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539883")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadedMembersFromConstructedInterfaces6_Feature(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I<X>
{
    void {|Definition:Goo|}(X x);
}

class C : I<int>, I<string>
{
    public void Goo(int x) { }
    public void {|Definition:$$Goo|}(string x) { }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_MultipleFiles(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_MultipleFiles_InOneFileOnly(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host, searchSingleFileOnly:=True)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpImplicitConstructor(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:$$C|}
        {
            void Goo()
            {
                new [|C|]();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpExplicitConstructor(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class {|Definition:$$C|}
        {
            public {|Definition:C|}() { }

            void Goo()
            {
                new [|C|]();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_VBImplicitConstructor(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpConstructorCallUsingNewOperator1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpConstructorCallUsingNewOperator2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_VBConstructorCallUsingNewOperator(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_VBModule(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_PartialClass(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Interface(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_GenericInterface(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539065")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Delegate(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539065")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Delegate1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539065")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Delegate2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539614")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Delegate3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    static void Main(string[] args)
    {
        myDel = new [|TestDelegate|](Goo);
    }

    private delegate void {|Definition:$$TestDelegate|}(string s);
    private static [|TestDelegate|] myDel;

    static void Goo(string arg) { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539614")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Delegate4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    static void Main(string[] args)
    {
        myDel = new [|$$TestDelegate|](Goo);
    }

    private delegate void {|Definition:TestDelegate|}(string s);
    private static [|TestDelegate|] myDel;

    static void Goo(string arg) { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539614")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Delegate5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    static void Main(string[] args)
    {
        myDel = new [|TestDelegate|](Goo);
    }

    private delegate void {|Definition:TestDelegate|}(string s);
    private static [|$$TestDelegate|] myDel;

    static void Goo(string arg) { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539646")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Delegate6(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537966")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CalledDynamic1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537966")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CalledDynamic2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538842")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CalledSystemString1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace System
        {
          public class {|Definition:St$$ring|}
          {
            void Goo(string s) { }
            void Goo([|String|] s) { }
          }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538842")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CalledSystemString2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace System
        {
          public class String
          {
            void Goo([|st$$ring|] s) { }
            void Goo(String s) { }
          }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538926")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CalledSystemString3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
namespace System
{
    public class {|Definition:String|}
    {
        void Goo([|Str$$ing|] s) { }
        void Goo(params [|String|][] s) { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539299")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_LeftSideOfMemberAccessExpression1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
public class C
{
    void Goo()
    {
        [|$$Console|].Write(0);
        [|Console|].Write(0);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539299")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_LeftSideOfMemberAccessExpression2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
public class C
{
    void Goo()
    {
        [|Console$$|].Write(0);
        [|Console|].Write(0);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539299")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_LeftSideOfMemberAccessExpression3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Public Class C
    Sub Goo()
        [|$$Console|].Write(0)
        [|Console|].Write(0)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539299")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_LeftSideOfMemberAccessExpression4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Public Class C
    Sub Goo()
        [|Console$$|].Write(0)
        [|Console|].Write(0)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCrefNamedType(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCrefNamedType2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775925")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_LeftSideOfGreaterThanTokenInAttribute(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR on primitive types"
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_PrimitiveTypeAsMethodParameter(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        namespace N
        {
            class D
            {
                void Goo([|str$$ing|] s)
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_PrimitiveTypeAsField(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        namespace N
        {
            class D
            {
                void Goo([|string|] s)
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR on value types"
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Struct(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_Enum(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_EnumMembers(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR on across projects"
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_NonDependentProjectCSharpRefsCSharp(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_NonDependentProjectVBRefsCSharp(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_InDependentProjectCSharpRefsCSharp(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_InDependentProjectVBRefsCSharp(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR in namespaces"
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_InNamespace(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR with case sensitivity"
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CaseSensitivity(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR through alias"
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_ThroughAlias(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_ThroughAliasNestedType(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_ThroughAliasGenericType(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR on object initializers"
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_ReferenceInObjectInitializers(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_ReferenceInObjectInitializersConstructorTakesNoParms(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR on collection initializers"
        <WpfTheory, CombinatorialData>
        Public Async Function TestLocal_CSharpColInitWithMultipleExpressionContainSameIdentifier(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C
        {          
            void M()
            {                    
                var col = new List<string> {[|Goo|](1), {[|Goo|](2), {[|Goo|](3), {[|Goo|](4) };
            }
            string {|Definition:$$Goo|}(int i) { return "1";}
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocal_VBColInitWithMultipleExpressionContainSameIdentifier(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C                
           sub M()
               Dim col = New List(Of string) From {[|Goo|](1), [|$$Goo|](2), [|Goo|](3), [|Goo|](4) }
           End Sub
           Function {|Definition:Goo|}(ByVal i as Integer) as string
                return "1"
           End Function
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR on array initializers"
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpArrayInitializerContainsALongExpression(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_VBArrayInitializerContainsALongExpression(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpArrayInitializerContansANestedArrayInitializer(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_VBArrayInitializerContainsANestedArrayInitializer(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpArrayInitializerDifferentTypesWithImplicitCasting(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_VBArrayInitializerDifferentTypesWithImplicitCasting(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpImplicitlyTypedArray(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_VBImplicitlyTypedArray(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR on query expressions"
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpQueryExpressionInitializedViaColInitializer(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_VBQueryExpressionInitializedViaColInitializer(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpQueryExpressionThatIncludeColInit(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_VBQueryExpressionThatIncludeColInit(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR in Venus Contexts"

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545325")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestHiddenCodeIsNotVisibleFromUI(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host, uiVisibleOnly:=True)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545325")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestHiddenCodeIsAccessibleViaApis(host As TestHost) As Task
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
            Await TestAPI(input, host, uiVisibleOnly:=False)
        End Function

#End Region

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542949")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_DoNotFindDestructor1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class {|Definition:$$A|} {    ~{|Definition:A|}()    {        Console.WriteLine("A");    }}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546229")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CrossLanguageModule(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
Public Module {|Definition:$$Goo|}
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
        [|Goo|].Bar();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestRetargetingType_Basic(kind As TestKind, host As TestHost) As Task
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
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
class Class2
{
    [|int|] x;
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestRetargetingType_GenericType(kind As TestKind, host As TestHost) As Task
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
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true">
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

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestDuplicatePublicTypeWithDuplicateConstructors(kind As TestKind, host As TestHost) As Task
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

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174256")>
        Public Async Function TestFarWithInternalsVisibleToNull(kind As TestKind, host As TestHost) As Task
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

            Await TestAPIAndFeature(input, kind, host)

        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_TypeOrNamespaceUsageInfo(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        namespace N1
        {
            using Alias1 = N2.{|TypeOrNamespaceUsageInfo.Import:[|Class1|]|};
        }

        namespace N2
        {
            public interface I<T> { }

            public class {|Definition:$$Class1|}
            {
                public static int Field;
                public class Nested { }
            }

            public class Class2 : {|TypeOrNamespaceUsageInfo.Base:[|Class1|]|}, I<{|TypeOrNamespaceUsageInfo.TypeArgument:[|Class1|]|}>
            {
                public static int M() => {|TypeOrNamespaceUsageInfo.Qualified:[|Class1|]|}.Field;
            }
        }

        namespace N2.N3
        {
            using Alias2 = N2.{|TypeOrNamespaceUsageInfo.Qualified,Import:[|Class1|]|}.Nested;

            public class Class3: {|TypeOrNamespaceUsageInfo.Qualified,Base:[|Class1|]|}.Nested, I<{|TypeOrNamespaceUsageInfo.Qualified,TypeArgument:[|Class1|]|}.Nested>
            {
                public static {|TypeOrNamespaceUsageInfo.None:[|Class1|]|} M2() => new {|TypeOrNamespaceUsageInfo.ObjectCreation:[|Class1|]|}();
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_ContainingTypeInfo(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        namespace N1
        {
            using Alias1 = N2.{|AdditionalProperty.ContainingTypeInfo.:[|Class1|]|};
        }

        namespace N2
        {
            public interface I<T> { }

            public class {|Definition:$$Class1|}
            {
                public static int Field;
                public class Nested { }
            }

            public class Class2 : {|AdditionalProperty.ContainingTypeInfo.Class2:[|Class1|]|}, I<{|AdditionalProperty.ContainingTypeInfo.Class2:[|Class1|]|}>
            {
                public static int M() => {|AdditionalProperty.ContainingTypeInfo.Class2:[|Class1|]|}.Field;
            }
        }

        namespace N2.N3
        {
            using Alias2 = N2.{|AdditionalProperty.ContainingTypeInfo.:[|Class1|]|}.Nested;

            public class Class3: {|AdditionalProperty.ContainingTypeInfo.Class3:[|Class1|]|}.Nested, I<{|AdditionalProperty.ContainingTypeInfo.Class3:[|Class1|]|}.Nested>
            {
                public static {|AdditionalProperty.ContainingTypeInfo.Class3:[|Class1|]|} M2() => new {|AdditionalProperty.ContainingTypeInfo.Class3:[|Class1|]|}();
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_ContainingMemberInfo(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        namespace N1
        {
            using Alias1 = N2.{|AdditionalProperty.ContainingMemberInfo.N1:[|Class1|]|};
        }

        namespace N2
        {
            public interface I<T> { }

            public class {|Definition:$$Class1|}
            {
                public static int Field;
                public class Nested { }
            }

            public class Class2 : {|AdditionalProperty.ContainingMemberInfo.Class2:[|Class1|]|}, I<{|AdditionalProperty.ContainingMemberInfo.Class2:[|Class1|]|}>
            {
                public static int M() => {|AdditionalProperty.ContainingMemberInfo.M:[|Class1|]|}.Field;
            }
        }

        namespace N2.N3
        {
            using Alias2 = N2.{|AdditionalProperty.ContainingMemberInfo.N3:[|Class1|]|}.Nested;

            public class Class3: {|AdditionalProperty.ContainingMemberInfo.Class3:[|Class1|]|}.Nested, I<{|AdditionalProperty.ContainingMemberInfo.Class3:[|Class1|]|}.Nested>
            {
                public static {|AdditionalProperty.ContainingMemberInfo.M2:[|Class1|]|} M2() => new {|AdditionalProperty.ContainingMemberInfo.M2:[|Class1|]|}();
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeReferenceInGlobalSuppression(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~T:N.[|C|]")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:N.[|C|].M")]

namespace N
{
    class {|Definition:$$C|}
    {
        void M()
        {
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeReferenceInGlobalSuppression_NestedType(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~T:N.C1.[|C2|]")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:N.C1.[|C2|].M")]

namespace N
{
    class C1
    {
        private class {|Definition:$$C2|}
        {
            void M()
            {
            }
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeReferenceInGlobalSuppression_GenericType(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~T:N.[|C|]`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:N.[|C|]`1.M")]

namespace N
{
    class {|Definition:$$C|}<T>
    {
        void M()
        {
        }
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/44401")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/44401")>
        <CombinatorialData>
        Public Async Function TestTypeReferenceInGlobalSuppressionParameter(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:N.D.M(N.[|C|])")]

namespace N
{
    class D
    {
        void M([|C|] c)
        {
        }
    }

    class {|Definition:$$C|}
    {
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/44401")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/44401")>
        <CombinatorialData>
        Public Async Function TestTypeReferenceInGlobalSuppressionParameter_GenericType(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:N.D.M(N.[|C|]{System.Int32})")]

namespace N
{
    class D
    {
        void M([|C|]<int> c)
        {
        }
    }

    class {|Definition:$$C|}<T>
    {
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/44401")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/44401")>
        <CombinatorialData>
        Public Async Function TestTypeReferenceInGlobalSuppressionTypeParameter_GenericType(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:N.[|D|].M(N.C{N.[|D|]})")]

namespace N
{
    class {|Definition:$$D|}
    {
        void M(C<[|D|]> c)
        {
        }
    }

    class C<T>
    {
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestNamedTypeUsedInSourceGeneratedOutput(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>class {|Definition:$$C|} { }</Document>
        <DocumentFromSourceGenerator>class D : [|C|] { }</DocumentFromSourceGenerator>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
