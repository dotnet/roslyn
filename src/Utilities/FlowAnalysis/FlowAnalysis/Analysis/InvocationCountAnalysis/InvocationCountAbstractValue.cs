// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal enum InvocationCountAbstractValueKind
    {
        Zero,
        OneTime,
        MoreThanOneTime,
        Unknown
    }

    internal class InvocationCountAbstractValue : CacheBasedEquatable<InvocationCountAbstractValue>
    {
        public static readonly InvocationCountAbstractValue Zero = new(InvocationCountAbstractValueKind.Zero);
        public static readonly InvocationCountAbstractValue OneTime = new(InvocationCountAbstractValueKind.OneTime);
        public static readonly InvocationCountAbstractValue MoreThanOneTime = new(InvocationCountAbstractValueKind.MoreThanOneTime);
        public static readonly InvocationCountAbstractValue Unknown = new(InvocationCountAbstractValueKind.Unknown);

        public InvocationCountAbstractValueKind Kind { get; }

        public InvocationCountAbstractValue(InvocationCountAbstractValueKind kind)
        {
            Kind = kind;
        }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(Kind.GetHashCode());
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<InvocationCountAbstractValue> obj)
        {
            return ((InvocationCountAbstractValue)obj).Kind.GetHashCode() == Kind.GetHashCode();
        }
    }
}