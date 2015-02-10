namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal enum ReplSpanKind
    {
        None,

        /// <summary>
        /// The span represents output from the program (standard output)
        /// </summary>
        Output,

        /// <summary>
        /// The span represents a prompt for input of code.
        /// </summary>
        Prompt,

        /// <summary>
        /// The span represents a secondary prompt for more code.
        /// </summary>
        SecondaryPrompt,

        /// <summary>
        /// The span represents code inputted after a prompt or secondary prompt.
        /// </summary>
        Language,

        /// <summary>
        /// The span represents the prompt for input for standard input (non code input)
        /// </summary>
        StandardInputPrompt,

        /// <summary>
        /// The span represents the input for a standard input (non code input)
        /// </summary>
        StandardInput,
    }

    internal static class ReplSpanKindExtensions
    {
        internal static bool IsPrompt(this ReplSpanKind kind)
        {
            switch (kind)
            {
                case ReplSpanKind.Prompt:
                case ReplSpanKind.SecondaryPrompt:
                case ReplSpanKind.StandardInputPrompt:
                    return true;
                default:
                    return false;
            }
        }
    }
}
