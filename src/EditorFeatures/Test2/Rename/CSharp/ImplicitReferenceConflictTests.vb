' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    Public Class ImplicitReferenceConflictTests

        <WorkItem(528966)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoveNextCausesConflictInForEach()
            Using result = RenameEngineResult.Create(
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

        <WorkItem(528966)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoveNextInVBCausesConflictInForEach()
            Using result = RenameEngineResult.Create(
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

        <WorkItem(528966)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoveNextInVBToUpperCaseOnlyCausesConflictInCSForEach()
            Using result = RenameEngineResult.Create(
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
    Public Sub Foo
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
