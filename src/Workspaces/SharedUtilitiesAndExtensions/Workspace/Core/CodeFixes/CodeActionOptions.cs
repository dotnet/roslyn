// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CodeActions
{
#if CODE_STYLE
    /// <summary>
    /// Empty type to avoid excessive ifdefs.
    /// </summary>
    internal readonly struct CodeActionOptions
    {
    }
#else
    /// <summary>
    /// Options available to code fixes that are supplied by the IDE (i.e. not stored in editorconfig).
    /// </summary>
    [DataContract]
    internal readonly record struct CodeActionOptions(
        [property: DataMember(Order = 0)] SymbolSearchOptions SearchOptions,
        [property: DataMember(Order = 1)] bool HideAdvancedMembers = false,
        [property: DataMember(Order = 2)] bool IsBlocking = false)
    {
        public CodeActionOptions()
            : this(SearchOptions: SymbolSearchOptions.Default)
        {
        }

        public static readonly CodeActionOptions Default = new();
    }
#endif

    internal delegate CodeActionOptions CodeActionOptionsProvider(string language);
}
