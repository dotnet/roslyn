// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using OptionSet = Microsoft.CodeAnalysis.Options.OptionSet;
#endif

namespace Microsoft.CodeAnalysis.Simplification
{
    internal static class SimplificationHelpers
    {
        public static readonly SyntaxAnnotation DontSimplifyAnnotation = new SyntaxAnnotation();
        public static readonly SyntaxAnnotation SimplifyModuleNameAnnotation = new SyntaxAnnotation();

        public static TNode CopyAnnotations<TNode>(SyntaxNode from, TNode to) where TNode : SyntaxNode
        {
            // Because we are removing a node that may have annotations (i.e. formatting), we need
            // to copy those annotations to the new node. However, we can only copy all annotations
            // which will mean that the new node will include a ParenthesesSimplification annotation,
            // even if didn't have one before. That results in potentially removing parentheses that
            // weren't annotated by the user. To address this, we add *another* annotation to indicate
            // that the new node shouldn't be simplified. This is to work around the
            // fact that there is no way to remove an annotation from a node in the current API. If
            // that gets added, we can clean this up.

            var dontSimplifyResult = !to.HasAnnotation(Simplifier.Annotation);

            to = from.CopyAnnotationsTo(to);

            if (dontSimplifyResult)
            {
                to = to.WithAdditionalAnnotations(DontSimplifyAnnotation);
            }

            return to;
        }

        public static SyntaxToken CopyAnnotations(SyntaxToken from, SyntaxToken to)
        {
            // Because we are removing a node that may have annotations (i.e. formatting), we need
            // to copy those annotations to the new node. However, we can only copy all annotations
            // which will mean that the new node will include a ParenthesesSimplification annotation,
            // even if didn't have one before. That results in potentially removing parentheses that
            // weren't annotated by the user. To address this, we add *another* annotation to indicate
            // that the new node shouldn't be simplified. This is to work around the
            // fact that there is no way to remove an annotation from a node in the current API. If
            // that gets added, we can clean this up.

            var dontSimplifyResult = !to.HasAnnotation(Simplifier.Annotation);

            to = from.CopyAnnotationsTo(to);

            if (dontSimplifyResult)
            {
                to = to.WithAdditionalAnnotations(DontSimplifyAnnotation);
            }

            return to;
        }

        internal static ISymbol GetOriginalSymbolInfo(SemanticModel semanticModel, SyntaxNode expression)
        {
            Contract.ThrowIfNull(expression);
            var annotation1 = expression.GetAnnotations(SymbolAnnotation.Kind).FirstOrDefault();
            if (annotation1 != null)
            {
                var typeSymbol = SymbolAnnotation.GetSymbol(annotation1, semanticModel.Compilation);
                if (IsValidSymbolInfo(typeSymbol))
                {
                    return typeSymbol;
                }
            }

            var annotation2 = expression.GetAnnotations(SpecialTypeAnnotation.Kind).FirstOrDefault();
            if (annotation2 != null)
            {
                var specialType = SpecialTypeAnnotation.GetSpecialType(annotation2);
                if (specialType != SpecialType.None)
                {
                    var typeSymbol = semanticModel.Compilation.GetSpecialType(specialType);
                    if (IsValidSymbolInfo(typeSymbol))
                    {
                        return typeSymbol;
                    }
                }
            }

            var symbolInfo = semanticModel.GetSymbolInfo(expression);
            if (!IsValidSymbolInfo(symbolInfo.Symbol))
            {
                return null;
            }

            return symbolInfo.Symbol;
        }

        internal static bool IsValidSymbolInfo(ISymbol symbol)
        {
            // name bound to only one symbol is valid
            return symbol != null && !symbol.IsErrorType();
        }

        internal static bool ShouldSimplifyThisOrMeMemberAccessExpression(
            SemanticModel semanticModel, OptionSet optionSet, ISymbol symbol)
        {
            // If we're accessing a static member off of this/me then we should always consider this
            // simplifiable.  Note: in C# this isn't even legal to access a static off of `this`,
            // but in VB it is legal to access a static off of `me`.
            if (symbol.IsStatic)
                return true;

            if ((symbol.IsKind(SymbolKind.Field) && optionSet.GetOption(CodeStyleOptions2.QualifyFieldAccess, semanticModel.Language).Value ||
                (symbol.IsKind(SymbolKind.Property) && optionSet.GetOption(CodeStyleOptions2.QualifyPropertyAccess, semanticModel.Language).Value) ||
                (symbol.IsKind(SymbolKind.Method) && optionSet.GetOption(CodeStyleOptions2.QualifyMethodAccess, semanticModel.Language).Value) ||
                (symbol.IsKind(SymbolKind.Event) && optionSet.GetOption(CodeStyleOptions2.QualifyEventAccess, semanticModel.Language).Value)))
            {
                return false;
            }

            return true;
        }

        internal static bool PreferPredefinedTypeKeywordInDeclarations(OptionSet optionSet, string language)
            => optionSet.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, language).Value;

        internal static bool PreferPredefinedTypeKeywordInMemberAccess(OptionSet optionSet, string language)
            => optionSet.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, language).Value;
    }
}
