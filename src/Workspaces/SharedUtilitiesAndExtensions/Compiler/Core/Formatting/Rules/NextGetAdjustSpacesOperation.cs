// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    [NonDefaultable]
    internal readonly struct NextGetAdjustSpacesOperation(
        ImmutableArray<AbstractFormattingRule> formattingRules,
        int index)
    {
        private NextGetAdjustSpacesOperation NextOperation
            => new(formattingRules, index + 1);

        public AdjustSpacesOperation? Invoke(in SyntaxToken previousToken, in SyntaxToken currentToken)
        {
            // If we have no remaining handlers to execute, then we'll execute our last handler
            if (index >= formattingRules.Length)
            {
                return null;
            }
            else
            {
                // Call the handler at the index, passing a continuation that will come back to here with index + 1
                return formattingRules[index].GetAdjustSpacesOperation(in previousToken, in currentToken, NextOperation);
            }
        }
    }
}
