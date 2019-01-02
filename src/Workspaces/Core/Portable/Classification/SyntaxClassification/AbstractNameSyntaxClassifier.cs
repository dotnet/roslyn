using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification.Classifiers
{
    internal abstract class AbstractNameSyntaxClassifier : AbstractSyntaxClassifier
    {
        protected abstract int? GetRightmostNameArity(SyntaxNode node);
        protected abstract bool IsParentAnAttribute(SyntaxNode node);

        protected ISymbol TryGetSymbol(SyntaxNode node, SymbolInfo symbolInfo, SemanticModel semanticModel)
        {
            var symbol = symbolInfo.Symbol;

            if (symbol is null && symbolInfo.CandidateSymbols.Length > 0)
            {
                var firstSymbol = symbolInfo.CandidateSymbols[0];

                switch (symbolInfo.CandidateReason)
                {
                    case CandidateReason.NotAValue:
                        return firstSymbol;

                    case CandidateReason.NotCreatable:
                        // We want to color types even if they can't be constructed.
                        if (firstSymbol.IsConstructor() || firstSymbol is ITypeSymbol)
                        {
                            symbol = firstSymbol;
                        }

                        break;

                    case CandidateReason.OverloadResolutionFailure:
                        // If we couldn't bind to a constructor, still classify the type.
                        if (firstSymbol.IsConstructor())
                        {
                            symbol = firstSymbol;
                        }

                        break;

                    case CandidateReason.Inaccessible:
                        // If a constructor wasn't accessible, still classify the type if it's accessible.
                        if (firstSymbol.IsConstructor() && semanticModel.IsAccessible(node.SpanStart, firstSymbol.ContainingType))
                        {
                            symbol = firstSymbol;
                        }

                        break;

                    case CandidateReason.WrongArity:
                        var arity = GetRightmostNameArity(node);

                        if (arity.HasValue && arity.Value == 0)
                        {
                            // When the user writes something like "IList" we don't want to *not* classify 
                            // just because the type bound to "IList<T>".  This is also important for use
                            // cases like "Add-using" where it can be confusing when the using is added for
                            // "using System.Collection.Generic" but then the type name still does not classify.
                            symbol = firstSymbol;
                        }

                        break;
                }
            }

            // Classify a reference to an attribute constructor in an attribute location
            // as if we were classifying the attribute type itself.
            if (symbol.IsConstructor() && IsParentAnAttribute(node))
            {
                symbol = symbol.ContainingType;
            }

            return symbol;
        }

        protected void TryClassifyStaticSymbol(
            ISymbol symbol,
            TextSpan span,
            ArrayBuilder<ClassifiedSpan> result)
        {
            if (symbol is null || !symbol.IsStatic)
            {
                return;
            }

            if (symbol.IsEnumMember())
            {
                // EnumMembers are not classified as static since there is no
                // instance equivalent of the concept and they have their own
                // classification type.
                return;
            }

            if (symbol.IsNamespace())
            {
                // Namespace names are not classified as static since there is no
                // instance equivalent of the concept and they have their own
                // classification type.
                return;
            }

            if (symbol.IsLocalFunction())
            {
                // Local function names are not classified as static since the
                // the symbol returning true for IsStatic is an implementation detail.
                return;
            }

            result.Add(new ClassifiedSpan(span, ClassificationTypeNames.StaticSymbol));
        }
    }
}
