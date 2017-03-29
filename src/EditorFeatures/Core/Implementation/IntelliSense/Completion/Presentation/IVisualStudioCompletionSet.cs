using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    /// <summary>
    /// See comment on VisualStudio15CompletionSet for an explanation of how these types 
    /// fit together and where code should go in them.
    /// 
    /// This type exists so we can expose normally protected properties on <see cref="CompletionSet"/> 
    /// to types like <see cref="Roslyn14CompletionSet"/> so they can read/write them.  This allows
    /// us to encapulate logic in our own inheritance hierarchy without having to fit into the
    /// editor's inheritance hierarchy.
    /// </summary>
    internal interface IVisualStudioCompletionSet : ICompletionSet
    {
        string DisplayName { get; set; }
        string Moniker { get; set; }

        ITrackingSpan ApplicableTo { get; set; }

        BulkObservableCollection<VSCompletion> WritableCompletionBuilders { get; }
        BulkObservableCollection<VSCompletion> WritableCompletions { get; }
        CompletionSelectionStatus SelectionStatus { get; set; }
    }
}