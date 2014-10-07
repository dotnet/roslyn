using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class GenericNameDefinition : SimpleNameDefinition
    {
        public IList<ITypeSymbol> TypeArguments { get; private set; }

        public GenericNameDefinition(string identifier, IList<ITypeSymbol> typeArguments)
            : base(identifier)
        {
            this.TypeArguments = typeArguments;
        }

        protected override CodeDefinition Clone()
        {
            return new GenericNameDefinition(this.Identifier, this.TypeArguments);
        }

        public override void Accept(ICodeDefinitionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(ICodeDefinitionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override TResult Accept<TArgument, TResult>(ICodeDefinitionVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.Visit(this, argument);
        }
    }
}