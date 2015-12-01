' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(538886)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_Property1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
namespace ConsoleApplication22
{
    class Program
    {
        static public int {|Definition:F$$oo|}
        {
            get
            {
                return 1;
            }
        } 
        static void Main(string[] args)
        {
            int temp = Program.[|Foo|];
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538886)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_Property2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
namespace ConsoleApplication22
{
    class Program
    {
        static public int {|Definition:Foo|}
        {
            get
            {
                return 1;
            }
        } 
        static void Main(string[] args)
        {
            int temp = Program.[|Fo$$o|];
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539022)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyCascadeThroughInterface1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    interface I
    {
        int {|Definition:$$P|} { get; }
    }
    class C : I
    {
        public int {|Definition:P|} { get; }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539022)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyCascadeThroughInterface2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    interface I
    {
        int {|Definition:P|} { get; }
    }
    class C : I
    {
        public int {|Definition:$$P|} { get; }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539047)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyThroughBase1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I1
{
    int {|Definition:$$Area|} { get; }
}
 
class C1 : I1
{
    public int {|Definition:Area|} { get { return 1; } }
}
 
class C2 : C1
{
    public int Area
    {
        get
        {
            return base.[|Area|];
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539047)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyThroughBase2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I1
{
    int {|Definition:Area|} { get; }
}
 
class C1 : I1
{
    public int {|Definition:$$Area|} { get { return 1; } }
}
 
class C2 : C1
{
    public int Area
    {
        get
        {
            return base.[|Area|];
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539047)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyThroughBase3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I1
{
    int Definition:Area { get; }
}
 
class C1 : I1
{
    public int Definition:Area { get { return 1; } }
}
 
class C2 : C1
{
    public int {|Definition:$$Area|}
    {
        get
        {
            return base.Area;
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539047)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyThroughBase4() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I1
{
    int {|Definition:Area|} { get; }
}
 
class C1 : I1
{
    public int {|Definition:Area|} { get { return 1; } }
}
 
class C2 : C1
{
    public int Area
    {
        get
        {
            return base.[|$$Area|];
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539523)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_ExplicitProperty1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public interface DD
{
    int {|Definition:$$Prop|} { get; set; }
}
public class A : DD
{
    int DD.{|Definition:Prop|}
    {
        get { return 1; }
        set { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539523)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_ExplicitProperty2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public interface DD
{
    int {|Definition:Prop|} { get; set; }
}
public class A : DD
{
    int DD.{|Definition:$$Prop|}
    {
        get { return 1; }
        set { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539885)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyFromGenericInterface1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System;
 
interface I1<T>
{
    T {|Definition:$$Name|} { get; set; }
}
 
interface I2
{
    int Name { get; set; }
}
 
interface I3<T> : I2
{
    new T {|Definition:Name|} { get; set; }
}
 
public class M<T> : I1<T>, I3<T>
{
    public T {|Definition:Name|} { get; set; }
    int I2.Name { get; set; }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539885)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyFromGenericInterface2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System;
 
interface I1<T>
{
    T Name { get; set; }
}
 
interface I2
{
    int {|Definition:$$Name|} { get; set; }
}
 
interface I3<T> : I2
{
    new T Name { get; set; }
}
 
public class M<T> : I1<T>, I3<T>
{
    public T Name { get; set; }
    int I2.{|Definition:Name|} { get; set; }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539885)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyFromGenericInterface3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System;
 
interface I1<T>
{
    T {|Definition:Name|} { get; set; }
}
 
interface I2
{
    int Name { get; set; }
}
 
interface I3<T> : I2
{
    new T {|Definition:$$Name|} { get; set; }
}
 
public class M<T> : I1<T>, I3<T>
{
    public T {|Definition:Name|} { get; set; }
    int I2.Name { get; set; }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539885)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyFromGenericInterface4() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System;
 
interface I1<T>
{
    T Name { get; set; }
}
 
interface I2
{
    int {|Definition:Name|} { get; set; }
}
 
interface I3<T> : I2
{
    new T Name { get; set; }
}
 
public class M<T> : I1<T>, I3<T>
{
    public T Name { get; set; }
    int I2.{|Definition:$$Name|} { get; set; }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539885)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_PropertyFromGenericInterface5() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System;
 
interface I1<T>
{
    T {|Definition:Name|} { get; set; }
}
 
interface I2
{
    int Name { get; set; }
}
 
interface I3<T> : I2
{
    new T {|Definition:Name|} { get; set; }
}
 
public class M<T> : I1<T>, I3<T>
{
    public T {|Definition:$$Name|} { get; set; }
    int I2.Name { get; set; }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540440)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_PropertyFunctionValue1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Module Program
    ReadOnly Property {|Definition:$$X|} As Integer ' Rename X to Y
        Get
            [|X|] = 1
        End Get
    End Property
End Module]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540440)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_PropertyFunctionValue2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Module Program
    ReadOnly Property {|Definition:X|} As Integer ' Rename X to Y
        Get
            [|$$X|] = 1
        End Get
    End Property
End Module]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(543125)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_AnonymousTypeProperties1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var a = new { $$[|{|Definition:P|}|] = 4 };
        var b = new { P = "asdf" };
        var c = new { [|P|] = 4 };
        var d = new { P = "asdf" };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(543125)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_AnonymousTypeProperties2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var a = new { [|P|] = 4 };
        var b = new { P = "asdf" };
        var c = new { $$[|{|Definition:P|}|] = 4 };
        var d = new { P = "asdf" };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(543125)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_AnonymousTypeProperties3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var a = new { P = 4 };
        var b = new { $$[|{|Definition:P|}|] = "asdf" };
        var c = new { P = 4 };
        var d = new { [|P|] = "asdf" };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(543125)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_AnonymousTypeProperties4() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var a = new { P = 4 };
        var b = new { [|P|] = "asdf" };
        var c = new { P = 4 };
        var d = new { $$[|{|Definition:P|}|] = "asdf" };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(542881)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_AnonymousTypeProperties1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim a1 = New With {Key.at = New With {.s = "hello"}}
        Dim query = From at In (From s In "1" Select s)
                    Select New With {Key {|Definition:a1|}}
        Dim hello = query.First()
        Console.WriteLine(hello.$$[|a1|].at.s)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(542881)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_AnonymousTypeProperties2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim a1 = New With {Key.[|{|Definition:at|}|] = New With {.s = "hello"}}
        Dim query = From at In (From s In "1" Select s)
                    Select New With {Key a1}
        Dim hello = query.First()
        Console.WriteLine(hello.a1.$$[|at|].s)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(542881)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_AnonymousTypeProperties3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim a1 = New With {Key.at = New With {.[|{|Definition:s|}|] = "hello"}}
        Dim query = From at In (From s In "1" Select s)
                    Select New With {Key a1}
        Dim hello = query.First()
        Console.WriteLine(hello.a1.at.$$[|s|])
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(545576)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_CascadeBetweenPropertyAndField1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Property {|Definition:$$X|}()

    Sub Foo()
        Console.WriteLine([|_X|])
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(545576)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_CascadeBetweenPropertyAndField2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Property {|Definition:X|}()

    Sub Foo()
        Console.WriteLine([|$$_X|])
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(529765)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_CascadeBetweenParameterizedVBPropertyAndCSharpMethod1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
Public Class A
    Public Overridable ReadOnly Property {|Definition:$$X|}(y As Integer) As Integer
        {|Definition:Get|}
            Return 0
        End Get
    End Property
End Class
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
class B : A
{
    public override int {|Definition:get_X|}(int y)
    {
        return base.[|get_X|](y);
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <WorkItem(529765)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_CascadeBetweenParameterizedVBPropertyAndCSharpMethod2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
Public Class A
    Public Overridable ReadOnly Property {|Definition:X|}(y As Integer) As Integer
        {|Definition:Get|}
            Return 0
        End Get
    End Property
End Class
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
class B : A
{
    public override int {|Definition:$$get_X|}(int y)
    {
        return base.[|get_X|](y);
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <WorkItem(529765)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_CascadeBetweenParameterizedVBPropertyAndCSharpMethod3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
Public Class A
    Public Overridable ReadOnly Property {|Definition:X|}(y As Integer) As Integer
        {|Definition:Get|}
            Return 0
        End Get
    End Property
End Class
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
class B : A
{
    public override int {|Definition:get_X|}(int y)
    {
        return base.[|$$get_X|](y);
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <WorkItem(665876)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_DefaultProperties() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On
Public Interface IA
    Default Property Foo(ByVal x As Integer) As Integer
End Interface
Public Interface IC
    Inherits IA
    Default Overloads Property {|Definition:$$Foo|}(ByVal x As Long) As String ' Rename Foo to Bar
End Interface

Class M
    Sub F(x As IC)
        Dim y = x[||](1L)
        Dim y2 = x(1)
    End Sub
End Class
        </Document>
        <Document>
Class M2
    Sub F(x As IC)
        Dim y = x[||](1L)
        Dim y2 = x(1)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(665876)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_DefaultProperties2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On
Public Interface IA
    Default Property {|Definition:$$Foo|}(ByVal x As Integer) As Integer
End Interface
Public Interface IC
    Inherits IA
    Default Overloads Property Foo(ByVal x As Long) As String ' Rename Foo to Bar
End Interface

Class M
    Sub F(x As IC)
        Dim y = x(1L)
        Dim y2 = x[||](1)
    End Sub
End Class
        </Document>
        <Document>
Class M2
    Sub F(x As IC)
        Dim y = x(1L)
        Dim y2 = x[||](1)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
    End Class
End Namespace
