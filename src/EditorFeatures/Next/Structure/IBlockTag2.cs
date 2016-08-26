// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Text.Tagging
{
    using Microsoft.VisualStudio.Text.Adornments;

    /// <summary>
    /// Represents a structural code block, which is used for vertical structural line adornments.
    /// </summary>
    internal interface IBlockTag2 : ITag
    {
        /// <summary>
        /// Gets the span of the structural block.
        /// </summary>
        SnapshotSpan Span { get; }

        /// <summary>
        /// Gets the level of nested-ness of the structural block.
        /// </summary>
        int Level { get; }

        /// <summary>
        /// Gets the span of the statement that control the structral block.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For example, in the following snippet of code,
        /// <code>
        /// if (condition1 &amp;&amp;
        ///     condition2) // comment
        /// {
        ///     something;
        /// }
        /// </code>
        /// this.StatementSpan would extend from the start of the "if" to the end of comment.
        /// this.Span would extend from before the "{" to the end of the "}".
        /// </para>
        /// </remarks>
        SnapshotSpan StatementSpan { get; }

        /// <summary>
        /// Gets the hierarchical parent of the structural block.
        /// </summary>
        IBlockTag2 Parent { get; }

        /// <summary>
        /// Determines the semantic type of the structural block.
        /// <remarks>
        /// See <see cref="PredefinedStructureTypes2"/> for the canonical types.
        /// Use <see cref="PredefinedStructureTypes2.NonStructural"/> for blocks that will not have any visible affordance
        /// (but will be used for outlining).
        /// </remarks>
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Determines whether a block can be collapsed.
        /// </summary>
        bool IsCollapsible { get; }

        /// <summary>
        /// Determines whether a block is collapsed by default.
        /// </summary>
        bool IsDefaultCollapsed { get; }

        /// <summary>
        /// Determines whether a block is an implementation block.
        /// </summary>
        /// <remarks>
        /// Implementation blocks are the blocks of code following a method definition.
        /// They are used for commands such as the Visual Studio Collapse to Definition command,
        /// which hides the implementation block and leaves only the method definition exposed.
        /// </remarks>
        bool IsImplementation { get; }

        /// <summary>
        /// Gets the data object for the collapsed UI. If the default is set, returns null.
        /// </summary>
        object CollapsedForm { get; }

        /// <summary>
        /// Gets the data object for the collapsed UI tooltip. If the default is set, returns null.
        /// </summary>
        object CollapsedHintForm { get; }
    }
}