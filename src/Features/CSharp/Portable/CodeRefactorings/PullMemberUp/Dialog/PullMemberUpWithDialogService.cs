using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PullMemberUp.Dialog;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.Dialog
{
    [ExportLanguageService(typeof(IPullMemberUpWithDialogService), LanguageNames.CSharp), Shared]
    internal class PullMemberUpWithDialogService : IPullMemberUpWithDialogService
    {
        public Task<Solution> ComputeClassRefactoring(PullMemberDialogResult result, Document contextDocument, CancellationToken cancellationToken)
        {
            var classPuller = new ClassPullerWithDialog();
            return classPuller.ComputeChangedSolution(result, contextDocument, cancellationToken);
        }

        public Task<Solution> ComputeInterfaceRefactoring(PullMemberDialogResult result, Document contextDocument, CancellationToken cancellationToken)
        {
            var interfacePuller = new InterfacePullerWithDialog();
            return interfacePuller.ComputeChangedSolution(result, contextDocument, cancellationToken);
        }
    }
}
