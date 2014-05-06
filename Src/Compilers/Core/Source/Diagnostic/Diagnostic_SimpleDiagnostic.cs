// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A diagnostic (such as a compiler error or a warning), along with the location where it occurred.
    /// </summary>
    public abstract partial class Diagnostic
    {
        [Serializable]
        internal sealed class SimpleDiagnostic : Diagnostic, ISerializable
        {
            private readonly string id;
            private readonly string category;
            private readonly string message;
            private readonly DiagnosticSeverity severity;
            private readonly bool isEnabledByDefault;
            private readonly int warningLevel;
            private readonly bool isWarningAsError;
            private readonly Location location;
            private readonly IReadOnlyList<Location> additionalLocations;
            private readonly IReadOnlyList<string> customTags;

            internal SimpleDiagnostic(string id, string category, string message, DiagnosticSeverity severity, bool isEnabledByDefault,
                                      int warningLevel, bool isWarningAsError, Location location,
                                      IEnumerable<Location> additionalLocations, IEnumerable<string> customTags)
            {
                if (isWarningAsError && severity != DiagnosticSeverity.Warning)
                {
                    throw new ArgumentException("isWarningAsError");
                }

                if ((warningLevel == 0 && severity == DiagnosticSeverity.Warning) ||
                    (warningLevel != 0 && severity != DiagnosticSeverity.Warning))
                {
                    throw new ArgumentException("warningLevel");
                }

                this.id = id;
                this.category = category;
                this.message = message;
                this.severity = severity;
                this.isEnabledByDefault = isEnabledByDefault;
                this.warningLevel = warningLevel;
                this.isWarningAsError = isWarningAsError;
                this.location = location;
                this.additionalLocations = additionalLocations == null ? SpecializedCollections.EmptyReadOnlyList<Location>() : additionalLocations.ToImmutableArray();
                this.customTags = customTags == null ? SpecializedCollections.EmptyReadOnlyList<string>() : customTags.ToImmutableArray();
            }

            private SimpleDiagnostic(SerializationInfo info, StreamingContext context)
            {
                this.id = info.GetString("id");
                this.category = info.GetString("category");
                this.message = info.GetString("message");
                this.severity = (DiagnosticSeverity)info.GetInt32("severity");
                this.isEnabledByDefault = info.GetBoolean("isEnabledByDefault");
                this.warningLevel = info.GetInt32("warningLevel");
                this.isWarningAsError = info.GetBoolean("isWarningAsError");
                this.location = (Location)info.GetValue("location", typeof(Location));
                this.additionalLocations = ((Location[])info.GetValue("additionalLocations", typeof(Location[]))).ToImmutableArrayOrEmpty();
                this.customTags = ((string[])info.GetValue("customTags", typeof(string[]))).ToImmutableArrayOrEmpty();
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("id", this.id);
                info.AddValue("category", this.category);
                info.AddValue("message", this.message);
                info.AddValue("severity", (int)this.severity);
                info.AddValue("isEnabledByDefault", this.isEnabledByDefault);
                info.AddValue("warningLevel", this.warningLevel);
                info.AddValue("isWarningAsError", this.isWarningAsError);
                info.AddValue("location", this.location, typeof(Location));
                info.AddValue("additionalLocations", this.additionalLocations.ToArray(), typeof(Location[]));
                info.AddValue("customTags", this.customTags.ToArray(), typeof(string[]));
            }

            public override string Id
            {
                get { return this.id; }
            }

            public override string Category
            {
                get { return this.category; }
            }

            public override string GetMessage(CultureInfo culture = null)
            {
                return this.message;
            }

            public override DiagnosticSeverity Severity
            {
                get { return this.severity; }
            }

            public override bool IsEnabledByDefault
            {
                get { return isEnabledByDefault; }
            }

            public override int WarningLevel
            {
                get { return this.warningLevel; }
            }

            public override bool IsWarningAsError
            {
                get { return this.isWarningAsError; }
            }

            public override Location Location
            {
                get { return this.location; }
            }

            public override IReadOnlyList<Location> AdditionalLocations
            {
                get { return this.additionalLocations; }
            }

            public override IReadOnlyList<string> CustomTags
            {
                get { return this.customTags; }
            }

            public override bool Equals(Diagnostic obj)
            {
                var other = obj as SimpleDiagnostic;
                return other != null
                    && this.id == other.id
                    && this.message == other.message
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
                return Hash.Combine(this.id,
                        Hash.Combine(this.message.GetHashCode(),
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
                    return new SimpleDiagnostic(this.id, this.category, this.message, this.severity, this.isEnabledByDefault, this.warningLevel, this.isWarningAsError, location, this.additionalLocations, this.customTags);
                }

                return this;
            }

            internal override Diagnostic WithWarningAsError(bool isWarningAsError)
            {
                if (this.isWarningAsError != isWarningAsError)
                {
                    return new SimpleDiagnostic(this.id, this.category, this.message, this.severity, this.isEnabledByDefault, this.warningLevel, isWarningAsError, this.location, this.additionalLocations, this.customTags);
                }

                return this;
            }

            internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
            {
                Debug.Assert(severity != DiagnosticSeverity.Error || this.severity != DiagnosticSeverity.Warning, "Use WithWarningAsError API");

                if (this.Severity != severity)
                {
                    return new SimpleDiagnostic(this.id, this.category, this.message, severity, this.isEnabledByDefault, severity == DiagnosticSeverity.Warning ? 1 : 0, isWarningAsError, this.location, this.additionalLocations, this.customTags);
                }

                return this;
            }
        }
    }
}