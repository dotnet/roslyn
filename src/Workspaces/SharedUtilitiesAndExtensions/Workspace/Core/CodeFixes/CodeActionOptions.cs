// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.ImplementType;
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
        [property: DataMember(Order = 1)] ImplementTypeOptions ImplementTypeOptions,
        [property: DataMember(Order = 2)] ExtractMethodOptions ExtractMethodOptions,
        [property: DataMember(Order = 3)] bool HideAdvancedMembers = false,
        [property: DataMember(Order = 4)] bool IsBlocking = false,
        [property: DataMember(Order = 5)] int WrappingColumn = CodeActionOptions.DefaultWrappingColumn)
    {
        /// <summary>
        /// Default value of 120 was picked based on the amount of code in a github.com diff at 1080p.
        /// That resolution is the most common value as per the last DevDiv survey as well as the latest
        /// Steam hardware survey.  This also seems to a reasonable length default in that shorter
        /// lengths can often feel too cramped for .NET languages, which are often starting with a
        /// default indentation of at least 16 (for namespace, class, member, plus the final construct
        /// indentation).
        /// 
        /// TODO: Currently the option has no storage and always has its default value. See https://github.com/dotnet/roslyn/pull/30422#issuecomment-436118696.
        /// </summary>
        public const int DefaultWrappingColumn = 120;

        public CodeActionOptions()
            : this(searchOptions: null)
        {
        }

        public CodeActionOptions(
            SymbolSearchOptions? searchOptions = null,
            ImplementTypeOptions? implementTypeOptions = null,
            ExtractMethodOptions? extractMethodOptions = null)
            : this(SearchOptions: searchOptions ?? SymbolSearchOptions.Default,
                   ImplementTypeOptions: implementTypeOptions ?? ImplementTypeOptions.Default,
                   ExtractMethodOptions: extractMethodOptions ?? ExtractMethodOptions.Default)
        {
        }

        public static readonly CodeActionOptions Default = new();
    }
#endif

    internal delegate CodeActionOptions CodeActionOptionsProvider(string language);
}
