' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Composition

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
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim token = root.FindToken(span.Start)
            If Not token.Span.IntersectsWith(span) Then
                Return
            End If

            Dim errorSyntax = token.GetAncestors(Of SyntaxNode)() _
                              .FirstOrDefault(Function(c) c.Span = span)

            If errorSyntax IsNot Nothing Then
                Dim semanticModel = DirectCast(Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False), SemanticModel)
                For Each node In errorSyntax.DescendantNodesAndSelf().OfType(Of SimpleNameSyntax)()
                    If node.IsMissing OrElse node.Identifier.ValueText.Length < 3 Then
                        Continue For
                    End If

                    Dim symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken)
                    If symbolInfo.Symbol IsNot Nothing Then
                        Continue For
                    End If

                    Dim actions = Await CreateSpellCheckCodeIssueAsync(document, node, cancellationToken).ConfigureAwait(False)

                    If actions IsNot Nothing Then
                        context.RegisterFixes(actions, context.Diagnostics)
                    End If
                    Return
                Next
            End If
        End Function

        Private Async Function CreateSpellCheckCodeIssueAsync(document As Document, identifierName As SimpleNameSyntax, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CodeAction))
            ' TODO(DustinCa): This isn't quite right. Using all default completion providers means that we might
            ' show items that don't make sense (like snippets)
            Dim completionService = document.GetLanguageService(Of ICompletionService)()
            Dim providers = completionService.GetDefaultCompletionProviders()

            Dim groups = Await completionService.GetGroupsAsync(document, identifierName.SpanStart, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo, providers, cancellationToken).ConfigureAwait(False)
            If groups Is Nothing Then
                Return Nothing
            End If

            ' We may encounter a case where we would like to suggest an identifier that could be escaped,
            ' like if the user types "intege" when Integer and [Integer] are both available. To handle this case,
            ' group completion items by their unescaped display text and use that text in the 
            ' edit distance algorithm.
            Dim onlyConsiderGenerics = TryCast(identifierName, GenericNameSyntax) IsNot Nothing
            Dim items = groups _
                .SelectMany(Function(g) g.Items) _
                .Where(Function(i)
                           Return i.Glyph.HasValue AndAlso
                           i.Glyph.Value <> Glyph.Error AndAlso
                           i.Glyph.Value <> Glyph.Namespace AndAlso
                               (Not onlyConsiderGenerics OrElse i.DisplayText.Contains("(Of"))
                       End Function) _
                .GroupBy(Function(i) GetUnescapedName(GetReasonableName(i.DisplayText)))

            Dim results = New List(Of SpellcheckResult)()

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)()

            Dim identifierText = identifierName.Identifier.ValueText

            For Each item In items
                Dim name = item.Key
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
                    results.Add(New SpellcheckResult(name, item, editDistancePercentage, longestCommonPercentage, goodness, complexify:=True))
                End If
            Next

            If Not results.Any() Then
                Return Nothing
            End If

            Dim sortedSymbols = results.OrderBy(Function(r) r.Goodness)

            Dim namesToSuggest = sortedSymbols _
                .Take(s_maxMatches) _
                .SelectMany(Function(res) res.Item) _
                .Select(Function(i) GetReasonableName(i.CompletionProvider.GetTextChange(i).NewText)) _
                .Distinct() _
                .Take(s_maxMatches)

            Return namesToSuggest.Select(
                Function(n)
                    Return New SpellCheckCodeAction(
                        document,
                        identifierName,
                        n,
                        ShouldComplexify(n, semanticModel, identifierName.SpanStart))
                End Function)
        End Function

        Private Function GetReasonableName(name As String) As String
            ' Note: Because we're using completion items rather than looking up available symbols,
            ' We need to trim the display text to a valid identifier. That way, we suggest List
            ' (as Dev11 did) rather than List(Of ...).
            Dim length = name.Length
            For i = 0 To name.Length - 1
                If i = 0 AndAlso name(i) = "["c Then
                    Continue For
                End If

                If Not SyntaxFacts.IsIdentifierPartCharacter(name(i)) Then
                    length = i

                    If name(i) = "]"c AndAlso i > 0 AndAlso name(0) = "["c Then
                        length += 1
                    End If

                    Exit For
                End If
            Next

            Return name.Substring(0, length)
        End Function

        Private Function GetUnescapedName(name As String) As String
            Return If(name IsNot Nothing AndAlso name.Length > 2 AndAlso name(0) = "["c AndAlso name(name.Length - 1) = "]"c,
                      name.Substring(1, name.Length - 2),
                      name)
        End Function

        Private Structure SpellcheckResult
            Public ReadOnly Name As String
            Public ReadOnly EditDistancePercentage As Double
            Public ReadOnly LongestCommonPercentage As Double
            Public ReadOnly Goodness As Integer
            Public ReadOnly Complexify As Boolean
            Public ReadOnly Item As IGrouping(Of String, CompletionItem)

            Public Sub New(name As String, item As IGrouping(Of String, CompletionItem), editDistancePercentage As Double, longestCommonPercentage As Double, goodness As Integer, complexify As Boolean)
                Me.Name = name
                Me.Item = item
                Me.EditDistancePercentage = editDistancePercentage
                Me.LongestCommonPercentage = longestCommonPercentage
                Me.Goodness = goodness
                Me.Complexify = complexify
            End Sub
        End Structure

        Private Function ShouldComplexify(item As String, semanticModel As SemanticModel, position As Integer) As Boolean
            ' If it's not a predefined type name, we should try to complexify
            Dim type = semanticModel.GetSpeculativeTypeInfo(position, SyntaxFactory.ParseExpression(item), SpeculativeBindingOption.BindAsTypeOrNamespace)
            Return type.Type IsNot Nothing AndAlso Not IsPredefinedType(type.Type)
        End Function

        Private Function IsPredefinedType(type As ITypeSymbol) As Boolean
            Select Case type.SpecialType
                Case SpecialType.System_Boolean,
                     SpecialType.System_Byte,
                     SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_UInt16,
                     SpecialType.System_Int32,
                     SpecialType.System_UInt32,
                     SpecialType.System_Int64,
                     SpecialType.System_UInt64,
                     SpecialType.System_Single,
                     SpecialType.System_Double,
                     SpecialType.System_Decimal,
                     SpecialType.System_DateTime,
                     SpecialType.System_Char,
                     SpecialType.System_String,
                     SpecialType.System_Enum,
                     SpecialType.System_Object,
                     SpecialType.System_Delegate
                    Return True
                Case Else
                    Return False
            End Select
        End Function
    End Class
End Namespace
