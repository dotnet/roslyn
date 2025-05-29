// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.SymbolUsageAnalysis;

/// <summary>
/// Analysis to compute all the symbol writes for local and parameter
/// symbols in an executable code block, along with the information of whether or not the definition
/// may be read on some control flow path.
/// </summary>
internal static partial class SymbolUsageAnalysis
{
    /// <summary>
    /// Runs dataflow analysis on the given control flow graph to compute symbol usage results
    /// for symbol read/writes.
    /// </summary>
    public static SymbolUsageResult Run(ControlFlowGraph cfg, ISymbol owningSymbol, CancellationToken cancellationToken)
        => DataFlowAnalyzer.RunAnalysis(cfg, owningSymbol, cancellationToken);

    /// <summary>
    /// Runs a fast, non-precise operation tree based analysis to compute symbol usage results
    /// for symbol read/writes.
    /// </summary>
    public static SymbolUsageResult Run(IOperation rootOperation, ISymbol owningSymbol, CancellationToken cancellationToken)
    {
        AnalysisData analysisData = null;
        using (analysisData = OperationTreeAnalysisData.Create(owningSymbol, AnalyzeLocalFunction))
        {
            var operations = SpecializedCollections.SingletonEnumerable(rootOperation);
            Walker.AnalyzeOperationsAndUpdateData(owningSymbol, operations, analysisData, cancellationToken);
            return analysisData.ToResult();
        }

        // Local functions.
        BasicBlockAnalysisData AnalyzeLocalFunction(IMethodSymbol localFunction)
        {
            var localFunctionOperation = rootOperation.Descendants()
                .FirstOrDefault(o => Equals((o as ILocalFunctionOperation)?.Symbol, localFunction));

            // Can likely be null for broken code.
            if (localFunctionOperation != null)
            {
                var operations = SpecializedCollections.SingletonEnumerable(localFunctionOperation);
                Walker.AnalyzeOperationsAndUpdateData(localFunction, operations, analysisData, cancellationToken);
            }

            return analysisData.CurrentBlockAnalysisData;
        }
    }
}
