// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Derived version of DocumentSymbol used so we can pass additional information we do not want lost for our
    /// internal consumers.  For example, the richer <see cref="Glyph"/> which is a superset of <see
    /// cref="DocumentSymbol.Kind"/>.
    /// </summary>
    internal sealed class RoslynDocumentSymbol : DocumentSymbol
    {
        [DataMember(IsRequired = false, Name = "glyph")]
        public int Glyph { get; set; }

        // Deliberately override the value in the base.
        [DataMember(Name = "children")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public new RoslynDocumentSymbol[]? Children { get; set; }
    }
}
