﻿using System;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Flags that affect how the tagger infrastructure responds to text changes.
    /// </summary>
    [Flags]
    internal enum TaggerTextChangeBehavior
    {
        /// <summary>
        /// The async tagger infrastructure will not track any text changes and will not do 
        /// anything special in the presence of them.
        /// </summary>
        None = 0,

        /// <summary>
        /// The async tagger infrastructure will track text changes to the subject buffer it is 
        /// attached to.  The text changes will be provided to the <see cref="AsynchronousTaggerContext{TTag}"/>
        /// that is passed to <see cref="IAsynchronousTaggerDataSource{TTag}.ProduceTagsAsync"/>.
        /// </summary>
        TrackTextChanges = 1 << 0,

        /// <summary>
        /// The async tagger infrastructure will track text changes to the subject buffer it is 
        /// attached to.  The text changes will be provided to the <see cref="AsynchronousTaggerContext{TTag}"/>
        /// that is passed to <see cref="IAsynchronousTaggerDataSource{TTag}.ProduceTagsAsync"/>.
        /// 
        /// On any edit, tags that intersect the text change range will immediately removed.
        /// </summary>
        RemoveTagsThatIntersectEdits = TrackTextChanges | (1 << 1),

        /// <summary>
        /// The async tagger infrastructure will track text changes to the subject buffer it is 
        /// attached to.
        /// 
        /// On any edit all tags will we be removed.
        /// </summary>
        RemoveAllTags = TrackTextChanges | (1 << 2),
    }
}
