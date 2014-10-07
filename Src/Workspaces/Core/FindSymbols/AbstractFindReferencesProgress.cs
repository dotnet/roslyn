using System.Collections.Generic;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.FindReferences
{
    /// <summary>
    /// A class that reports the current progress made when finding references to symbols.
    /// </summary>
    public class AbstractFindReferencesProgress : IFindReferencesProgress
    {
        protected virtual void OnStartFindInSolution(ISymbol symbol, ISolution solution)
        {
        }

        protected virtual void OnCompleteFindInSolution(ISymbol symbol, ISolution solution)
        {
        }

        protected virtual void OnStartFindInProject(ISymbol symbol, IProject project)
        {
        }

        protected virtual void OnCompleteFindInProject(ISymbol symbol, IProject project)
        {
        }

        protected virtual void OnStartFindInDocument(ISymbol symbol, IDocument document)
        {
        }

        protected virtual void OnCompleteFindInDocument(ISymbol symbol, IDocument document)
        {
        }

        protected virtual void OnFoundReference(ISymbol symbol, IDocument document, ILocation location)
        {
        }

        protected virtual void OnCompleted()
        {
        }

        void IFindReferencesProgress.OnStartFindInSolution(ISymbol symbol, ISolution solution)
        {
            this.OnStartFindInSolution(symbol, solution);
        }

        void IFindReferencesProgress.OnCompleteFindInSolution(ISymbol symbol, ISolution solution)
        {
            this.OnCompleteFindInSolution(symbol, solution);
        }

        void IFindReferencesProgress.OnStartFindInProject(ISymbol symbol, IProject project)
        {
            this.OnStartFindInProject(symbol, project);
        }

        void IFindReferencesProgress.OnCompleteFindInProject(ISymbol symbol, IProject project)
        {
            this.OnCompleteFindInProject(symbol, project);
        }

        void IFindReferencesProgress.OnStartFindInDocument(ISymbol symbol, IDocument document)
        {
            this.OnStartFindInDocument(symbol, document);
        }

        void IFindReferencesProgress.OnCompleteFindInDocument(ISymbol symbol, IDocument document)
        {
            this.OnCompleteFindInDocument(symbol, document);
        }

        void IFindReferencesProgress.OnFoundReference(ISymbol symbol, IDocument document, ILocation location)
        {
            this.OnFoundReference(symbol, document, location);
        }

        void IFindReferencesProgress.OnCompleted()
        {
            this.OnCompleted();
        }
    }
}