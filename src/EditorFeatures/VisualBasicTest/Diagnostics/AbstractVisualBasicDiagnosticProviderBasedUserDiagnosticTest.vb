' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

    Public MustInherit Class AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
        Inherits AbstractDiagnosticProviderBasedUserDiagnosticTest

        Private ReadOnly _compilationOptions As CompilationOptions =
            New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True)

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Protected Overrides Function CreateWorkspaceFromFile(
            definition As String,
            parseOptions As ParseOptions,
            compilationOptions As CompilationOptions
        ) As TestWorkspace

            Return VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(
                definition,
                parseOptions,
                If(compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Protected Shared Function NewLines(input As String) As String
            Return input.Replace("\n", vbCrLf)
        End Function

        Protected Overloads Async Function TestAsync(initialMarkup As XElement, expected As XElement, Optional index As Integer = 0, Optional compareTokens As Boolean = True) As Threading.Tasks.Task
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()
            Dim expectedStr = expected.ConvertTestSourceTag()

            Await MyBase.TestAsync(initialMarkupStr, expectedStr, parseOptions:=Nothing, compilationOptions:=_compilationOptions, index:=index, compareTokens:=compareTokens)
        End Function

        Protected Overloads Async Function TestMissingWithWorkspaceXmlAsync(initialMarkup As XElement) As Threading.Tasks.Task
            Using workspace = TestWorkspaceFactory.CreateWorkspace(initialMarkup)
                Dim diagnostics = Await GetDiagnosticAndFixAsync(workspace)
                Assert.Null(diagnostics)
            End Using
        End Function

        Protected Overloads Async Function TestMissingAsync(initialMarkup As XElement) As Threading.Tasks.Task
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()

            Await MyBase.TestMissingAsync(initialMarkupStr, parseOptions:=Nothing, compilationOptions:=_compilationOptions)
        End Function

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function
    End Class
End Namespace
