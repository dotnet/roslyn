// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal abstract class AbstractDataFlowAnalyzer<TBlockAnalysisData> : IDataFlowAnalyzer<TBlockAnalysisData>
    {
        protected abstract bool AnalyzeUnreachableBlocks { get; }
        protected abstract TBlockAnalysisData AnalyzeBlock(BasicBlock basicBlock, CancellationToken cancellationToken);
        protected abstract TBlockAnalysisData AnalyzeNonConditionalBranch(BasicBlock basicBlock, TBlockAnalysisData currentAnalysisData, CancellationToken cancellationToken);
        protected abstract (TBlockAnalysisData fallThroughSuccessorData, TBlockAnalysisData conditionalSuccessorData) AnalyzeConditionalBranch(
            BasicBlock basicBlock, TBlockAnalysisData currentAnalysisData, CancellationToken cancellationToken);
        protected abstract TBlockAnalysisData GetCurrentAnalysisData(BasicBlock basicBlock);
        protected abstract TBlockAnalysisData GetEmptyAnalysisData();
        protected abstract bool IsEqual(TBlockAnalysisData analysisData1, TBlockAnalysisData analysisData2);
        protected abstract TBlockAnalysisData Merge(TBlockAnalysisData analysisData1, TBlockAnalysisData analysisData2, CancellationToken cancellationToken);
        protected abstract void SetCurrentAnalysisData(BasicBlock basicBlock, TBlockAnalysisData data);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        void System.IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

        #region IDataFlowAnalyzer implementation
        bool IDataFlowAnalyzer<TBlockAnalysisData>.AnalyzeUnreachableBlocks
            => this.AnalyzeUnreachableBlocks;

        TBlockAnalysisData IDataFlowAnalyzer<TBlockAnalysisData>.AnalyzeBlock(BasicBlock basicBlock, CancellationToken cancellationToken)
            => this.AnalyzeBlock(basicBlock, cancellationToken);

        TBlockAnalysisData IDataFlowAnalyzer<TBlockAnalysisData>.AnalyzeNonConditionalBranch(BasicBlock basicBlock, TBlockAnalysisData currentAnalysisData, CancellationToken cancellationToken)
            => this.AnalyzeNonConditionalBranch(basicBlock, currentAnalysisData, cancellationToken);

        (TBlockAnalysisData fallThroughSuccessorData, TBlockAnalysisData conditionalSuccessorData) IDataFlowAnalyzer<TBlockAnalysisData>.AnalyzeConditionalBranch(
            BasicBlock basicBlock, TBlockAnalysisData currentAnalysisData, CancellationToken cancellationToken)
            => this.AnalyzeConditionalBranch(basicBlock, currentAnalysisData, cancellationToken);

        TBlockAnalysisData IDataFlowAnalyzer<TBlockAnalysisData>.GetCurrentAnalysisData(BasicBlock basicBlock)
            => this.GetCurrentAnalysisData(basicBlock);

        TBlockAnalysisData IDataFlowAnalyzer<TBlockAnalysisData>.GetEmptyAnalysisData()
            => this.GetEmptyAnalysisData();

        bool IDataFlowAnalyzer<TBlockAnalysisData>.IsEqual(TBlockAnalysisData analysisData1, TBlockAnalysisData analysisData2)
            => this.IsEqual(analysisData1, analysisData2);

        TBlockAnalysisData IDataFlowAnalyzer<TBlockAnalysisData>.Merge(TBlockAnalysisData analysisData1, TBlockAnalysisData analysisData2, CancellationToken cancellationToken)
            => this.Merge(analysisData1, analysisData2, cancellationToken);

        void IDataFlowAnalyzer<TBlockAnalysisData>.SetCurrentAnalysisData(BasicBlock basicBlock, TBlockAnalysisData data)
            => this.SetCurrentAnalysisData(basicBlock, data);
        #endregion
    }
}
