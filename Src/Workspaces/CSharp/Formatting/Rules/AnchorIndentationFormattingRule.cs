// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
#if MEF
    [ExportFormattingRule(Name, LanguageNames.CSharp)]
    [ExtensionOrder(After = SuppressFormattingRule.Name)]
#endif
    internal class AnchorIndentationFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Anchor Indentation Formatting Rule";

        public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AnchorIndentationOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            var block = node as BlockSyntax;
            if (block != null && block.Parent is BlockSyntax)
            {
                // if it is not nested block, then its anchor will be first token that this block is
                // associated with. otherwise, "{" of block is the anchor token its children would follow
                var startToken = block.GetFirstToken(includeZeroWidth: true);
                var lastToken = block.GetLastToken(includeZeroWidth: true);

                AddAnchorIndentationOperation(list, startToken, lastToken);
                return;
            }

            var statement = node as StatementSyntax;
            if (statement != null)
            {
                var startToken = statement.GetFirstToken(includeZeroWidth: true);
                var lastToken = statement.GetLastToken(includeZeroWidth: true);

                AddAnchorIndentationOperation(list, startToken, lastToken);
                return;
            }

            var usingNode = node as UsingDirectiveSyntax;
            if (usingNode != null)
            {
                var startToken = usingNode.GetFirstToken(includeZeroWidth: true);
                var lastToken = usingNode.GetLastToken(includeZeroWidth: true);
                AddAnchorIndentationOperation(list, startToken, lastToken);
                return;
            }

            var namespaceNode = node as NamespaceDeclarationSyntax;
            if (namespaceNode != null)
            {
                var startToken = namespaceNode.GetFirstToken(includeZeroWidth: true);
                var lastToken = namespaceNode.GetLastToken(includeZeroWidth: true);
                AddAnchorIndentationOperation(list, startToken, lastToken);
                return;
            }

            var typeNode = node as TypeDeclarationSyntax;
            if (typeNode != null)
            {
                var startToken = typeNode.GetFirstToken(includeZeroWidth: true);
                var lastToken = typeNode.GetLastToken(includeZeroWidth: true);
                AddAnchorIndentationOperation(list, startToken, lastToken);
                return;
            }

            var memberDeclNode = node as MemberDeclarationSyntax;
            if (memberDeclNode != null)
            {
                var startToken = memberDeclNode.GetFirstToken(includeZeroWidth: true);
                var lastToken = memberDeclNode.GetLastToken(includeZeroWidth: true);
                AddAnchorIndentationOperation(list, startToken, lastToken);
                return;
            }

            var accessorDeclNode = node as AccessorDeclarationSyntax;
            if (accessorDeclNode != null)
            {
                var startToken = accessorDeclNode.GetFirstToken(includeZeroWidth: true);
                var lastToken = accessorDeclNode.GetLastToken(includeZeroWidth: true);
                AddAnchorIndentationOperation(list, startToken, lastToken);
                return;
            }
        }
    }
}