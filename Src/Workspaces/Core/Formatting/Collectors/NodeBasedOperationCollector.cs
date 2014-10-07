using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Internal.Measurement;
using Roslyn.Services.Shared.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// this collector gathers formatting operations that are based on a node
    /// </summary>
    internal class NodeBasedOperationCollector
    {
        public Task<List<IndentBlockOperation>> IndentBlockOperationTask { get; private set; }
        public Task<List<SuppressOperation>> SuppressOperationTask { get; private set; }
        public Task<List<AlignTokensOperation>> AlignmentOperationTask { get; private set; }
        public Task<List<AnchorIndentationOperation>> AnchorIndentationOperationsTask { get; private set; }

        public NodeBasedOperationCollector(
            ChainedFormattingRules chainedFormattingRules,
            CommonSyntaxNode root,
            TextSpan span,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(chainedFormattingRules);
            Contract.ThrowIfNull(root);

            var taskScheduler = TaskScheduler.Default;

            // iterating tree is very expensive. do it once and cache it to list
            var nodeIteratorTask = Task.Factory.SafeStartNew(() =>
                {
                    using (MeasurementBlockFactorySelector.ActiveFactory.BeginNew(FunctionId.Services_FormattingEngine_IterateNodes))
                    {
                        var nodeIterator = new NodeIterator(root, span, cancellationToken);
                        var result = nodeIterator.ToList();
                        return result;
                    }
                },
                cancellationToken,
                TaskCreationOptions.None,
                taskScheduler);

            // iterate through each operation using index to not create any unnecessary object
            this.IndentBlockOperationTask = nodeIteratorTask.SafeContinueWith(task =>
                {
                    using (MeasurementBlockFactorySelector.ActiveFactory.BeginNew(FunctionId.Services_FormattingEngine_CollectIndentBlock))
                    {
                        var nodes = task.Result;

                        var operations = new List<IndentBlockOperation>();
                        var list = new List<IndentBlockOperation>();

                        for (int i = 0; i < nodes.Count; i++)
                        {
                            chainedFormattingRules.AddIndentBlockOperations(list, nodes[i]);

                            operations.AddRange(list);
                            list.Clear();
                        }

                        return operations;
                    }
                },
                cancellationToken,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                taskScheduler);

            this.SuppressOperationTask = nodeIteratorTask.SafeContinueWith(task =>
                {
                    return CreateSuppressOperationTask(task, chainedFormattingRules);
                },
                cancellationToken,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                taskScheduler);

            this.AlignmentOperationTask = nodeIteratorTask.SafeContinueWith(task =>
                {
                    using (MeasurementBlockFactorySelector.ActiveFactory.BeginNew(FunctionId.Services_FormattingEngine_CollectAlignOperation))
                    {
                        var nodes = task.Result;

                        var operations = new List<AlignTokensOperation>();
                        var list = new List<AlignTokensOperation>();

                        for (int i = 0; i < nodes.Count; i++)
                        {
                            chainedFormattingRules.AddAlignTokensOperations(list, nodes[i]);

                            operations.AddRange(list);
                            list.Clear();
                        }

                        // make sure we order align operation from left to right
                        operations.Sort((o1, o2) => o1.BaseToken.Span.CompareTo(o2.BaseToken.Span));

                        return operations;
                    }
                },
                cancellationToken,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                taskScheduler);

            this.AnchorIndentationOperationsTask = nodeIteratorTask.SafeContinueWith(task =>
                {
                    using (MeasurementBlockFactorySelector.ActiveFactory.BeginNew(FunctionId.Services_FormattingEngine_CollectAnchorOperation))
                    {
                        var nodes = task.Result;

                        var operations = new List<AnchorIndentationOperation>();
                        var list = new List<AnchorIndentationOperation>();

                        for (int i = 0; i < nodes.Count; i++)
                        {
                            chainedFormattingRules.AddAnchorIndentationOperations(list, nodes[i]);

                            operations.AddRange(list);
                            list.Clear();
                        }

                        return operations;
                    }
                },
                cancellationToken,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                taskScheduler);
        }

        private static List<SuppressOperation> CreateSuppressOperationTask(Task<List<CommonSyntaxNode>> task, ChainedFormattingRules provider)
        {
            using (MeasurementBlockFactorySelector.ActiveFactory.BeginNew(FunctionId.Services_FormattingEngine_CollectSuppressOperation))
            {
                var nodes = task.Result;

                var operations = new List<SuppressOperation>();
                var list = new List<SuppressOperation>();

                for (int i = 0; i < nodes.Count; i++)
                {
                    provider.AddSuppressOperations(list, nodes[i]);

                    operations.AddRange(list);
                    list.Clear();
                }

                return operations;
            }
        }
    }
}
