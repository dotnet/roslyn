using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal static class OptionSetExtensions
    {
        public static OptionSet WithDebuggerCompletionOptions(this OptionSet options)
        {
            return options
                .WithChangedOption(CompletionOptions.AlwaysShowBuilder, true)
                .WithChangedOption(CompletionOptions.FilterOutOfScopeLocals, false)
                .WithChangedOption(CompletionOptions.ShowXmlDocCommentCompletion, false);
        }
    }
}
