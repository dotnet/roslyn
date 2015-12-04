' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests

        <WorkItem(538972)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestExplicitMethod1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I
{
    void {|Definition:F$$oo|}();
}
 
class C : I
{
    void I.{|Definition:Foo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538972)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestExplicitMethod2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface I
{
    void {|Definition:Foo|}();
}
 
class C : I
{
    void I.{|Definition:F$$oo|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestExplicitMethodAndInheritance() As Task
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
            Await TestAsync(input)
        End Function
    End Class
End Namespace
