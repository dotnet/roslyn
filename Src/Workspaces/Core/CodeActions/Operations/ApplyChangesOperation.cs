using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// A <see cref="CodeActionOperation"/> for applying solution changes to a workspace.
    /// </summary>
    public sealed class ApplyChangesOperation : CodeActionOperation
    {
        private readonly Solution changedSolution;

        public ApplyChangesOperation(Solution changedSolution)
        {
            if (changedSolution == null)
            {
                throw new ArgumentNullException("changedSolution");
            }

            this.changedSolution = changedSolution;
        }

        public Solution ChangedSolution
        {
            get { return this.changedSolution; }
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            workspace.TryApplyChanges(this.changedSolution);
        }
    }
}