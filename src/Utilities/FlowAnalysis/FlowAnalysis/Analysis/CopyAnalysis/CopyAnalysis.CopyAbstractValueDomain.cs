// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;

    public partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        /// <summary>
        /// Abstract value domain for <see cref="CopyAnalysis"/> to merge and compare <see cref="CopyAbstractValue"/> values.
        /// </summary>
        private sealed class CopyAbstractValueDomain : AbstractValueDomain<CopyAbstractValue>
        {
            public static CopyAbstractValueDomain Default = new CopyAbstractValueDomain();
            private readonly SetAbstractDomain<AnalysisEntity> _entitiesDomain = SetAbstractDomain<AnalysisEntity>.Default;

            private CopyAbstractValueDomain() { }

            public override CopyAbstractValue Bottom => CopyAbstractValue.NotApplicable;

            public override CopyAbstractValue UnknownOrMayBeValue => CopyAbstractValue.Unknown;

            public override int Compare(CopyAbstractValue oldValue, CopyAbstractValue newValue, bool assertMonotonicity)
            {
                if (ReferenceEquals(oldValue, newValue))
                {
                    return 0;
                }

                if (oldValue.Kind == newValue.Kind ||
                    (oldValue.Kind.IsKnown() && newValue.Kind.IsKnown()))
                {
                    return _entitiesDomain.Compare(oldValue.AnalysisEntities, newValue.AnalysisEntities) * -1;
                }
                else if (oldValue.Kind < newValue.Kind)
                {
                    return -1;
                }
                else
                {
                    FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                    return 1;
                }
            }

            public override CopyAbstractValue Merge(CopyAbstractValue value1, CopyAbstractValue value2)
            {
                if (value1 == null)
                {
                    return value2;
                }
                else if (value2 == null)
                {
                    return value1;
                }
                else if (value1.Kind == CopyAbstractValueKind.Invalid || value1.Kind == CopyAbstractValueKind.NotApplicable)
                {
                    return value2;
                }
                else if (value2.Kind == CopyAbstractValueKind.Invalid || value2.Kind == CopyAbstractValueKind.NotApplicable)
                {
                    return value1;
                }
                else if (value1.Kind == CopyAbstractValueKind.Unknown || value2.Kind == CopyAbstractValueKind.Unknown)
                {
                    return CopyAbstractValue.Unknown;
                }

                Debug.Assert(value1.Kind.IsKnown());
                Debug.Assert(value2.Kind.IsKnown());

                var mergedEntities = _entitiesDomain.Intersect(value1.AnalysisEntities, value2.AnalysisEntities);
                if (mergedEntities.IsEmpty)
                {
                    return CopyAbstractValue.Unknown;
                }
                else if (mergedEntities.Count == value1.AnalysisEntities.Count)
                {
                    Debug.Assert(_entitiesDomain.Equals(mergedEntities, value1.AnalysisEntities));
                    return value1;
                }
                else if (mergedEntities.Count == value2.AnalysisEntities.Count)
                {
                    Debug.Assert(_entitiesDomain.Equals(mergedEntities, value2.AnalysisEntities));
                    return value2;
                }

                var mergedKind = value1.Kind > value2.Kind ? value1.Kind : value2.Kind;
                return new CopyAbstractValue(mergedEntities, mergedKind);
            }
        }
    }
}
