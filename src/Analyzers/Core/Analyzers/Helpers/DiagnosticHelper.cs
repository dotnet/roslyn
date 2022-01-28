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
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticHelper
    {
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
        public static Diagnostic Create(
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

            LocalizableString message;
            if (messageArgs == null || messageArgs.Length == 0)
            {
                message = descriptor.MessageFormat;
            }
            else
            {
                message = new LocalizableStringWithArguments(descriptor.MessageFormat, messageArgs);
            }

            return CreateWithMessage(descriptor, location, effectiveSeverity, additionalLocations, properties, message);
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
        public static Diagnostic CreateWithLocationTags(
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
        public static Diagnostic CreateWithLocationTags(
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
        private static Diagnostic CreateWithLocationTags(
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
        /// <param name="message">Localizable message for the diagnostic.</param>
        /// <returns>The <see cref="Diagnostic"/> instance.</returns>
        public static Diagnostic CreateWithMessage(
            DiagnosticDescriptor descriptor,
            Location location,
            ReportDiagnostic effectiveSeverity,
            IEnumerable<Location>? additionalLocations,
            ImmutableDictionary<string, string?>? properties,
            LocalizableString message)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return Diagnostic.Create(
                descriptor.Id,
                descriptor.Category,
                message,
                effectiveSeverity.ToDiagnosticSeverity() ?? descriptor.DefaultSeverity,
                descriptor.DefaultSeverity,
                descriptor.IsEnabledByDefault,
                warningLevel: effectiveSeverity.WithDefaultSeverity(descriptor.DefaultSeverity) == ReportDiagnostic.Error ? 0 : 1,
                effectiveSeverity == ReportDiagnostic.Suppress,
                descriptor.Title,
                descriptor.Description,
                descriptor.HelpLinkUri,
                location,
                additionalLocations,
                descriptor.CustomTags,
                properties);
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

        public sealed class LocalizableStringWithArguments : LocalizableString, IObjectWritable
        {
            private readonly LocalizableString _messageFormat;
            private readonly string[] _formatArguments;

            static LocalizableStringWithArguments()
                => ObjectBinder.RegisterTypeReader(typeof(LocalizableStringWithArguments), reader => new LocalizableStringWithArguments(reader));

            public LocalizableStringWithArguments(LocalizableString messageFormat, params object[] formatArguments)
            {
                if (messageFormat == null)
                {
                    throw new ArgumentNullException(nameof(messageFormat));
                }

                if (formatArguments == null)
                {
                    throw new ArgumentNullException(nameof(formatArguments));
                }

                _messageFormat = messageFormat;
                _formatArguments = new string[formatArguments.Length];
                for (var i = 0; i < formatArguments.Length; i++)
                {
                    _formatArguments[i] = $"{formatArguments[i]}";
                }
            }

            private LocalizableStringWithArguments(ObjectReader reader)
            {
                _messageFormat = (LocalizableString)reader.ReadValue();

                var length = reader.ReadInt32();
                if (length == 0)
                {
                    _formatArguments = Array.Empty<string>();
                }
                else
                {
                    using var argumentsBuilderDisposer = ArrayBuilder<string>.GetInstance(length, out var argumentsBuilder);
                    for (var i = 0; i < length; i++)
                    {
                        argumentsBuilder.Add(reader.ReadString());
                    }

                    _formatArguments = argumentsBuilder.ToArray();
                }
            }

            bool IObjectWritable.ShouldReuseInSerialization => false;

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                writer.WriteValue(_messageFormat);
                var length = _formatArguments.Length;
                writer.WriteInt32(length);
                for (var i = 0; i < length; i++)
                {
                    writer.WriteString(_formatArguments[i]);
                }
            }

            protected override string GetText(IFormatProvider? formatProvider)
            {
                var messageFormat = _messageFormat.ToString(formatProvider);
                return messageFormat != null ?
                    (_formatArguments.Length > 0 ? string.Format(formatProvider, messageFormat, _formatArguments) : messageFormat) :
                    string.Empty;
            }

            protected override bool AreEqual(object? other)
            {
                return other is LocalizableStringWithArguments otherResourceString &&
                    _messageFormat.Equals(otherResourceString._messageFormat) &&
                    _formatArguments.SequenceEqual(otherResourceString._formatArguments, (a, b) => a == b);
            }

            protected override int GetHash()
            {
                return Hash.Combine(
                    _messageFormat.GetHashCode(),
                    Hash.CombineValues(_formatArguments));
            }
        }
    }
}
