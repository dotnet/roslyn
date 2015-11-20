' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus

    Public MustInherit Class AbstractContainedLanguageCodeSupportTests

        Protected MustOverride ReadOnly Property Language As String
        Protected MustOverride ReadOnly Property DefaultCode As String

        Protected Function AssertValidIdAsync(id As String) As Threading.Tasks.Task
            Return AssertValidIdAsync(id, Sub(value) Assert.True(value))
        End Function

        Protected Function AssertNotValidIdAsync(id As String) As Threading.Tasks.Task
            Return AssertValidIdAsync(id, Sub(value) Assert.False(value))
        End Function

        Private Async Function AssertValidIdAsync(id As String, assertion As Action(Of Boolean)) As Threading.Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(
<Workspace>
    <Project Language=<%= Language %> AssemblyName="Assembly" CommonReferences="true">
        <Document>
            <%= DefaultCode %>
        </Document>
    </Project>
</Workspace>)
                Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                assertion(ContainedLanguageCodeSupport.IsValidId(document, id))
            End Using

        End Function

        Protected Function GetWorkspaceAsync(code As String) As Threading.Tasks.Task(Of TestWorkspace)
            Return TestWorkspaceFactory.CreateWorkspaceAsync(
<Workspace>
    <Project Language=<%= Language %> AssemblyName="Assembly" CommonReferences="true">
        <Document FilePath="file">
            <%= code.Replace(vbCrLf, vbLf) %>
        </Document>
    </Project>
</Workspace>, exportProvider:=VisualStudioTestExportProvider.ExportProvider)
        End Function

        Protected Function GetDocument(workspace As TestWorkspace) As Document
            Return workspace.CurrentSolution.Projects.Single().Documents.Single()
        End Function
    End Class
End Namespace