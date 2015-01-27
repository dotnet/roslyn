' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent
    Partial Friend Class GenerateEventCodeFixProvider
        Private Class GenerateEventCodeAction
            Inherits CodeAction

            Private solution As Solution
            Private targetSymbol As INamedTypeSymbol
            Private generatedEvent As IEventSymbol
            Private codeGenerationOptions As CodeGenerationOptions
            Private codeGenService As ICodeGenerationService
            Private generatedType As INamedTypeSymbol

            Sub New(solution As Solution,
                    targetSymbol As INamedTypeSymbol,
                    generatedEvent As IEventSymbol,
                    generatedType As INamedTypeSymbol,
                    codeGenService As ICodeGenerationService,
                    codeGenerationOptions As CodeGenerationOptions)
                Me.solution = solution
                Me.targetSymbol = targetSymbol
                Me.generatedEvent = generatedEvent
                Me.generatedType = generatedType
                Me.codeGenService = codeGenService
                Me.codeGenerationOptions = codeGenerationOptions
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return String.Format(VBFeaturesResources.GeneratedeventnameTargets, generatedEvent.Name, targetSymbol.Name)
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim withEvent = Await codeGenService.AddEventAsync(solution, targetSymbol, generatedEvent, codeGenerationOptions, cancellationToken).ConfigureAwait(False)
                If generatedType IsNot Nothing Then
                    Dim compilation = Await withEvent.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
                    Dim newTargetSymbol = targetSymbol.GetSymbolKey().Resolve(compilation).Symbol
                    If newTargetSymbol.ContainingType IsNot Nothing Then
                        Return Await codeGenService.AddNamedTypeAsync(withEvent.Project.Solution, newTargetSymbol.ContainingType, generatedType, codeGenerationOptions, cancellationToken).ConfigureAwait(False)
                    ElseIf newTargetSymbol.ContainingNamespace IsNot Nothing Then
                        Return Await codeGenService.AddNamedTypeAsync(withEvent.Project.Solution, newTargetSymbol.ContainingNamespace, generatedType, codeGenerationOptions, cancellationToken).ConfigureAwait(False)
                    End If
                End If

                Return Await codeGenService.AddEventAsync(solution, targetSymbol, generatedEvent, codeGenerationOptions, cancellationToken).ConfigureAwait(False)
            End Function
        End Class
    End Class
End Namespace

