' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538972")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestExplicitMethod1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I
{
    void {|Definition:G$$oo|}();
}
 
class C : I
{
    void I.{|Definition:Goo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538972")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestExplicitMethod2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I
{
    void {|Definition:Goo|}();
}
 
class C : I
{
    void I.{|Definition:G$$oo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestExplicitMethod3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I
{
    void {|Definition:G$$oo|}() {}
}
 
class C : I
{
    void I.{|Definition:Goo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestExplicitMethod4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I
{
    void {|Definition:Goo|}() {}
}
 
class C : I
{
    void I.{|Definition:G$$oo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestExplicitMethod5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I
{
    void {|Definition:G$$oo|}();
}
 
interface C : I
{
    void I.{|Definition:Goo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestExplicitMethod6(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I
{
    void {|Definition:Goo|}();
}
 
interface C : I
{
    void I.{|Definition:G$$oo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestExplicitMethod7(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I
{
    void {|Definition:G$$oo|}() {}
}
 
interface C : I
{
    void I.{|Definition:Goo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestExplicitMethod8(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I
{
    void {|Definition:Goo|}() {}
}
 
interface C : I
{
    void I.{|Definition:G$$oo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestExplicitMethodAndInheritance(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
abstract class C
{
    public abstract void {|Definition:Boo|}();
}
interface A
{
    void Boo();
}
class B : C, A
{
   void A.Boo() { }
   public override void {|Definition:$$Boo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
