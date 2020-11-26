' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(EnumCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(ObjectCreationCompletionProvider))>
    <[Shared]>
    Partial Friend Class EnumCompletionProvider
        Inherits AbstractEnumCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return text(characterPosition) = " "c OrElse
                text(characterPosition) = "("c OrElse
                (characterPosition > 1 AndAlso text(characterPosition) = "="c AndAlso text(characterPosition - 1) = ":"c) OrElse
                SyntaxFacts.IsIdentifierStartCharacter(text(characterPosition)) AndAlso
                options.GetOption(CompletionOptions.TriggerOnTypingLetters2, LanguageNames.VisualBasic)
        End Function

        Friend Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) =
            ImmutableHashSet.Create(" "c, "("c, "="c)

        Protected Overrides Function GetDefaultDisplayAndSuffixAndInsertionText(symbol As ISymbol,
                context As SyntaxContext) As (displayText As String, suffix As String, insertionText As String)
            Return GetDisplayAndSuffixAndInsertionText(symbol, context)
        End Function

        Protected Overrides Async Function CreateContextAsync(document As Document, position As Integer,
                cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Dim semanticModel = Await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(False)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function GetInsertionText(item As CompletionItem, ch As Char) As String
            Return GetInsertionTextAtInsertionTime(item, ch)
        End Function
    End Class
End Namespace
