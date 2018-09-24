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
        private static readonly Dictionary<string, SanitizerInfo> ConcreteSanitizers = new Dictionary<string, SanitizerInfo>();

        static PrimitiveTypeConverterSanitizers()
        {
            string[] parseMethods = new string[] { "Parse", "TryParse" };

            AddConcreteSanitizer(
                WellKnownTypes.SystemBoolean,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemByte,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemChar,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemInt16,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemInt32,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemInt64,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemSingle,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemDouble,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemDecimal,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemDateTime,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemTimeSpan,
                isConstructorSanitizing: false,
                sanitizingMethods: parseMethods);
            AddConcreteSanitizer(
                WellKnownTypes.SystemNumber,
                isConstructorSanitizing: false,
                sanitizingMethods: new string[] {
                    "ParseInt32",
                    "ParseInt64",
                    "TryParseInt32",
                    "TryParseInt64"
                });
        }

        private static void AddConcreteSanitizer(
            string fullTypeName,
            bool isConstructorSanitizing,
            string[] sanitizingMethods)
        {
            SanitizerInfo info = new SanitizerInfo(
                fullTypeName,
                isConstructorSanitizing,
                sanitizingMethods != null ? ImmutableHashSet.Create<string>(sanitizingMethods) : ImmutableHashSet<string>.Empty);
            ConcreteSanitizers.Add(fullTypeName, info);
        }

        /// <summary>
        /// Determines if the instance method call returns tainted data.
        /// </summary>
        /// <param name="wellKnownTypeProvider">Well known types cache.</param>
        /// <param name="method">Instance method being called.</param>
        /// <returns>True if the method returns tainted data, false otherwise.</returns>
        public static bool IsSanitizingMethod(WellKnownTypeProvider wellKnownTypeProvider, IMethodSymbol method)
        {
            foreach (SanitizerInfo sanitizerInfo in GetSanitizerInfosForType(wellKnownTypeProvider, method.ContainingType))
            {
                if (method.MethodKind == MethodKind.Constructor
                    && sanitizerInfo.IsConstructorSanitizing)
                {
                    return true;
                }

                if (sanitizerInfo.SanitizingMethods.Contains(method.MetadataName))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<SanitizerInfo> GetSanitizerInfosForType(
            WellKnownTypeProvider wellKnownTypeProvider,
            INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol == null)
            {
                yield break;
            }

            for (INamedTypeSymbol typeSymbol = namedTypeSymbol; typeSymbol != null; typeSymbol = typeSymbol.BaseType)
            {
                if (!wellKnownTypeProvider.TryGetFullTypeName(typeSymbol, out string typeFullName)
                    || !ConcreteSanitizers.TryGetValue(typeFullName, out SanitizerInfo sinkInfo))
                {
                    continue;
                }

                yield return sinkInfo;
            }
        }
    }
}
