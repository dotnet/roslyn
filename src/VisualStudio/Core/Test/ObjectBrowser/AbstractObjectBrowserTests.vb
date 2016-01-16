' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.ExceptionServices
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser.Mocks

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser
    Public MustInherit Class AbstractObjectBrowserTests

        Protected MustOverride ReadOnly Property LanguageName As String

        Protected Function GetWorkspaceDefinition(code As XElement) As XElement
            Return <Workspace>
                       <Project Language=<%= LanguageName %> CommonReferences="true">
                           <Document><%= code.Value.Trim() %></Document>
                       </Project>
                   </Workspace>
        End Function

        <HandleProcessCorruptedStateExceptions()>
        Friend Async Function CreateLibraryManagerAsync(definition As XElement) As Threading.Tasks.Task(Of TestState)
            Dim workspace = Await TestWorkspace.CreateWorkspaceAsync(definition, exportProvider:=VisualStudioTestExportProvider.ExportProvider)
            Dim result As TestState = Nothing

            Try
                Dim mockComponentModel = New MockComponentModel(workspace.ExportProvider)
                mockComponentModel.ProvideService(Of VisualStudioWorkspace)(New MockVisualStudioWorkspace(workspace))
                Dim mockServiceProvider = New MockServiceProvider(mockComponentModel)
                Dim libraryManager = CreateLibraryManager(mockServiceProvider)

                result = New TestState(workspace, libraryManager)
            Finally
                If result Is Nothing Then
                    workspace.Dispose()
                End If
            End Try

            Return result
        End Function

        Friend MustOverride Function CreateLibraryManager(serviceProvider As IServiceProvider) As AbstractObjectBrowserLibraryManager

        Friend Function ProjectNode(name As String) As NavInfoNodeDescriptor
            Return New NavInfoNodeDescriptor With {.Kind = ObjectListKind.Projects, .Name = name}
        End Function

        Friend Function NamespaceNode(name As String) As NavInfoNodeDescriptor
            Return New NavInfoNodeDescriptor With {.Kind = ObjectListKind.Namespaces, .Name = name}
        End Function

        Friend Function TypeNode(name As String) As NavInfoNodeDescriptor
            Return New NavInfoNodeDescriptor With {.Kind = ObjectListKind.Types, .Name = name}
        End Function

    End Class
End Namespace
