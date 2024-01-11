' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
    <UseExportProvider>
    Public Class WpfTextBufferVisibilityTrackerTests
        <WpfFact>
        Public Sub TestMutationInCallback()
            Using workspace = EditorTestWorkspace.CreateCSharp("", composition:=EditorTestCompositions.EditorFeaturesWpf)
                Dim visibilityTracker = DirectCast(workspace.ExportProvider.GetExportedValue(Of ITextBufferVisibilityTracker), WpfTextBufferVisibilityTracker)

                Dim buffer = workspace.Documents.Single().GetTextBuffer()

                Dim action1Called = False
                Dim action2Called = False

                Dim action1 As Action = Nothing
                action1 = Sub()
                              action1Called = True
                              visibilityTracker.UnregisterForVisibilityChanges(buffer, action1)
                          End Sub

                Dim action2 = Sub()
                                  action2Called = True
                              End Sub

                visibilityTracker.RegisterForVisibilityChanges(buffer, action1)
                visibilityTracker.RegisterForVisibilityChanges(buffer, action2)

                ' Getting the text view will cause it to become visible which will iterate and invoke the actions.  The first
                ' action callback will then unregister itself.  This should must not break things, and we should still call action2.
                Dim view = workspace.Documents.Single().GetTextView()

                visibilityTracker.GetTestAccessor().TriggerCallbacks(buffer)

                Assert.True(action1Called)
                Assert.True(action2Called)
            End Using
        End Sub
    End Class
End Namespace
