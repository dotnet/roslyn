// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    using static DocumentationCommentXmlNames;

    [ExportCompletionProvider(nameof(XmlDocCommentCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(PartialTypeCompletionProvider))]
    [Shared]
    internal partial class XmlDocCommentCompletionProvider : AbstractDocCommentCompletionProvider<DocumentationCommentTriviaSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XmlDocCommentCompletionProvider() : base(s_defaultRules)
        {
        }

        internal override string Language => LanguageNames.CSharp;

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
            => text[characterPosition] is ('<' or '"') ||
               CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = ImmutableHashSet.Create('<', '"', ' ');

        protected override async Task<IEnumerable<CompletionItem>?> GetItemsWorkerAsync(
            Document document, int position,
            CompletionTrigger trigger, CancellationToken cancellationToken)
        {
            try
            {
                var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
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

                var semanticModel = await document.ReuseExistingSpeculativeModelAsync(attachedToken.Parent, cancellationToken).ConfigureAwait(false);

                ISymbol? declaredSymbol = null;
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

                if (IsAttributeNameContext(token, position, out var elementName, out var existingAttributes))
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

                if (IsAttributeValueContext(token, out elementName, out var attributeName))
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

                if (token.Parent?.Kind() is SyntaxKind.XmlEmptyElement or SyntaxKind.XmlText ||
                    (token.Parent.IsKind(SyntaxKind.XmlElementEndTag) && token.IsKind(SyntaxKind.GreaterThanToken)) ||
                    (token.Parent.IsKind(SyntaxKind.XmlName) && token.Parent.IsParentKind(SyntaxKind.XmlEmptyElement)))
                {
                    // The user is typing inside an XmlElement
                    if (token.Parent.IsParentKind(SyntaxKind.XmlElement) ||
                        token.Parent.Parent.IsParentKind(SyntaxKind.XmlElement))
                    {
                        // Avoid including language keywords when following < or <text, since these cases should only be
                        // attempting to complete the XML name (which for language keywords is 'see'). While the parser
                        // treats the 'name' in '< name' as an XML name, we don't treat it like that here so the completion
                        // experience is consistent for '< ' and '< n'.
                        var xmlNameOnly = token.IsKind(SyntaxKind.LessThanToken)
                            || (token.Parent.IsKind(SyntaxKind.XmlName) && !token.HasLeadingTrivia);
                        var includeKeywords = !xmlNameOnly;

                        items.AddRange(GetNestedItems(declaredSymbol, includeKeywords));
                    }

                    if (token.Parent.Parent is XmlElementSyntax xmlElement)
                    {
                        AddXmlElementItems(items, xmlElement.StartTag);
                    }

                    if (token.Parent.IsParentKind(SyntaxKind.XmlEmptyElement) &&
                        token.Parent.Parent!.Parent is XmlElementSyntax nestedXmlElement)
                    {
                        AddXmlElementItems(items, nestedXmlElement.StartTag);
                    }

                    if (token.Parent.Parent is DocumentationCommentTriviaSyntax ||
                        (token.Parent.Parent.IsKind(SyntaxKind.XmlEmptyElement) && token.Parent.Parent.Parent is DocumentationCommentTriviaSyntax))
                    {
                        items.AddRange(GetTopLevelItems(declaredSymbol, parentTrivia));
                    }
                }

                if (token.Parent is XmlElementStartTagSyntax startTag &&
                    token == startTag.GreaterThanToken)
                {
                    AddXmlElementItems(items, startTag);
                }

                items.AddRange(GetAlwaysVisibleItems());
                return items;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken, ErrorSeverity.General))
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }
        }

        private void AddXmlElementItems(List<CompletionItem> items, XmlElementStartTagSyntax startTag)
        {
            var xmlElementName = startTag.Name.LocalName.ValueText;
            if (xmlElementName == ListElementName)
            {
                items.AddRange(GetListItems());
            }
            else if (xmlElementName == ListHeaderElementName)
            {
                items.AddRange(GetListHeaderItems());
            }
            else if (xmlElementName == ItemElementName)
            {
                items.AddRange(GetItemTagItems());
            }
        }

        private bool IsAttributeNameContext(SyntaxToken token, int position, [NotNullWhen(true)] out string? elementName, [NotNullWhen(true)] out ISet<string>? attributeNames)
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
                (elementName, attributes) = GetElementNameAndAttributes(token.Parent.Parent!);
            }
            else if (token.Parent is XmlAttributeSyntax(
                        SyntaxKind.XmlCrefAttribute or
                        SyntaxKind.XmlNameAttribute or
                        SyntaxKind.XmlTextAttribute) attributeSyntax)
            {
                // In the following, 'attr1' may be a regular text attribute, or one of the special 'cref' or 'name' attributes
                // <elem attr1="" $$
                // <elem attr1="" $$attr2	
                // <elem attr1="" attr2$$

                if (token == attributeSyntax.EndQuoteToken)
                {
                    (elementName, attributes) = GetElementNameAndAttributes(attributeSyntax.Parent!);
                }
            }

            attributeNames = attributes.Select(GetAttributeName).ToSet();
            return elementName != null;
        }

        private static (string? name, SyntaxList<XmlAttributeSyntax> attributes) GetElementNameAndAttributes(SyntaxNode node)
        {
            XmlNameSyntax? nameSyntax;
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
                    attributes = default;
                    break;
            }

            return (name: nameSyntax?.LocalName.ValueText, attributes);
        }

        private static bool IsAttributeValueContext(SyntaxToken token, [NotNullWhen(true)] out string? tagName, [NotNullWhen(true)] out string? attributeName)
        {
            XmlAttributeSyntax? attributeSyntax;
            if (token.Parent.IsKind(SyntaxKind.IdentifierName) &&
                token.Parent?.Parent is XmlNameAttributeSyntax xmlName)
            {
                // Handle the special 'name' attributes: name="bar$$
                attributeSyntax = xmlName;
            }
            else if (token.IsKind(SyntaxKind.XmlTextLiteralToken) &&
                     token.Parent is XmlTextAttributeSyntax xmlText)
            {
                // Handle the other general text attributes: foo="bar$$
                attributeSyntax = xmlText;
            }
            else if (token.Parent.IsKind(SyntaxKind.XmlNameAttribute, out attributeSyntax) ||
                     token.Parent.IsKind(SyntaxKind.XmlTextAttribute, out attributeSyntax))
            {
                // When there's no attribute value yet, the parent attribute is returned:
                //     name="$$
                //     foo="$$
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

        protected override IEnumerable<string> GetKeywordNames()
        {
            yield return SyntaxFacts.GetText(SyntaxKind.NullKeyword);
            yield return SyntaxFacts.GetText(SyntaxKind.StaticKeyword);
            yield return SyntaxFacts.GetText(SyntaxKind.VirtualKeyword);
            yield return SyntaxFacts.GetText(SyntaxKind.TrueKeyword);
            yield return SyntaxFacts.GetText(SyntaxKind.FalseKeyword);
            yield return SyntaxFacts.GetText(SyntaxKind.AbstractKeyword);
            yield return SyntaxFacts.GetText(SyntaxKind.SealedKeyword);
            yield return SyntaxFacts.GetText(SyntaxKind.AsyncKeyword);
            yield return SyntaxFacts.GetText(SyntaxKind.AwaitKeyword);
        }

        protected override IEnumerable<string> GetExistingTopLevelElementNames(DocumentationCommentTriviaSyntax syntax)
            => syntax.Content.Select(GetElementName).WhereNotNull();

        protected override IEnumerable<string?> GetExistingTopLevelAttributeValues(DocumentationCommentTriviaSyntax syntax, string elementName, string attributeName)
        {
            var attributeValues = SpecializedCollections.EmptyEnumerable<string?>();

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

        private string? GetElementName(XmlNodeSyntax node) => GetElementNameAndAttributes(node).name;

        private string GetAttributeName(XmlAttributeSyntax attribute) => attribute.Name.LocalName.ValueText;

        private string? GetAttributeValue(XmlAttributeSyntax attribute)
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

        protected override ImmutableArray<IParameterSymbol> GetParameters(ISymbol declarationSymbol)
        {
            var declaredParameters = declarationSymbol.GetParameters();
            if (declarationSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.TryGetPrimaryConstructor(out var primaryConstructor))
                {
                    declaredParameters = primaryConstructor.Parameters;
                }
                else if (namedTypeSymbol is { DelegateInvokeMethod.Parameters: var delegateInvokeParameters })
                {
                    declaredParameters = delegateInvokeParameters;
                }
            }

            return declaredParameters;
        }

        private static readonly CompletionItemRules s_defaultRules =
            CompletionItemRules.Create(
                filterCharacterRules: FilterRules,
                commitCharacterRules: ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, '>', '\t')),
                enterKeyRule: EnterKeyRule.Never);
    }
}
