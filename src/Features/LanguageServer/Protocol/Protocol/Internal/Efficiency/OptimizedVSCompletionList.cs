// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// A subclass of the VS LSP protocol extension <see cref="VSInternalCompletionList"/> that has a fast serialization path.
    /// </summary>
    [DataContract]
    [JsonConverter(typeof(OptimizedVSCompletionListJsonConverter))]
    internal sealed class OptimizedVSCompletionList : VSInternalCompletionList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizedVSCompletionList"/> class.
        /// </summary>
        /// <param name="completionList">The completion list to wrap.</param>
        public OptimizedVSCompletionList(VSInternalCompletionList completionList)
        {
            this.Items = completionList.Items;
            this.IsIncomplete = completionList.IsIncomplete;
            this.SuggestionMode = completionList.SuggestionMode;
            this.ContinueCharacters = completionList.ContinueCharacters;
            this.Data = completionList.Data;
            this.CommitCharacters = completionList.CommitCharacters;
            this.ItemDefaults = completionList.ItemDefaults;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizedVSCompletionList"/> class.
        /// </summary>
        /// <param name="completionList">The completion list to wrap.</param>
        public OptimizedVSCompletionList(CompletionList completionList)
        {
            this.Items = completionList.Items;
            this.IsIncomplete = completionList.IsIncomplete;
            this.ItemDefaults = completionList.ItemDefaults;
        }
    }
}
