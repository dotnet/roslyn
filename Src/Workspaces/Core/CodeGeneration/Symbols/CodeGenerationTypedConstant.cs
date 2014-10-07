using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.CodeGeneration
{
    internal class CodeGenerationTypedConstant : ITypedConstant
    {
        public TypedConstantKind Kind { get; private set; }
        public ITypeSymbol Type { get; private set; }
        public object Value { get; private set; }
        public IEnumerable<ITypedConstant> Values { get; private set; }

        public CodeGenerationTypedConstant(IArrayTypeSymbol arrayType, IEnumerable<ITypedConstant> values)
        {
            this.Kind = TypedConstantKind.Array;
            this.Type = arrayType;
            this.Values = values;
        }

        public CodeGenerationTypedConstant(TypedConstantKind kind, ITypeSymbol type, object value)
        {
            this.Kind = kind;
            this.Type = type;
            this.Value = value;
        }
    }
}