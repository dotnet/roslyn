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
    using static DocumentationCommentXmlNames;

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

            if (IsAttributeNameContext(token, position, out string elementName, out ISet<string> existingAttributes))
            {
                return GetAttributeItems(elementName, existingAttributes);
            }

            var wasTriggeredAfterSpace = trigger.Kind == CompletionTriggerKind.Insertion && trigger.Character == ' ';
            if (wasTriggeredAfterSpace)
            {
                // Nothing below this point should triggered by a space character
                // (only attribute names should be triggered by <SPACE>)
                return null;
            }

            if (IsAttributeValueContext(token, out elementName, out string attributeName))
            {
                return GetAttributeValueItems(declaredSymbol, elementName, attributeName);
            }

            if (trigger.Kind == CompletionTriggerKind.Insertion && trigger.Character != '<')
            {
                // With the use of IsTriggerAfterSpaceOrStartOfWordCharacter, the code below is much
                // too aggressive at suggesting tags, so exit early before degrading the experience
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
                    items.AddRange(GetNestedItems(declaredSymbol));
                }

                if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement && ((XmlElementSyntax)token.Parent.Parent).StartTag.Name.LocalName.ValueText == ListElementName)
                {
                    items.AddRange(GetListItems());
                }

                if (token.Parent.IsParentKind(SyntaxKind.XmlEmptyElement) && token.Parent.Parent.IsParentKind(SyntaxKind.XmlElement))
                {
                    var element = (XmlElementSyntax)token.Parent.Parent.Parent;
                    if (element.StartTag.Name.LocalName.ValueText == ListElementName)
                    {
                        items.AddRange(GetListItems());
                    }
                }

                if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement && ((XmlElementSyntax)token.Parent.Parent).StartTag.Name.LocalName.ValueText == ListHeaderElementName)
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

                if (token == startTag.GreaterThanToken && startTag.Name.LocalName.ValueText == ListElementName)
                {
                    items.AddRange(GetListItems());
                }

                if (token == startTag.GreaterThanToken && startTag.Name.LocalName.ValueText == ListHeaderElementName)
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
                // Unlike VB, the C# lexer has a preference for leading trivia. In the following example...
                //
                //    /// <exception          $$
                //
                // ...the trailing whitespace will not be attached as trivia to any node. Instead it will
                // be treated as an independent XmlTextLiteralToken, so skip backwards by one token.
                token = token.GetPreviousToken();
            }

            // Handle the <elem$$ case by going back one token (the subsequent checks need to account for this)
            token = token.GetPreviousTokenIfTouchingWord(position);

            var attributes = default(SyntaxList<XmlAttributeSyntax>);

            if (token.IsKind(SyntaxKind.IdentifierToken) && token.Parent.IsKind(SyntaxKind.XmlName))
            {
                // <elem $$
                // <elem attr$$
                (elementName, attributes) = GetElementNameAndAttributes(token.Parent.Parent);
            }
            else if (token.Parent.IsKind(SyntaxKind.XmlCrefAttribute) ||
                     token.Parent.IsKind(SyntaxKind.XmlNameAttribute) ||
                     token.Parent.IsKind(SyntaxKind.XmlTextAttribute))
            {
                // In the following, 'attr1' may be a regular text attribute, or one of the special 'cref' or 'name' attributes
                // <elem attr1="" $$
                // <elem attr1="" $$attr2	
                // <elem attr1="" attr2$$
                var attributeSyntax = (XmlAttributeSyntax)token.Parent;

                if (token == attributeSyntax.EndQuoteToken)
                {
                    (elementName, attributes) = GetElementNameAndAttributes(attributeSyntax.Parent);
                }
            }

            attributeNames = attributes.Select(GetAttributeName).ToSet();
            return elementName != null;
        }

        private (string name, SyntaxList<XmlAttributeSyntax> attributes) GetElementNameAndAttributes(SyntaxNode node)
        {
            XmlNameSyntax nameSyntax;
            SyntaxList<XmlAttributeSyntax> attributes;

            switch (node)
            {
                // Self contained empty element <tag />
                case XmlEmptyElementSyntax emptyElementSyntax:
                    nameSyntax = emptyElementSyntax.Name;
                    attributes = emptyElementSyntax.Attributes;
                    break;

                // Parent node of a non-empty element: <tag></tag>
                case XmlElementSyntax elementSyntax:
                    // Defer to the start-tag logic
                    return GetElementNameAndAttributes(elementSyntax.StartTag);

                // Start tag of a non-empty element: <tag>
                case XmlElementStartTagSyntax startTagSyntax:
                    nameSyntax = startTagSyntax.Name;
                    attributes = startTagSyntax.Attributes;
                    break;

                default:
                    nameSyntax = null;
                    attributes = default(SyntaxList<XmlAttributeSyntax>);
                    break;
            }

            return (name: nameSyntax?.LocalName.ValueText, attributes: attributes);
        }

        private bool IsAttributeValueContext(SyntaxToken token, out string tagName, out string attributeName)
        {
            XmlAttributeSyntax attributeSyntax = null;

            if (token.Parent.IsKind(SyntaxKind.IdentifierName) && token.Parent.IsParentKind(SyntaxKind.XmlNameAttribute))
            {
                // Handle the special 'name' attributes: name="bar$$
                attributeSyntax = (XmlNameAttributeSyntax)token.Parent.Parent;
            }
            else if (token.IsKind(SyntaxKind.XmlTextLiteralToken) && token.Parent.IsKind(SyntaxKind.XmlTextAttribute))
            {
                // Handle the other general text attributes: foo="bar$$
                attributeSyntax = (XmlTextAttributeSyntax)token.Parent;
            }
            else if (token.Parent.IsKind(SyntaxKind.XmlNameAttribute) || token.Parent.IsKind(SyntaxKind.XmlTextAttribute))
            {
                // When there's no attribute value yet, the parent attribute is returned:
                //     name="$$
                //     foo="$$
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
                    // Empty element tags: <tag attr=... />
                    tagName = emptyElement.Name.LocalName.Text;
                    return true;
                }

                var startTagSyntax = token.GetAncestor<XmlElementStartTagSyntax>();
                if (startTagSyntax != null)
                {
                    // Non-empty element start tags: <tag attr=... >
                    tagName = startTagSyntax.Name.LocalName.Text;
                    return true;
                }
            }

            attributeName = null;
            tagName = null;
            return false;
        }

        protected override IEnumerable<string> GetKeywordNames() =>
            SyntaxFacts.GetKeywordKinds().Select(SyntaxFacts.GetText);

        protected override IEnumerable<string> GetExistingTopLevelElementNames(DocumentationCommentTriviaSyntax syntax) =>
            syntax.Content.Select(GetElementName);

        protected override IEnumerable<string> GetExistingTopLevelAttributeValues(DocumentationCommentTriviaSyntax syntax, string elementName, string attributeName)
        {
            var attributeValues = SpecializedCollections.EmptyEnumerable<string>();

            foreach (var node in syntax.Content)
            {
                (var name, var attributes) = GetElementNameAndAttributes(node);

                if (name == elementName)
                {
                    attributeValues = attributeValues.Concat(
                        attributes.Where(attribute => GetAttributeName(attribute) == attributeName)
                                  .Select(GetAttributeValue));
                }
            }

            return attributeValues;
        }

        private string GetElementName(XmlNodeSyntax node) => GetElementNameAndAttributes(node).name;

        private string GetAttributeName(XmlAttributeSyntax attribute) => attribute.Name.LocalName.ValueText;

        private string GetAttributeValue(XmlAttributeSyntax attribute)
        {
            switch (attribute)
            {
                case XmlTextAttributeSyntax textAttribute:
                    // Decode any XML enities and concatentate the results
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
