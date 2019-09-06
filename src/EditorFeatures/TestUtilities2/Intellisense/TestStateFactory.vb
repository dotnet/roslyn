' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Class TestStateFactory
        Public Shared Function CreateCSharpTestState(completionImplementation As CompletionImplementation,
                                                     documentElement As XElement,
                                                     Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                     Optional excludedTypes As List(Of Type) = Nothing,
                                                     Optional extraExportedTypes As List(Of Type) = Nothing,
                                                     Optional includeFormatCommandHandler As Boolean = False,
                                                     Optional languageVersion As CodeAnalysis.CSharp.LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.Default) As TestStateBase

            Return CreateTestState(completionImplementation,
                                   <Workspace>
                                       <Project Language="C#" CommonReferences="true" LanguageVersion=<%= DirectCast(languageVersion, Int32) %>>
                                           <Document>
                                               <%= documentElement.Value %>
                                           </Document>
                                       </Project>
                                   </Workspace>,
                                   extraCompletionProviders, excludedTypes, extraExportedTypes,
                                   includeFormatCommandHandler, workspaceKind:=Nothing)
        End Function

        Public Shared Function CreateVisualBasicTestState(completionImplementation As CompletionImplementation,
                                                           documentElement As XElement,
                                                           Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                           Optional extraExportedTypes As List(Of Type) = Nothing) As TestStateBase

            Return CreateTestState(completionImplementation,
                                   <Workspace>
                                       <Project Language="Visual Basic" CommonReferences="true">
                                           <Document>
                                               <%= documentElement.Value %>
                                           </Document>
                                       </Project>
                                   </Workspace>,
                                   extraCompletionProviders, excludedTypes:=Nothing, extraExportedTypes,
                                   includeFormatCommandHandler:=False, workspaceKind:=Nothing)
        End Function

        Public Shared Function CreateTestStateFromWorkspace(completionImplementation As CompletionImplementation,
                                                            workspaceElement As XElement,
                                                            Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                            Optional extraExportedTypes As List(Of Type) = Nothing,
                                                            Optional workspaceKind As String = Nothing) As TestStateBase

            Return CreateTestState(completionImplementation, workspaceElement, extraCompletionProviders,
                                   excludedTypes:=Nothing, extraExportedTypes, includeFormatCommandHandler:=False, workspaceKind)
        End Function

        Private Shared Function CreateTestState(completionImplementation As CompletionImplementation,
                                                workspaceElement As XElement,
                                                extraCompletionProviders As CompletionProvider(),
                                                excludedTypes As List(Of Type),
                                                extraExportedTypes As List(Of Type),
                                                includeFormatCommandHandler As Boolean,
                                                workspaceKind As String) As TestStateBase

            Select Case completionImplementation
                Case CompletionImplementation.Modern
                    Return New ModernCompletionTestState(workspaceElement, extraCompletionProviders, excludedTypes, extraExportedTypes,
                                                         includeFormatCommandHandler, workspaceKind)
            End Select

            Throw New ArgumentException(completionImplementation.ToString())
        End Function

        Public Shared Function GetAllCompletionImplementations() As IEnumerable(Of Object())
            Return {New Object() {CompletionImplementation.Modern}}
        End Function
    End Class
End Namespace
