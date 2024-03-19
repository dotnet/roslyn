' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    Public Class GenericTypeParameterTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/403671")>
        Public Sub CustomerReported_ErrorTolerance(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class A { 
void F&lt;[|$$T|]&gt;() { G&lt;{|stmt1:T|}&gt;(); } 
} 
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="U")

                result.AssertLabeledSpansAre("stmt1", "U", Microsoft.CodeAnalysis.Rename.ConflictEngine.RelatedLocationType.NoConflict)
            End Using
        End Sub
    End Class
End Namespace
