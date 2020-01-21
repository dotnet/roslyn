' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus

    <[UseExportProvider]>
    Public MustInherit Class AbstractContainedLanguageCodeSupportTests

        Protected MustOverride ReadOnly Property Language As String
        Protected MustOverride ReadOnly Property DefaultCode As String

        Protected Sub AssertValidId(id As String)
            AssertValidId(id, Sub(value) Assert.True(value))
        End Sub

        Protected Sub AssertNotValidId(id As String)
            AssertValidId(id, Sub(value) Assert.False(value))
        End Sub

        Private Sub AssertValidId(id As String, assertion As Action(Of Boolean))
            Using workspace = TestWorkspace.Create(
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

        End Sub

        Protected Function GetWorkspace(code As String) As TestWorkspace
            Return TestWorkspace.Create(
<Workspace>
    <Project Language=<%= Language %> AssemblyName="Assembly" CommonReferences="true">
        <Document FilePath="file">
            <%= code.Replace(vbCrLf, vbLf) %>
        </Document>
    </Project>
</Workspace>, exportProvider:=VisualStudioTestExportProvider.Factory.CreateExportProvider())
        End Function

        Protected Function GetDocument(workspace As TestWorkspace) As Document
            Return workspace.CurrentSolution.Projects.Single().Documents.Single()
        End Function
    End Class
End Namespace
