' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Class TestStateFactory
        Public Shared Function CreateCSharpTestState(documentElement As XElement,
                                                     Optional excludedTypes As List(Of Type) = Nothing,
                                                     Optional extraExportedTypes As List(Of Type) = Nothing,
                                                     Optional includeFormatCommandHandler As Boolean = False,
                                                     Optional languageVersion As CodeAnalysis.CSharp.LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.Default) As TestState

            Return New TestState(<Workspace>
                                     <Project Language="C#" CommonReferences="true" LanguageVersion=<%= DirectCast(languageVersion, Int32) %>>
                                         <Document>
                                             <%= documentElement.Value %>
                                         </Document>
                                     </Project>
                                 </Workspace>,
                                 excludedTypes, extraExportedTypes,
                                 includeFormatCommandHandler, workspaceKind:=Nothing)
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
                                                            Optional workspaceKind As String = Nothing) As TestState

            Return New TestState(workspaceElement,
                                   excludedTypes:=Nothing, extraExportedTypes, includeFormatCommandHandler:=False, workspaceKind)
        End Function
    End Class
End Namespace
