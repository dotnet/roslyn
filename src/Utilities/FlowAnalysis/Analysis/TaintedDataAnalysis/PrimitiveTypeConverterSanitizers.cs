using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{

    internal static class PrimitiveTypeConverterSanitizers
    {
        public static ImmutableDictionary<string, SanitizerInfo> ConcreteSanitizers { get; }

        static PrimitiveTypeConverterSanitizers()
        {
            ImmutableDictionary<string, SanitizerInfo>.Builder builder =
                ImmutableDictionary.CreateBuilder<string, SanitizerInfo>(StringComparer.Ordinal);

            string[] parseMethods = new string[] { "Parse", "TryParse" };

            AddSanitizer(
                builder,
                WellKnownTypes.SystemBoolean,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemByte,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemChar,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemInt16,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemInt32,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemInt64,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemSingle,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemDouble,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemDecimal,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemDateTime,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemTimeSpan,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddSanitizer(
                builder,
                WellKnownTypes.SystemNumber,
                isConstructorSanitizing: false,
                sanitizingMethods: new string[] {
                    "ParseInt32",
                    "ParseInt64",
                    "TryParseInt32",
                    "TryParseInt64"
                });

            ConcreteSanitizers = builder.ToImmutable();
        }

        private static void AddSanitizer(
            ImmutableDictionary<string, SanitizerInfo>.Builder builder,
            string fullTypeName,
            bool isConstructorSanitizing,
            string[] sanitizingMethods)
        {
            SanitizerInfo info = new SanitizerInfo(
                fullTypeName,
                isConstructorSanitizing,
                sanitizingMethods != null ? ImmutableHashSet.Create<string>(sanitizingMethods) : ImmutableHashSet<string>.Empty);
            builder.Add(fullTypeName, info);
        }
    }
}
