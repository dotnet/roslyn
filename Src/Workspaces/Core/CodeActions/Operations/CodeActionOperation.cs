using System.Threading;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Represents a single operation of a multi-operation code action.
    /// </summary>
    public abstract class CodeActionOperation
    {
        /// <summary>
        /// A description of the effect of the operation.
        /// </summary>
        public virtual string Description
        {
            get { return null; }
        }

        /// <summary>
        /// Called by the host environment to apply the effect of the operation.
        /// This method is gauranteed to be called on the UI thread.
        /// </summary>
        public virtual void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
        }
    }
}