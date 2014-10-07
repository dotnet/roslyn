using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Internal.Measurement;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// this collector will gather operations applied to trivia between two tokens.
    /// </summary>
    internal partial class TokenBasedOperationCollector
    {
        private readonly FormattingOptions options;
        private readonly TokenStream tokenStream;

        private readonly Task<TokenPairWithOperations[]> tokenOperationsTask;

        public TokenBasedOperationCollector(
            FormattingOptions options,
            ChainedFormattingRules chainedFormattingRules,
            TokenStream tokenStream,
            CommonSyntaxNode root,
            TextSpan span,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(options);
            Contract.ThrowIfNull(chainedFormattingRules);
            Contract.ThrowIfNull(tokenStream);
            Contract.ThrowIfNull(root);

            this.options = options;
            this.tokenStream = tokenStream;

            this.tokenOperationsTask = Task.Factory.SafeStartNew(() =>
            {
                using (MeasurementBlockFactorySelector.ActiveFactory.BeginNew(FunctionId.Services_FormattingEngine_CollectTokenOperation))
                {
                    // get all operations concurrently
                    var result = GetAllOperationsIncludingZeroLengthTokens(chainedFormattingRules, root, span, cancellationToken);
                    return result;
                }
            },
            cancellationToken,
            TaskCreationOptions.None,
            TaskScheduler.Default);
        }

        private TokenPairWithOperations[] GetAllOperationsIncludingZeroLengthTokens(
            ChainedFormattingRules operationProvider,
            CommonSyntaxNode root,
            TextSpan span,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(operationProvider);
            Contract.ThrowIfNull(root);

            var iterator = new Iterator(this.tokenStream.Tokens);

            // pre-allocate list once. this is cheaper than re-adjusting list as items are added.
            var list = new TokenPairWithOperations[this.tokenStream.TokenCount - 1];

            // gather operations concurrently. put item directly to right spot to avoid locking and sorting
            if (this.options.DebugMode)
            {
                foreach (var pair in iterator)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    AddSpaceAndLineOperationToList(operationProvider, list, pair);
                }
            }
            else
            {
                var option = new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    TaskScheduler = TaskScheduler.Default
                };

                Parallel.ForEach(iterator, option, pair =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    AddSpaceAndLineOperationToList(operationProvider, list, pair);
                });
            }

            return list;
        }

        private void AddSpaceAndLineOperationToList(
            ChainedFormattingRules operationProvider,
            TokenPairWithOperations[] list,
            ValueTuple<int, CommonSyntaxToken, CommonSyntaxToken> pair)
        {
            var spaceOperation = operationProvider.GetAdjustSpacesOperation(pair.Item2, pair.Item3);
            var lineOperation = operationProvider.GetAdjustNewLinesOperation(pair.Item2, pair.Item3);

            list[pair.Item1] = new TokenPairWithOperations(this.tokenStream, pair.Item1, spaceOperation, lineOperation);
        }

        public TokenPairWithOperations[] TokenOperations
        {
            get
            {
                return this.tokenOperationsTask.Result;
            }
        }
    }
}
