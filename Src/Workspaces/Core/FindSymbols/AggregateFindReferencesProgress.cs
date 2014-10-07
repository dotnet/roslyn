using System.Collections.Generic;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.FindReferences
{
    internal class AggregateFindReferencesProgress : IFindReferencesProgress
    {
        private readonly IEnumerable<IFindReferencesProgress> progresses;

        public AggregateFindReferencesProgress(IEnumerable<IFindReferencesProgress> progresses)
        {
            this.progresses = progresses;
        }

        public virtual void OnCompleted()
        {
            this.progresses.Do(p => p.OnCompleted());
        }

        public virtual void OnFindInDocumentStarted(ISymbol symbol, IDocument document)
        {
            this.progresses.Do(p => p.OnFindInDocumentStarted(symbol, document));
        }

        public virtual void OnFindInDocumentCompleted(ISymbol symbol, IDocument document)
        {
            this.progresses.Do(p => p.OnFindInDocumentCompleted(symbol, document));
        }

        public virtual void OnReferenceFound(ISymbol symbol, IDocument document, CommonLocation location)
        {
            this.progresses.Do(p => p.OnReferenceFound(symbol, document, location));
        }

        public void OnStarted()
        {
            this.progresses.Do(p => p.OnStarted());
        }
    }
}
