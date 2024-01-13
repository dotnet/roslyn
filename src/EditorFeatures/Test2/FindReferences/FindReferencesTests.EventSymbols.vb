' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542443")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestEvent1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
public delegate void MyDelegate();   // delegate declaration
public interface I { event MyDelegate {|Definition:$$MyEvent|}; void FireAway(); }
public class MyClass : I
{
    public event MyDelegate {|Definition:MyEvent|};
    public void FireAway()
    {
        if ([|MyEvent|] != null)
            [|MyEvent|]();
    }
}
public class MainClass
{
    static private void f()
    { Console.WriteLine("This is called when the event fires."); }
    static public void Main()
    {
        I i = new MyClass();
        i.[|MyEvent|] += new MyDelegate(f); i.FireAway();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542443")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestEvent2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
public delegate void MyDelegate();   // delegate declaration
public interface I { event MyDelegate {|Definition:MyEvent|}; void FireAway(); }
public class MyClass : I
{
    public event MyDelegate {|Definition:$$MyEvent|};
    public void FireAway()
    {
        if ([|MyEvent|] != null)
            [|MyEvent|]();
    }
}
public class MainClass
{
    static private void f()
    { Console.WriteLine("This is called when the event fires."); }
    static public void Main()
    {
        I i = new MyClass();
        i.[|MyEvent|] += new MyDelegate(f); i.FireAway();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542443")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestEvent3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
public delegate void MyDelegate();   // delegate declaration
public interface I { event MyDelegate {|Definition:MyEvent|}; void FireAway(); }
public class MyClass : I
{
    public event MyDelegate {|Definition:MyEvent|};
    public void FireAway()
    {
        if ([|$$MyEvent|] != null)
            [|MyEvent|]();
    }
}
public class MainClass
{
    static private void f()
    { Console.WriteLine("This is called when the event fires."); }
    static public void Main()
    {
        I i = new MyClass();
        i.[|MyEvent|] += new MyDelegate(f); i.FireAway();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542443")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestEvent4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
public delegate void MyDelegate();   // delegate declaration
public interface I { event MyDelegate {|Definition:MyEvent|}; void FireAway(); }
public class MyClass : I
{
    public event MyDelegate {|Definition:MyEvent|};
    public void FireAway()
    {
        if ([|MyEvent|] != null)
            [|MyEvent|]();
    }
}
public class MainClass
{
    static private void f()
    { Console.WriteLine("This is called when the event fires."); }
    static public void Main()
    {
        I i = new MyClass();
        i.[|$$MyEvent|] += new MyDelegate(f); i.FireAway();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529819")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestEventCascading1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class B
    Event {|Definition:$$X|}()
    Sub Goo()
        [|XEvent|]()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529819")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestEventCascading2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class B
    Event {|Definition:X|}()
    Sub Goo()
        [|$$XEvent|]()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/14428")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553324")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestEventParameterCascading1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Event E({|Definition:$$x|} As Object)
    Sub Main()
        Dim e As EEventHandler
        e.BeginInvoke([|x|]:=Nothing, DelegateCallback:=Nothing, DelegateAsyncState:=Nothing)
        e.Invoke([|x|]:=Nothing)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/14428")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553324")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestEventParameterCascading2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Event E({|Definition:x|} As Object)
    Sub Main()
        Dim e As EEventHandler
        e.BeginInvoke([|$$x|]:=Nothing, DelegateCallback:=Nothing, DelegateAsyncState:=Nothing)
        e.Invoke([|x|]:=Nothing)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/14428")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553324")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestEventParameterCascading3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Event E({|Definition:x|} As Object)
    Sub Main()
        Dim e As EEventHandler
        e.BeginInvoke([|x|]:=Nothing, DelegateCallback:=Nothing, DelegateAsyncState:=Nothing)
        e.Invoke([|$$x|]:=Nothing)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529804")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCrossAssemblyEventImplementation1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
Imports System

Public Interface I
    Event {|Definition:$$X|} As EventHandler
End Interface
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
using System;
 
class C : I
{
    event EventHandler I.{|Definition:X|}
    {
        add { }
        remove { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529804")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCrossAssemblyEventImplementation2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
using System;
 
class C : I
{
    event EventHandler I.{|Definition:$$X|}
    {
        add { }
        remove { }
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
Imports System

Public Interface I
    Event {|Definition:X|} As EventHandler
End Interface
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestStaticAbstractEventInInterface(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
interface I3
{
    abstract static event System.Action {|Definition:E$$3|};
}

class C3_1 : I3
{
    public static event System.Action {|Definition:E3|};
}

class C3_2 : I3
{
    static event System.Action I3.{|Definition:E3|}
    {
        add { }
        remove { }
    }
}        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestStaticAbstractEventViaFeature1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
interface I3
{
    abstract static event System.Action {|Definition:E3|};
}

class C3_1 : I3
{
    public static event System.Action {|Definition:E$$3|};
}

class C3_2 : I3
{
    static event System.Action I3.E3
    {
        add { }
        remove { }
    }
}        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestStaticAbstractEventViaFeature2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
interface I3
{
    abstract static event System.Action {|Definition:E3|};
}

class C3_1 : I3
{
    public static event System.Action E3;
}

class C3_2 : I3
{
    static event System.Action I3.{|Definition:E$$3|}
    {
        add { }
        remove { }
    }
}        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestStaticAbstractEventViaAPI1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
interface I3
{
    abstract static event System.Action {|Definition:E3|};
}

class C3_1 : I3
{
    public static event System.Action {|Definition:E3|};
}

class C3_2 : I3
{
    static event System.Action I3.{|Definition:E$$3|}
    {
        add { }
        remove { }
    }
}        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestStaticAbstractEventViaAPI2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
interface I3
{
    abstract static event System.Action {|Definition:E3|};
}

class C3_1 : I3
{
    public static event System.Action {|Definition:E$$3|};
}

class C3_2 : I3
{
    static event System.Action I3.{|Definition:E3|}
    {
        add { }
        remove { }
    }
}        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function
    End Class
End Namespace
