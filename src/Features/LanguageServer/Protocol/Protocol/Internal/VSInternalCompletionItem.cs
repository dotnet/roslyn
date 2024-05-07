// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Roslyn.Text.Adornments;
    using Newtonsoft.Json;

    /// <summary>
    /// Extension class for CompletionItem with fields specific to Visual Studio functionalities.
    /// </summary>
    [DataContract]
    internal class VSInternalCompletionItem : CompletionItem
    {
        internal const string IconSerializedName = "_vs_icon";
        internal const string DescriptionSerializedName = "_vs_description";
        internal const string VsCommitCharactersSerializedName = "_vs_commitCharacters";
        internal const string VsResolveTextEditOnCommitName = "_vs_resolveTextEditOnCommit";

        /// <summary>
        /// Gets or sets the icon to show for the completion item. In VS, this is more extensive than the completion kind.
        /// </summary>
        [DataMember(Name = IconSerializedName)]
        [JsonConverter(typeof(ImageElementConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ImageElement? Icon { get; set; }

        /// <summary>
        /// Gets or sets the description for a completion item.
        /// </summary>
        [DataMember(Name = DescriptionSerializedName)]
        [JsonConverter(typeof(ClassifiedTextElementConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ClassifiedTextElement? Description { get; set; }

        /// <summary>
        /// Gets or sets the set of characters that will commit completion when this <see cref="CompletionItem" /> is selected.
        /// Allows customization of commit behavior.
        /// If present, client will use this value instead of <see cref="CompletionOptions.AllCommitCharacters"/>.
        /// If absent, client will default to <see cref="CompletionOptions.AllCommitCharacters"/>.
        /// </summary>
        [DataMember(Name = VsCommitCharactersSerializedName)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<string[], VSInternalCommitCharacter[]>? VsCommitCharacters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the client should call <see cref="Methods.TextDocumentCompletionResolve"/> to
        /// get the value of the text edit to commit.
        /// </summary>
        [DataMember(Name = VsResolveTextEditOnCommitName)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool VsResolveTextEditOnCommit { get; set; }
    }
}
