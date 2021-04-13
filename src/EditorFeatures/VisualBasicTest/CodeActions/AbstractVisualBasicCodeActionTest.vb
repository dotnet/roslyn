' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings

    Public MustInherit Class AbstractVisualBasicCodeActionTest
        Inherits AbstractCodeActionTest

        Private ReadOnly _compilationOptions As VisualBasicCompilationOptions =
            New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True).WithParseOptions(New VisualBasicParseOptions(LanguageVersion.Latest))

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Protected Overrides Function SetParameterDefaults(parameters As TestParameters) As TestParameters
            Return parameters.WithCompilationOptions(If(parameters.compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Protected Shared Function NewLines(input As String) As String
            Return input.Replace("\n", vbCrLf)
        End Function

        Protected Overloads Async Function TestAsync(initialMarkup As XElement, expected As XElement, Optional index As Integer = 0, Optional parseOptions As ParseOptions = Nothing) As Threading.Tasks.Task
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()
            Dim expectedStr = expected.ConvertTestSourceTag()
            If parseOptions Is Nothing Then
                Await MyBase.TestAsync(initialMarkupStr, expectedStr, parseOptions:=_compilationOptions.ParseOptions, compilationOptions:=_compilationOptions, index:=index)
            Else
                Await MyBase.TestAsync(initialMarkupStr, expectedStr, parseOptions:=parseOptions, compilationOptions:=_compilationOptions, index:=index)
            End If
        End Function

        Protected Overloads Async Function TestMissingAsync(initialMarkup As XElement) As Threading.Tasks.Task
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()

            Await MyBase.TestMissingAsync(initialMarkupStr,
                New TestParameters(parseOptions:=Nothing, compilationOptions:=_compilationOptions))
        End Function

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function
    End Class
End Namespace
