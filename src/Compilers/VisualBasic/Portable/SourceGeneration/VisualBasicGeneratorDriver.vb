﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic

    Public Class VisualBasicGeneratorDriver
        Inherits GeneratorDriver

        Private Sub New(state As GeneratorDriverState)
            MyBase.New(state)
        End Sub

        Friend Sub New(parseOptions As VisualBasicParseOptions, generators As ImmutableArray(Of ISourceGenerator), optionsProvider As AnalyzerConfigOptionsProvider, additionalTexts As ImmutableArray(Of AdditionalText))
            MyBase.New(parseOptions, generators, optionsProvider, additionalTexts, enableIncremental:=False)
        End Sub

        Friend Overrides ReadOnly Property MessageProvider As CommonMessageProvider
            Get
                Return VisualBasic.MessageProvider.Instance
            End Get
        End Property

        Friend Overrides Function FromState(state As GeneratorDriverState) As GeneratorDriver
            Return New VisualBasicGeneratorDriver(state)
        End Function

        Friend Overrides Function ParseGeneratedSourceText(input As GeneratedSourceText, fileName As String, cancellationToken As CancellationToken) As SyntaxTree
            Return VisualBasicSyntaxTree.ParseTextLazy(input.Text, CType(_state.ParseOptions, VisualBasicParseOptions), fileName)
        End Function

        Public Shared Function Create(generators As ImmutableArray(Of ISourceGenerator), Optional additionalTexts As ImmutableArray(Of AdditionalText) = Nothing, Optional parseOptions As VisualBasicParseOptions = Nothing, Optional analyzerConfigOptionsProvider As AnalyzerConfigOptionsProvider = Nothing) As VisualBasicGeneratorDriver
            Return New VisualBasicGeneratorDriver(parseOptions, generators, If(analyzerConfigOptionsProvider, CompilerAnalyzerConfigOptionsProvider.Empty), additionalTexts.NullToEmpty())
        End Function

        Friend Overrides Function CreateSourcesCollection() As AdditionalSourcesCollection
            Return New AdditionalSourcesCollection(".vb")
        End Function

    End Class

End Namespace
