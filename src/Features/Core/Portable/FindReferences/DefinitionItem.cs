// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.FindReferences
{
    /// <summary>
    /// Information about a symbol's definition that can be displayed in an editor
    /// and used for navigation.
    /// </summary>
    internal sealed class DefinitionItem
    {
        /// <summary>
        /// Descriptive tags from <see cref="CompletionTags"/>. These tags may influence how the 
        /// item is displayed.
        /// </summary>
        public ImmutableArray<string> Tags { get; }

        /// <summary>
        /// The display text that should be displayed to the user.
        /// </summary>
        public ImmutableArray<TaggedText> DisplayParts { get; }

        /// <summary>
        /// The locations to present in the UI.  A definition may have multiple locations for cases
        /// like partial types/members.
        /// </summary>
        public ImmutableArray<DefinitionLocation> Locations { get; }

        /// <summary>
        /// Whether or not this definition should be presented if we never found any references to
        /// it.  For example, when searching for a property, the FindReferences engine will cascade
        /// to the accessors in case any code specifically called those accessors (can happen in 
        /// cross-language cases).  However, in the normal case where there were no calls specifically
        /// to the accessor, we would not want to display them in the UI.  
        /// 
        /// For most definitions we will want to display them, even if no references were found.  
        /// This property allows for this customization in behavior.
        /// </summary>
        public bool DisplayIfNoReferences { get; }

        public DefinitionItem(
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> displayParts,
            ImmutableArray<DefinitionLocation> locations,
            bool displayIfNoReferences)
        {
            Tags = tags;
            DisplayParts = displayParts;
            Locations = locations;
            DisplayIfNoReferences = displayIfNoReferences;
        }
    }
}