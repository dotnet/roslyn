' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Class TestStateFactory
        Public Shared Function CreateCSharpTestState(completionImplementation As CompletionImplementation,
                                                     documentElement As XElement,
                                                     Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                     Optional excludedTypes As List(Of Type) = Nothing,
                                                     Optional extraExportedTypes As List(Of Type) = Nothing,
                                                     Optional includeFormatCommandHandler As Boolean = False) As ITestState
            Select Case completionImplementation
                Case CompletionImplementation.Legacy
                    Return TestState.CreateCSharpTestState(documentElement, extraCompletionProviders, excludedTypes, extraExportedTypes, includeFormatCommandHandler)
                Case CompletionImplementation.Modern
                    Return ModernCompletionTestState.CreateCSharpTestState(documentElement, extraCompletionProviders, excludedTypes, extraExportedTypes, includeFormatCommandHandler)
            End Select

            Throw New ArgumentException(completionImplementation.ToString())
        End Function

        Public Shared Function CreateVisualBasicTestState(completionImplementation As CompletionImplementation,
                                                           documentElement As XElement,
                                                           Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                           Optional extraExportedTypes As List(Of Type) = Nothing) As ITestState
            Select Case completionImplementation
                Case CompletionImplementation.Legacy
                    Return TestState.CreateVisualBasicTestState(documentElement, extraCompletionProviders, extraExportedTypes)
                Case CompletionImplementation.Modern
                    Return ModernCompletionTestState.CreateVisualBasicTestState(documentElement, extraCompletionProviders, extraExportedTypes)
            End Select

            Throw New ArgumentException(completionImplementation.ToString())
        End Function

        Public Shared Function CreateTestStateFromWorkspace(completionImplementation As CompletionImplementation,
                                                            workspaceElement As XElement,
                                                            Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                            Optional extraExportedTypes As List(Of Type) = Nothing,
                                                            Optional workspaceKind As String = Nothing) As ITestState
            Select Case completionImplementation
                Case CompletionImplementation.Legacy
                    Return TestState.CreateTestStateFromWorkspace(workspaceElement, extraCompletionProviders, extraExportedTypes, workspaceKind)
                Case CompletionImplementation.Modern
                    Return ModernCompletionTestState.CreateTestStateFromWorkspace(workspaceElement, extraCompletionProviders, extraExportedTypes, workspaceKind)
            End Select

            Throw New ArgumentException(completionImplementation.ToString())
        End Function

        Public Shared Function GetAllCompletionImplementations() As IEnumerable(Of Object())
            Return {New Object() {CompletionImplementation.Legacy}, New Object() {CompletionImplementation.Modern}}
        End Function
    End Class
End Namespace
