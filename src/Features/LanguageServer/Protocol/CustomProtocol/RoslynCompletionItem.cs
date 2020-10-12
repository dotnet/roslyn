﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.CustomProtocol
{
    // TODO - This should be deleted when liveshare moves to VSCompletionItem.
    // https://github.com/dotnet/roslyn/projects/45#card-21249665
    [DataContract]
    internal class RoslynCompletionItem : CompletionItem
    {
        /// <summary>
        /// A set of custom tags on a completion item. Roslyn has information here to get icons.
        /// </summary>
        [DataMember(Name = "tags")]
        public string[] Tags { get; set; }

        /// <summary>
        /// The description for a completion item.
        /// </summary>
        [DataMember(Name = "description")]
        public RoslynTaggedText[] Description { get; set; }

        public static RoslynCompletionItem From(CompletionItem completionItem)
        {
            return new RoslynCompletionItem
            {
                AdditionalTextEdits = completionItem.AdditionalTextEdits,
                Command = completionItem.Command,
                CommitCharacters = completionItem.CommitCharacters,
                Data = completionItem.Data,
                Detail = completionItem.Detail,
                Documentation = completionItem.Documentation,
                FilterText = completionItem.FilterText,
                InsertText = completionItem.InsertText,
                InsertTextFormat = completionItem.InsertTextFormat,
                Kind = completionItem.Kind,
                Label = completionItem.Label,
                SortText = completionItem.SortText,
                TextEdit = completionItem.TextEdit
            };
        }
    }
}
