using System;
 using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.PullMemberUp.Dialog
{
    internal interface IPullMemberUpWithDialogService : ILanguageService
    {
        Task<Solution> ComputeInterfaceRefactoring(PullMemberDialogResult result, Document contextDocument, CancellationToken cancellationToken);

        Task<Solution> ComputeClassRefactoring(PullMemberDialogResult result, Document contextDocument, CancellationToken cancellationToken);
    }
}
