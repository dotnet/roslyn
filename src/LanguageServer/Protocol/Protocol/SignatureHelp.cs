// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the signature of something callable. This class is returned from the textDocument/signatureHelp request.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelp">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class SignatureHelp
    {
        /// <summary>
        /// One or more signatures.
        /// <para>
        /// If no signatures are available the signature help
        /// request should return <see langword="null"/>.
        /// </para>
        /// </summary>
        [JsonPropertyName("signatures")]
        [JsonRequired]
        public SignatureInformation[] Signatures
        {
            get;
            set;
        }

        /// <summary>
        /// The active signature.
        /// <para>
        /// </para>
        /// If omitted or the value lies outside the range of <see cref="Signatures"/> the
        /// value defaults to zero or is ignored if the <see cref="SignatureHelp"/> has no signatures.
        /// <para>
        /// Whenever possible implementors should make an active decision about
        /// the active signature and shouldn't rely on a default value.
        /// </para>
        /// <para>
        /// In a future version of the protocol this property might become
        /// mandatory to better express this.
        /// </para>
        /// </summary>
        [JsonPropertyName("activeSignature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ActiveSignature
        {
            get;
            set;
        }

        /// <summary>
        /// The active parameter of the active signature.
        /// <para>
        /// If omitted or the value lies outside the range of <c>Signatures[ActiveSignature].Parameters</c>
        /// it defaults to 0 if the active signature has parameters.
        /// If the active signature has no parameters it is ignored.
        /// </para>
        /// <para>
        /// In a future version of the protocol this property might become
        /// mandatory to better express the active parameter if the active
        /// signature has any parameters.
        /// </para>
        /// </summary>
        [JsonPropertyName("activeParameter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ActiveParameter
        {
            get;
            set;
        }
    }
}
