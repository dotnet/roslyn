' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    Public Class InvalidIdentifierTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingToInvalidIdentifier()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class {|Invalid:$$C|}
    Dim x as {|Invalid:C|}
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="`")

                result.AssertReplacementTextInvalid()
                result.AssertLabeledSpansAre("Invalid", "`", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingToInvalidIdentifier2()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class {|Invalid:$$C|}
    Dim x as {|Invalid:C|}
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="C[")

                result.AssertReplacementTextInvalid()
                result.AssertLabeledSpansAre("Invalid", "C[", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename), WorkItem(545164)>
        Public Sub RenamingToUnderscoreAttribute()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
<[|A|]>
Class [|$$AAttribute|]
    Inherits System.Attribute
End Class
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="_Attribute")


            End Using
        End Sub
    End Class
End Namespace
