// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class IndentUserSettingsFormattingRule : BaseFormattingRule
    {
        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, in NextIndentBlockOperationAction nextOperation)
        {
            nextOperation.Invoke();

            var bracePair = node.GetBracePair();

            // don't put block indentation operation if the block only contains lambda expression body block
            if (node.IsLambdaBodyBlock() || !bracePair.IsValidBracePair())
            {
                return;
            }

            if (optionSet.GetOption(CSharpFormattingOptions.IndentBraces))
            {
                AddIndentBlockOperation(list, bracePair.Item1, bracePair.Item1, bracePair.Item1.Span);
                AddIndentBlockOperation(list, bracePair.Item2, bracePair.Item2, bracePair.Item2.Span);
            }
        }
    }
}
