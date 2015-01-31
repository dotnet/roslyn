' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus
    Public MustInherit Class AbstractContainedDocumentTests

        Protected Const HtmlMarkup As String = "<html><body><%{|S1:|}%></body></html>"

        Protected Function GetWorkspace(code As String, language As String) As TestWorkspace
            Return TestWorkspaceFactory.CreateWorkspace(
    <Workspace>
        <Project Language=<%= language %> AssemblyName="Assembly" CommonReferences="true">
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
