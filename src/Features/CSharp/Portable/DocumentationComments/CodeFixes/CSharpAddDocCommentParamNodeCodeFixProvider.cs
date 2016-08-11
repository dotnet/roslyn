// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddAttribute), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class CSharpAddDocCommentParamNodeCodeFixProvider
        : AbstractAddDocCommentParamNodeCodeFixProvider<XmlElementSyntax, XmlNameAttributeSyntax, XmlTextSyntax, MemberDeclarationSyntax, ParameterSyntax>
    {
        /// <summary>
        /// Parameter has no matching param tag in XML comment
        /// </summary>
        private const string CS1573 = nameof(CS1573);

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1573);

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
            if (member is BaseMethodDeclarationSyntax)
            {
                return ((BaseMethodDeclarationSyntax)member).ParameterList.Parameters.Select(s => s.Identifier.ValueText).ToList();
            }

            var parameterList = (ParameterListSyntax)member.DescendantNodes(descendIntoChildren: _ => true, descendIntoTrivia: false)
                                                           .FirstOrDefault(f => f is ParameterListSyntax);

            return parameterList != null
                   ? parameterList.Parameters.Select(s => s.Identifier.ValueText).ToList()
                   : new List<string>();
        }

        protected override string GetParameterName(ParameterSyntax parameter)
            => parameter.Identifier.ValueText;

        protected override XmlElementSyntax GetNewNode(string parameterName, bool isFirstNodeInComment)
        {
            var newDocCommentNode = SyntaxFactory.DocumentationComment(SyntaxFactory.XmlParamElement(parameterName));
            var elementNode = (XmlElementSyntax)newDocCommentNode.ChildNodes().ElementAt(0);

            // return node on new line
            return !isFirstNodeInComment
                ? elementNode.WithLeadingTrivia(
                    SyntaxFactory.ParseLeadingTrivia(Environment.NewLine)
                        .AddRange(elementNode.GetLeadingTrivia().Select(s => s)))
                : elementNode.WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(Environment.NewLine));
        }
    }
}