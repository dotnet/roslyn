using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.FindUsages
{
    public interface IFSharpFindUsagesContext
    {
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Report a message to be displayed to the user.
        /// </summary>
        Task ReportMessageAsync(string message);

        /// <summary>
        /// Set the title of the window that results are displayed in.
        /// </summary>
        Task SetSearchTitleAsync(string title);

        Task OnDefinitionFoundAsync(DefinitionItem definition);
        Task OnReferenceFoundAsync(SourceReferenceItem reference);

        Task ReportProgressAsync(int current, int maximum);
    }
}
