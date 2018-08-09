' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Class TestStateFactory
        Public Shared Function CreateCSharpTestState(completion As Completions,
                                                      documentElement As XElement,
                                                     Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                     Optional excludedTypes As List(Of Type) = Nothing,
                                                     Optional extraExportedTypes As List(Of Type) = Nothing,
                                                     Optional includeFormatCommandHandler As Boolean = False) As ITestState
            If completion = Completions.OldCompletion Then
                Return TestState.CreateCSharpTestState(documentElement, extraCompletionProviders, excludedTypes, extraExportedTypes, includeFormatCommandHandler)
            End If

            Throw New ArgumentException(completion.ToString())
        End Function

        Public Shared Function CreateVisualBasicTestState(completion As Completions,
                                                           documentElement As XElement,
                                                           Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                           Optional extraExportedTypes As List(Of Type) = Nothing) As ITestState
            If completion = Completions.OldCompletion Then
                Return TestState.CreateVisualBasicTestState(documentElement, extraCompletionProviders, extraExportedTypes)
            End If

            Throw New ArgumentException(completion.ToString())
        End Function

        Public Shared Function CreateTestStateFromWorkspace(completion As Completions,
                                                            workspaceElement As XElement,
                                                            Optional extraCompletionProviders As CompletionProvider() = Nothing,
                                                            Optional extraExportedTypes As List(Of Type) = Nothing,
                                                            Optional workspaceKind As String = Nothing) As TestState

            If completion = Completions.OldCompletion Then
                Return TestState.CreateTestStateFromWorkspace(workspaceElement, extraCompletionProviders, extraExportedTypes, workspaceKind)
            End If

            Throw New ArgumentException(completion.ToString())
        End Function

        Public Shared Function GetAllCompletions() As IEnumerable(Of Object())
            Return {New Object() {Completions.OldCompletion}}
        End Function
    End Class
End Namespace
