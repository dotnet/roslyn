using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Microsoft.Cci;
using Roslyn.Compilers.Internal.MetadataReader;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;

namespace Microsoft.Cci
{
    internal class DummyArrayMethodParameter : IParameterTypeInformation
    {
        internal DummyArrayMethodParameter(ISignature containingSignature, ushort index, ITypeReference type)
        {
            this.containingSignature = containingSignature;
            this.index = index;
            this.type = type;
        }

        public ISignature ContainingSignature
        {
            get { return this.containingSignature; }
        }

        private ISignature containingSignature;

        public IEnumerable<ICustomModifier> CustomModifiers
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
        }

        public ushort Index
        {
            get { return this.index; }
        }

        private ushort index;

        public bool IsByReference
        {
            get { return false; }
        }

        public bool IsModified
        {
            get { return false; }
        }

        public ITypeReference Type
        {
            get { return type; }
        }

        private ITypeReference type;
    }
}