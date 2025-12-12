// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

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
                WellKnownTypeNames.SystemIOFile,
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
