' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.CSharp.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(PreprocessorCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(SymbolCompletionProvider))>
    <[Shared]>
    Friend Class PreprocessorCompletionProvider
        Inherits AbstractPreprocessorCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Friend Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CommonTriggerCharsAndParen

        Protected Overrides Async Function CreateContextAsync(Workspace As Workspace, semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
