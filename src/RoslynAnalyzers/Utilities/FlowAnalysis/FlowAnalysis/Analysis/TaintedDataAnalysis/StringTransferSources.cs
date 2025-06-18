// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class StringTranferSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for transferring string tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static StringTranferSources()
        {
            var sourceInfosBuilder = PooledHashSet<SourceInfo>.GetInstance();

            sourceInfosBuilder.AddSourceInfoSpecifyingTaintedTargets(
                WellKnownTypeNames.SystemTextStringBuilder,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: null,
                transferProperties: new[] { TaintedDataProperties.IndexerName },
                transferMethods: new (MethodMatcher, (string, string)[])[]{
                    (
                        (methodName, arguments) =>
                            methodName == "Append" &&
                            !arguments.IsEmpty &&
                            arguments[0].Parameter is { } firstParameter &&
                            (firstParameter.Type.SpecialType == SpecialType.System_String ||
                             firstParameter.Type.SpecialType == SpecialType.System_Char ||
                             (firstParameter.Type is IArrayTypeSymbol arrayType &&
                              arrayType.Rank == 1 &&
                              arrayType.ElementType.SpecialType == SpecialType.System_Char) ||
                             (firstParameter.Type is IPointerTypeSymbol pointerType &&
                              pointerType.PointedAtType.SpecialType == SpecialType.System_Char)),
                        new (string, string)[]{
                            ("value", TaintedTargetValue.This),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "AppendFormat",
                        new (string, string)[]{
                            ("format", TaintedTargetValue.This),
                            ("arg0", TaintedTargetValue.This),
                            ("arg1", TaintedTargetValue.This),
                            ("arg2", TaintedTargetValue.This),
                            ("args", TaintedTargetValue.This),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "AppendJoin",
                        new (string, string)[]{
                            ("separator", TaintedTargetValue.This),
                            ("values", TaintedTargetValue.This),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "AppendLine",
                        new (string, string)[]{
                            ("value", TaintedTargetValue.This),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "CopyTo",
                        new (string, string)[]{
                            (TaintedTargetValue.This, "destination"),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "Insert" &&
                            arguments.Length > 1 &&
                            arguments[1].Parameter is { } secondParameter &&
                            (secondParameter.Type.SpecialType == SpecialType.System_String ||
                             secondParameter.Type.SpecialType == SpecialType.System_Char ||
                             (secondParameter.Type is IArrayTypeSymbol arrayType &&
                              arrayType.Rank == 1 &&
                              arrayType.ElementType.SpecialType == SpecialType.System_Char)),
                        new (string, string)[]{
                            ("value", TaintedTargetValue.This),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "Replace",
                        new (string, string)[]{
                            ("newValue", TaintedTargetValue.This),
                            ("newChar", TaintedTargetValue.This),
                        }
                    ),
                });

            SourceInfos = sourceInfosBuilder.ToImmutableAndFree();
        }
    }
}
