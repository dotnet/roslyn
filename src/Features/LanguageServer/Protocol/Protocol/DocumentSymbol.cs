// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents programming constructs like variables, classes, interfaces etc. that appear in a document. Document symbols can be
    /// hierarchical and they have two ranges: one that encloses its definition and one that points to its most interesting range,
    /// e.g. the range of an identifier.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentSymbol">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DocumentSymbol
    {
        /// <summary>
        /// Gets or sets the name of this symbol.
        /// </summary>
        [DataMember(IsRequired = true, Name = "name")]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets more detail for this symbol, e.g the signature of a function.
        /// </summary>
        [DataMember(Name = "detail")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Detail
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="SymbolKind" /> of this symbol.
        /// </summary>
        [DataMember(Name = "kind")]
        public SymbolKind Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this symbol is deprecated.
        /// </summary>
        [DataMember(Name = "deprecated")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Deprecated
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range enclosing this symbol not including leading/trailing whitespace but everything else
        /// like comments.This information is typically used to determine if the clients cursor is
        /// inside the symbol to reveal in the symbol in the UI.
        /// </summary>
        [DataMember(IsRequired = true, Name = "range")]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range that should be selected and revealed when this symbol is being picked, e.g the name of a function.
        /// Must be contained by the `range`.
        /// </summary>
        [DataMember(IsRequired = true, Name = "selectionRange")]
        public Range SelectionRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the children of this symbol, e.g. properties of a class.
        /// </summary>
        [DataMember(Name = "children")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DocumentSymbol[]? Children
        {
            get;
            set;
        }
    }
}
