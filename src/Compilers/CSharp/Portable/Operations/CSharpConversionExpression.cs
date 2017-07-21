// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class BaseCSharpConversionExpression : BaseConversionExpression<Conversion>
    {
        protected BaseCSharpConversionExpression(Conversion conversion, bool isExplicitInCode, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(conversion, isExplicitInCode, syntax, type, constantValue)
        {
        }

        public override string LanguageName => LanguageNames.CSharp;
        // We override this here so that we don't return the method symbol if the internal conversion is a MethodGroup conversion, instead of
        // a user defined conversion. Operator method should only be returning a MethodSymbol if IsUserDefined is true.
        public override IMethodSymbol OperatorMethod => ConversionInternal.IsUserDefined ? ConversionInternal.MethodSymbol : null;
    }

    internal sealed partial class CSharpConversionExpression : BaseCSharpConversionExpression
    {
        public CSharpConversionExpression(IOperation operand, Conversion conversion, bool isExplicit, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(conversion, isExplicit, syntax, type, constantValue)
        {
            Operand = operand;
        }

        public override IOperation Operand { get; }
    }

    internal sealed partial class LazyCSharpConversionExpression : BaseCSharpConversionExpression
    {
        private readonly Lazy<IOperation> _operand;
        public LazyCSharpConversionExpression(Lazy<IOperation> operand, Conversion conversion, bool isExplicit, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(conversion, isExplicit, syntax, type, constantValue)
        {
            _operand = operand;
        }

        public override IOperation Operand => _operand.Value;
    }

    public static class IConversionExpressionExtensions
    {
        public static Conversion GetCSharpConversion(this IConversionExpression conversionExpression)
        {
            if (conversionExpression is BaseCSharpConversionExpression csharpConversionExpression)
            {
                return csharpConversionExpression.ConversionInternal;
            }
            else
            {
                throw new InvalidCastException(string.Format(CSharpResources.IConversionExpression_Is_Not_A_Valid_CSharp_Conversion, conversionExpression));
            }
        }
    }
}
