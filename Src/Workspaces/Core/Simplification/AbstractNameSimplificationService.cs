using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Utilities;

namespace Roslyn.Services.Simplification
{
    internal abstract partial class AbstractNameSimplificationService
    {
        protected static readonly SymbolDisplayFormat TypeNameFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeMethodKind |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                localOptions: SymbolDisplayLocalOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        protected static readonly SymbolDisplayFormat TypeNameWithoutAttributeSuffixFormat =
            TypeNameFormat.WithMiscellaneousOptions(TypeNameFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix);

        public abstract SimplificationResult SimplifyNames(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken);

        protected static ReadOnlyArray<SymbolDisplayPart> GetNonWhitespaceParts(
            ReadOnlyArray<SymbolDisplayPart> parts)
        {
            return parts.Where(p => p.Kind != SymbolDisplayPartKind.Space).AsReadOnly();
        }

        protected static bool IsValidSymbolInfo(ISymbol symbol)
        {
            // name bound to only one symbol is valid
            return symbol != null && !symbol.IsErrorType();
        }

        protected static ISymbol GetOriginalSymbolInfo(ISemanticModel semanticModel, CommonSyntaxNode expression)
        {
            Contract.ThrowIfNull(expression);
            var annotation1 = expression.GetAnnotations<SymbolIdAnnotation>().FirstOrDefault();
            if (annotation1 != null && annotation1.SymbolId != null)
            {
                var id = annotation1.SymbolId;
                var typeSymbol = id.Resolve(semanticModel.Compilation).Symbol;
                if (IsValidSymbolInfo(typeSymbol))
                {
                    return typeSymbol;
                }
            }

            var annotation2 = expression.GetAnnotations<SpecialTypeAnnotation>().FirstOrDefault();
            if (annotation2 != null && annotation2.SpecialType != SpecialType.None)
            {
                var typeSymbol = semanticModel.Compilation.GetSpecialType(annotation2.SpecialType);
                if (IsValidSymbolInfo(typeSymbol))
                {
                    return typeSymbol;
                }
            }

            var symbolInfo = semanticModel.GetSymbolInfo(expression);
            if (!IsValidSymbolInfo(symbolInfo.Symbol))
            {
                return null;
            }

            return symbolInfo.Symbol;
        }
    }
}