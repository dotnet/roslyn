// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    using InvocationCountAnalysisResult = DataFlowAnalysisResult<InvocationCountBlockAnalysisResult, InvocationCountAbstractValue>;

    internal class InvocationCountDataFlowOperationVisitor : PredicateAnalysisEntityDataFlowOperationVisitor<
        InvocationCountAnalysisData,
        InvocationCountAnalysisContext,
        InvocationCountAnalysisResult,
        InvocationCountAbstractValue>
    {
        private static readonly InvocationCountAnalysisDomain s_analysisDomain = new(
            new MapAbstractDomain<AnalysisEntity, InvocationCountAbstractValue>(InvocationCountValueDomain.Instance));

        private readonly ImmutableArray<string> _trackingMethodNames;

        public InvocationCountDataFlowOperationVisitor(
            InvocationCountAnalysisContext analysisContext, ImmutableArray<string> trackingMethodNames) : base(analysisContext)
        {
            _trackingMethodNames = trackingMethodNames;
        }

        protected override InvocationCountAbstractValue GetAbstractDefaultValue(ITypeSymbol type)
            => InvocationCountAbstractValue.Zero;

        protected override bool HasAnyAbstractValue(InvocationCountAnalysisData data)
            => data.CoreAnalysisData.Count > 0;

        protected override void ResetCurrentAnalysisData()
            => CurrentAnalysisData.Reset((_, _) => InvocationCountAbstractValue.Zero);

        protected override void UpdateValuesForAnalysisData(InvocationCountAnalysisData targetAnalysisData)
            => UpdateValuesForAnalysisData(CurrentAnalysisData.CoreAnalysisData, targetAnalysisData.CoreAnalysisData);

        protected override InvocationCountAnalysisData MergeAnalysisData(InvocationCountAnalysisData value1, InvocationCountAnalysisData value2)
            => s_analysisDomain.Merge(value1, value2);

        protected override InvocationCountAnalysisData GetClonedAnalysisData(InvocationCountAnalysisData analysisData)
            => new(analysisData);

        public override InvocationCountAnalysisData GetEmptyAnalysisData()
            => new(ImmutableDictionary<AnalysisEntity, InvocationCountAbstractValue>.Empty);

        protected override InvocationCountAnalysisData GetExitBlockOutputData(InvocationCountAnalysisResult analysisResult)
            => new(analysisResult.ExitBlockOutput.Data);

        protected override bool Equals(InvocationCountAnalysisData value1, InvocationCountAnalysisData value2)
            => EqualsHelper(value1.CoreAnalysisData, value2.CoreAnalysisData);

        protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(
            InvocationCountAnalysisData dataAtException,
            ThrownExceptionInfo throwBranchWithExceptionType)
            => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(
                dataAtException.CoreAnalysisData,
                CurrentAnalysisData.CoreAnalysisData,
                throwBranchWithExceptionType);

        protected override void AddTrackedEntities(
            InvocationCountAnalysisData analysisData,
            HashSet<AnalysisEntity> builder,
            bool forInterproceduralAnalysis = false)
        {
            builder.UnionWith(analysisData.CoreAnalysisData.Keys);
        }

        protected override void SetAbstractValue(AnalysisEntity analysisEntity, InvocationCountAbstractValue value)
            => SetAbstractValue(CurrentAnalysisData, analysisEntity, value);

        private static void SetAbstractValue(InvocationCountAnalysisData analysisData, AnalysisEntity analysisEntity, InvocationCountAbstractValue value)
        {
            analysisData.SetAbstractValue(analysisEntity, value);
        }

        protected override void ResetAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.SetAbstractValue(analysisEntity, InvocationCountAbstractValue.Zero);

        protected override InvocationCountAbstractValue GetAbstractValue(AnalysisEntity analysisEntity)
        {
            if (CurrentAnalysisData.TryGetValue(analysisEntity, out var value))
            {
                return value;
            }

            return InvocationCountAbstractValue.Zero;
        }

        protected override bool HasAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.CoreAnalysisData.ContainsKey(analysisEntity);

        protected override void StopTrackingEntity(AnalysisEntity analysisEntity, InvocationCountAnalysisData analysisData)
            => analysisData.RemoveEntries(analysisEntity);

        protected override InvocationCountAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
            => GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData.CoreAnalysisData, SetAbstractValue);

        protected override void ApplyInterproceduralAnalysisResultCore(InvocationCountAnalysisData resultData)
            => ApplyInterproceduralAnalysisResultHelper(resultData.CoreAnalysisData);

        public override InvocationCountAbstractValue DefaultVisit(IOperation operation, object? argument)
        {
            _ = base.DefaultVisit(operation, argument);
            if (operation is IInvocationOperation)
            {
                return InvocationCountAbstractValue.Zero;
            }

            return InvocationCountAbstractValue.Unknown;
        }

        protected override PredicateValueKind SetValueForIsNullComparisonOperator(
            IOperation leftOperand,
            bool equals,
            InvocationCountAnalysisData targetAnalysisData)
        {
            return PredicateValueKind.Unknown;
        }

        public override InvocationCountAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
            IMethodSymbol method,
            IOperation? visitedInstance,
            ImmutableArray<IArgumentOperation> visitedArguments,
            bool invokedAsDelegate,
            IOperation originalOperation,
            InvocationCountAbstractValue defaultValue)
        {
            // TODO: Instead of hard code, this should be changed to a function.
            if (_trackingMethodNames.Contains(method.ToDisplayString(InvocationCountAnalysis.MethodFullyQualifiedNameFormat))
                && originalOperation is IInvocationOperation invocationOperation)
            {
                if (visitedInstance == null
                    && method.IsExtensionMethod
                    && !invocationOperation.Arguments.IsEmpty
                    && AnalysisEntityFactory.TryCreate(invocationOperation.Arguments[0], out var analysisEntity))
                {
                    var existingValue = GetAbstractValue(analysisEntity);
                    var newValue = InvocationCountValueDomain.Instance.Merge(existingValue, InvocationCountAbstractValue.OneTime);
                    SetAbstractValue(analysisEntity, newValue);
                    return newValue;
                }
            }

            return base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
                method,
                visitedInstance,
                visitedArguments,
                invokedAsDelegate,
                originalOperation,
                defaultValue);
        }
    }
}