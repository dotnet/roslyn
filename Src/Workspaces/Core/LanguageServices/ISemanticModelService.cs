using System.Threading;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.LanguageServices
{
    internal interface ISemanticModelService : ILanguageService
    {
        bool TryGetDefinition(ISemanticModel semanticModel, CommonSyntaxToken token, CancellationToken cancellationToken, out ISymbol definition);
    }
}