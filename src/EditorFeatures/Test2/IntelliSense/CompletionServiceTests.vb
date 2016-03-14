' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.CSharp.Completion
Imports Microsoft.CodeAnalysis.CSharp.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text

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
                Dim completionService As AbstractCompletionService = New CSharpCompletionService()
                Dim list = Await completionService.GetCompletionListAsync(
                    document:=document,
                    position:=0,
                    triggerInfo:=CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(),
                    options:=Nothing,
                    providers:={New TestCompletionProvider()},
                    cancellationToken:=Nothing)
                Assert.Null(list)
            End Using
        End Function

        Private Class TestCompletionProvider
            Inherits CompletionListProvider
            Public Sub New()
            End Sub

            Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Int32, options As OptionSet) As [Boolean]
                Return True
            End Function

            Public Overrides Function ProduceCompletionListAsync(context As CompletionListContext) As Task
                Return Task.CompletedTask
            End Function
        End Class
    End Class
End Namespace