// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes
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

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1573);

        protected override string NodeName { get; } = "param";

        protected override List<XmlNameAttributeSyntax> GetNameAttributes(XmlElementSyntax node)
            => node.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().ToList();

        protected override string GetValueFromNameAttribute(XmlNameAttributeSyntax attribute)
            => attribute.Identifier.Identifier.ValueText;

        protected override SyntaxNode GetDocCommentNode(SyntaxTriviaList leadingTrivia)
            => leadingTrivia.FirstOrDefault(f => f.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).GetStructure();

        protected override string GetXmlElementLocalName(XmlElementSyntax element)
            => element.StartTag.Name.LocalName.ValueText;

        protected override List<string> GetParameterNames(MemberDeclarationSyntax member)
        {
            var parameterList = (ParameterListSyntax)member.DescendantNodes(descendIntoChildren: _ => true, descendIntoTrivia: false)
                                                           .FirstOrDefault(f => f is ParameterListSyntax);

            return parameterList != null
                   ? parameterList.Parameters.Select(s => s.Identifier.ValueText).ToList()
                   : new List<string>();
        }

        protected override XmlElementSyntax GetNewNode(string parameterName, bool isFirstNodeInComment)
        {
            var newDocCommentNode = SyntaxFactory.DocumentationComment(SyntaxFactory.XmlParamElement(parameterName));
            var elementNode = (XmlElementSyntax)newDocCommentNode.ChildNodes().ElementAt(0);

            // return node on new line
            return !isFirstNodeInComment
                ? elementNode.WithLeadingTrivia(
                    SyntaxFactory.ParseLeadingTrivia(Environment.NewLine)
                        .AddRange(elementNode.GetLeadingTrivia()))
                : elementNode.WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(Environment.NewLine));
        }
    }
}