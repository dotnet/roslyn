using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal abstract class Reference : Microsoft.Cci.IReference
    {
        protected readonly Module ModuleBeingBuilt;

        public Reference(Module moduleBeingBuilt)
        {
            Contract.ThrowIfNull(moduleBeingBuilt);

            this.ModuleBeingBuilt = moduleBeingBuilt;
        }

        protected abstract Symbol UnderlyingSymbol { get; }

        public override string ToString()
        {
            return UnderlyingSymbol.ToString();
        }

        IEnumerable<Microsoft.Cci.ICustomAttribute> Microsoft.Cci.IReference.Attributes
        {
            get { return Enumerable.Empty<Microsoft.Cci.ICustomAttribute>(); }

            // get { foreach (var a in GetAttributes()) yield return a; } this throws today.
        }

        public abstract void /*Microsoft.Cci.IReference*/ Dispatch(Microsoft.Cci.IMetadataVisitor visitor);

        IEnumerable<Microsoft.Cci.ILocation> Microsoft.Cci.IObjectWithLocations.Locations
        {
            get { throw new NotImplementedException(); }
        }

        Microsoft.Cci.IDefinition Microsoft.Cci.IReference.AsDefinition(object m)
        {
            return AsDefinition;
        }

        protected abstract Microsoft.Cci.IDefinition AsDefinition { get; }
    }
}
