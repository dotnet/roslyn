' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CompletionServiceTests
        <Fact>
        Public Async Function TestCompletionDoesNotCrashWhenSyntaxTreeNotPresent() As Task
            Dim workspaceDefinition =
            <Workspace>
                <Project Language="NoCompilation" AssemblyName="TestAssembly" CommonReferencesPortable="true">
                    <Document>
                        var x = {}; // e.g., TypeScript code or anything else that doesn't support compilations
                    </Document>
                </Project>
            </Workspace>
            Using workspace = TestWorkspace.Create(workspaceDefinition)
                Dim document = workspace.CurrentSolution.Projects.First.Documents.First
                Dim completionService = New TestCompletionService(workspace)

                Dim list = Await completionService.GetCompletionsAsync(
                    document, caretPosition:=0, trigger:=CompletionTrigger.Invoke)

                Assert.NotNull(list)
                Assert.NotEmpty(list.Items)
                Assert.True(list.Items.Length = 1, "Completion list contained more than one item")
                Assert.Equal("Completion Item From Test Completion Provider", list.Items.First.DisplayText)
            End Using
        End Function

        Friend Class TestCompletionService
            Inherits CompletionServiceWithProviders

            Public Sub New(workspace As Workspace)
                MyBase.New(workspace)
            End Sub

            Public Overrides ReadOnly Property Language As String
                Get
                    Return "NoCompilation"
                End Get
            End Property

            Private Shared s_providers As ImmutableArray(Of CompletionProvider) = ImmutableArray.Create(Of CompletionProvider)(New TestCompletionProvider())

            Protected Overrides Function GetBuiltInProviders() As ImmutableArray(Of CompletionProvider)
                Return s_providers
            End Function
        End Class

        Private Class TestCompletionProvider
            Inherits CompletionProvider

            Public Overrides Function ShouldTriggerCompletion(text As SourceText, position As Int32, trigger As CompletionTrigger, options As OptionSet) As [Boolean]
                Return True
            End Function

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.AddItem(CompletionItem.Create("Completion Item From Test Completion Provider"))
                Return Task.CompletedTask
            End Function
        End Class
    End Class
End Namespace
