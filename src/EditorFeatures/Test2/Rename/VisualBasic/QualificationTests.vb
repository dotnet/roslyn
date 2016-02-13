' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    Public Class QualificationTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(545576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545576")>
        Public Sub QualifyBackingField()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class X
    Shared Function _Y()
    End Function
    Class B
        Property [|$$X|]()
        Sub Foo()
            Dim y = {|stmt1:_Y|}()
        End Sub
    End Class
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Y")

                result.AssertLabeledSpansAre("stmt1", "Dim y = X._Y()", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(992721, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/992721")>
        Public Sub ConflictingLocalWithFieldWithExtensionMethodInvolved()
            Using result = RenameEngineResult.Create(
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Imports System.Collections.Generic
Imports System.Linq
Class Class1
    Private {|def:_list|} As List(Of Object)
    Public Sub Program(list As IEnumerable(Of Object))
        {|stmt2:_list|} = list.ToList()
        For Each i In {|stmt1:$$_list|}.OfType(Of Integer)()
        Next
    End Sub
End Class
                    </Document>
                </Project>
            </Workspace>, renameTo:="list")

                result.AssertLabeledSpansAre("def", "list", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt1", "For Each i In Me.list.OfType(Of Integer)()", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("stmt2", "Me.list = list.ToList()", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub
    End Class
End Namespace
