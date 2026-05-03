' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class QualificationTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545576")>
        Public Sub QualifyBackingField(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class X
    Shared Function _Y()
    End Function
    Class B
        Property [|$$X|]()
        Sub Goo()
            Dim y = {|stmt1:_Y|}()
        End Sub
    End Class
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpansAre("stmt1", "Dim y = X._Y()", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/992721")>
        Public Sub ConflictingLocalWithFieldWithExtensionMethodInvolved(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
            </Workspace>, host:=host, renameTo:="list")

                result.AssertLabeledSpansAre("def", "list", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt1", "For Each i In Me.list.OfType(Of Integer)()", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("stmt2", "Me.list = list.ToList()", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub
    End Class
End Namespace
