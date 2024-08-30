// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Value representing the format used to describe semantic tokens.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#tokenFormat">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonConverter(typeof(StringEnumConverter<SemanticTokenFormat>))]
    [TypeConverter(typeof(StringEnumConverter<SemanticTokenFormat>.TypeConverter))]
    internal readonly record struct SemanticTokenFormat(string Value) : IStringEnum
    {
        /// <summary>
        /// Tokens are described using relative positions.
        /// </summary>
        public static readonly SemanticTokenFormat Relative = new("relative");
    }
}
