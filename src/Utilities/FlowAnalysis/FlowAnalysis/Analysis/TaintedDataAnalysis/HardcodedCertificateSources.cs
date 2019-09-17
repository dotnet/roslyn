// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class HardcodedCertificateSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for hardcoded certificate tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static HardcodedCertificateSources()
        {
            var builder = PooledHashSet<SourceInfo>.GetInstance();

            builder.AddSourceInfoSpecifyingTaintedTargets(
                WellKnownTypeNames.SystemIOFileFullName,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: null,
                transferMethods: new (MethodMatcher, (string, string)[])[]{
                    (
                        (methodName, arguments) =>
                            methodName == "WriteAllBytes",
                        new (string, string)[]{
                            ("bytes", "path"),
                        }
                    ),
                });

            SourceInfos = builder.ToImmutableAndFree();
        }
    }
}
