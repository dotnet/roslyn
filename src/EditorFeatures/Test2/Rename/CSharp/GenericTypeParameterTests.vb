' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    Public Class GenericTypeParameterTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <WorkItem(403671, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/403671")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CustomerReported_ErrorTolerance()
            Using result = RenameEngineResult.Create(_outputHelper,
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
