// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class AbstractFormatEngine
    {
        private class Partitioner
        {
            private const int MinimumItemsPerPartition = 30000;

            private readonly FormattingContext context;
            private readonly TokenStream tokenStream;
            private readonly TokenPairWithOperations[] operationPairs;

            public Partitioner(FormattingContext context, TokenStream tokenStream, TokenPairWithOperations[] operationPairs)
            {
                Contract.ThrowIfNull(context);
                Contract.ThrowIfNull(tokenStream);
                Contract.ThrowIfNull(operationPairs);

                this.context = context;
                this.tokenStream = tokenStream;
                this.operationPairs = operationPairs;
            }

            public List<IEnumerable<TokenPairWithOperations>> GetPartitions(int partitionCount)
            {
                Contract.ThrowIfFalse(partitionCount > 0);

                var list = new List<IEnumerable<TokenPairWithOperations>>();

                // too small items in the list. give out one list
                int perPartition = this.operationPairs.Length / partitionCount;
                if (perPartition < 10 || partitionCount <= 1 || this.operationPairs.Length < MinimumItemsPerPartition)
                {
                    list.Add(GetOperationPairsFromTo(0, operationPairs.Length));
                    return list;
                }

                // split items up to the partition count with about same number of items if possible
                // this algorithm has one problem. if there is an operation that marked whole tree
                // as inseparable region, then it wouldn't go into the inseparable regions to find
                // local parts that could run concurrently; which means everything will run
                // synchronously.
                var currentOperationIndex = 0;
                while (currentOperationIndex < this.operationPairs.Length)
                {
                    var nextPartitionStartOperationIndex = Math.Min(currentOperationIndex + perPartition, this.operationPairs.Length);
                    if (nextPartitionStartOperationIndex >= this.operationPairs.Length)
                    {
                        // reached end of operation pairs
                        list.Add(GetOperationPairsFromTo(currentOperationIndex, this.operationPairs.Length));
                        break;
                    }

                    var nextToken = this.context.GetEndTokenForRelativeIndentationSpan(this.operationPairs[nextPartitionStartOperationIndex].Token1);
                    if (nextToken.RawKind == 0)
                    {
                        // reached the last token in the tree
                        list.Add(GetOperationPairsFromTo(currentOperationIndex, this.operationPairs.Length));
                        break;
                    }

                    var nextTokenWithIndex = this.tokenStream.GetTokenData(nextToken);
                    if (nextTokenWithIndex.IndexInStream < 0)
                    {
                        // first token for next partition is out side of valid token stream
                        list.Add(GetOperationPairsFromTo(currentOperationIndex, this.operationPairs.Length));
                        break;
                    }

                    Contract.ThrowIfFalse(currentOperationIndex < nextTokenWithIndex.IndexInStream);
                    Contract.ThrowIfFalse(nextTokenWithIndex.IndexInStream <= this.operationPairs.Length);

                    list.Add(GetOperationPairsFromTo(currentOperationIndex, nextTokenWithIndex.IndexInStream));
                    currentOperationIndex = nextTokenWithIndex.IndexInStream;
                }

                return list;
            }

            private IEnumerable<TokenPairWithOperations> GetOperationPairsFromTo(int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    yield return operationPairs[i];
                }
            }
        }
    }
}
