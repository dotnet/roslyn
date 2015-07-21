using System;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// What the async tagger infrastructure should do in the presence of text edits.
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
        /// attached to.  The text changes will be provided to the <see cref="AsynchronousTaggerContext{TTag, TState}"/>
        /// that is passed to <see cref="IAsynchronousTaggerDataSource{TTag, TState}.ProduceTagsAsync"/>.
        /// </summary>
        TrackTextChanges = 1 << 0,

        /// <summary>
        /// The async tagger infrastructure will not track text changes to the subject buffer it is 
        /// attached to.  The text changes will be provided to the <see cref="AsynchronousTaggerContext{TTag, TState}"/>
        /// that is passed to <see cref="IAsynchronousTaggerDataSource{TTag, TState}.ProduceTagsAsync"/>.
        /// 
        /// Tags that intersect the text change range will immediately removed.
        /// </summary>
        RemoveTagsThatIntersectEdits = TrackTextChanges | (1 << 1)
    }
}
