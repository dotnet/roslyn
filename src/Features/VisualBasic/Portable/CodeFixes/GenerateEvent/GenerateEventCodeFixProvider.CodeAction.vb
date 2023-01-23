' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent
    Partial Friend Class GenerateEventCodeFixProvider
        Private Class GenerateEventCodeAction
            Inherits CodeAction

            Private ReadOnly _solution As Solution
            Private ReadOnly _targetSymbol As INamedTypeSymbol
            Private ReadOnly _generatedEvent As IEventSymbol
            Private ReadOnly _codeGenService As ICodeGenerationService
            Private ReadOnly _fallbackOptions As CodeAndImportGenerationOptionsProvider

            Public Sub New(solution As Solution,
                    targetSymbol As INamedTypeSymbol,
                    generatedEvent As IEventSymbol,
                    codeGenService As ICodeGenerationService,
                    fallbackOptions As CodeAndImportGenerationOptionsProvider)
                _solution = solution
                _targetSymbol = targetSymbol
                _generatedEvent = generatedEvent
                _codeGenService = codeGenService
                _fallbackOptions = fallbackOptions
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return String.Format(VBFeaturesResources.Create_event_0_in_1, _generatedEvent.Name, _targetSymbol.Name)
                End Get
            End Property

            Protected Overrides Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Return _codeGenService.AddEventAsync(
                    New CodeGenerationSolutionContext(
                        _solution,
                        CodeGenerationContext.Default,
                        _fallbackOptions),
                    _targetSymbol,
                    _generatedEvent,
                    cancellationToken)
            End Function
        End Class
    End Class
End Namespace

