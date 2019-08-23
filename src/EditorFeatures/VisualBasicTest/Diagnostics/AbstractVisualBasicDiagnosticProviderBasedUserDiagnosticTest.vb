' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.CodeActions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

    Public MustInherit Class AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
        Inherits AbstractDiagnosticProviderBasedUserDiagnosticTest

        Private ReadOnly _compilationOptions As VisualBasicCompilationOptions =
            New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True).WithParseOptions(New VisualBasicParseOptions(LanguageVersion.Latest))

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Protected Overrides Function CreateWorkspaceFromFile(initialMarkup As String, parameters As TestParameters) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(
                initialMarkup,
                parameters.parseOptions,
                If(parameters.compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Friend Overloads Async Function TestAsync(
                initialMarkup As XElement, expected As XElement, Optional index As Integer = 0,
                Optional priority As CodeActionPriority? = Nothing) As Threading.Tasks.Task
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()
            Dim expectedStr = expected.ConvertTestSourceTag()

            Await MyBase.TestAsync(initialMarkupStr, expectedStr,
                                   parseOptions:=_compilationOptions.ParseOptions, compilationOptions:=_compilationOptions,
                                   index:=index,
                                   priority:=priority)
        End Function

        Protected Overloads Async Function TestMissingAsync(initialMarkup As XElement) As Threading.Tasks.Task
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()

            Await MyBase.TestMissingAsync(initialMarkupStr, New TestParameters(parseOptions:=Nothing, compilationOptions:=_compilationOptions))
        End Function

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function
    End Class
End Namespace
