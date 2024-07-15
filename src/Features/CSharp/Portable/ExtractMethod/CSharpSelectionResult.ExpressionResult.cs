// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal partial class CSharpSelectionResult
{
    private class ExpressionResult(
        TextSpan originalSpan,
        TextSpan finalSpan,
        ExtractMethodOptions options,
        bool selectionInExpression,
        SemanticDocument document,
        SyntaxAnnotation firstTokenAnnotation,
        SyntaxAnnotation lastTokenAnnotation,
        bool selectionChanged) : CSharpSelectionResult(
            originalSpan, finalSpan, options, selectionInExpression, document, firstTokenAnnotation, lastTokenAnnotation, selectionChanged)
    {
        public override bool ContainingScopeHasAsyncKeyword()
            => false;

        public override SyntaxNode? GetContainingScope()
        {
            Contract.ThrowIfNull(SemanticDocument);
            Contract.ThrowIfFalse(SelectionInExpression);

            var firstToken = GetFirstTokenInSelection();
            var lastToken = GetLastTokenInSelection();
            var scope = firstToken.GetCommonRoot(lastToken).GetAncestorOrThis<ExpressionSyntax>();
            if (scope == null)
                return null;

            return CSharpSyntaxFacts.Instance.GetRootStandaloneExpression(scope);
        }

        public override (ITypeSymbol? returnType, bool returnsByRef) GetReturnType()
        {
            if (GetContainingScope() is not ExpressionSyntax node)
            {
                throw ExceptionUtilities.Unreachable();
            }

            var model = SemanticDocument.SemanticModel;

            // special case for array initializer and explicit cast
            if (node.IsArrayInitializer())
            {
                var variableDeclExpression = node.GetAncestorOrThis<VariableDeclarationSyntax>();
                if (variableDeclExpression != null)
                    return (model.GetTypeInfo(variableDeclExpression.Type).Type, returnsByRef: false);
            }

            if (node.IsExpressionInCast())
            {
                // bug # 12774 and # 4780
                // if the expression is under cast, we use the heuristic below
                // 1. if regular binding returns a meaningful type, we use it as it is
                // 2. if it doesn't, even if the cast itself wasn't included in the selection, we will treat it 
                //    as it was in the selection
                var (regularType, returnsByRef) = GetRegularExpressionType(model, node);
                if (regularType != null)
                    return (regularType, returnsByRef);

                if (node.Parent is CastExpressionSyntax castExpression)
                    return (model.GetTypeInfo(castExpression).Type, returnsByRef: false);
            }

            return GetRegularExpressionType(model, node);
        }

        private static (ITypeSymbol? typeSymbol, bool returnsByRef) GetRegularExpressionType(SemanticModel semanticModel, ExpressionSyntax node)
        {
            // regular case. always use ConvertedType to get implicit conversion right.
            var expression = node.GetUnparenthesizedExpression();
            var returnsByRef = false;
            if (expression is RefExpressionSyntax refExpression)
            {
                expression = refExpression.Expression;
                returnsByRef = true;
            }

            var typeSymbol = GetRegularExpressionTypeWorker();
            return (typeSymbol, returnsByRef);

            ITypeSymbol? GetRegularExpressionTypeWorker()
            {
                var info = semanticModel.GetTypeInfo(expression);
                var conv = semanticModel.GetConversion(expression);

                if (info.ConvertedType == null || info.ConvertedType.IsErrorType())
                {
                    // there is no implicit conversion involved. no need to go further
                    return info.GetTypeWithAnnotatedNullability();
                }

                // always use converted type if method group
                if ((!node.IsKind(SyntaxKind.ObjectCreationExpression) && semanticModel.GetMemberGroup(expression).Length > 0) ||
                    IsCoClassImplicitConversion(info, conv, semanticModel.Compilation.CoClassType()))
                {
                    return info.GetConvertedTypeWithAnnotatedNullability();
                }

                // check implicit conversion
                if (conv.IsImplicit && (conv.IsConstantExpression || conv.IsEnumeration))
                {
                    return info.GetConvertedTypeWithAnnotatedNullability();
                }

                // use FormattableString if conversion between String and FormattableString
                if (info.Type?.SpecialType == SpecialType.System_String &&
                    info.ConvertedType?.IsFormattableStringOrIFormattable() == true)
                {
                    return info.GetConvertedTypeWithAnnotatedNullability();
                }

                // always try to use type that is more specific than object type if possible.
                return !info.Type.IsObjectType() ? info.GetTypeWithAnnotatedNullability() : info.GetConvertedTypeWithAnnotatedNullability();
            }
        }
    }

    private static bool IsCoClassImplicitConversion(TypeInfo info, Conversion conversion, ISymbol? coclassSymbol)
    {
        if (!conversion.IsImplicit ||
             info.ConvertedType == null ||
             info.ConvertedType.TypeKind != TypeKind.Interface)
        {
            return false;
        }

        // let's see whether this interface has coclass attribute
        return info.ConvertedType.GetAttributes().Any(static (c, coclassSymbol) => c.AttributeClass?.Equals(coclassSymbol) == true, coclassSymbol);
    }
}
