' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class ImplicitReferenceConflictTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <CombinatorialData>
        Public Sub RenameMoveNextCausesConflictInForEach(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="MovNext")

                result.AssertLabeledSpansAre("foreachconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <CombinatorialData>
        Public Sub RenameMoveNextToChangeCasingDoesntCauseConflictInForEach(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="MOVENEXT")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <CombinatorialData>
        Public Sub RenameMoveNextToChangeCasingInCSDoesntCauseConflictInForEach(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="MOVENEXT")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528966")>
        <CombinatorialData>
        Public Sub RenameMoveNextInCSCauseConflictInForEach(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="Move")

                result.AssertLabeledSpansAre("foreachconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

    End Class
End Namespace
