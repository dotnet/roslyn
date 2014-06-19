using System.Collections.Generic;
using Microsoft.Cci;

namespace Roslyn.Compilers.CodeGen
{
    internal sealed class CustomAttribute : ICustomAttribute
    {
        private readonly IMethodReference constructor;
        private readonly ITypeReference type;
        private readonly ReadOnlyArray<MetadataConstant> positionalArguments;
        private readonly ReadOnlyArray<IMetadataNamedArgument> namedArguments;

        public CustomAttribute(
            IMethodReference constructor,
            ITypeReference type,
            ReadOnlyArray<MetadataConstant> positionalArguments) :
            this(constructor, type, positionalArguments, ReadOnlyArray<IMetadataNamedArgument>.Empty)
        {
        }

        public CustomAttribute(
            IMethodReference constructor,
            ITypeReference type,
            ReadOnlyArray<MetadataConstant> positionalArguments,
            ReadOnlyArray<IMetadataNamedArgument> namedArguments)
        {
            this.constructor = constructor;
            this.type = type;
            this.positionalArguments = positionalArguments;
            this.namedArguments = namedArguments;
        }

        IEnumerable<IMetadataExpression> ICustomAttribute.Arguments
        {
            get { return this.positionalArguments.AsEnumerable(); }
        }

        IMethodReference ICustomAttribute.Constructor
        {
            get { return this.constructor; }
        }

        IEnumerable<IMetadataNamedArgument> ICustomAttribute.NamedArguments
        {
            get { return this.namedArguments.AsEnumerable(); }
        }

        ushort ICustomAttribute.NumberOfNamedArguments
        {
            get { return (ushort)this.namedArguments.Count; }
        }

        ITypeReference ICustomAttribute.Type
        {
            get { return this.type; }
        }
    }
}
