' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.FindUsages
Imports Microsoft.CodeAnalysis.Editor.GoToBase

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToBase
    Public MustInherit Class GoToBaseTestsBase
        Protected Async Function TestAsync(workspaceDefinition As XElement, Optional shouldSucceed As Boolean = True) As Task
            Await GoToHelpers.TestAsync(
                workspaceDefinition,
                Async Function(document As Document, position As Integer, context As SimpleFindUsagesContext)
                    Dim gotoBaseService = document.GetLanguageService(Of IGoToBaseService)
                    Await gotoBaseService.FindBasesAsync(document, position, context)
                End Function,
                shouldSucceed)
        End Function

        Protected Async Function TestAsync(source As String, language As String, Optional shouldSucceed As Boolean = True) As Task
            Await TestAsync(
                   <Workspace>
                       <Project Language=<%= language %> CommonReferences="true">
                           <Document>
                               <%= source %>
                           </Document>
                       </Project>
                   </Workspace>,
                shouldSucceed)
        End Function
    End Class
End Namespace
