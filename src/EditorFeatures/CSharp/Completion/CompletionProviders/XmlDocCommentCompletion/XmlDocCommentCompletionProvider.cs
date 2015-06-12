// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.CompletionProviders.XmlDocCommentCompletion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders.XmlDocCommentCompletion
{
    [ExportCompletionProvider("DocCommentCompletionProvider", LanguageNames.CSharp)]
    internal partial class XmlDocCommentCompletionProvider : AbstractDocCommentCompletionProvider
    {
        public override bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            if ((ch == '"' || ch == ' ')
                && completionItem.DisplayText.Contains(ch))
            {
                return false;
            }

            return CompletionUtilities.IsCommitCharacter(completionItem, ch, textTypedSoFar) || ch == '>' || ch == '\t';
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return text[characterPosition] == '<';
        }

        public override bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar)
        {
            return false;
        }

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            if (triggerInfo.IsDebugger)
            {
                return null;
            }

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var parentTrivia = token.GetAncestor<DocumentationCommentTriviaSyntax>();

            if (parentTrivia == null)
            {
                return null;
            }

            var items = new List<CompletionItem>();
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var span = CompletionUtilities.GetTextChangeSpan(text, position);

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

            if (declaredSymbol != null)
            {
                items.AddRange(GetTagsForSymbol(declaredSymbol, span, parentTrivia, token));
            }

            if (token.Parent.Kind() == SyntaxKind.XmlEmptyElement || token.Parent.Kind() == SyntaxKind.XmlText ||
                (token.Parent.IsKind(SyntaxKind.XmlElementEndTag) && token.IsKind(SyntaxKind.GreaterThanToken)) ||
                (token.Parent.IsKind(SyntaxKind.XmlName) && token.Parent.IsParentKind(SyntaxKind.XmlEmptyElement)))
            {
                if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement)
                {
                    items.AddRange(GetNestedTags(span));
                }

                if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement && ((XmlElementSyntax)token.Parent.Parent).StartTag.Name.LocalName.ValueText == "list")
                {
                    items.AddRange(GetListItems(span));
                }

                if (token.Parent.IsParentKind(SyntaxKind.XmlEmptyElement) & token.Parent.Parent.IsParentKind(SyntaxKind.XmlElement))
                {
                    var element = (XmlElementSyntax)token.Parent.Parent.Parent;
                    if (element.StartTag.Name.LocalName.ValueText == "list")
                    {
                        items.AddRange(GetListItems(span));
                    }
                }

                if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement && ((XmlElementSyntax)token.Parent.Parent).StartTag.Name.LocalName.ValueText == "listheader")
                {
                    items.AddRange(GetListHeaderItems(span));
                }

                if (token.Parent.Parent is DocumentationCommentTriviaSyntax)
                {
                    items.AddRange(GetTopLevelSingleUseNames(parentTrivia, span));
                    items.AddRange(GetTopLevelRepeatableItems(span));
                }
            }

            if (token.Parent.Kind() == SyntaxKind.XmlElementStartTag)
            {
                var startTag = (XmlElementStartTagSyntax)token.Parent;

                if (token == startTag.GreaterThanToken && startTag.Name.LocalName.ValueText == "list")
                {
                    items.AddRange(GetListItems(span));
                }

                if (token == startTag.GreaterThanToken && startTag.Name.LocalName.ValueText == "listheader")
                {
                    items.AddRange(GetListHeaderItems(span));
                }
            }

            items.AddRange(GetAlwaysVisibleItems(span));
            return items;
        }

        private IEnumerable<CompletionItem> GetTopLevelSingleUseNames(DocumentationCommentTriviaSyntax parentTrivia, TextSpan span)
        {
            var names = new HashSet<string>(new[] { "summary", "remarks", "example", "completionlist" });

            RemoveExistingTags(parentTrivia, names, (x) => x.StartTag.Name.LocalName.ValueText);

            return names.Select(n => GetItem(n, span));
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

        private IEnumerable<CompletionItem> GetTagsForSymbol(ISymbol symbol, TextSpan filterSpan, DocumentationCommentTriviaSyntax trivia, SyntaxToken token)
        {
            if (symbol is IMethodSymbol)
            {
                return GetTagsForMethod((IMethodSymbol)symbol, filterSpan, trivia, token);
            }

            if (symbol is IPropertySymbol)
            {
                return GetTagsForProperty((IPropertySymbol)symbol, filterSpan, trivia);
            }

            if (symbol is INamedTypeSymbol)
            {
                return GetTagsForType((INamedTypeSymbol)symbol, filterSpan, trivia);
            }

            return SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }

        private IEnumerable<CompletionItem> GetTagsForType(INamedTypeSymbol symbol, TextSpan filterSpan, DocumentationCommentTriviaSyntax trivia)
        {
            var items = new List<CompletionItem>();

            var typeParameters = symbol.TypeParameters.Select(p => p.Name).ToSet();

            RemoveExistingTags(trivia, typeParameters, x => AttributeSelector(x, "typeparam"));

            items.AddRange(typeParameters.Select(t => new XmlItem(this,
                filterSpan,
                FormatParameter("typeparam", t))));
            return items;
        }

        private string AttributeSelector(XmlElementSyntax element, string attribute)
        {
            if (!element.StartTag.IsMissing && !element.EndTag.IsMissing)
            {
                var startTag = element.StartTag;
                var nameAttribute = startTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault(a => a.Name.LocalName.ValueText == "name");
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

        private IEnumerable<CompletionItem> GetTagsForProperty(IPropertySymbol symbol, TextSpan filterSpan, DocumentationCommentTriviaSyntax trivia)
        {
            var items = new List<CompletionItem>();

            var typeParameters = symbol.GetTypeArguments().Select(p => p.Name).ToSet();

            RemoveExistingTags(trivia, typeParameters, x => AttributeSelector(x, "typeparam"));

            items.AddRange(typeParameters.Select(t => new XmlItem(this, filterSpan, "typeparam", "name", t)));
            items.Add(new XmlItem(this, filterSpan, "value"));
            return items;
        }

        private IEnumerable<CompletionItem> GetTagsForMethod(IMethodSymbol symbol, TextSpan filterSpan, DocumentationCommentTriviaSyntax trivia, SyntaxToken token)
        {
            var items = new List<CompletionItem>();

            var parameters = symbol.GetParameters().Select(p => p.Name).ToSet();
            var typeParameters = symbol.TypeParameters.Select(t => t.Name).ToSet();

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

                // We're writing the name of a paramref or typeparamref
                if (parentElementName == "paramref")
                {
                    items.AddRange(parameters.Select(p => new XmlItem(this, filterSpan, p)));
                }
                else if (parentElementName == "typeparamref")
                {
                    items.AddRange(typeParameters.Select(t => new XmlItem(this, filterSpan, t)));
                }

                return items;
            }

            var returns = true;

            RemoveExistingTags(trivia, parameters, x => AttributeSelector(x, "param"));
            RemoveExistingTags(trivia, typeParameters, x => AttributeSelector(x, "typeparam"));

            foreach (var node in trivia.Content)
            {
                var element = node as XmlElementSyntax;
                if (element != null && !element.StartTag.IsMissing && !element.EndTag.IsMissing)
                {
                    var startTag = element.StartTag;

                    if (startTag.Name.LocalName.ValueText == "returns")
                    {
                        returns = false;
                        break;
                    }
                }
            }

            items.AddRange(parameters.Select(p => new XmlItem(this, filterSpan, FormatParameter("param", p))));
            items.AddRange(typeParameters.Select(t => new XmlItem(this, filterSpan, FormatParameter("typeparam", t))));

            if (returns && !symbol.ReturnsVoid)
            {
                items.Add(new XmlItem(this, filterSpan, "returns"));
            }

            return items;
        }
    }
}
