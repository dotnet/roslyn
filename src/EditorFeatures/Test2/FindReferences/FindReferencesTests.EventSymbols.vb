' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(542443, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542443")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestEvent1() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(542443, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542443")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestEvent2() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(542443, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542443")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestEvent3() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(542443, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542443")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestEvent4() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(529819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529819")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestEventCascading1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class B
    Event {|Definition:$$X|}()
    Sub Foo()
        [|XEvent|]()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(529819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529819")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestEventCascading2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class B
    Event {|Definition:X|}()
    Sub Foo()
        [|$$XEvent|]()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(553324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553324")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestEventParameterCascading() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(529804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529804")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrossAssemblyEventImplementation1() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(529804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529804")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrossAssemblyEventImplementation2() As Task
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
            Await TestAsync(input)
        End Function
    End Class
End Namespace
