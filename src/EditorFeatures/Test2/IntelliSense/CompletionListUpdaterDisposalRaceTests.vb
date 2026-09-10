' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.ExceptionServices
Imports System.Text
Imports System.Threading
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

                Dim objectDisposedExceptionCount = 0
                Dim firstChanceHandler As EventHandler(Of FirstChanceExceptionEventArgs) =
                    Sub(sender, e)
                        If TypeOf e.Exception Is ObjectDisposedException AndAlso
                           e.Exception.StackTrace?.Contains("CompletionListUpdater") = True Then
                            Interlocked.Increment(objectDisposedExceptionCount)
                        End If
                    End Sub

                AddHandler AppDomain.CurrentDomain.FirstChanceException, firstChanceHandler
                Try
                    For iteration = 1 To IterationCount
                        state.SendBackspace()
                        Await state.AssertCompletionSession()

                        state.SendTypeChars("Z")

                        state.AssertNoCompletionSessionWithNoBlock()
                        Await Task.Delay(50)

                        state.SendBackspace()
                        state.SendTypeChars("0")
                    Next
                Finally
                    RemoveHandler AppDomain.CurrentDomain.FirstChanceException, firstChanceHandler
                End Try

                Assert.Equal(0, Volatile.Read(objectDisposedExceptionCount))
            End Using
        End Function
    End Class
End Namespace
