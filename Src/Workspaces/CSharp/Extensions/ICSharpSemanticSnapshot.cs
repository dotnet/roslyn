using System.Threading;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;

namespace Roslyn.Services.Editor.CSharp
{
    internal interface ICSharpSemanticSnapshot
    {
        IDocument Document { get; }
        SemanticModel GetSemanticModel(CancellationToken cancellationToken = default(CancellationToken));
    }

    internal static class ICSharpSemanticSnapshotExtensions
    {
        public static SyntaxTree GetSyntaxTree(this ICSharpSemanticSnapshot snapshot, CancellationToken cancellationToken)
        {
            return (SyntaxTree)snapshot.Document.GetSyntaxTree(cancellationToken);
        }

        public static Compilation GetCompilation(this ICSharpSemanticSnapshot snapshot, CancellationToken cancellationToken)
        {
            return (Compilation)snapshot.Document.Project.GetCompilation(cancellationToken);
        }

        public static ISolution GetSolution(this ICSharpSemanticSnapshot snapshot)
        {
            return snapshot.Document.Project.Solution;
        }

        public static IProject GetProject(this ICSharpSemanticSnapshot snapshot)
        {
            return snapshot.Document.Project;
        }

        public static TService GetService<TService>(this ICSharpSemanticSnapshot snapshot) where TService : ILanguageService
        {
            return snapshot.Document.LanguageServices.GetService<TService>();
        }
    }
}