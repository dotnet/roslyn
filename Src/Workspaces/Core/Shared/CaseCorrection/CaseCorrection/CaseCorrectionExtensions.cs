using System.Collections.Generic;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.CaseCorrection
{
    public static class CaseCorrectionExtensions
    {
        private static ICaseCorrectionService GetCaseCorrectionService(string language)
        {
            var factory = WorkspaceComposition.Composition.GetExportedValue<ILanguageServiceProviderFactory>();
            var languageService = factory.CreateLanguageServiceProvider(language);
            return languageService.GetService<ICaseCorrectionService>();
        }

        public static ICaseCorrectionResult CaseCorrect(this ISemanticModel semanticModel, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetCaseCorrectionService(semanticModel.Language).CaseCorrect(semanticModel, cancellationToken);
        }

        public static ICaseCorrectionResult CaseCorrect(this ISemanticModel semanticModel, SyntaxAnnotation annotation, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetCaseCorrectionService(semanticModel.Language).CaseCorrect(semanticModel, annotation, cancellationToken);
        }

        public static ICaseCorrectionResult CaseCorrect(this ISemanticModel semanticModel, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetCaseCorrectionService(semanticModel.Language).CaseCorrect(semanticModel, span, cancellationToken);
        }

        public static ICaseCorrectionResult CaseCorrect(this ISemanticModel semanticModel, IEnumerable<TextSpan> spans, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetCaseCorrectionService(semanticModel.Language).CaseCorrect(semanticModel, spans, cancellationToken);
        }
    }
}
