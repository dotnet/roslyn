' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef
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

            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(
                GetType(NoCompilationContentTypeDefinitions),
                GetType(NoCompilationContentTypeLanguageService),
                GetType(TestCompletionProvider))

            Using workspace = TestWorkspace.Create(workspaceDefinition, composition:=composition)
                Dim document = workspace.CurrentSolution.Projects.First.Documents.First
                Dim completionService = New TestCompletionService(workspace)

                Dim list = Await completionService.GetCompletionsAsync(
                    document, caretPosition:=0, CompletionOptions.Default, OptionValueSet.Empty, CompletionTrigger.Invoke)

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

            Friend Overrides Function GetRules(options As CompletionOptions) As CompletionRules
                Return CompletionRules.Default
            End Function
        End Class

        <ExportCompletionProvider(NameOf(TestCompletionProvider), "NoCompilation")>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class TestCompletionProvider
            Inherits CompletionProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

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
