namespace Microsoft.CodeAnalysis.Completion
{
    public enum SnippetsRule
    {
        /// <summary>
        /// Snippet triggering follows the default rules of the language.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Snippets are never included in the completion list
        /// </summary>
        NeverInclude = 1,

        /// <summary>
        /// Snippets are always included in the completion list.
        /// </summary>
        AlwaysInclude = 2,

        /// <summary>
        /// Snippets are included if the user types: id?&lt;tab&gt;
        /// </summary>
        IncludeAfterTypingIdentifierQuestionTab = 3,
    }
}
