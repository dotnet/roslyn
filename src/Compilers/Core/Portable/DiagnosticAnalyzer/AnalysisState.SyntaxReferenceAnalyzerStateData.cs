// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
            public CodeBlockAnalyzerStateData CodeBlockAnalysisState { get; set; }

            public DeclarationAnalyzerStateData()
            {
                CodeBlockAnalysisState = new CodeBlockAnalyzerStateData();
            }

            public override void SetStateKind(StateKind stateKind)
            {
                CodeBlockAnalysisState.SetStateKind(stateKind);
                base.SetStateKind(stateKind);
            }

            public override void Free()
            {
                base.Free();
                CodeBlockAnalysisState.Free();
            }
        }

        /// <summary>
        /// Stores the partial analysis state for syntax node actions executed on the declaration.
        /// </summary>
        internal class SyntaxNodeAnalyzerStateData : AnalyzerStateData
        {
            public HashSet<SyntaxNode> ProcessedNodes { get; set; }
            public SyntaxNode CurrentNode { get; set; }

            public SyntaxNodeAnalyzerStateData()
            {
                CurrentNode = null;
                ProcessedNodes = new HashSet<SyntaxNode>();
            }

            public void ClearNodeAnalysisState()
            {
                CurrentNode = null;
                ProcessedActions.Clear();
            }

            public override void Free()
            {
                base.Free();
                CurrentNode = null;
                ProcessedNodes.Clear();
            }
        }

        /// <summary>
        /// Stores the partial analysis state for code block actions executed on the declaration.
        /// </summary>
        internal sealed class CodeBlockAnalyzerStateData : AnalyzerStateData
        {
            public SyntaxNodeAnalyzerStateData ExecutableNodesAnalysisState { get; }

            public ImmutableHashSet<AnalyzerAction> CurrentCodeBlockEndActions { get; set; }
            public ImmutableHashSet<AnalyzerAction> CurrentCodeBlockNodeActions { get; set; }

            public CodeBlockAnalyzerStateData()
            {
                ExecutableNodesAnalysisState = new SyntaxNodeAnalyzerStateData();
                CurrentCodeBlockEndActions = null;
                CurrentCodeBlockNodeActions = null;
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
                CurrentCodeBlockEndActions = null;
                CurrentCodeBlockNodeActions = null;
            }
        }
    }
}
