' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeCleanup.Providers
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Internal.Log
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    <Export(GetType(ICommitFormatter))>
    Friend Class CommitFormatter
        Implements ICommitFormatter

        Private Shared ReadOnly s_codeCleanupPredicate As Func(Of ICodeCleanupProvider, Boolean) =
            Function(p)
                Return p.Name <> PredefinedCodeCleanupProviderNames.Simplification AndAlso
                       p.Name <> PredefinedCodeCleanupProviderNames.Format
            End Function

        Public Sub CommitRegion(spanToFormat As SnapshotSpan,
                                isExplicitFormat As Boolean,
                                useSemantics As Boolean,
                                dirtyRegion As SnapshotSpan,
                                baseSnapshot As ITextSnapshot,
                                baseTree As SyntaxTree,
                                cancellationToken As CancellationToken) Implements ICommitFormatter.CommitRegion

            Using (Logger.LogBlock(FunctionId.LineCommit_CommitRegion, cancellationToken))
                Dim buffer = spanToFormat.Snapshot.TextBuffer
                Dim currentSnapshot = buffer.CurrentSnapshot

                ' make sure things are current
                spanToFormat = spanToFormat.TranslateTo(currentSnapshot, SpanTrackingMode.EdgeInclusive)
                dirtyRegion = dirtyRegion.TranslateTo(currentSnapshot, SpanTrackingMode.EdgeInclusive)

                Dim document = currentSnapshot.GetOpenDocumentInCurrentContextWithChanges()
                If document Is Nothing Then
                    Return
                End If

                Dim optionService = document.Project.Solution.Workspace.Services.GetService(Of IOptionService)()
                If Not (isExplicitFormat OrElse optionService.GetOption(FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic)) Then
                    Return
                End If

                Dim textSpanToFormat = spanToFormat.Span.ToTextSpan()
                If AbortForDiagnostics(document, textSpanToFormat, cancellationToken) Then
                    Return
                End If

                ' create commit formatting cleanup provider that has line commit specific behavior
                Dim commitFormattingCleanup = GetCommitFormattingCleanupProvider(
                                                document,
                                                spanToFormat,
                                                baseSnapshot, baseTree,
                                                dirtyRegion, document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken),
                                                cancellationToken)

                Dim codeCleanups = CodeCleaner.GetDefaultProviders(document).Where(s_codeCleanupPredicate).Concat(commitFormattingCleanup)

                Dim finalDocument As Document
                If useSemantics OrElse isExplicitFormat Then
                    finalDocument = CodeCleaner.CleanupAsync(document,
                                                             textSpanToFormat,
                                                             codeCleanups,
                                                             cancellationToken).WaitAndGetResult(cancellationToken)
                Else
                    Dim root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken)
                    Dim newRoot = CodeCleaner.Cleanup(root, textSpanToFormat, document.Project.Solution.Workspace, codeCleanups, cancellationToken)
                    If root Is newRoot Then
                        finalDocument = document
                    Else
                        Dim text As SourceText = Nothing
                        If newRoot.SyntaxTree IsNot Nothing AndAlso newRoot.SyntaxTree.TryGetText(text) Then
                            finalDocument = document.WithText(text)
                        Else
                            finalDocument = document.WithSyntaxRoot(newRoot)
                        End If
                    End If
                End If

                finalDocument.Project.Solution.Workspace.ApplyDocumentChanges(finalDocument, cancellationToken)
            End Using
        End Sub

        Private Function AbortForDiagnostics(document As Document, textSpanToFormat As TextSpan, cancellationToken As CancellationToken) As Boolean
            Const UnterminatedStringId = "BC30648"

            Dim tree = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken)

            ' If we have any unterminated strings that overlap what we're trying to format, then
            ' bail out.  It's quite likely the unterminated string will cause a bunch of code to
            ' swap between real code and string literals, and committing will just cause problems.
            Dim diagnostics = tree.GetDiagnostics(cancellationToken).Where(
                    Function(d) d.Descriptor.Id = UnterminatedStringId)

            Return diagnostics.Any()
        End Function

        Private Function GetCommitFormattingCleanupProvider(
            document As Document,
            spanToFormat As SnapshotSpan,
            oldSnapshot As ITextSnapshot,
            oldTree As SyntaxTree,
            newDirtySpan As SnapshotSpan,
            newTree As SyntaxTree,
            cancellationToken As CancellationToken) As ICodeCleanupProvider

            Dim oldDirtySpan = newDirtySpan.TranslateTo(oldSnapshot, SpanTrackingMode.EdgeInclusive)

            ' based on changes made to dirty spans, get right formatting rules to apply
            Dim rules = GetFormattingRules(document, spanToFormat, oldDirtySpan, oldTree, newDirtySpan, newTree, cancellationToken)

            Return New SimpleCodeCleanupProvider(PredefinedCodeCleanupProviderNames.Format,
                                                 Function(doc, spans, c) FormatAsync(doc, spans, rules, c),
                                                 Function(r, spans, w, c) Format(r, spans, w, rules, c))
        End Function

        Private Async Function FormatAsync(document As Document, spans As IEnumerable(Of TextSpan), rules As IEnumerable(Of IFormattingRule), cancellationToken As CancellationToken) As Task(Of Document)
            ' if old text already exist, use fast path for formatting
            Dim oldText As SourceText = Nothing

            If document.TryGetText(oldText) Then
                Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim newText = oldText.WithChanges(Formatter.GetFormattedTextChanges(root, spans, document.Project.Solution.Workspace, options:=Nothing, rules:=rules, cancellationToken:=cancellationToken))
                Return document.WithText(newText)
            End If

            Return Await Formatter.FormatAsync(document, spans, options:=Nothing, rules:=rules, cancellationToken:=cancellationToken).ConfigureAwait(False)
        End Function

        Private Function Format(root As SyntaxNode, spans As IEnumerable(Of TextSpan), workspace As Workspace, rules As IEnumerable(Of IFormattingRule), cancellationToken As CancellationToken) As SyntaxNode
            ' if old text already exist, use fast path for formatting
            Dim oldText As SourceText = Nothing

            If root.SyntaxTree IsNot Nothing AndAlso root.SyntaxTree.TryGetText(oldText) Then
                Dim changes = Formatter.GetFormattedTextChanges(root, spans, workspace, options:=Nothing, rules:=rules, cancellationToken:=cancellationToken)

                ' no change 
                If changes.Count = 0 Then
                    Return root
                End If

                Return root.SyntaxTree.WithChangedText(oldText.WithChanges(changes)).GetRoot(cancellationToken)
            End If

            Return Formatter.Format(root, spans, workspace, options:=Nothing, rules:=rules, cancellationToken:=cancellationToken)
        End Function

        Private Function GetFormattingRules(
            document As Document,
            spanToFormat As SnapshotSpan,
            oldDirtySpan As SnapshotSpan,
            oldTree As SyntaxTree,
            newDirtySpan As SnapshotSpan,
            newTree As SyntaxTree,
            cancellationToken As CancellationToken) As IEnumerable(Of IFormattingRule)

            ' if the span we are going to format is same as the span that got changed, don't bother to do anything special.
            ' just do full format of the span.
            If spanToFormat = newDirtySpan Then
                Return Formatter.GetDefaultFormattingRules(document)
            End If

            If oldTree Is Nothing OrElse newTree Is Nothing Then
                Return Formatter.GetDefaultFormattingRules(document)
            End If

            ' TODO: remove this in dev14
            '
            ' workaround for VB razor case.
            ' if we are under VB razor, we always use anchor operation otherwise, due to our double formatting, everything will just get messed.
            ' this is really a hacky workaround we should remove this in dev14
            Dim formattingRuleService = document.Project.Solution.Workspace.Services.GetService(Of IHostDependentFormattingRuleFactoryService)()
            If formattingRuleService IsNot Nothing Then
                If formattingRuleService.ShouldUseBaseIndentation(document) Then
                    Return Formatter.GetDefaultFormattingRules(document)
                End If
            End If

            ' when commit formatter formats given span, it formats the span with or without anchor operations.
            ' the way we determine which formatting rules are used for the span is based on whether the region user has changed would change indentation
            ' following the dirty (committed) region. if indentation has changed, we will format with anchor operations. if not, we will format without anchor operations.
            '
            ' for example, for the code below
            '[          ]|If True And
            '                   False Then|
            '                 Dim a = 1
            ' if the [] is changed, when line commit runs, it sees indentation right after the commit (|If .. Then|) is same, so formatter will run without anchor operations,
            ' meaning, "False Then" will stay as it is even if "If True And" is moved due to change in []
            ' 
            ' if the [] is changed to
            '[       If True Then
            '      ]|If True And
            '              False Then|
            '            Dim a = 1
            ' when line commit runs, it sees that indentation after the commit is changed (due to inserted "If True Then"), so formatter runs with anchor operations,
            ' meaning, "False Then" will move along with "If True And"
            '
            ' for now, do very simple checking. basically, we see whether we get same number of indent operation for the give span. alternative, but little bit
            ' more expensive and complex, we can actually calculate indentation right after the span, and see whether that is changed. not sure whether that much granularity
            ' is needed.
            If GetNumberOfIndentOperations(document, oldTree, oldDirtySpan, cancellationToken) =
               GetNumberOfIndentOperations(document, newTree, newDirtySpan, cancellationToken) Then
                Return (New NoAnchorFormatterRule()).Concat(Formatter.GetDefaultFormattingRules(document))
            End If

            Return Formatter.GetDefaultFormattingRules(document)
        End Function

        Private Function GetNumberOfIndentOperations(document As Document,
                                                     syntaxTree As SyntaxTree,
                                                     span As SnapshotSpan,
                                                     cancellationToken As CancellationToken) As Integer
            Dim vbTree = syntaxTree

            ' find containing statement of the end point, and use its end point as position to get indent operation
            Dim containingStatement = ContainingStatementInfo.GetInfo(span.End, vbTree, cancellationToken)
            Dim endPosition = If(containingStatement Is Nothing, span.End.Position + 1, containingStatement.TextSpan.End + 1)

            ' get token right after given span
            Dim token = vbTree.GetRoot(cancellationToken).FindToken(Math.Min(endPosition, syntaxTree.GetRoot(cancellationToken).FullSpan.End))

            Dim node = token.Parent
            Dim optionSet = document.Project.Solution.Workspace.Options

            ' collect all indent operation
            Dim operations = New List(Of IndentBlockOperation)()
            While node IsNot Nothing
                operations.AddRange(FormattingOperations.GetIndentBlockOperations(
                                    Formatter.GetDefaultFormattingRules(document), node, lastToken:=Nothing, optionSet:=optionSet))
                node = node.Parent
            End While

            ' get number of indent operation that affects the token. 
            Return operations.Where(Function(o) o.TextSpan.Contains(token.SpanStart)).Count()
        End Function

        Private Class NoAnchorFormatterRule
            Inherits AbstractFormattingRule

            Public Overrides Sub AddAnchorIndentationOperations(list As List(Of AnchorIndentationOperation), node As SyntaxNode, optionSet As OptionSet, nextOperation As NextAction(Of AnchorIndentationOperation))
                ' no anchor/relative formatting
                Return
            End Sub
        End Class
    End Class
End Namespace
