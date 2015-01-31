' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    Public Class GenericTypeParameterTests
        <WorkItem(403671)>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CustomerReported_ErrorTolerance()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class A { 
void F&lt;[|$$T|]&gt;() { G&lt;{|stmt1:T|}&gt;(); } 
} 
                            </Document>
                    </Project>
                </Workspace>, renameTo:="U")

                result.AssertLabeledSpansAre("stmt1", "U", Microsoft.CodeAnalysis.Rename.ConflictEngine.RelatedLocationType.NoConflict)
            End Using
        End Sub
    End Class
End Namespace
