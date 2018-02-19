﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [ExportFormattingRule(Name, LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = SuppressFormattingRule.Name)]
    internal class AnchorIndentationFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Anchor Indentation Formatting Rule";

        public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AnchorIndentationOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            if (node.IsKind(SyntaxKind.SimpleLambdaExpression) || node.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
            {
                AddAnchorIndentationOperation(list, node);
                return;
            }

            if (node.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                AddAnchorIndentationOperation(list, node);
                return;
            }

            if (node is BlockSyntax block)
            {
                // if it is not nested block, then its anchor will be first token that this block is
                // associated with. otherwise, "{" of block is the anchor token its children would follow
                if (block.Parent == null || block.Parent is BlockSyntax)
                {
                    AddAnchorIndentationOperation(list, block);
                    return;
                }
                else
                {
                    AddAnchorIndentationOperation(list,
                        block.Parent.GetFirstToken(includeZeroWidth: true),
                        block.GetLastToken(includeZeroWidth: true));
                    return;
                }
            }

            switch (node)
            {
                case StatementSyntax statement:
                    AddAnchorIndentationOperation(list, statement);
                    return;
                case UsingDirectiveSyntax usingNode:
                    AddAnchorIndentationOperation(list, usingNode);
                    return;
                case NamespaceDeclarationSyntax namespaceNode:
                    AddAnchorIndentationOperation(list, namespaceNode);
                    return;
                case TypeDeclarationSyntax typeNode:
                    AddAnchorIndentationOperation(list, typeNode);
                    return;
                case MemberDeclarationSyntax memberDeclNode:
                    AddAnchorIndentationOperation(list, memberDeclNode);
                    return;
                case AccessorDeclarationSyntax accessorDeclNode:
                    AddAnchorIndentationOperation(list, accessorDeclNode);
                    return;
            }
        }

        private void AddAnchorIndentationOperation(List<AnchorIndentationOperation> list, SyntaxNode node)
        {
            AddAnchorIndentationOperation(list, node.GetFirstToken(includeZeroWidth: true), node.GetLastToken(includeZeroWidth: true));
        }
    }
}
