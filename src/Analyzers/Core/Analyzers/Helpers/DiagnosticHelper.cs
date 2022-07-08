// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticHelper
    {
        public readonly struct DiagnosticWrapper
        {
            /// <summary>
            /// Code-style options can have a ReportDiagnostic.Suppress severity. Such diagnostics shouldn't
            /// be reported, so to do that we want to create a "null" diagnostic.
            /// Because context.ReportDiagnostic doesn't take null, and it's tedious to do null check in every analyzer,
            /// we create extension methods that check for null. But the parameter type has to be different for the
            /// compiler to resolve it during overload resolution. So we use DiagnosticWrapper as the parameter type.
            /// </summary>
            /// <param name="diagnostic"></param>
            public DiagnosticWrapper(Diagnostic? diagnostic)
            {
                Diagnostic = diagnostic;
            }

            public Diagnostic? Diagnostic { get; }
        }

        public static void ReportDiagnostic(this AdditionalFileAnalysisContext context, DiagnosticWrapper diagnostic)
        {
            if (diagnostic.Diagnostic is not null)
            {
                context.ReportDiagnostic(diagnostic.Diagnostic);
            }
        }

        public static void ReportDiagnostic(this CodeBlockAnalysisContext context, DiagnosticWrapper diagnostic)
        {
            if (diagnostic.Diagnostic is not null)
            {
                context.ReportDiagnostic(diagnostic.Diagnostic);
            }
        }

        public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticWrapper diagnostic)
        {
            if (diagnostic.Diagnostic is not null)
            {
                context.ReportDiagnostic(diagnostic.Diagnostic);
            }
        }

        public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticWrapper diagnostic)
        {
            if (diagnostic.Diagnostic is not null)
            {
                context.ReportDiagnostic(diagnostic.Diagnostic);
            }
        }

        public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticWrapper diagnostic)
        {
            if (diagnostic.Diagnostic is not null)
            {
                context.ReportDiagnostic(diagnostic.Diagnostic);
            }
        }

        public static void ReportDiagnostic(this SemanticModelAnalysisContext context, DiagnosticWrapper diagnostic)
        {
            if (diagnostic.Diagnostic is not null)
            {
                context.ReportDiagnostic(diagnostic.Diagnostic);
            }
        }

        public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticWrapper diagnostic)
        {
            if (diagnostic.Diagnostic is not null)
            {
                context.ReportDiagnostic(diagnostic.Diagnostic);
            }
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticWrapper diagnostic)
        {
            if (diagnostic.Diagnostic is not null)
            {
                context.ReportDiagnostic(diagnostic.Diagnostic);
            }
        }

        public static void ReportDiagnostic(this SyntaxTreeAnalysisContext context, DiagnosticWrapper diagnostic)
        {
            if (diagnostic.Diagnostic is not null)
            {
                context.ReportDiagnostic(diagnostic.Diagnostic);
            }
        }

        /// <summary>
        /// Creates a <see cref="Diagnostic"/> instance.
        /// </summary>
        /// <param name="descriptor">A <see cref="DiagnosticDescriptor"/> describing the diagnostic.</param>
        /// <param name="location">An optional primary location of the diagnostic. If null, <see cref="Location"/> will return <see cref="Location.None"/>.</param>
        /// <param name="effectiveSeverity">Effective severity of the diagnostic.</param>
        /// <param name="additionalLocations">
        /// An optional set of additional locations related to the diagnostic.
        /// Typically, these are locations of other items referenced in the message.
        /// If null, <see cref="Diagnostic.AdditionalLocations"/> will return an empty list.
        /// </param>
        /// <param name="properties">
        /// An optional set of name-value pairs by means of which the analyzer that creates the diagnostic
        /// can convey more detailed information to the fixer. If null, <see cref="Diagnostic.Properties"/> will return
        /// <see cref="ImmutableDictionary{TKey, TValue}.Empty"/>.
        /// </param>
        /// <param name="messageArgs">Arguments to the message of the diagnostic.</param>
        /// <returns>The <see cref="Diagnostic"/> instance.</returns>
        public static DiagnosticWrapper Create(
            DiagnosticDescriptor descriptor,
            Location location,
            ReportDiagnostic effectiveSeverity,
            IEnumerable<Location>? additionalLocations,
            ImmutableDictionary<string, string?>? properties,
            params object[] messageArgs)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (effectiveSeverity == CodeAnalysis.ReportDiagnostic.Suppress)
            {
                return new(null);
            }

            return new(Diagnostic.Create(descriptor, location, effectiveSeverity.ToDiagnosticSeverity() ?? descriptor.DefaultSeverity, additionalLocations, properties, messageArgs));
        }

        /// <summary>
        /// Create a diagnostic that adds properties specifying a tag for a set of locations.
        /// </summary>
        /// <param name="descriptor">A <see cref="DiagnosticDescriptor"/> describing the diagnostic.</param>
        /// <param name="location">An optional primary location of the diagnostic. If null, <see cref="Location"/> will return <see cref="Location.None"/>.</param>
        /// <param name="effectiveSeverity">Effective severity of the diagnostic.</param>
        /// <param name="additionalLocations">
        /// An optional set of additional locations related to the diagnostic.
        /// Typically, these are locations of other items referenced in the message.
        /// These locations are joined with <paramref name="additionalUnnecessaryLocations"/> to produce the value for
        /// <see cref="Diagnostic.AdditionalLocations"/>.
        /// </param>
        /// <param name="additionalUnnecessaryLocations">
        /// An optional set of additional locations indicating unnecessary code related to the diagnostic.
        /// These locations are joined with <paramref name="additionalLocations"/> to produce the value for
        /// <see cref="Diagnostic.AdditionalLocations"/>.
        /// </param>
        /// <param name="messageArgs">Arguments to the message of the diagnostic.</param>
        /// <returns>The <see cref="Diagnostic"/> instance.</returns>
        public static DiagnosticWrapper CreateWithLocationTags(
            DiagnosticDescriptor descriptor,
            Location location,
            ReportDiagnostic effectiveSeverity,
            ImmutableArray<Location> additionalLocations,
            ImmutableArray<Location> additionalUnnecessaryLocations,
            params object[] messageArgs)
        {
            if (additionalUnnecessaryLocations.IsEmpty)
            {
                return Create(descriptor, location, effectiveSeverity, additionalLocations, ImmutableDictionary<string, string?>.Empty, messageArgs);
            }

            var tagIndices = ImmutableDictionary<string, IEnumerable<int>>.Empty
                .Add(WellKnownDiagnosticTags.Unnecessary, Enumerable.Range(additionalLocations.Length, additionalUnnecessaryLocations.Length));
            return CreateWithLocationTags(
                descriptor,
                location,
                effectiveSeverity,
                additionalLocations.AddRange(additionalUnnecessaryLocations),
                tagIndices,
                ImmutableDictionary<string, string?>.Empty,
                messageArgs);
        }

        /// <summary>
        /// Create a diagnostic that adds properties specifying a tag for a set of locations.
        /// </summary>
        /// <param name="descriptor">A <see cref="DiagnosticDescriptor"/> describing the diagnostic.</param>
        /// <param name="location">An optional primary location of the diagnostic. If null, <see cref="Location"/> will return <see cref="Location.None"/>.</param>
        /// <param name="effectiveSeverity">Effective severity of the diagnostic.</param>
        /// <param name="additionalLocations">
        /// An optional set of additional locations related to the diagnostic.
        /// Typically, these are locations of other items referenced in the message.
        /// These locations are joined with <paramref name="additionalUnnecessaryLocations"/> to produce the value for
        /// <see cref="Diagnostic.AdditionalLocations"/>.
        /// </param>
        /// <param name="additionalUnnecessaryLocations">
        /// An optional set of additional locations indicating unnecessary code related to the diagnostic.
        /// These locations are joined with <paramref name="additionalLocations"/> to produce the value for
        /// <see cref="Diagnostic.AdditionalLocations"/>.
        /// </param>
        /// <param name="properties">
        /// An optional set of name-value pairs by means of which the analyzer that creates the diagnostic
        /// can convey more detailed information to the fixer.
        /// </param>
        /// <param name="messageArgs">Arguments to the message of the diagnostic.</param>
        /// <returns>The <see cref="Diagnostic"/> instance.</returns>
        public static DiagnosticWrapper CreateWithLocationTags(
            DiagnosticDescriptor descriptor,
            Location location,
            ReportDiagnostic effectiveSeverity,
            ImmutableArray<Location> additionalLocations,
            ImmutableArray<Location> additionalUnnecessaryLocations,
            ImmutableDictionary<string, string?> properties,
            params object[] messageArgs)
        {
            if (additionalUnnecessaryLocations.IsEmpty)
            {
                return Create(descriptor, location, effectiveSeverity, additionalLocations, properties, messageArgs);
            }

            var tagIndices = ImmutableDictionary<string, IEnumerable<int>>.Empty
                .Add(WellKnownDiagnosticTags.Unnecessary, Enumerable.Range(additionalLocations.Length, additionalUnnecessaryLocations.Length));
            return CreateWithLocationTags(
                descriptor,
                location,
                effectiveSeverity,
                additionalLocations.AddRange(additionalUnnecessaryLocations),
                tagIndices,
                properties,
                messageArgs);
        }

        /// <summary>
        /// Create a diagnostic that adds properties specifying a tag for a set of locations.
        /// </summary>
        /// <param name="descriptor">A <see cref="DiagnosticDescriptor"/> describing the diagnostic.</param>
        /// <param name="location">An optional primary location of the diagnostic. If null, <see cref="Location"/> will return <see cref="Location.None"/>.</param>
        /// <param name="effectiveSeverity">Effective severity of the diagnostic.</param>
        /// <param name="additionalLocations">
        /// An optional set of additional locations related to the diagnostic.
        /// Typically, these are locations of other items referenced in the message.
        /// </param>
        /// <param name="tagIndices">
        /// a map of location tag to index in additional locations.
        /// "AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer" for an example of usage.
        /// </param>
        /// <param name="properties">
        /// An optional set of name-value pairs by means of which the analyzer that creates the diagnostic
        /// can convey more detailed information to the fixer.
        /// </param>
        /// <param name="messageArgs">Arguments to the message of the diagnostic.</param>
        /// <returns>The <see cref="Diagnostic"/> instance.</returns>
        private static DiagnosticWrapper CreateWithLocationTags(
            DiagnosticDescriptor descriptor,
            Location location,
            ReportDiagnostic effectiveSeverity,
            IEnumerable<Location> additionalLocations,
            IDictionary<string, IEnumerable<int>> tagIndices,
            ImmutableDictionary<string, string?> properties,
            params object[] messageArgs)
        {
            Contract.ThrowIfTrue(additionalLocations.IsEmpty());
            Contract.ThrowIfTrue(tagIndices.IsEmpty());

            properties ??= ImmutableDictionary<string, string?>.Empty;
            properties = properties.AddRange(tagIndices.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, EncodeIndices(kvp.Value, additionalLocations.Count()))));

            return Create(descriptor, location, effectiveSeverity, additionalLocations, properties, messageArgs);

            static string EncodeIndices(IEnumerable<int> indices, int additionalLocationsLength)
            {
                // Ensure that the provided tag index is a valid index into additional locations.
                Contract.ThrowIfFalse(indices.All(idx => idx >= 0 && idx < additionalLocationsLength));

                using var stream = new MemoryStream();
                var serializer = new DataContractJsonSerializer(typeof(IEnumerable<int>));
                serializer.WriteObject(stream, indices);

                var jsonBytes = stream.ToArray();
                stream.Close();
                return Encoding.UTF8.GetString(jsonBytes, 0, jsonBytes.Length);
            }
        }

        public static string? GetHelpLinkForDiagnosticId(string id)
        {
            // TODO: Add documentation for Regex and Json analyzer
            // Tracked with https://github.com/dotnet/roslyn/issues/48530
            if (id == "RE0001")
                return null;

            if (id.StartsWith("JSON", StringComparison.Ordinal))
                return null;

            Debug.Assert(id.StartsWith("IDE", StringComparison.Ordinal));
            return $"https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{id.ToLowerInvariant()}";
        }
    }
}
