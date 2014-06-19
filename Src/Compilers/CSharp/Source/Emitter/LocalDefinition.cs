using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Roslyn.Compilers.CSharp.Emit;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    internal class LocalDefinition : Microsoft.Cci.ILocalDefinition
    {
        private string name;
        private Microsoft.Cci.ITypeReference type;

        public LocalDefinition(string name, Microsoft.Cci.ITypeReference type)
        {
            this.name = name;
            this.type = type;
        }

        public Microsoft.Cci.IMetadataConstant CompileTimeValue
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<Microsoft.Cci.ICustomModifier> CustomModifiers
        {
            get { return Enumerable.Empty<Microsoft.Cci.ICustomModifier>(); }
        }

        public bool IsConstant
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsModified
        {
            get { return false; }
        }

        public bool IsPinned
        {
            get { return false; }
        }

        public bool IsReference
        {
            get { return false; }
        }

        public Microsoft.Cci.IMethodDefinition MethodDefinition
        {
            get { throw new NotImplementedException(); }
        }

        public Microsoft.Cci.ITypeReference Type
        {
            get { return this.type; }
        }

        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<Microsoft.Cci.ILocation> Locations
        {
            get { throw new NotImplementedException(); }
        }
    }
}