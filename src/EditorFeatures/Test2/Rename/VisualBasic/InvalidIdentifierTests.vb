' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class InvalidIdentifierTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenamingToInvalidIdentifier(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class {|Invalid:$$C|}
    Dim x as {|Invalid:C|}
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="`")

                result.AssertReplacementTextInvalid()
                result.AssertLabeledSpansAre("Invalid", "`", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenamingToInvalidIdentifier2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class {|Invalid:$$C|}
    Dim x as {|Invalid:C|}
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="C[")

                result.AssertReplacementTextInvalid()
                result.AssertLabeledSpansAre("Invalid", "C[", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545164")>
        Public Sub RenamingToUnderscoreAttribute(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
<[|A|]>
Class [|$$AAttribute|]
    Inherits System.Attribute
End Class
                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="_Attribute")

            End Using
        End Sub
    End Class
End Namespace
