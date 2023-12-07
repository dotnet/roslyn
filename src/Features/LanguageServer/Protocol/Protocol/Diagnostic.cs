// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents a source code diagnostic message.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#diagnostic">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class Diagnostic : IEquatable<Diagnostic>
    {
        /// <summary>
        /// Gets or sets the source code range.
        /// </summary>
        [DataMember(Name = "range")]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the diagnostic severity.
        /// </summary>
        [DataMember(Name = "severity")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticSeverity? Severity
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the diagnostic's code, which usually appear in the user interface.
        /// </summary>
        /// <remarks>
        /// The value can be an <see cref="int"/>, <see cref="string"/>.
        /// </remarks>
        [DataMember(Name = "code")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<int, string>? Code
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an optional value that describes the error code.
        /// </summary>
        [DataMember(Name = "codeDescription")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CodeDescription? CodeDescription
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a human-readable string describing the source of this
        /// diagnostic, e.g. 'typescript' or 'super lint'. It usually appears in the user interface.
        /// </summary>
        [DataMember(Name = "source")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Source
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the diagnostic's message.
        /// </summary>
        [DataMember(Name = "message")]
        public string Message
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the diagnostic's tags.
        /// </summary>
        [DataMember(Name = "tags")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticTag[]? Tags
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the diagnostic related information
        /// </summary>
        [DataMember(Name = "relatedInformation")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticRelatedInformation[]? RelatedInformation
        {
            get;
            set;
        }

        public static bool operator ==(Diagnostic? value1, Diagnostic? value2)
        {
            if (ReferenceEquals(value1, value2))
            {
                return true;
            }

            // Is null?
            if (ReferenceEquals(null, value2))
            {
                return false;
            }

            return value1?.Equals(value2) ?? false;
        }

        public static bool operator !=(Diagnostic? value1, Diagnostic? value2)
        {
            return !(value1 == value2);
        }

        /// <inheritdoc/>
        public bool Equals(Diagnostic other)
        {
            return other is not null
                && this.Range == other.Range
                && this.Severity == other.Severity
                && object.Equals(this.Code, other.Code)
                && this.CodeDescription == other.CodeDescription
                && string.Equals(this.Source, other.Source, StringComparison.Ordinal)
                && string.Equals(this.Message, other.Message, StringComparison.Ordinal)
                && (this.Tags == null
                        ? other.Tags == null
                        : this.Tags.Equals(other.Tags) || this.Tags.SequenceEqual(other.Tags));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is Diagnostic other)
            {
                return this.Equals(other);
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (this.Range == null ? 53 : this.Range.GetHashCode() * 13)
                ^ (this.Severity.GetHashCode() * 17)
                ^ (this.Code == null ? 47 : this.Code.GetHashCode() * 19)
                ^ (this.Source == null ? 61 : this.Source.GetHashCode() * 79)
                ^ (this.Message == null ? 83 : this.Message.GetHashCode() * 23)
                ^ (this.Tags == null ? 89 : this.Tags.Sum(t => (int)t) * 73)
                ^ (this.CodeDescription == null ? 23 : this.CodeDescription.GetHashCode() * 29);
        }
    }
}
