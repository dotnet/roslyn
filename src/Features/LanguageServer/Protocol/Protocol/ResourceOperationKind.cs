// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Value representing the kind of resource operations supported by the client.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#resourceOperationKind">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    [JsonConverter(typeof(StringEnumConverter<ResourceOperationKind>))]
    [TypeConverter(typeof(StringEnumConverter<ResourceOperationKind>.TypeConverter))]
    internal readonly record struct ResourceOperationKind(string Value) : IStringEnum
    {
        /// <summary>
        /// Supports creating new files and folders.
        /// </summary>
        public static readonly ResourceOperationKind Create = new("create");

        /// <summary>
        /// Supports renaming existing files and folders.
        /// </summary>
        public static readonly ResourceOperationKind Rename = new("rename");

        /// <summary>
        /// Supports deleting existing files and folders.
        /// </summary>
        public static readonly ResourceOperationKind Delete = new("delete");
    }
}
