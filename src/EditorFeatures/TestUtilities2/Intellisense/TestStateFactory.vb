﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Class TestStateFactory
        Public Shared Function CreateCSharpTestState(documentElement As XElement,
                                                     Optional excludedTypes As List(Of Type) = Nothing,
                                                     Optional extraExportedTypes As List(Of Type) = Nothing,
                                                     Optional includeFormatCommandHandler As Boolean = False,
                                                     Optional languageVersion As CodeAnalysis.CSharp.LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.Default,
                                                     Optional showCompletionInArgumentLists As Boolean = True) As TestState

            Dim testState = New TestState(<Workspace>
                                              <Project Language="C#" CommonReferences="true" LanguageVersion=<%= DirectCast(languageVersion, Integer) %>>
                                                  <Document>
                                                      <%= documentElement.Value %>
                                                  </Document>
                                              </Project>
                                          </Workspace>,
                                 excludedTypes, extraExportedTypes,
                                 includeFormatCommandHandler, workspaceKind:=Nothing)

            testState.Workspace.SetOptions(
                testState.Workspace.Options.WithChangedOption(CompletionOptions.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists))

            Return testState
        End Function

        Public Shared Function CreateVisualBasicTestState(documentElement As XElement,
                                                           Optional extraExportedTypes As List(Of Type) = Nothing) As TestState

            Return New TestState(<Workspace>
                                     <Project Language="Visual Basic" CommonReferences="true">
                                         <Document>
                                             <%= documentElement.Value %>
                                         </Document>
                                     </Project>
                                 </Workspace>,
                                 excludedTypes:=Nothing, extraExportedTypes,
                                 includeFormatCommandHandler:=False, workspaceKind:=Nothing)
        End Function

        Public Shared Function CreateTestStateFromWorkspace(workspaceElement As XElement,
                                                            Optional extraExportedTypes As List(Of Type) = Nothing,
                                                            Optional workspaceKind As String = Nothing,
                                                            Optional showCompletionInArgumentLists As Boolean = True) As TestState

            Dim testState = New TestState(
                workspaceElement, excludedTypes:=Nothing, extraExportedTypes, includeFormatCommandHandler:=False, workspaceKind)

            testState.Workspace.SetOptions(
                testState.Workspace.Options.WithChangedOption(CompletionOptions.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists))

            Return testState
        End Function
    End Class
End Namespace
