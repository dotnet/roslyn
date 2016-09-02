' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities.DocumentationCommentXmlNames

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class XmlDocCommentCompletionProvider
        Inherits AbstractDocCommentCompletionProvider(Of DocumentationCommentTriviaSyntax)

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return text(characterPosition) = "<"c OrElse text(characterPosition) = """"c OrElse
                (text(characterPosition) = "/"c AndAlso characterPosition > 0 AndAlso text(characterPosition - 1) = "<"c) OrElse
                IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options)
        End Function

        Public Function GetPreviousTokenIfTouchingText(token As SyntaxToken, position As Integer) As SyntaxToken
            Return If(token.IntersectsWith(position) AndAlso IsText(token),
                      token.GetPreviousToken(includeSkipped:=True),
                      token)
        End Function

        Private Function IsText(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.XmlNameToken, SyntaxKind.XmlTextLiteralToken, SyntaxKind.IdentifierToken)
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, trigger As CompletionTrigger, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CompletionItem))
            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments:=True)

            Dim parent = token.GetAncestor(Of DocumentationCommentTriviaSyntax)()

            If parent Is Nothing Then
                Return Nothing
            End If

            ' If the user is typing in xml text, don't trigger on backspace.
            If token.IsKind(SyntaxKind.XmlTextLiteralToken) AndAlso
                trigger.Kind = CompletionTriggerKind.Deletion Then
                Return Nothing
            End If

            ' Never provide any items inside a cref
            If token.Parent.IsKind(SyntaxKind.XmlString) AndAlso token.Parent.Parent.IsKind(SyntaxKind.XmlAttribute) Then
                Dim attribute = DirectCast(token.Parent.Parent, XmlAttributeSyntax)
                Dim name = TryCast(attribute.Name, XmlNameSyntax)
                If name IsNot Nothing AndAlso name.LocalName.ValueText = CrefAttributeName Then
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

            Dim semanticModel = Await document.GetSemanticModelForNodeAsync(attachedToken.Parent, cancellationToken).ConfigureAwait(False)
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

            If trigger.Kind = CompletionTriggerKind.Insertion AndAlso Not trigger.Character = """"c AndAlso Not trigger.Character = "<"c Then
                Return items
            End If

            items.AddRange(GetAlwaysVisibleItems())

            Dim parentElement = token.GetAncestor(Of XmlElementSyntax)()
            If parentElement Is Nothing Then
                Return items
            End If

            Dim grandParent = parentElement.Parent

            If grandParent.IsKind(SyntaxKind.XmlElement) Then
                items.AddRange(GetNestedItems(symbol))

                If GetStartTagName(grandParent) = ListElementName Then
                    items.AddRange(GetListItems())
                End If

                If GetStartTagName(grandParent) = ListHeaderElementName Then
                    items.AddRange(GetListHeaderItems())
                End If
            ElseIf token.Parent.IsKind(SyntaxKind.XmlText) AndAlso token.Parent.Parent.IsKind(SyntaxKind.XmlElement) Then
                items.AddRange(GetNestedItems(symbol))

                If GetStartTagName(token.Parent.Parent) = ListElementName Then
                    items.AddRange(GetListItems())
                End If

                If GetStartTagName(token.Parent.Parent) = ListHeaderElementName Then
                    items.AddRange(GetListHeaderItems())
                End If
            ElseIf grandParent.IsKind(SyntaxKind.DocumentationCommentTrivia) Then
                items.AddRange(GetItemsForSymbol(symbol, parent))
                items.AddRange(GetTopLevelSingleUseItems(parent))
                items.AddRange(GetTopLevelRepeatableItems())
            End If

            If token.Parent.IsKind(SyntaxKind.XmlElementStartTag, SyntaxKind.XmlName) Then
                If parentElement.IsParentKind(SyntaxKind.XmlElement) Then
                    If GetStartTagName(parentElement.Parent) = ListElementName Then
                        items.AddRange(GetListItems())
                    End If

                    If GetStartTagName(parentElement.Parent) = ListHeaderElementName Then
                        items.AddRange(GetListHeaderItems())
                    End If
                End If
            End If

            Return items
        End Function

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
                Return SpecializedCollections.SingletonEnumerable(CreateCompletionItem(nameToken.ValueText, nameToken.ValueText & ">", String.Empty))
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
                    items.AddRange(GetAttributes(tagName, tagAttributes))
                End If

                '<exception a|
                If targetToken.IsChildToken(Function(n As XmlNameSyntax) n.LocalName) AndAlso targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute) Then
                    ' <exception |
                    items.AddRange(GetAttributes(tagName, tagAttributes))
                End If

                '<exception a=""|
                If targetToken.IsChildToken(Function(s As XmlStringSyntax) s.EndQuoteToken) AndAlso targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute) Then
                    items.AddRange(GetAttributes(tagName, tagAttributes))
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

        Protected Overrides Function GetKeywordNames() As IEnumerable(Of String)
            Return SyntaxFacts.GetKeywordKinds().Select(AddressOf SyntaxFacts.GetText)
        End Function

        Protected Overrides Function GetExistingTopLevelElementNames(parentTrivia As DocumentationCommentTriviaSyntax) As IEnumerable(Of String)
            Return parentTrivia.Content.Select(AddressOf GetElementName)
        End Function

        Protected Overrides Function GetExistingTopLevelAttributeValues(syntax As DocumentationCommentTriviaSyntax, elementName As String, attributeName As String) As IEnumerable(Of String)
            Dim attributeValues = SpecializedCollections.EmptyEnumerable(Of String)()

            For Each node In syntax.Content
                Dim attributes As SyntaxList(Of XmlNodeSyntax) = Nothing
                If GetElementNameAndAttributes(node, attributes) = elementName Then
                    attributeValues = attributeValues.Concat(
                        attributes.Where(Function(attribute) GetAttributeName(attribute) = attributeName) _
                                  .Select(AddressOf GetAttributeValue))
                End If
            Next

            Return attributeValues
        End Function

        Private Function GetElementName(node As XmlNodeSyntax) As String
            Dim attributes As SyntaxList(Of XmlNodeSyntax) = Nothing
            Return GetElementNameAndAttributes(node, attributes)
        End Function

        Private Function GetElementNameAndAttributes(node As XmlNodeSyntax, ByRef attributes As SyntaxList(Of XmlNodeSyntax)) As String
            attributes = Nothing

            Dim nameSyntax As XmlNameSyntax = Nothing

            If node.IsKind(SyntaxKind.XmlEmptyElement) Then
                Dim emptyElementSyntax = DirectCast(node, XmlEmptyElementSyntax)
                nameSyntax = TryCast(emptyElementSyntax.Name, XmlNameSyntax)
                attributes = emptyElementSyntax.Attributes
            ElseIf node.IsKind(SyntaxKind.XmlElement) Then
                Dim elementSyntax = DirectCast(node, XmlElementSyntax)
                nameSyntax = TryCast(elementSyntax.StartTag.Name, XmlNameSyntax)
                attributes = elementSyntax.StartTag.Attributes
            End If

            Return nameSyntax?.LocalName.ValueText
        End Function

        Private Function GetAttributeValue(attribute As XmlNodeSyntax) As String
            If TypeOf attribute Is XmlAttributeSyntax Then
                Return DirectCast(DirectCast(attribute, XmlAttributeSyntax).Value, XmlStringSyntax).TextTokens.GetValueText()
            End If

            Return TryCast(attribute, XmlNameAttributeSyntax)?.Reference?.Identifier.ValueText
        End Function

        Private Function GetAttributes(tagName As String, attributes As SyntaxList(Of XmlNodeSyntax)) As IEnumerable(Of CompletionItem)
            Dim existingAttributeNames = attributes.Select(AddressOf GetAttributeName).WhereNotNull().ToSet()
            Return GetAttributeItems(tagName, existingAttributeNames)
        End Function

        Private Shared Function GetAttributeName(node As XmlNodeSyntax) As String
            Dim nameSyntax As XmlNameSyntax = node.TypeSwitch(
                Function(attribute As XmlAttributeSyntax) TryCast(attribute.Name, XmlNameSyntax),
                Function(attribute As XmlNameAttributeSyntax) attribute.Name,
                Function(attribute As XmlCrefAttributeSyntax) attribute.Name)

            Return nameSyntax?.LocalName.ValueText
        End Function

        Private Shared s_defaultRules As CompletionItemRules =
            CompletionItemRules.Create(
                filterCharacterRules:=FilterRules,
                enterKeyRule:=EnterKeyRule.Never)

        Protected Overrides Function GetCompletionItemRules(displayText As String) As CompletionItemRules
            Dim commitRules = s_defaultRules.CommitCharacterRules

            If displayText.Contains("""") Then
                commitRules = commitRules.Add(WithoutQuoteRule)
            End If

            If displayText.Contains(" ") Then
                commitRules = commitRules.Add(WithoutSpaceRule)
            End If

            Return s_defaultRules.WithCommitCharacterRules(commitRules)
        End Function

    End Class
End Namespace
