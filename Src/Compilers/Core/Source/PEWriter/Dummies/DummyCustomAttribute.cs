using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyCustomAttribute : ICustomAttribute
    {
        #region ICustomAttribute Members

        public IEnumerable<IMetadataExpression> Arguments
        {
            get { return IteratorHelper.GetEmptyEnumerable<IMetadataExpression>(); }
        }

        public IMethodReference Constructor
        {
            get { return Dummy.MethodReference; }
        }

        public IEnumerable<IMetadataNamedArgument> NamedArguments
        {
            get { return IteratorHelper.GetEmptyEnumerable<IMetadataNamedArgument>(); }
        }

        public ushort NumberOfNamedArguments
        {
            get { return 0; }
        }

        public ITypeReference Type
        {
            get { return Dummy.TypeReference; }
        }

        #endregion
    }
}