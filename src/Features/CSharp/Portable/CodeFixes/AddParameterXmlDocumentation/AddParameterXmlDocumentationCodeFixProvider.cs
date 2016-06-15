// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddParameterXmlDocumentation
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddParameterXmlDocumentaion), Shared]
    internal partial class AddParameterXmlDocumentationCodeFixProvider : CodeFixProvider
    {
        // Parameter 'parameter' has no matching param tag in the XML comment for 'parameter' (but other parameters do)
        private const string CS1573 = nameof(CS1573);
        // Type parameter 'type parameter' has no matching typeparam tag in the XML comment on 'type' (but other type parameters do)
        private const string CS1712 = nameof(CS1712);

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS1573, CS1712);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private BaseParameterListSyntax FindFirstAncestorParameterItem(SyntaxNode node)
        {
            for (; node != null; node = node.Parent)
            {
                if (node is BaseMethodDeclarationSyntax)
                {
                    return ((BaseMethodDeclarationSyntax)node).ParameterList;
                }
                if (node is IndexerDeclarationSyntax)
                {
                    return ((IndexerDeclarationSyntax)node).ParameterList;
                }
                if (node is DelegateDeclarationSyntax)
                {
                    return ((DelegateDeclarationSyntax)node).ParameterList;
                }
            }
            return null;
        }

        // Can't use the generic GetAncestor method because structs and classes can nest each other.
        private TypeParameterListSyntax FindFirstAncestorTypeParameterItem(SyntaxNode node)
        {
            for (; node != null; node = node.Parent)
            {
                if (node is MethodDeclarationSyntax)
                {
                    return ((MethodDeclarationSyntax)node).TypeParameterList;
                }
                if (node is ClassDeclarationSyntax)
                {
                    return ((ClassDeclarationSyntax)node).TypeParameterList;
                }
                if (node is StructDeclarationSyntax)
                {
                    return ((StructDeclarationSyntax)node).TypeParameterList;
                }
                if (node is InterfaceDeclarationSyntax)
                {
                    return ((InterfaceDeclarationSyntax)node).TypeParameterList;
                }
                if (node is DelegateDeclarationSyntax)
                {
                    return ((DelegateDeclarationSyntax)node).TypeParameterList;
                }
            }
            return null;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO many CodeFixProvider seem to do that and it works. Shouldn't there be a loop over all of the fixes?
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;


            if (diagnostic.Id == CS1573)
            {
                var parameterList = FindFirstAncestorParameterItem(root.FindToken(diagnosticSpan.Start).Parent);
                if (parameterList == null) { return; }

                var parameterSyntax = root.FindToken(diagnosticSpan.Start).GetAncestor<ParameterSyntax>();
                if (parameterSyntax == null) { return; }
                var parameterName = parameterSyntax.Identifier.ValueText;

                XmlNameAttributeSyntax adjacentAttribute;
                bool adjacentAttributeIsBefore;
                FindAdjacentParameterDocumentation(
                    parameterName,
                    allParameterNames: parameterList.Parameters.Select(p => p.Identifier.ValueText),
                    tokenWithDocumentationComment: parameterList.Parent.GetFirstToken(),
                    elementKind: XmlNameAttributeElementKind.Parameter,
                    adjacentNameAttribute: out adjacentAttribute,
                    adjacentAttributeIsBefore: out adjacentAttributeIsBefore);

                if (adjacentAttribute == null) { return; }

                context.RegisterCodeFix(
                    new AddParameterXmlDocumentationCodeAction(context.Document, parameterName, adjacentAttribute,
                        adjacentAttributeIsBefore, isTypeParameter: false),
                    diagnostic);
            }
            else if (diagnostic.Id == CS1712)
            {
                var parameterList = FindFirstAncestorTypeParameterItem(root.FindToken(diagnosticSpan.Start).Parent);
                if (parameterList == null) { return; }

                var parameterSyntax = root.FindToken(diagnosticSpan.Start).GetAncestor<TypeParameterSyntax>();
                if (parameterSyntax == null) { return; }
                var parameterName = parameterSyntax.Identifier.ValueText;

                XmlNameAttributeSyntax adjacentAttribute;
                bool adjacentAttributeIsBefore;
                FindAdjacentParameterDocumentation(
                    parameterName,
                    allParameterNames: parameterList.Parameters.Select(p => p.Identifier.ValueText),
                    tokenWithDocumentationComment: parameterList.Parent.GetFirstToken(),
                    elementKind: XmlNameAttributeElementKind.TypeParameter,
                    adjacentNameAttribute: out adjacentAttribute,
                    adjacentAttributeIsBefore: out adjacentAttributeIsBefore);

                if (adjacentAttribute == null) { return; }

                context.RegisterCodeFix(
                    new AddParameterXmlDocumentationCodeAction(context.Document, parameterName, adjacentAttribute,
                        adjacentAttributeIsBefore, isTypeParameter: true),
                    diagnostic);
            }
        }

        /// <summary>
        /// Finds a xml documentation tag with a name attribute at which another a new tag can be inserted.
        /// </summary>
        /// <param name="parameterName">The parameter of the new tag</param>
        /// <param name="allParameterNames">All the parameter of which <paramref name="parameterName"/> is one.</param>
        /// <param name="tokenWithDocumentationComment"></param>
        /// <param name="elementKind">Specifies which elements can be found.</param>
        /// <param name="adjacentNameAttribute">The name attribute of the adjacent parameter. If null no parameter was found.</param>
        /// <param name="adjacentAttributeIsBefore">Indicates if the found adjacent attribute is before or after the parameter.</param>
        private static void FindAdjacentParameterDocumentation(string parameterName, IEnumerable<string> allParameterNames, SyntaxToken tokenWithDocumentationComment,
            XmlNameAttributeElementKind elementKind, out XmlNameAttributeSyntax adjacentNameAttribute, out bool adjacentAttributeIsBefore)
        {
            FindAdjacentParameterDocumentationWalker.FindAdjacentParameterDocumenteden(parameterName, allParameterNames, tokenWithDocumentationComment,
                elementKind, out adjacentNameAttribute, out adjacentAttributeIsBefore);
        }

        private class FindAdjacentParameterDocumentationWalker : CSharpSyntaxWalker
        {
            private FindAdjacentParameterDocumentationWalker(int parameterIndex, Dictionary<string, int> parameterIndices, XmlNameAttributeElementKind elementKind)
                : base(SyntaxWalkerDepth.StructuredTrivia)
            {
                _parameterIndex = parameterIndex;
                _parameterIndices = parameterIndices;
                _elementKind = elementKind;
            }

            private int _parameterIndex = -1;
            private Dictionary<string, int> _parameterIndices = new Dictionary<string, int>();

            private XmlNameAttributeSyntax _documentedParameterBefore = null;
            private int _documentedParameterBeforeIndex = -1;
            private XmlNameAttributeSyntax _documentedParameterAfter = null;
            private int _documentedParameterAfterIndex = int.MaxValue;

            private XmlNameAttributeElementKind _elementKind;

            public static void FindAdjacentParameterDocumenteden(string parameterName, IEnumerable<string> allParameterNames,
                SyntaxToken tokenWithDocumentationComment, XmlNameAttributeElementKind elementKind, out XmlNameAttributeSyntax adjacentNameAttribute, out bool adjacentAttributeIsBefore)
            {
                var parameterIndices = new Dictionary<string, int>();
                int index = 0;
                foreach (var param in allParameterNames)
                {
                    parameterIndices.Add(param, index);
                    index++;
                }
                if (!parameterIndices.ContainsKey(parameterName))
                {
                    // something went very wrong
                    Debug.Assert(false); // TODO message or remove assert
                    adjacentNameAttribute = null;
                    adjacentAttributeIsBefore = false;
                    return;
                }

                var walker = new FindAdjacentParameterDocumentationWalker(parameterIndices[parameterName], parameterIndices, elementKind);
                walker.VisitLeadingTrivia(tokenWithDocumentationComment);

                if (walker._documentedParameterBefore != null)
                {
                    adjacentNameAttribute = walker._documentedParameterBefore;
                    adjacentAttributeIsBefore = true;
                }
                else
                {
                    adjacentNameAttribute = walker._documentedParameterAfter;
                    adjacentAttributeIsBefore = false;
                }
            }

            public override void VisitXmlNameAttribute(XmlNameAttributeSyntax node)
            {
                if (node.GetElementKind() == _elementKind)
                {
                    int index;
                    if (_parameterIndices.TryGetValue(node.Identifier.Identifier.ValueText, out index))
                    {
                        if (_documentedParameterBeforeIndex < index & index < _documentedParameterAfterIndex)
                        {
                            if (index < _parameterIndex)
                            {
                                _documentedParameterBefore = node;
                                _documentedParameterBeforeIndex = index;
                            }
                            if (_parameterIndex < index)
                            {
                                _documentedParameterAfter = node;
                                _documentedParameterAfterIndex = index;
                            }
                        }
                    }
                    else if (_documentedParameterAfter == null)
                    {
                        // in case all the param or typeparam tags that exist are not a parameter
                        _documentedParameterAfter = node;
                    }
                }
                base.VisitXmlNameAttribute(node);
            }
        }

        private class AddParameterXmlDocumentationCodeAction : CodeAction
        {
            private string _title;
            private Document _document;
            private string _parameterName;
            private XmlNameAttributeSyntax _adjacentAttribute;
            private bool _adjacentAttributeIsBefore;
            private bool _isTypeParameter;

            public override string Title => _title;

            public AddParameterXmlDocumentationCodeAction(Document document, string parameterName, XmlNameAttributeSyntax adjacentAttribute,
                bool adjacentAttributeIsBefore, bool isTypeParameter)
            {
                if (isTypeParameter)
                {
                    _title = string.Format(CSharpFeaturesResources.AddTypeParameterXmlDocumentation, parameterName);
                }
                else
                {
                    _title = string.Format(CSharpFeaturesResources.AddParameterXmlDocumentation, parameterName);
                }
                _document = document;
                _parameterName = parameterName;
                _adjacentAttribute = adjacentAttribute;
                _adjacentAttributeIsBefore = adjacentAttributeIsBefore;
                _isTypeParameter = isTypeParameter;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // TODO Handle (or disallow) /** */ comments
                var newLineAndSpace = GetXmlCommentNewLineAndSpace("///");
                var elementName = _isTypeParameter ? "typeparam" : "param";
                var newDocumentation = GetNewDocumentationParamElement(_parameterName, elementName, _adjacentAttribute);

                if (_adjacentAttributeIsBefore)
                {
                    root = root.InsertNodesAfter(_adjacentAttribute.Parent.Parent,
                        new SyntaxNode[] { newLineAndSpace, newDocumentation });
                }
                else
                {
                    root = root.InsertNodesBefore(_adjacentAttribute.Parent.Parent,
                        new SyntaxNode[] { newDocumentation, newLineAndSpace });
                }

                return _document.WithSyntaxRoot(root);
            }

            private static XmlTextSyntax GetXmlCommentNewLineAndSpace(string commentPrefix)
            {
                var newlineToken = SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.XmlTextLiteralNewLineToken, "\n", "\n", default(SyntaxTriviaList));
                var documentationComment = SyntaxFactory.SyntaxTrivia(SyntaxKind.DocumentationCommentExteriorTrivia, commentPrefix);
                var spaceToken = SyntaxFactory.Token(SyntaxTriviaList.Create(documentationComment), SyntaxKind.XmlTextLiteralToken, " ", " ", default(SyntaxTriviaList));
                return SyntaxFactory.XmlText(SyntaxFactory.TokenList(newlineToken, spaceToken));
            }

            private static XmlElementSyntax GetNewDocumentationParamElement(string parameterName, string elementName,
                XmlNameAttributeSyntax referenceDocumentationAttribute)
            {
                var quote = referenceDocumentationAttribute.StartQuoteToken;
                var nameAttribute = SyntaxFactory.XmlNameAttribute(referenceDocumentationAttribute.Name, quote, parameterName, quote);

                var xmlElementName = SyntaxFactory.XmlName(elementName);
                var startTag = SyntaxFactory.XmlElementStartTag(xmlElementName,
                    SyntaxFactory.List(Enumerable.Repeat((XmlAttributeSyntax)nameAttribute, 1)));
                var endTag = SyntaxFactory.XmlElementEndTag(xmlElementName);
                return SyntaxFactory.XmlElement(startTag, endTag);
            }
        }
    }
}
