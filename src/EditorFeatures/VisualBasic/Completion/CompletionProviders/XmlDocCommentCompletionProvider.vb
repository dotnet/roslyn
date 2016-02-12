' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.CompletionProviders.XmlDocCommentCompletion
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Completion.CompletionProviders
    <ExportCompletionProvider("XmlDocCommentCompletionProvider", LanguageNames.VisualBasic)>
    Partial Friend Class XmlDocCommentCompletionProvider
        Inherits AbstractDocCommentCompletionProvider

        ' Tag names
        Private Const CompletionListTagName = "completionlist"
        Private Const ExampleTagName = "example"
        Private Const ExceptionTagName = "exception"
        Private Const IncludeTagName = "include"
        Private Const ParamTagName = "param"
        Private Const PermissionTagName = "permission"
        Private Const RemarksTagName = "remarks"
        Private Const ReturnsTagName = "returns"
        Private Const SummaryTagName = "summary"
        Private Const TypeParamTagName = "typeparam"
        Private Const ValueTagName = "value"

        ' Attribute names
        Private Const CrefAttributeName = "cref"
        Private Const ListTagName = "list"
        Private Const ListHeaderTagName = "listheader"
        Private Const NameAttributeName = "name"

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return text(characterPosition) = "<"c OrElse (text(characterPosition) = "/"c AndAlso characterPosition > 0 AndAlso text(characterPosition - 1) = "<"c)
        End Function

        Public Function GetPreviousTokenIfTouchingText(token As SyntaxToken, position As Integer) As SyntaxToken
            Return If(token.IntersectsWith(position) AndAlso IsText(token),
                      token.GetPreviousToken(includeSkipped:=True),
                      token)
        End Function

        Private Function IsText(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.XmlNameToken, SyntaxKind.XmlTextLiteralToken, SyntaxKind.IdentifierToken)
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As CompletionTriggerInfo, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CompletionItem))
            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments:=True)

            Dim parent = token.GetAncestor(Of DocumentationCommentTriviaSyntax)()

            If parent Is Nothing Then
                Return Nothing
            End If

            ' If the user is typing in xml text, don't trigger on backspace.
            If token.IsKind(SyntaxKind.XmlTextLiteralToken) AndAlso
                triggerInfo.TriggerReason = CompletionTriggerReason.BackspaceOrDeleteCommand Then
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

            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim span = CompletionUtilities.GetTextChangeSpan(text, position)

            Dim items = New List(Of CompletionItem)()

            Dim attachedToken = parent.ParentTrivia.Token
            If attachedToken.Kind = SyntaxKind.None Then
                Return items
            End If

            Dim declaration = attachedToken.GetAncestor(Of DeclarationStatementSyntax)()

            ' Maybe we're going to suggest the close tag
            If token.Kind = SyntaxKind.LessThanSlashToken Then
                Return GetCloseTagItem(token, span)
            ElseIf token.IsKind(SyntaxKind.XmlNameToken) AndAlso token.GetPreviousToken().IsKind(SyntaxKind.LessThanSlashToken) Then
                Return GetCloseTagItem(token.GetPreviousToken(), span)
            End If

            ' Maybe we're going to do attribute completion
            Dim semanticModel = Await document.GetSemanticModelForNodeAsync(attachedToken.Parent, cancellationToken).ConfigureAwait(False)
            TryGetAttributes(token, position, span, items, declaration, semanticModel, cancellationToken)
            If items.Any() Then
                Return items
            End If

            items.AddRange(GetAlwaysVisibleItems(span))

            If declaration IsNot Nothing Then
                items.AddRange(GetTagsForDeclaration(semanticModel, declaration, span, parent, cancellationToken))
            End If

            Dim parentElement = token.GetAncestor(Of XmlElementSyntax)()
            If parentElement Is Nothing Then
                Return items
            End If

            Dim grandParent = parentElement.Parent

            If grandParent.IsKind(SyntaxKind.XmlElement) Then
                items.AddRange(GetNestedTags(span))

                If GetStartTagName(grandParent) = ListTagName Then
                    items.AddRange(GetListItems(span))
                End If

                If GetStartTagName(grandParent) = ListHeaderTagName Then
                    items.AddRange(GetListHeaderItems(span))
                End If
            ElseIf token.Parent.IsKind(SyntaxKind.XmlText) AndAlso token.Parent.Parent.IsKind(SyntaxKind.XmlElement) Then
                items.AddRange(GetNestedTags(span))

                If GetStartTagName(token.Parent.Parent) = ListTagName Then
                    items.AddRange(GetListItems(span))
                End If

                If GetStartTagName(token.Parent.Parent) = ListHeaderTagName Then
                    items.AddRange(GetListHeaderItems(span))
                End If
            ElseIf grandParent.IsKind(SyntaxKind.DocumentationCommentTrivia) Then
                items.AddRange(GetSingleUseTopLevelItems(parent, span))
                items.AddRange(GetTopLevelRepeatableItems(span))
            End If

            If token.Parent.IsKind(SyntaxKind.XmlElementStartTag, SyntaxKind.XmlName) Then
                If parentElement.IsParentKind(SyntaxKind.XmlElement) Then
                    If GetStartTagName(parentElement.Parent) = ListTagName Then
                        items.AddRange(GetListItems(span))
                    End If

                    If GetStartTagName(parentElement.Parent) = ListHeaderTagName Then
                        items.AddRange(GetListHeaderItems(span))
                    End If
                End If
            End If

            Return items
        End Function

        Private Function GetCloseTagItem(token As SyntaxToken, span As TextSpan) As IEnumerable(Of CompletionItem)
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
                Return SpecializedCollections.SingletonEnumerable(Of CompletionItem)(New XmlDocCommentCompletionItem(Me, span, nameToken.ValueText, nameToken.ValueText + ">", String.Empty, GetCompletionItemRules()))
            End If

            Return Nothing
        End Function

        Private Shared Function GetStartTagName(element As SyntaxNode) As String
            Return DirectCast(DirectCast(element, XmlElementSyntax).StartTag.Name, XmlNameSyntax).LocalName.ValueText
        End Function

        Private Sub TryGetAttributes(token As SyntaxToken,
                                     position As Integer,
                                     span As TextSpan,
                                     items As List(Of CompletionItem),
                                     declaration As DeclarationStatementSyntax,
                                     semanticModel As SemanticModel,
                                     cancellationToken As CancellationToken)
            Dim startTagSyntax = token.GetAncestor(Of XmlElementStartTagSyntax)()
            If startTagSyntax IsNot Nothing Then
                Dim targetToken = GetPreviousTokenIfTouchingText(token, position)

                If targetToken.IsChildToken(Function(n As XmlNameSyntax) n.LocalName) AndAlso targetToken.Parent Is startTagSyntax.Name Then
                    ' <exception |
                    items.AddRange(GetAttributes(startTagSyntax, span))
                End If

                '<exception a|
                If targetToken.IsChildToken(Function(n As XmlNameSyntax) n.LocalName) AndAlso targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute) Then
                    ' <exception |
                    items.AddRange(GetAttributes(startTagSyntax, span))
                End If

                '<exception a=""|
                If targetToken.IsChildToken(Function(s As XmlStringSyntax) s.EndQuoteToken) AndAlso targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute) Then
                    items.AddRange(GetAttributes(startTagSyntax, span))
                End If

                ' <param name="|"
                If (targetToken.IsChildToken(Function(s As XmlStringSyntax) s.StartQuoteToken) AndAlso targetToken.Parent.IsParentKind(SyntaxKind.XmlAttribute)) OrElse
                    targetToken.IsChildToken(Function(a As XmlNameAttributeSyntax) a.StartQuoteToken) Then
                    Dim symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken)

                    Dim name = TryCast(startTagSyntax.Name, XmlNameSyntax)
                    Dim tagName = name.LocalName.ValueText

                    Dim attributeName As String

                    Dim xmlAttributeName = targetToken.GetAncestor(Of XmlNameAttributeSyntax)()
                    If xmlAttributeName IsNot Nothing Then
                        attributeName = xmlAttributeName.Name.LocalName.ValueText
                    Else
                        attributeName = DirectCast(targetToken.GetAncestor(Of XmlAttributeSyntax)().Name, XmlNameSyntax).LocalName.ValueText
                    End If

                    If attributeName = NameAttributeName Then
                        If tagName = ParamTagName Then
                            items.AddRange(symbol.GetParameters().Select(Function(s) New XmlDocCommentCompletionItem(Me, span, s.Name, GetCompletionItemRules())))
                        End If

                        If tagName = TypeParamTagName Then
                            items.AddRange(symbol.GetTypeArguments().Select(Function(s) New XmlDocCommentCompletionItem(Me, span, s.Name, GetCompletionItemRules())))
                        End If
                    End If
                End If
            End If
        End Sub

        Private Function GetTagsForDeclaration(semanticModel As SemanticModel,
                                               declaration As DeclarationStatementSyntax,
                                               span As TextSpan,
                                               parent As DocumentationCommentTriviaSyntax,
                                               cancellationToken As CancellationToken) As IEnumerable(Of CompletionItem)
            Dim items = New List(Of CompletionItem)()

            Dim symbol = semanticModel.GetDeclaredSymbol(declaration)
            If symbol Is Nothing Then
                Return items
            End If

            Dim method = TryCast(symbol, IMethodSymbol)
            If method IsNot Nothing Then
                items.AddRange(GetTagsForMethod(method, span, parent))
            End If

            Dim [property] = TryCast(symbol, IPropertySymbol)
            If [property] IsNot Nothing Then
                items.AddRange(GetTagsForProperty([property], span, parent))
            End If

            Dim type = TryCast(symbol, INamedTypeSymbol)
            If type IsNot Nothing Then
                items.AddRange(GetTagsForType(type, span, parent))
            End If

            Return items
        End Function

        Private Function GetTagsForType(type As INamedTypeSymbol, span As TextSpan, parent As DocumentationCommentTriviaSyntax) As IEnumerable(Of CompletionItem)
            Dim items = New List(Of CompletionItem)

            Dim typeParameters = type.GetTypeArguments().Select(Function(t) t.Name).ToSet()
            RemoveExistingTags(parent, typeParameters, Function(e) FindName(TypeParamTagName, e))

            items.AddRange(typeParameters.Select(Function(p) New XmlDocCommentCompletionItem(Me, span, FormatParameter(TypeParamTagName, p), GetCompletionItemRules())))

            Return items
        End Function

        Private Function GetTagsForProperty([property] As IPropertySymbol,
                                            span As TextSpan,
                                            parent As DocumentationCommentTriviaSyntax) As IEnumerable(Of CompletionItem)
            Dim items = New List(Of CompletionItem)

            Dim typeParameters = [property].GetTypeArguments().Select(Function(t) t.Name).ToSet()
            Dim value = True

            For Each node In parent.ChildNodes
                Dim element = TryCast(node, XmlElementSyntax)
                If element IsNot Nothing AndAlso Not element.StartTag.IsMissing AndAlso Not element.EndTag.IsMissing Then
                    Dim startTag = element.StartTag

                    If startTag.ToString() = ValueTagName Then
                        value = False
                    End If
                End If
            Next

            If [property].IsIndexer Then
                Dim parameters = [property].Parameters.Select(Function(p) p.Name).ToSet()
                RemoveExistingTags(parent, parameters, Function(e) FindName(ParamTagName, e))
                items.AddRange(parameters.Select(Function(p) New XmlDocCommentCompletionItem(Me, span, FormatParameter(ParamTagName, p), GetCompletionItemRules())))
            End If

            items.AddRange(typeParameters.Select(Function(p) New XmlDocCommentCompletionItem(Me, span, FormatParameter(TypeParamTagName, p), GetCompletionItemRules())))

            If value Then
                items.Add(GetItem(ValueTagName, span))
            End If

            Return items
        End Function

        Private Function GetTagsForMethod(method As IMethodSymbol, span As TextSpan, parent As DocumentationCommentTriviaSyntax) As IEnumerable(Of CompletionItem)
            Dim items = New List(Of CompletionItem)

            Dim parameters = method.Parameters.Select(Function(p) p.Name).ToSet()
            Dim typeParameters = method.TypeParameters.Select(Function(t) t.Name).ToSet()
            Dim returns = True

            RemoveExistingTags(parent, parameters, Function(e) FindName(ParamTagName, e))
            RemoveExistingTags(parent, typeParameters, Function(e) FindName(TypeParamTagName, e))

            For Each node In parent.ChildNodes
                Dim element = TryCast(node, XmlElementSyntax)
                If element IsNot Nothing AndAlso Not element.StartTag.IsMissing AndAlso Not element.EndTag.IsMissing Then
                    Dim startTag = element.StartTag

                    If startTag.ToString() = ReturnsTagName Then
                        returns = False
                    End If
                End If
            Next

            items.AddRange(parameters.Select(Function(p) New XmlDocCommentCompletionItem(Me, span, FormatParameter(ParamTagName, p), GetCompletionItemRules())))
            items.AddRange(typeParameters.Select(Function(p) New XmlDocCommentCompletionItem(Me, span, FormatParameter(TypeParamTagName, p), GetCompletionItemRules())))

            If returns Then
                items.Add(GetItem(ReturnsTagName, span))
            End If

            Return items
        End Function

        Private Function FindName(name As String, element As XmlElementSyntax) As String
            Dim startTag = element.StartTag
            Dim nameSyntax = TryCast(startTag.Name, XmlNameSyntax)

            If nameSyntax.LocalName.ValueText = name Then
                Return startTag.Attributes.OfType(Of XmlNameAttributeSyntax)() _
                    .Where(Function(a) a.Name.ToString() = NameAttributeName) _
                    .Select(Function(a) a.Reference.Identifier.ValueText) _
                    .FirstOrDefault()
            End If

            Return Nothing
        End Function

        Private Function GetSingleUseTopLevelItems(parentTrivia As DocumentationCommentTriviaSyntax, span As TextSpan) As IEnumerable(Of CompletionItem)
            Dim names = New HashSet(Of String)({SummaryTagName, RemarksTagName, ExceptionTagName, IncludeTagName, PermissionTagName, ExampleTagName, CompletionListTagName})

            RemoveExistingTags(parentTrivia, names, Function(x) DirectCast(x.StartTag.Name, XmlNameSyntax).LocalName.ValueText)

            Return names.Select(Function(n) GetItem(n, span))
        End Function

        Private Sub RemoveExistingTags(parentTrivia As DocumentationCommentTriviaSyntax, names As ISet(Of String), selector As Func(Of XmlElementSyntax, String))
            For Each node In parentTrivia.Content
                Dim element = TryCast(node, XmlElementSyntax)
                If element IsNot Nothing Then
                    names.Remove(selector(element))
                End If
            Next
        End Sub

        Private Function GetAttributes(startTag As XmlElementStartTagSyntax, span As TextSpan) As IEnumerable(Of CompletionItem)
            Dim nameSyntax = TryCast(startTag.Name, XmlNameSyntax)
            If nameSyntax IsNot Nothing Then
                Dim name = nameSyntax.LocalName.ValueText
                Dim existingAttributeNames = startTag.Attributes.OfType(Of XmlAttributeSyntax).Select(Function(a) DirectCast(a.Name, XmlNameSyntax).LocalName.ValueText)
                Return GetAttributeItem(name, span).Where(Function(i) Not existingAttributeNames.Contains(i.DisplayText))
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetCompletionItemRules() As AbstractXmlDocCommentCompletionItemRules
            Return XmlDocCommentCompletionItemRules.Instance
        End Function

    End Class
End Namespace
