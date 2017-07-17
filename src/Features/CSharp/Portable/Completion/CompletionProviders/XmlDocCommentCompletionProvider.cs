﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    internal partial class XmlDocCommentCompletionProvider : AbstractDocCommentCompletionProvider
    {
        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return text[characterPosition] == '<';
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

            var items = new List<CompletionItem>();

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

            // User is trying to write a name, try to suggest only names.
            if (token.Parent.IsKind(SyntaxKind.XmlNameAttribute) ||
                (token.Parent.IsKind(SyntaxKind.IdentifierName) && token.Parent.IsParentKind(SyntaxKind.XmlNameAttribute)))
            {
                string parentElementName = null;

                var emptyElement = token.GetAncestor<XmlEmptyElementSyntax>();
                if (emptyElement != null)
                {
                    parentElementName = emptyElement.Name.LocalName.Text;
                }

                if (parentElementName == ParamRefTagName)
                {
                    return GetParamNameItems(declaredSymbol);
                }
                else if (parentElementName == TypeParamRefTagName)
                {
                    return GetTypeParamNameItems(declaredSymbol);
                }
            }

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
                    items.AddRange(GetTopLevelSingleUseNames(parentTrivia));
                    items.AddRange(GetTopLevelRepeatableItems());
                    items.AddRange(GetTagsForSymbol(declaredSymbol, parentTrivia));
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

        private IEnumerable<CompletionItem> GetTopLevelSingleUseNames(DocumentationCommentTriviaSyntax parentTrivia)
        {
            var names = new HashSet<string>(new[] { SummaryTagName, RemarksTagName, ExampleTagName, CompletionListTagName });

            RemoveExistingTags(parentTrivia, names, x => x.StartTag.Name.LocalName.ValueText);

            return names.Select(GetItem);
        }

        private void RemoveExistingTags(DocumentationCommentTriviaSyntax parentTrivia, ISet<string> names, Func<XmlElementSyntax, string> selector)
        {
            if (parentTrivia != null)
            {
                foreach (var node in parentTrivia.Content)
                {
                    var element = node as XmlElementSyntax;
                    if (element != null)
                    {
                        names.Remove(selector(element));
                    }
                }
            }
        }

        private IEnumerable<CompletionItem> GetTagsForSymbol(ISymbol symbol, DocumentationCommentTriviaSyntax trivia)
        {
            if (symbol is IMethodSymbol method)
            {
                return GetTagsForMethod(method, trivia);
            }

            if (symbol is IPropertySymbol property)
            {
                return GetTagsForProperty(property, trivia);
            }

            if (symbol is INamedTypeSymbol namedType)
            {
                return GetTagsForType(namedType, trivia);
            }

            return SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }

        private IEnumerable<CompletionItem> GetTagsForType(INamedTypeSymbol symbol, DocumentationCommentTriviaSyntax trivia)
        {
            var items = new List<CompletionItem>();

            var typeParameters = symbol.TypeParameters.Select(p => p.Name).ToSet();

            RemoveExistingTags(trivia, typeParameters, x => AttributeSelector(x, TypeParamTagName));

            items.AddRange(typeParameters.Select(t => CreateCompletionItem(FormatParameter(TypeParamTagName, t))));
            return items;
        }

        private string AttributeSelector(XmlElementSyntax element, string attribute)
        {
            if (!element.StartTag.IsMissing && !element.EndTag.IsMissing)
            {
                var startTag = element.StartTag;
                var nameAttribute = startTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault(a => a.Name.LocalName.ValueText == NameAttributeName);
                if (nameAttribute != null)
                {
                    if (startTag.Name.LocalName.ValueText == attribute)
                    {
                        return nameAttribute.Identifier.Identifier.ValueText;
                    }
                }
            }

            return null;
        }

        private IEnumerable<CompletionItem> GetTagsForProperty(IPropertySymbol symbol, DocumentationCommentTriviaSyntax trivia)
        {
            var items = new List<CompletionItem>();

            if (symbol.IsIndexer)
            {
                var parameters = symbol.GetParameters().Select(p => p.Name).ToSet();
                RemoveExistingTags(trivia, parameters, x => AttributeSelector(x, ParamTagName));
                items.AddRange(parameters.Select(p => CreateCompletionItem(FormatParameter(ParamTagName, p))));
            }

            var typeParameters = symbol.GetTypeArguments().Select(p => p.Name).ToSet();
            items.AddRange(typeParameters.Select(t => CreateCompletionItem(TypeParamTagName, NameAttributeName, t)));
            items.Add(CreateCompletionItem("value"));
            return items;
        }

        private IEnumerable<CompletionItem> GetTagsForMethod(IMethodSymbol symbol, DocumentationCommentTriviaSyntax trivia)
        {
            var items = new List<CompletionItem>();

            var parameters = symbol.GetParameters().Select(p => p.Name).ToSet();
            var typeParameters = symbol.TypeParameters.Select(t => t.Name).ToSet();

            RemoveExistingTags(trivia, parameters, x => AttributeSelector(x, ParamTagName));
            RemoveExistingTags(trivia, typeParameters, x => AttributeSelector(x, TypeParamTagName));

            items.AddRange(parameters.Select(p => CreateCompletionItem(FormatParameter(ParamTagName, p))));
            items.AddRange(typeParameters.Select(t => CreateCompletionItem(FormatParameter(TypeParamTagName, t))));

            // Provide a return completion item in case the function returns something
            var returns = true;

            foreach (var node in trivia.Content)
            {
                var element = node as XmlElementSyntax;
                if (element != null && !element.StartTag.IsMissing && !element.EndTag.IsMissing)
                {
                    var startTag = element.StartTag;

                    if (startTag.Name.LocalName.ValueText == ReturnsTagName)
                    {
                        returns = false;
                        break;
                    }
                }
            }

            if (returns && !symbol.ReturnsVoid)
            {
                items.Add(CreateCompletionItem(ReturnsTagName));
            }

            return items;
        }

        protected IEnumerable<CompletionItem> GetParamNameItems(ISymbol declaredSymbol)
        {
            var items = declaredSymbol?.GetParameters()
                                       .Select(parameter => CreateCompletionItem(parameter.Name));

            return items ?? SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }

        protected IEnumerable<CompletionItem> GetTypeParamNameItems(ISymbol declaredSymbol)
        {
            var items = declaredSymbol?.GetTypeParameters()
                                       .Select(typeParameter => CreateCompletionItem(typeParameter.Name));

            return items ?? SpecializedCollections.EmptyEnumerable<CompletionItem>();
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
