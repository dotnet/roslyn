// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = IDictionary<AbstractLocation, DisposeAbstractValue>;

    internal partial class DisposeAnalysis : ForwardDataFlowAnalysis<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeBlockAnalysisResult, DisposeAbstractValue>
    {
        /// <summary>
        /// Abstract value domain for <see cref="DisposeAnalysis"/> to merge and compare <see cref="DisposeAbstractValue"/> values.
        /// </summary>
        private class DisposeAbstractValueDomain : AbstractValueDomain<DisposeAbstractValue>
        {
            public static DisposeAbstractValueDomain Default = new DisposeAbstractValueDomain();
            private readonly SetAbstractDomain<IOperation> _disposingOperationsDomain = new SetAbstractDomain<IOperation>();

            private DisposeAbstractValueDomain() { }

            public override DisposeAbstractValue Bottom => DisposeAbstractValue.NotDisposable;
            
            public override DisposeAbstractValue UnknownOrMayBeValue => DisposeAbstractValue.Unknown;

            public override int Compare(DisposeAbstractValue oldValue, DisposeAbstractValue newValue)
            {
                Debug.Assert(oldValue != null);
                Debug.Assert(newValue != null);

                if (ReferenceEquals(oldValue, newValue))
                {
                    return 0;
                }

                if (oldValue.Kind == newValue.Kind)
                {
                    return _disposingOperationsDomain.Compare(oldValue.DisposingOrEscapingOperations, newValue.DisposingOrEscapingOperations);
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

            public override DisposeAbstractValue Merge(DisposeAbstractValue value1, DisposeAbstractValue value2)
            {
                if (value1 == null)
                {
                    return value2;
                }
                else if (value2 == null)
                {
                    return value1;
                }
                else if (value1.Kind == DisposeAbstractValueKind.NotDisposable || value2.Kind == DisposeAbstractValueKind.NotDisposable)
                {
                    return DisposeAbstractValue.NotDisposable;
                }
                else if (value1.Kind == DisposeAbstractValueKind.NotDisposed && value2.Kind == DisposeAbstractValueKind.NotDisposed)
                {
                    return DisposeAbstractValue.NotDisposed;
                }

                DisposeAbstractValueKind kind = value1.Kind == DisposeAbstractValueKind.Disposed && value2.Kind == DisposeAbstractValueKind.Disposed ?
                    DisposeAbstractValueKind.Disposed :
                    DisposeAbstractValueKind.MaybeDisposed;

                var mergedDisposingOperations = _disposingOperationsDomain.Merge(value1.DisposingOrEscapingOperations, value2.DisposingOrEscapingOperations);
                if (mergedDisposingOperations.IsEmpty)
                {
                    Debug.Assert(kind == DisposeAbstractValueKind.MaybeDisposed);
                    return DisposeAbstractValue.Unknown;
                }

                return new DisposeAbstractValue(mergedDisposingOperations, kind);
            }
        }
    }
}
