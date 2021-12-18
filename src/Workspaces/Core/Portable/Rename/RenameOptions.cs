// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Rename
{
    [Obsolete]
    public static class RenameOptions
    {
        public static Option<bool> RenameOverloads { get; } = new Option<bool>(nameof(RenameOptions), nameof(RenameOverloads), defaultValue: false);

        public static Option<bool> RenameInStrings { get; } = new Option<bool>(nameof(RenameOptions), nameof(RenameInStrings), defaultValue: false);

        public static Option<bool> RenameInComments { get; } = new Option<bool>(nameof(RenameOptions), nameof(RenameInComments), defaultValue: false);

        public static Option<bool> PreviewChanges { get; } = new Option<bool>(nameof(RenameOptions), nameof(PreviewChanges), defaultValue: false);
    }

    [DataContract]
    public readonly record struct SymbolRenameOptions(
        [property: DataMember(Order = 0)] bool RenameOverloads = false,
        [property: DataMember(Order = 1)] bool RenameInStrings = false,
        [property: DataMember(Order = 2)] bool RenameInComments = false,
        [property: DataMember(Order = 3)] bool RenameFile = false);

    [DataContract]
    public readonly record struct DocumentRenameOptions(
        [property: DataMember(Order = 0)] bool RenameMatchingTypeInStrings = false,
        [property: DataMember(Order = 1)] bool RenameMatchingTypeInComments = false);
}
