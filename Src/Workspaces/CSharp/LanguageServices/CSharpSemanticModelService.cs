using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Editor.CSharp.Extensions;
using Roslyn.Services.Shared.LanguageServices;

namespace Roslyn.Services.Editor.CSharp.LanguageServices
{
    [ExportLanguageService(typeof(ISemanticModelService), LanguageNames.CSharp)]
    internal class CSharpSemanticModelService : ISemanticModelService
    {
        public bool TryGetDefinition(ISemanticModel semanticModel, CommonSyntaxToken token, CancellationToken cancellationToken, out ISymbol definition)
        {
            Symbol symbol;
            if (((SemanticModel)semanticModel).TryGetDefinition((SyntaxToken)token, cancellationToken, out symbol))
            {
                definition = symbol;
                return true;
            }
            else
            {
                definition = default(ISymbol);
                return false;
            }
        }
    }
}