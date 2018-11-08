using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.PullMemberUp
{
    internal interface IPullMemberUpActionAndSolutionGenerator : ILanguageService 
    {
        /// <summary>
        /// Generate the changed solution base on the <param name="result"></param>.
        /// It will provide fix if the modifier of target and members mismatch.
        /// </summary>
        /// <returns></returns>
        Task<Solution> GetSolutionAsync(AnalysisResult result, Document contextDocument, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate the CodeAction base on <param name="result"></param>.
        /// It won't provide fix if the modifier of target and members mismatch.
        /// </summary>
        /// <returns></returns>
        CodeAction GetCodeActionAsync(AnalysisResult result, Document contextDocument, string title);
    }
}
