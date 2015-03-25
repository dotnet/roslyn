' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent
    Partial Friend Class GenerateEventCodeFixProvider
        Private Class GenerateEventCodeAction
            Inherits CodeAction

            Private _solution As Solution
            Private _targetSymbol As INamedTypeSymbol
            Private _generatedEvent As IEventSymbol
            Private _codeGenerationOptions As CodeGenerationOptions
            Private _codeGenService As ICodeGenerationService
            Private _generatedType As INamedTypeSymbol

            Public Sub New(solution As Solution,
                    targetSymbol As INamedTypeSymbol,
                    generatedEvent As IEventSymbol,
                    generatedType As INamedTypeSymbol,
                    codeGenService As ICodeGenerationService,
                    codeGenerationOptions As CodeGenerationOptions)
                Me._solution = solution
                Me._targetSymbol = targetSymbol
                Me._generatedEvent = generatedEvent
                Me._generatedType = generatedType
                Me._codeGenService = codeGenService
                Me._codeGenerationOptions = codeGenerationOptions
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return String.Format(VBFeaturesResources.GeneratedeventnameTargets, _generatedEvent.Name, _targetSymbol.Name)
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim withEvent = Await _codeGenService.AddEventAsync(_solution, _targetSymbol, _generatedEvent, _codeGenerationOptions, cancellationToken).ConfigureAwait(False)
                If _generatedType IsNot Nothing Then
                    Dim compilation = Await withEvent.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
                    Dim newTargetSymbol = _targetSymbol.GetSymbolKey().Resolve(compilation).Symbol
                    If newTargetSymbol.ContainingType IsNot Nothing Then
                        Return Await _codeGenService.AddNamedTypeAsync(withEvent.Project.Solution, newTargetSymbol.ContainingType, _generatedType, _codeGenerationOptions, cancellationToken).ConfigureAwait(False)
                    ElseIf newTargetSymbol.ContainingNamespace IsNot Nothing Then
                        Return Await _codeGenService.AddNamedTypeAsync(withEvent.Project.Solution, newTargetSymbol.ContainingNamespace, _generatedType, _codeGenerationOptions, cancellationToken).ConfigureAwait(False)
                    End If
                End If

                Return Await _codeGenService.AddEventAsync(_solution, _targetSymbol, _generatedEvent, _codeGenerationOptions, cancellationToken).ConfigureAwait(False)
            End Function
        End Class
    End Class
End Namespace

