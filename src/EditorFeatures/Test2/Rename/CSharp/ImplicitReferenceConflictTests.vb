' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    Public Class ImplicitReferenceConflictTests

        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <WorkItem(528966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoveNextCausesConflictInForEach()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class B
{
    public int Current { get; set; }
    public bool [|$$MoveNext|]() 
    {
        return false;
    }
}
 
class C
{
    static void Main()
    {
        foreach (var x in {|foreachconflict:new C()|}) { }
    }
 
    public B GetEnumerator()
    {
        return null;
    }
}

                        </Document>
                        </Project>
                    </Workspace>, renameTo:="Next")


                result.AssertLabeledSpansAre("foreachconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameDeconstructCausesConflictInDeconstructionAssignment()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void M()
    {
        {|deconstructconflict:var (y1, y2)|} = this;
    }

    public void [|$$Deconstruct|](out int x1, out int x2) { x1 = 1; x2 = 2; }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Deconstruct2")

                result.AssertLabeledSpansAre("deconstructconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameDeconstructCausesConflictInDeconstructionForEach()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void M()
    {
        foreach({|deconstructconflict:var (y1, y2)|} in new[] { this })
        {
        }
    }

    public void [|$$Deconstruct|](out int x1, out int x2) { x1 = 1; x2 = 2; }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Deconstruct2")

                result.AssertLabeledSpansAre("deconstructconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameGetAwaiterCausesConflict()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
public class C
{
    public TaskAwaiter<bool> [|Get$$Awaiter|]() => Task.FromResult(true).GetAwaiter();

    static async void M(C c)
    {
        {|awaitconflict:await|} c;
    }
}
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="GetAwaiter2")

                result.AssertLabeledSpansAre("awaitconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(528966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoveNextInVBCausesConflictInForEach()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>

                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Option Infer On

Imports System

Public Class B
    Public Property Current As Integer
    Public Function [|$$MoveNext|]() As Boolean
        Return False
    End Function
End Class

Public Class C
    Public Function GetEnumerator() As B
        Return Nothing
    End Function
End Class
                            </Document>
                        </Project>

                        <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                            <ProjectReference>Project1</ProjectReference>
                            <Document>
class D
{
    static void Main()
    {
        foreach (var x in {|foreachconflict:new C()|}) { }
    }
}

                        </Document>
                        </Project>
                    </Workspace>, renameTo:="MovNext")


                result.AssertLabeledSpansAre("foreachconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(528966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoveNextInVBToUpperCaseOnlyCausesConflictInCSForEach()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>

                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Option Infer On

Imports System

Public Class B
    Public Property Current As Integer
    Public Function [|$$MoveNext|]() As Boolean
        Return False
    End Function
End Class

Public Class C
    Public Function GetEnumerator() As B
        Return Nothing
    End Function
End Class

Public Class E
    Public Sub Goo
        for each x in new C()
        next
    End Sub
End Class
                            </Document>
                        </Project>

                        <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                            <ProjectReference>Project1</ProjectReference>
                            <Document>
class D
{
    static void Main()
    {
        foreach (var x in {|foreachconflict:new C()|}) { }
    }
}

                        </Document>
                        </Project>
                    </Workspace>, renameTo:="MoveNEXT")


                result.AssertLabeledSpansAre("foreachconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

    End Class
End Namespace
