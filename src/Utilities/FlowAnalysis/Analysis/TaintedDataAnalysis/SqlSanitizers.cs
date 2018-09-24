using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{

    internal static class SqlSanitizers
    {
        private static readonly Dictionary<string, SanitizerInfo> ConcreteSanitizers = new Dictionary<string, SanitizerInfo>();

        static SqlSanitizers()
        {
            // TODO paulming: FxCop had sanitizers for SqlParameteresque classes, but necessary here?
            // Right now, we're not tainting any object that receives tainted data in its constructor,
            // and SqlParameter classes (usually) don't hit any of the SQL sinks anyway.  Considering
            // that the SQL sinks are strings, it'd still be bad for a string SqlParameter.Value to
            // enter the sink.
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
