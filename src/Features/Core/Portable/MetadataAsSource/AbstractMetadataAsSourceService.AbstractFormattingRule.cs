// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        protected abstract class AbstractFormattingRule : Formatting.Rules.AbstractFormattingRule
        {
            protected abstract AdjustNewLinesOperation GetAdjustNewLinesOperationBetweenMembersAndUsings(SyntaxToken token1, SyntaxToken token2);
            protected abstract bool IsNewLine(char c);

            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(
                    SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
            {
                if (previousToken.RawKind == 0 || currentToken.RawKind == 0)
                {
                    nextOperation.Invoke();
                }

                var betweenMembersAndUsingsOperation = GetAdjustNewLinesOperationBetweenMembersAndUsings(previousToken, currentToken);
                if (betweenMembersAndUsingsOperation != null)
                {
                    return betweenMembersAndUsingsOperation;
                }

                return nextOperation.Invoke();
            }

            protected int GetNumberOfLines(IEnumerable<SyntaxTrivia> triviaList)
            {
                var count = 0;
                bool inElasticTriviaRun = false;

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
}
