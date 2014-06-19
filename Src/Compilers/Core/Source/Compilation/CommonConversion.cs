using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public struct CommonConversion
    {
        public IMethodSymbol Method { get; private set; }
        public bool Exists { get; private set; }
        public bool IsImplicit { get; private set; }
        public bool IsExplicit { get; private set; }
        public bool IsIdentity { get; private set; }
        public bool IsNumeric { get; private set; }
        public bool IsNullLiteral { get; private set; }
        public bool IsUserDefined { get; private set; }
        public bool IsNullable { get; private set; }
        public bool IsConstantExpression { get; private set; }
        public bool IsBoxing { get; private set; }
        public bool IsAnonymousFunction { get; private set; }
        public bool IsReference { get; private set; }
        public bool IsEnumeration { get; private set; }
        public bool IsAnonymousDelegate { get; private set; }
        public bool IsLambda { get; private set; }

        internal CommonConversion(
            IMethodSymbol method,
            bool exists,
            bool isImplicit,
            bool isExplicit,
            bool isIdentity,
            bool isNumeric,
            bool isNullLiteral,
            bool isUserDefined,
            bool isNullable,
            bool isConstantExpression,
            bool isBoxing,
            bool isAnonymousFunction,
            bool isReference,
            bool isEnumeration,
            bool isAnonymousDelegate,
            bool isLambda)
            : this()
        {
            this.Method = method;
            this.Exists = exists;
            this.IsImplicit = isImplicit;
            this.IsExplicit = isExplicit;
            this.IsIdentity = isIdentity;
            this.IsNumeric = isNumeric;
            this.IsNullLiteral = isNullLiteral;
            this.IsUserDefined = isUserDefined;
            this.IsNullable = isNullable;
            this.IsConstantExpression = isConstantExpression;
            this.IsBoxing = isBoxing;
            this.IsAnonymousFunction = isAnonymousFunction;
            this.IsReference = isReference;
            this.IsEnumeration = isEnumeration;
            this.IsAnonymousDelegate = isAnonymousDelegate;
            this.IsLambda = isLambda;
        }
    }
}