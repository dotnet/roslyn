' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

    Partial Public MustInherit Class AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
        Inherits AbstractDiagnosticProviderBasedUserDiagnosticTest

        Private ReadOnly _compilationOptions As VisualBasicCompilationOptions =
            New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True).WithParseOptions(New VisualBasicParseOptions(LanguageVersion.Latest))

        Protected Sub New(Optional logger As ITestOutputHelper = Nothing)
            MyBase.New(logger)
        End Sub

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Protected Overrides Function SetParameterDefaults(parameters As TestParameters) As TestParameters
            Return parameters.WithCompilationOptions(If(parameters.compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Friend Overloads Async Function TestAsync(
                initialMarkup As XElement, expected As XElement, Optional index As Integer = 0) As Task
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()
            Dim expectedStr = expected.ConvertTestSourceTag()

            Await MyBase.TestAsync(
                initialMarkupStr, expectedStr,
                New TestParameters(
                    parseOptions:=_compilationOptions.ParseOptions, compilationOptions:=_compilationOptions,
                    index:=index))
        End Function

        Protected Overloads Async Function TestMissingAsync(initialMarkup As XElement) As Task
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()

            Await MyBase.TestMissingAsync(initialMarkupStr, New TestParameters(parseOptions:=Nothing, compilationOptions:=_compilationOptions))
        End Function

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function

        Friend ReadOnly Property RequireArithmeticBinaryParenthesesForClarity As OptionsCollection
            Get
                Return ParenthesesOptionsProvider.RequireArithmeticBinaryParenthesesForClarity
            End Get
        End Property

        Friend ReadOnly Property RequireRelationalBinaryParenthesesForClarity As OptionsCollection
            Get
                Return ParenthesesOptionsProvider.RequireRelationalBinaryParenthesesForClarity
            End Get
        End Property

        Friend ReadOnly Property RequireOtherBinaryParenthesesForClarity As OptionsCollection
            Get
                Return ParenthesesOptionsProvider.RequireOtherBinaryParenthesesForClarity
            End Get
        End Property

        Friend ReadOnly Property IgnoreAllParentheses As OptionsCollection
            Get
                Return ParenthesesOptionsProvider.IgnoreAllParentheses
            End Get
        End Property

        Friend ReadOnly Property RemoveAllUnnecessaryParentheses As OptionsCollection
            Get
                Return ParenthesesOptionsProvider.RemoveAllUnnecessaryParentheses
            End Get
        End Property

        Friend ReadOnly Property RequireAllParenthesesForClarity As OptionsCollection
            Get
                Return ParenthesesOptionsProvider.RequireAllParenthesesForClarity
            End Get
        End Property
    End Class
End Namespace
