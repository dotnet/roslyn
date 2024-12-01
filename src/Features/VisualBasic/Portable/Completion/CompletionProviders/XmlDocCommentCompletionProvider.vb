' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities.DocumentationCommentXmlNames

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(XmlDocCommentCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(OverrideCompletionProvider))>
    <[Shared]>
    Friend Class XmlDocCommentCompletionProvider
        Inherits AbstractDocCommentCompletionProvider(Of DocumentationCommentTriviaSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(s_defaultRules)
        End Sub

        Private Shared ReadOnly s_keywordNames As ImmutableArray(Of String)

        Shared Sub New()
            Dim keywordsBuilder As New List(Of String)

            For Each keywordKind In SyntaxFacts.GetKeywordKinds()
                keywordsBuilder.Add(SyntaxFacts.GetText(keywordKind))
            Next

            s_keywordNames = keywordsBuilder.ToImmutableArray()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Dim isStartOfTag = text(characterPosition) = "<"c
            Dim isClosingTag = (text(characterPosition) = "/"c AndAlso characterPosition > 0 AndAlso text(characterPosition - 1) = "<"c)
            Dim isDoubleQuote = text(characterPosition) = """"c

            Return isStartOfTag OrElse isClosingTag OrElse isDoubleQuote OrElse
                   IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = ImmutableHashSet.Create("<"c, "/"c, """"c, " "c)

        Public Shared Function GetPreviousTokenIfTouchingText(token As SyntaxToken, position As Integer) As SyntaxToken
            Return If(token.IntersectsWith(position) AndAlso IsText(token),
                      token.GetPreviousToken(includeSkipped:=True),
                      token)
        End Function

        Private Shared Function IsText(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.XmlNameToken, SyntaxKind.XmlTextLiteralToken, SyntaxKind.IdentifierToken)
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, trigger As CompletionTrigger, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CompletionItem))
            Try
                Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
                Dim token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments:=True)

                Dim parent = token.GetAncestor(Of DocumentationCommentTriviaSyntax)()

                If parent Is Nothing Then
                    Return Nothing
                End If

                ' If the user is typing in xml text, don't trigger on backspace.
                If token.IsKind(SyntaxKind.XmlTextLiteralToken) AndAlso
                    Not token.Parent.IsKind(SyntaxKind.XmlString) AndAlso
                    trigger.Kind = CompletionTriggerKind.Deletion Then
                    Return Nothing
                End If

                ' Never provide any items inside a cref
                If token.Parent.IsKind(SyntaxKind.XmlString) AndAlso token.Parent.Parent.IsKind(SyntaxKind.XmlAttribute) Then
                    Dim attribute = DirectCast(token.Parent.Parent, XmlAttributeSyntax)
                    Dim name = TryCast(attribute.Name, XmlNameSyntax)
                    Dim value = TryCast(attribute.Value, XmlStringSyntax)
                    If name?.LocalName.ValueText = CrefAttributeName AndAlso Not token = value?.EndQuoteToken Then
                        Return Nothing
                    End If
                End If

                If token.Parent.GetAncestor(Of XmlCrefAttributeSyntax)() IsNot Nothing Then
                    Return Nothing
                End If

                Dim items = New List(Of CompletionItem)()

                Dim attachedToken = parent.ParentTrivia.Token
                If attachedToken.Kind = SyntaxKind.None Then
                    Return items
                End If

                Dim declaration = attachedToken.GetAncestor(Of DeclarationStatementSyntax)()

                ' Maybe we're going to suggest the close tag
                If token.Kind = SyntaxKind.LessThanSlashToken Then
                    Return GetCloseTagItem(token)
                ElseIf token.IsKind(SyntaxKind.XmlNameToken) AndAlso token.GetPreviousToken().IsKind(SyntaxKind.LessThanSlashToken) Then
                    Return GetCloseTagItem(token.GetPreviousToken())
                End If

                Dim semanticModel = Await document.ReuseExistingSpeculativeModelAsync(attachedToken.Parent, cancellationToken).ConfigureAwait(False)
                Dim symbol As ISymbol = Nothing

                If declaration IsNot Nothing Then
                    symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken)
                End If

                If symbol IsNot Nothing Then
                    ' Maybe we're going to do attribute completion
                    TryGetAttributes(token, position, items, symbol)
                    If items.Any() Then
                        Return items
                    End If
                End If

                If trigger.Kind = CompletionTriggerKind.Insertion AndAlso
                    Not trigger.Character = """"c AndAlso
                    Not trigger.Character = "<"c Then
                    ' With the use of IsTriggerAfterSpaceOrStartOfWordCharacter, the code below is much
                    ' too aggressive at suggesting tags, so exit early before degrading the experience
                    Return items
                End If

                items.AddRange(GetAlwaysVisibleItems())

                Dim parentElement = token.GetAncestor(Of XmlElementSyntax)()
                Dim grandParent = parentElement?.Parent

                If grandParent.IsKind(SyntaxKind.XmlElement) Then
                    ' Avoid including language keywords when following < Or <text, since these cases should only be
                    ' attempting to complete the XML name (which for language keywords Is 'see'). The VB parser treats
                    ' spaces after a < character as trailing whitespace, even if an identifier follows it on the same line.
                    ' Therefore, the consistent VB experience says we never show keywords for < followed by spaces.
                    Dim xmlNameOnly = token.IsKind(SyntaxKind.LessThanToken) OrElse token.Parent.IsKind(SyntaxKind.XmlName)
                    Dim includeKeywords = Not xmlNameOnly

                    items.AddRange(GetNestedItems(symbol, includeKeywords))
                    AddXmlElementItems(items, grandParent)
                ElseIf token.Parent.IsKind(SyntaxKind.XmlText) AndAlso
                       token.Parent.IsParentKind(SyntaxKind.DocumentationCommentTrivia) Then

                    ' Top level, without tag:
                    '     ''' $$
                    items.AddRange(GetTopLevelItems(symbol, parent))
                ElseIf token.Parent.IsKind(SyntaxKind.XmlText) AndAlso
                       token.Parent.Parent.IsKind(SyntaxKind.XmlElement) Then
                    items.AddRange(GetNestedItems(symbol, includeKeywords:=True))
                    Dim xmlElement = token.Parent.Parent

                    AddXmlElementItems(items, xmlElement)
                ElseIf grandParent.IsKind(SyntaxKind.DocumentationCommentTrivia) Then
                    ' Top level, with tag:
                    '     ''' <$$
                    '     ''' <tag$$
                    items.AddRange(GetTopLevelItems(symbol, parent))
                End If

                If token.Parent.IsKind(SyntaxKind.XmlElementStartTag, SyntaxKind.XmlName) AndAlso
                   parentElement.IsParentKind(SyntaxKind.XmlElement) Then

                    AddXmlElementItems(items, parentElement.Parent)
                End If

                Return items
            Catch e As Exception When FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken)
                Return SpecializedCollections.EmptyEnumerable(Of CompletionItem)
            End Try
        End Function

        Private Sub AddXmlElementItems(items As List(Of CompletionItem), xmlElement As SyntaxNode)
            Dim startTagName = GetStartTagName(xmlElement)
            If startTagName = ListElementName Then
                items.AddRange(GetListItems())
            ElseIf startTagName = ListHeaderElementName Then
                items.AddRange(GetListHeaderItems())
            ElseIf startTagName = ItemElementName Then
                items.AddRange(GetItemTagItems())
            End If
        End Sub

        Private Function GetCloseTagItem(token As SyntaxToken) As IEnumerable(Of CompletionItem)
            Dim endTag = TryCast(token.Parent, XmlElementEndTagSyntax)
            If endTag Is Nothing Then
                Return Nothing
            End If

            Dim element = TryCast(endTag.Parent, XmlElementSyntax)
            If element Is Nothing Then
                Return Nothing
            End If

            Dim startElement = element.StartTag
            Dim name = TryCast(startElement.Name, XmlNameSyntax)
            If name Is Nothing Then
                Return Nothing
            End If

            Dim nameToken = name.LocalName
            If Not nameToken.IsMissing AndAlso nameToken.ValueText.Length > 0 Then
                Return SpecializedCollections.SingletonEnumerable(CreateCompletionItem(nameToken.ValueText, beforeCaretText:=nameToken.ValueText & ">", afterCaretText:=String.Empty))
            End If

            Return Nothing
        End Function

        Private Shared Function GetStartTagName(element As SyntaxNode) As String
            Return DirectCast(DirectCast(element, XmlElementSyntax).StartTag.Name, XmlNameSyntax).LocalName.ValueText
        End Function

        Private Sub TryGetAttributes(token As SyntaxToken,
                                     position As Integer,
                                     items As List(Of CompletionItem),
                                     symbol As ISymbol)
            Dim tagNameSyntax As XmlNameSyntax = Nothing
            Dim tagAttributes As SyntaxList(Of XmlNodeSyntax) = Nothing

            Dim startTagSyntax = token.GetAncestor(Of XmlElementStartTagSyntax)()
            If startTagSyntax IsNot Nothing Then
                tagNameSyntax = TryCast(startTagSyntax.Name, XmlNameSyntax)
                tagAttributes = startTagSyntax.Attributes
            Else

                Dim emptyElementSyntax = token.GetAncestor(Of XmlEmptyElementSyntax)()
                If emptyElementSyntax IsNot Nothing Then
                    tagNameSyntax = TryCast(emptyElementSyntax.Name, XmlNameSyntax)
                    tagAttributes = emptyElementSyntax.Attributes
                End If

            End If

            If tagNameSyntax IsNot Nothing Then
                Dim targetToken = GetPreviousTokenIfTouchingText(token, position)
                Dim tagName = tagNameSyntax.LocalName.ValueText

                If targetToken.IsChildToken(Function(n As XmlNameSyntax) n.LocalName) AndAlso targetToken.Parent Is tagNameSyntax Then
                    ' <exception |
                    items.AddRange(GetAttributes(token, tagName, tagAttributes))
                End If

                '<exception a|
                If targetToken.IsChildToken(Function(n As XmlNameSyntax) n.LocalName) AndAlso targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute) Then
                    ' <exception |
                    items.AddRange(GetAttributes(token, tagName, tagAttributes))
                End If

                '<exception a=""|
                If (targetToken.IsChildToken(Function(s As XmlStringSyntax) s.EndQuoteToken) AndAlso targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute)) OrElse
                    targetToken.IsChildToken(Function(a As XmlNameAttributeSyntax) a.EndQuoteToken) OrElse
                    targetToken.IsChildToken(Function(a As XmlCrefAttributeSyntax) a.EndQuoteToken) Then
                    items.AddRange(GetAttributes(token, tagName, tagAttributes))
                End If

                ' <param name="|"
                If (targetToken.IsChildToken(Function(s As XmlStringSyntax) s.StartQuoteToken) AndAlso targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute)) OrElse
                    targetToken.IsChildToken(Function(a As XmlNameAttributeSyntax) a.StartQuoteToken) Then
                    Dim attributeName As String

                    Dim xmlAttributeName = targetToken.GetAncestor(Of XmlNameAttributeSyntax)()
                    If xmlAttributeName IsNot Nothing Then
                        attributeName = xmlAttributeName.Name.LocalName.ValueText
                    Else
                        attributeName = DirectCast(targetToken.GetAncestor(Of XmlAttributeSyntax)().Name, XmlNameSyntax).LocalName.ValueText
                    End If

                    items.AddRange(GetAttributeValueItems(symbol, tagName, attributeName))
                End If
            End If
        End Sub

        Protected Overrides Function GetKeywordNames() As ImmutableArray(Of String)
            Return s_keywordNames
        End Function

        Protected Overrides Function GetExistingTopLevelElementNames(parentTrivia As DocumentationCommentTriviaSyntax) As IEnumerable(Of String)
            Return parentTrivia.Content _
                               .Select(Function(node) GetElementNameAndAttributes(node).Name) _
                               .WhereNotNull()
        End Function

        Protected Overrides Function GetExistingTopLevelAttributeValues(syntax As DocumentationCommentTriviaSyntax, elementName As String, attributeName As String) As IEnumerable(Of String)
            Dim attributeValues = SpecializedCollections.EmptyEnumerable(Of String)()

            For Each node In syntax.Content
                Dim nameAndAttributes = GetElementNameAndAttributes(node)
                If nameAndAttributes.Name = elementName Then
                    attributeValues = attributeValues.Concat(
                        nameAndAttributes.Attributes _
                                         .Where(Function(attribute) GetAttributeName(attribute) = attributeName) _
                                         .Select(AddressOf GetAttributeValue))
                End If
            Next

            Return attributeValues
        End Function

        Private Shared Function GetElementNameAndAttributes(node As XmlNodeSyntax) As (Name As String, Attributes As SyntaxList(Of XmlNodeSyntax))
            Dim nameSyntax As XmlNameSyntax = Nothing
            Dim attributes As SyntaxList(Of XmlNodeSyntax) = Nothing

            If node.IsKind(SyntaxKind.XmlEmptyElement) Then
                Dim emptyElementSyntax = DirectCast(node, XmlEmptyElementSyntax)
                nameSyntax = TryCast(emptyElementSyntax.Name, XmlNameSyntax)
                attributes = emptyElementSyntax.Attributes
            ElseIf node.IsKind(SyntaxKind.XmlElement) Then
                Dim elementSyntax = DirectCast(node, XmlElementSyntax)
                nameSyntax = TryCast(elementSyntax.StartTag.Name, XmlNameSyntax)
                attributes = elementSyntax.StartTag.Attributes
            End If

            Return (nameSyntax?.LocalName.ValueText, attributes)
        End Function

        Private Function GetAttributeValue(attribute As XmlNodeSyntax) As String
            If TypeOf attribute Is XmlAttributeSyntax Then
                ' Decode any XML enities and concatentate the results
                Return DirectCast(DirectCast(attribute, XmlAttributeSyntax).Value, XmlStringSyntax).TextTokens.GetValueText()
            End If

            Return TryCast(attribute, XmlNameAttributeSyntax)?.Reference?.Identifier.ValueText
        End Function

        Private Function GetAttributes(token As SyntaxToken, tagName As String, attributes As SyntaxList(Of XmlNodeSyntax)) As IEnumerable(Of CompletionItem)
            Dim existingAttributeNames = attributes.Select(AddressOf GetAttributeName).WhereNotNull().ToSet()
            Dim nextToken = token.GetNextToken()
            Return GetAttributeItems(tagName, existingAttributeNames,
                                     addEqualsAndQuotes:=Not nextToken.IsKind(SyntaxKind.EqualsToken) Or nextToken.HasLeadingTrivia)
        End Function

        Private Shared Function GetAttributeName(node As XmlNodeSyntax) As String
            Dim nameSyntax As XmlNameSyntax = node.TypeSwitch(
                Function(attribute As XmlAttributeSyntax) TryCast(attribute.Name, XmlNameSyntax),
                Function(attribute As XmlNameAttributeSyntax) attribute.Name,
                Function(attribute As XmlCrefAttributeSyntax) attribute.Name)

            Return nameSyntax?.LocalName.ValueText
        End Function

        Protected Overrides Function GetParameters(symbol As ISymbol) As ImmutableArray(Of IParameterSymbol)
            Dim declaredParameters = symbol.GetParameters()
            Dim namedTypeSymbol = TryCast(symbol, INamedTypeSymbol)
            If namedTypeSymbol IsNot Nothing Then
                If namedTypeSymbol.DelegateInvokeMethod IsNot Nothing Then
                    declaredParameters = namedTypeSymbol.DelegateInvokeMethod.Parameters
                End If
            End If

            Return declaredParameters
        End Function

        Private Shared ReadOnly s_defaultRules As CompletionItemRules =
            CompletionItemRules.Create(
                filterCharacterRules:=FilterRules,
                enterKeyRule:=EnterKeyRule.Never)

    End Class
End Namespace
