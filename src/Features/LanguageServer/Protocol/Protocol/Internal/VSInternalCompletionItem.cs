// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using Roslyn.Text.Adornments;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Extension class for CompletionItem with fields specific to Visual Studio functionalities.
    /// </summary>
    internal class VSInternalCompletionItem : CompletionItem
    {
        internal const string IconSerializedName = "_vs_icon";
        internal const string DescriptionSerializedName = "_vs_description";
        internal const string VsCommitCharactersSerializedName = "_vs_commitCharacters";
        internal const string VsResolveTextEditOnCommitName = "_vs_resolveTextEditOnCommit";

        /// <summary>
        /// Gets or sets the icon to show for the completion item. In VS, this is more extensive than the completion kind.
        /// </summary>
        [JsonPropertyName(IconSerializedName)]
        [JsonConverter(typeof(ImageElementConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImageElement? Icon { get; set; }

        /// <summary>
        /// Gets or sets the description for a completion item.
        /// </summary>
        [JsonPropertyName(DescriptionSerializedName)]
        [JsonConverter(typeof(ClassifiedTextElementConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ClassifiedTextElement? Description { get; set; }

        /// <summary>
        /// Gets or sets the set of characters that will commit completion when this <see cref="CompletionItem" /> is selected.
        /// Allows customization of commit behavior.
        /// If present, client will use this value instead of <see cref="CompletionOptions.AllCommitCharacters"/>.
        /// If absent, client will default to <see cref="CompletionOptions.AllCommitCharacters"/>.
        /// </summary>
        [JsonPropertyName(VsCommitCharactersSerializedName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<string[], VSInternalCommitCharacter[]>? VsCommitCharacters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the client should call <see cref="Methods.TextDocumentCompletionResolve"/> to
        /// get the value of the text edit to commit.
        /// </summary>
        [JsonPropertyName(VsResolveTextEditOnCommitName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool VsResolveTextEditOnCommit { get; set; }
    }
}
