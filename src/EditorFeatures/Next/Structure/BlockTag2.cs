using System;

namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// An implementation of <see cref="IBlockTag2" />.
    /// </summary>
    internal abstract class BlockTag2 : IBlockTag2
    {
        public BlockTag2(SnapshotSpan span, SnapshotSpan statementSpan, IBlockTag2 parent, string type, bool isCollapsible, bool isDefaultCollapsed, bool isImplementation, object collapsedForm, object collapsedHintForm)
        {
            this.Span = span;
            this.Level = (parent == null) ? 0 : (parent.Level + 1);
            this.StatementSpan = statementSpan;
            this.Parent = parent;
            this.Type = type;
            this.IsCollapsible = isCollapsible;
            this.IsDefaultCollapsed = isDefaultCollapsed;
            this.IsImplementation = isImplementation;
            this.CollapsedForm = collapsedForm;
            this.CollapsedHintForm = collapsedHintForm;
        }

        /// <summary>
        /// Gets the span of the structural block.
        /// </summary>
        public SnapshotSpan Span { get; }

        /// <summary>
        /// Gets the level of nested-ness of the structural block.
        /// </summary>
        public int Level { get; }

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
        public SnapshotSpan StatementSpan { get; }

        /// <summary>
        /// Gets the hierarchical parent of the structural block.
        /// </summary>
        public IBlockTag2 Parent { get; }

        /// <summary>
        /// Determines the semantic type of the structural block.
        /// </summary>
        /// <remarks>
        /// The type should be set to "NonStructural" if an outlining region should be created but
        /// not adorned with vertical structure lines, else the type should be set to "Structural".
        /// Types are defined in the class PredefinedStructureTypes.
        /// </remarks>
        public string Type { get; }

        /// <summary>
        /// Determines whether a block can be collapsed.
        /// </summary>
        public bool IsCollapsible { get; }

        /// <summary>
        /// Determines whether a block is collapsed by default.
        /// </summary>
        public bool IsDefaultCollapsed { get; }

        /// <summary>
        /// Determines whether a block is an block region.
        /// </summary>
        /// <remarks>
        /// Implementation blocks are the blocks of code following a method definition.
        /// They are used for commands such as the Visual Studio Collapse to Definition command,
        /// which hides the implementation block and leaves only the method definition exposed.
        /// </remarks>
        public bool IsImplementation { get; }

        /// <summary>
        /// Gets the data object for the collapsed UI. If the default is set, returns null.
        /// </summary>
        public object CollapsedForm { get; }

        /// <summary>
        /// Gets the data object for the collapsed UI tooltip. If the default is set, returns null.
        /// </summary>
        public object CollapsedHintForm { get; }
    }
}