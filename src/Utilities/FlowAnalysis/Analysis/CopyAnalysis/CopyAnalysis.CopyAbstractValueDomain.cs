// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    internal partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        /// <summary>
        /// Abstract value domain for <see cref="CopyAnalysis"/> to merge and compare <see cref="CopyAbstractValue"/> values.
        /// </summary>
        private sealed class CopyAbstractValueDomain : AbstractValueDomain<CopyAbstractValue>
        {
            public static CopyAbstractValueDomain Default = new CopyAbstractValueDomain();
            private readonly SetAbstractDomain<AnalysisEntity> _entitiesDomain = new SetAbstractDomain<AnalysisEntity>();

            private CopyAbstractValueDomain() { }

            public override CopyAbstractValue Bottom => CopyAbstractValue.NotApplicable;

            public override CopyAbstractValue UnknownOrMayBeValue => CopyAbstractValue.Unknown;

            public override int Compare(CopyAbstractValue oldValue, CopyAbstractValue newValue)
            {
                Debug.Assert(oldValue != null);
                Debug.Assert(newValue != null);

                if (ReferenceEquals(oldValue, newValue))
                {
                    return 0;
                }

                if (oldValue.Kind == newValue.Kind)
                {
                    return _entitiesDomain.Compare(oldValue.AnalysisEntities, newValue.AnalysisEntities) * -1;
                }
                else if (oldValue.Kind < newValue.Kind)
                {
                    return -1;
                }
                else
                {
                    Debug.Fail("Non-monotonic Merge function");
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

                var mergedLocations = _entitiesDomain.Intersect(value1.AnalysisEntities, value2.AnalysisEntities);
                return mergedLocations.IsEmpty ? CopyAbstractValue.Unknown : new CopyAbstractValue(mergedLocations);
            }
        }
    }
}
