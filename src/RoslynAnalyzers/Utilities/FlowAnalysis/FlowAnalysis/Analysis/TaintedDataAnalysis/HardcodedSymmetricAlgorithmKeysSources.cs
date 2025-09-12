// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class HardcodedSymmetricAlgorithmKeysSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for hardcoded symmetric algorithm cryptographic keys tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; } = BuildSources();

        /// <summary>
        /// Statically constructs.
        /// </summary>
        private static ImmutableHashSet<SourceInfo> BuildSources()
        {
            var builder = PooledHashSet<SourceInfo>.GetInstance();

            builder.AddSourceInfo(
                WellKnownTypeNames.SystemConvert,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: new (MethodMatcher, ValueContentCheck[])[]
                {
                    (
                        (methodName, _) =>
                            methodName == "FromBase64String",
                        new ValueContentCheck[]
                        {
                            (argumentPointsTos, argumentValueContents) =>
                                argumentValueContents.Length == 1
                                && argumentValueContents[0].LiteralValues.Any(
                                    v => v is string s && s.Length % 4 == 0 && IsLegalKeySize(s.Length * 3 / 4))
                        }
                    ),
                });

            // Consider also checking the resulting lengths of Encoding.GetBytes() with IsLegalKeySize(). ValueContentCheck
            // would need to be aware of concrete type.
            builder.AddSourceInfoSpecifyingTaintedTargets(
                WellKnownTypeNames.SystemTextEncoding,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: new (MethodMatcher, (ValueContentCheck, string)[])[]
                {
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Length == 5 &&
                            arguments[0].Parameter?.Type.SpecialType == SpecialType.System_String,
                        new (ValueContentCheck, string)[]
                        {
                            (
                                (argumentPointsTos, argumentValueContents) => argumentValueContents[0].IsLiteralState,
                                "chars"
                            ),
                        }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Length == 1 &&
                            arguments[0].Parameter?.Type.SpecialType == SpecialType.System_String,
                        new (ValueContentCheck, string)[]
                        {
                            (
                                (argumentPointsTos, argumentValueContents) => argumentValueContents[0].IsLiteralState,
                                TaintedTargetValue.Return
                            ),
                        }
                    ),
                },
                transferMethods: new (MethodMatcher, (string, string)[])[]
                {
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Length == 5 &&
                            arguments[0].Parameter?.Type is IArrayTypeSymbol arrayTypeSymbol &&
                            arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_Char,
                        new (string, string)[] { ("chars", "bytes"), }
                    ),
                    (
                        (methodName, arguments) =>
                            methodName == "GetBytes" &&
                            arguments.Length == 5 &&
                            arguments[0].Parameter?.Type.SpecialType == SpecialType.System_String,
                        new (string, string)[] { ("chars", "bytes"), }
                    ),
                });
            builder.AddSourceInfo(
                WellKnownTypeNames.SystemByte,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: null,
                taintConstantArray: true,
                constantArrayLengthMatcher: IsLegalKeySize);

            return builder.ToImmutableAndFree();
        }

        private static bool IsLegalKeySize(int byteCount)
        {
            /*
                These classes' keys are sinks:
                    System.Security.Cryptography.AesCng
                    System.Security.Cryptography.AesCryptoServiceProvider
                    System.Security.Cryptography.AesManaged
                    System.Security.Cryptography.DESCryptoServiceProvider
                    System.Security.Cryptography.RC2CryptoServiceProvider
                    System.Security.Cryptography.RijndaelManaged
                    System.Security.Cryptography.TripleDESCng
                    System.Security.Cryptography.TripleDESCryptoServiceProvider

                LegalKeySizes in bits:
                    MinSize MaxSize SkipSize
                    ------- ------- --------
                        128     256       64
                        128     256       64
                        128     256       64
                         64      64        0
                         40     128        8
                        128     256       64
                        128     192       64
                        128     192       64

                So the set of legal sizes in bytes is:
                    5-16, 24, 32
            */

            return byteCount is (>= 5 and <= 16) or 24 or 32;
        }
    }
}
