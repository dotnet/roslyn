// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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

            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemBoolean,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemByte,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemChar,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemInt16,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemInt32,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemInt64,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemSingle,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemDouble,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemDecimal,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemDateTime,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemTimeSpan,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                builder,
                WellKnownTypes.SystemNumber,
                isConstructorSanitizing: false,
                sanitizingMethods: new string[] {
                    "ParseInt32",
                    "ParseInt64",
                    "TryParseInt32",
                    "TryParseInt64"
                });

            SanitizerInfos = builder.ToImmutableAndFree();
        }

        private static void AddConcreteSanitizer(
            PooledHashSet<SanitizerInfo> builder,
            string fullTypeName,
            bool isConstructorSanitizing,
            string[] sanitizingMethods)
        {
            SanitizerInfo info = new SanitizerInfo(
                fullTypeName,
                isInterface: false,
                isConstructorSanitizing: isConstructorSanitizing,
                sanitizingMethods: sanitizingMethods != null ? ImmutableHashSet.Create<string>(sanitizingMethods) : ImmutableHashSet<string>.Empty);
            builder.Add(info);
        }
    }
}
