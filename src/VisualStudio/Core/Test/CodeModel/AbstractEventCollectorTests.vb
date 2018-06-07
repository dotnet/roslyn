' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    <[UseExportProvider]>
    Public MustInherit Class AbstractEventCollectorTests

        Protected MustOverride ReadOnly Property LanguageName As String

        Friend Function Add(node As String, Optional parent As String = Nothing) As Action(Of CodeModelEvent, ICodeModelService)
            Return Sub(codeModelEvent, codeModelService)
                       Assert.NotNull(codeModelEvent)

                       Assert.Equal(CodeModelEventType.Add, codeModelEvent.Type)

                       CheckCodeModelEvents(codeModelEvent, codeModelService, node, parent)
                   End Sub
        End Function

        Friend Function Change(type As CodeModelEventType, node As String, Optional parent As String = Nothing) As Action(Of CodeModelEvent, ICodeModelService)
            Return Sub(codeModelEvent, codeModelService)
                       Assert.NotNull(codeModelEvent)

                       Assert.Equal(type, codeModelEvent.Type)

                       If node IsNot Nothing Then
                           Assert.NotNull(codeModelEvent.Node)
                           Assert.Equal(node, codeModelService.GetName(codeModelEvent.Node))
                       Else
                           Assert.Null(codeModelEvent.Node)
                       End If

                       If parent IsNot Nothing Then
                           Assert.NotNull(codeModelEvent.ParentNode)
                           Assert.Equal(parent, codeModelService.GetName(codeModelEvent.ParentNode))
                       End If
                   End Sub
        End Function

        Friend Function ArgChange(node As String, Optional parent As String = Nothing) As Action(Of CodeModelEvent, ICodeModelService)
            Return Change(CodeModelEventType.ArgChange, node, parent)
        End Function

        Friend Function BaseChange(node As String, Optional parent As String = Nothing) As Action(Of CodeModelEvent, ICodeModelService)
            Return Change(CodeModelEventType.BaseChange, node, parent)
        End Function

        Friend Function TypeRefChange(node As String, Optional parent As String = Nothing) As Action(Of CodeModelEvent, ICodeModelService)
            Return Change(CodeModelEventType.TypeRefChange, node, parent)
        End Function

        Friend Function Rename(node As String, Optional parent As String = Nothing) As Action(Of CodeModelEvent, ICodeModelService)
            Return Change(CodeModelEventType.Rename, node, parent)
        End Function

        Friend Function Unknown(node As String, Optional parent As String = Nothing) As Action(Of CodeModelEvent, ICodeModelService)
            Return Change(CodeModelEventType.Unknown, node, parent)
        End Function

        Friend Function Remove(node As String, parent As String) As Action(Of CodeModelEvent, ICodeModelService)
            Return Sub(codeModelEvent, codeModelService)
                       Assert.NotNull(codeModelEvent)

                       Assert.Equal(CodeModelEventType.Remove, codeModelEvent.Type)

                       CheckCodeModelEvents(codeModelEvent, codeModelService, node, parent)
                   End Sub
        End Function

        Private Sub CheckCodeModelEvents(codeModelEvent As CodeModelEvent, codeModelService As ICodeModelService, node As String, parent As String)
            If node IsNot Nothing Then
                Assert.NotNull(codeModelEvent.Node)
                Assert.Equal(node, codeModelService.GetName(codeModelEvent.Node))
            Else
                Assert.Null(codeModelEvent.Node)
            End If

            If parent IsNot Nothing Then
                Assert.NotNull(codeModelEvent.ParentNode)
                Assert.Equal(parent, codeModelService.GetName(codeModelEvent.ParentNode))
            Else
                Assert.Null(codeModelEvent.ParentNode)
            End If
        End Sub

        Friend Async Function TestAsync(code As XElement, change As XElement, ParamArray expectedEvents As Action(Of CodeModelEvent, ICodeModelService)()) As Task
            Dim definition =
<Workspace>
    <Project Language=<%= LanguageName %> CommonReferences="true">
        <Document><%= code.Value %></Document>
        <Document><%= change.Value %></Document>
    </Project>
</Workspace>

            Using workspace = TestWorkspace.Create(definition, exportProvider:=VisualStudioTestExportProvider.Factory.CreateExportProvider())
                Dim project = workspace.CurrentSolution.Projects.First()
                Dim codeModelService = project.LanguageServices.GetService(Of ICodeModelService)()
                Assert.NotNull(codeModelService)

                Dim codeDocument = workspace.CurrentSolution.GetDocument(workspace.Documents(0).Id)
                Dim codeTree = Await codeDocument.GetSyntaxTreeAsync()

                Dim changeDocument = workspace.CurrentSolution.GetDocument(workspace.Documents(1).Id)
                Dim changeTree = Await changeDocument.GetSyntaxTreeAsync()

                Dim collectedEvents = codeModelService.CollectCodeModelEvents(codeTree, changeTree)
                Assert.NotNull(collectedEvents)
                Assert.Equal(expectedEvents.Length, collectedEvents.Count)

                For Each expectedEvent In expectedEvents
                    Dim collectedEvent = collectedEvents.Dequeue()
                    expectedEvent(collectedEvent, codeModelService)
                Next
            End Using
        End Function

    End Class
End Namespace
