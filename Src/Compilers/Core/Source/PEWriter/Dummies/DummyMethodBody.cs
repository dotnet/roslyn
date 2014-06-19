using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyMethodBody : IMethodBody
    {
        #region IMethodBody Members

        public IMethodDefinition MethodDefinition
        {
            get { return Dummy.Method; }
        }

        // public IBlockStatement Block { get { return Dummy.Block; } }
        //
        // public IOperation GetOperationAt(int offset, out int offsetOfNextOperation) {
        //  offsetOfNextOperation = -1;
        //  return Dummy.Operation;
        // }

        public IEnumerable<ILocalDefinition> LocalVariables
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocalDefinition>(); }
        }

        public bool LocalsAreZeroed
        {
            get { return false; }
        }

        public IEnumerable<IOperation> Operations
        {
            get { return IteratorHelper.GetEmptyEnumerable<IOperation>(); }
        }

        public IEnumerable<ITypeDefinition> PrivateHelperTypes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinition>(); }
        }

        public ushort MaxStack
        {
            get { return 0; }
        }

        public IEnumerable<IOperationExceptionInformation> OperationExceptionInformation
        {
            get { return IteratorHelper.GetEmptyEnumerable<IOperationExceptionInformation>(); }
        }

        #endregion

        #region IDoubleDispatcher Members

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        #endregion

        public MemoryStream IL
        {
            get { throw new NotImplementedException(); }
        }

        public SequencePoint[] SequencePoints
        {
            get { throw new NotImplementedException(); }
        }

        public SequencePoint[] Locations
        {
            get { throw new NotImplementedException(); }
        }
    }
}