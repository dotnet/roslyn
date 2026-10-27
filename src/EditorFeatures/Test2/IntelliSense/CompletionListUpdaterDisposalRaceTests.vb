' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public NotInheritable Class CompletionListUpdaterDisposalRaceTests
        <WpfFact>
        Public Async Function EarlyReturnFromDeletionTriggerDoesNotRaceCompletionListUpdaterDisposal() As Task
            Const ItemCount = 400
            Const IterationCount = 20

            Dim documentContent As New StringBuilder()
            documentContent.AppendLine("class C")
            documentContent.AppendLine("{")
            documentContent.AppendLine("    void M()")
            documentContent.AppendLine("    {")
            For i = 0 To ItemCount - 1
                documentContent.AppendLine($"        int raceItem{i} = 0;")
            Next
            documentContent.AppendLine("        raceItem0$$;")
            documentContent.AppendLine("    }")
            documentContent.AppendLine("}")

            Using state = TestStateFactory.CreateCSharpTestState(<Document><%= documentContent.ToString() %></Document>)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                For iteration = 1 To IterationCount
                    state.SendBackspace()
                    Await state.AssertCompletionSession()

                    state.SendTypeChars("Z")

                    ' Filtering exceptions are caught by the test error handler and fail this test during cleanup.
                    state.AssertNoCompletionSessionWithNoBlock()
                    Await Task.Delay(50)

                    state.SendBackspace()
                    state.SendTypeChars("0")
                Next
            End Using
        End Function
    End Class
End Namespace
