// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class HardcodedBytesSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for hardcoded bytes tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static HardcodedBytesSources()
        {
            var builder = PooledHashSet<SourceInfo>.GetInstance();

            builder.AddSourceInfo(
                WellKnownTypeNames.SystemConvert,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: new (MethodMatcher, ValueContentCheck[])[]{
                    (
                        (methodName, arguments) =>
                            methodName == "FromBase64String",
                        new ValueContentCheck[]{
                            (argumentPointsTos, argumentValueContents) => argumentValueContents.All(o => o.IsLiteralState),
                        }
                    ),
                });
            builder.AddSourceInfoSpecifyingTaintedTargets(
                WellKnownTypeNames.SystemTextEncoding,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: new (MethodMatcher, (ValueContentCheck, string)[])[]{
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Count() == 5 &&
                            arguments[0].Parameter?.Type.SpecialType == SpecialType.System_String,
                        new (ValueContentCheck, string)[]{
                            (
                                (argumentPointsTos, argumentValueContents) =>
                                    argumentValueContents[0].IsLiteralState,
                                "chars"
                            ),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Count() == 1 &&
                            arguments[0].Parameter?.Type.SpecialType == SpecialType.System_String,
                        new (ValueContentCheck, string)[]{
                            (
                                (argumentPointsTos, argumentValueContents) =>
                                    argumentValueContents[0].IsLiteralState,
                                TaintedTargetValue.Return
                            ),
                        }
                    ),
                },
                transferMethods: new (MethodMatcher, (string, string)[])[]{
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Count() == 5 &&
                            arguments[0].Parameter?.Type is IArrayTypeSymbol arrayTypeSymbol &&
                            arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_Char,
                        new (string, string)[]{
                            ("chars", "bytes"),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Count() == 5 &&
                            arguments[0].Parameter?.Type.SpecialType == SpecialType.System_String,
                        new (string, string)[]{
                            ("chars", "bytes"),
                        }
                    ),
                });
            builder.AddSourceInfo(
                WellKnownTypeNames.SystemByte,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: null,
                taintConstantArray: true);

            SourceInfos = builder.ToImmutableAndFree();
        }
    }
}
