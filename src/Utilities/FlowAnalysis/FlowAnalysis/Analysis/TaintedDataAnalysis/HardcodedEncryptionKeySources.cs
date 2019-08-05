// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class HardcodedEncryptionKeySources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for hardcoded key tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static HardcodedEncryptionKeySources()
        {
            var builder = PooledHashSet<SourceInfo>.GetInstance();

            builder.AddSourceInfo(
                WellKnownTypeNames.SystemConvert,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: new (string, IsInvocationTaintedWithValueContentAnalysis)[]{
                    ("FromBase64String",
                    (IEnumerable<PointsToAbstractValue> argumentPonitsTos, IEnumerable<ValueContentAbstractValue> argumentValueContents) => argumentValueContents.All(o => o.IsLiteralState)),
                });
            builder.AddSourceInfo(
                WellKnownTypeNames.SystemByte,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: null,
                taintConstantArray: true);

            SourceInfos = builder.ToImmutableAndFree();
        }
    }
}
