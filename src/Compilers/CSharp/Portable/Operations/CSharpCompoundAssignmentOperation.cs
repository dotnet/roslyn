using System;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class BaseCSharpCompoundAssignmentOperation : BaseCompoundAssignmentExpression
    {
        protected BaseCSharpCompoundAssignmentOperation(Conversion inConversion, Conversion outConversion, Operations.BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InConversionInternal = inConversion;
            OutConversionInternal = outConversion;
        }

        internal Conversion InConversionInternal { get; }
        internal Conversion OutConversionInternal { get; }

        public override CommonConversion InConversion => InConversionInternal.ToCommonConversion();
        public override CommonConversion OutConversion => OutConversionInternal.ToCommonConversion();
    }

    internal class CSharpCompoundAssignmentOperation : BaseCSharpCompoundAssignmentOperation
    {
        public CSharpCompoundAssignmentOperation(IOperation target, IOperation value, Conversion inConversion, Conversion outConversion, Operations.BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(inConversion, outConversion, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TargetImpl = target;
            ValueImpl = value;
        }

        protected override IOperation TargetImpl { get; }
        protected override IOperation ValueImpl { get; }
    }

    internal class LazyCSharpCompoundAssignmentOperation : BaseCSharpCompoundAssignmentOperation
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;
        public LazyCSharpCompoundAssignmentOperation(Lazy<IOperation> target, Lazy<IOperation> value, Conversion inConversion, Conversion outConversion, Operations.BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : 
            base(inConversion, outConversion, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyTarget = target;
            _lazyValue = value;
        }

        protected override IOperation TargetImpl => _lazyTarget.Value;
        protected override IOperation ValueImpl => _lazyValue.Value;
    }
}
