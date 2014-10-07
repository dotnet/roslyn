// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
#if MEF
    [ExportFormattingRule(Name, LanguageNames.CSharp)]
#endif
    internal class AlignTokensFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Align Tokens Formatting Rule";

        public override void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AlignTokensOperation> nextOperation)
        {
            nextOperation.Invoke(list);
            var syntaxNode = node;

            var bracePair = node.GetBracePair();

            if (!bracePair.IsValidBracePair())
            {
                return;
            }

            if (syntaxNode.IsLambdaBodyBlock() ||
                syntaxNode.IsAnonymousMethodBlock() ||
                node is InitializerExpressionSyntax)
            {
                AddAlignIndentationOfTokensToFirstTokenOfBaseTokenLineOperation(list, syntaxNode, bracePair.Item1, SpecializedCollections.SingletonEnumerable((SyntaxToken)bracePair.Item2));
            }
        }
    }
}
