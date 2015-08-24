' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.CaseCorrection
    Partial Friend Class VisualBasicCaseCorrectionService
        Inherits AbstractCaseCorrectionService

        Private Const s_threshold As Integer = 50
        Private Const s_attributeSuffix = "Attribute"

        Private ReadOnly _syntaxFactsService As ISyntaxFactsService

        Public Sub New(provider As HostLanguageServices)
            _syntaxFactsService = provider.GetService(Of ISyntaxFactsService)()
        End Sub

        Protected Overrides Sub AddReplacements(semanticModel As SemanticModel,
                                                root As SyntaxNode,
                                                spans As IEnumerable(Of TextSpan),
                                                workspace As Workspace,
                                                replacements As ConcurrentDictionary(Of SyntaxToken, SyntaxToken),
                                                cancellationToken As CancellationToken)
            For Each span In spans
                AddReplacementsWorker(semanticModel, root, span, replacements, cancellationToken)
            Next
        End Sub

        Private Sub AddReplacementsWorker(semanticModel As SemanticModel,
                                    root As SyntaxNode,
                                    span As TextSpan,
                                    replacements As ConcurrentDictionary(Of SyntaxToken, SyntaxToken),
                                    cancellationToken As CancellationToken)
            Dim candidates = root.DescendantTokens(span).Where(Function(tk As SyntaxToken) tk.Width > 0 OrElse tk.IsKind(SyntaxKind.EndOfFileToken))
            If Not candidates.Any() Then
                Return
            End If

            Dim rewriter = New Rewriter(_syntaxFactsService, semanticModel, cancellationToken)

            If span.Length <= s_threshold Then
                candidates.Do(Sub(t) Rewrite(t, rewriter, replacements))
            Else
                ' checkIdentifier is expensive. make sure we run this in parallel.
                Parallel.ForEach(candidates, Sub(t) Rewrite(t, rewriter, replacements))
            End If
        End Sub

        Private Sub Rewrite(token As SyntaxToken, rewriter As Rewriter, replacements As ConcurrentDictionary(Of SyntaxToken, SyntaxToken))
            Dim newToken = rewriter.VisitToken(token)
            If newToken <> token Then
                replacements(token) = newToken
            End If
        End Sub
    End Class
End Namespace
