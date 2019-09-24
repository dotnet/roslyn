' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Class TestStateFactory

        Private Const EditorTimeoutMs = 20000

        Public Shared Function CreateCSharpTestState(documentElement As XElement,
                                                     Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                     Optional excludedTypes As List(Of Type) = Nothing,
                                                     Optional extraExportedTypes As List(Of Type) = Nothing,
                                                     Optional includeFormatCommandHandler As Boolean = False,
                                                     Optional languageVersion As CodeAnalysis.CSharp.LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.Default) As TestState

            Return Create(<Workspace>
                              <Project Language="C#" CommonReferences="true" LanguageVersion=<%= DirectCast(languageVersion, Int32) %>>
                                  <Document>
                                      <%= documentElement.Value %>
                                  </Document>
                              </Project>
                          </Workspace>,
                                 extraCompletionProviders, excludedTypes, extraExportedTypes,
                                 includeFormatCommandHandler, workspaceKind:=Nothing)
        End Function

        Public Shared Function CreateVisualBasicTestState(documentElement As XElement,
                                                           Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                           Optional extraExportedTypes As List(Of Type) = Nothing) As TestState

            Return Create(<Workspace>
                              <Project Language="Visual Basic" CommonReferences="true">
                                  <Document>
                                      <%= documentElement.Value %>
                                  </Document>
                              </Project>
                          </Workspace>,
                                 extraCompletionProviders, excludedTypes:=Nothing, extraExportedTypes,
                                 includeFormatCommandHandler:=False, workspaceKind:=Nothing)
        End Function

        Public Shared Function CreateTestStateFromWorkspace(workspaceElement As XElement,
                                                            Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                            Optional extraExportedTypes As List(Of Type) = Nothing,
                                                            Optional workspaceKind As String = Nothing) As TestState

            Return Create(workspaceElement, extraCompletionProviders,
                                   excludedTypes:=Nothing, extraExportedTypes, includeFormatCommandHandler:=False, workspaceKind)
        End Function

        Private Shared Function Create(workspaceElement As XElement,
                       extraCompletionProviders As CompletionProvider(),
                       excludedTypes As List(Of Type),
                       extraExportedTypes As List(Of Type),
                       includeFormatCommandHandler As Boolean,
                       workspaceKind As String) As TestState

            Dim state = New TestState(workspaceElement, extraCompletionProviders,
                                   excludedTypes, extraExportedTypes, includeFormatCommandHandler, workspaceKind)

            ' The current default timeout defined in the Editor may not work on slow virtual test machines.
            ' Need to use a safe timeout there to follow real code paths.
            state.TextView.Options.GlobalOptions.SetOptionValue(DefaultOptions.ResponsiveCompletionThresholdOptionId, EditorTimeoutMs)

            Return state
        End Function
    End Class
End Namespace
