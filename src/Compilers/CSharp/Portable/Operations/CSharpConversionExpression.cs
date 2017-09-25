// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class BaseCSharpConversionExpression : BaseConversionExpression
    {
        protected BaseCSharpConversionExpression(Conversion conversion, bool isExplicitInCode, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isExplicitInCode, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ConversionInternal = conversion;
        }

        internal Conversion ConversionInternal { get; }

        public override CommonConversion Conversion => ConversionInternal.ToCommonConversion();
    }

    internal sealed partial class CSharpConversionExpression : BaseCSharpConversionExpression
    {
        public CSharpConversionExpression(IOperation operand, Conversion conversion, bool isExplicit, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(conversion, isExplicit, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperandImpl = operand;
        }

        public override IOperation OperandImpl { get; }
    }

    internal sealed partial class LazyCSharpConversionExpression : BaseCSharpConversionExpression
    {
        private readonly Lazy<IOperation> _operand;
        public LazyCSharpConversionExpression(Lazy<IOperation> operand, Conversion conversion, bool isExplicit, bool isTryCast, bool isChecked, SemanticModel semanticModel,SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(conversion, isExplicit, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operand = operand;
        }

        public override IOperation OperandImpl => _operand.Value;
    }
}
