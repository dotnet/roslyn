using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class InformationDisclosureSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for information disclosure tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static InformationDisclosureSources()
        {
            ImmutableHashSet<SourceInfo>.Builder sourceInfosBuilder = ImmutableHashSet.CreateBuilder<SourceInfo>();

            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemException,
                isInterface: false,
                taintedProperties: new[] {
                    "Message",
                    "StackTrace",
                },
                taintedMethods: new[] {
                    "ToString",
                });

            SourceInfos = sourceInfosBuilder.ToImmutable();
        }
    }
}