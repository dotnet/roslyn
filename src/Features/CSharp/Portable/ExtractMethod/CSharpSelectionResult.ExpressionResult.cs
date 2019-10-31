// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable 

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpSelectionResult
    {
        private class ExpressionResult : CSharpSelectionResult
        {
            public ExpressionResult(
                OperationStatus status,
                TextSpan originalSpan,
                TextSpan finalSpan,
                OptionSet options,
                bool selectionInExpression,
                SemanticDocument document,
                SyntaxAnnotation firstTokenAnnotation,
                SyntaxAnnotation lastTokenAnnotation)
                : base(status, originalSpan, finalSpan, options, selectionInExpression, document, firstTokenAnnotation, lastTokenAnnotation)
            {
            }

            public override bool ContainingScopeHasAsyncKeyword()
            {
                return false;
            }

            public override SyntaxNode? GetContainingScope()
            {
                Contract.ThrowIfNull(this.SemanticDocument);
                Contract.ThrowIfFalse(this.SelectionInExpression);

                var firstToken = this.GetFirstTokenInSelection();
                var lastToken = this.GetLastTokenInSelection();
                return firstToken.GetCommonRoot(lastToken).GetAncestorOrThis<ExpressionSyntax>();
            }

            public override ITypeSymbol GetContainingScopeType()
            {
                var node = this.GetContainingScope();
                var model = this.SemanticDocument.SemanticModel;

                if (!node.IsExpression())
                {
                    Contract.Fail("this shouldn't happen");
                }

                // special case for array initializer and explicit cast
                if (node.IsArrayInitializer())
                {
                    var variableDeclExpression = node.GetAncestorOrThis<VariableDeclarationSyntax>();
                    if (variableDeclExpression != null)
                    {
                        return model.GetTypeInfo(variableDeclExpression.Type).GetTypeWithAnnotatedNullability();
                    }
                }

                if (node.IsExpressionInCast())
                {
                    // bug # 12774 and # 4780
                    // if the expression is under cast, we use the heuristic below
                    // 1. if regular binding returns a meaningful type, we use it as it is
                    // 2. if it doesn't, even if the cast itself wasn't included in the selection, we will treat it 
                    //    as it was in the selection
                    var regularType = GetRegularExpressionType(model, node);
                    if (regularType != null && !regularType.IsObjectType())
                    {
                        return regularType;
                    }

                    if (node.Parent is CastExpressionSyntax castExpression)
                    {
                        return model.GetTypeInfo(castExpression.Type).GetTypeWithAnnotatedNullability();
                    }
                }

                return GetRegularExpressionType(model, node);
            }

            private static ITypeSymbol GetRegularExpressionType(SemanticModel semanticModel, SyntaxNode node)
            {
                // regular case. always use ConvertedType to get implicit conversion right.
                var expression = node.GetUnparenthesizedExpression();

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
                    info.ConvertedType?.IsFormattableString() == true)
                {
                    return info.GetConvertedTypeWithAnnotatedNullability();
                }

                // always try to use type that is more specific than object type if possible.
                return !info.Type.IsObjectType() ? info.GetTypeWithAnnotatedNullability() : info.GetConvertedTypeWithAnnotatedNullability();
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
            return info.ConvertedType.GetAttributes().Any(c => c.AttributeClass.Equals(coclassSymbol));
        }
    }
}
