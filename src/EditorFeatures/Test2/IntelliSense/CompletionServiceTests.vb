' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.CSharp.Completion
Imports Microsoft.CodeAnalysis.CSharp.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class CompletionServiceTests
        <Fact>
        Public Async Function TestComplemtionDoesNotCrashWhenSyntaxTreeNotPresent() As Task
            Dim workspaceDefinition =
            <Workspace>
                <Project Language="NoCompilation" AssemblyName="TestAssembly" CommonReferencesPortable="true">
                    <Document>
                        var x = {}; // e.g., TypeScript code or anything else that doesn't support compilations
                    </Document>
                </Project>
            </Workspace>
            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Dim document = workspace.CurrentSolution.Projects.First.Documents.First
                Dim mockCompletionService = New Mock(Of AbstractCompletionService)
                mockCompletionService.Setup(Function(service) service.GetDefaultCompletionProviders()).Returns({New TestCompletionProvider()})

                Dim list = Await mockCompletionService.Object.GetCompletionListAsync(
                    document:=document,
                    position:=0,
                    triggerInfo:=CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(),
                    options:=Nothing,
                    providers:=Nothing,
                    cancellationToken:=Nothing)
                Assert.NotNull(list)
                Assert.NotEmpty(list.Items)
                Assert.True(list.Items.Length = 1, "Completion list contained more than one item")
                Assert.Equal("Completion Item From Test Completion Provider", list.Items.First.DisplayText)
            End Using
        End Function

        Private Class TestCompletionProvider
            Inherits CompletionListProvider

            Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Int32, options As OptionSet) As [Boolean]
                Return True
            End Function

            Public Overrides Function ProduceCompletionListAsync(context As CompletionListContext) As Task
                context.AddItem(New CompletionItem(Me, "Completion Item From Test Completion Provider", filterSpan:=New TextSpan(), descriptionFactory:=Nothing, glyph:=Nothing))
                Return Task.CompletedTask
            End Function
        End Class
    End Class
End Namespace