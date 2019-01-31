// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class PrimitiveTypeConverterSanitizers
    {
        /// <summary>
        /// <see cref="SanitizerInfo"/>s for primitive type conversion tainted data sanitizers.
        /// </summary>
        public static ImmutableHashSet<SanitizerInfo> SanitizerInfos { get; }

        static PrimitiveTypeConverterSanitizers()
        {
            var builder = PooledHashSet<SanitizerInfo>.GetInstance();

            string[] parseMethods = new string[] { "Parse", "TryParse" };

            builder.AddSanitizer(
                WellKnownTypes.SystemBoolean,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemByte,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemChar,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemInt16,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemInt32,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemInt64,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemSingle,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemDouble,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemDecimal,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemDateTime,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemTimeSpan,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            builder.AddSanitizer(
                WellKnownTypes.SystemNumber,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new string[] {
                    "ParseInt32",
                    "ParseInt64",
                    "TryParseInt32",
                    "TryParseInt64"
                });

            SanitizerInfos = builder.ToImmutableAndFree();
        }
    }
}
