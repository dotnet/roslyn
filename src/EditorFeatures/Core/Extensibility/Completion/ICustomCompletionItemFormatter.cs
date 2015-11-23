using System.Windows.Media.TextFormatting;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Implemented by a <see cref="CompletionListProvider"/> that wants to 
    /// customize the presentation of <see cref="CompletionItem"/>s in the
    /// editor
    /// </summary>
    internal interface ICustomCompletionItemFormatter
    {
        /// /// <summary>
        /// Gets a set of <see cref="TextRunProperties"/> that will override the "default" <see cref="TextRunProperties"/> used to
        /// display this <see cref="CompletionItem"/>'s text.
        /// </summary>
        /// <param name="completionItem">The item to theme</param>
        /// <param name="defaultTextRunProperties">
        /// The set of <see cref="TextRunProperties"/> that would have been used to present this object had no overriding taken
        /// place.
        /// </param>
        /// <returns>A set of <see cref="TextRunProperties"/> that should be used to display this object's text.</returns>
        TextRunProperties GetTextRunProperties(CompletionItem completionItem, TextRunProperties defaultTextRunProperties);

        /// <summary>
        /// Gets a set of <see cref="TextRunProperties"/> that will override the "default" <see cref="TextRunProperties"/> used to
        /// display this object's text when this object is highlighted.
        /// </summary>
        /// /// <param name="completionItem">The item to theme</param>
        /// <param name="defaultHighlightedTextRunProperties">The set of <see cref="TextRunProperties"/> that would have been used to present the highlighted object had no
        /// overriding taken place.</param>
        /// <returns>A set of <see cref="TextRunProperties"/> that should be used to display this object's highlighted text.</returns>
        /// <remarks>An completion item is highlighted in the default statement completion presenter when it is fully-selected.  The
        /// <see cref="TextRunProperties"/> selected to render the highlighted text should be chosen so as to not clash with the
        /// style of the selection rectangle.
        /// </remarks>

        TextRunProperties GetHighlightedTextRunProperties(CompletionItem completionItem, TextRunProperties defaultHighlightedTextRunProperties);
    }
}
