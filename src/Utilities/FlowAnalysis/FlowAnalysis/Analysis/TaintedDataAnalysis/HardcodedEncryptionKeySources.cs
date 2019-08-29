// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;

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
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: new (MethodMapper, ValueContentCheck[])[]{
                    (
                        (methodName, arguments) =>
                            methodName == "FromBase64String",
                        new ValueContentCheck[]{
                            (argumentPonitsTos, argumentValueContents) => argumentValueContents.All(o => o.IsLiteralState),
                        }
                    ),
                });
            builder.AddSourceInfoSpecifyingTaintedTargets(
                WellKnownTypeNames.SystemTextEncoding,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: new (MethodMapper, (ValueContentCheck, string[])[])[]{
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Count() == 5 &&
                            arguments[0].Parameter.Type.SpecialType == SpecialType.System_String,
                        new (ValueContentCheck, string[])[]{
                            (
                                (argumentPonitsTos, argumentValueContents) =>
                                    argumentValueContents[0].IsLiteralState,
                                new[] { "chars", }
                            ),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Count() == 1 &&
                            arguments[0].Parameter.Type.SpecialType == SpecialType.System_String,
                        new (ValueContentCheck, string[])[]{
                            (
                                (argumentPonitsTos, argumentValueContents) =>
                                    argumentValueContents[0].IsLiteralState,
                                new[] { TaintedTargetValue.Return, }
                            ),
                        }
                    ),
                },
                passerMethods: new (MethodMapper, (string, string)[])[]{
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Count() == 5 &&
                            arguments[0].Parameter.Type is IArrayTypeSymbol arrayTypeSymbol &&
                            arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_Char,
                        new (string, string)[]{
                            ("chars", "bytes"),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Count() == 5 &&
                            arguments[0].Parameter.Type.SpecialType == SpecialType.System_String,
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
            builder.AddSourceInfo(
                WellKnownTypeNames.SystemChar,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: null,
                taintConstantArray: true);

            SourceInfos = builder.ToImmutableAndFree();
        }
    }
}
