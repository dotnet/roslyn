' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Triggers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Spellcheck

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SpellCheck), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)>
    Partial Friend Class SpellcheckCodeFixProvider
        Inherits CodeFixProvider

        ''' <summary>
        ''' Type xxx is not defined
        ''' </summary>
        Friend Const BC30002 = "BC30002"

        ''' <summary>
        ''' Error 'x' is not declared
        ''' </summary>
        Friend Const BC30451 = "BC30451"

        ''' <summary>
        ''' xxx is not a member of yyy
        ''' </summary>
        Friend Const BC30456 = "BC30456"

        ''' <summary>
        ''' 'A' has no type parameters and so cannot have type arguments.
        ''' </summary>
        Friend Const BC32045 = "BC32045"

        Private Const s_maxMatches As Integer = 3
        Private Const s_maximumEditDistancePercentage = 0.8
        Private Const s_minimumLongestCommonSubsequencePercentage = 0.2

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30002, BC30451, BC30456, BC32045)
            End Get
        End Property

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken

            Dim syntaxRoot = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim errorNode = syntaxRoot.FindNode(span)
            If errorNode.Span <> span Then
                Return
            End If

            If errorNode IsNot Nothing Then
                Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

                For Each node In errorNode.DescendantNodesAndSelf().OfType(Of SimpleNameSyntax)()
                    If Not node.IsMissing AndAlso node.Identifier.ValueText.Length >= 3 Then
                        Dim symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken)
                        If symbolInfo.Symbol Is Nothing Then
                            Dim actions = Await CreateSpellCheckCodeIssueAsync(document, node, cancellationToken).ConfigureAwait(False)

                            If actions IsNot Nothing Then
                                context.RegisterFixes(actions, context.Diagnostics)
                            End If

                            Exit For
                        End If
                    End If
                Next
            End If
        End Function

        Private Async Function CreateSpellCheckCodeIssueAsync(document As Document, identifierName As SimpleNameSyntax, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CodeAction))
            ' TODO(DustinCa): This isn't quite right. Using all default completion providers means that we might
            ' show items that don't make sense (like snippets)
            Dim completionService = document.GetLanguageService(Of ICompletionService)()
            Dim providers = completionService.GetDefaultCompletionProviders()

            Dim completionList = Await completionService.GetCompletionListAsync(document, identifierName.SpanStart, New DisplayListCompletionTrigger(), providers, cancellationToken).ConfigureAwait(False)
            If completionList Is Nothing Then
                Return Nothing
            End If

            Dim completionRules = completionService.GetCompletionRules()
            Dim onlyConsiderGenerics = TryCast(identifierName, GenericNameSyntax) IsNot Nothing

            Dim results = New List(Of SpellcheckResult)()
            Dim identifierText = identifierName.Identifier.ValueText

            For Each item In completionList.Items
                If Not item.Glyph.HasValue OrElse
                   item.Glyph.Value = Glyph.Error OrElse
                   item.Glyph.Value = Glyph.Namespace OrElse
                   (onlyConsiderGenerics AndAlso Not item.DisplayText.Contains("(Of")) Then

                    Continue For
                End If

                Dim name = GetUnescapedName(GetReasonableName(item.DisplayText))
                Dim distance = EditDistance.GetEditDistance(name, identifierText)
                Dim longestCommonSequence = EditDistance.GetLongestCommonSubsequenceLength(name, identifierText)

                ' Completion gave us the unbound identifier, so it's obviously okay. Bail.
                If distance = 0 Then
                    Return Nothing
                End If

                ' The old IDE code uses this as a metric of how well a string matches,
                ' in addition to the edit distance and longest common substring length percentages.
                Dim goodness = identifierText.Length - longestCommonSequence + distance

                ' Calculate ED and LCS% against the longer of the two strings.
                Dim maxLength = Math.Max(name.Length, identifierText.Length)

                Dim editDistancePercentage = distance / maxLength
                Dim longestCommonPercentage = longestCommonSequence / maxLength

                ' If it's within tolerances, keep it.
                If editDistancePercentage <= s_maximumEditDistancePercentage AndAlso longestCommonPercentage >= s_minimumLongestCommonSubsequencePercentage Then
                    results.Add(New SpellcheckResult(name, GetReasonableName(completionRules.GetTextChange(item).NewText), goodness))
                End If
            Next

            If Not results.Any() Then
                Return Nothing
            End If

            results.Sort(
                Function(r1, r2)
                    Dim goodnessComparisan = r1.Goodness.CompareTo(r2.Goodness)
                    If goodnessComparisan <> 0 Then
                        Return goodnessComparisan
                    End If

                    ' Try to order by name. Note that this is unescaped version of the name.
                    Dim nameCompare = String.Compare(r1.Name, r2.Name, StringComparison.CurrentCultureIgnoreCase)
                    If nameCompare <> 0 Then
                        Return nameCompare
                    End If

                    ' Prefer escaped identifiers ahead of non-escaped
                    Dim r1StartsWithEscape = r1.ReplacementText.Length > 0 AndAlso r1.ReplacementText(0) = "["c
                    Dim r2StartsWithEscape = r2.ReplacementText.Length > 0 AndAlso r2.ReplacementText(0) = "["c

                    If r1StartsWithEscape Then
                        Return -1
                    ElseIf r2StartsWithEscape
                        Return 1
                    End If

                    Return String.Compare(r1.ReplacementText, r2.ReplacementText, StringComparison.CurrentCultureIgnoreCase)
                End Function)

            Return results _
                .Select(Function(result) result.ReplacementText) _
                .Distinct() _
                .Take(s_maxMatches) _
                .Select(Function(name) New SpellCheckCodeAction(document, identifierName, name))
        End Function

        Private Structure SpellcheckResult
            Public ReadOnly Name As String
            Public ReadOnly Goodness As Integer
            Public ReadOnly ReplacementText As String

            Public Sub New(name As String, replacementText As String, goodness As Integer)
                Me.Name = name
                Me.ReplacementText = replacementText
                Me.Goodness = goodness
            End Sub

            Public Overrides Function ToString() As String
                Return $"{{{NameOf(Name)} = {Name}, {NameOf(Goodness)} = {Goodness}, {NameOf(ReplacementText)} = {ReplacementText}}}"
            End Function
        End Structure

        Private Shared Function GetReasonableName(name As String) As String
            ' Note: Because we're using completion items rather than looking up available symbols,
            ' We need to trim the display text to a valid identifier. That way, we suggest List
            ' (as Dev11 did) rather than List(Of ...). However, we don't want to remove escaping characters.

            Dim start = 0

            ' If the first character is a starting escape character, we want to skip it when processing
            ' the name for valid identifier characters.
            If name.Length > 0 AndAlso name(0) = "["c Then
                start = 1
            End If

            Dim length = name.Length
            For i = start To name.Length - 1
                If Not SyntaxFacts.IsIdentifierPartCharacter(name(i)) Then
                    length = i

                    ' If this is a closing escape character and the first character was a starting escape
                    ' character, we increase the length to ensure that the name we return includes both
                    ' escape characters.
                    If name(i) = "]"c AndAlso name(0) = "["c Then
                        length += 1
                    End If

                    Exit For
                End If
            Next

            Return name.Substring(0, length)
        End Function

        Private Shared Function GetUnescapedName(name As String) As String
            If name IsNot Nothing AndAlso name.Length > 2 AndAlso name(0) = "["c AndAlso name(name.Length - 1) = "]"c Then
                Return name.Substring(1, name.Length - 2)
            End If

            Return name
        End Function

    End Class
End Namespace
