﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            IEnumerable<Location> additionalLocations,
            ImmutableDictionary<string, string> properties,
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
            IEnumerable<Location> additionalLocations,
            ImmutableDictionary<string, string> properties,
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

        public sealed class LocalizableStringWithArguments : LocalizableString, IObjectWritable
        {
            private readonly LocalizableString _messageFormat;
            private readonly string[] _formatArguments;

            static LocalizableStringWithArguments()
            {
                ObjectBinder.RegisterTypeReader(typeof(LocalizableStringWithArguments), reader => new LocalizableStringWithArguments(reader));
            }

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
                    var argumentsBuilder = ArrayBuilder<string>.GetInstance(length);
                    for (var i = 0; i < length; i++)
                    {
                        argumentsBuilder.Add(reader.ReadString());
                    }

                    _formatArguments = argumentsBuilder.ToArrayAndFree();
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

            protected override string GetText(IFormatProvider formatProvider)
            {
                var messageFormat = _messageFormat.ToString(formatProvider);
                return messageFormat != null ?
                    (_formatArguments.Length > 0 ? string.Format(formatProvider, messageFormat, _formatArguments) : messageFormat) :
                    string.Empty;
            }

            protected override bool AreEqual(object other)
            {
                var otherResourceString = other as LocalizableStringWithArguments;
                return otherResourceString != null &&
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
