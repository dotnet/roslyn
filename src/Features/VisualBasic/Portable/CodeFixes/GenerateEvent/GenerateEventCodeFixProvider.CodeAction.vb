' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Private ReadOnly _codeGenerationOptions As CodeGenerationOptions
            Private ReadOnly _codeGenService As ICodeGenerationService

            Public Sub New(solution As Solution,
                    targetSymbol As INamedTypeSymbol,
                    generatedEvent As IEventSymbol,
                    codeGenService As ICodeGenerationService,
                    codeGenerationOptions As CodeGenerationOptions)
                _solution = solution
                _targetSymbol = targetSymbol
                _generatedEvent = generatedEvent
                _codeGenService = codeGenService
                _codeGenerationOptions = codeGenerationOptions
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return String.Format(VBFeaturesResources.Create_event_0_in_1, _generatedEvent.Name, _targetSymbol.Name)
                End Get
            End Property

            Protected Overrides Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Return _codeGenService.AddEventAsync(
                    _solution, _targetSymbol, _generatedEvent,
                    _codeGenerationOptions, cancellationToken)
            End Function
        End Class
    End Class
End Namespace

