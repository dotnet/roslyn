// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal partial class AbstractMetadataAsSourceService
{
    protected abstract class AbstractMetadataFormattingRule : AbstractFormattingRule
    {
        protected abstract AdjustNewLinesOperation GetAdjustNewLinesOperationBetweenMembersAndUsings(SyntaxToken token1, SyntaxToken token2);
        protected abstract bool IsNewLine(char c);

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(
                in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            if (previousToken.RawKind == 0 || currentToken.RawKind == 0)
            {
                nextOperation.Invoke(in previousToken, in currentToken);
            }

            var betweenMembersAndUsingsOperation = GetAdjustNewLinesOperationBetweenMembersAndUsings(previousToken, currentToken);
            if (betweenMembersAndUsingsOperation != null)
            {
                return betweenMembersAndUsingsOperation;
            }

            return nextOperation.Invoke(in previousToken, in currentToken);
        }

        protected int GetNumberOfLines(IEnumerable<SyntaxTrivia> triviaList)
        {
            var count = 0;
            var inElasticTriviaRun = false;

            // If we have a run of elastic trivia, that would get collapsed into one line.
            foreach (var trivia in triviaList)
            {
                if (trivia.IsElastic())
                {
                    if (!inElasticTriviaRun)
                    {
                        count += 1;
                    }

                    inElasticTriviaRun = true;
                    continue;
                }

                inElasticTriviaRun = false;
                count += trivia.ToFullString().Replace("\r\n", "\r").ToCharArray().Count(IsNewLine);
            }

            return count;
        }
    }
}
