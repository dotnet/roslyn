// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;
    using Roslyn.Utilities;

    /// <summary>
    /// Class representing information about programming constructs like variables, classes, interfaces, etc.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#symbolInformation">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class SymbolInformation : IEquatable<SymbolInformation>
    {

        // Code has to reference SymbolInformation in a SumType even if it's not using the class itself.
        // This means that if we deprecate the type itself, referencing code would have to suppress
        // deprecation  warnings even if they are only using non-deprecated types. We work around
        // by deprecating the members instead of the type itself.
        const string DeprecationMessage = "The SymbolInformation class is deprecated. Use DocumentSymbol or WorkspaceSymbol instead.";

        /// <summary>
        /// Gets or sets the name of this symbol.
        /// </summary>
        [JsonPropertyName("name")]
        [Obsolete(DeprecationMessage)]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="SymbolKind"/> of this symbol.
        /// </summary>
        [JsonPropertyName("kind")]
        [Obsolete(DeprecationMessage)]
        public SymbolKind Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Tags for this document symbol.
        /// </summary>
        /// <remarks>Since 3.16</remarks>
        [JsonPropertyName("tags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [Obsolete(DeprecationMessage)]
        public SymbolTag[]? Tags { get; init; }

        /// <summary>
        /// Indicates whether this symbol is deprecated.
        /// </summary>
        [JsonPropertyName("deprecated")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [Obsolete("Use the Tags property instead")]
        public bool Deprecated { get; init; }

        /// <summary>
        /// The location of this symbol, used by a tool to reveal the location in the editor.
        /// <para>
        /// If the symbol is selected in the tool the range's start information is used to
        /// position the cursor. So the range usually spans more then the actual symbol's
        /// name and does normally include things like visibility modifiers.
        /// </para>
        /// <para>
        /// The range doesn't have to denote a node range in the sense of an abstract
        /// syntax tree. It can therefore not be used to re-construct a hierarchy of
        /// the symbols.
        /// </para>
        /// </summary>
        [JsonPropertyName("location")]
        [Obsolete(DeprecationMessage)]
        public Location Location
        {
            get;
            set;
        }

        /// <summary>
        /// <para>
        /// The name of the symbol containing this symbol.
        /// </para>
        /// This information is for user interface purposes (e.g. to render a qualifier in
        /// the user interface if necessary). It can't be used to re-infer a hierarchy for
        /// the document symbols.
        /// </summary>
        [JsonPropertyName("containerName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [Obsolete(DeprecationMessage)]
        public string? ContainerName
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as SymbolInformation);
        }

        /// <inheritdoc/>
        public bool Equals(SymbolInformation? other)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return other != null
                && this.Name == other.Name
                && this.Kind == other.Kind
                && (this.Tags == null
                        ? other.Tags == null
                        : (this.Tags.Equals(other.Tags) || this.Tags.SequenceEqual(other.Tags)))
                && this.Deprecated == other.Deprecated
                && EqualityComparer<Location>.Default.Equals(this.Location, other.Location)
                && this.ContainerName == other.ContainerName;
#pragma warning restore CS0618
        }

        /// <inheritdoc/>
        public override int GetHashCode() =>
#pragma warning disable CS0618 // Type or member is obsolete
#if NET
            HashCode.Combine(Name, Kind, Hash.CombineValues(Tags), Deprecated, Location, ContainerName);
#else
            Hash.Combine(Name,
            Hash.Combine((int)Kind,
            Hash.Combine(Hash.CombineValues(Tags),
            Hash.Combine(Deprecated,
            Hash.Combine(ContainerName, Location?.GetHashCode() ?? 0)))));
#endif
#pragma warning restore CS0618
    }
}
