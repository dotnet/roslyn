// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = DictionaryAnalysisData<AbstractLocation, DisposeAbstractValue>;

    public partial class DisposeAnalysis : ForwardDataFlowAnalysis<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeBlockAnalysisResult, DisposeAbstractValue>
    {
        /// <summary>
        /// Abstract value domain for <see cref="DisposeAnalysis"/> to merge and compare <see cref="DisposeAbstractValue"/> values.
        /// </summary>
        private class DisposeAbstractValueDomain : AbstractValueDomain<DisposeAbstractValue>
        {
            public static DisposeAbstractValueDomain Default = new DisposeAbstractValueDomain();
            private readonly SetAbstractDomain<IOperation> _disposingOperationsDomain = SetAbstractDomain<IOperation>.Default;

            private DisposeAbstractValueDomain() { }

            public override DisposeAbstractValue Bottom => DisposeAbstractValue.NotDisposable;

            public override DisposeAbstractValue UnknownOrMayBeValue => DisposeAbstractValue.Unknown;

            public override int Compare(DisposeAbstractValue oldValue, DisposeAbstractValue newValue, bool assertMonotonicity)
            {
                if (ReferenceEquals(oldValue, newValue))
                {
                    return 0;
                }

                if (oldValue.Kind == newValue.Kind)
                {
                    return _disposingOperationsDomain.Compare(oldValue.DisposingOrEscapingOperations, newValue.DisposingOrEscapingOperations);
                }
                else if (oldValue.Kind < newValue.Kind ||
                    newValue.Kind == DisposeAbstractValueKind.Invalid ||
                    newValue.Kind == DisposeAbstractValueKind.Disposed)
                {
                    return -1;
                }
                else
                {
                    FireNonMonotonicAssertIfNeeded(assertMonotonicity);
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
                else if (value1.Kind == DisposeAbstractValueKind.Invalid)
                {
                    return value2;
                }
                else if (value2.Kind == DisposeAbstractValueKind.Invalid)
                {
                    return value1;
                }
                else if (value1.Kind == DisposeAbstractValueKind.NotDisposable || value2.Kind == DisposeAbstractValueKind.NotDisposable)
                {
                    return DisposeAbstractValue.NotDisposable;
                }
                else if (value1.Kind == DisposeAbstractValueKind.Unknown || value2.Kind == DisposeAbstractValueKind.Unknown)
                {
                    return DisposeAbstractValue.Unknown;
                }
                else if (value1.Kind == DisposeAbstractValueKind.NotDisposed && value2.Kind == DisposeAbstractValueKind.NotDisposed)
                {
                    return DisposeAbstractValue.NotDisposed;
                }

                var mergedDisposingOperations = _disposingOperationsDomain.Merge(value1.DisposingOrEscapingOperations, value2.DisposingOrEscapingOperations);
                Debug.Assert(!mergedDisposingOperations.IsEmpty);
                return new DisposeAbstractValue(mergedDisposingOperations, GetMergedKind());

                // Local functions.
                DisposeAbstractValueKind GetMergedKind()
                {
                    Debug.Assert(!value1.DisposingOrEscapingOperations.IsEmpty || !value2.DisposingOrEscapingOperations.IsEmpty);

                    if (value1.Kind == value2.Kind)
                    {
                        return value1.Kind;
                    }
                    else if (value1.Kind == DisposeAbstractValueKind.MaybeDisposed ||
                        value2.Kind == DisposeAbstractValueKind.MaybeDisposed)
                    {
                        return DisposeAbstractValueKind.MaybeDisposed;
                    }

                    switch (value1.Kind)
                    {
                        case DisposeAbstractValueKind.NotDisposed:
                            switch (value2.Kind)
                            {
                                case DisposeAbstractValueKind.Escaped:
                                case DisposeAbstractValueKind.NotDisposedOrEscaped:
                                    return DisposeAbstractValueKind.NotDisposedOrEscaped;

                                case DisposeAbstractValueKind.Disposed:
                                    return DisposeAbstractValueKind.MaybeDisposed;
                            }

                            break;

                        case DisposeAbstractValueKind.Escaped:
                            switch (value2.Kind)
                            {
                                case DisposeAbstractValueKind.NotDisposed:
                                case DisposeAbstractValueKind.NotDisposedOrEscaped:
                                    return DisposeAbstractValueKind.NotDisposedOrEscaped;

                                case DisposeAbstractValueKind.Disposed:
                                    return DisposeAbstractValueKind.Disposed;
                            }

                            break;

                        case DisposeAbstractValueKind.NotDisposedOrEscaped:
                            switch (value2.Kind)
                            {
                                case DisposeAbstractValueKind.NotDisposed:
                                case DisposeAbstractValueKind.Escaped:
                                    return DisposeAbstractValueKind.NotDisposedOrEscaped;

                                case DisposeAbstractValueKind.Disposed:
                                    return DisposeAbstractValueKind.MaybeDisposed;
                            }

                            break;

                        case DisposeAbstractValueKind.Disposed:
                            switch (value2.Kind)
                            {
                                case DisposeAbstractValueKind.Escaped:
                                    return DisposeAbstractValueKind.Disposed;

                                case DisposeAbstractValueKind.NotDisposed:
                                case DisposeAbstractValueKind.NotDisposedOrEscaped:
                                    return DisposeAbstractValueKind.MaybeDisposed;
                            }

                            break;
                    }

                    Debug.Fail($"Unhandled dispose value kind merge: {value1.Kind} and {value2.Kind}");
                    return DisposeAbstractValueKind.MaybeDisposed;
                }
            }
        }
    }
}
