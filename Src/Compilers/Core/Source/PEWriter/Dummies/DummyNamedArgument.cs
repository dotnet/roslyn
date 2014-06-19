using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyNamedArgument : IMetadataNamedArgument
    {
        #region IMetadataNamedArgument Members

        public string ArgumentName
        {
            get { return Dummy.Name; }
        }

        public IMetadataExpression ArgumentValue
        {
            get { return Dummy.Expression; }
        }

        public bool IsField
        {
            get { return false; }
        }

        public object ResolvedDefinition
        {
            get { return Dummy.Property; }
        }

        #endregion

        #region IMetadataExpression Members

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        public ITypeReference Type
        {
            get { return Dummy.Type; }
        }

        #endregion
    }
}