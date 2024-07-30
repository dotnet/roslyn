// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Value representing the kind of a diagnostic.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter<VSInternalDiagnosticKind>))]
    [TypeConverter(typeof(StringEnumConverter<VSInternalDiagnosticKind>.TypeConverter))]
    internal readonly record struct VSInternalDiagnosticKind(string Value) : IStringEnum
    {
        /// <summary>
        /// Task list diagnostic kind.
        /// </summary>
        public static readonly VSInternalDiagnosticKind Task = new("task");

        /// <summary>
        /// Syntax diagnostic kind.
        /// </summary>
        public static readonly VSInternalDiagnosticKind Syntax = new("syntax");
    }
}
