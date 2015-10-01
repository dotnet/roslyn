Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class BooleanCompletionProvider
        Inherits CompletionListProvider

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Public Overrides Async Function ProduceCompletionListAsync(context As CompletionListContext) As Task
            Dim typeInferenceService = context.Document.Project.LanguageServices.GetService(Of ITypeInferenceService)()
            Dim semanticModel = Await context.Document.GetSemanticModelForSpanAsync(New TextSpan(context.Position, 0), context.CancellationToken).ConfigureAwait(False)
            Dim inferredType = typeInferenceService.InferType(semanticModel, context.Position, objectAsDefault:=False, cancellationToken:=context.CancellationToken)

            If inferredType IsNot Nothing AndAlso inferredType.SpecialType = SpecialType.System_Boolean Then
                Dim text = Await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(False)
                Dim textChangeSpan = CompletionUtilities.GetTextChangeSpan(text, context.Position)

                Dim trueItem = New CompletionItem(Me, "True", textChangeSpan, glyph:=Glyph.Keyword)
                trueItem.AddTag("Boolean")

                Dim falseItem = New CompletionItem(Me, "False", textChangeSpan, glyph:=Glyph.Keyword)
                falseItem.AddTag("Boolean")

                context.AddItem(trueItem)
                context.AddItem(falseItem)
            End If

        End Function
    End Class
End Namespace
