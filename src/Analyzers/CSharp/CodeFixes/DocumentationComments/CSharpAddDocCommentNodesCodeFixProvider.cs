// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.DocumentationComments
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddDocCommentNodes), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class CSharpAddDocCommentNodesCodeFixProvider
        : AbstractAddDocCommentNodesCodeFixProvider<XmlElementSyntax, XmlNameAttributeSyntax, XmlTextSyntax, MemberDeclarationSyntax>
    {
        /// <summary>
        /// Parameter has no matching param tag in XML comment
        /// </summary>
        private const string CS1573 = nameof(CS1573);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpAddDocCommentNodesCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1573);

        protected override string NodeName { get; } = "param";

        protected override List<XmlNameAttributeSyntax> GetNameAttributes(XmlElementSyntax node)
            => node.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().ToList();

        protected override string GetValueFromNameAttribute(XmlNameAttributeSyntax attribute)
            => attribute.Identifier.Identifier.ValueText;

        protected override SyntaxNode? TryGetDocCommentNode(SyntaxTriviaList leadingTrivia)
        {
            var docCommentNodes = leadingTrivia.Where(f => f.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));

            foreach (var node in docCommentNodes)
            {
                var nodeStructure = node.GetStructure()!;
                var descendentXmlElements = nodeStructure.DescendantNodes().OfType<XmlElementSyntax>();

                if (descendentXmlElements.Any(element => GetXmlElementLocalName(element) == NodeName))
                    return nodeStructure;
            }

            return null;
        }

        protected override string GetXmlElementLocalName(XmlElementSyntax element)
            => element.StartTag.Name.LocalName.ValueText;

        protected override ImmutableArray<string> GetParameterNames(MemberDeclarationSyntax member)
        {
            var parameterList = (ParameterListSyntax?)member
                .DescendantNodes(descendIntoChildren: _ => true, descendIntoTrivia: false)
                .FirstOrDefault(f => f is ParameterListSyntax);

            return parameterList == null
                ? ImmutableArray<string>.Empty
                : parameterList.Parameters.SelectAsArray(s => s.Identifier.ValueText);
        }

        protected override XmlElementSyntax GetNewNode(string parameterName, bool isFirstNodeInComment)
        {
            // This is the simplest way of getting the XML node with the correct leading trivia
            // However, trying to add a `DocumentationCommentTriviaSyntax` to the node in the abstract
            // implementation causes an exception, so we have to add an XmlElementSyntax
            var newDocCommentNode = SyntaxFactory.DocumentationComment(SyntaxFactory.XmlParamElement(parameterName));
            var elementNode = (XmlElementSyntax)newDocCommentNode.ChildNodes().ElementAt(0);

            // return node on new line
            if (isFirstNodeInComment)
            {
                return elementNode.WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(Environment.NewLine));
            }

            return elementNode.WithLeadingTrivia(
                SyntaxFactory.ParseLeadingTrivia(Environment.NewLine)
                    .AddRange(elementNode.GetLeadingTrivia()));
        }
    }
}
