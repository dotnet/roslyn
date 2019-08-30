' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CompletionServiceTests_Exclusivity
        Public Const CompletionItemNonExclusive As String = "Completion Item from Non Exclusive Provider {0}"
        Public Const CompletionItemExclusive As String = "Completion Item from Exclusive Provider {0}"

        <Fact>
        Public Async Function TestExclusiveProvidersAreGroupedTogether() As Task
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
                Assert.True(list.Items.Length = 2, "Completion List does not contain exactly two items.")
                Assert.Equal(String.Format(CompletionItemExclusive, 2), list.Items.First.DisplayText)
                Assert.Equal(String.Format(CompletionItemExclusive, 3), list.Items.Last.DisplayText)
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

            Private Shared s_providers As ImmutableArray(Of CompletionProvider) = ImmutableArray.Create(Of CompletionProvider)(
                New TestCompletionProviderWithMockExclusivity(False, CompletionServiceTests_Exclusivity.CompletionItemNonExclusive, 1),
                New TestCompletionProviderWithMockExclusivity(True, CompletionServiceTests_Exclusivity.CompletionItemExclusive, 2),
                New TestCompletionProviderWithMockExclusivity(True, CompletionServiceTests_Exclusivity.CompletionItemExclusive, 3))

            Protected Overrides Function GetBuiltInProviders() As ImmutableArray(Of CompletionProvider)
                Return s_providers
            End Function
        End Class

        Private Class TestCompletionProviderWithMockExclusivity
            Inherits CompletionProvider

            Private s_isExclusive As Boolean
            Private s_itemText As String
            Private s_index As Integer

            Public Sub New(isExclusive As Boolean, text As String, index As Integer)
                s_isExclusive = isExclusive
                s_itemText = text
                s_index = index
            End Sub

            Public Overrides Function ShouldTriggerCompletion(text As SourceText, position As Int32, trigger As CompletionTrigger, options As OptionSet) As [Boolean]
                Return True
            End Function

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.IsExclusive = s_isExclusive
                context.AddItem(CompletionItem.Create(String.Format(s_itemText, s_index)))
                Return Task.CompletedTask
            End Function
        End Class
    End Class
End Namespace
