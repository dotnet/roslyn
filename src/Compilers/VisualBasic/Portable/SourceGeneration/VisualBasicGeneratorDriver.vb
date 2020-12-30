' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic

    Public Class VisualBasicGeneratorDriver
        Inherits GeneratorDriver

        Private Shared ReadOnly s_parsedGeneratedSources As New ConditionalWeakTable(Of SourceText, SyntaxTree)()

        Private Sub New(state As GeneratorDriverState)
            MyBase.New(state)
        End Sub

        Friend Sub New(parseOptions As VisualBasicParseOptions, generators As ImmutableArray(Of ISourceGenerator), optionsProvider As AnalyzerConfigOptionsProvider, additionalTexts As ImmutableArray(Of AdditionalText))
            MyBase.New(parseOptions, generators, optionsProvider, additionalTexts)
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
            Dim existingTree As SyntaxTree = Nothing
            If s_parsedGeneratedSources.TryGetValue(input.Text, existingTree) _
                AndAlso Equals(_state.ParseOptions, existingTree.Options) _
                AndAlso Equals(fileName, existingTree.FilePath) Then
                Return existingTree
            End If

            Dim tree = SyntaxFactory.ParseSyntaxTree(input.Text, _state.ParseOptions, fileName, cancellationToken)

#If NETCOREAPP Then
            s_parsedGeneratedSources.AddOrUpdate(input.Text, tree)
#Else
            s_parsedGeneratedSources.Remove(input.Text)
            s_parsedGeneratedSources.Add(input.Text, tree)
#End If

            Return tree
        End Function

        Public Shared Function Create(generators As ImmutableArray(Of ISourceGenerator), Optional additionalTexts As ImmutableArray(Of AdditionalText) = Nothing, Optional parseOptions As VisualBasicParseOptions = Nothing, Optional analyzerConfigOptionsProvider As AnalyzerConfigOptionsProvider = Nothing) As VisualBasicGeneratorDriver
            Return New VisualBasicGeneratorDriver(parseOptions, generators, analyzerConfigOptionsProvider, additionalTexts)
        End Function

        Friend Overrides Function CreateSourcesCollection() As AdditionalSourcesCollection
            Return New AdditionalSourcesCollection(".vb")
        End Function

    End Class

End Namespace
