// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalysisState
    {
        /// <summary>
        /// Stores the partial analysis state for a specific symbol declaration for a specific analyzer.
        /// </summary>
        internal sealed class DeclarationAnalyzerStateData : SyntaxNodeAnalyzerStateData
        {
            /// <summary>
            /// Partial analysis state for code block actions executed on the declaration.
            /// </summary>
            public CodeBlockAnalyzerStateData CodeBlockAnalysisState { get; }

            /// <summary>
            /// Partial analysis state for operation block actions executed on the declaration.
            /// 
            /// NOTE: This state tracks operations actions registered inside operation block start context.
            /// Operation actions registered outside operation block start context are tracked
            /// with <see cref="OperationAnalysisState"/>.
            /// </summary>
            public OperationBlockAnalyzerStateData OperationBlockAnalysisState { get; }

            /// <summary>
            /// Partial analysis state for operation actions executed on the declaration.
            /// 
            /// NOTE: This state tracks operations actions registered outside of operation block start context.
            /// Operation actions registered inside operation block start context are tracked
            /// with <see cref="OperationBlockAnalyzerStateData"/>.
            /// </summary>
            public OperationAnalyzerStateData OperationAnalysisState { get; }

            public static new readonly DeclarationAnalyzerStateData FullyProcessedInstance = CreateFullyProcessedInstance();

            public DeclarationAnalyzerStateData()
            {
                CodeBlockAnalysisState = new CodeBlockAnalyzerStateData();
                OperationBlockAnalysisState = new OperationBlockAnalyzerStateData();
                OperationAnalysisState = new OperationAnalyzerStateData();
            }

            private static DeclarationAnalyzerStateData CreateFullyProcessedInstance()
            {
                var instance = new DeclarationAnalyzerStateData();
                instance.SetStateKind(StateKind.FullyProcessed);
                return instance;
            }

            public override void SetStateKind(StateKind stateKind)
            {
                CodeBlockAnalysisState.SetStateKind(stateKind);
                OperationBlockAnalysisState.SetStateKind(stateKind);
                OperationAnalysisState.SetStateKind(stateKind);
                base.SetStateKind(stateKind);
            }

            public override void Free()
            {
                base.Free();
                CodeBlockAnalysisState.Free();
                OperationBlockAnalysisState.Free();
                OperationAnalysisState.Free();
            }
        }

        /// <summary>
        /// Stores the partial analysis state for syntax node actions executed on the declaration.
        /// </summary>
        internal class SyntaxNodeAnalyzerStateData : AnalyzerStateData
        {
            public HashSet<SyntaxNode> ProcessedNodes { get; }
            public SyntaxNode CurrentNode { get; set; }

            public SyntaxNodeAnalyzerStateData()
            {
                CurrentNode = null;
                ProcessedNodes = new HashSet<SyntaxNode>();
            }

            public void OnAllActionsExecutedForNode(SyntaxNode node)
            {
                CurrentNode = null;
                ProcessedActions.Clear();
                ProcessedNodes.Add(node);
            }

            public override void Free()
            {
                base.Free();
                CurrentNode = null;
                ProcessedNodes.Clear();
            }
        }

        /// <summary>
        /// Stores the partial analysis state for operation actions executed on the declaration.
        /// </summary>
        internal class OperationAnalyzerStateData : AnalyzerStateData
        {
            public HashSet<IOperation> ProcessedOperations { get; }
            public IOperation CurrentOperation { get; set; }

            public OperationAnalyzerStateData()
            {
                CurrentOperation = null;
                ProcessedOperations = new HashSet<IOperation>();
            }

            public void OnAllActionsExecutedForOperation(IOperation operation)
            {
                CurrentOperation = null;
                ProcessedActions.Clear();
                ProcessedOperations.Add(operation);
            }

            public override void Free()
            {
                base.Free();
                CurrentOperation = null;
                ProcessedOperations.Clear();
            }
        }

        /// <summary>
        /// Stores the partial analysis state for code block actions or operation block actions executed on the declaration.
        /// </summary>
        internal abstract class BlockAnalyzerStateData<TBlockAction, TNodeStateData> : AnalyzerStateData
            where TBlockAction : AnalyzerAction
            where TNodeStateData : AnalyzerStateData, new()
        {
            public TNodeStateData ExecutableNodesAnalysisState { get; }

            public ImmutableHashSet<TBlockAction> CurrentBlockEndActions { get; set; }
            public ImmutableHashSet<AnalyzerAction> CurrentBlockNodeActions { get; set; }

            public BlockAnalyzerStateData()
            {
                ExecutableNodesAnalysisState = new TNodeStateData();
                CurrentBlockEndActions = null;
                CurrentBlockNodeActions = null;
            }

            public override void SetStateKind(StateKind stateKind)
            {
                ExecutableNodesAnalysisState.SetStateKind(stateKind);
                base.SetStateKind(stateKind);
            }

            public override void Free()
            {
                base.Free();
                ExecutableNodesAnalysisState.Free();
                CurrentBlockEndActions = null;
                CurrentBlockNodeActions = null;
            }
        }

        /// <summary>
        /// Stores the partial analysis state for code block actions executed on the declaration.
        /// </summary>
        internal sealed class CodeBlockAnalyzerStateData : BlockAnalyzerStateData<CodeBlockAnalyzerAction, SyntaxNodeAnalyzerStateData>
        {
        }

        /// <summary>
        /// Stores the partial analysis state for operation block actions executed on the declaration.
        /// </summary>
        internal sealed class OperationBlockAnalyzerStateData : BlockAnalyzerStateData<OperationBlockAnalyzerAction, OperationAnalyzerStateData>
        {
        }
    }
}
