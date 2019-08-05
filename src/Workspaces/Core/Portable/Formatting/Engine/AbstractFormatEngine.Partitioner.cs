// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class AbstractFormatEngine
    {
        private class Partitioner
        {
            private const int MinimumItemsPerPartition = 30000;

            private readonly FormattingContext _context;
            private readonly TokenPairWithOperations[] _operationPairs;

            public Partitioner(FormattingContext context, TokenPairWithOperations[] operationPairs)
            {
                Contract.ThrowIfNull(context);
                Contract.ThrowIfNull(operationPairs);

                _context = context;
                _operationPairs = operationPairs;
            }

            public List<IEnumerable<TokenPairWithOperations>> GetPartitions(int partitionCount, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.Formatting_Partitions, cancellationToken))
                {
                    Contract.ThrowIfFalse(partitionCount > 0);

                    var list = new List<IEnumerable<TokenPairWithOperations>>();

                    // too small items in the list. give out one list
                    var perPartition = _operationPairs.Length / partitionCount;
                    if (perPartition < 10 || partitionCount <= 1 || _operationPairs.Length < MinimumItemsPerPartition)
                    {
                        list.Add(GetOperationPairsFromTo(0, _operationPairs.Length));
                        return list;
                    }

                    // split items up to the partition count with about same number of items if possible
                    // this algorithm has one problem. if there is an operation that marked whole tree
                    // as inseparable region, then it wouldn't go into the inseparable regions to find
                    // local parts that could run concurrently; which means everything will run
                    // synchronously.
                    var currentOperationIndex = 0;
                    while (currentOperationIndex < _operationPairs.Length)
                    {
                        if (!TryGetNextPartitionIndex(currentOperationIndex, perPartition, out var nextPartitionStartOperationIndex))
                        {
                            // reached end of operation pairs
                            list.Add(GetOperationPairsFromTo(currentOperationIndex, _operationPairs.Length));
                            break;
                        }

                        var nextToken = GetNextPartitionToken(nextPartitionStartOperationIndex, perPartition, cancellationToken);
                        if (nextToken.RawKind == 0)
                        {
                            // reached the last token in the tree
                            list.Add(GetOperationPairsFromTo(currentOperationIndex, _operationPairs.Length));
                            break;
                        }

                        var nextTokenWithIndex = _context.TokenStream.GetTokenData(nextToken);
                        if (nextTokenWithIndex.IndexInStream < 0)
                        {
                            // first token for next partition is out side of valid token stream
                            list.Add(GetOperationPairsFromTo(currentOperationIndex, _operationPairs.Length));
                            break;
                        }

                        Contract.ThrowIfFalse(currentOperationIndex < nextTokenWithIndex.IndexInStream);
                        Contract.ThrowIfFalse(nextTokenWithIndex.IndexInStream <= _operationPairs.Length);

                        list.Add(GetOperationPairsFromTo(currentOperationIndex, nextTokenWithIndex.IndexInStream));
                        currentOperationIndex = nextTokenWithIndex.IndexInStream;
                    }

                    return list;
                }
            }

            private SyntaxToken GetNextPartitionToken(int index, int perPartition, CancellationToken cancellationToken)
            {
                while (true)
                {
                    if (_context.TryGetEndTokenForRelativeIndentationSpan(_operationPairs[index].Token1, 10, out var nextToken, cancellationToken))
                    {
                        return nextToken;
                    }

                    // we couldn't determine how to split chunks in short time period. make partition bigger
                    if (!TryGetNextPartitionIndex(index, perPartition, out index))
                    {
                        // reached end of operation pairs
                        return default;
                    }
                }
            }

            private bool TryGetNextPartitionIndex(int index, int perPartition, out int nextIndex)
            {
                nextIndex = Math.Min(index + perPartition, _operationPairs.Length);
                return nextIndex < _operationPairs.Length;
            }

            private IEnumerable<TokenPairWithOperations> GetOperationPairsFromTo(int from, int to)
            {
                for (var i = from; i < to; i++)
                {
                    yield return _operationPairs[i];
                }
            }
        }
    }
}
