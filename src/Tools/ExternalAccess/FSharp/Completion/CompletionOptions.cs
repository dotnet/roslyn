using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    public static class CompletionOptions
    {
        public static readonly PerLanguageOption<bool> BlockForCompletionItems = Microsoft.CodeAnalysis.Completion.CompletionOptions.BlockForCompletionItems
    }
}
