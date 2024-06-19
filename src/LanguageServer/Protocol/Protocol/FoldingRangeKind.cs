// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Value representing kinds of folding range.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRangeKind">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonConverter(typeof(StringEnumConverter<FoldingRangeKind>))]
    [TypeConverter(typeof(StringEnumConverter<FoldingRangeKind>.TypeConverter))]
    internal readonly record struct FoldingRangeKind(string Value) : IStringEnum
    {
        /// <summary>
        /// Comment folding range.
        /// </summary>
        public static readonly FoldingRangeKind Comment = new("comment");

        /// <summary>
        /// Imports folding range.
        /// </summary>
        public static readonly FoldingRangeKind Imports = new("imports");

        /// <summary>
        /// Region folding range.
        /// </summary>
        public static readonly FoldingRangeKind Region = new("region");
    }
}
