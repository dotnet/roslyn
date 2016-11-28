using System.Threading;
using Microsoft.CodeAnalysis.Editor.Navigation;

namespace Microsoft.CodeAnalysis.Editor
{
    internal abstract class FindReferencesContext
    {
        public virtual CancellationToken CancellationToken { get; }

        protected FindReferencesContext()
        {
        }

        public virtual void OnStarted()
        {
        }

        public virtual void OnCompleted()
        {
        }

        public virtual void OnFindInDocumentStarted(Document document)
        {
        }

        public virtual void OnFindInDocumentCompleted(Document document)
        {
        }

        public virtual void OnDefinitionFound(INavigableItem definition)
        {
        }

        public virtual void OnReferenceFound(INavigableItem definition, INavigableItem reference)
        {
        }

        public virtual void ReportProgress(int current, int maximum)
        {
        }
    }
}
