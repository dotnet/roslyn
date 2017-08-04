// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class BaseCSharpConversionExpression : BaseConversionExpression
    {
        protected BaseCSharpConversionExpression(Conversion conversion, bool isExplicitInCode, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isExplicitInCode, isTryCast, isChecked, semanticModel, syntax, type, constantValue)
        {
            ConversionInternal = conversion;
        }

        internal Conversion ConversionInternal { get; }

        public override CommonConversion Conversion => ConversionInternal.ToCommonConversion();

        public override string LanguageName => LanguageNames.CSharp;
    }

    internal sealed partial class CSharpConversionExpression : BaseCSharpConversionExpression
    {
        public CSharpConversionExpression(IOperation operand, Conversion conversion, bool isExplicit, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(conversion, isExplicit, isTryCast, isChecked, semanticModel, syntax, type, constantValue)
        {
            OperandImpl = operand;
        }

        public override IOperation OperandImpl { get; }
    }

    internal sealed partial class LazyCSharpConversionExpression : BaseCSharpConversionExpression
    {
        private readonly Lazy<IOperation> _operand;
        public LazyCSharpConversionExpression(Lazy<IOperation> operand, Conversion conversion, bool isExplicit, bool isTryCast, bool isChecked, SemanticModel semanticModel,SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(conversion, isExplicit, isTryCast, isChecked, semanticModel, syntax, type, constantValue)
        {
            _operand = operand;
        }

        public override IOperation OperandImpl => _operand.Value;
    }

    public static class IConversionExpressionExtensions
    {
        /// <summary>
        /// Gets the underlying <see cref="Conversion"/> information from this <see cref="IConversionExpression"/>. This
        /// <see cref="IConversionExpression"/> must have been created from CSharp code.
        /// </summary>
        /// <param name="conversionExpression">The conversion expression to get original info from.</param>
        /// <returns>The underlying <see cref="Conversion"/>.</returns>
        /// <exception cref="InvalidCastException">If the <see cref="IConversionExpression"/> was not created from CSharp code.</exception>
        public static Conversion GetCSharpConversion(this IConversionExpression conversionExpression)
        {
            if (conversionExpression is BaseCSharpConversionExpression csharpConversionExpression)
            {
                return csharpConversionExpression.ConversionInternal;
            }
            else
            {
                throw new ArgumentException(string.Format(CSharpResources.IConversionExpressionIsNotCSharpConversion,
                                                          nameof(IConversionExpression)),
                                            nameof(conversionExpression));
            }
        }
    }
}
