' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    Public Class ImplicitReferenceConflictTests

        <Fact>
        <WorkItem(528966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoveNextCausesConflictInForEach()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System

Class B
    Public Property Current As Integer
    Public Function [|$$MoveNext|]() As Boolean
        Return False
    End Function
End Class

Class C
    Shared Sub Main()
        For Each x In {|foreachconflict:New C()|}
        Next
    End Sub

    Public Function GetEnumerator() As B
        Return Nothing
    End Function
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="MovNext")


                result.AssertLabeledSpansAre("foreachconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(528966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoveNextToChangeCasingDoesntCauseConflictInForEach()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System

Class B
    Public Property Current As Integer
    Public Function [|$$MoveNext|]() As Boolean
        Return False
    End Function
End Class

Class C
    Shared Sub Main()
        For Each x In {|foreachconflict:New C()|}
        Next
    End Sub

    Public Function GetEnumerator() As B
        Return Nothing
    End Function
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="MOVENEXT")


            End Using
        End Sub

        <Fact>
        <WorkItem(528966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoveNextToChangeCasingInCSDoesntCauseConflictInForEach()
            Using result = RenameEngineResult.Create(
                <Workspace>

                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document>
public class B
{
    public int Current { get; set; }
    public bool [|$$MoveNext|]() 
    {
        return false;
    }
}
 
public class C
{
    public B GetEnumerator()
    {
        return null;
    }
}

                        </Document>
                    </Project>

                    <Project Language="Visual Basic" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
Option Infer On

Imports System

Class X
    Shared Sub Main()
        For Each x In {|foreachconflict:New C()|}
        Next
    End Sub
End Class
                        </Document>
                    </Project>

                </Workspace>, renameTo:="MOVENEXT")


            End Using
        End Sub

        <Fact>
        <WorkItem(528966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoveNextInCSCauseConflictInForEach()
            Using result = RenameEngineResult.Create(
                <Workspace>

                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document>
public class B
{
    public int Current { get; set; }
    public bool [|$$MoveNext|]() 
    {
        return false;
    }
}
 
public class C
{
    public B GetEnumerator()
    {
        return null;
    }
}

                        </Document>
                    </Project>

                    <Project Language="Visual Basic" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
Option Infer On

Imports System

Class X
    Shared Sub Main()
        For Each x In {|foreachconflict:New C()|}
        Next
    End Sub
End Class
                        </Document>
                    </Project>

                </Workspace>, renameTo:="Move")


                result.AssertLabeledSpansAre("foreachconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

    End Class
End Namespace
