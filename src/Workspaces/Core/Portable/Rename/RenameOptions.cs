// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Rename
{
    [Obsolete("Use SymbolRenameOptions or DocumentRenameOptions instead")]
    public static class RenameOptions
    {
        public static Option<bool> RenameOverloads { get; } = new Option<bool>(nameof(RenameOptions), nameof(RenameOverloads), defaultValue: false);

        public static Option<bool> RenameInStrings { get; } = new Option<bool>(nameof(RenameOptions), nameof(RenameInStrings), defaultValue: false);

        public static Option<bool> RenameInComments { get; } = new Option<bool>(nameof(RenameOptions), nameof(RenameInComments), defaultValue: false);

        public static Option<bool> PreviewChanges { get; } = new Option<bool>(nameof(RenameOptions), nameof(PreviewChanges), defaultValue: false);
    }

    /// <summary>
    /// Options for renaming a symbol.
    /// </summary>
    /// <param name="RenameOverloads">If the symbol is a method rename its overloads as well.</param>
    /// <param name="RenameInStrings">Rename identifiers in string literals that match the name of the symbol.</param>
    /// <param name="RenameInComments">Rename identifiers in comments that match the name of the symbol.</param>
    /// <param name="RenameFile">If the symbol is a type renames the file containing the type declaration as well.</param>
    [DataContract]
    public readonly record struct SymbolRenameOptions(
        [property: DataMember(Order = 0)] bool RenameOverloads = false,
        [property: DataMember(Order = 1)] bool RenameInStrings = false,
        [property: DataMember(Order = 2)] bool RenameInComments = false,
        [property: DataMember(Order = 3)] bool RenameFile = false);

    /// <summary>
    /// Options for renaming a document.
    /// </summary>
    /// <param name="RenameMatchingTypeInStrings">If the document contains a type declaration with matching name rename identifiers in strings that match the name as well.</param>
    /// <param name="RenameMatchingTypeInComments">If the document contains a type declaration with matching name rename identifiers in comments that match the name as well.</param>
    [DataContract]
    public readonly record struct DocumentRenameOptions(
        [property: DataMember(Order = 0)] bool RenameMatchingTypeInStrings = false,
        [property: DataMember(Order = 1)] bool RenameMatchingTypeInComments = false);
}
