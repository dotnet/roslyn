using System;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class BaseCSharpCompoundAssignmentOperation : BaseCompoundAssignmentExpression
    {
        private static readonly CommonConversion CSharpConversion = new CommonConversion(exists: true, isIdentity: true, isNumeric: false, isReference: false, methodSymbol: null);

        protected BaseCSharpCompoundAssignmentOperation(Operations.BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        public override CommonConversion InConversion => CSharpConversion;
        public override CommonConversion OutConversion => CSharpConversion;
    }

    internal class CSharpCompoundAssignmentOperation : BaseCSharpCompoundAssignmentOperation
    {
        public CSharpCompoundAssignmentOperation(IOperation target, IOperation value, Operations.BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
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
        public LazyCSharpCompoundAssignmentOperation(Lazy<IOperation> target, Lazy<IOperation> value, Operations.BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyTarget = target;
            _lazyValue = value;
        }

        protected override IOperation TargetImpl => _lazyTarget.Value;
        protected override IOperation ValueImpl => _lazyValue.Value;
    }
}
