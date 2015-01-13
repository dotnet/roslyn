// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A diagnostic (such as a compiler error or a warning), along with the location where it occurred.
    /// </summary>
    public abstract partial class Diagnostic
    {
        internal sealed class SimpleDiagnostic : Diagnostic
        {
            private readonly DiagnosticDescriptor descriptor;
            private readonly DiagnosticSeverity severity;
            private readonly int warningLevel;
            private readonly Location location;
            private readonly IReadOnlyList<Location> additionalLocations;
            private readonly object[] messageArgs;

            private SimpleDiagnostic(
                DiagnosticDescriptor descriptor,
                DiagnosticSeverity severity, 
                int warningLevel, 
                Location location,
                IEnumerable<Location> additionalLocations,
                object[] messageArgs)
            {
                if ((warningLevel == 0 && severity != DiagnosticSeverity.Error) ||
                    (warningLevel != 0 && severity == DiagnosticSeverity.Error))
                {
                    throw new ArgumentException("warningLevel");
                }

                this.descriptor = descriptor;
                this.severity = severity;
                this.warningLevel = warningLevel;
                this.location = location;
                this.additionalLocations = additionalLocations == null ? SpecializedCollections.EmptyReadOnlyList<Location>() : additionalLocations.ToImmutableArray();
                this.messageArgs = messageArgs ?? SpecializedCollections.EmptyArray<object>();
            }

            internal static SimpleDiagnostic Create(
                DiagnosticDescriptor descriptor,
                DiagnosticSeverity severity,
                int warningLevel,
                Location location,
                IEnumerable<Location> additionalLocations,
                object[] messageArgs)
            {
                return new SimpleDiagnostic(descriptor, severity, warningLevel, location, additionalLocations, messageArgs);
            }

            internal static SimpleDiagnostic Create(string id, LocalizableString title, string category, LocalizableString message, LocalizableString description, string helpLink,
                                      DiagnosticSeverity severity, DiagnosticSeverity defaultSeverity,
                                      bool isEnabledByDefault, int warningLevel, Location location,
                                      IEnumerable<Location> additionalLocations, IEnumerable<string> customTags)
            {
                var descriptor = new DiagnosticDescriptor(id, title, message,
                     category, defaultSeverity, isEnabledByDefault, description, helpLink, customTags.ToImmutableArrayOrEmpty());
                return new SimpleDiagnostic(descriptor, severity, warningLevel, location, additionalLocations, messageArgs: null);
            }

            public override DiagnosticDescriptor Descriptor
            {
                get { return this.descriptor; }
            }

            public override string Id
            {
                get { return this.descriptor.Id; }
            }

            public override string GetMessage(IFormatProvider formatProvider = null)
            {
                if (this.messageArgs.Length == 0)
                {
                    return this.descriptor.MessageFormat.ToString(formatProvider);
                }

                var localizedMessageFormat = this.descriptor.MessageFormat.ToString(formatProvider);
                return string.Format(formatProvider, localizedMessageFormat, this.messageArgs);
            }

            internal override IReadOnlyList<object> Arguments
            {
                get { return this.messageArgs; }
            }

            public override DiagnosticSeverity Severity
            {
                get { return this.severity; }
            }

            public override int WarningLevel
            {
                get { return this.warningLevel; }
            }

            public override Location Location
            {
                get { return this.location; }
            }

            public override IReadOnlyList<Location> AdditionalLocations
            {
                get { return this.additionalLocations; }
            }

            public override bool Equals(Diagnostic obj)
            {
                var other = obj as SimpleDiagnostic;
                return other != null
                    && this.descriptor == other.descriptor
                    && this.messageArgs.SequenceEqual(other.messageArgs, (a, b) => a == b)
                    && this.location == other.location
                    && this.severity == other.severity
                    && this.warningLevel == other.warningLevel;
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as Diagnostic);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(this.descriptor,
                        Hash.Combine(this.messageArgs.GetHashCode(),
                         Hash.Combine(this.location.GetHashCode(),
                          Hash.Combine(this.severity.GetHashCode(), this.warningLevel)
                        )));
            }

            internal override Diagnostic WithLocation(Location location)
            {
                if (location == null)
                {
                    throw new ArgumentNullException("location");
                }

                if (location != this.location)
                {
                    return new SimpleDiagnostic(this.descriptor, this.severity, this.warningLevel, location, this.additionalLocations, this.messageArgs);
                }

                return this;
            }

            internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
            {
                if (this.Severity != severity)
                {
                    var warningLevel = GetDefaultWarningLevel(severity);
                    return new SimpleDiagnostic(this.descriptor, severity, warningLevel, location, this.additionalLocations, this.messageArgs);
                }

                return this;
            }
        }
    }
}