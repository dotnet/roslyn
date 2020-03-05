﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.FindUsages
Imports Microsoft.CodeAnalysis.Editor.GoToBase

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToBase
    Public MustInherit Class GoToBaseTestsBase
        Protected Async Function TestAsync(workspaceDefinition As XElement, Optional shouldSucceed As Boolean = True,
                                           Optional metadataDefinitions As String() = Nothing) As Task
            Await GoToHelpers.TestAsync(
                workspaceDefinition,
                Async Function(document As Document, position As Integer, context As SimpleFindUsagesContext)
                    Dim gotoBaseService = document.GetLanguageService(Of IGoToBaseService)
                    Await gotoBaseService.FindBasesAsync(document, position, context)
                End Function,
                shouldSucceed, metadataDefinitions)
        End Function

        Protected Async Function TestAsync(source As String, language As String, Optional shouldSucceed As Boolean = True,
                                           Optional metadataDefinitions As String() = Nothing) As Task
            Await TestAsync(
                   <Workspace>
                       <Project Language=<%= language %> CommonReferences="true">
                           <Document>
                               <%= source %>
                           </Document>
                       </Project>
                   </Workspace>,
                shouldSucceed, metadataDefinitions)
        End Function
    End Class
End Namespace
