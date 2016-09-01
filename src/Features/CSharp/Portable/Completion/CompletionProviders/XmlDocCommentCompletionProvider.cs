// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class XmlDocCommentCompletionProvider : AbstractDocCommentCompletionProvider<DocumentationCommentTriviaSyntax>
    {
        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            var c = text[characterPosition];
            return c == '<' || c == '"' || CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);
        }

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(
            Document document, int position,
            CompletionTrigger trigger, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var parentTrivia = token.GetAncestor<DocumentationCommentTriviaSyntax>();

            if (parentTrivia == null)
            {
                return null;
            }

            var attachedToken = parentTrivia.ParentTrivia.Token;
            if (attachedToken.Kind() == SyntaxKind.None)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(attachedToken.Parent, cancellationToken).ConfigureAwait(false);

            ISymbol declaredSymbol = null;
            var memberDeclaration = attachedToken.GetAncestor<MemberDeclarationSyntax>();
            if (memberDeclaration != null)
            {
                declaredSymbol = semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken);
            }
            else
            {
                var typeDeclaration = attachedToken.GetAncestor<TypeDeclarationSyntax>();
                if (typeDeclaration != null)
                {
                    declaredSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
                }
            }

            string elementName, attributeName;
            if (!(trigger.Kind == CompletionTriggerKind.Insertion && trigger.Character == ' ') &&
                IsAttributeValueContext(token, out elementName, out attributeName))
            {
                return GetAttributeValueItems(declaredSymbol, elementName, attributeName);
            }

            ISet<string> existingAttributes;
            if (IsAttributeNameContext(token, position, out elementName, out existingAttributes))
            {
                return GetAttributeItems(elementName, existingAttributes);
            }

            if (trigger.Kind == CompletionTriggerKind.Insertion && trigger.Character != '<')
            {
                return null;
            }

            var items = new List<CompletionItem>();

            if (token.Parent.Kind() == SyntaxKind.XmlEmptyElement || token.Parent.Kind() == SyntaxKind.XmlText ||
                (token.Parent.IsKind(SyntaxKind.XmlElementEndTag) && token.IsKind(SyntaxKind.GreaterThanToken)) ||
                (token.Parent.IsKind(SyntaxKind.XmlName) && token.Parent.IsParentKind(SyntaxKind.XmlEmptyElement)))
            {
                // The user is typing inside an XmlElement
                if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement ||
                    token.Parent.Parent.IsParentKind(SyntaxKind.XmlElement))
                {
                    items.AddRange(GetNestedTags(declaredSymbol));
                }

                if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement && ((XmlElementSyntax)token.Parent.Parent).StartTag.Name.LocalName.ValueText == ListTagName)
                {
                    items.AddRange(GetListItems());
                }

                if (token.Parent.IsParentKind(SyntaxKind.XmlEmptyElement) && token.Parent.Parent.IsParentKind(SyntaxKind.XmlElement))
                {
                    var element = (XmlElementSyntax)token.Parent.Parent.Parent;
                    if (element.StartTag.Name.LocalName.ValueText == ListTagName)
                    {
                        items.AddRange(GetListItems());
                    }
                }

                if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement && ((XmlElementSyntax)token.Parent.Parent).StartTag.Name.LocalName.ValueText == ListHeaderTagName)
                {
                    items.AddRange(GetListHeaderItems());
                }

                if (token.Parent.Parent is DocumentationCommentTriviaSyntax ||
                    (token.Parent.Parent.IsKind(SyntaxKind.XmlEmptyElement) && token.Parent.Parent.Parent is DocumentationCommentTriviaSyntax))
                {
                    items.AddRange(GetTopLevelSingleUseItems(parentTrivia));
                    items.AddRange(GetTopLevelRepeatableItems());
                    items.AddRange(GetItemsForSymbol(declaredSymbol, parentTrivia));
                }
            }

            if (token.Parent.Kind() == SyntaxKind.XmlElementStartTag)
            {
                var startTag = (XmlElementStartTagSyntax)token.Parent;

                if (token == startTag.GreaterThanToken && startTag.Name.LocalName.ValueText == ListTagName)
                {
                    items.AddRange(GetListItems());
                }

                if (token == startTag.GreaterThanToken && startTag.Name.LocalName.ValueText == ListHeaderTagName)
                {
                    items.AddRange(GetListHeaderItems());
                }
            }

            items.AddRange(GetAlwaysVisibleItems());
            return items;
        }

        private bool IsAttributeNameContext(SyntaxToken token, int position, out string elementName, out ISet<string> attributeNames)
        {
            elementName = null;

            if (token.IsKind(SyntaxKind.XmlTextLiteralToken) && string.IsNullOrWhiteSpace(token.Text))
            {
                // Unlike VB, the C# lexer has a preference for leading trivia so, in the following text...
                //
                //     <exception          $$
                //
                // ...the trailing whitespace will not be attached as trivia to any node. Instead it will
                // be treated as an independent XmlTextLiteralToken, so we skip backwards by one token.
                token = token.GetPreviousToken();
            }

            // Handle the <elem$$ case by going back one token (some of the subsequent checks need to account for this)
            token = token.GetPreviousTokenIfTouchingWord(position);

            SyntaxList<XmlAttributeSyntax> attributes = default(SyntaxList<XmlAttributeSyntax>);

            if (token.IsKind(SyntaxKind.IdentifierToken) && token.IsParentKind(SyntaxKind.XmlName))
            {
                // <elem $$
                // <elem attr$$
                elementName = GetElementNameAndAttributes(token.Parent.Parent, out attributes);
            }
            else if (token.IsParentKind(SyntaxKind.XmlCrefAttribute) ||
                     token.IsParentKind(SyntaxKind.XmlNameAttribute) ||
                     token.IsParentKind(SyntaxKind.XmlTextAttribute))
            {
                // <elem attr="" $$
                // <elem attr="" $$attr	
                // <elem attr="" attr$$
                var attributeSyntax = (XmlAttributeSyntax)token.Parent;

                if (token == attributeSyntax.EndQuoteToken)
                {
                    elementName = GetElementNameAndAttributes(attributeSyntax.Parent, out attributes);
                }
            }

            attributeNames = attributes.Select(attribute => GetAttributeName(attribute))
                                       .ToSet();

            return elementName != null;
        }

        private string GetElementNameAndAttributes(SyntaxNode node, out SyntaxList<XmlAttributeSyntax> attributes)
        {
            XmlNameSyntax nameSyntax;

            switch (node.Kind())
            {
                case SyntaxKind.XmlEmptyElement:
                {
                    var emptyElementSyntax = (XmlEmptyElementSyntax)node;
                    nameSyntax = emptyElementSyntax.Name;
                    attributes = emptyElementSyntax.Attributes;
                    break;
                }

                case SyntaxKind.XmlElement:
                {
                    node = ((XmlElementSyntax)node).StartTag;
                    goto case SyntaxKind.XmlElementStartTag;
                }

                case SyntaxKind.XmlElementStartTag:
                {
                    var startTagSyntax = (XmlElementStartTagSyntax)node;
                    nameSyntax = startTagSyntax.Name;
                    attributes = startTagSyntax.Attributes;
                    break;
                }

                default:
                    nameSyntax = null;
                    attributes = default(SyntaxList<XmlAttributeSyntax>);
                    break;
            }

            return nameSyntax?.LocalName.ValueText;
        }

        private bool IsAttributeValueContext(SyntaxToken token, out string tagName, out string attributeName)
        {
            XmlAttributeSyntax attributeSyntax = null;

            if (token.IsParentKind(SyntaxKind.IdentifierName) && token.Parent.IsParentKind(SyntaxKind.XmlNameAttribute))
            {
                // name="bar$$
                attributeSyntax = (XmlNameAttributeSyntax)token.Parent.Parent;
            }
            else if (token.IsKind(SyntaxKind.XmlTextLiteralToken) && token.IsParentKind(SyntaxKind.XmlTextAttribute))
            {
                // foo="bar$$
                attributeSyntax = (XmlTextAttributeSyntax)token.Parent;
            }
            else if (token.IsParentKind(SyntaxKind.XmlNameAttribute) || token.IsParentKind(SyntaxKind.XmlTextAttribute))
            {
                // name="$$
                // foo="$$
                attributeSyntax = (XmlAttributeSyntax)token.Parent;
                if (token != attributeSyntax.StartQuoteToken)
                {
                    attributeSyntax = null;
                }
            }


            if (attributeSyntax != null)
            {
                attributeName = attributeSyntax.Name.LocalName.ValueText;

                var emptyElement = attributeSyntax.GetAncestor<XmlEmptyElementSyntax>();
                if (emptyElement != null)
                {
                    tagName = emptyElement.Name.LocalName.Text;
                    return true;
                }
                else
                {
                    var startTagSyntax = token.GetAncestor<XmlElementStartTagSyntax>();
                    if (startTagSyntax != null)
                    {
                        tagName = startTagSyntax.Name.LocalName.Text;
                        return true;
                    }
                }
            }

            attributeName = null;
            tagName = null;
            return false;
        }

        protected override IEnumerable<string> GetKeywordNames()
        {
            return SyntaxFacts.GetKeywordKinds().Select(keyword => SyntaxFacts.GetText(keyword));
        }

        protected override IEnumerable<string> GetExistingTopLevelElementNames(DocumentationCommentTriviaSyntax syntax)
        {
            return syntax.Content.Select(GetElementName);
        }

        protected override IEnumerable<string> GetExistingTopLevelAttributeValues(DocumentationCommentTriviaSyntax syntax, string elementName, string attributeName)
        {
            var attributeValues = SpecializedCollections.EmptyEnumerable<string>();

            foreach (var node in syntax.Content)
            {
                SyntaxList<XmlAttributeSyntax> attributes;
                if (GetElementNameAndAttributes(node, out attributes) == elementName)
                {
                    attributeValues = attributeValues.Concat(
                        attributes.Where(attribute => GetAttributeName(attribute) == attributeName)
                                  .Select(GetAttributeValue));
                }
            }

            return attributeValues;
        }

        private string GetElementName(XmlNodeSyntax node)
        {
            SyntaxList<XmlAttributeSyntax> attributes;
            return GetElementNameAndAttributes(node, out attributes);
        }

        private string GetAttributeName(XmlAttributeSyntax attribute)
        {
            return attribute.Name.LocalName.ValueText;
        }

        private string GetAttributeValue(XmlAttributeSyntax attribute)
        {
            switch (attribute)
            {
                case XmlTextAttributeSyntax textAttribute:
                    return textAttribute.TextTokens.GetValueText();

                case XmlNameAttributeSyntax nameAttribute:
                    return nameAttribute.Identifier.Identifier.ValueText;

                default:
                    return null;
            }
        }

        private static CompletionItemRules s_defaultRules = 
            CompletionItemRules.Create(
                filterCharacterRules: FilterRules, 
                commitCharacterRules: ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, '>', '\t')),
                enterKeyRule: EnterKeyRule.Never);

        protected override CompletionItemRules GetCompletionItemRules(string displayText)
        {
            var commitRules = s_defaultRules.CommitCharacterRules;

            if (displayText.Contains("\""))
            {
                commitRules = commitRules.Add(WithoutQuoteRule);
            }

            if (displayText.Contains(" "))
            {
                commitRules = commitRules.Add(WithoutSpaceRule);
            }

            return s_defaultRules.WithCommitCharacterRules(commitRules);
        }
    }
}
