// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class IndentUserSettingsFormattingRule : BaseFormattingRule
    {
        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextIndentBlockOperationAction nextOperation)
        {
            nextOperation.Invoke();

            var bracePair = node.GetBracePair();

            // don't put block indentation operation if the block only contains lambda expression body block
            if (node.IsLambdaBodyBlock() || !bracePair.IsValidBracePair())
            {
                return;
            }

            if (options.GetOption(CSharpFormattingOptions.IndentBraces))
            {
                AddIndentBlockOperation(list, bracePair.Item1, bracePair.Item1, bracePair.Item1.Span);
                AddIndentBlockOperation(list, bracePair.Item2, bracePair.Item2, bracePair.Item2.Span);
            }
        }
    }
}
