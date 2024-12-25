' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.TestHooks
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

            Using workspace = EditorTestWorkspace.Create(workspaceDefinition, composition:=composition)
                Dim document = workspace.CurrentSolution.Projects.First.Documents.First
                Dim completionService = New TestCompletionService(workspace.Services.SolutionServices, workspace.GetService(Of IAsynchronousOperationListenerProvider)())

                Dim list = Await completionService.GetCompletionsAsync(
                    document, caretPosition:=0, CompletionOptions.Default, OptionSet.Empty, CompletionTrigger.Invoke)

                Assert.NotEmpty(list.ItemsList)
                Assert.True(list.ItemsList.Count = 1, "Completion list contained more than one item")
                Assert.Equal("Completion Item From Test Completion Provider", list.ItemsList.First.DisplayText)
            End Using
        End Function

        Friend Class TestCompletionService
            Inherits CompletionService

            Public Sub New(services As SolutionServices, listenerProvider As IAsynchronousOperationListenerProvider)
                MyBase.New(services, listenerProvider)
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

        <Fact>
        Public Async Function TestProviderForDifferentTextViewRoles() As Task
            Dim workspaceDefinition =
            <Workspace>
                <Project Language="C#" AssemblyName="TestAssembly" CommonReferencesPortable="true">
                    <Document>
$$
                </Document>
                </Project>
            </Workspace>

            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(GetType(MyRoleProvider))

            Using workspace = EditorTestWorkspace.Create(workspaceDefinition, composition:=composition)
                Dim document = workspace.CurrentSolution.Projects.First.Documents.First
                Dim completionService = document.GetRequiredLanguageService(Of CompletionService)()

                Dim list = Await completionService.GetCompletionsAsync(
                    document, caretPosition:=0, CompletionOptions.Default, OptionSet.Empty, CompletionTrigger.Invoke,
                    roles:=ImmutableHashSet.Create("MyTextViewRole"))

                Assert.True(list.ItemsList.Contains(MyRoleProvider.Item))

                Dim myRoleDescription = Await completionService.GetDescriptionAsync(document, MyRoleProvider.Item, CompletionOptions.Default, SymbolDescriptionOptions.Default)
                Assert.Equal(MyRoleProvider.DescriptionText, myRoleDescription.Text)
            End Using
        End Function

        <ExportCompletionProviderMef1(NameOf(MyRoleProvider), LanguageNames.CSharp)>
        <Microsoft.VisualStudio.Text.Editor.TextViewRole("MyTextViewRole")>
        Private Class MyRoleProvider
            Inherits CompletionProvider

            Public Shared Item As CompletionItem = CompletionItem.Create("MyRoleItem")
            Public Shared DescriptionText As String = "DescriptionForMyRole"

            <System.ComponentModel.Composition.ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Friend Overrides Function ShouldTriggerCompletion(languageServices As CodeAnalysis.Host.LanguageServices, text As SourceText, caretPosition As Integer, trigger As CompletionTrigger, options As CompletionOptions, passThroughOptions As OptionSet) As Boolean
                Return True
            End Function

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.AddItem(Item)
                Return Task.CompletedTask
            End Function

            Public Overrides Function GetDescriptionAsync(document As Document, item As CompletionItem, cancellationToken As Threading.CancellationToken) As Task(Of CompletionDescription)
                Return Task.FromResult(CompletionDescription.FromText(DescriptionText))
            End Function
        End Class
    End Class
End Namespace
