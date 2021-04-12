﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef
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

            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(
                GetType(NoCompilationContentTypeDefinitions),
                GetType(NoCompilationContentTypeLanguageService),
                GetType(CompletionItemNonExclusiveCompletionProvider),
                GetType(CompletionItemExclusiveCompletionProvider),
                GetType(CompletionItemExclusive2CompletionProvider))

            Using workspace = TestWorkspace.Create(workspaceDefinition, composition:=composition)
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
        End Class

        Private MustInherit Class TestCompletionProviderWithMockExclusivity
            Inherits CompletionProvider

            Private ReadOnly s_isExclusive As Boolean
            Private ReadOnly s_itemText As String
            Private ReadOnly s_index As Integer

            Protected Sub New(isExclusive As Boolean, text As String, index As Integer)
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

        <ExportCompletionProvider(NameOf(CompletionItemNonExclusiveCompletionProvider), "NoCompilation")>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class CompletionItemNonExclusiveCompletionProvider
            Inherits TestCompletionProviderWithMockExclusivity

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
                MyBase.New(False, CompletionServiceTests_Exclusivity.CompletionItemNonExclusive, 1)
            End Sub
        End Class

        <ExportCompletionProvider(NameOf(CompletionItemExclusiveCompletionProvider), "NoCompilation")>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class CompletionItemExclusiveCompletionProvider
            Inherits TestCompletionProviderWithMockExclusivity

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
                MyBase.New(True, CompletionServiceTests_Exclusivity.CompletionItemExclusive, 2)
            End Sub
        End Class

        <ExportCompletionProvider(NameOf(CompletionItemExclusive2CompletionProvider), "NoCompilation")>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class CompletionItemExclusive2CompletionProvider
            Inherits TestCompletionProviderWithMockExclusivity

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
                MyBase.New(True, CompletionServiceTests_Exclusivity.CompletionItemExclusive, 3)
            End Sub
        End Class
    End Class
End Namespace
